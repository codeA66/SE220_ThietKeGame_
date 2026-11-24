# Round 9 Post-Review Remediation, Ultra-Low-RAM Execution & Clean PR Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended, strictly sequential `Concurrency = 1` in `Workspace: "inherit"`) or `superpowers:executing-plans` (single-session inline, lowest RAM footprint) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Every task binds mandatory **Superpowers**, **Unity Plugin (`AGENTS.md §9`)**, **CCGS Studio**, and **Discipline (`Ponytail / Karpathy / Caveman`)** skills with exact on-disk paths — each agent MUST call `read_file` on its assigned skill files BEFORE making any edit or running any verification command.

**Goal:** Close 100% of `P0` Blockers (`P0-1..P0-4`), `P1` Major Concerns (`P1-1..P1-3`), `P2` Tech Debt (`P2-1..P2-3`), and `NOT ASSESSED` stale-evidence gaps from the Round 9 Full-Project Audit under a strict **8 GB RAM survival budget**, achieving **`PASS` across all 8 Audit Axes and 15/15 D3D12 GPU shots** with a history-scrubbed Pull Request ready for `main`.

**Architecture:** Preserve all 15 `.asmdef` boundaries and keep `Main_Level.unity` / `Sandbox_Player.unity` untouched (`0` `.asmdef` edits, `0` manual `.meta` edits). Order work into **5 right-sized tasks** so `NhaGiamGiengChuongBuilder.BuildAll()` executes **exactly once** across the entire run (cutting 3 redundant full-project scene/prefab rebuilds): (1) pure runtime/test-purity/`CS0414` fixes in `Player` + `HUD` + `Core.Tests`; (2) opt-in camera-frustum edge pinning in `ParallaxLayer2D` (default `false` so `Main_Level` is untouched); (3) `NhaGiamGiengChuongBuilder` visual/gravity closure (`PlayerSpawnHead` EditMode hide via `PrefabUtility`, `PlayerRimLight` normal-map guard, 6-segment `1:1` frustum-pinned `Foreground_Silhouettes`, collision-verified full-height `SpawnerPillar` pilasters, Room S alchemy ceiling chains, Room A spawn vat clearance + fill light, and explicit `UnloadUnusedAssetsImmediate` + `GC.Collect` memory caps) + `MapWiringTests.A22`; (4) monotonic XML suites + `3×5` ultra-low-RAM D3D12 GPU captures + Win64 build; and (5) zero-conflict Git history scrub onto `origin/main` (`1521a95`) preserving all local persona/scratch files on disk.

**Tech Stack:** Unity `6000.6.9f1` (URP 2D `17.6.0`, 2D Lights, Tilemap `9.0.1`, 2D Animation `16.0.1`, InputSystem `1.20.1`), C#, NUnit / Unity Test Framework `1.8.0` (EditMode + PlayMode), `codegraph` MCP/CLI, `rtk` / `rtk proxy`, Python 3, Git (`origin/main` base `1521a95`).

