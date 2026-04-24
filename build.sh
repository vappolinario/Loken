#!/usr/bin/env bash
set -euo pipefail

# Build and publish Loken for linux-x64
# Usage: ./build.sh

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/Loken.Cli"
OUTPUT_DIR="$SCRIPT_DIR/publish"

echo "=== Loken Build Script ==="
echo "Project: $PROJECT_DIR"
echo "Output:  $OUTPUT_DIR"
echo ""

# Ensure we're in the git repository context for the MSBuild target to embed the commit hash
if git rev-parse --git-dir > /dev/null 2>&1; then
    COMMIT_HASH=$(git rev-parse --short HEAD)
    echo "Git commit hash: $COMMIT_HASH"
else
    echo "Warning: Not in a git repository. Version will be '0.1-unknown'."
fi

echo ""
echo "Publishing..."

dotnet publish "$PROJECT_DIR" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -o "$OUTPUT_DIR"

echo ""
echo "=== Build complete ==="
echo "Binary: $OUTPUT_DIR/loken"
echo "Version: $(git rev-parse --short HEAD 2>/dev/null || echo '0.1-unknown')"
