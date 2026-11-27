#!/usr/bin/env sh
set -eu

REPO_ROOT="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"

echo "==> Configuring local Git repository settings in $REPO_ROOT..."
git -C "$REPO_ROOT" config --local core.hooksPath .githooks
git -C "$REPO_ROOT" config --local core.longpaths true
git -C "$REPO_ROOT" config --local core.autocrlf input
git -C "$REPO_ROOT" config --local rerere.enabled true
git -C "$REPO_ROOT" config --local fetch.prune true

chmod +x "$REPO_ROOT"/.githooks/* "$REPO_ROOT"/tools/ci/verify_guardrails.py 2>/dev/null || true

if command -v git-lfs >/dev/null 2>&1; then
    echo "==> Git LFS detected: $(git -C "$REPO_ROOT" lfs version)"
else
    echo "WARNING: git-lfs is not installed! Install git-lfs before pushing binary assets." >&2
fi

UNITY_VERSION="6000.6.9f1"
VERSION_FILE="$REPO_ROOT/ProjectSettings/ProjectVersion.txt"
if [ -f "$VERSION_FILE" ]; then
    PARSED="$( sed -n 's/^m_EditorVersion:[[:space:]]*//p' "$VERSION_FILE" | tr -d '\r' )"
    if [ -n "$PARSED" ]; then
        UNITY_VERSION="$PARSED"
    fi
fi

for CANDIDATE in \
    "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/Tools/UnityYAMLMerge" \
    "$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/Tools/UnityYAMLMerge" \
    "/opt/unity/Editor/Data/Tools/UnityYAMLMerge"
do
    if [ -x "$CANDIDATE" ]; then
        git -C "$REPO_ROOT" config --local merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
        git -C "$REPO_ROOT" config --local merge.unityyamlmerge.driver "'$CANDIDATE' merge -h -p --force --fallback none %O %B %A %A"
        git -C "$REPO_ROOT" config --local merge.unityyamlmerge.recursive binary
        echo "==> Configured UnityYAMLMerge: $CANDIDATE"
        break
    fi
done

echo "==> Git setup complete! Active hooks path: $(git -C "$REPO_ROOT" config --local --get core.hooksPath)"
