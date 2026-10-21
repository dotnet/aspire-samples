#!/usr/bin/env bash

set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
aspire_cli="${ASPIRE_CLI:-aspire}"

cd "$repo_root"

mapfile -t apphosts < <(git ls-files ':(glob)samples/**/apphost.go' | sort)

# Moving or renaming the AppHost must not turn this job into a silent pass.
if (( ${#apphosts[@]} == 0 )); then
    echo "::error::No Go AppHosts found under samples/"
    exit 1
fi

for apphost in "${apphosts[@]}"; do
    sample_dir="$(dirname "$apphost")"
    echo "::group::Validate Go AppHost $sample_dir"
    # errexit is ignored inside an if condition, so the commands are chained with && to stop at
    # the first failure. "go build" writes the AppHost binary into the sample directory unless
    # its output is discarded.
    if (
        cd "$sample_dir" &&
        "$aspire_cli" restore --apphost . --non-interactive --nologo &&
        go build -o /dev/null ./... &&
        go vet ./...
    ); then
        echo "::endgroup::"
    else
        exit_code=$?
        echo "::endgroup::"
        echo "::error file=$apphost::Go AppHost validation failed for $sample_dir"
        exit "$exit_code"
    fi
done

echo "Validated ${#apphosts[@]} Go AppHosts."
