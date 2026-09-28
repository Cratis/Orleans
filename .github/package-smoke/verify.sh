#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

repo="$(cd "$(dirname "$0")/../.." && pwd)"
work="$repo/.ai-work/orleans-package-smoke"
version="1.10.2-packagecheck.$(date +%s).$$"
mkdir -p "$work/packages" "$work/consumer"
cp "$repo/.github/package-smoke/Consumer.csproj" "$repo/.github/package-smoke/Directory.Build.props" "$work/consumer/"

for project in Orleans.Storage Orleans; do
    dotnet pack "$repo/Source/$project/$project.csproj" -c Release -p:IsPackaging=true -p:Version="$version" --output "$work/packages"
done

for tfm in net8.0 net9.0 net10.0; do
    echo "Checking packed Cratis.Orleans consumer for $tfm"
    dotnet restore "$work/consumer/Consumer.csproj" \
        --source "$work/packages" --source https://api.nuget.org/v3/index.json \
        -p:TargetFramework="$tfm" -p:CratisOrleansVersion="$version" --force
    python3 - "$work/consumer/obj/project.assets.json" "$tfm" <<'PY'
import json
import sys

with open(sys.argv[1], encoding='utf-8') as file:
    assets = json.load(file)

packages = {name: version for package in assets['libraries']
            for name, version in [package.split('/')]
            if name.startswith('Microsoft.Orleans.')}
assert 'Microsoft.Orleans.Server' in packages, f"{sys.argv[2]}: Server not restored"
assert 'Microsoft.Orleans.Reminders' in packages, f"{sys.argv[2]}: Reminders not restored"
assert all(version == packages['Microsoft.Orleans.Server'] for version in packages.values()), \
    f"{sys.argv[2]}: mixed Orleans packages: {packages}"
print(f"{sys.argv[2]}: {len(packages)} Microsoft.Orleans packages at {packages['Microsoft.Orleans.Server']}")
PY
    dotnet build "$work/consumer/Consumer.csproj" --no-restore -c Release \
        -p:TargetFramework="$tfm" -p:CratisOrleansVersion="$version"
done
