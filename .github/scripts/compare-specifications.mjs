#!/usr/bin/env node
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Canonical specification project inventory, TRX validation and exact before/after parity comparison.
//
// Subcommands are selected by flags:
//   --list-projects SCOPE                    print "<project path>\t<assembly name>" rows (ordinary|integration|all)
//   --check-inventory ROOT                   verify the inventory equals recursive discovery and solution membership
//   --check-run TRX --discovery FILE         verify one executed run (positive total, consistent, matches discovery)
//   --baseline DIR --candidate DIR --scope S compare identities and per-case outcomes per project
//   --self-test                              run compare-specifications.test.mjs

import { spawnSync } from 'node:child_process';
import { existsSync, globSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { basename, dirname, join, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';

// The one canonical inventory: [project path, executable AssemblyName, scope].
export const PROJECTS = [
    ['Source/Orleans.Specs/Orleans.Specs.csproj', 'Cratis.Orleans.Specs', 'ordinary'],
    ['Source/Orleans.Storage.Specs/Orleans.Storage.Specs.csproj', 'Cratis.Orleans.Storage.Specs', 'ordinary'],
    ['Source/Orleans.Storage.MongoDB.Specs/Orleans.Storage.MongoDB.Specs.csproj', 'Cratis.Orleans.Storage.MongoDB.Specs', 'ordinary'],
    ['Source/Orleans.Storage.Sql.Specs/Orleans.Storage.Sql.Specs.csproj', 'Cratis.Orleans.Storage.Sql.Specs', 'ordinary'],
    ['Source/Orleans.Chronicle.Specs/Orleans.Chronicle.Specs.csproj', 'Cratis.Orleans.Chronicle.Specs', 'ordinary'],
    ['Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj', 'Cratis.Orleans.Tools.GrainCompatibility.Specs', 'ordinary'],
    ['Integration/Jobs.Integration/Jobs.Integration.csproj', 'Cratis.Orleans.Jobs.Integration', 'integration'],
    ['Integration/Chronicle.Integration/Chronicle.Integration.csproj', 'Cratis.Orleans.Chronicle.Integration', 'integration']
];
export const SCOPES = ['ordinary', 'integration', 'all'];
const EXCLUDED_DIRECTORIES = new Set(['bin', 'obj', 'node_modules', '.git', '.ai-work', 'TestResults']);

/** A validation or parity failure; the message names the project and case. */
export class ParityError extends Error {
    constructor(message) {
        super(message);
        this.name = 'ParityError';
    }
}

export function projectsIn(scope) {
    if (!SCOPES.includes(scope)) throw new ParityError(`Unknown scope '${scope}'`);
    return PROJECTS.filter(([, , kind]) => scope === 'all' || kind === scope).map(([path, assembly]) => [path, assembly]);
}

export const projectName = path => basename(path).slice(0, -'.csproj'.length);

function readText(path) {
    return readFileSync(path, 'utf8');
}

export function discoverProjects(root) {
    const found = new Set();
    for (const path of globSync('**/*.csproj', { cwd: root })) {
        const parts = path.split(sep);
        if (parts.some(part => EXCLUDED_DIRECTORIES.has(part))) continue;
        const name = parts[parts.length - 1];
        if (name.endsWith('.Specs.csproj') || name.endsWith('.Integration.csproj')) found.add(parts.join('/'));
    }
    return found;
}

const sorted = values => [...values].sort();

export function checkInventory(root) {
    const expected = new Set(PROJECTS.map(([path]) => path));
    const found = discoverProjects(root);
    const missing = sorted([...expected].filter(p => !found.has(p)));
    const unknown = sorted([...found].filter(p => !expected.has(p)));
    if (missing.length || unknown.length) {
        throw new ParityError(`Specification project inventory mismatch: missing=${JSON.stringify(missing)} unknown=${JSON.stringify(unknown)}`);
    }
    const solution = join(root, 'Orleans.slnx');
    const members = new Set(existsSync(solution) ? [...readText(solution).matchAll(/Path="([^"]+)"/g)].map(m => m[1]) : []);
    const absent = sorted([...expected].filter(p => !members.has(p)));
    if (absent.length) throw new ParityError(`Projects missing from Orleans.slnx: ${JSON.stringify(absent)}`);
}

/** Return the multiset (Map name -> count) of test names listed by `dotnet test --list-tests`. */
export function parseDiscovery(path) {
    let lines;
    try {
        lines = readText(path).split(/\r?\n/);
    } catch (error) {
        throw new ParityError(`Cannot read discovery output ${path}: ${error.message}`);
    }
    const names = new Map();
    let listing = false;
    for (const line of lines) {
        if (!listing) {
            listing = line.trim() === 'The following Tests are available:';
            continue;
        }
        if ((line[0] === ' ' || line[0] === '\t') && line.trim()) {
            names.set(line.trim(), (names.get(line.trim()) ?? 0) + 1);
        } else if (line.trim()) {
            break;
        }
    }
    if (names.size === 0) throw new ParityError(`No discovered tests in ${path}`);
    return names;
}

const total = counter => [...counter.values()].reduce((sum, count) => sum + count, 0);

const ENTITIES = { quot: '"', apos: "'", lt: '<', gt: '>', amp: '&' };
const decode = text => text.replace(/&(#x[0-9a-fA-F]+|#\d+|\w+);/g, (whole, entity) => {
    if (entity[0] === '#') {
        return String.fromCodePoint(entity[1] === 'x' ? parseInt(entity.slice(2), 16) : parseInt(entity.slice(1), 10));
    }
    return ENTITIES[entity] ?? whole;
});

/** Minimal well-formedness-checking XML reader: elements and attributes only, which is all a TRX needs. */
export function parseXml(text) {
    const root = { name: '#document', attributes: {}, children: [] };
    const stack = [root];
    const token = /<!--[\s\S]*?-->|<\?[\s\S]*?\?>|<!\[CDATA\[[\s\S]*?\]\]>|<!DOCTYPE[^>]*>|<(\/?)([^\s/>]+)((?:\s+[^\s=/>]+\s*=\s*(?:"[^"]*"|'[^']*'))*)\s*(\/?)>/gy;
    let position = 0;
    while (position < text.length) {
        const next = text.indexOf('<', position);
        if (next === -1) break;
        if (text.slice(position, next).trim() && stack.length === 1) throw new Error('text outside the root element');
        token.lastIndex = next;
        const match = token.exec(text);
        if (!match) throw new Error(`malformed markup at offset ${next}`);
        position = token.lastIndex;
        const [, closing, rawName, rawAttributes, selfClosing] = match;
        if (rawName === undefined) continue;
        const name = rawName.slice(rawName.indexOf(':') + 1);
        if (closing) {
            const open = stack.pop();
            if (stack.length === 0 || open.name !== name) throw new Error(`unexpected closing tag </${rawName}>`);
            continue;
        }
        const attributes = {};
        for (const [, key, quoted] of rawAttributes.matchAll(/([^\s=/>]+)\s*=\s*("[^"]*"|'[^']*')/g)) {
            attributes[key.slice(key.indexOf(':') + 1)] = decode(quoted.slice(1, -1));
        }
        const element = { name, attributes, children: [] };
        stack[stack.length - 1].children.push(element);
        if (!selfClosing) stack.push(element);
    }
    if (stack.length !== 1) throw new Error('unclosed element');
    if (root.children.length !== 1) throw new Error('expected exactly one root element');
    return root.children[0];
}

const kids = (element, name) => (element ? element.children.filter(child => child.name === name) : []);
const path = (element, ...names) => names.reduce((nodes, name) => nodes.flatMap(node => kids(node, name)), [element]);

/** Return { total, outcomes: Map identity -> [outcomes] } after validating internal consistency. */
export function parseTrx(file, project = '?') {
    let root;
    try {
        root = parseXml(readText(file));
    } catch (error) {
        throw new ParityError(`${project}: invalid TRX ${file}: ${error.message}`);
    }
    if (root.name !== 'TestRun') throw new ParityError(`${project}: ${file} is not a TestRun`);
    const counters = path(root, 'ResultSummary', 'Counters');
    if (counters.length !== 1) throw new ParityError(`${project}: expected one TRX result summary counter in ${file}`);
    const declaredTotal = counters[0].attributes.total ?? '0';
    const totalCases = Number(declaredTotal);
    if (!/^-?\d+$/.test(declaredTotal.trim())) throw new ParityError(`${project}: malformed TRX total in ${file}`);
    if (totalCases <= 0) throw new ParityError(`${project}: expected a positive TRX test total in ${file}`);
    const definitions = new Map();
    for (const unit of path(root, 'TestDefinitions', 'UnitTest')) {
        const method = kids(unit, 'TestMethod')[0];
        const className = method?.attributes.className;
        const name = method?.attributes.name;
        if (!unit.attributes.id || !className || !name) {
            throw new ParityError(`${project}: test definition without id/class/method in ${file}: ${unit.attributes.name}`);
        }
        definitions.set(unit.attributes.id, `${className}.${name}`);
    }
    const results = path(root, 'Results', 'UnitTestResult');
    if (results.length !== totalCases) {
        throw new ParityError(`${project}: TRX total ${totalCases} does not match ${results.length} results in ${file}`);
    }
    const outcomes = new Map();
    const tally = new Map();
    for (const result of results) {
        const identity = definitions.get(result.attributes.testId);
        if (identity === undefined) {
            throw new ParityError(`${project}: result '${result.attributes.testName}' has no test definition in ${file}`);
        }
        const outcome = result.attributes.outcome;
        if (!outcome) throw new ParityError(`${project}: result '${identity}' has no outcome in ${file}`);
        outcomes.set(identity, [...(outcomes.get(identity) ?? []), outcome]);
        tally.set(outcome, (tally.get(outcome) ?? 0) + 1);
    }
    for (const [identity, values] of outcomes) {
        if (values.length > 1) {
            throw new ParityError(`${project}: ambiguous duplicate case identity '${identity}' (${values.length} results) in ${file}`);
        }
    }
    for (const [attribute, outcome] of [['passed', 'Passed'], ['failed', 'Failed']]) {
        const declared = counters[0].attributes[attribute];
        const actual = tally.get(outcome) ?? 0;
        if (declared !== undefined && Number(declared) !== actual) {
            throw new ParityError(`${project}: counter ${attribute}=${declared} contradicts ${actual} ${outcome} results in ${file}`);
        }
    }
    return { total: totalCases, outcomes };
}

export function checkRun(trx, discovery, project = '?') {
    const { total: executed } = parseTrx(trx, project);
    const discovered = total(parseDiscovery(discovery));
    if (discovered !== executed) throw new ParityError(`${project}: discovery listed ${discovered} tests but the TRX holds ${executed}`);
    return executed;
}

function findSingleTrx(directory, project) {
    const files = readdirSync(directory).filter(name => name.endsWith('.trx')).sort();
    if (files.length !== 1) throw new ParityError(`${project}: expected exactly one TRX in ${directory}, found ${files.length}`);
    return join(directory, files[0]);
}

function loadRun(directory, project) {
    if (!existsSync(directory) || !statSync(directory).isDirectory()) {
        throw new ParityError(`${project}: missing results directory ${directory}`);
    }
    const { total: executed, outcomes } = parseTrx(findSingleTrx(directory, project), project);
    const discovery = parseDiscovery(join(directory, 'discovery.txt'));
    if (total(discovery) !== executed) {
        throw new ParityError(`${project}: discovery listed ${total(discovery)} tests but the TRX holds ${executed}`);
    }
    return { outcomes, discovery };
}

const subtract = (left, right) => sorted([...left].flatMap(([name, count]) => Array(Math.max(0, count - (right.get(name) ?? 0))).fill(name)));
const sameCounter = (left, right) => left.size === right.size && [...left].every(([name, count]) => right.get(name) === count);
const show = values => `[${values.map(value => `'${value}'`).join(', ')}]`;

function compareProject(project, baseline, candidate) {
    const base = loadRun(baseline, project);
    const cand = loadRun(candidate, project);
    const problems = [];
    for (const identity of sorted(base.outcomes.keys())) {
        if (!cand.outcomes.has(identity)) problems.push(`${project}: case missing from candidate: ${identity}`);
    }
    for (const identity of sorted(cand.outcomes.keys())) {
        if (!base.outcomes.has(identity)) problems.push(`${project}: case added in candidate: ${identity}`);
    }
    for (const identity of sorted(base.outcomes.keys())) {
        const before = base.outcomes.get(identity);
        const after = cand.outcomes.get(identity);
        if (after && before.join() !== after.join()) {
            problems.push(`${project}: outcome changed for ${identity}: ${before[0]} -> ${after[0]}`);
        }
    }
    if (!sameCounter(base.discovery, cand.discovery)) {
        const removed = subtract(base.discovery, cand.discovery).slice(0, 5);
        const added = subtract(cand.discovery, base.discovery).slice(0, 5);
        problems.push(`${project}: discovery differs: removed=${show(removed)} added=${show(added)}`);
    }
    if (problems.length) throw new ParityError(problems.join('\n'));
    return base.outcomes.size;
}

export function compare(baseline, candidate, scope) {
    const failures = [];
    const counts = new Map();
    for (const [projectPath] of projectsIn(scope)) {
        const name = projectName(projectPath);
        try {
            counts.set(name, compareProject(name, join(baseline, name), join(candidate, name)));
        } catch (error) {
            if (!(error instanceof ParityError)) throw error;
            failures.push(error.message);
        }
    }
    if (failures.length) throw new ParityError(failures.join('\n'));
    return counts;
}

function main(argv) {
    const { values } = parseArgs({
        args: argv,
        options: {
            'list-projects': { type: 'string' },
            'check-inventory': { type: 'string' },
            'check-run': { type: 'string' },
            discovery: { type: 'string' },
            project: { type: 'string', default: '?' },
            baseline: { type: 'string' },
            candidate: { type: 'string' },
            scope: { type: 'string', default: 'ordinary' },
            'self-test': { type: 'boolean', default: false }
        }
    });
    try {
        if (values['self-test']) {
            const test = join(dirname(fileURLToPath(import.meta.url)), 'compare-specifications.test.mjs');
            return spawnSync(process.execPath, ['--test', test], { stdio: 'inherit' }).status ?? 1;
        }
        if (values['list-projects'] !== undefined) {
            for (const [projectPath, assembly] of projectsIn(values['list-projects'])) console.log(`${projectPath}\t${assembly}`);
        } else if (values['check-inventory'] !== undefined) {
            checkInventory(values['check-inventory']);
        } else if (values['check-run'] !== undefined) {
            if (!values.discovery) throw new ParityError('--check-run requires --discovery');
            console.log(checkRun(values['check-run'], values.discovery, values.project));
        } else if (values.baseline && values.candidate) {
            for (const [name, count] of compare(values.baseline, values.candidate, values.scope)) console.log(`${name}: ${count} cases identical`);
        } else {
            console.error('error: no action given');
            return 2;
        }
    } catch (error) {
        if (!(error instanceof ParityError)) throw error;
        console.error(`::error::${error.message}`);
        return 1;
    }
    return 0;
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) process.exit(main(process.argv.slice(2)));
