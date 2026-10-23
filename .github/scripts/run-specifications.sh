#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the built specification executables one at a time and fails closed. It never restores or builds.
# Usage: run-specifications.sh --self-test
#        run-specifications.sh CONFIGURATION [--scope ordinary|integration|all] [--project PATH] [--results DIR]
# Exit: 0 ran clean, 1 a project failed or its results are invalid, 2 could not run (preflight).
# The canonical project inventory lives in compare-specifications.py.

set -uo pipefail

root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd) || exit 2
if [[ ${1:-} == --self-test ]]; then
    exec node --test "$root/.github/scripts/run-specifications.test.mjs"
fi
if ! command -v python3 >/dev/null; then
    echo "::error::python3 is required to validate specification TRX results"
    exit 2
fi
inventory="$root/.github/scripts/compare-specifications.py"

configuration=${1:-Debug}
shift || true
scope=all
selected_project=
results=
while [[ $# -gt 0 ]]; do
    case $1 in
        --scope) scope=${2:-}; shift 2 || exit 2 ;;
        --project) selected_project=${2:-}; shift 2 || exit 2 ;;
        --results) results=${2:-}; shift 2 || exit 2 ;;
        *) echo "::error::Unknown argument: $1"; exit 2 ;;
    esac
done

python3 "$inventory" --check-inventory "$root" || exit 2
rows=$(python3 "$inventory" --list-projects "$scope") || exit 2
if [[ -n $selected_project ]]; then
    all_rows=$(python3 "$inventory" --list-projects all) || exit 2
    selected_project=${selected_project#./}
    if ! printf '%s\n' "$rows" | grep -Fq "$selected_project"$'\t'; then
        if printf '%s\n' "$all_rows" | grep -Fq "$selected_project"$'\t'; then
            echo "::error::Project is outside scope '$scope': $selected_project"
        else
            echo "::error::Unknown specification project: $selected_project"
        fi
        exit 2
    fi
    rows=$(printf '%s\n' "$rows" | grep -F "$selected_project"$'\t')
fi
if [[ -z $rows ]]; then
    echo "::error::No specification projects selected"
    exit 2
fi

suffix=
case ${OS:-$(uname -s)} in
    Windows_NT|MINGW*|MSYS*|CYGWIN*) suffix=.exe ;;
esac
paths=()
executables=()
while IFS=$'\t' read -r path assembly; do
    executable="$root/${path%/*}/bin/$configuration/net10.0/$assembly$suffix"
    if [[ ! -x $executable ]]; then
        echo "::error::Missing specification executable: $executable"
        exit 2
    fi
    paths+=("$path")
    executables+=("$executable")
done <<< "$rows"

if [[ -z $results ]]; then
    mkdir -p "$root/TestResults" || exit 2
    results=$(mktemp -d "$root/TestResults/run.XXXXXX") || exit 2
fi
mkdir -p "$results" || exit 2
echo "Running ${#paths[@]} specification projects sequentially; results: $results"

result=0
total=0
for index in "${!paths[@]}"; do
    path=${paths[$index]}
    name=$(basename "$path" .csproj)
    out="$results/$name"
    rm -rf "$out" && mkdir -p "$out" || exit 2
    status=0
    dotnet test "$root/$path" --configuration "$configuration" --no-build --no-restore --list-tests > "$out/discovery.txt" 2>&1 || status=$?
    if [[ $status -ne 0 ]]; then
        echo "::error::$name discovery exited with $status"
    else
        "${executables[$index]}" -noLogo -noColor -result-trx "$out/$name.trx" > "$out/$name.log" 2>&1 || status=$?
        echo "::group::$name (exit $status)"
        cat "$out/$name.log" || { [[ $status -ne 0 ]] || status=2; }
        echo "::endgroup::"
        if [[ $status -ne 0 ]]; then
            echo "::error::$name exited with $status"
        elif count=$(python3 "$inventory" --check-run "$out/$name.trx" --discovery "$out/discovery.txt" --project "$name"); then
            total=$((total + count))
            echo "$name: $count tests"
        else
            echo "::error::$name produced no valid TRX matching discovery"
            status=1
        fi
    fi
    if [[ $result -eq 0 && $status -ne 0 ]]; then
        result=$status
    fi
done
if [[ $result -eq 0 ]]; then
    echo "Validated ${#paths[@]} specification projects, $total tests"
fi
exit "$result"
