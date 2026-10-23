#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

import importlib.util
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
SPEC = importlib.util.spec_from_file_location("compare_specifications", HERE / "compare-specifications.py")
compare = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(compare)

NS = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
ORDINARY = [compare.project_name(path) for path, _ in compare.projects_in("ordinary")]


def trx(cases, run_id="run-1", total=None, passed=None, extra_result=None):
    """cases: list of (class, method, outcome[, display name])."""
    definitions, results = [], []
    for index, case in enumerate(cases):
        class_name, method, outcome = case[:3]
        display = case[3] if len(case) > 3 else f"{class_name}.{method}"
        test_id = f"id-{run_id}-{index}"
        definitions.append(
            f'<UnitTest name="{display}" id="{test_id}"><TestMethod className="{class_name}" name="{method}" /></UnitTest>')
        results.append(f'<UnitTestResult testId="{test_id}" testName="{display}" outcome="{outcome}" startTime="{run_id}" />')
    if extra_result:
        results.append(extra_result)
    real_passed = sum(1 for case in cases if case[2] == "Passed") if passed is None else passed
    real_total = len(cases) if total is None else total
    return (f'<TestRun id="{run_id}" xmlns="{NS}"><Results>{"".join(results)}</Results>'
            f'<TestDefinitions>{"".join(definitions)}</TestDefinitions>'
            f'<ResultSummary outcome="Completed"><Counters total="{real_total}" passed="{real_passed}" /></ResultSummary></TestRun>')


def discovery(cases):
    names = "\n".join(f"    {case[0]}.{case[1]}" for case in cases)
    return f"Test run for something\nThe following Tests are available:\n{names}\n"


class Fixture(unittest.TestCase):
    def setUp(self):
        (ROOT / ".ai-work").mkdir(exist_ok=True)
        self.dir = Path(tempfile.mkdtemp(prefix="compare-specifications-test-", dir=ROOT / ".ai-work"))
        self.addCleanup(shutil.rmtree, self.dir)

    def write_run(self, side, project, cases, trx_name="results.trx", discovery_cases=None, **options):
        directory = self.dir / side / project
        directory.mkdir(parents=True, exist_ok=True)
        for stale in directory.glob("*.trx"):
            stale.unlink()
        (directory / trx_name).write_text(trx(cases, run_id=side, **options), encoding="utf-8")
        (directory / "discovery.txt").write_text(discovery(cases if discovery_cases is None else discovery_cases), encoding="utf-8")

    def populate(self, cases, only=None):
        for name in ORDINARY:
            if only and name != only:
                continue
            self.write_run("baseline", name, cases)
            self.write_run("candidate", name, cases, trx_name=f"{name}.trx")

    def run_compare(self, scope="ordinary"):
        return compare.compare(self.dir / "baseline", self.dir / "candidate", scope)


BASE = [("A.when_x", "should_one", "Passed"), ("A.when_x", "should_two", "Failed"), ("B.when_y", "should_skip", "NotExecuted")]


