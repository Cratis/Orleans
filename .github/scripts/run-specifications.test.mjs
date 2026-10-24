// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

const scripts = dirname(fileURLToPath(import.meta.url));
const root = resolve(scripts, '../..');

const ordinary = [
    ['Source/Orleans.Specs/Orleans.Specs.csproj', 'Cratis.Orleans.Specs'],
    ['Source/Orleans.Storage.Specs/Orleans.Storage.Specs.csproj', 'Cratis.Orleans.Storage.Specs'],
    ['Source/Orleans.Storage.MongoDB.Specs/Orleans.Storage.MongoDB.Specs.csproj', 'Cratis.Orleans.Storage.MongoDB.Specs'],
    ['Source/Orleans.Storage.Sql.Specs/Orleans.Storage.Sql.Specs.csproj', 'Cratis.Orleans.Storage.Sql.Specs'],
    ['Source/Orleans.Chronicle.Specs/Orleans.Chronicle.Specs.csproj', 'Cratis.Orleans.Chronicle.Specs'],
    ['Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj', 'Cratis.Orleans.Tools.GrainCompatibility.Specs']
];
const integration = [
    ['Integration/Jobs.Integration/Jobs.Integration.csproj', 'Cratis.Orleans.Jobs.Integration'],
    ['Integration/Chronicle.Integration/Chronicle.Integration.csproj', 'Cratis.Orleans.Chronicle.Integration']
];
const all = [...ordinary, ...integration];
const ns = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010';

function trx(count, name) {
    const ids = Array.from({ length: count }, (_, i) => i);
    return `<TestRun xmlns="${ns}"><Results>${ids.map(i => `<UnitTestResult testId="${i}" testName="${name}${i}" outcome="Passed" />`).join('')}</Results>`
        + `<TestDefinitions>${ids.map(i => `<UnitTest id="${i}" name="${name}${i}"><TestMethod className="${name}" name="m${i}" /></UnitTest>`).join('')}</TestDefinitions>`
        + `<ResultSummary><Counters total="${count}" passed="${count}" /></ResultSummary></TestRun>`;
}

function fixture(t, options = {}) {
    const {
        omitProject = '', extraProject = '', omitSolution = '', missing = '', exits = {}, counts = {}, discovered = {},
        noTrx = '', documents = {}, windows = false, discoveryExit = {}, args = ['Debug'], preexisting = false
    } = options;
    mkdirSync(join(root, '.ai-work'), { recursive: true });
    const directory = mkdtempSync(join(root, '.ai-work/run-specifications-test-'));
    t.after(() => rmSync(directory, { recursive: true }));
    const target = join(directory, '.github/scripts');
    mkdirSync(target, { recursive: true });
    for (const file of ['run-specifications.sh', 'compare-specifications.mjs']) copyFileSync(join(scripts, file), join(target, file));
    const tools = join(directory, 'tools');
    mkdirSync(tools);
    const order = join(directory, 'order.log');
    writeFileSync(join(tools, 'dotnet'), `#!/usr/bin/env bash
name=$(basename "$2" .csproj)
case "$name" in ${Object.keys(discoveryExit).map(n => `${n}) exit ${discoveryExit[n]} ;;`).join(' ')} esac
[[ "$*" == *"--no-build"* && "$*" == *"--no-restore"* && "$*" == *"--list-tests"* ]] || { echo "unexpected dotnet $*"; exit 9; }
echo 'The following Tests are available:'
count=$(cat "${directory}/discovered/$name" 2>/dev/null || echo 1)
for ((i = 0; i < count; i++)); do echo "    $name$i"; done
`, { mode: 0o755 });
    mkdirSync(join(directory, 'discovered'));
    const rows = all.filter(([path]) => path !== omitProject);
    const solution = all.filter(([path]) => path !== omitSolution).map(([path]) => `<Project Path="${path}" />`);
    writeFileSync(join(directory, 'Orleans.slnx'), `<Solution>${solution.join('')}</Solution>`);
    const projectFiles = [...rows.map(([path]) => path), ...(extraProject ? [extraProject] : [])];
    for (const path of projectFiles) {
        mkdirSync(dirname(join(directory, path)), { recursive: true });
        writeFileSync(join(directory, path), '<Project />');
    }
    for (const [path, assembly] of all) {
        const name = path.split('/').pop().replace('.csproj', '');
        writeFileSync(join(directory, 'discovered', name), String(discovered[name] ?? counts[name] ?? 1));
        const output = join(directory, dirname(path), 'bin/Debug/net10.0');
        mkdirSync(output, { recursive: true });
        if (path === omitProject || assembly === missing) continue;
        const document = documents[name] ?? trx(counts[name] ?? 1, name);
        writeFileSync(join(output, `${assembly}${windows ? '.exe' : ''}`), `#!/usr/bin/env bash
set -eu
[[ "$1" == -noLogo && "$2" == -noColor && "$3" == -result-trx ]]
[[ ! -e "${directory}/running" ]] || { echo 'CONCURRENT'; exit 99; }
touch "${directory}/running"
echo 'start ${name}' >> "${order}"
echo '${name} executed'
${name === noTrx ? ':' : `printf '%s' '${document.replaceAll("'", "'\\''")}' > "$4"`}
echo 'end ${name}' >> "${order}"
rm "${directory}/running"
exit ${exits[name] ?? 0}
`, { mode: 0o755 });
    }
    const results = join(directory, 'results');
    if (preexisting) {
        mkdirSync(join(results, 'Orleans.Specs'), { recursive: true });
        writeFileSync(join(results, 'Orleans.Specs/Orleans.Specs.trx'), trx(1, 'Stale'));
    }
    const run = spawnSync('bash', [join(target, 'run-specifications.sh'), ...args.map(a => a === '@results' ? results : a)], {
        encoding: 'utf8',
        timeout: 60000,
        env: { ...process.env, OS: windows ? 'Windows_NT' : 'Linux', PATH: `${tools}:${process.env.PATH}` }
    });
    assert.ifError(run.error);
    const output = run.stdout + run.stderr;
    return { ...run, output, directory, results, started: existsSync(order) ? readFileSync(order, 'utf8') : '' };
}

