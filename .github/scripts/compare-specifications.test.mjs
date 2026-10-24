// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, unlinkSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { checkInventory, checkRun, compare, parseXml, projectName, projectsIn } from './compare-specifications.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '../..');
const script = join(here, 'compare-specifications.mjs');
const NS = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010';
const ORDINARY = projectsIn('ordinary').map(([path]) => projectName(path));
const BASE = [['A.when_x', 'should_one', 'Passed'], ['A.when_x', 'should_two', 'Failed'], ['B.when_y', 'should_skip', 'NotExecuted']];
const parity = pattern => ({ name: 'ParityError', message: pattern });

function trx(cases, { runId = 'run-1', total, passed } = {}) {
    const definitions = [];
    const results = [];
    cases.forEach(([className, method, outcome, display = `${className}.${method}`], index) => {
        const id = `id-${runId}-${index}`;
        definitions.push(`<UnitTest name="${display}" id="${id}"><TestMethod className="${className}" name="${method}" /></UnitTest>`);
        results.push(`<UnitTestResult testId="${id}" testName="${display}" outcome="${outcome}" startTime="${runId}" />`);
    });
    const realPassed = passed ?? cases.filter(c => c[2] === 'Passed').length;
    const realTotal = total ?? cases.length;
    return `<TestRun id="${runId}" xmlns="${NS}"><Results>${results.join('')}</Results>` +
        `<TestDefinitions>${definitions.join('')}</TestDefinitions>` +
        `<ResultSummary outcome="Completed"><Counters total="${realTotal}" passed="${realPassed}" /></ResultSummary></TestRun>`;
}

const discovery = cases => `Test run for something\nThe following Tests are available:\n${cases.map(c => `    ${c[0]}.${c[1]}`).join('\n')}\n`;

function fixture(t) {
    mkdirSync(join(root, '.ai-work'), { recursive: true });
    const dir = mkdtempSync(join(root, '.ai-work/compare-specifications-test-'));
    t.after(() => rmSync(dir, { recursive: true }));
    const writeRun = (side, project, cases, { trxName = 'results.trx', discoveryCases = cases, ...options } = {}) => {
        const directory = join(dir, side, project);
        mkdirSync(directory, { recursive: true });
        for (const stale of readdirSync(directory).filter(n => n.endsWith('.trx'))) unlinkSync(join(directory, stale));
        writeFileSync(join(directory, trxName), trx(cases, { runId: side, ...options }));
        writeFileSync(join(directory, 'discovery.txt'), discovery(discoveryCases));
    };
    const populate = cases => {
        for (const name of ORDINARY) {
            writeRun('baseline', name, cases);
            writeRun('candidate', name, cases, { trxName: `${name}.trx` });
        }
    };
    const run = (scope = 'ordinary') => compare(join(dir, 'baseline'), join(dir, 'candidate'), scope);
    return { dir, writeRun, populate, run };
}

test('identical runs pass despite run metadata and file names', t => {
    const f = fixture(t);
    f.populate(BASE);
    const counts = f.run();
    assert.deepEqual([...counts.keys()].sort(), [...ORDINARY].sort());
    assert.ok([...counts.values()].every(count => count === 3));
});

test('replaced case with same count is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', [...BASE.slice(0, 2), ['B.when_y', 'should_other', 'NotExecuted']], { trxName: 'x.trx' });
    assert.throws(f.run, parity(/Orleans\.Specs: case missing from candidate: B\.when_y\.should_skip[\s\S]*case added in candidate: B\.when_y\.should_other/));
});

test('outcome change with same totals is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', [['A.when_x', 'should_one', 'Failed'], ['A.when_x', 'should_two', 'Passed'], BASE[2]], { trxName: 'x.trx' });
    assert.throws(f.run, parity(/outcome changed for A\.when_x\.should_one: Passed -> Failed/));
});

