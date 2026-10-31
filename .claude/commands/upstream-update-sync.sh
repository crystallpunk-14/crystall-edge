#!/usr/bin/env bash
# Triage of upstream commits after the ledger cursor.
# Usage: triage.sh <cursor-sha> [upstream-ref]
# Output: TSV  bucket  sha  pr  existing/total  title
#   code    - touches C#/XAML/prototypes/audio that still exist in our tree: read the PR
#   engine  - RobustToolbox submodule bump: compare with our engine version
#   gone    - every touched code/prototype file is already cut from CE
#   assets  - only textures/maps/locale/changelog/docs
#   picked  - PR number already appears in our history since the last full sync
set -euo pipefail

cursor="$1"
ref="${2:-upstream/stable}"
sync_base="e6ad92ca50"   # last full upstream merge (#446); history after it holds our cherry-picks

ours=$(mktemp); picked=$(mktemp)
trap 'rm -f "$ours" "$picked"' EXIT

git ls-files 'Content.*' 'Resources/Prototypes' 'Resources/Audio' > "$ours"
git log --format=%s "$sync_base..HEAD" | grep -oE '#[0-9]{4,6}' | sort -u > "$picked" || true

git log --no-merges --name-only --format='@@%h%x09%s' "$cursor..$ref" | awk -F'\t' -v ours="$ours" -v picked="$picked" '
BEGIN {
    while ((getline f < ours) > 0) have[f] = 1
    while ((getline p < picked) > 0) done[p] = 1
}
function flush(   pr, bucket) {
    if (sha == "") return
    pr = "-"
    if (match(title, /\(#[0-9]+\)$/)) pr = substr(title, RSTART + 1, RLENGTH - 2)
    if (title ~ /[Aa]utomatic changelog/) bucket = ""
    else if (pr in done) bucket = "picked"
    else if (exist > 0) bucket = "code"
    else if (engine > 0) bucket = "engine"
    else if (relevant > 0) bucket = "gone"
    else bucket = "assets"
    if (bucket != "") printf "%s\t%s\t%s\t%d/%d\t%s\n", bucket, sha, pr, exist, relevant, title
}
/^@@/ {
    flush()
    sha = substr($1, 3); title = $2
    exist = 0; relevant = 0; engine = 0
    next
}
/^$/ { next }
{
    if ($0 == "RobustToolbox") { engine++; next }
    if ($0 ~ /\.(cs|xaml)$/ || $0 ~ /^Resources\/(Prototypes|Audio)\//) {
        relevant++
        if ($0 in have) exist++
    }
}
END { flush() }
' | sort -s -t$'\t' -k1,1