class Comparison(Fixture):
    def test_identical_runs_pass_despite_run_metadata_and_file_names(self):
        self.populate(BASE)
        counts = self.run_compare()
        self.assertEqual(set(counts), set(ORDINARY))
        self.assertTrue(all(count == 3 for count in counts.values()))

    def test_replaced_case_with_same_count_is_rejected(self):
        self.populate(BASE)
        changed = BASE[:2] + [("B.when_y", "should_other", "NotExecuted")]
        self.write_run("candidate", "Orleans.Specs", changed, trx_name="x.trx")
        with self.assertRaisesRegex(compare.ParityError, "Orleans.Specs: case missing from candidate: B.when_y.should_skip") as raised:
            self.run_compare()
        self.assertIn("case added in candidate: B.when_y.should_other", str(raised.exception))

    def test_outcome_change_with_same_totals_is_rejected(self):
        self.populate(BASE)
        swapped = [("A.when_x", "should_one", "Failed"), ("A.when_x", "should_two", "Passed"), BASE[2]]
        self.write_run("candidate", "Orleans.Specs", swapped, trx_name="x.trx")
        with self.assertRaisesRegex(compare.ParityError, "outcome changed for A.when_x.should_one: Passed -> Failed"):
            self.run_compare()

    def test_lost_skipped_case_is_rejected(self):
        self.populate(BASE)
        self.write_run("candidate", "Orleans.Specs", BASE[:2], trx_name="x.trx")
        with self.assertRaisesRegex(compare.ParityError, "case missing from candidate: B.when_y.should_skip"):
            self.run_compare()

    def test_skipped_becoming_passed_is_rejected(self):
        self.populate(BASE)
        self.write_run("candidate", "Orleans.Specs", BASE[:2] + [("B.when_y", "should_skip", "Passed")], trx_name="x.trx")
        with self.assertRaisesRegex(compare.ParityError, "NotExecuted -> Passed"):
            self.run_compare()

    def test_duplicate_identity_is_ambiguous(self):
        self.populate(BASE)
        duplicated = BASE + [BASE[0]]
        self.write_run("candidate", "Orleans.Specs", duplicated, trx_name="x.trx")
        with self.assertRaisesRegex(compare.ParityError, "ambiguous duplicate case identity 'A.when_x.should_one'"):
            self.run_compare()

    def test_total_that_disagrees_with_results_is_rejected(self):
        self.populate(BASE)
        self.write_run("candidate", "Orleans.Specs", BASE, trx_name="x.trx", total=4)
        with self.assertRaisesRegex(compare.ParityError, "Orleans.Specs: TRX total 4 does not match 3 results"):
            self.run_compare()

    def test_counter_contradicting_results_is_rejected(self):
        self.populate(BASE)
        self.write_run("candidate", "Orleans.Specs", BASE, trx_name="x.trx", passed=3)
        with self.assertRaisesRegex(compare.ParityError, "counter passed=3 contradicts 1 Passed"):
            self.run_compare()

    def test_malformed_trx_is_rejected(self):
        self.populate(BASE)
        (self.dir / "candidate/Orleans.Specs/Orleans.Specs.trx").write_text("<TestRun", encoding="utf-8")
        with self.assertRaisesRegex(compare.ParityError, "Orleans.Specs: invalid TRX"):
            self.run_compare()

    def test_zero_total_is_rejected(self):
        self.populate(BASE)
        self.write_run("candidate", "Orleans.Specs", [], trx_name="x.trx")
        with self.assertRaisesRegex(compare.ParityError, "Orleans.Specs: expected a positive TRX test total"):
            self.run_compare()

    def test_missing_project_results_are_rejected(self):
        self.populate(BASE)
        shutil.rmtree(self.dir / "candidate/Orleans.Storage.Specs")
        with self.assertRaisesRegex(compare.ParityError, "Orleans.Storage.Specs: missing results directory"):
            self.run_compare()

    def test_empty_results_are_rejected(self):
        with self.assertRaisesRegex(compare.ParityError, "missing results directory"):
            self.run_compare()

    def test_extra_trx_is_ambiguous(self):
        self.populate(BASE)
        (self.dir / "candidate/Orleans.Specs/extra.trx").write_text(trx(BASE), encoding="utf-8")
        with self.assertRaisesRegex(compare.ParityError, "expected exactly one TRX"):
            self.run_compare()

    def test_discovery_must_match_execution_total(self):
        self.populate(BASE)
        self.write_run("candidate", "Orleans.Specs", BASE, trx_name="x.trx", discovery_cases=BASE[:2])
        with self.assertRaisesRegex(compare.ParityError, "discovery listed 2 tests but the TRX holds 3"):
            self.run_compare()

    def test_discovery_change_with_same_count_is_rejected(self):
        self.populate(BASE)
        other = BASE[:2] + [("B.when_y", "renamed", "NotExecuted")]
        self.write_run("candidate", "Orleans.Specs", BASE, trx_name="x.trx", discovery_cases=other)
        with self.assertRaisesRegex(compare.ParityError, "discovery differs"):
            self.run_compare()

    def test_missing_discovery_is_rejected(self):
        self.populate(BASE)
        (self.dir / "candidate/Orleans.Specs/discovery.txt").unlink()
        with self.assertRaisesRegex(compare.ParityError, "Cannot read discovery output"):
            self.run_compare()

    def test_all_failures_are_reported_together(self):
        self.populate(BASE)
        shutil.rmtree(self.dir / "candidate/Orleans.Storage.Specs")
        shutil.rmtree(self.dir / "candidate/Orleans.Chronicle.Specs")
        with self.assertRaises(compare.ParityError) as raised:
            self.run_compare()
        self.assertIn("Orleans.Storage.Specs", str(raised.exception))
        self.assertIn("Orleans.Chronicle.Specs", str(raised.exception))

    def test_cli_exit_codes(self):
        self.populate(BASE)
        script = str(HERE / "compare-specifications.py")
        ok = subprocess.run([sys.executable, script, "--baseline", str(self.dir / "baseline"), "--candidate", str(self.dir / "candidate")],
                            capture_output=True, text=True)
        self.assertEqual(ok.returncode, 0, ok.stderr)
        self.assertIn("Orleans.Specs: 3 cases identical", ok.stdout)
        shutil.rmtree(self.dir / "candidate/Orleans.Specs")
        bad = subprocess.run([sys.executable, script, "--baseline", str(self.dir / "baseline"), "--candidate", str(self.dir / "candidate")],
                             capture_output=True, text=True)
        self.assertEqual(bad.returncode, 1)
        self.assertNotIn("identical", bad.stdout)