test('lost skipped case is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', BASE.slice(0, 2), { trxName: 'x.trx' });
    assert.throws(f.run, parity(/case missing from candidate: B\.when_y\.should_skip/));
});

test('skipped becoming passed is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', [...BASE.slice(0, 2), ['B.when_y', 'should_skip', 'Passed']], { trxName: 'x.trx' });
    assert.throws(f.run, parity(/NotExecuted -> Passed/));
});

test('duplicate identity is ambiguous', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', [...BASE, BASE[0]], { trxName: 'x.trx' });
    assert.throws(f.run, parity(/ambiguous duplicate case identity 'A\.when_x\.should_one'/));
});

test('total that disagrees with results is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', BASE, { trxName: 'x.trx', total: 4 });
    assert.throws(f.run, parity(/Orleans\.Specs: TRX total 4 does not match 3 results/));
});

test('counter contradicting results is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', BASE, { trxName: 'x.trx', passed: 3 });
    assert.throws(f.run, parity(/counter passed=3 contradicts 1 Passed/));
});

test('malformed TRX is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    writeFileSync(join(f.dir, 'candidate/Orleans.Specs/Orleans.Specs.trx'), '<TestRun');
    assert.throws(f.run, parity(/Orleans\.Specs: invalid TRX/));
});

test('zero total is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', [], { trxName: 'x.trx' });
    assert.throws(f.run, parity(/Orleans\.Specs: expected a positive TRX test total/));
});

test('missing project results are rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    rmSync(join(f.dir, 'candidate/Orleans.Storage.Specs'), { recursive: true });
    assert.throws(f.run, parity(/Orleans\.Storage\.Specs: missing results directory/));
});

test('empty results are rejected', t => {
    assert.throws(fixture(t).run, parity(/missing results directory/));
});

test('extra TRX is ambiguous', t => {
    const f = fixture(t);
    f.populate(BASE);
    writeFileSync(join(f.dir, 'candidate/Orleans.Specs/extra.trx'), trx(BASE));
    assert.throws(f.run, parity(/expected exactly one TRX/));
});

test('discovery must match execution total', t => {
    const f = fixture(t);
    f.populate(BASE);
    f.writeRun('candidate', 'Orleans.Specs', BASE, { trxName: 'x.trx', discoveryCases: BASE.slice(0, 2) });
    assert.throws(f.run, parity(/discovery listed 2 tests but the TRX holds 3/));
});

test('readable and underscore display names with the same class and method are equal', t => {
    const f = fixture(t);
    f.populate(BASE);
    const readable = BASE.map(([c, m, o]) => [c, m, o, `${c.replace(/_/g, ' ')} ${m.replace(/_/g, ' ')}`]);
    f.writeRun('candidate', 'Orleans.Specs', readable, { trxName: 'x.trx', discoveryCases: readable.map(([, , , d]) => ['readable', d]) });
    assert.equal(f.run().get('Orleans.Specs'), 3);
});

test('extra case in candidate is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    const more = [...BASE, ['B.when_y', 'should_extra', 'Passed']];
    f.writeRun('candidate', 'Orleans.Specs', more, { trxName: 'x.trx' });
    assert.throws(f.run, parity(/case added in candidate: B\.when_y\.should_extra/));
});

test('discovery multiplicity differing between sides is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    writeFileSync(join(f.dir, 'candidate/Orleans.Specs/discovery.txt'), discovery([...BASE, BASE[0]]));
    assert.throws(f.run, parity(/discovery listed 4 tests but the TRX holds 3/));
});

test('missing discovery is rejected', t => {
    const f = fixture(t);
    f.populate(BASE);
    unlinkSync(join(f.dir, 'candidate/Orleans.Specs/discovery.txt'));
    assert.throws(f.run, parity(/Cannot read discovery output/));
});

test('all failures are reported together', t => {
    const f = fixture(t);
    f.populate(BASE);
    rmSync(join(f.dir, 'candidate/Orleans.Storage.Specs'), { recursive: true });
    rmSync(join(f.dir, 'candidate/Orleans.Chronicle.Specs'), { recursive: true });
    assert.throws(f.run, parity(/Orleans\.Storage\.Specs[\s\S]*Orleans\.Chronicle\.Specs/));
});

