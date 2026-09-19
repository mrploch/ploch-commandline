#!/usr/bin/env bash
set -euo pipefail

# Create a temporary workspace
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
ORIG_DIR=$PWD

cd "$WORK_DIR"

# Mock dotnet command
mkdir -p bin
cat << 'MOCK' > bin/dotnet
#!/usr/bin/env bash
echo "MOCK DOTNET: $@"
MOCK
chmod +x bin/dotnet
export PATH="$WORK_DIR/bin:$PATH"

# Create mock packages
mkdir -p src/ProjectA/bin/Release src/ProjectA/bin/Debug
touch src/ProjectA/bin/Release/ProjectA.1.0.0.nupkg
touch src/ProjectA/bin/Release/ProjectA.1.0.0.snupkg
touch src/ProjectA/bin/Debug/ProjectA.1.0.0.nupkg

mkdir -p tests/ProjectATests/bin/Release
touch tests/ProjectATests/bin/Release/ProjectATests.1.0.0.nupkg

mkdir -p Tests/ProjectBTests/bin/Release
touch Tests/ProjectBTests/bin/Release/ProjectBTests.1.0.0.nupkg

mkdir -p samples/SampleApp/bin/Release
touch samples/SampleApp/bin/Release/SampleApp.1.0.0.nupkg

mkdir -p Samples/OtherSample/bin/Release
touch Samples/OtherSample/bin/Release/OtherSample.1.0.0.nupkg

# Run the script
export NUGET_PUSH_TOKEN="mock_token"
export SYMBOLS_REQUIRED="true"

echo "Testing --list functionality"
list_output=$(bash "$ORIG_DIR/.github/scripts/publish-nuget-packages.sh" --list)

list_count=$(echo "$list_output" | wc -l)
if [[ "$list_count" -ne 1 ]]; then
    echo "::error::--list output contains $list_count packages, expected 1"
    echo "$list_output"
    exit 1
fi

if ! echo "$list_output" | grep -q "src/ProjectA/bin/Release/ProjectA.1.0.0.nupkg"; then
    echo "::error::--list output missing ProjectA"
    echo "$list_output"
    exit 1
fi
echo "  --list functionality OK"

echo "Testing publish execution..."
bash "$ORIG_DIR/.github/scripts/publish-nuget-packages.sh" "https://mock.feed" > output.log

# Verify output
grep -q "Publishing ./src/ProjectA/bin/Release/ProjectA.1.0.0.nupkg" output.log || { echo "Failed to find ProjectA"; exit 1; }
grep -q "Publishing ./src/ProjectA/bin/Release/ProjectA.1.0.0.snupkg" output.log || { echo "Failed to find ProjectA symbols"; exit 1; }

# Should NOT contain tests or samples (case insensitive)
if grep -qi "test" output.log; then
    echo "Failed: tests were published"
    cat output.log
    exit 1
fi
if grep -qi "sample" output.log; then
    echo "Failed: samples were published"
    cat output.log
    exit 1
fi
if grep -qi "Debug" output.log; then
    echo "Failed: debug package was published"
    cat output.log
    exit 1
fi

echo "Test passed!"