const executed = result => (result.output.match(/ executed/g) ?? []).length;

test('all scope runs six ordinary and two integration projects sequentially', t => {
    const result = fixture(t, { args: ['Debug', '--results', '@results'] });
    assert.equal(result.status, 0, result.output);
    assert.match(result.output, /Running 8 specification projects sequentially/);
    assert.match(result.output, /Validated 8 specification projects, 8 tests/);
    assert.equal(executed(result), 8);
    assert.equal(result.started.split('\n').filter(l => l.startsWith('start')).length, 8);
    assert.doesNotMatch(result.output, /CONCURRENT/);
    assert.equal(readdirSync(join(result.results, 'Orleans.Specs')).filter(n => n.endsWith('.trx')).length, 1);
});

test('ordinary scope never runs integration projects', t => {
    const result = fixture(t, { args: ['Debug', '--scope', 'ordinary'] });
    assert.equal(result.status, 0, result.output);
    assert.match(result.output, /Validated 6 specification projects/);
    assert.doesNotMatch(result.output, /Integration/);
    assert.equal(executed(result), 6);
});

test('integration scope runs only the two integration projects', t => {
    const result = fixture(t, { args: ['Debug', '--scope', 'integration'] });
    assert.equal(result.status, 0, result.output);
    assert.match(result.output, /Validated 2 specification projects/);
    assert.equal(executed(result), 2);
});

test('runs exactly one selected project', t => {
    const result = fixture(t, { args: ['Debug', '--project', 'Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj'] });
    assert.equal(result.status, 0, result.output);
    assert.match(result.output, /Validated 1 specification projects/);
    assert.equal(executed(result), 1);
});

test('rejects an unknown project and one outside the scope before running anything', async t => {
    for (const [args, message] of [
        [['Debug', '--project', 'Source/Other.Specs/Other.Specs.csproj'], /Unknown specification project/],
        [['Debug', '--project', 'Source/Orleans.Specs'], /Unknown specification project/],
        [['Debug', '--project', 'Orleans.Specs/Orleans.Specs.csproj'], /Unknown specification project/],
        [['Debug', '--scope', 'ordinary', '--project', 'Integration/Jobs.Integration/Jobs.Integration.csproj'], /outside scope 'ordinary'/]
    ]) {
        await t.test(args.join(' '), child => {
            const result = fixture(child, { args });
            assert.equal(result.status, 2, result.output);
            assert.match(result.output, message);
            assert.equal(executed(result), 0);
        });
    }
});

