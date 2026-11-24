# Round 9 Remediation — Ready-to-Ship Agent Prompt Pack (8 GB Ultra-Low-RAM Edition)

**Plan Source of Truth:** [2026-11-24-round9-post-review-remediation-plan.md](file:///d:/thietkegame/docs/superpowers/plans/2026-11-24-round9-post-review-remediation-plan.md)

## Which Dispatch Mode Uses the Least RAM on This Machine (`7.32 GB` Usable RAM)?

1. **Mode A — Single-Agent Inline (`executing-plans`) via Prompt 0A *(Absolute Lowest RAM — Recommended on 8 GB RAM)*:**
   - **1 agent process total**, `0` subagent spawns, `0` git worktrees, `1` `Unity.exe` at a time, `VBCSCompiler.exe` killed after every Unity run. Saves `~1.5–2.5 GB` of Node/Python/MCP duplicate process memory.
2. **Mode B — Sequential Coordinator (`subagent-driven-development`, `Concurrency = 1`) via Prompt 0B:**
   - Coordinator dispatches **1 fresh worker at a time** in `Workspace: "inherit"` (never `branch`/`share`, never `git worktree`, never 2 subagents concurrently) using file-based task briefs.
3. **Mode C — Manual Task-by-Task Prompts (`Prompt 1` → `Prompt 5` + `Prompt 6` Final Audit):**
   - Paste one prompt per fresh agent session in strict order `1 → 2 → 3 → 4 → 5`, then run `Prompt 6` for independent final gate sign-off.

---

## Prompt 0A — Single-Agent Inline Execution (`executing-plans` — Lowest RAM Footprint)

```markdown
Execute the implementation plan at `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` end-to-end from Task 1 through Task 5 in this single session.

MANDATORY PRE-FLIGHT:
1. Read `d:\thietkegame\AGENTS.md`, `d:\thietkegame\docs\ProjectConfig.yaml`, `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\executing-plans\SKILL.md`, `C:\Users\habayakka\.gemini\config\skills\ponytail\SKILL.md`, and the plan file `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` first.
2. Before starting each task (Tasks 1–5), call `read_file` on EVERY skill path listed in that task's **Required Skills** block.

HARD 8 GB ULTRA-LOW-RAM RULES (NON-NEGOTIABLE):
- ZERO `git worktree` creation and ZERO subagent spawning — work directly in `D:\thietkegame` so Unity reuses `Library/`.
- NEVER run 2 `Unity.exe` processes concurrently. Always pass `-job-worker-count 1 -disable-assembly-updater` (never `-noUpm`).
- Immediately after EVERY `Unity.exe` invocation, reclaim Roslyn RAM via `rtk proxy taskkill /F /IM VBCSCompiler.exe`.
- Before Task 4 GPU captures and Win64 build, check `FreeVirtualMemory` via `rtk proxy pwsh -NoProfile -Command "Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory,FreeVirtualMemory"`. Stop and report if `FreeVirtualMemory < 2200000`.
- Run `NhaGiamGiengChuongBuilder.BuildAll` ONLY ONCE (in Task 3 Step 4). Capture the 15 D3D12 GPU frames in 3 micro-batches of 5 (`1..5`, `6..10`, `11..15`), and inspect PNGs via `read_file` in 3 separate turns of 5 images (never 15 at once).
- Query `codegraph_explore` (`projectPath: "D:/thietkegame"`, `maxFiles <= 6`) before reading C# files; NEVER read full multi-thousand-line files without `StartLine`/`EndLine` (`<= 180` lines).
- Run all shell commands via `rtk` / `rtk proxy`; avoid `$` variables inside `pwsh -Command`.
- Zero `.asmdef` edits, zero manual `.meta` edits, zero raw YAML edits to `Player.prefab` (use `PrefabUtility` in `EnsurePlayerPrefabVfxWiring`), zero edits to `Main_Level.unity` or `Sandbox_Player.unity`.
- Protect `AGENTS.habayakka.md` and the 5 scratch files on local disk at all costs (external `%TEMP%` backup first + zero-checkout `GIT_INDEX_FILE` / `git commit-tree` history scrub in Task 5). Do NOT `git push` or create a PR.
```

---

## Prompt 0B — Sequential Subagent Coordinator (`subagent-driven-development`, `Concurrency = 1`)

```markdown
Coordinate and execute `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` (Tasks 1–5) using `superpowers:subagent-driven-development` adapted for an 8 GB RAM Windows host.

MANDATORY SETUP:
1. Read `d:\thietkegame\AGENTS.md`, `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\subagent-driven-development\SKILL.md`, and `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md`.
2. Override `using-git-worktrees`: DO NOT create a git worktree (a fresh Unity worktree triggers a 5 GB `Library/` reimport). Execute directly in `D:\thietkegame` with `Workspace: "inherit"` on every subagent.
3. Enforce STRICT `Concurrency = 1`: NEVER have 2 subagents (implementer or reviewer) running at the same time, and NEVER allow subagents to spawn child subagents.
4. Maintain a compact progress ledger at `d:\thietkegame\.superpowers\sdd\round9-remediation-progress.md` (git-ignored; ensure `.superpowers/` is ignored or cleaned before Task 5 Step 2). Pass task briefs and diffs via files — never paste multi-KB session history into subagent prompts.
5. Enforce all 8 GB Low-RAM rules across every dispatched worker (`-job-worker-count 1 -disable-assembly-updater`, `rtk proxy taskkill /F /IM VBCSCompiler.exe` after every Unity run, `1× BuildAll` total, `3×5` GPU micro-batches, `3×5` chunked PNG reads, zero-checkout `git commit-tree` history scrub in Task 5, and `0` `git push`).
```

---

## Prompt 1 — Worker: Task 1 (Runtime Safety, Test Purity, Zero `CS0414` & Config Sync)

```markdown
Implement **ONLY Task 1** of `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` (`Lines 110–228`) in `D:\thietkegame`.

MANDATORY SKILL LOAD FIRST (call `read_file` before any edit/command):
- `d:\thietkegame\AGENTS.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\test-driven-development\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\create-2d-physics\create-2d-physics.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\ui\ui.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\ui-ugui\ui-ugui.md`
- `d:\thietkegame\.agents\skills\unity-cli\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-test-evidence-review\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-code-review\SKILL.md`
- `C:\Users\habayakka\.gemini\config\skills\test-value\SKILL.md`
- `C:\Users\habayakka\.gemini\config\skills\ponytail\SKILL.md`

SCOPE & LOW-RAM CONSTRAINTS:
- Read ONLY lines `1–109` and `110–228` of the plan; use `codegraph_explore` (`projectPath: "D:/thietkegame"`, `maxFiles: 5`) and bounded `StartLine`/`EndLine` (`<= 120` lines) on target files.
- Do NOT run `BuildAll()`. Do NOT spawn subagents. Do NOT commit yet.
- Run every shell command via `rtk` / `rtk proxy`. Immediately after both the RED and GREEN Unity EditMode runs, execute `rtk proxy taskkill /F /IM VBCSCompiler.exe`.
- Verify: `T09` and `T10` fail first (RED), then after applying all 5 fixes (`PlayerContext.EndRoll` restoring `_defaultExcludeLayers`, `PlayerFeedback` `CS0414` removal, `ScrollPickupPresentation.OnDestroy` freeing `_snapshot`, `VisualShaderAndNormalTests` removing `EnsureAll()`, `docs/ProjectConfig.yaml` version sync) + `rtk proxy cmd /c codegraph sync`, `Logs/r9-fix-t1-green.xml` has `failed="0"` and `Logs/r9-fix-t1-green.log` has `0` `warning CS` lines.
- Return concise receipt: modified files, RED/GREEN test counts, `CS` warning count.
```

---

## Prompt 2 — Worker: Task 2 (`ParallaxLayer2D` Camera-Frustum Edge Pinning)

```markdown
Implement **ONLY Task 2** of `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` (`Lines 229–351`) in `D:\thietkegame`.

MANDATORY SKILL LOAD FIRST (call `read_file` before any edit/command):
- `d:\thietkegame\AGENTS.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\test-driven-development\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\2d-pixel-perfect\2d-pixel-perfect.md`
- `d:\thietkegame\.agents\skills\unity-cli\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-regression-suite\SKILL.md`
- `C:\Users\habayakka\.gemini\config\skills\ponytail\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\caveman\skills\surgical-patch\SKILL.md`

SCOPE & LOW-RAM CONSTRAINTS:
- Read ONLY lines `229–351` of the plan; inspect `ParallaxLayer2D.cs` (`120` lines) and the tail of `Round9CombatPhysicsTests.cs` (`StartLine: 1650, EndLine: 1760`).
- Keep `_pinColumnsToCameraFrustumEdges = false` by default so `Main_Level.unity` is 100% unaffected. Never alter `FgColumn_*` `localScale` (`Vector3.one` always).
- Do NOT run `BuildAll()`. Do NOT spawn subagents. Do NOT commit yet.
- Run RED (`T11`), implement `ConfigureFrustumEdgePinning` + `ResetFrustumPinnedColumnsToRest`, run `rtk proxy cmd /c codegraph sync`, then run GREEN (`T11_ParallaxLayer2D|MainLevelIntegrityTests`). Reclaim RAM after each Unity call with `rtk proxy taskkill /F /IM VBCSCompiler.exe`.
- Return concise receipt: modified files, `T11` + `MainLevelIntegrityTests` XML pass/fail counts.
```

---

## Prompt 3 — Worker: Task 3 (`NhaGiamGiengChuongBuilder` Closure, Low-RAM Caps & `A22`)

```markdown
Implement **ONLY Task 3** of `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` (`Lines 352–570`) in `D:\thietkegame`.

MANDATORY SKILL LOAD FIRST (call `read_file` before any edit/command):
- `d:\thietkegame\AGENTS.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\test-driven-development\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\craft-game-floors\craft-game-floors.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\2d-pixel-perfect\2d-pixel-perfect.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\urp-postprocessing\urp-postprocessing.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\sprite-editor\sprite-editor.md`
- `d:\thietkegame\.agents\skills\unity-cli\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-level\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-polish\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\andrej-karpathy-skills\skills\karpathy-guidelines\SKILL.md`

SCOPE & LOW-RAM CONSTRAINTS:
- Read ONLY lines `352–570` of the plan, and read ONLY the exact line ranges of `NhaGiamGiengChuongBuilder.cs` and `MapWiringTests.cs` listed in Task 3's **Files** block (`<= 150` lines per read — NEVER read the whole 5,641-line file!).
- NEVER hand-edit `Player.prefab` YAML — apply `PlayerSpawnHead` EditMode hide (`SpriteRenderer.enabled = false`, `TrailRenderer.emitting = false`) and `PlayerRimLight` normal-map guard strictly inside `EnsurePlayerPrefabVfxWiring()` + `BuildRound5SceneAtmosphereAndLighting()`.
- Use the EXACT collision-verified coordinates in Task 3 Step 3 (`Art_A_Cage @ 14.8f`, `Art_N_Cage2 @ 146.5f`, `Art_N_Pillar1 @ 97.62f`, `Art_N_Pillar2 @ 131.5f`, `Art_O_Pillar1 @ 95.8f`, `Art_Q_Chained1 @ -58.5f`, `Art_Q_SkeletonPier` root `-12` + child `Art_Q_SkeletonPier_Cap`, `Art_R_Pillar1 @ -38.5f`, `Art_T_Pillar2 @ 109.75f`, `Art_T_Chained2 @ 114.0f`, `Art_S_AlchemyBibli1/2` chains at `45.967f` / `62.467f`, and 6-segment `1:1` `Foreground_Silhouettes` `{ -10.0f, -5.2f, -0.4f, 4.4f, 9.2f, 14.0f }` at `{ -38.0f, -12.85f, 12.85f, 38.0f }`).
- Include ALL Low-RAM checkpoints (`EditorUtility.UnloadUnusedAssetsImmediate(true); GC.Collect();` after the 9 `SaveRoomPrefab` calls, after `CloseScene(mainLevelScene, true)`, after `SaveAssets()`, and inside `CaptureGpuVerificationFramesCore`).
- Run `A22` RED -> apply edits -> `rtk proxy cmd /c codegraph sync` -> run the SINGLE `BuildAll()` -> run `DeadCells.NhaGiamGiengChuong.Tests.EditMode` GREEN (`52/52` PASS), executing `rtk proxy taskkill /F /IM VBCSCompiler.exe` after every Unity process. Do NOT commit yet.
```

---

## Prompt 4 — Worker: Task 4 (Monotonic 3-Suite XMLs, `3×5` D3D12 GPU Audit & Win64 Build)

```markdown
Execute **ONLY Task 4** of `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` (`Lines 571–663`) in `D:\thietkegame`.

MANDATORY SKILL LOAD FIRST (call `read_file` before any command):
- `d:\thietkegame\AGENTS.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\verification-before-completion\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\SKILL.md`
- `d:\thietkegame\.agents\skills\unity-cli\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\unity-pipeline\unity-pipeline.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\urp-postprocessing\urp-postprocessing.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-qa\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-smoke-check\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-test-evidence-review\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-gate-check\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\caveman\skills\verify-and-stop\SKILL.md`

SCOPE & LOW-RAM CONSTRAINTS:
- Make ZERO edits to `Assets/`, `Packages/`, or `ProjectSettings/` (any edit invalidates `BuildAll()` and monotonic timestamps). If any test or visual gate fails, STOP and report exact evidence without mutating files.
- Check `FreeVirtualMemory >= 2200000` KB before GPU captures and before `BuildStandaloneWin64`. Kill `VBCSCompiler.exe` after EVERY Unity run.
- Run the 3 XML suites sequentially (`Logs/r9-final-core-edit.xml`, `Logs/r9-final-nggc-edit.xml`, `Logs/r9-final-playmode.xml`).
- Run D3D12 GPU capture (omit `-nographics`) in **3 micro-batches of 5 shots** (`1..5`, `6..10`, `11..15`) using the exact `$`-free Python runner in Task 4 Step 2.
- Visually inspect all 15 PNGs (`gpu_r9_ng_01..15.png`) via `read_file` in **3 separate turns of 5 images** (`01..05`, then `06..10`, then `11..15` — NEVER load 15 PNGs in a single turn).
- Build `BuildStandaloneWin64` (`Logs/r9-final-win64.log`) and run the 19-gate monotonic timestamp + zero-warning Python proof. Report exact XML totals, GPU metrics (`URP_PreFlight`, `shaderErrors`, `maxClip`), per-shot visual verdict, and `MONOTONIC_PROOF_OK`.
```

---

## Prompt 5 — Worker: Task 5 (Zero-Checkout Git History Scrub & Clean PR Gate)

```markdown
Execute **ONLY Task 5** of `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md` (`Lines 664–780`) in `D:\thietkegame`.

MANDATORY SKILL LOAD FIRST (call `read_file` before any command):
- `d:\thietkegame\AGENTS.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\finishing-a-development-branch\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\verification-before-completion\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-release\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-release-checklist\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-asset-audit\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\caveman\skills\verify-and-stop\SKILL.md`

SAFETY & MONOTONIC CONSTRAINTS:
- Launch ZERO Unity processes (`0 MB` Unity RAM).
- NEVER delete `AGENTS.habayakka.md`, `# MASTER PROMPT — COMPLETE UNITY 2D.md`, `GEMINI.txt`, `tools/caveman-posttooluse.patch`, `tools/fortress/prepare_workspaces.py`, or `tools/fortress/test_prepare_workspaces.py` from local working disk! Back all 6 up to `%TEMP%\thietkegame_r9_preserved` in Step 1 BEFORE any Git command.
- If `.superpowers/` exists from Coordinator bookkeeping, ensure `.superpowers/` is either added to `.gitignore` or outside tracked files before staging.
- Use ONLY the zero-checkout `GIT_INDEX_FILE` + `git commit-tree` + `git read-tree HEAD` Python script in Task 5 Step 3 (NEVER run `git checkout` or `git cherry-pick`, which would rewrite `Assets/` on disk and break Task 4's monotonic `st_mtime` proof!).
- Run Step 4's 8-point combined verification (`git diff` tree identity vs `backup/pre-r9-post-commit`, `0` history leaks in `origin/main..HEAD`, `6/6` local files intact on disk, `100%` Conventional Commits, `.meta` sync, and `19/19` post-scrub monotonic gates).
- HARD STOP: Do NOT run `git push` or `gh pr create`.
```

---

## Prompt 6 — Independent Final Auditor (Read-Only 8-Axis + Low-RAM Gate Sign-Off)

```markdown
Perform an independent, strictly READ-ONLY final release audit of `D:\thietkegame` on branch `map-feat-designing` against `d:\thietkegame\docs\superpowers\plans\2026-11-24-round9-post-review-remediation-plan.md`.

MANDATORY SKILL LOAD FIRST:
- `d:\thietkegame\AGENTS.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\verification-before-completion\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-gate-check\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-test-evidence-review\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-release-checklist\SKILL.md`

READ-ONLY & LOW-RAM RULES:
- Modify ZERO files and launch ZERO `Unity.exe` processes (`0 MB` Editor RAM).
- Verify from on-disk artifacts + Git + bounded `read_file` calls:
  1. All 3 XML files (`Logs/r9-final-core-edit.xml`, `Logs/r9-final-nggc-edit.xml`, `Logs/r9-final-playmode.xml`) exist, have `failed="0"` and `skipped="0"`, and have `st_mtime` strictly newer than every file in `Assets/_Project`, `Packages`, and `ProjectSettings`.
  2. `Logs/r9-final-win64.log` reports `Win64 build succeeded` with `0` `warning CS` lines, and `Build/NhaGiamGiengChuong/DeadCells_NG.exe` exists.
  3. Visually inspect `Logs/r9-gpu-captures/gpu_r9_ng_01..15.png` in 3 separate turns of 5 PNGs (`01..05`, `06..10`, `11..15`) and verify `0` interior `FgColumn` bands, `0` detached `PlayerSpawnHead` at spawn, `0` mid-wall truncated `spawnerPillar_00` columns, continuous Room S alchemy ceiling chains, and clean Room A spawn vat separation.
  4. Verify `git status --short` is empty, `git diff origin/main...HEAD -- Assets/_Project/Scenes/Main_Level.unity Assets/_Project/Scenes/Sandbox_Player.unity` is empty, `git diff origin/main...HEAD -- "*.asmdef"` (outside tests if any — check `0` unexpected `.asmdef` edits per plan), `git log origin/main..HEAD -- "AGENTS.habayakka.md" "# MASTER PROMPT — COMPLETE UNITY 2D.md" "GEMINI.txt" "tools/"` is empty, `100%` of commits in `origin/main..HEAD` follow Conventional Commits, and `AGENTS.habayakka.md` remains intact on local disk.
- Output a concise `PASS / FAIL` verdict table across all 8 Audit Axes with file/line/timestamp evidence.
```
