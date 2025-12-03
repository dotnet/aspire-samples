#!/usr/bin/env bash
set -euo pipefail

# Check if dotnet CLI is installed
if ! command -v dotnet >/dev/null 2>&1; then
    echo "Error: dotnet CLI is not found. Please install the .NET SDK from https://dotnet.microsoft.com/download" >&2
    exit 1
fi

# Check if Build.proj exists before attempting to build
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ ! -f "$SCRIPT_DIR/Build.proj" ]; then
    echo "Error: Build.proj not found in $SCRIPT_DIR" >&2
    exit 1
fi

dotnet build "$SCRIPT_DIR/Build.proj" "$@"