test('rejects an incomplete project population before starting any project', t => {
    const result = fixture(t, { omitProject: 'Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj' });
    assert.equal(result.status, 2, result.output);
    assert.match(result.output, /inventory mismatch: missing=.*GrainCompatibility.Specs/);
    assert.equal(executed(result), 0);
});

test('rejects an extra discovered specification project', t => {
    const result = fixture(t, { extraProject: 'Source/Extra.Specs/Extra.Specs.csproj' });
    assert.equal(result.status, 2, result.output);
    assert.match(result.output, /unknown=.*Extra.Specs/);
    assert.equal(executed(result), 0);
});

test('rejects a project missing from the solution', t => {
    const result = fixture(t, { omitSolution: 'Integration/Jobs.Integration/Jobs.Integration.csproj' });
    assert.equal(result.status, 2, result.output);
    assert.match(result.output, /missing from Orleans.slnx/);
    assert.equal(executed(result), 0);
});

test('checks every executable before starting any project', t => {
    const result = fixture(t, { missing: 'Cratis.Orleans.Chronicle.Integration' });
    assert.equal(result.status, 2, result.output);
    assert.match(result.output, /Missing specification executable: .*Cratis.Orleans.Chronicle.Integration/);
    assert.equal(executed(result), 0);
});

test('rejects a scope with no projects and an unknown scope', t => {
    const result = fixture(t, { args: ['Debug', '--scope', 'everything'] });
    assert.equal(result.status, 2, result.output);
    assert.equal(executed(result), 0);
});

test('finishes every project and preserves the first nonzero child status, even with a positive TRX', t => {
    const result = fixture(t, { exits: { 'Orleans.Storage.Specs': 7, 'Orleans.Specs': 3 } });
    assert.equal(result.status, 3, result.output);
    assert.match(result.output, /::error::Orleans.Specs exited with 3/);
    assert.match(result.output, /::error::Orleans.Storage.Specs exited with 7/);
    assert.equal(executed(result), 8);
    assert.doesNotMatch(result.output, /Validated/);
});

test('rejects zero tests even when the executable succeeds', t => {
    const result = fixture(t, { documents: { 'Orleans.Storage.Specs': trx(0, 'Zero') }, counts: {} });
    assert.equal(result.status, 1, result.output);
    assert.match(result.output, /::error::.*Orleans.Storage.Specs/);
    assert.doesNotMatch(result.output, /Validated/);
});

test('rejects a missing TRX', t => {
    const result = fixture(t, { noTrx: 'Orleans.Chronicle.Specs' });
    assert.equal(result.status, 1, result.output);
    assert.match(result.output, /Orleans.Chronicle.Specs produced no valid TRX/);
});

test('rejects a malformed TRX', t => {
    const result = fixture(t, { documents: { 'Orleans.Specs': '<TestRun' } });
    assert.equal(result.status, 1, result.output);
    assert.match(result.output, /Orleans.Specs: invalid TRX/);
});

test('rejects zero discovery', t => {
    const result = fixture(t, { discovered: { 'Orleans.Specs': 0 } });
    assert.equal(result.status, 1, result.output);
    assert.match(result.output, /No discovered tests/);
});

test('rejects a discovery and execution mismatch', t => {
    const result = fixture(t, { discovered: { 'Orleans.Specs': 3 } });
    assert.equal(result.status, 1, result.output);
    assert.match(result.output, /discovery listed 3 tests but the TRX holds 1/);
});

test('a failing discovery fails the project without running it', t => {
    const result = fixture(t, { discoveryExit: { 'Orleans.Specs': 5 } });
    assert.equal(result.status, 5, result.output);
    assert.match(result.output, /Orleans.Specs discovery exited with 5/);
    assert.equal(executed(result), 7);
});

test('does not reuse stale results for a project', t => {
    const result = fixture(t, { args: ['Debug', '--results', '@results'], preexisting: true, noTrx: 'Orleans.Specs' });
    assert.equal(result.status, 1, result.output);
    assert.match(result.output, /Orleans.Specs produced no valid TRX/);
});

test('accepts the Windows executable suffix', t => {
    const result = fixture(t, { windows: true, args: ['Debug', '--scope', 'ordinary'] });
    assert.equal(result.status, 0, result.output);
    assert.equal(executed(result), 6);
});

test('never builds or restores', t => {
    const source = readFileSync(join(scripts, 'run-specifications.sh'), 'utf8');
    assert.doesNotMatch(source, /dotnet (build|restore|publish)/);
    assert.match(source, /--no-build --no-restore --list-tests/);
});