class Run(Fixture):
    def test_check_run_accepts_consistent_run(self):
        self.write_run("candidate", "P", BASE)
        self.assertEqual(compare.check_run(self.dir / "candidate/P/results.trx", self.dir / "candidate/P/discovery.txt"), 3)

    def test_check_run_rejects_zero_discovery(self):
        self.write_run("candidate", "P", BASE, discovery_cases=[])
        with self.assertRaisesRegex(compare.ParityError, "No discovered tests"):
            compare.check_run(self.dir / "candidate/P/results.trx", self.dir / "candidate/P/discovery.txt")


class Inventory(Fixture):
    def make_tree(self, extra=(), omit=()):
        solution = []
        for path, _ in compare.projects_in("all"):
            if path in omit:
                continue
            (self.dir / path).parent.mkdir(parents=True, exist_ok=True)
            (self.dir / path).write_text("<Project />", encoding="utf-8")
            solution.append(f'<Project Path="{path}" />')
        for path in extra:
            (self.dir / path).parent.mkdir(parents=True, exist_ok=True)
            (self.dir / path).write_text("<Project />", encoding="utf-8")
        (self.dir / "Orleans.slnx").write_text("<Solution>" + "".join(solution) + "</Solution>", encoding="utf-8")

    def test_inventory_has_six_ordinary_and_two_integration(self):
        self.assertEqual(len(compare.projects_in("ordinary")), 6)
        self.assertEqual(len(compare.projects_in("integration")), 2)
        self.assertEqual(len(compare.projects_in("all")), 8)
        self.assertIn("Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj", [p for p, _ in compare.projects_in("ordinary")])
        self.assertFalse(any("Integration" in p for p, _ in compare.projects_in("ordinary")))

    def test_complete_tree_passes(self):
        self.make_tree()
        compare.check_inventory(self.dir)

    def test_unknown_discovered_project_is_rejected(self):
        self.make_tree(extra=["Source/New.Specs/New.Specs.csproj"])
        with self.assertRaisesRegex(compare.ParityError, "unknown=.*New.Specs"):
            compare.check_inventory(self.dir)

    def test_missing_project_is_rejected(self):
        self.make_tree(omit=["Source/Tools/GrainCompatibility.Specs/GrainCompatibility.Specs.csproj"])
        with self.assertRaisesRegex(compare.ParityError, "missing=.*GrainCompatibility.Specs"):
            compare.check_inventory(self.dir)

    def test_project_missing_from_solution_is_rejected(self):
        self.make_tree()
        text = (self.dir / "Orleans.slnx").read_text(encoding="utf-8").replace('<Project Path="Integration/Jobs.Integration/Jobs.Integration.csproj" />', "")
        (self.dir / "Orleans.slnx").write_text(text, encoding="utf-8")
        with self.assertRaisesRegex(compare.ParityError, "missing from Orleans.slnx"):
            compare.check_inventory(self.dir)

    def test_unknown_scope_is_rejected(self):
        with self.assertRaises(compare.ParityError):
            compare.projects_in("everything")

    def test_real_repository_inventory_matches(self):
        compare.check_inventory(ROOT)


if __name__ == "__main__":
    unittest.main(verbosity=1)
