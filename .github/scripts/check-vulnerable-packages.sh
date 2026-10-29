#!/usr/bin/env bash
# Fails when any project in the solution resolves a package with a known High or Critical
# advisory (direct or transitive). Moderate and Low advisories are reported as warnings only.
#
# Usage: check-vulnerable-packages.sh <solution> [report.json]
# Needs a restored solution, the .NET SDK and jq.
set -euo pipefail

solution="${1:?usage: check-vulnerable-packages.sh <solution> [report.json]}"
report="${2:-vulnerable-packages.json}"

# docker-compose.dcproj is not a PackageReference project, so the command always reports one
# problem for it and exits non-zero. The JSON is checked below instead of the exit code.
dotnet list "$solution" package --vulnerable --include-transitive --format json > "$report" || true

if ! jq -e '(.projects | type) == "array" and (.projects | length) > 0' "$report" > /dev/null; then
  echo "::error::The vulnerability scan produced no project results."
  cat "$report"
  exit 1
fi

unexpected=$(jq -r '.problems // [] | map(select(.project | endswith(".dcproj") | not)) | .[] | "\(.project): \(.text)"' "$report")
if [[ -n "$unexpected" ]]; then
  echo "::error::The vulnerability scan reported problems:"
  echo "$unexpected"
  exit 1
fi

# One line per (package, version, severity, advisory), with the projects that resolve it.
rows=$(jq -r '
  [ .projects[] as $p | $p.frameworks[]? | ((.topLevelPackages // []) + (.transitivePackages // []))[]
    | . as $pkg | .vulnerabilities[]
    | { id: $pkg.id, version: $pkg.resolvedVersion, severity: .severity, url: .advisoryurl,
        project: ($p.path | split("/") | last | sub("\\.csproj$"; "")) } ]
  | group_by([.id, .version, .severity, .url])
  | map(.[0] + { projects: (map(.project) | unique | join(", ")) })
  | sort_by(({"Critical":0,"High":1,"Moderate":2,"Low":3}[.severity] // 4), .id)
  | .[] | [.severity, .id, .version, .url, .projects] | @tsv' "$report")

if [[ -z "$rows" ]]; then
  echo "No vulnerable packages found."
  exit 0
fi

failed=0
summary="| Severity | Package | Version | Advisory | Projects |"$'\n'"|---|---|---|---|---|"
while IFS=$'\t' read -r severity id version url projects; do
  summary+=$'\n'"| $severity | $id | $version | $url | $projects |"
  case "$severity" in
    Critical|High)
      echo "::error title=Vulnerable package::$id $version ($severity) $url"
      failed=1
      ;;
    *)
      echo "::warning title=Vulnerable package::$id $version ($severity) $url"
      ;;
  esac
done <<< "$rows"

echo "$summary"
if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  {
    echo "### Vulnerable NuGet packages"
    echo
    echo "$summary"
  } >> "$GITHUB_STEP_SUMMARY"
fi

if [[ "$failed" -ne 0 ]]; then
  echo "High or Critical vulnerable packages found. Upgrade the package or the top-level package that pulls it in (dotnet nuget why <project> <package>)."
  exit 1
fi
