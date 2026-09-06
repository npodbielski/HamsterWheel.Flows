#!/usr/bin/env bash
# Coverage gate + per-class gap table (coverage-plan S2/S3; extracted from
# .gitlab-ci.yml by ci-cleanup-plan S1).
#
#   usage: bash ci/coverage-gate.sh [results-dir]     # default: ./test-results
#
# Exits 0 when the cobertura line-rate is at or above GATE, 1 below. The
# output strings are CI-grepped (the gap table is the coverage plan's working
# instrument), so keep them stable.
#
# Pure grep/sed/awk on purpose: the dotnet sdk image has no python3, and
# mawk (Ubuntu's default awk) has no multi-character RS — hence the sed
# pre-split on </class>.
set -euo pipefail

GATE=0.80
RESULTS_DIR="${1:-./test-results}"

XML=$(ls "$RESULTS_DIR"/coverage.cobertura.*.xml 2>/dev/null | head -1 || true)
if [ -z "$XML" ]; then
  echo "[coverage] no cobertura XML found in $RESULTS_DIR" >&2
  exit 1
fi

# Extract line-rate for the HamsterWheel.Flows package specifically (not the overall
# rate which may include other measured assemblies).
LC=$(grep -oP '<package name="[^"]*HamsterWheel\.Flows"[^>]*line-rate="\K[0-9.]+' "$XML" | head -1)
# Fallback to overall rate if package-specific not found
if [ -z "$LC" ]; then
  LC=$(grep -oP 'line-rate="\K[0-9.]+' "$XML" | head -1)
fi
awk -v r="$LC" -v gate="$GATE" 'BEGIN {
  if (r + 0 >= gate) { printf "[coverage] line-rate %s (gate: %s) ok\n", r, gate; exit 0 }
  printf "[coverage] line-rate %s (gate: %s) BELOW GATE\n", r, gate; exit 1
}'
echo "[coverage top gaps by missed lines]"
sed 's|</class>|</class>\n|g' "$XML" | awk '
  match($0, /<class name="[^"]*"/) {
    incls = 1; missed = 0; total = 0
    nm = $0; sub(/^[ \t]*<class name="/, "", nm); sub(/".*$/, "", nm); sub(/^.*\./, "", nm)
  }
  incls {
    missed += gsub(/<line [^>]*hits="0"/, "&")
    total  += gsub(/<line [^>]*hits=/, "&")
  }
  /<\/class>/ && incls {
    if (missed > 0) printf "%d\t%d\t%s\n", missed, total, nm
    incls = 0
  }' | sort -rn | head -15 | awk -F'\t' '{ printf "  %-30s missed %4d/%4d\n", $3, $1, $2 }'
