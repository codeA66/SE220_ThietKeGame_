#!/usr/bin/env python3
"""
Fast, zero-dependency Unity & Git repository guardrail verifier.
Runs both in GitHub Actions CI (full repository scan) and in .githooks/pre-commit (--staged).
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Dict, Iterable, List, Set, Tuple

FORBIDDEN_TRACKED_PREFIXES = (
    ".superpowers/",
    ".fortress-workspaces/",
    ".codegraph/",
    "graphify-out/",
    ".ags/",
    ".gemini/",
    ".antigravity/",
    ".cursor/",
    ".windsurf/",
    "Assets/_Recovery/",
    "Assets/_ThirdParty/",
)

CONFLICT_RE = re.compile(r"^(<{7} |={7}\r?$|>{7} )", re.MULTILINE)
GUID_RE = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)
MISSING_SCRIPT_RE = re.compile(r"^\s*m_Script:\s*\{fileID:\s*0\s*\}\s*$", re.MULTILINE)
ASMDEF_GUID_RE = re.compile(r"^GUID:([0-9a-f]{32})$", re.IGNORECASE)
RAW_DEBUG_RE = re.compile(r"\b(?:UnityEngine\.)?Debug\.Log(?:Warning|Error|Exception|Assertion|Format|WarningFormat|ErrorFormat)?\s*\(")
LEGACY_INPUT_RE = re.compile(
    r"\b(?:UnityEngine\.)?Input\.(?:GetKey|GetKeyDown|GetKeyUp|GetAxis|GetAxisRaw|GetButton|GetButtonDown|GetButtonUp|GetMouseButton|GetMouseButtonDown|GetMouseButtonUp|mousePosition)\b"
)
BLOCK_COMMENT_RE = re.compile(r"/\*.*?\*/", re.DOTALL)
LINE_COMMENT_RE = re.compile(r"//[^\n]*")
STRING_LITERAL_RE = re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"')

TEXT_CONFLICT_EXTS = {
    ".cs", ".shader", ".hlsl", ".cginc", ".asmdef", ".asmref",
    ".json", ".yaml", ".yml", ".md", ".xml", ".inputactions",
    ".sh", ".ps1", ".py", ".unity", ".prefab", ".asset",
}


def git_lines(repo_root: Path, *args: str) -> List[str]:
    proc = subprocess.run(
        ["git", "-C", str(repo_root), *args],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=True,
    )
    return [line for line in proc.stdout.splitlines() if line]


def strip_csharp_comments_and_strings(source: str) -> str:
    without_blocks = BLOCK_COMMENT_RE.sub("", source)
    without_strings = STRING_LITERAL_RE.sub('""', without_blocks)
    return LINE_COMMENT_RE.sub("", without_strings)


def is_runtime_csharp(rel_path: str) -> bool:
    if not rel_path.startswith("Assets/_Project/") or not rel_path.endswith(".cs"):
        return False
    parts = rel_path.split("/")
    if "Editor" in parts or "Tests" in parts:
        return False
    if parts[-1] == "GameDebug.cs":
        return False
    return True


def check_forbidden_tracked(paths: Iterable[str], errors: List[str]) -> None:
    for p in paths:
        for prefix in FORBIDDEN_TRACKED_PREFIXES:
            if p.startswith(prefix):
                errors.append(f"[AGENT-SCRATCH] Forbidden generated path tracked in Git: {p}")


def check_meta_pairing_full(tracked_assets: List[str], errors: List[str]) -> None:
    file_set: Set[str] = set(tracked_assets)
    dir_set: Set[str] = set()
    for p in tracked_assets:
        parent = os.path.dirname(p)
        while parent and parent != "Assets":
            dir_set.add(parent)
            parent = os.path.dirname(parent)

    for p in tracked_assets:
        if p.endswith(".meta"):
            target = p[:-5]
            if target not in file_set and target not in dir_set:
                errors.append(f"[ORPHAN-META] Orphan .meta file without matching tracked asset/folder: {p}")
        else:
            meta = f"{p}.meta"
            if meta not in file_set:
                errors.append(f"[MISSING-META] Tracked asset missing .meta file: {p}")

    for d in sorted(dir_set):
        meta = f"{d}.meta"
        if meta not in file_set:
            errors.append(f"[MISSING-DIR-META] Tracked folder under Assets/ missing .meta file: {d}")


def check_duplicate_guids(repo_root: Path, tracked_files: List[str], errors: List[str]) -> Dict[str, str]:
    guid_to_asset: Dict[str, str] = {}
    for p in tracked_files:
        if not p.endswith(".meta"):
            continue
        full = repo_root / p
        if not full.is_file():
            continue
        content = full.read_text(encoding="utf-8", errors="replace")
        m = GUID_RE.search(content)
        if not m:
            continue
        guid = m.group(1)
        asset_path = p[:-5]
        existing = guid_to_asset.get(guid)
        if existing and existing != asset_path:
            errors.append(f"[DUPLICATE-GUID] GUID {guid} shared by '{existing}' and '{asset_path}'")
        else:
            guid_to_asset[guid] = asset_path
    return guid_to_asset


def check_build_scenes_and_missing_scripts(repo_root: Path, tracked_files: List[str], errors: List[str]) -> None:
    tracked_set = set(tracked_files)
    build_settings = repo_root / "ProjectSettings" / "EditorBuildSettings.asset"
    enabled_scenes: List[str] = []
    if build_settings.is_file():
        lines = build_settings.read_text(encoding="utf-8", errors="replace").splitlines()
        cur_enabled = False
        for line in lines:
            s = line.strip()
            if s.startswith("- enabled:"):
                cur_enabled = s.split(":", 1)[1].strip() == "1"
            elif s.startswith("path:") and cur_enabled:
                scene_path = s.split(":", 1)[1].strip()
                if scene_path:
                    enabled_scenes.append(scene_path)
                    if scene_path not in tracked_set:
                        errors.append(f"[BUILD-SCENE] Enabled scene in EditorBuildSettings not tracked: {scene_path}")

    yaml_targets = [
        p for p in tracked_files
        if p.endswith(".prefab") or (p.endswith(".unity") and p in enabled_scenes)
    ]
    for p in yaml_targets:
        full = repo_root / p
        if not full.is_file():
            continue
        text = full.read_text(encoding="utf-8", errors="replace")
        if MISSING_SCRIPT_RE.search(text):
            errors.append(f"[MISSING-SCRIPT] Null m_Script reference detected in {p}")


def check_asmdef_dag(repo_root: Path, tracked_files: List[str], guid_to_asset: Dict[str, str], errors: List[str]) -> None:
    asmdef_paths = [p for p in tracked_files if p.startswith("Assets/") and p.endswith(".asmdef")]
    path_to_name: Dict[str, str] = {}
    name_to_refs: Dict[str, List[str]] = {}

    for p in asmdef_paths:
        full = repo_root / p
        if not full.is_file():
            continue
        try:
            data = json.loads(full.read_text(encoding="utf-8-sig"))
        except json.JSONDecodeError as exc:
            errors.append(f"[ASMDEF-JSON] Invalid JSON in {p}: {exc}")
            continue
        name = data.get("name")
        if not name:
            errors.append(f"[ASMDEF-NAME] Missing 'name' field in {p}")
            continue
        path_to_name[p] = name
        name_to_refs[name] = list(data.get("references") or [])

    resolved_graph: Dict[str, List[str]] = {}
    for name, raw_refs in name_to_refs.items():
        edges: List[str] = []
        for ref in raw_refs:
            m = ASMDEF_GUID_RE.match(ref.strip())
            if m:
                target_path = guid_to_asset.get(m.group(1).lower())
                if target_path and target_path in path_to_name:
                    edges.append(path_to_name[target_path])
            elif ref in name_to_refs:
                edges.append(ref)
        resolved_graph[name] = edges

    visiting: Set[str] = set()
    visited: Set[str] = set()

    def dfs(node: str, stack: List[str]) -> None:
        visiting.add(node)
        stack.append(node)
        for nxt in resolved_graph.get(node, ()):
            if nxt in visiting:
                cycle = " -> ".join(stack[stack.index(nxt):] + [nxt])
                errors.append(f"[ASMDEF-CYCLE] Circular assembly reference: {cycle}")
            elif nxt not in visited:
                dfs(nxt, stack)
        stack.pop()
        visiting.remove(node)
        visited.add(node)

    for asm_name in sorted(resolved_graph):
        if asm_name not in visited:
            dfs(asm_name, [])


def check_source_files(repo_root: Path, paths: Iterable[str], errors: List[str], check_yaml_conflicts: bool = False) -> None:
    for p in paths:
        ext = os.path.splitext(p)[1].lower()
        if ext not in TEXT_CONFLICT_EXTS:
            continue
        if not check_yaml_conflicts and ext in {".unity", ".prefab", ".asset"}:
            continue
        full = repo_root / p
        if not full.is_file():
            continue
        text = full.read_text(encoding="utf-8", errors="replace")
        if CONFLICT_RE.search(text):
            errors.append(f"[CONFLICT-MARKER] Unresolved merge conflict marker in {p}")

        if is_runtime_csharp(p):
            stripped = strip_csharp_comments_and_strings(text)
            if RAW_DEBUG_RE.search(stripped):
                errors.append(f"[RAW-DEBUG-LOG] Use DeadCells.Core.GameDebug instead of Debug.Log* in runtime code: {p}")
            if LEGACY_INPUT_RE.search(stripped):
                errors.append(f"[LEGACY-INPUT] Use Unity Input System instead of legacy UnityEngine.Input in: {p}")


def run_staged_check(repo_root: Path) -> int:
    errors: List[str] = []
    staged_raw = git_lines(repo_root, "diff", "--cached", "--name-status")
    if not staged_raw:
        return 0

    staged_Existing: List[str] = []
    for row in staged_raw:
        parts = row.split("\t")
        status = parts[0]
        path = parts[-1]
        if not status.startswith("D"):
            staged_Existing.append(path)

    check_forbidden_tracked(staged_Existing, errors)
    check_source_files(repo_root, staged_Existing, errors, check_yaml_conflicts=True)

    # Verify full Assets/ index meta pairing using git ls-files (reads index directly in <50ms!)
    tracked_assets = git_lines(repo_root, "ls-files", "Assets")
    check_meta_pairing_full(tracked_assets, errors)

    if any(p.endswith(".asmdef") for p in staged_Existing):
        all_tracked = git_lines(repo_root, "ls-files")
        guid_map = check_duplicate_guids(repo_root, all_tracked, errors)
        check_asmdef_dag(repo_root, all_tracked, guid_map, errors)

    if errors:
        print("Unity & Git pre-commit guardrails FAILED:", file=sys.stderr)
        for err in errors:
            print(f"  - {err}", file=sys.stderr)
        return 1
    return 0


def run_full_check(repo_root: Path) -> int:
    errors: List[str] = []
    tracked_all = git_lines(repo_root, "ls-files")
    tracked_assets = [p for p in tracked_all if p.startswith("Assets/")]

    check_forbidden_tracked(tracked_all, errors)
    check_meta_pairing_full(tracked_assets, errors)
    guid_map = check_duplicate_guids(repo_root, tracked_all, errors)
    check_build_scenes_and_missing_scripts(repo_root, tracked_all, errors)
    check_asmdef_dag(repo_root, tracked_all, guid_map, errors)
    check_source_files(repo_root, tracked_all, errors, check_yaml_conflicts=False)

    if errors:
        print(f"Guardrail check FAILED with {len(errors)} error(s):", file=sys.stderr)
        for err in errors:
            print(f"  - {err}", file=sys.stderr)
        return 1

    print(
        f"Guardrail check PASSED: {len(tracked_all)} tracked files, "
        f"{len(guid_map)} unique GUIDs, 0 orphan/missing .meta, 0 asmdef cycles, 0 missing scripts."
    )
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify Unity & Git repository guardrails.")
    parser.add_argument("--staged", action="store_true", help="Check staged index changes (fast pre-commit mode).")
    parser.add_argument("--repo", default=None, help="Repository root path (defaults to two levels up from script).")
    args = parser.parse_args()

    repo_root = Path(args.repo).resolve() if args.repo else Path(__file__).resolve().parents[2]
    return run_staged_check(repo_root) if args.staged else run_full_check(repo_root)


if __name__ == "__main__":
    sys.exit(main())