test('CLI exit codes', t => {
    const f = fixture(t);
    f.populate(BASE);
    const args = ['--baseline', join(f.dir, 'baseline'), '--candidate', join(f.dir, 'candidate')];
    const ok = spawnSync(process.execPath, [script, ...args], { encoding: 'utf8' });
    assert.equal(ok.status, 0, ok.stderr);
    assert.match(ok.stdout, /Orleans\.Specs: 3 cases identical/);
    rmSync(join(f.dir, 'candidate/Orleans.Specs'), { recursive: true });
    const bad = spawnSync(process.execPath, [script, ...args], { encoding: 'utf8' });
    assert.equal(bad.status, 1);
    assert.doesNotMatch(bad.stdout, /identical/);
});

test('check-run accepts a consistent run', t => {
    const f = fixture(t);
    f.writeRun('candidate', 'P', BASE);
    assert.equal(checkRun(join(f.dir, 'candidate/P/results.trx'), join(f.dir, 'candidate/P/discovery.txt')), 3);
});

test('check-run rejects zero discovery', t => {
    const f = fixture(t);
    f.writeRun('candidate', 'P', BASE, { discoveryCases: [] });
    assert.throws(() => checkRun(join(f.dir, 'candidate/P/results.trx'), join(f.dir, 'candidate/P/discovery.txt')), parity(/No discovered tests/));
});

test('xml reader decodes entities and rejects mismatched tags', () => {
    assert.equal(parseXml('<a x="1 &amp; 2"><b/></a>').attributes.x, '1 & 2');
    assert.throws(() => parseXml('<a><b></a></b>'));
});

function inventoryTree(t, { extra = [], omit = [] } = {}) {
    const f = fixture(t);
    const members = [];
    const write = path => {
        mkdirSync(dirname(join(f.dir, path)), { recursive: true });
        writeFileSync(join(f.dir, path), '<Project />');
    };
    for (const [path] of projectsIn('all')) {
        if (omit.includes(path)) continue;
        write(path);
        members.push(`<Project Path="${path}" />`);
    }
    extra.forEach(write);
    writeFileSync(join(f.dir, 'Orleans.slnx'), `<Solution>${members.join('')}</Solution>`);
    return f.dir;
}

test('inventory has six ordinary and two integration projects', () => {
    assert.equal(projectsIn('ordinary').length, 6);
    assert.equal(projectsIn('integration').length, 2);
    assert.equal(projectsIn('all').length, 8);
    assert.ok(projectsIn('ordinary').some(([p]) => p === 'Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj'));
    assert.ok(!projectsIn('ordinary').some(([p]) => p.includes('Integration')));
});

test('complete tree passes', t => checkInventory(inventoryTree(t)));

test('unknown discovered project is rejected', t => {
    assert.throws(() => checkInventory(inventoryTree(t, { extra: ['Source/New.Specs/New.Specs.csproj'] })), parity(/unknown=.*New\.Specs/));
});

test('missing project is rejected', t => {
    assert.throws(() => checkInventory(inventoryTree(t, { omit: ['Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj'] })), parity(/missing=.*GrainCompatibility\.Specs/));
});

test('project missing from solution is rejected', t => {
    const dir = inventoryTree(t);
    const solution = join(dir, 'Orleans.slnx');
    writeFileSync(solution, readFileSync(solution, 'utf8').replace('<Project Path="Integration/Jobs.Integration/Jobs.Integration.csproj" />', ''));
    assert.throws(() => checkInventory(dir), parity(/missing from Orleans\.slnx/));
});

test('unknown scope is rejected', () => {
    assert.throws(() => projectsIn('everything'), parity(/Unknown scope/));
});

test('real repository inventory matches', () => checkInventory(root));