**Spec:** [docs/superpowers/plans/2026-11-23-round9-visual-combat-level-e2e-pr-plan.md](file:///d:/thietkegame/docs/superpowers/plans/2026-11-23-round9-visual-combat-level-e2e-pr-plan.md) + Round 9 Independent 8-Axis Audit Report (2026-11-24).

---

## Global Constraints & Ultra-Low-RAM Survival Protocol (8 GB Host Machine)

- **Host Hardware Reality (Measured):** `7.32 GB` usable physical RAM (`~1.1–1.5 GB` free physical RAM at idle; `~5.3 GB` free OS virtual commit charge). Violating any RAM rule below risks `OutOfMemoryException`, D3D12 device loss, or OS swap thrashing.
- **RAM Rule 1 — ZERO Worktrees & Strict `Concurrency = 1`:** NEVER create a `git worktree` (`using-git-worktrees` override: work directly on `D:/thietkegame` in `Workspace: "inherit"` so Unity reuses `Library/` without a 5 GB reimport). NEVER spawn 2+ subagents in parallel (no parallel implementers, no parallel reviewers). At most **1 subagent** and **1 `Unity.exe`** process may exist at any instant.
- **RAM Rule 2 — Pre/Post Unity Daemon Reclaim (`VBCSCompiler` Kill + Commit-Charge Gate):** Before and immediately after **every** `Unity.exe` invocation, run the `$`-free Low-RAM Reclaim command to terminate idle Roslyn `VBCSCompiler.exe` (`300–600 MB` RAM) and verify `FreeVirtualMemory >= 2,200,000 KB` (`~2.1 GB`):
  ```powershell
  rtk proxy python -c "import subprocess, ctypes; subprocess.run(['taskkill','/F','/IM','VBCSCompiler.exe'], capture_output=True); k=ctypes.windll.kernel32; b=(ctypes.c_uint64*9)(); ctypes.memset(b,0,72); ctypes.c_uint32.from_address(ctypes.addressof(b)).value=64; print('VBCSCompiler reclaimed.')"
  ```
  And before D3D12 GPU capture or `BuildStandaloneWin64`, check OS memory via:
  ```powershell
  rtk proxy pwsh -NoProfile -Command "Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory,FreeVirtualMemory"
  ```
  If `FreeVirtualMemory < 2200000` (`< 2.1 GB`), STOP and report to Master before launching GPU capture or player build.
- **RAM Rule 3 — Single `BuildAll()` & `3×5` GPU Micro-Batches:** Execute `NhaGiamGiengChuongBuilder.BuildAll` **ONCE** in Task 3 Step 4 (after Tasks 1–3 C# edits are complete). Do NOT re-run `BuildAll` in Task 4 unless a test fails. Run D3D12 GPU captures in **3 micro-batches of 5 shots** (`R9_CAPTURE_START/END = 1..5`, `6..10`, `11..15`) instead of 8–15 shots at once, and inspect PNGs via `read_file` in **3 turns of 5 images** (never load all 15 `1280×720` PNGs in one turn).
- **RAM Rule 4 — Bounded File Reads & CodeGraph First (`AGENTS.md §6`):** Query `codegraph_explore` (`projectPath: "D:/thietkegame"`, `maxFiles <= 6`) before `rg` or `read_file`. NEVER read entire multi-thousand-line files (`NhaGiamGiengChuongBuilder.cs` = `5,641` lines; `MapWiringTests.cs` = `2,867` lines; `Round9CombatPhysicsTests.cs` = `1,873` lines; `Player.prefab` = `5,327` lines) — always pass `StartLine` and `EndLine` (`<= 180` lines per read). Run `rtk proxy cmd /c codegraph sync` after C# edits.
- **Unity CLI & Shell Discipline (`AGENTS.md §0, §9`):** Editor `6000.6.9f1` (`D:/Unity/6000.6.9f1/Editor/Unity.exe`). Every shell command MUST run via `rtk` / `rtk proxy`. Always pass `-job-worker-count 1 -disable-assembly-updater` to `Unity.exe` (NEVER pass `-noUpm` because URP, InputSystem, Tilemap Extras, and Test Framework are UPM packages). Avoid `$` PowerShell variables inside `rtk proxy pwsh -Command "..."` (outer runner strips `$`); use `rtk proxy git ...`, `rtk proxy rg ...`, or `rtk proxy python -c "..."`.
- **Assembly, Scene & Meta Immutability (`AGENTS.md §0, §2`):** Zero `.asmdef` edits; zero manual `.meta` edits; zero changes to `Assets/_Project/Scenes/Main_Level.unity` or `Assets/_Project/Scenes/Sandbox_Player.unity`. Do NOT hand-edit `Player.prefab` raw YAML — mutate `Player.prefab` strictly through `PrefabUtility.LoadPrefabContents` inside `EnsurePlayerPrefabVfxWiring()`.
- **Monotonic Evidence Law (`AGENTS.md §1`):** Every final XML (`Logs/r9-final-core-edit.xml`, `Logs/r9-final-nggc-edit.xml`, `Logs/r9-final-playmode.xml`), every GPU capture (`Logs/r9-gpu-captures/gpu_r9_ng_01..15.png`), and `Logs/r9-final-win64.log` MUST have `st_mtime` strictly **newer** than the newest file under `Assets/_Project/`, `Packages/`, and `ProjectSettings/`.
- **Local Persona File Protection (`P0-1` Safety):** `AGENTS.habayakka.md` and local prompt/scratch files MUST NEVER be deleted from Master's working disk — back them up to `%TEMP%\thietkegame_r9_preserved` BEFORE touching Git, add `.gitignore` exclusions, scrub them from branch commit history, and verify they remain intact on disk afterward.

---

## Mandatory Skill Matrix & Per-Task Agent Skill Protocol

> **HARD RULE FOR COORDINATOR & ALL SUBAGENTS:** Before starting any task, the assigned agent MUST call `read_file` on **every** skill listed in that task's **Required Skills** block and follow its workflow. Skipping skill loading is a protocol violation (`using-superpowers` + `AGENTS.md §9`).

### Verified On-Disk Skill Catalog

| Category | Skill Name | Exact On-Disk Path | Purpose in This Plan |
| --- | --- | --- | --- |
| **Superpowers (Process)** | `using-superpowers` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\using-superpowers\SKILL.md` | Enforces skill-first execution before any action |
| **Superpowers (Process)** | `subagent-driven-development` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\subagent-driven-development\SKILL.md` | Sequential coordinator loop (`Concurrency = 1`, `Workspace: "inherit"`) |
| **Superpowers (Process)** | `executing-plans` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\executing-plans\SKILL.md` | Single-session inline execution (lowest RAM option) |
| **Superpowers (Process)** | `test-driven-development` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\test-driven-development\SKILL.md` | Strict RED → GREEN → REFACTOR cycle per task |
| **Superpowers (Process)** | `systematic-debugging` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\systematic-debugging\SKILL.md` | Root-cause isolation on any compile, shader, or test failure |
| **Superpowers (Process)** | `verification-before-completion` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\verification-before-completion\SKILL.md` | Hard gate: fresh command/XML/PNG evidence before claiming PASS |
| **Superpowers (Process)** | `requesting-code-review` / `receiving-code-review` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\requesting-code-review\SKILL.md` / `...\receiving-code-review\SKILL.md` | Task review dispatch & technical verification of feedback |
| **Superpowers (Process)** | `finishing-a-development-branch` | `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\finishing-a-development-branch\SKILL.md` | Final branch verification and PR handoff |
| **Unity Plugin (`AGENTS.md §9`)** | `using-unity` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\SKILL.md` | Master index for Unity `6000.6` workflows |
| **Unity Plugin (`AGENTS.md §9`)** | `unity-cli` | `d:\thietkegame\.agents\skills\unity-cli\SKILL.md` | Batchmode Editor execution, `PrefabUtility` rule, exit codes |
| **Unity Plugin (`AGENTS.md §9`)** | `unity-pipeline` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\unity-pipeline\unity-pipeline.md` | Deterministic scene/prefab & standalone player build pipeline |
| **Unity Plugin (`AGENTS.md §9`)** | `2d-pixel-perfect` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\2d-pixel-perfect\2d-pixel-perfect.md` | 24-PPU `1:1` pixel snap & camera frustum-edge math |
| **Unity Plugin (`AGENTS.md §9`)** | `sprite-editor` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\sprite-editor\sprite-editor.md` | Sprite bounds, pivots, PPU, and chromakey/alpha hygiene |
| **Unity Plugin (`AGENTS.md §9`)** | `urp-postprocessing` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\urp-postprocessing\urp-postprocessing.md` | URP 2D `Light2D` sorting-layer masks, normal-map quality, HDR clip gates |
| **Unity Plugin (`AGENTS.md §9`)** | `create-2d-physics` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\create-2d-physics\create-2d-physics.md` | `Rigidbody2D.excludeLayers` bitmasks and layer collision invariants |
| **Unity Plugin (`AGENTS.md §9`)** | `craft-game-floors` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\craft-game-floors\craft-game-floors.md` | Grounded pilasters, ceiling anchors, and room framing |
| **Unity Plugin (`AGENTS.md §9`)** | `ui` & `ui-ugui` | `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\ui\ui.md` / `...\ui-ugui\ui-ugui.md` | uGUI `RawImage` blur `_snapshot` lifecycle in `ScrollPickupPresentation` |
| **CCGS Studio** | `ccgs-team-polish` / `ccgs-team-level` | `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-polish\SKILL.md` / `...\ccgs-team-level\SKILL.md` | Visual readability, lighting, sightlines, and architectural gravity |
| **CCGS Studio** | `ccgs-code-review` / `ccgs-asset-audit` | `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-code-review\SKILL.md` / `...\ccgs-asset-audit\SKILL.md` | Warning-free C#, zero texture leaks, `.meta` sync |
| **CCGS Studio** | `ccgs-team-qa` / `ccgs-smoke-check` / `ccgs-regression-suite` / `ccgs-test-evidence-review` / `ccgs-gate-check` | `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-qa\SKILL.md` (+ sibling dirs) | Assertion purity, cross-scene regression, monotonic XML/GPU/Win64 gates |
| **CCGS Studio** | `ccgs-team-release` / `ccgs-release-checklist` | `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-release\SKILL.md` / `...\ccgs-release-checklist\SKILL.md` | Clean Git history, Conventional Commits, and PR readiness |
| **Discipline** | `ponytail`, `karpathy-guidelines`, `surgical-patch`, `test-value`, `verify-and-stop` | `C:\Users\habayakka\.gemini\config\skills\ponytail\SKILL.md`, `...\karpathy-guidelines\SKILL.md`, `...\surgical-patch\SKILL.md`, `...\test-value\SKILL.md`, `...\verify-and-stop\SKILL.md` | Minimal surgical diffs, zero overbuilding, strict stop conditions |

### Task-to-Skill & Model Tier Binding

| Task | Findings Closed | Recommended Model Tier | Unity Runs | Mandatory Skills Agent MUST Read |
| --- | --- | --- | --- | --- |
| **Task 1: Runtime Resource Safety, Test Purity, Zero `CS0414` & Config Sync** | `P1-2`, `P1-3`, `P2-1`, `P2-2`, `P2-3` | Standard / Fast (exact code & tests) | `2` (`-nographics` filter) | `test-driven-development`, `using-unity`, `create-2d-physics`, `ui`, `ui-ugui`, `unity-cli`, `ccgs-test-evidence-review`, `ccgs-code-review`, `test-value`, `ponytail` |
| **Task 2: `ParallaxLayer2D` Camera-Frustum Edge Pinning (`Main_Level` Safe)** | `P0-2` (Core API) | Standard / Fast (single component) | `2` (`-nographics` filter) | `test-driven-development`, `using-unity`, `2d-pixel-perfect`, `unity-cli`, `ccgs-regression-suite`, `ponytail`, `surgical-patch` |
| **Task 3: Builder Visual/Gravity Closure, Low-RAM Caps & `A22` Scene Contract** | `P0-2` (Scene), `P0-3`, `P0-4`, `P1-1` | Capable / Pro (multi-room geometry + builder) | `3` (`RED` + `1× BuildAll` + `GREEN`) | `test-driven-development`, `using-unity`, `craft-game-floors`, `2d-pixel-perfect`, `urp-postprocessing`, `sprite-editor`, `unity-cli`, `ccgs-team-level`, `ccgs-team-polish`, `karpathy-guidelines` |
| **Task 4: Monotonic 3-Suite XML Gate, `3×5` Low-RAM D3D12 GPU Audit & Win64 Build** | `Trục 5` (`NOT ASSESSED`), `Trục 6`, `Trục 7` | Capable / Pro (vision audit across 15 PNGs) | `7` (`3 XML` + `3×5 GPU` + `Win64`) | `verification-before-completion`, `using-unity`, `unity-cli`, `unity-pipeline`, `urp-postprocessing`, `ccgs-team-qa`, `ccgs-smoke-check`, `ccgs-test-evidence-review`, `ccgs-gate-check`, `verify-and-stop` |
| **Task 5: Zero-Conflict Git History Scrub (`AGENTS.habayakka.md` Purge) & PR Gate** | `P0-1` (`Trục 8`) | Standard / Capable (Git graph surgery) | `0` (`0 MB` Unity RAM) | `finishing-a-development-branch`, `verification-before-completion`, `ccgs-team-release`, `ccgs-release-checklist`, `ccgs-asset-audit`, `verify-and-stop` |

---

## Review Focus (Top 5 Failure Modes Pinned to Owning Tasks)

1. **Zoomed-Out Verification Camera Slicing & Vertical Mixel Stretch (`orthoSize = 5.2 .. 11.2`):** Static `FgColumn` offsets cut inside zoomed-out shots (`Shot 02` `ortho=10.2` → `±18.13u`; `Shot 14` `ortho=11.2` → `±19.91u`), while stretching `FgColumn.localScale.y` creates vertical mixel artifacts. Task 2 pins `FgColumn_*` horizontally to `±(halfW + halfColW - 0.46f)` at `localScale = Vector3.one`, and Task 3 builds **6 `1:1` 24-PPU segments** (`{ -10.0f, -5.2f, -0.4f, 4.4f, 9.2f, 14.0f }`, camera-relative `[-14.02u..+15.02u]`) covering up to `ortho = 14.0u` with zero mixel distortion while keeping `_pinColumnsToCameraFrustumEdges = false` by default for `Main_Level.unity`. **Pinned in Task 2 (`T11`) & Task 3 (`A22`).**
2. **Full-Height Pilaster Collisions Against Existing Windows, Cages, Mushrooms, Torches & Anchors (`A14 / A15 / A16 / A20`):** Extending `SpawnerPillar` columns in place at their old truncated X coordinates would smash into `Art_N_Window2` (`@99.5`), `Art_N_Cage2` (`@131.5`), `GlowingMushroom_5` (`@98.0` in Room O), `Art_Q_Chained1` (`@-53.8`), and `Art_T_TorchMid_3` (`@108.0`). All 10 coordinates in Task 3 have been mathematically verified against every room AABB (`0` overlaps) and retain `Art_Q_SkeletonPier`'s direct root `SpriteRenderer` (`sortingOrder == -12` for `A18:2019`) with child `Art_Q_SkeletonPier_Cap` (`localScale == Vector3.one` for `A20:2523`). **Pinned in Task 3 (`A14..A22`).**
3. **Room A Spawn Cage & Ceiling Stamp Clash (`A10 / A14 / A22`):** Moving `Art_A_Cage` left to `x = 2.0f` hits `Art_A_BeamTL [0..1.5, 4..5.5]` and `Art_A_HomonculusPipes [2.6..6.0, 1.33..3.88]`. Task 3 places `Art_A_Cage` at **`x = 14.8f, ceilingY = 5.5f`** (`[14.24..15.36, 3.50..5.50]`, `min.y = 3.5f >= 3.0f`, `100%` clear of Bay #0 `[8..12]`, `Art_A_Torch @13.6`, `TP_0_Spawn [14.5..16.5, 0..2.5]`, and `Art_A_BeamTR [16.5..18.0]`) and adds `Art_A_HomonculusPipes` to `ScrubRoomVisualClutter` `ceilingObstacles` so no `A_CeilStamp_*` / `A_RoofMoss_*` ever touches the spawn pipes. **Pinned in Task 3 (`A22`).**
4. **8 GB Host OOM During `BuildAll()` or 15-Shot D3D12 Capture:** Accumulating 9 generated room prefabs + additive `Main_Level.unity` in `BuildAll()` or encoding 8–15 `1280×720` PNG byte arrays onto the Mono Large Object Heap risks OOM when host free commit charge is `~5.3 GB`. Task 3 adds explicit `EditorUtility.UnloadUnusedAssetsImmediate(true); GC.Collect();` checkpoints after room-prefab generation, after closing `Main_Level.unity`, and per GPU shot, and Task 4 splits GPU capture into `3×5` shots with `VBCSCompiler.exe` reclaim between processes. **Pinned in Task 3 & Task 4.**
5. **Accidental Deletion of `AGENTS.habayakka.md`, Cherry-Pick Conflict, or `Assets/` `st_mtime` Invalidation During History Scrub (`P0-1` + `Trục 5`):** Running `git checkout` + `git cherry-pick` rewrites working-tree files on disk, bumping `Assets/_Project` `st_mtime` *after* Task 4 and invalidating all 19 monotonic XML/GPU/build timestamps! Task 5 rewrites `88069c8..HEAD` purely inside `.git/objects` via a temporary `GIT_INDEX_FILE` + `git commit-tree` + `git read-tree HEAD` (**`0` working-tree disk writes, `0` `mtime` changes on `Assets/`, `0` risk to `AGENTS.habayakka.md` on disk**), then re-verifies both the 7-point Git gate and the 19-gate monotonic timestamp proof. **Pinned in Task 5.**

---

## File Structure & Ownership Map

- **Modify (Task 1):**
  - `Assets/_Project/Features/Player/Scripts/PlayerContext.cs` — capture `private readonly LayerMask _defaultExcludeLayers` from `body.excludeLayers` in constructor; OR with `_rollPassThroughMask` in `BeginRoll()`; restore `_defaultExcludeLayers` in `EndRoll()`.
  - `Assets/_Project/Features/Player/Scripts/PlayerFeedback.cs` — delete the 6 unused `[SerializeField]` floats (`_hitStopDuration`, `_hitShakeAmplitude`, `_hitShakeDuration`, `_hurtShakeAmplitude`, `_hurtShakeDuration`, `_flashDuration`) and their 2 `[Header]` lines (`0` `CS0414` warnings).
  - `Assets/_Project/Features/HUD/Scripts/Feedback/ScrollPickupPresentation.cs` — add `OnDestroy()` freeing `_snapshot` (`Destroy` in PlayMode / `DestroyImmediate` in EditMode) and clearing `_blur.texture`.
  - `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs` — remove `PrisonPropSpriteGenerator.EnsureAll();` mutation from `PrisonChromakeyProps_HaveZeroRawMagentaPixels()`.
  - `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs` — add `T09_EndRoll_RestoresInitialBodyExcludeLayersInsteadOfZeroing`, `T10_ScrollPickupPresentation_DestroyingGameObjectMidSequenceDestroysSnapshotTexture` (Task 1), and `T11_ParallaxLayer2D_FrustumEdgePinningKeepsInteriorClearAcrossOrthoSizes` (Task 2).
  - `docs/ProjectConfig.yaml` — sync package versions (`com.unity.inputsystem: "1.20.1"`, `com.unity.2d.tilemap.extras: "9.0.1"`, `com.unity.2d.animation: "16.0.1"`, `com.unity.render-pipelines.universal: "17.6.0"`).
- **Modify (Task 2):**
  - `Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs` — opt-in `_pinColumnsToCameraFrustumEdges` (`false` default), `ConfigureFrustumEdgePinning(...)`, and `ResetFrustumPinnedColumnsToRest()`.
- **Modify (Task 3):**
  - `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` — `EnsurePlayerPrefabVfxWiring` (`PlayerSpawnHead` EditMode hide), `PlayerRimLight` normal-map guard, 6-segment frustum-pinned `Foreground_Silhouettes`, collision-free Room A / N / O / Q / R / S / T props & chains, `Light2D(FindObjectsInactive.Include)` refresh, and `UnloadUnusedAssetsImmediate` + `GC.Collect` low-RAM checkpoints.
  - `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` — add `A22_Round9PostReview_SpawnCleanlinessFrustumSilhouettesAndFullGravity`.
  - Regenerated automatically by `BuildAll()` in Task 3 Step 4: `Assets/_Project/Features/Player/Prefabs/Player.prefab`, `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/*.prefab` (9 prefabs), and `Assets/_Project/Scenes/NhaGiamGiengChuong.unity`.
- **Modify (Task 5):**
  - `.gitignore` — ignore local persona/scratch files (`/AGENTS.habayakka.md`, `/GEMINI.txt`, `/*MASTER PROMPT*.md`, `/tools/caveman-posttooluse.patch`, `/tools/fortress/`).

---

### Task 1: Runtime Resource Safety, Test Purity, Zero `CS0414` Warnings & Config Doc Sync (`P1-2`, `P1-3`, `P2-1`, `P2-2`, `P2-3`)

**Required Skills (Agent MUST `read_file` before starting):**
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

**Files:**
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerContext.cs:55-90, 347-377`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerFeedback.cs:16-35`
- Modify: `Assets/_Project/Features/HUD/Scripts/Feedback/ScrollPickupPresentation.cs:119-130`
- Modify: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs:690-700`
- Modify: `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs:1660-1690` (append `T09`, `T10` above helper region)
- Modify: `docs/ProjectConfig.yaml:22-28`

**Interfaces:**
- Consumes: Existing 9-parameter `PlayerContext` constructor:
  `public PlayerContext(Rigidbody2D body, BoxCollider2D collider, SpriteRenderer sprite, LayerMask groundMask, LayerMask rollPassThroughMask, LayerMask climbableMask, PlayerMovementConfig config, PlayerInputReader input, PlayerAnimatorBridge animator)`
- Produces:
  - `PlayerContext.BeginRoll()` sets `_body.excludeLayers = _defaultExcludeLayers | _rollPassThroughMask;` and `EndRoll()` restores `_body.excludeLayers = _defaultExcludeLayers;` (`_defaultExcludeLayers = body.excludeLayers` captured in constructor).
  - `ScrollPickupPresentation.OnDestroy()` destroys `_snapshot` if non-null (`Destroy` when `Application.isPlaying`, `DestroyImmediate` otherwise) and nulls `_blur.texture`.
  - Pure read-only `PrisonChromakeyProps_HaveZeroRawMagentaPixels()` and `0` `CS0414` warnings across the entire repo.

- [ ] **Step 1: Inspect exact target spans via `codegraph_explore` (do NOT read full files) and add `T09` + `T10` to `Round9CombatPhysicsTests.cs`**

Add `T09` and `T10` to `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs` using the exact 9-argument `PlayerContext` constructor:
```csharp
[Test]
public void T09_EndRoll_RestoresInitialBodyExcludeLayersInsteadOfZeroing()
{
    var root = new GameObject("T09_Root");
    try
    {
        var harness = CreatePlayerHarness(root.transform, Vector2.zero);
        LayerMask customInitialMask = 1 << GroundLayer;
        harness.Body.excludeLayers = customInitialMask;

        var ctx = new PlayerContext(
            harness.Body,
            harness.PlayerGo.GetComponent<BoxCollider2D>(),
            harness.PlayerGo.GetComponentInChildren<SpriteRenderer>(),
            (LayerMask)((1 << GroundLayer) | (1 << OneWayLayer)),
            (LayerMask)(1 << EnemyLayer),
            ClimbProbeMask,
            AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath),
            harness.Input,
            harness.PlayerGo.GetComponent<PlayerAnimatorBridge>());

        ctx.BeginRoll();
        Assert.AreEqual(((1 << GroundLayer) | (1 << EnemyLayer)), harness.Body.excludeLayers.value);
        ctx.EndRoll();
        Assert.AreEqual(customInitialMask.value, harness.Body.excludeLayers.value,
            "EndRoll must restore the Rigidbody2D's initial excludeLayers rather than hardcoding 0.");
    }
    finally
    {
        Object.DestroyImmediate(root);
    }
}

[Test]
public void T10_ScrollPickupPresentation_DestroyingGameObjectMidSequenceDestroysSnapshotTexture()
{
    var go = new GameObject("T10_ScrollPresentation");
    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
    try
    {
        var pres = go.AddComponent<DeadCells.HUD.ScrollPickupPresentation>();
        SetPrivateField(pres, "_snapshot", tex);
        Object.DestroyImmediate(go);
        Assert.IsTrue(tex == null, "OnDestroy must destroy an active _snapshot Texture2D when the GameObject is torn down.");
    }
    finally
    {
        if (tex != null)
            Object.DestroyImmediate(tex);
        if (go != null)
            Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: Run targeted EditMode filter (`T09|T10`) to verify RED failure, then reclaim `VBCSCompiler`**

Run:
```powershell
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "T09_EndRoll_RestoresInitialBodyExcludeLayersInsteadOfZeroing|T10_ScrollPickupPresentation_DestroyingGameObjectMidSequenceDestroysSnapshotTexture" -testResults "Logs/r9-fix-t1-red.xml" -logFile "Logs/r9-fix-t1-red.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
```
Expected: Both `T09` and `T10` compile and FAIL (`failed="2"` in `Logs/r9-fix-t1-red.xml`). *(Note: `taskkill` exits non-zero if `VBCSCompiler.exe` already exited; ignore `not found`.)*

- [ ] **Step 3: Apply the 5 surgical fixes across `PlayerContext.cs`, `PlayerFeedback.cs`, `ScrollPickupPresentation.cs`, `VisualShaderAndNormalTests.cs`, and `docs/ProjectConfig.yaml`**

1. **`PlayerContext.cs` (`:55-90, 347-377`):** Add `private readonly LayerMask _defaultExcludeLayers;`; assign `_defaultExcludeLayers = body.excludeLayers;` in constructor; set `_body.excludeLayers = _defaultExcludeLayers | _rollPassThroughMask;` in `BeginRoll()`; set `_body.excludeLayers = _defaultExcludeLayers;` in `EndRoll()`.
2. **`PlayerFeedback.cs` (`:21-30`):** Delete `[Header("On Hit Dealt")]`, `_hitStopDuration`, `_hitShakeAmplitude`, `_hitShakeDuration`, `[Header("On Damage Taken")]`, `_hurtShakeAmplitude`, `_hurtShakeDuration`, and `_flashDuration`.
3. **`ScrollPickupPresentation.cs` (`:128`):** Add `private void OnDestroy()` clearing `_blur.texture = null` (if `_blur != null`) and destroying `_snapshot` (`if (Application.isPlaying) Destroy(_snapshot); else DestroyImmediate(_snapshot); _snapshot = null;`).
4. **`VisualShaderAndNormalTests.cs` (`:693`):** Delete `PrisonPropSpriteGenerator.EnsureAll();` from `PrisonChromakeyProps_HaveZeroRawMagentaPixels()`.
5. **`docs/ProjectConfig.yaml` (`:23-27`):** Sync summary versions to `Packages/manifest.json`: `com.unity.inputsystem: "1.20.1"`, `com.unity.2d.tilemap.extras: "9.0.1"`, `com.unity.2d.animation: "16.0.1"`, `com.unity.render-pipelines.universal: "17.6.0"`.

- [ ] **Step 4: Sync CodeGraph, run targeted EditMode verification (`T09|T10|PrisonChromakeyProps|Round8Phase2FeelMovementTests`), confirm `0` `CS0414` warnings, and reclaim `VBCSCompiler`**

Run:
```powershell
rtk proxy cmd /c codegraph sync
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "T09_EndRoll|T10_ScrollPickupPresentation|PrisonChromakeyProps_HaveZeroRawMagentaPixels|Round8Phase2FeelMovementTests" -testResults "Logs/r9-fix-t1-green.xml" -logFile "Logs/r9-fix-t1-green.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
rtk proxy python -c "import pathlib; log=pathlib.Path('Logs/r9-fix-t1-green.log').read_text(encoding='utf-8',errors='ignore'); warns=[l for l in log.splitlines() if 'warning CS' in l]; print('CS_WARNINGS:', len(warns)); assert not warns, warns"
```
Expected: `failed="0"` in `Logs/r9-fix-t1-green.xml` and `CS_WARNINGS: 0`.

---

### Task 2: `ParallaxLayer2D` Camera-Frustum Edge Pinning — Zero Interior Slicing & `Main_Level` Safe (`P0-2` Core API)

**Required Skills (Agent MUST `read_file` before starting):**
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\test-driven-development\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\2d-pixel-perfect\2d-pixel-perfect.md`
- `d:\thietkegame\.agents\skills\unity-cli\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-regression-suite\SKILL.md`
- `C:\Users\habayakka\.gemini\config\skills\ponytail\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\caveman\skills\surgical-patch\SKILL.md`

**Files:**
- Modify: `Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs:12-120`
- Test: `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs` (`T11_ParallaxLayer2D_FrustumEdgePinningKeepsInteriorClearAcrossOrthoSizes`)

**Interfaces:**
- Consumes: `UnityEngine.Camera` (`orthographic`, `orthographicSize`, `aspect`) on `_cameraTransform` or `Camera.main`.
- Produces on `DeadCells.CameraSystem.ParallaxLayer2D`:
  - Serialized fields (all preserving legacy behavior when `_pinColumnsToCameraFrustumEdges == false`):
    - `[SerializeField] private bool _pinColumnsToCameraFrustumEdges;` (default `false`)
    - `[SerializeField] private float _frustumEdgeVisibleInsetUnits = 0.46f;`
    - `[SerializeField] private float _frustumOuterGuardOffsetUnits = 26f;`
  - `public void ConfigureFrustumEdgePinning(bool enabled, float edgeVisibleInsetUnits = 0.46f, float outerGuardOffsetUnits = 26f)`
  - `public void ResetFrustumPinnedColumnsToRest()` (restores initial `localPosition` of `FgColumn_*` children without altering `localScale`, which stays `Vector3.one` at all times).

- [ ] **Step 1: Add `T11_ParallaxLayer2D_FrustumEdgePinningKeepsInteriorClearAcrossOrthoSizes` to `Round9CombatPhysicsTests.cs`**

```csharp
[Test]
public void T11_ParallaxLayer2D_FrustumEdgePinningKeepsInteriorClearAcrossOrthoSizes()
{
    var root = new GameObject("T11_Root");
    var tex = new Texture2D(63, 121, TextureFormat.RGBA32, false);
    var sp = Sprite.Create(tex, new Rect(0, 0, 63, 121), new Vector2(0.5f, 0.5f), 24f);
    try
    {
        var camGo = new GameObject("Cam");
        camGo.transform.SetParent(root.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.aspect = 16f / 9f;

        var fgGo = new GameObject("Foreground_Silhouettes");
        fgGo.transform.SetParent(root.transform, false);
        var layer = fgGo.AddComponent<DeadCells.CameraSystem.ParallaxLayer2D>();
        float[] restXs = { -38f, -12.85f, 12.85f, 38f };
        for (int i = 0; i < restXs.Length; i++)
        {
            var col = new GameObject($"FgColumn_{i}");
            col.transform.SetParent(fgGo.transform, false);
            col.transform.localPosition = new Vector3(restXs[i], 0f, 0f);
            var seg = new GameObject("Seg_0");
            seg.transform.SetParent(col.transform, false);
            seg.AddComponent<SpriteRenderer>().sprite = sp;
        }

        layer.Configure(cam.transform, new Vector2(0f, 1f), 0f, pixelSnap: true, pixelsPerUnit: 24f);
        layer.ConfigureVerticalCameraLock(true, 1.5f);
        layer.ConfigureFrustumEdgePinning(true, 0.46f, 26f);

        foreach (var (cx, ortho) in new[] { (18f, 5.8f), (47f, 10.2f), (-49.5f, 11.2f), (119f, 6.8f) })
        {
            cam.transform.position = new Vector3(cx, 3f, -10f);
            cam.orthographicSize = ortho;
            layer.UpdateParallax(cam.transform.position);

            float halfW = ortho * cam.aspect;
            float clearHalfW = halfW - 0.55f;
            foreach (Transform col in fgGo.transform)
            {
                Assert.AreEqual(Vector3.one, col.localScale, "FgColumn localScale must remain 1:1 (no vertical mixel stretch).");
                var b = col.GetComponentInChildren<SpriteRenderer>().bounds;
                bool intrudes = b.max.x > cx - clearHalfW && b.min.x < cx + clearHalfW;
                Assert.IsFalse(intrudes, $"{col.name} ({b.min.x:F2}..{b.max.x:F2}) intrudes [{cx - clearHalfW:F2}..{cx + clearHalfW:F2}] at ortho={ortho}.");
            }
        }

        layer.ResetFrustumPinnedColumnsToRest();
        for (int i = 0; i < restXs.Length; i++)
            Assert.AreEqual(restXs[i], fgGo.transform.GetChild(i).localPosition.x, 1e-3f);
    }
    finally
    {
        Object.DestroyImmediate(sp);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(root);
    }
}
```

- [ ] **Step 2: Run `T11` to verify RED failure, then reclaim `VBCSCompiler`**

Run:
```powershell
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "T11_ParallaxLayer2D_FrustumEdgePinningKeepsInteriorClearAcrossOrthoSizes" -testResults "Logs/r9-fix-t2-red.xml" -logFile "Logs/r9-fix-t2-red.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
```
Expected: Compile error (`ConfigureFrustumEdgePinning` / `ResetFrustumPinnedColumnsToRest` undefined).

- [ ] **Step 3: Implement `ConfigureFrustumEdgePinning` & `ResetFrustumPinnedColumnsToRest` in `ParallaxLayer2D.cs`**

In `Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs`:
1. Keep `_pinColumnsToCameraFrustumEdges = false` by default (`Main_Level.unity` executes the untouched legacy code path).
2. Cache initial `FgColumn_*` `localPosition` values lazily (`EnsureColumnRestStateCached()`) on first `ConfigureFrustumEdgePinning` / `UpdateParallax` / `ResetFrustumPinnedColumnsToRest` call.
3. When `_pinColumnsToCameraFrustumEdges` is `true` inside `UpdateParallax(Vector3 cameraPosition)`:
   - Resolve `Camera cam = _cameraTransform != null ? _cameraTransform.GetComponent<Camera>() : Camera.main;`
   - Compute `float aspect = (cam != null && cam.aspect > 0.01f) ? cam.aspect : (16f / 9f); float ortho = (cam != null && cam.orthographic && cam.orthographicSize > 0.01f) ? cam.orthographicSize : 6.75f; float halfW = ortho * aspect;`
   - Set `targetX = cameraPosition.x` (snapped to `_pixelsPerUnit` if `_pixelSnap`).
   - For the 4 `FgColumn_*` children ordered by rest `localPosition.x` (`[0]` outer-left, `[1]` inner-left, `[2]` inner-right, `[3]` outer-right): measure `halfColW` from the column's first `SpriteRenderer` (`sr.sprite.bounds.extents.x * Mathf.Abs(sr.transform.localScale.x)`, fallback `1.3125f`), set inner local X to `±(halfW + halfColW - _frustumEdgeVisibleInsetUnits)` (snapped to `1f / _pixelsPerUnit`), and set outer guard local X to `±Mathf.Max(38f, halfW + _frustumOuterGuardOffsetUnits)`. Never modify `localScale`.
4. `ResetFrustumPinnedColumnsToRest()` restores each `FgColumn_*` child's cached rest `localPosition`.

- [ ] **Step 4: Sync CodeGraph, run `T11` + `MainLevelIntegrityTests` to verify GREEN, and reclaim `VBCSCompiler`**

Run:
```powershell
rtk proxy cmd /c codegraph sync
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "T11_ParallaxLayer2D|MainLevelIntegrityTests" -testResults "Logs/r9-fix-t2-green.xml" -logFile "Logs/r9-fix-t2-green.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
```
Expected: `failed="0"` in `Logs/r9-fix-t2-green.xml` (`T11` and all `MainLevelIntegrityTests` PASS).

---

### Task 3: Builder Visual/Gravity Closure, Low-RAM Caps & `A22` Scene Contract (`P0-2` Scene, `P0-3`, `P0-4`, `P1-1`)

**Required Skills (Agent MUST `read_file` before starting):**
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

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (read ONLY targeted spans: `:215-241`, `:590-616`, `:1013-1042`, `:1192-1200`, `:1334-1378`, `:1476-1560`, `:2960-2968`, `:3427-3481`, `:4370-4410`, `:4801-4880`, `:5122-5141`, `:5225-5245`, `:5486-5631`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (read ONLY `:2660-2685` to append `A22`)
- Regenerated by `BuildAll()` in Step 4: `Player.prefab`, `Rooms/*.prefab` (9 prefabs), `NhaGiamGiengChuong.unity`

**Interfaces:**
- Consumes: `ParallaxLayer2D.ConfigureFrustumEdgePinning` & `ResetFrustumPinnedColumnsToRest` from Task 2; `AddFullHeightStonePilaster`, `PrefabUtility.LoadPrefabContents` in `EnsurePlayerPrefabVfxWiring`.
- Produces:
  - `Player.prefab` updated cleanly via `PrefabUtility` (`PlayerSpawnHead` `SpriteRenderer.enabled == false` and `TrailRenderer.emitting == false` at EditMode rest; `PlayerRimLight` `m_NormalMapQuality == 0` preserved across `BuildAll()`).
  - Collision-verified Room A, N, O, Q, R, S, T props with `0` truncated `spawnerPillar_00`, `0` unanchored alchemy shelves, `0` spawn vat/cage/ceiling-stamp overlaps, and `6`-segment `1:1` frustum-pinned `Foreground_Silhouettes`.
  - Low-RAM memory checkpoints inside `BuildAll()`, `CloneMainLevelScrollsAndGoldEconomy()`, and `CaptureGpuVerificationFramesCore()`.

- [ ] **Step 1: Add `A22_Round9PostReview_SpawnCleanlinessFrustumSilhouettesAndFullGravity` to `MapWiringTests.cs`**

Append `A22` right after `A21` in `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs`:
```csharp
[Test]
public void A22_Round9PostReview_SpawnCleanlinessFrustumSilhouettesAndFullGravity()
{
    EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

    // 1. PlayerSpawnHead hidden in EditMode + PlayerRimLight preserves m_NormalMapQuality == 0 after BuildAll()
    var player = Object.FindAnyObjectByType<PlayerController>();
    Assert.IsNotNull(player, "[A22/Player]");
    var spawnHead = player.GetComponentInChildren<PlayerSpawnHead>(true);
    Assert.IsNotNull(spawnHead, "[A22/PlayerSpawnHead]");
    var spawnHeadSr = spawnHead.GetComponent<SpriteRenderer>();
    Assert.IsNotNull(spawnHeadSr, "[A22/PlayerSpawnHeadSr]");
    Assert.IsFalse(spawnHeadSr.enabled, "[A22/SpawnHeadHiddenAtRest] PlayerSpawnHead SpriteRenderer must be disabled in EditMode.");
    var spawnTrail = spawnHead.GetComponentInChildren<TrailRenderer>(true);
    if (spawnTrail != null)
        Assert.IsFalse(spawnTrail.emitting, "[A22/SpawnTrailOffAtRest]");

    var rimLight = player.transform.Find("PlayerRimLight")?.GetComponent<Light2D>();
    Assert.IsNotNull(rimLight, "[A22/PlayerRimLight]");
    Assert.AreEqual(0, new SerializedObject(rimLight).FindProperty("m_NormalMapQuality").intValue,
        "[A22/PlayerRimLightNoNormalMap] BuildAll() must preserve m_NormalMapQuality=0 on PlayerRimLight.");

    // 2. Foreground_Silhouettes frustum-edge pinning keeps interior clear across all 15 Round 9 camera shots at 1:1 scale
    var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
    var fgLayer = GameObject.Find("Prison_Parallax_R5/Foreground_Silhouettes")?.GetComponent<ParallaxLayer2D>();
    Assert.IsNotNull(fgLayer, "[A22/FgParallax]");
    Assert.IsTrue(new SerializedObject(fgLayer).FindProperty("_pinColumnsToCameraFrustumEdges").boolValue, "[A22/FgFrustumPinEnabled]");

    (Vector3 pos, float ortho)[] shots =
    {
        (new Vector3(18f, 3.0f, -10f), 5.8f), (new Vector3(47.0f, 9.8f, -10f), 10.2f),
        (new Vector3(66f, 5.5f, -10f), 6.2f), (new Vector3(-8f, 19.5f, -10f), 6.0f),
        (new Vector3(8f, 29.5f, -10f), 6.5f), (new Vector3(122f, 11.5f, -10f), 7.0f),
        (new Vector3(198f, -10.5f, -10f), 6.8f), (new Vector3(-66f, 27.5f, -10f), 7.0f),
        (new Vector3(72f, 38.0f, -10f), 7.0f), (new Vector3(14.0f, 12.5f, -10f), 5.8f),
        (new Vector3(9.0f, 2.8f, -10f), 5.2f), (new Vector3(148.5f, 13.5f, -10f), 7.8f),
        (new Vector3(119.0f, 45.0f, -10f), 6.8f), (new Vector3(-49.5f, 26.8f, -10f), 11.2f),
        (new Vector3(58.5f, 31.0f, -10f), 8.8f),
    };
    float origOrtho = cam.orthographicSize;
    Vector3 origCamPos = cam.transform.position;
    Vector3 origFgPos = fgLayer.transform.position;
    try
    {
        cam.aspect = 16f / 9f;
        foreach (var (pos, ortho) in shots)
        {
            cam.transform.position = pos;
            cam.orthographicSize = ortho;
            fgLayer.UpdateParallax(pos);
            float halfW = ortho * cam.aspect;
            float clearZoneHalfW = halfW - 0.55f;
            foreach (Transform col in fgLayer.transform)
            {
                if (!col.name.StartsWith("FgColumn_", StringComparison.Ordinal))
                    continue;
                Assert.AreEqual(Vector3.one, col.localScale, $"[A22/FgNoMixelStretch/{col.name}]");
                Bounds cb = GetVisualSpriteBounds(col.gameObject);
                bool intrudes = cb.max.x > pos.x - clearZoneHalfW && cb.min.x < pos.x + clearZoneHalfW;
                Assert.IsFalse(intrudes,
                    $"[A22/FgIntrudesInterior] '{col.name}' ({cb.min.x:F2}..{cb.max.x:F2}) intrudes [{pos.x - clearZoneHalfW:F2}..{pos.x + clearZoneHalfW:F2}] at cam={pos}, ortho={ortho}.");
                if (Mathf.Abs(cb.center.x - pos.x) < halfW + 5f)
                {
                    Assert.LessOrEqual(cb.min.y, pos.y - ortho, $"[A22/FgCoversBottom/{col.name}] ortho={ortho}");
                    Assert.GreaterOrEqual(cb.max.y, pos.y + ortho, $"[A22/FgCoversTop/{col.name}] ortho={ortho}");
                }
            }
        }
    }
    finally
    {
        cam.ResetAspect();
        cam.orthographicSize = origOrtho;
        cam.transform.position = origCamPos;
        fgLayer.transform.position = origFgPos;
        fgLayer.ResetFrustumPinnedColumnsToRest();
    }

    // 3. Room A Spawn Vat + Pipes disjoint from Art_A_Cage & ceiling accents + lit by Art_A_VatFillLight
    var pipesGo = GameObject.Find("Art_A_HomonculusPipes");
    var fountainGo = GameObject.Find("Art_A_Fountain");
    var cageAGo = GameObject.Find("Art_A_Cage");
    var vatLight = GameObject.Find("Art_A_VatFillLight")?.GetComponent<Light2D>();
    Assert.IsNotNull(pipesGo, "[A22/Art_A_HomonculusPipes]");
    Assert.IsNotNull(cageAGo, "[A22/Art_A_Cage]");
    Assert.IsNotNull(vatLight, "[A22/Art_A_VatFillLight]");
    Assert.IsTrue(vatLight.enabled && vatLight.intensity >= 0.40f, "[A22/VatFillLightActive]");
    Bounds pipesBounds = GetVisualSpriteBounds(pipesGo);
    Bounds cageBounds = GetVisualSpriteBounds(cageAGo);
    Assert.IsFalse(OverlapsAabb(pipesBounds, cageBounds, -0.02f, -0.02f), "[A22/SpawnVatCageClear]");
    Assert.IsFalse(OverlapsAabb(GetVisualSpriteBounds(fountainGo), cageBounds, -0.02f, -0.02f), "[A22/SpawnFountainCageClear]");

    var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
    foreach (var sr in allRenderers.Where(r => r != null && (r.name.StartsWith("Art_A_CeilStamp", StringComparison.Ordinal) || r.name.StartsWith("Art_A_RoofMoss", StringComparison.Ordinal))))
        Assert.IsFalse(OverlapsAabb(sr.bounds, pipesBounds, -0.02f, -0.02f), $"[A22/SpawnPipesCeilAccentClear/{sr.name}]");

    // 4. Every SpawnerPillar column under Rooms/Room_*/Art spans floorY..ceilingY (zero mid-wall truncated pillars)
    (string propName, float floorY, float ceilingY)[] fullHeightSpawnerPillars =
    {
        ("Art_N_Pillar1", 8f, 22.5f),
        ("Art_N_Pillar2", 8f, 22.5f),
        ("Art_O_Pillar1", -16f, -1f),
        ("Art_Q_SkeletonPier", 16f, 23f),
        ("Art_R_Pillar1", 24f, 38.5f),
        ("Art_T_Pillar2", 40f, 62.5f),
    };
    foreach (var (propName, floorY, ceilingY) in fullHeightSpawnerPillars)
    {
        var go = GameObject.Find(propName);
        Assert.IsNotNull(go, $"[A22/PillarMissing/{propName}]");
        Bounds pb = GetVisualSpriteBounds(go);
        Assert.AreEqual(floorY, pb.min.y, 0.15f, $"[A22/PillarFloor/{propName}]");
        Assert.AreEqual(ceilingY, pb.max.y, 0.15f, $"[A22/PillarCeiling/{propName}] Truncated mid-wall pillar detected.");
    }

    // 5. Room S Art_S_AlchemyBibli1 & Art_S_AlchemyBibli2 ceiling chains reach y=39.0 clear of Bay #14 [47..55, 34..38]
    Bounds bay14 = new Bounds(new Vector3(51f, 36f, 0f), new Vector3(8f, 4f, 2f));
    foreach (string bibliName in new[] { "Art_S_AlchemyBibli1", "Art_S_AlchemyBibli2" })
    {
        string chainPrefix = bibliName + "_Chain_";
        var chainSegs = allRenderers.Where(sr => sr != null && sr.gameObject.name.StartsWith(chainPrefix, StringComparison.Ordinal)).ToArray();
        Assert.IsNotEmpty(chainSegs, $"[A22/BibliChainMissing/{bibliName}]");
        Assert.LessOrEqual(chainSegs.Min(s => s.bounds.min.y), 33.15f, $"[A22/BibliChainBottom/{bibliName}]");
        Assert.That(chainSegs.Max(s => s.bounds.max.y), Is.InRange(38.85f, 39.15f), $"[A22/BibliChainCeiling/{bibliName}]");
        foreach (var seg in chainSegs)
            Assert.IsFalse(OverlapsAabb(seg.bounds, bay14, -0.02f, -0.02f), $"[A22/BibliChainBay14Clash/{seg.name}]");
    }
}
```

- [ ] **Step 2: Run `A22` in EditMode to verify RED failure, then reclaim `VBCSCompiler`**

Run:
```powershell
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "A22_Round9PostReview_SpawnCleanlinessFrustumSilhouettesAndFullGravity" -testResults "Logs/r9-fix-t3-red.xml" -logFile "Logs/r9-fix-t3-red.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
```
Expected: FAIL on `[A22/SpawnHeadHiddenAtRest]` (`failed="1"`).

- [ ] **Step 3: Apply all collision-verified visual/gravity fixes & low-RAM checkpoints in `NhaGiamGiengChuongBuilder.cs`**

1. **`EnsurePlayerPrefabVfxWiring()` (`:3445-3476`) — `PrefabUtility` EditMode Hygiene (NO raw YAML hand-edit):**
   - On `spawnHead`: get `SpriteRenderer` (`headSr`) and `TrailRenderer` (`headTrail = spawnHead.GetComponentInChildren<TrailRenderer>(true)`); if `headSr != null && headSr.enabled` set `headSr.enabled = false; changed = true;`; if `headTrail != null && headTrail.emitting` set `headTrail.emitting = false; changed = true;`.
   - On `prefabRoot.transform.Find("PlayerRimLight")?.GetComponent<Light2D>()`: ensure `m_NormalMapQuality == 0` and `m_UseNormalMap == false` via `SerializedObject` if ever non-zero (`changed = true`).
2. **`BuildRound5SceneAtmosphereAndLighting()` (`:5122-5140` & `:5232-5243`):**
   - **`Foreground_Silhouettes` (`:5122-5140`):**
     - `fgParallax.Configure(camTransform, new Vector2(0f, 1.0f), 0f, pixelSnap: true, pixelsPerUnit: 24f);`
     - `fgParallax.ConfigureVerticalCameraLock(true, 1.5f);`
     - `fgParallax.ConfigureFrustumEdgePinning(true, 0.46f, 26f);`
     - Set rest `fgColXs = { -38.0f, -12.85f, 12.85f, 38.0f }` (`|relCenterX| >= 12.85f >= 12.0f` and total `[-39.31..+39.31]`, satisfying `A18` and `A20`).
     - Set **6 `1:1` vertical segments** `fgSegLocalYs = { -10.0f, -5.2f, -0.4f, 4.4f, 9.2f, 14.0f }` (spans `[-12.52..+16.52]` at rest → camera-relative `[-14.02..+15.02]`, covering up to `ortho = 14.0u` with `localScale == Vector3.one`).
   - **`PlayerRimLight` Guard (`:5235`):** Keep `SetLightSortingLayers(light, gameplayLayerIds)` for all `Point` lights, then wrap `SetLightNormalMapQuality(light, Light2D.NormalMapQuality.Fast, 3.2f)` inside `if (light.gameObject.name != "PlayerRimLight" && light.GetComponentInParent<PlayerController>() == null)`.
3. **Room A Spawn Separation & Fill Light (`BuildRoomAB`, `:594-615`) + `ScrubRoomVisualClutter` (`:4380-4385`):**
   - Move `Art_A_Cage` from `x = 4.8f` to **`x = 14.8f, ceilingY = 5.5f`** (`[14.24..15.36, 3.50..5.50]`, `min.y = 3.5f >= 3.0f` for `A10`, `0` overlap with Bay #0 `[8..12]`, `Art_A_Torch @13.6`, `TP_0_Spawn @15.5`, or `Art_A_BeamTR [16.5..18]`).
   - In `ScrubRoomVisualClutter` (`:4381`), add `|| n == "Art_A_HomonculusPipes"` to the `ceilingObstacles` check (`if (n == "LanternFixture_R5" || n.Contains("Cage") || n == "Art_A_HomonculusPipes")`) so `Art_A_CeilStamp_*` / `Art_A_RoofMoss_*` relocate cleanly away from the spawn pipes.
   - Add `Art_A_VatFillLight` under `artRoot` at `(4.3f, 2.8f, 0f)`: `Point Light2D`, `color = new Color(0.72f, 0.90f, 0.96f, 1f)`, `intensity = 0.52f`, `pointLightInnerRadius = 0.6f`, `pointLightOuterRadius = 3.4f`, `falloffIntensity = 0.65f`, `ConfigureGameplayPointLight(vatLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f)`.
4. **Collision-Verified Full-Height `SpawnerPillar` Upgrades (0 overlaps with windows, cages, mushrooms, lanterns, bays, or platforms):**
   - **Room N (`:1017, 1037-1038`):** Move `Art_N_Cage2` from `131.5f` to **`146.5f`** (`[145.94..147.06, 20.5..22.5]`, `2.5u` right of Bay #10 `[138..144]` and `7.5u` above `Art_N_Alcove2 [146..151, 8..13]`); replace `Art_N_Pillar1` with `AddFullHeightStonePilaster(artRoot, "Art_N_Pillar1", pal, mat, 97.62f, 8f, 22.5f);` (clear of `Art_N_OrnateCage1 <=96.40`, `OneWay_N_Mid1 <=96.0`, `Art_N_Window2 >=98.83`, `OneWay_N_WellCap >=101.0`); replace `Art_N_Pillar2` with `AddFullHeightStonePilaster(artRoot, "Art_N_Pillar2", pal, mat, 131.5f, 8f, 22.5f);` (clear of `Art_N_Junk2_B <=129.94` and `Art_N_Lantern_2 >=132.78`).
   - **Room O (`:1195`):** Replace `Art_O_Pillar1` with `AddFullHeightStonePilaster(artRoot, "Art_O_Pillar1", pal, mat, 95.8f, -16f, -1f);` (clear of `Art_O_Junk1 <=94.48`, `GlowingMushroom_5 >=97.25`, and Bay #6 `[100..106]`).
   - **Room Q (`:1337, 1373`):** Shift `Art_Q_Chained1` (`:1373`) from `-53.8f` to **`-58.5f`** (`new Vector2(-58.5f, 19.6f)`, clear of `Art_Q_Billot @-61.0` and `Art_Q_Torch_2 @-56.0`). Keep `"Art_Q_SkeletonPier"` root `GameObject` with its direct `SpriteRenderer` (`pal.SpawnerPillar`, `scale = 0.92f`, `Background`, `sortingOrder = -12`, grounded at `y = 16f` — required verbatim by `A18:2019-2028`), and add child `"Art_Q_SkeletonPier_Cap"` parented to `Art_Q_SkeletonPier` with `localScale = Vector3.one` (world scale `0.92f`, satisfying `A20:2523`), `pal.SpawnerPillar`, `mat`, `Background`, `sortingOrder = -12`, world position `new Vector3(-52.2f, 23f - pal.SpawnerPillar.bounds.max.y * 0.92f, 0f)`.
   - **Room R (`:1360`):** Replace `Art_R_Pillar1` with `AddFullHeightStonePilaster(artRoot, "Art_R_Pillar1", pal, mat, -38.5f, 24f, 38.5f);` (`100%` clear of `OneWay_R_Mid1 [-54..-42]`, `Art_R_Lantern_2 @-36.0`, and `Art_R_Torch_3 @-35.0`).
   - **Room T (`:1486, 1557`):** Replace `Art_T_Pillar2` with `AddFullHeightStonePilaster(artRoot, "Art_T_Pillar2", pal, mat, 109.75f, 40f, 62.5f);` (clear of `Art_T_TorchMid_3 <=108.42`, `Art_T_JunkEast2_A >=111.14`, and `Art_T_Lantern_3 >=111.58`), and shift `Art_T_Chained2` (`:1557`) from `109.5f` to **`114.0f`** (`new Vector2(114.0f, 55.5f)`, clear of `Art_T_Lantern_3 <=112.42` and `Art_T_Window3 >=115.33`).
5. **Room S Alchemy Library Ceiling Chains (`:1478-1479`):**
   - Add helper `AddAlchemyBibliCeilingChain(artRoot, "Art_S_AlchemyBibli1", pal, mat, 46.0f, 24f, 0.80f, 39f)` and `AddAlchemyBibliCeilingChain(artRoot, "Art_S_AlchemyBibli2", pal, mat, 62.5f, 24f, 0.80f, 39f)`:
     - Chain X = `bibliX - 0.0333f` (`45.967f` and `62.467f`, matching pixel column `185.0 / 372` of `alchemyBibli_00.png`, `0.63u` clear of Bay #14 `[47..55, 34..38]`).
     - Stack `pal.CageChain` (`localScale = new Vector3(0.80f, 0.80f, 1f)`, `Background`, `sortingOrder = -15`, tint `new Color(0.78f, 0.56f, 0.38f, 0.95f)`) named `$"{bibliName}_Chain_{idx}"` starting at bottom center `y = 32.95f + pal.CageChain.bounds.extents.y * 0.80f` (`min.y = 32.95f <= 33.033f` sprite top) stepping by `+0.72f` up to `topCenterY = 39.0f - pal.CageChain.bounds.max.y * 0.80f` (`max.y == 39.0f`).
6. **Low-RAM Checkpoints & GPU Capture Hygiene (`:227, 238, 2965, 5490, 5606-5630`):**
   - In `BuildAll()` right after the 9 `SaveRoomPrefab(...)` calls (`:227`) and after `AssetDatabase.SaveAssets()` (`:237`), call `EditorUtility.UnloadUnusedAssetsImmediate(true); GC.Collect();`.
   - In `CloneMainLevelScrollsAndGoldEconomy()` `finally` (`:2965`), immediately after `EditorSceneManager.CloseScene(mainLevelScene, true);`, call `EditorUtility.UnloadUnusedAssetsImmediate(true); GC.Collect();`.
   - In `CaptureGpuVerificationFramesCore()`:
     - Change `:5490` `FindObjectsByType<Light2D>()` to `FindObjectsByType<Light2D>(FindObjectsInactive.Include)`.
     - After `File.WriteAllBytes(...)` (`:5606`), set `pngBytes = null;` and in `ultraLowRam` mode call `GC.Collect();` on every shot (preventing `1280×720` PNG byte arrays from accumulating on the Mono Large Object Heap) plus `EditorUtility.UnloadUnusedAssetsImmediate(true);` every 3 shots.
     - In `finally` (`:5625`), call `parallaxLayers[i].ResetFrustumPinnedColumnsToRest()` alongside restoring `origParallaxPositions[i]`, and end `finally` with `EditorUtility.UnloadUnusedAssetsImmediate(true); GC.Collect();`.

- [ ] **Step 4: Sync CodeGraph, execute the SINGLE `BuildAll()` run, verify `NhaGiamGiengChuong.Tests.EditMode` (`A01..A22`), and reclaim `VBCSCompiler`**

Run:
```powershell
rtk proxy cmd /c codegraph sync
rtk proxy taskkill /F /IM VBCSCompiler.exe
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -logFile "Logs/r9-fix-t3-buildall.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.EditMode" -testResults "Logs/r9-fix-t3-green.xml" -logFile "Logs/r9-fix-t3-green.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
```
Expected: `BuildAll completed cleanly` (`Exit 0`), `failed="0"` across all `52/52` tests in `Logs/r9-fix-t3-green.xml` (including `A08`, `A10`, `A11`, `A12`, `A14`, `A15`, `A16`, `A18`, `A20`, `A21`, `A22`), and `0` `CS0414` warnings.

---

### Task 4: Monotonic 3-Suite XML Gate, `3×5` Ultra-Low-RAM D3D12 GPU Audit & Win64 Build (`Trục 5, 6, 7`)

**Required Skills (Agent MUST `read_file` before starting):**
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

**Files:**
- Zero `Assets/`, `Packages/`, or `ProjectSettings/` edits permitted in Task 4 (preserving Task 3 `BuildAll()` outputs untouched!).
- Produce git-ignored evidence under `Logs/` & `Build/`:
  - `Logs/r9-final-core-edit.xml`, `Logs/r9-final-nggc-edit.xml`, `Logs/r9-final-playmode.xml`
  - `Logs/r9-final-gpu-b1.log`, `Logs/r9-final-gpu-b2.log`, `Logs/r9-final-gpu-b3.log`
  - `Logs/r9-gpu-captures/gpu_r9_ng_01..15.png`
  - `Logs/r9-final-win64.log` & `Build/NhaGiamGiengChuong/DeadCells_NG.exe`

**Interfaces:**
- Consumes: Final on-disk assets/code from Tasks 1–3; `NhaGiamGiengChuongBuilder.CaptureRound9GpuVerificationFrames` (`R9_CAPTURE_START` / `R9_CAPTURE_END` env vars) and `BuildStandaloneWin64`.
- Produces: `19/19` strictly monotonic verification artifacts (`3` XMLs + `15` D3D12 PNGs + `1` Win64 build log) all newer than `max(Assets/_Project mtime)`.

- [ ] **Step 1: Verify CodeGraph index sync & OS commit charge (`>= 2.2 GB` free), then run all 3 XML test suites sequentially with `VBCSCompiler` reclaim**

Run:
```powershell
rtk proxy cmd /c codegraph status
rtk proxy pwsh -NoProfile -Command "Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory,FreeVirtualMemory"
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests" -testResults "Logs/r9-final-core-edit.xml" -logFile "Logs/r9-final-core-edit.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.EditMode" -testResults "Logs/r9-final-nggc-edit.xml" -logFile "Logs/r9-final-nggc-edit.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform PlayMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.PlayMode" -testResults "Logs/r9-final-playmode.xml" -logFile "Logs/r9-final-playmode.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
rtk proxy python -c "import xml.etree.ElementTree as ET; [print(p, ET.parse(p).getroot().attrib['total'], 'passed=', ET.parse(p).getroot().attrib['passed'], 'failed=', ET.parse(p).getroot().attrib['failed']) or (_ for _ in ()).throw(AssertionError(p)) if ET.parse(p).getroot().attrib['failed']!='0' else None for p in ['Logs/r9-final-core-edit.xml','Logs/r9-final-nggc-edit.xml','Logs/r9-final-playmode.xml']]"
```
Expected: `failed='0'` and `skipped='0'` in all 3 XML files (`70/70` Core EditMode, `52/52` NGGC EditMode, `10/10` NGGC PlayMode).

- [ ] **Step 2: Capture all 15 D3D12 GPU Verification Frames in `3×5` Ultra-Low-RAM Micro-Batches (`1..5`, `6..10`, `11..15` — omit `-nographics`!)**

Run (`$`-free Python env wrapper guarantees exact `R9_CAPTURE_START` / `R9_CAPTURE_END` injection and reclaims `VBCSCompiler` between batches):
```powershell
rtk proxy pwsh -NoProfile -Command "Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory,FreeVirtualMemory"
rtk proxy python -c "import os, subprocess
u = r'D:/Unity/6000.6.9f1/Editor/Unity.exe'
for idx, (s, e) in enumerate([(1,5),(6,10),(11,15)], 1):
    env = dict(os.environ, R9_CAPTURE_START=str(s), R9_CAPTURE_END=str(e))
    log = f'Logs/r9-final-gpu-b{idx}.log'
    cmd = [u,'-batchmode','-quit','-projectPath','D:/thietkegame','-job-worker-count','1','-disable-assembly-updater','-executeMethod','DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.CaptureRound9GpuVerificationFrames','-logFile',log]
    subprocess.run(cmd, env=env, check=True)
    subprocess.run(['taskkill','/F','/IM','VBCSCompiler.exe'], capture_output=True)
    print(f'BATCH_{idx}_OK:', s, '..', e, log)"
```
Expected: `BATCH_1_OK`, `BATCH_2_OK`, `BATCH_3_OK`; D3D12 hardware renderer, `URP_PreFlight 5/5 True`, `shaderErrors=0`, `defaultMaterialRenderers=0`, `maxClip <= 0.500%`, and `15/15` fresh PNGs in `Logs/r9-gpu-captures/`.

- [ ] **Step 3: Visually inspect all 15 PNGs via `read_file` in 3 Chunked Turns of 5 Images (`01..05`, then `06..10`, then `11..15` — NEVER 15 in one turn)**

Call `read_file` in 3 separate batches (`01..05`, `06..10`, `11..15`) to keep client/agent memory low, and verify:
- **Batch A (`01..05`):** `Shot 02` (`ortho=10.2`) and `Shot 03` (`Room G`) show `0` interior `FgColumn` vertical bands; `I2_Chain_G` and `I3_HealingChest` are 100% unobstructed; `FgColumn` appears only as a subtle `0.46u` outer left/right border frame.
- **Batch B (`06..10`):** `Shot 06` (`Room M/N`) and `Shot 09` (`Room S/T`) have `0` interior `FgColumn` bands and `0` mid-wall truncated `spawnerPillar_00` columns; `Shot 10` (`Stairs_H`) shows a clean single-row 45° ramp with no floating shadow box.
- **Batch C (`11..15`):**
  - `Shot 11` (`Spawn`): `0` detached white-outlined `PlayerSpawnHead` at Player's feet; `PlayerRimLight` cleanly rims Player torso; `Art_A_Fountain` + `Art_A_HomonculusPipes` lit by `Art_A_VatFillLight` with `0` overlap from `Art_A_Cage` (`@14.8`) or ceiling stamps.
  - `Shot 12, 13, 14`: `Art_N_Pillar1/2`, `Art_T_Pillar2`, `Art_R_Pillar1`, and `Art_Q_SkeletonPier` extend cleanly to their room ceilings with `0` window/cage/lantern/chained collisions; `I18_PromenadeExitTrigger` and `Art_R_BigSword` are 100% unobstructed.
  - `Shot 15` (`Room S`): Both `Art_S_AlchemyBibli1` and `Art_S_AlchemyBibli2` chains extend continuously into Room S ceiling (`y = 39.0`) clear of Bay #14.

- [ ] **Step 4: Build Win64 Standalone `.exe`, reclaim `VBCSCompiler`, and run the 19-Gate Monotonic Timestamp & Zero-Warning Proof**

Run:
```powershell
rtk proxy pwsh -NoProfile -Command "Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory,FreeVirtualMemory"
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildStandaloneWin64 -logFile "Logs/r9-final-win64.log"
rtk proxy taskkill /F /IM VBCSCompiler.exe
rtk proxy python -c "import pathlib
tracked = [p.stat().st_mtime for root in ('Assets/_Project','Packages','ProjectSettings') for p in pathlib.Path(root).rglob('*') if p.is_file()]
max_src = max(tracked)
gates = ['Logs/r9-final-core-edit.xml','Logs/r9-final-nggc-edit.xml','Logs/r9-final-playmode.xml','Logs/r9-final-win64.log'] + [f'Logs/r9-gpu-captures/{p.name}' for p in sorted(pathlib.Path('Logs/r9-gpu-captures').glob('gpu_r9_ng_*.png'))]
assert len(gates) == 19, f'Expected 19 gates, got {len(gates)}'
stale = [g for g in gates if pathlib.Path(g).stat().st_mtime <= max_src]
wlog = pathlib.Path('Logs/r9-final-win64.log').read_text(encoding='utf-8', errors='ignore')
cs_warns = [l for l in wlog.splitlines() if 'warning CS' in l]
assert 'Win64 build succeeded' in wlog, 'Win64 build did not succeed!'
assert not cs_warns, f'Compiler warnings found: {cs_warns}'
assert not stale, f'Stale gates detected: {stale}'
print('MONOTONIC_PROOF_OK: 19/19 gates newer than source; 0 CS warnings; DeadCells_NG.exe exists=', pathlib.Path('Build/NhaGiamGiengChuong/DeadCells_NG.exe').exists())"
```
Expected: `MONOTONIC_PROOF_OK: 19/19 gates newer than source; 0 CS warnings; DeadCells_NG.exe exists= True`.

---

### Task 5: Zero-Conflict Git History Scrub (`AGENTS.habayakka.md` + Scratch Purge), Conventional Commits & PR Gate (`P0-1`)

**Required Skills (Agent MUST `read_file` before starting):**
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\finishing-a-development-branch\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\verification-before-completion\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-team-release\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-release-checklist\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-asset-audit\SKILL.md`
- `C:\Users\habayakka\.gemini\config\plugins\caveman\skills\verify-and-stop\SKILL.md`

**Files:**
- Modify: `.gitignore`
- Preserve intact on local working disk (NEVER delete from disk): `AGENTS.habayakka.md`, `# MASTER PROMPT — COMPLETE UNITY 2D.md`, `GEMINI.txt`, `tools/caveman-posttooluse.patch`, `tools/fortress/prepare_workspaces.py`, `tools/fortress/test_prepare_workspaces.py`

**Interfaces:**
- Consumes: Verified working tree from Task 4 (`0` Unity processes running → `0 MB` Unity RAM).
- Produces: Rebased/rewritten linear branch `map-feat-designing` onto `origin/main` (`1521a95`) with `100%` Conventional Commits, `0` commits in `origin/main..HEAD` ever touching the 6 local persona/scratch files, byte-identical `Assets/`, `Packages/`, `ProjectSettings/`, and `docs/` tree, and all 6 local files intact on disk as ignored untracked files.

- [ ] **Step 1: Back up all 6 local persona/scratch files to `%TEMP%\thietkegame_r9_preserved` and create safety ref `backup/pre-r9-history-scrub`**

Run:
```powershell
rtk proxy python -c "import os, shutil, pathlib
bk = pathlib.Path(os.environ['TEMP']) / 'thietkegame_r9_preserved'
bk.mkdir(parents=True, exist_ok=True)
targets = ['AGENTS.habayakka.md','# MASTER PROMPT \u2014 COMPLETE UNITY 2D.md','GEMINI.txt','tools/caveman-posttooluse.patch','tools/fortress/prepare_workspaces.py','tools/fortress/test_prepare_workspaces.py']
for t in targets:
    p = pathlib.Path(t)
    assert p.exists(), f'Missing expected local file before backup: {t}'
    dst = bk / t
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(p, dst)
    print('BACKED_UP:', t, '->', dst)"
rtk proxy git branch -f backup/pre-r9-history-scrub HEAD
```
Expected: All `6/6` local files backed up to `%TEMP%\thietkegame_r9_preserved` and `backup/pre-r9-history-scrub` created.

- [ ] **Step 2: Append local ignore rules to `.gitignore` and commit Tasks 1–4 changes cleanly (do NOT `git rm --cached` here so cherry-pick stays 100% conflict-free!)**

Append to `.gitignore`:
```gitignore

# Local agent persona & scratch files (never commit to course repo)
/AGENTS.habayakka.md
/GEMINI.txt
/*MASTER PROMPT*.md
/tools/caveman-posttooluse.patch
/tools/fortress/
```
Stage project changes and commit:
```powershell
rtk proxy git add .gitignore docs/ProjectConfig.yaml docs/superpowers/plans/ Assets/_Project/ ProjectSettings/EditorBuildSettings.asset
rtk git status --short
rtk proxy git commit -m "fix(nggc,player): resolve Round 9 audit visual stop-gates, test purity, and build warnings"
rtk proxy git branch -f backup/pre-r9-post-commit HEAD
```
Expected: Clean commit created; `backup/pre-r9-post-commit` points to the exact pre-scrub tip.

- [ ] **Step 3: Scrub the 6 scratch paths & normalize `0a8f583`+`88069c8` via zero-checkout `GIT_INDEX_FILE` + `git commit-tree` (preserves `Assets/` `st_mtime` and local disk files 100% untouched!)**

Run (`0` working-tree checkouts, `< 20 MB` RAM, `< 2s` runtime):
```powershell
rtk proxy python -c "import os, subprocess, tempfile, pathlib
forbidden = ['AGENTS.habayakka.md','# MASTER PROMPT \u2014 COMPLETE UNITY 2D.md','GEMINI.txt','tools/caveman-posttooluse.patch','tools/fortress']
def git(*args, env=None, inp=None):
    return subprocess.check_output(['git', *args], env=env, input=inp).decode('utf-8').strip()
base_main = git('rev-parse', 'origin/main')
merge_base_commit = git('rev-parse', '88069c8')
linear_commits = [c for c in git('rev-list', '--reverse', f'{merge_base_commit}..HEAD').splitlines() if c]
idx_path = pathlib.Path(tempfile.gettempdir()) / 'thietkegame_scrub.idx'
if idx_path.exists(): idx_path.unlink()
idx_env = dict(os.environ, GIT_INDEX_FILE=str(idx_path))
def clean_tree_of(commit_sha):
    git('read-tree', commit_sha, env=idx_env)
    git('rm', '--cached', '-r', '--ignore-unmatch', '--', *forbidden, env=idx_env)
    return git('write-tree', env=idx_env)
b0_tree = clean_tree_of(merge_base_commit)
b0_meta = git('show', '-s', '--format=%an%n%ae%n%aI%n%cn%n%ce%n%cI', merge_base_commit).splitlines()
c_env = dict(os.environ, GIT_AUTHOR_NAME=b0_meta[0], GIT_AUTHOR_EMAIL=b0_meta[1], GIT_AUTHOR_DATE=b0_meta[2], GIT_COMMITTER_NAME=b0_meta[3], GIT_COMMITTER_EMAIL=b0_meta[4], GIT_COMMITTER_DATE=b0_meta[5])
prev = git('commit-tree', b0_tree, '-p', base_main, env=c_env, inp=b'chore(project): import GothicDungeon tileset, URP 6000.6 settings, and project docs\n')
for c in linear_commits:
    t = clean_tree_of(c)
    meta = git('show', '-s', '--format=%an%n%ae%n%aI%n%cn%n%ce%n%cI', c).splitlines()
    msg = subprocess.check_output(['git', 'show', '-s', '--format=%B', c])
    ce = dict(os.environ, GIT_AUTHOR_NAME=meta[0], GIT_AUTHOR_EMAIL=meta[1], GIT_AUTHOR_DATE=meta[2], GIT_COMMITTER_NAME=meta[3], GIT_COMMITTER_EMAIL=meta[4], GIT_COMMITTER_DATE=meta[5])
    prev = git('commit-tree', t, '-p', prev, env=ce, inp=msg)
if idx_path.exists(): idx_path.unlink()
git('update-ref', 'refs/heads/map-feat-designing', prev)
git('read-tree', 'HEAD')
print('SCRUBBED_TIP:', prev, 'REWRITTEN_LINEAR_COMMITS:', len(linear_commits) + 1)"
```
Expected: `SCRUBBED_TIP: <sha> REWRITTEN_LINEAR_COMMITS: 37` (or exact count); zero working-tree files touched on disk (`Assets/` `st_mtime` and all 6 local files untouched!).

- [ ] **Step 4: Verify byte-identical project tree, delete temporary backup refs, and run the Combined 8-Point Repo + Monotonic Gate**

Run:
```powershell
rtk proxy git diff backup/pre-r9-post-commit HEAD -- Assets/ Packages/ ProjectSettings/ docs/ .gitignore
rtk proxy git branch -D backup/pre-r9-history-scrub backup/pre-r9-post-commit
rtk git status --short
rtk proxy git log origin/main..HEAD --oneline
rtk proxy git diff --check origin/main...HEAD
rtk proxy python -c "import pathlib, subprocess
targets = ['AGENTS.habayakka.md','# MASTER PROMPT \u2014 COMPLETE UNITY 2D.md','GEMINI.txt','tools/caveman-posttooluse.patch','tools/fortress/prepare_workspaces.py','tools/fortress/test_prepare_workspaces.py']
for t in targets:
    assert pathlib.Path(t).exists(), f'LOCAL FILE LOST ON DISK: {t}'
tracked = set(subprocess.check_output(['git','ls-files']).decode('utf-8').splitlines())
for t in targets:
    assert t not in tracked, f'FORBIDDEN FILE STILL TRACKED: {t}'
hist = subprocess.check_output(['git','log','origin/main..HEAD','--oneline','--'] + targets).decode('utf-8').strip()
assert not hist, f'FORBIDDEN FILE STILL IN COMMIT HISTORY:\n{hist}'
subjects = subprocess.check_output(['git','log','origin/main..HEAD','--format=%s']).decode('utf-8').splitlines()
allowed = ('feat','fix','chore','docs','test','refactor','perf','ci','build')
bad = [s for s in subjects if not any(s.startswith(p + '(') or s.startswith(p + ':') for p in allowed)]
assert not bad, f'NON_CONVENTIONAL_COMMITS: {bad}'
for p in pathlib.Path('Assets/_Project').rglob('*'):
    if p.suffix != '.meta':
        assert pathlib.Path(str(p) + '.meta').exists(), f'Missing .meta for {p}'
src_mtimes = [p.stat().st_mtime for root in ('Assets/_Project','Packages','ProjectSettings') for p in pathlib.Path(root).rglob('*') if p.is_file()]
max_src = max(src_mtimes)
gates = ['Logs/r9-final-core-edit.xml','Logs/r9-final-nggc-edit.xml','Logs/r9-final-playmode.xml','Logs/r9-final-win64.log'] + [f'Logs/r9-gpu-captures/{p.name}' for p in sorted(pathlib.Path('Logs/r9-gpu-captures').glob('gpu_r9_ng_*.png'))]
stale = [g for g in gates if pathlib.Path(g).stat().st_mtime <= max_src]
assert len(gates) == 19 and not stale, f'Post-scrub stale gates: {stale}'
print('CLEAN_PR_AND_MONOTONIC_GATE_OK: commits=', len(subjects), 'history_leaks=0 local_files=6/6 monotonic_gates=19/19')"
```
Expected:
- `git diff backup/pre-r9-post-commit HEAD -- Assets/ Packages/ ProjectSettings/ docs/ .gitignore` is **empty**.
- `git status --short` outputs **nothing** (clean tree; all 6 local files remain intact on disk and ignored by `.gitignore`).
- `CLEAN_PR_AND_MONOTONIC_GATE_OK: commits= 37 history_leaks=0 local_files=6/6 monotonic_gates=19/19`.
- **STOP CONDITION:** Do NOT run `git push` or `gh pr create` unless Master explicitly commands it.
