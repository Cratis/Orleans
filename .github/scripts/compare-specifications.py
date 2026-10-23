#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Canonical specification project inventory, TRX validation and exact before/after parity comparison.

Subcommands are selected by flags:
  --list-projects SCOPE                    print "<project path>\\t<assembly name>" rows (ordinary|integration|all)
  --check-inventory ROOT                   verify the inventory equals recursive discovery and solution membership
  --check-run TRX --discovery FILE         verify one executed run (positive total, consistent, matches discovery)
  --baseline DIR --candidate DIR --scope S compare identities and per-case outcomes per project
  --self-test                              run compare-specifications.test.py
"""

import argparse
import re
import subprocess
import sys
from collections import Counter
from pathlib import Path
import xml.etree.ElementTree as ET

# The one canonical inventory: (project path, executable AssemblyName, scope).
PROJECTS = [
    ("Source/Orleans.Specs/Orleans.Specs.csproj", "Cratis.Orleans.Specs", "ordinary"),
    ("Source/Orleans.Storage.Specs/Orleans.Storage.Specs.csproj", "Cratis.Orleans.Storage.Specs", "ordinary"),
    ("Source/Orleans.Storage.MongoDB.Specs/Orleans.Storage.MongoDB.Specs.csproj", "Cratis.Orleans.Storage.MongoDB.Specs", "ordinary"),
    ("Source/Orleans.Storage.Sql.Specs/Orleans.Storage.Sql.Specs.csproj", "Cratis.Orleans.Storage.Sql.Specs", "ordinary"),
    ("Source/Orleans.Chronicle.Specs/Orleans.Chronicle.Specs.csproj", "Cratis.Orleans.Chronicle.Specs", "ordinary"),
    ("Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj", "Cratis.Orleans.Tools.GrainCompatibility.Specs", "ordinary"),
    ("Integration/Jobs.Integration/Jobs.Integration.csproj", "Cratis.Orleans.Jobs.Integration", "integration"),
    ("Integration/Chronicle.Integration/Chronicle.Integration.csproj", "Cratis.Orleans.Chronicle.Integration", "integration"),
]
SCOPES = ("ordinary", "integration", "all")
EXCLUDED_DIRECTORIES = {"bin", "obj", "node_modules", ".git", ".ai-work", "TestResults"}


class ParityError(Exception):
    """A validation or parity failure; the message names the project and case."""


def projects_in(scope):
    if scope not in SCOPES:
        raise ParityError(f"Unknown scope '{scope}'")
    return [(path, assembly) for path, assembly, kind in PROJECTS if scope == "all" or kind == scope]


def project_name(path):
    return Path(path).name[: -len(".csproj")]


def discover_projects(root):
    found = set()
    for path in Path(root).rglob("*.csproj"):
        relative = path.relative_to(root)
        if EXCLUDED_DIRECTORIES.intersection(relative.parts):
            continue
        if path.name.endswith(".Specs.csproj") or path.name.endswith(".Integration.csproj"):
            found.add(relative.as_posix())
    return found


def check_inventory(root):
    expected = {path for path, _, _ in PROJECTS}
    found = discover_projects(root)
    if found != expected:
        raise ParityError(
            f"Specification project inventory mismatch: missing={sorted(expected - found)} unknown={sorted(found - expected)}")
    solution = Path(root) / "Orleans.slnx"
    members = set(re.findall(r'Path="([^"]+)"', solution.read_text(encoding="utf-8"))) if solution.exists() else set()
    if not expected <= members:
        raise ParityError(f"Projects missing from Orleans.slnx: {sorted(expected - members)}")


def parse_discovery(path):
    """Return the multiset of test names listed by `dotnet test --list-tests`."""
    try:
        lines = Path(path).read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError as error:
        raise ParityError(f"Cannot read discovery output {path}: {error}") from error
    names = Counter()
    listing = False
    for line in lines:
        if not listing:
            listing = line.strip() == "The following Tests are available:"
            continue
        if line[:1] in (" ", "\t") and line.strip():
            names[line.strip()] += 1
        elif line.strip():
            break
    if not names:
        raise ParityError(f"No discovered tests in {path}")
    return names


def local(tag):
    return tag.rsplit("}", 1)[-1]


def parse_trx(path, project="?"):
    """Return (total, {identity: [outcomes]}) after validating internal consistency."""
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as error:
        raise ParityError(f"{project}: invalid TRX {path}: {error}") from error
    if local(root.tag) != "TestRun":
        raise ParityError(f"{project}: {path} is not a TestRun")
    counters = root.findall("./{*}ResultSummary/{*}Counters")
    if len(counters) != 1:
        raise ParityError(f"{project}: expected one TRX result summary counter in {path}")
    try:
        total = int(counters[0].get("total", "0"))
    except ValueError as error:
        raise ParityError(f"{project}: malformed TRX total in {path}") from error
    if total <= 0:
        raise ParityError(f"{project}: expected a positive TRX test total in {path}")
    definitions = {}
    for unit in root.findall("./{*}TestDefinitions/{*}UnitTest"):
        method = unit.find("{*}TestMethod")
        class_name = method.get("className") if method is not None else None
        name = method.get("name") if method is not None else None
        if not unit.get("id") or not class_name or not name:
            raise ParityError(f"{project}: test definition without id/class/method in {path}: {unit.get('name')}")
        definitions[unit.get("id")] = f"{class_name}.{name}"
    results = root.findall("./{*}Results/{*}UnitTestResult")
    if len(results) != total:
        raise ParityError(f"{project}: TRX total {total} does not match {len(results)} results in {path}")
    outcomes = {}
    tally = Counter()
    for result in results:
        identity = definitions.get(result.get("testId"))
        if identity is None:
            raise ParityError(f"{project}: result '{result.get('testName')}' has no test definition in {path}")
        outcome = result.get("outcome")
        if not outcome:
            raise ParityError(f"{project}: result '{identity}' has no outcome in {path}")
        outcomes.setdefault(identity, []).append(outcome)
        tally[outcome] += 1
    for identity, values in outcomes.items():
        if len(values) > 1:
            raise ParityError(f"{project}: ambiguous duplicate case identity '{identity}' ({len(values)} results) in {path}")
    for attribute, outcome in (("passed", "Passed"), ("failed", "Failed")):
        declared = counters[0].get(attribute)
        if declared is not None and int(declared) != tally[outcome]:
            raise ParityError(f"{project}: counter {attribute}={declared} contradicts {tally[outcome]} {outcome} results in {path}")
    return total, outcomes


def check_run(trx, discovery, project="?"):
    total, _ = parse_trx(trx, project)
    discovered = sum(parse_discovery(discovery).values())
    if discovered != total:
        raise ParityError(f"{project}: discovery listed {discovered} tests but the TRX holds {total}")
    return total


def find_single_trx(directory, project):
    files = sorted(Path(directory).glob("*.trx"))
    if len(files) != 1:
        raise ParityError(f"{project}: expected exactly one TRX in {directory}, found {len(files)}")
    return files[0]


def load_run(directory, project):
    if not Path(directory).is_dir():
        raise ParityError(f"{project}: missing results directory {directory}")
    trx = find_single_trx(directory, project)
    total, outcomes = parse_trx(trx, project)
    discovery = parse_discovery(Path(directory) / "discovery.txt")
    if sum(discovery.values()) != total:
        raise ParityError(f"{project}: discovery listed {sum(discovery.values())} tests but the TRX holds {total}")
    return outcomes, discovery


def compare_project(project, baseline, candidate):
    base_outcomes, base_discovery = load_run(baseline, project)
    cand_outcomes, cand_discovery = load_run(candidate, project)
    problems = []
    for identity in sorted(set(base_outcomes) - set(cand_outcomes)):
        problems.append(f"{project}: case missing from candidate: {identity}")
    for identity in sorted(set(cand_outcomes) - set(base_outcomes)):
        problems.append(f"{project}: case added in candidate: {identity}")
    for identity in sorted(set(base_outcomes) & set(cand_outcomes)):
        if base_outcomes[identity] != cand_outcomes[identity]:
            problems.append(
                f"{project}: outcome changed for {identity}: {base_outcomes[identity][0]} -> {cand_outcomes[identity][0]}")
    if base_discovery != cand_discovery:
        removed = sorted((base_discovery - cand_discovery).elements())
        added = sorted((cand_discovery - base_discovery).elements())
        problems.append(f"{project}: discovery differs: removed={removed[:5]} added={added[:5]}")
    if problems:
        raise ParityError("\n".join(problems))
    return len(base_outcomes)


def compare(baseline, candidate, scope):
    check_failures = []
    counts = {}
    for path, _ in projects_in(scope):
        name = project_name(path)
        try:
            counts[name] = compare_project(name, Path(baseline) / name, Path(candidate) / name)
        except ParityError as error:
            check_failures.append(str(error))
    if check_failures:
        raise ParityError("\n".join(check_failures))
    return counts


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--list-projects", metavar="SCOPE")
    parser.add_argument("--check-inventory", metavar="ROOT")
    parser.add_argument("--check-run", metavar="TRX")
    parser.add_argument("--discovery")
    parser.add_argument("--project", default="?")
    parser.add_argument("--baseline")
    parser.add_argument("--candidate")
    parser.add_argument("--scope", default="ordinary")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    try:
        if args.self_test:
            test = Path(__file__).with_name("compare-specifications.test.py")
            return subprocess.call([sys.executable, str(test)])
        if args.list_projects:
            for path, assembly in projects_in(args.list_projects):
                print(f"{path}\t{assembly}")
        elif args.check_inventory:
            check_inventory(args.check_inventory)
        elif args.check_run:
            if not args.discovery:
                raise ParityError("--check-run requires --discovery")
            print(check_run(args.check_run, args.discovery, args.project))
        elif args.baseline and args.candidate:
            for name, count in compare(args.baseline, args.candidate, args.scope).items():
                print(f"{name}: {count} cases identical")
        else:
            parser.error("no action given")
    except ParityError as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
