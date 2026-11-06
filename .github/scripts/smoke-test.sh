#!/usr/bin/env bash

set -euo pipefail

if [ $# -ne 2 ]; then
    echo "Usage: $0 <package directory> <package version>" >&2
    exit 2
fi

export MSYS_NO_PATHCONV=1

package_directory=$(cd "$1" && { pwd -W 2> /dev/null || pwd; })
version=$2

for sample in SimpleDataRace SimpleDeadlock SimpleDataRaceTestsMtp SimpleDataRaceTestsVSTest; do
    dotnet build "src/Samples/$sample/$sample.csproj" -c Release
done

mkdir -p src/artifacts/smoke
cd src/artifacts/smoke

dotnet tool install SharpDetect --tool-path tool --source "$package_directory" --version "$version"

failures=0
# check <name> <expected exit code> <sharpdetect arguments...>
check() {
    local name=$1 expected=$2 actual=0
    shift 2
    echo "::group::$name (expected exit code $expected)"
    ./tool/sharpdetect "$@" || actual=$?
    echo "::endgroup::"
    if [ "$actual" -ne "$expected" ]; then
        echo "::error::Smoke test '$name' exited with $actual, expected $expected."
        failures=$((failures + 1))
    fi
}

dll() { printf '../../Samples/%s/bin/Release/net10.0/%s.dll' "$1" "$1"; }

check simple-data-race 5 run -t "$(dll SimpleDataRace)" -p FastTrack
check simple-deadlock 5 run -t "$(dll SimpleDeadlock)" -p Deadlock
check mtp-race-fact 5 run -t "$(dll SimpleDataRaceTestsMtp)" -p FastTrack --test --filter "/*/*/*/RaceFact"
check mtp-clean-fact 0 run -t "$(dll SimpleDataRaceTestsMtp)" -p FastTrack --test --filter "/*/*/*/CleanFact"
check vstest-race-fact 5 run -t "$(dll SimpleDataRaceTestsVSTest)" -p FastTrack --test --runner VSTest --filter "FullyQualifiedName~RaceFact"

if [ "$failures" -ne 0 ]; then
    echo "::error::$failures smoke test(s) failed."
    exit 1
fi
echo "All smoke tests passed."
