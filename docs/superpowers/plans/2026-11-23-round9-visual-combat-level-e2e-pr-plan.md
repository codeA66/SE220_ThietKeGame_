# Round 9 E2E Remediation, Visual Audit, Combat Overhaul, QA Gate & PR Plan (v2 — Hardened & Evidence-Locked)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (or `/swarm` coordinator + isolated `share` workers) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Every phase and task lists mandatory **Unity Plugin**, **CCGS**, and **Superpowers** skills — workers MUST read each assigned skill file before modifying code, prefabs, shaders, or scene assets.

**Goal:** Resolve all 13 verified playtest, physics, level-design, hack-and-slash game-feel, and visual/atmosphere defects in `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` (`P0-01..P0-05`, `P1-01..P1-08`) plus 2 visual defects uncovered in the Round 9 GPU screenshot audit (outdoor window `LittleBeamTL/TR` black L-ears + open-doorway floating cobweb bowties), prove zero regressions across BOTH `Main_Level.unity` and `NhaGiamGiengChuong.unity` via full automated EditMode/PlayMode suites + **15-shot D3D12 GPU visual audit** (`Logs/r9-gpu-captures/gpu_r9_ng_01..15.png`) + Win64 standalone `.exe` build, enforce clean git branch hygiene on `map-feat-designing`, push to `origin`, and open a review-ready Pull Request targeting `main`.

**Architecture:** Preserve all `.asmdef` module boundaries (`Core`, `VFX`, `CameraSystem`, `Combat`, `Currency`, `Enemies`, `HUD`, `Player`, `RunFlow`, `NhaGiamGiengChuong`, `TilesetTools.Editor`, and all 3 test `.asmdef`s) with **zero `.asmdef` edits** and **zero manual `.meta` edits**. Fix physics, 45° slope traversal, chain climbing, and hack-and-slash combat directly in Player/Enemy/Currency runtime scripts & prefabs; rebuild `Stairs_H`, purposeful platforms, Room N/T payoffs, Core Spine encounters, curved gothic weathered windows, visibly silky dual-layer `GroundMist2D`, floor-to-ceiling stone pilasters, true 24-PPU pixel-density cohesion, zero-overlap floor junk, inter-tier stone slab strata, and full-height peripheral foreground framing deterministically inside `NhaGiamGiengChuongBuilder` + `PrisonPropSpriteGenerator` + `EnvironmentMaterialBuilder`; lock every invariant with automated EditMode & PlayMode tests (`Core.Tests.EditMode` + `A01..A21` + `P01..P10`) and a 15-shot D3D12 GPU visual verification gate.

**Tech Stack:** Unity `6000.6.9f1` (URP 2D, 2D Lights, 2D ShadowCaster, InputSystem, Tilemap, Sprite2D), C#, HLSL (`DeadCells/GroundMist2D`), NUnit / Unity Test Framework (EditMode + PlayMode), PowerShell + `rtk` / `rtk proxy`, `codegraph`, `gh` CLI, Git (`origin = https://github.com/codeA66/SE220_ThietKeGame_.git`, default base branch `main`, feature branch `map-feat-designing`).

---

## 0. Audit Hardening Ledger (Critical Traps Fixed vs Plan v1)

Every item below was verified against on-disk code, `.asset`/`.prefab` YAML, sprite pixel dimensions, and the 9 existing D3D12 GPU captures (`Logs/r6-gpu-captures/gpu_r6_ng_01..09.png`):

1. **45° `Stairs_H` Slope Physics Blocker (`PlayerContext.cs:14,153`):** `MinGroundNormalY = 0.7f` has only `0.007f` margin against $\cos(45^\circ) = 0.7071$, and `_body.linearVelocity.y <= 0.01f` flips `IsGrounded = false` on frame 1 when walking UP a 45° ramp (plus idle player slides downhill). **Locked fix in Task 1.2b:** `MinGroundNormalY = 0.62f`, normal-velocity ground check `Dot(v, n) <= 0.15f` on solid slopes (preserving `vy <= 0.01f` on `OneWayPlatform`), tangential velocity projection on slopes, and zero-slide idle gravity pause while stationary on a slope.
2. **Multi-Frame `PerformStrike` VFX/SFX Spam (`PlayerCombat.cs:74-77`):** Calling `PerformStrike` across a `0.065s` active window would invoke `StrikePerformed` 4–8× per swing (spawning 4–8 duplicate slash trails + SFX). **Locked fix in Task 1.3:** Add `bool emitSwingEvent = true` parameter to `PlayerCombat.PerformStrike`; pass `true` ONLY on the first active frame and `false` on subsequent active-window ticks.
3. **Chain Top Dismount `P01` Infinite Re-Grab Loop (`PilotPlayTests.cs:75` + `PlayerClimbState.cs`):** Blocking `W` re-grab only when `IsGrounded` fails during the 1-frame airborne settle right after top dismount while `W` is still held. **Locked fix in Task 1.2a:** Reject `W` climb grab whenever `Position.y >= climbable.bounds.max.y - 0.22f` **regardless of `IsGrounded`**, AND snap top dismount to `bounds.max.y - 0.06f` (`8.02f` on `8.08f` trigger -> satisfies `P01`'s `y >= 8.0f` and settles flush within `GroundCheckDistance = 0.05f`).
4. **Exact Real Paths & Serialized Fields:**
   - `PlayerContext.cs` is at `Assets/_Project/Features/Player/Scripts/PlayerContext.cs` (properties: `ClimbTarget`, `Position`, `_droppedPlatform != null`).
   - `Weapon_StartSword.asset` is at `Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset` (struct `WeaponData.Strike`: `AnimSpeed`, `HitFrame`, `RangeWidthCells`, `RangeHeightCells`, `Knockback`, `CanCrit` — hitstop lives in `PlayerFeedback.cs`).
   - `PlayerMovementConfig.asset` uses `_runSpeed`, `_airSpeed`, `_comboChainNormalizedTime`, `_attackMoveSpeedMultiplier` (no `_groundAcceleration`/`_groundDeceleration`).
   - `VisualShaderAndNormalTests.cs` is at `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`.
   - `I8_Chain_MO` (not `I8_Chain_OM`); Win64 build entry is `DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildStandaloneWin64`.
   - Unity Plugin skill docs live at `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\<name>\<name>.md`.
5. **`GoldPickup.cs` Static Scene Serialization (`GoldPickup.cs:15-17`):** `_wallet`, `_player`, `_value` are private non-serialized fields. **Locked fix in Task 2.3:** Add `[SerializeField]` static-cache support to `GoldPickup.cs` (`_wallet`, `_player`, `_value`, `_staticPickup` keeping `Rigidbody2D` kinematic/sleeping until player enters magnet range) so Room N's 4-coin treasure cache actually credits `GoldWallet` in PlayMode.
6. **Existing Test Contracts Locked Explicitly (Zero Surprise Failures):**
   - `Round8Phase2FeelMovementTests.Feel.cs:26-52` asserts exact `Weapon_StartSword` values -> updated in Task 1.3.
   - `ExpansionPlayTests.cs` already owns **`P08`** AND calls `FindAnyObjectByType<LoreTablet>()` & `FindAnyObjectByType<SecondaryBiomeExitTrigger>()` -> updated to name-targeted `.Single(...)` in Task 2.3; new Round 9 PlayMode tests are **`P09` & `P10`**.
   - `MainLevelIntegrityTests.cs` loads `Main_Level.unity` which instantiates `Player.prefab` and checks EVERY `Point Light2D` `m_ApplyToSortingLayers` (must exclude Parallax/Foreground) AND checks `prisonArchSill_00` (`24×8`) / `prisonArchJamb_00` (`8×24`) -> `PlayerRimLight` MUST serialize exact TagManager layer IDs directly on `Player.prefab`, and `prisonArchSill_00` / `prisonArchJamb_00` MUST keep exact names, `24×8`/`8×24` dimensions, and 24 PPU!
   - `MapGeometryTests.A06/T3` (`[-102..9]@16` -> `10f`), `A14:1115` (`roomHStairAirCells` `(15,10)/(12,12)` -> `(15,11)/(12,14)`), `A15:1280,1336` (`ArchBeam_TL/TR` + deleted braces), `A16:1683` (`_BedrockStone_` must stay `dirt_00` -> inter-tier stone strata named `Art_*_SlabStrata_*`), `A11:838` + `A18:1810` (`GroundMist_R5` count `6 → 9`), `A18:1786,1821,1829` (45° stairs, `Art_Q_SkeletonSpawn`, removed `Art_H_StepJunkMid`) -> every single one updated in its owning task!
7. **Visual Audit Findings Directly From GPU Captures `01..09`:**
   - **Outdoor window black L-ears (`CarveAndFrameArchBay:3627-3628`):** `LittleBeamTL/TR` (`45×45px` black silhouettes) stick outside every carved window corner! Replaced by custom 24-PPU carved stone Gothic arch spandrels (`prisonArchSpandrel_00.png`, `24×24px`) recessed flush into top corners, with lintel/jambs trimmed cleanly to meet them (zero double-drawn box corners!).
   - **Floating cobweb bowties at open doorways (`gpu_r6_ng_01`, `02`):** `AddCornerBeamsAndWebs:4137-4145` places `WebSmallTL/TR` at `(17,4)` & `(18,4)` under shallow open doorway headers! Gated on solid wall corners (`hasLeftBottomWall` / `hasRightBottomWall` or deep header >= 3.0u).
   - **Room overflow prevented on all 12 mixel props (measured native px):** `snakeLandmark_00` (`23.5×19.4u`), `skeletonSpawn_00` (`12×13u`), `weaponDeco_00` (`16×5u`), `bannerBig_00` (`3×9u`), `fioleA/B/C` (`3×3u`) CANNOT be scaled to `0.85x` in `6–7u` rooms! Task 3.5 specifies exact native-24-PPU replacements/relocations & custom 24-PPU `prisonShelfVial_00..02.png` + `prisonBell_00.png` with verified zero overflow!
   - **Room G `Art_G_Winch` (`62.5` overlapped `Art_G_Alcove [56.15..64.85]`!):** Moved cleanly to `(73.6, 8)` on solid `Floor_G_UpperEast` using native 24-PPU `chainAnchor_00.png`.
   - **Tall pilasters (`P1-02`):** Use slender `spawnerPillar_00.png` (`2.62u` wide, NOT `7.54u`-wide `alcovePillar_00`!) at verified clear X coordinates from `floorY` to `ceilingY`.
   - **15-Shot Round 9 GPU Capture Suite:** Baseline 9 shots missed `Stairs_H`, Player spawn, East Room N, and East Room T! Added `CaptureRound9GpuVerificationFrames()` capturing **15 shots** (`Logs/r9-gpu-captures/gpu_r9_ng_01..15.png`) with an explicit per-shot visual inspection rubric!

---

## 1. Mandatory Skill Matrix (Exact On-Disk Paths)

Every agent executing a phase or task MUST read the assigned skill files **before editing**. Per `AGENTS.md §9`, Unity plugin skills are mandatory for all Unity work; CCGS skills enforce studio-grade combat, level, polish, QA, and release rigor.

| Phase / Role | Unity Plugin Skills | CCGS Studio Skills | Superpowers & Discipline Skills |
| --- | --- | --- | --- |
| **Coordinator / All Phases** | `using-unity`, `unity-cli`, `unity-pipeline` | `ccgs-gate-check`, `ccgs-milestone-review` | `subagent-driven-development` (or `executing-plans`), `karpathy-guidelines`, `ponytail` |
| **Phase 1: Physics, 45° Slopes, Climb & Combat** | `create-2d-physics`, `craft-game-floors`, `unity-cli` | `ccgs-team-combat`, `ccgs-balance-check`, `ccgs-code-review` | `test-driven-development`, `systematic-debugging`, `verification-before-completion` |
| **Phase 2: 45° `Stairs_H`, Platforms, Room N/T & Spine Packs** | `create-tile-palette-or-rule-tiles`, `create-2d-physics`, `unity-cli` | `ccgs-team-level`, `ccgs-balance-check`, `ccgs-consistency-check` | `test-driven-development`, `verification-before-completion` |
| **Phase 3: Visual Overhaul, Gothic Windows, Silky Fog, Cohesion & Readability** | `2d-pixel-perfect`, `sprite-editor`, `urp-postprocessing`, `craft-game-floors`, `unity-cli` | `ccgs-team-polish`, `ccgs-asset-audit`, `ccgs-perf-profile` | `test-driven-development`, `verification-before-completion` |
| **Phase 4: PlayMode Proof, 15-Shot GPU Audit, Build, Git Hygiene, Push & PR** | `unity-cli`, `unity-pipeline` | `ccgs-team-qa`, `ccgs-smoke-check`, `ccgs-regression-suite`, `ccgs-test-evidence-review`, `ccgs-code-review`, `ccgs-team-release`, `ccgs-release-checklist`, `ccgs-changelog` | `requesting-code-review`, `verification-before-completion`, `finishing-a-development-branch` |

> **Verified Skill File Paths (exact on-disk locations):**
> - Unity Plugin Index: `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\SKILL.md`
> - Unity Plugin Sub-Skills (`create-2d-physics`, `craft-game-floors`, `create-tile-palette-or-rule-tiles`, `2d-pixel-perfect`, `sprite-editor`, `urp-postprocessing`, `unity-pipeline`): `C:\Users\habayakka\.gemini\config\plugins\unity\skills\using-unity\references\<name>\<name>.md`
> - Unity CLI (repo-local): `d:\thietkegame\.agents\skills\unity-cli\SKILL.md`
> - CCGS Skills (`ccgs-<name>`): `C:\Users\habayakka\.gemini\config\plugins\codex-migrated\skills\ccgs-<name>\SKILL.md`
> - Superpowers Skills: `C:\Users\habayakka\.gemini\config\plugins\superpowers\skills\<name>\SKILL.md`

---

## 2. Verified Round 9 Traceability Matrix (13 Issue Groups + 2 GPU-Audit Finds → Tasks → Proofs)

| ID | Priority | Verified Defect & Evidence | Owning Task(s) | Automated Proof + Visual Proof (`Logs/r9-gpu-captures/`) |
| --- | --- | --- | --- | --- |
| **P0-01** | P0 | `Stairs_H` uses 5 blocky `2u` steps with double-stacked diagonal tiles (`Builder:665-679, 3904-3930`), floating `6×6u` `Caster_StairsH` (`Builder:4874`), cluttered landing props/torches, AND `PlayerContext` cannot walk smoothly up 45° slopes (`MinGroundNormalY=0.7f`, `vy<=0.01f`) | Task 1.2b, Task 2.1 | `Round9CombatPhysicsTests` (`T04b`), `A04`, `A06/T3`, `A14`, `A18`, **`P09`** (PlayMode smooth walk up/down `Stairs_H` + zero idle slide), **GPU Shot 10** (`Stairs_H` close-up) |
| **P0-02** | P0 | All 7 chain triggers overhang `+0.50u` above deck (`Builder:455,602,692,909,1064,1234,1373`); `PlayerClimbState:47` floats above bridge; `W` on deck re-grabs air; `S` cannot descend; `ladderHole_00` horns poke `+0.5u` above bridge (`Builder:4002`); `Art_G_Winch` sits on `I2_Chain_G` (`Builder:632`) | Task 1.2a, Task 2.2 | `Round9CombatPhysicsTests` (`T02..T04`), `A01`, `A03`, `A04`, `A05b..e`, `A06`, `A20`, `P01`, **`P09`** (flush dismount, no `W` re-grab, `S` hold-descend), **GPU Shots 02, 03** |
| **P0-03** | P0 | Player stands on top of `Enemy_Shield` & all enemies (`Physics2DSettings.asset:56` Layer 7↔8 solid). **Constraint:** `Projectile_Arrow.prefab` is Layer 8 `OnTriggerEnter2D` -> keep Layer 7↔8 matrix enabled; set `m_ExcludeLayers = 128` (`1<<7`) ONLY on the 5 `Enemy_*.prefab` `Rigidbody2D`s + `EnemyController.Awake` | Task 1.1 | `Round9CombatPhysicsTests` (`T01`), `P02` (arrow hits Player), `Round8Phase1CombatAiTests` (bow still hits enemies), **`P09`** (Player drops through `Enemy_Shield` head) |
| **P0-04** | P0 | 6 zero-purpose `OneWay_*` platforms (`OneWay_A_Practice`, `OneWay_C_Rest`, `OneWay_N_High`, `OneWay_O_West`, `OneWay_S_Gallery`, `OneWay_T_High`) + dead-end `Room N` (0 POIs) & `Room T` (`102u` wide, fake exit `Art_T_CourtyardArch`, 0 rewards); `OneWay_E_Combat` needs tactical purpose without breaking `P02`/`P05` | Task 2.2, Task 2.3 | `A01`, `A02`, `A05`, `A05b..e`, `A06`, `A10`, `A11`, `A13`, `A14`, `A15`, `P01`, `P02`, `P05`, `P08`, **`P10`** (Room T Promenade exit win + Room N lore/gold), **GPU Shots 01, 02, 05, 11, 12, 13** |
| **P0-05** | P0 | Slow platformer combat feel: ground strike `3.8 u/s` lunge wiped next `FixedTick` (`PlayerAttackState:73,109`), Hit 3→Hit 1 combo buffer blocked (`L65`), `RunSpeed=7` & `ComboChainNormalizedTime=0.6`, 1-frame hitbox, no air hit-stall, flat heavily-damped enemy knockback (`EnemyStunState:27,48`), sparse 5-enemy Core Spine | Task 1.3, Task 1.4, Task 2.4 | `Round9CombatPhysicsTests` (`T05..T08`), `Round8Phase2FeelMovementTests.Feel`, **`P10`** (persistent lunge displacement, 4-hit continuous 1→2→3→1 combo, single-swing VFX guard, knockback pop), `A07`/`A13`/`A17`/`P07` (34 enemies, 12 zones) |
| **P1-01** | P1 | `47/85` `AddFloorJunkCluster` self-overlaps (hardcoded `+0.65f` at `Builder:4571`) + direct landmark/torch clashes (`Art_O_JunkEast4` on `Art_O_MonsterDoor`, `Art_O_Junk1` vs `Mid1`, `Art_O_Junk2` vs `East2`, `Art_S_JunkEast2` on `Art_S_AlchemyBibli2`, 4+ torch clashes) | Task 3.3 | `A20` (Automated zero-overlap audit across ALL junk pairs, landmarks, POIs, and lantern fixtures), **GPU Shots 01–15** |
| **P1-02** | P1 | 7 `WoodenBeamTruss` (`PrisonwoodenBeam_00.png`, `6.0u`) end in mid-air inside `14.5–22.5u` tall rooms and read as false climbable ladders | Task 3.4 | `A16` updated + `A20` (All 7 replaced by floor-to-ceiling stacked `spawnerPillar_00` stone pilasters reaching `ceilingY`, zero clashes), **GPU Shots 06, 07, 08, 09, 12, 13** |
| **P1-03** | P1 | Floating black mid-wall blobs (`wallJunk_10..13` in `PaintPrisonBackWall:3584-3585`), unanchored `ChainedPrisoners`, cut-off dangling `Foreground_Silhouettes` stubs (`Builder:4795-4812`), AND floating open-doorway cobweb bowties (`AddCornerBeamsAndWebs:4137-4145`, seen in `gpu_r6_ng_01`/`02`) | Task 3.4 | `A16` + `A20` (`0` `wallJunk_10..13` on `TM_WallDecals`, all 8 `ChainedPrisoners` chain-anchored to `ceilingY`, full-height peripheral `Foreground_Silhouettes`, zero open-doorway top webs), **GPU Shots 01, 02, 06, 07, 08, 09, 14** |
| **P1-04** | P1 | Art style clash (`GothicDungeon` barrel/crate/winch/bell in Rooms G/C) + 12 severe mixel scale violations (`0.24x` `WeaponDeco`, `0.28x` `SnakeLandmark` & `AddShelfWithFioles` `Fiole_0..2`, `0.48x` `SkeletonSpawn`, `0.50x` `B_Banner`, `0.55x` `BigSword`, `0.58x` `AlchemyBibli1/2` & `G_Alcove`, `0.60–0.75x` banners/desk) | Task 3.5 | `A08`, `A10`, `A12`, `A14`, `A18`, `A20` (Strict pixel-density guard `localScale in [0.78..1.25]` + `effectivePPU in [19.2..30.8]` on ALL `Art_*` props, zero room/bay/platform overflow, cohesive 24-PPU Room G/C), **GPU Shots 01, 02, 03, 06, 14, 15** |
| **P1-05** | P1 | Low Player figure-ground readability in dark corners after `PlayerSpawnHead` merges (`PlayerSpawnHead.cs`) | Task 3.6 | `A21` + `MainLevelIntegrityTests` (`Player.prefab` carries `PlayerRimLight` `Light2D` with serialized TagManager gameplay layer IDs strictly excluding Parallax/Foreground), **GPU Shot 11** |
| **P1-06** | P1 | `Art_A_Torch` squeezed between Bay #0 & `TP_0_Spawn`, flat black `1.0–2.5u` inter-tier floor/ceiling slabs, pitch-black `Art_G_Alcove` | Task 3.6 | `A21` (`Art_A_Torch` >= 1.2u clear of Bay #0 & `TP_0_Spawn`; `Art_*_SlabStrata_*` carved stone courses on inter-tier slabs without breaking `A16` `_BedrockStone_`; `Art_G_AlcoveFillLight`), **GPU Shots 01, 02, 03, 04, 11** |
| **P1-07** | P1 | Outdoor windows look like pasted rectangular stickers: giant black `LittleBeamTL/TR` corner ears (`Builder:3627-3628`), uniform bright rectangular jamb/sill border with `1px` black outline, identical cloned cobweb pairs on all 16 bays, zero gothic bars/recessed depth, zero moonlight spill | Task 3.1 | `VisualShaderAndNormalTests` + `MainLevelIntegrityTests` + `A12`/`A15`/`A21` (24-PPU weathered chipped jamb/sill `00..02`, curved stone `prisonArchSpandrel_00` replacing black ears, wrought-iron `prisonArchMullion_00`, varied per-bay ivy/web dressing, exterior cool moonlight `Light2D` spill), **GPU Shots 01, 03, 04, 05, 07, 11** |
| **P1-08** | P1 | `GroundMist2D` fog is practically invisible (`~4.5%` effective pixel alpha) and only exists in 6 rooms (missing Room A/B, E, T) | Task 3.2 | `VisualShaderAndNormalTests` + `A11` + `A18` + `A21` (Dual-layer smooth quintic FBM, `_MistColor.a = 0.62`, `mistTint.a = 0.86`, `2.8u` height, `9` strips covering damp floors including A/B, E, T, effective core alpha `0.26..0.44` with zero rectangular edges), **GPU Shots 01, 02, 03, 05, 07, 11, 13** |

---

## 3. Global Constraints, Unity Command Standard & Pinned Failure Modes

1. **Zero `.asmdef` edits, zero manual `.meta` edits.** Let Unity batchmode generate `.meta` files cleanly whenever new generated sprites (`prisonArchSill_01/02`, `prisonArchJamb_01/02`, `prisonArchSpandrel_00`, `prisonArchMullion_00`, `prisonShelfVial_00..02`, `prisonBell_00`) and their `_n.png` normal maps are written via `PrisonPropSpriteGenerator.EnsureAll()`.
2. **All shell commands via `rtk` / `rtk proxy`** (`AGENTS.md §0`). Always run Unity batchmode with:
   ```powershell
   rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater ...
   ```
   - Omit `-nographics` **ONLY** for `CaptureRound9GpuVerificationFrames` (D3D12 GPU rendering).
   - Never run two Unity Editor processes concurrently against `D:/thietkegame` (shared `Library/` lock).
3. **Top 7 Failure Modes Pinned to Owning Tests:**
   - **FM-1 — Breaking `Projectile_Arrow` trigger contact (`P0-03`):** Keep Layer 7↔8 enabled in `Physics2DSettings.asset`; set `m_ExcludeLayers.m_Bits = 128` (`1<<7`) ONLY on the 5 `Enemy_*.prefab` `Rigidbody2D`s + `EnemyController.Awake`. **Pinned:** `Round9CombatPhysicsTests.T01`, `EncounterPlayTests.P02`, `Round9PlayTests.P09`.
   - **FM-2 — Breaking `PilotPlayTests.P01` top dismount OR crouch-drop (`P0-02`):** Reject `W` climb grab whenever `Position.y >= climbable.bounds.max.y - 0.22f` (grounded OR airborne); snap top dismount to `bounds.max.y - 0.06f` (`8.02f >= 8.0f`); hold `S` without `Space` for `>= 0.18s` in `PlayerCrouchState` before mounting chain downward; block `S` mid-air chain catch while `Ctx.IsDroppingThrough` (`_droppedPlatform != null`). **Pinned:** `PilotPlayTests.P01`, `Round9CombatPhysicsTests.T02..T04`, `Round9PlayTests.P09`.
   - **FM-3 — Stuttering / sliding on 45° `Stairs_H` (`P0-01`):** Lower `MinGroundNormalY` to `0.62f`, use `Dot(v, hit.normal) <= 0.15f` on solid slopes (not `vy <= 0.01f`), project grounded move velocity along slope tangent, zero velocity & pause gravity while idle on slope. **Pinned:** `Round9CombatPhysicsTests.T04b`, `Round9PlayTests.P09`.
   - **FM-4 — Multi-frame `PerformStrike` duplicating slash VFX/SFX (`P0-05`):** Pass `emitSwingEvent: true` ONLY on the first active frame of `PlayerAttackState`, `false` on follow-up frames of the `0.065s` window. **Pinned:** `Round9CombatPhysicsTests.T05`, `Round9PlayTests.P10`.
   - **FM-5 — Breaking `Main_Level.unity` tests (`MainLevelIntegrityTests`):** Adding `PlayerRimLight` to `Player.prefab` MUST serialize `m_ApplyToSortingLayers = [0, 989596349, 1656045982, 2034603147, 219262143]` directly on `Player.prefab`; `prisonArchSill_00.png` (`24×8`) & `prisonArchJamb_00.png` (`8×24`) MUST keep exact names, dimensions, and 24 PPU. **Pinned:** `Core.Tests.EditMode` (`MainLevelIntegrityTests` + `VisualShaderAndNormalTests`).
   - **FM-6 — Breaking `ExpansionPlayTests.P08` with 2nd LoreTablet / 2nd SecondaryBiomeExit / static GoldPickups (`P0-04`):** Update `ExpansionPlayTests.cs` to select `"I16_LoreTablet"` and `"I15_SewerExitTrigger"` by name and measure newly dropped `GoldPickup` count near `(92, 3.5)`. **Pinned:** `ExpansionPlayTests.P08`, `Round9PlayTests.P10`.
   - **FM-7 — Committing Unity autosave junk (`Assets/_Recovery/`) or breaking clean-clone build on `map-feat-designing`:** Audit all 209+ dirty/untracked paths in Phase 4, add `/[Aa]ssets/_Recovery/` to `.gitignore` (and never stage `_Recovery/`), commit only verified clean project files in conventional commits, verify `git diff --check` + zero files > 50 MB before push & PR to `main`. **Pinned:** Task 4.3.

---

## Phase 1 — Physics, 45° Slope Grounding, Chain Climb & Hack-and-Slash Combat Feel (`P0-01` Runtime, `P0-02` Runtime, `P0-03`, `P0-05`)

> **Phase 1 Mandatory Skills (read before editing):**
> - Unity Plugin: `create-2d-physics`, `craft-game-floors`, `unity-cli`
> - CCGS: `ccgs-team-combat`, `ccgs-balance-check`, `ccgs-code-review`
> - Superpowers: `test-driven-development`, `verification-before-completion`

### Task 1.1: Enemy↔Player Body Pass-Through Without Breaking Arrow Triggers (`P0-03`)

**Skills:** `create-2d-physics`, `ccgs-team-combat`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/Enemies/Scripts/EnemyController.cs`
- Modify: `Assets/_Project/Features/Enemies/Prefabs/Enemy_Zombie.prefab`
- Modify: `Assets/_Project/Features/Enemies/Prefabs/Enemy_Rat.prefab`
- Modify: `Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab`
- Modify: `Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab`
- Modify: `Assets/_Project/Features/Enemies/Prefabs/Enemy_Grenadier.prefab`
- Create: `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs`

**Design Interface:**
- All 5 `Enemy_*.prefab` `Rigidbody2D` components serialize `m_ExcludeLayers.m_Bits: 128` (`1 << 7`, Layer 7 = `Player`).
- `EnemyController.Awake()` enforces defensively before constructing `EnemyContext`:
  ```csharp
  var body = GetComponent<Rigidbody2D>();
  int playerLayer = LayerMask.NameToLayer("Player");
  if (body != null && playerLayer >= 0)
      body.excludeLayers |= 1 << playerLayer;
  ```
- `ProjectSettings/Physics2DSettings.asset` stays **UNTOUCHED** (`Physics2D.GetIgnoreLayerCollision(7, 8) == false`) so `Projectile_Arrow.prefab` (Layer 8, `m_ExcludeLayers.m_Bits: 0`) continues firing `OnTriggerEnter2D` on `Player`, while `HitQuery` (`OverlapBox`) and `RecoverableArrow` (raycast) continue hitting all enemies identically.

**Steps:**
- [x] **Step 1:** Read `create-2d-physics.md` and `ccgs-team-combat/SKILL.md`.
- [x] **Step 2:** Create `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs` with failing EditMode test `T01_AllFiveEnemyPrefabsExcludePlayerLayerOnRigidbody_WhileArrowKeepsTriggerContact`: loads all 5 `Enemy_*.prefab` + `Projectile_Arrow.prefab`, asserts `(enemyRb.excludeLayers.value & (1 << 7)) != 0` on all 5 enemies, asserts runtime `EnemyController.Awake()` enforces bit `1 << 7` on a fresh `EnemyController` test GameObject, asserts `(arrowRb.excludeLayers.value & (1 << 7)) == 0` on `Projectile_Arrow.prefab`, and asserts `Physics2D.GetIgnoreLayerCollision(7, 8) == false`.
- [x] **Step 3:** Run `Core.Tests.EditMode` filter `Round9CombatPhysicsTests` via Unity batchmode -> verify **RED**.
- [x] **Step 4:** Add the defensive `excludeLayers |= 1 << playerLayer` guard in `EnemyController.Awake()` and set `m_ExcludeLayers.m_Bits: 128` on the `Rigidbody2D` of all 5 `Enemy_*.prefab` files (preserving every existing GUID and fileID).
- [x] **Step 5:** Re-run `Round9CombatPhysicsTests` -> verify **GREEN**.
- [x] **Step 6:** Commit: `fix(enemies,physics): exclude Player layer on enemy Rigidbodies to prevent head-standing`

---

### Task 1.2: Chain Climb Precision (`P0-02` Runtime) + 45° Slope Grounding & Walk Support (`P0-01` Runtime)

**Skills:** `create-2d-physics`, `ccgs-team-combat`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerContext.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerStateBase.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerClimbState.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerCrouchState.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerFallState.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerRunState.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerIdleState.cs`
- Modify: `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs`

**Design Interface — Part A: Chain Climb Precision (`P0-02`):**
- `PlayerContext.cs`:
  - Expose `public bool IsDroppingThrough => _droppedPlatform != null;`
  - Add `public bool CanMountClimbableUpward()`: calls `TryFindClimbable()`; returns `true` ONLY if `ClimbTarget != null && Position.y < ClimbTarget.bounds.max.y - 0.22f` (blocks `W` re-grab at the top deck whether grounded OR during the 1-frame settle after top dismount!). If rejected, clear `ClimbTarget = null` and return `false`.
  - Add `public bool TryFindClimbableBelowDeck(out Collider2D climbable)`: returns `true` only when `IsGrounded && _groundCollider != null && IsOneWayPlatform(_groundCollider)`; probes `Physics2D.OverlapBox(new Vector2(_collider.bounds.center.x, _collider.bounds.min.y - 0.30f), new Vector2(0.55f, 0.55f), 0f, _climbableMask)` to find a chain directly beneath the wooden deck slot.
  - Add `public void BeginDescendClimb(Collider2D climbable)`: ignores `_groundCollider` for `Config.DropThroughDuration` (`_droppedPlatform = _groundCollider; _ledgeIgnoredPlatform = _groundCollider; _dropRestoreTime = Time.time + Config.DropThroughDuration; Physics2D.IgnoreCollision(_collider, _droppedPlatform, true);`), sets `ClimbTarget = climbable`, sets `_body.position = new Vector2(climbable.bounds.center.x - FacingDirection * Config.ClimbHandOffsetX, climbable.bounds.max.y - 0.30f)` and `IsGrounded = false`.
  - Add `public void SnapPosition(Vector2 worldFeet)` (`_body.position = worldFeet; _body.linearVelocity = Vector2.zero;`).
- `PlayerStateBase.cs`:
  - Update `TryClimb()` to call `Ctx.IsUpHeld && Ctx.CanMountClimbableUpward() && Machine.ChangeState(Machine.Climb)`.
- `PlayerClimbState.cs`:
  - In `Tick()`: when `Ctx.IsUpHeld && y >= bounds.max.y - 0.10f`: call `Ctx.SnapPosition(new Vector2(Ctx.Position.x, bounds.max.y - 0.06f))` (places feet at `8.02f` on an `8.08f` trigger -> satisfies `P01`'s `y >= 8.0f`, sits within `GroundCheckDistance=0.05f` of `deckTopY=8.00f`, ZERO `+0.50u` mid-air float!) and exit cleanly via `Machine.ChangeState(Ctx.HasMoveInput ? Machine.Run : Machine.Idle)` (or `ReturnToDefault()`).
- `PlayerCrouchState.cs`:
  - Track `private float _holdTimer;` (reset to `0f` in `Enter()`).
  - Frame-0 behavior stays 100% identical (preserves `PilotPlayTests.P01` pressing `S` then immediately `Space`!).
  - In `Tick()` after `if (TryGroundedActions()) return;`: accumulate `_holdTimer += Time.deltaTime;` (provide overload `Tick(float deltaTime)` for deterministic EditMode testing!). When `_holdTimer >= 0.18f && Ctx.IsDownHeld && !Ctx.Input.JumpHeld && Ctx.TryFindClimbableBelowDeck(out var chain)`: call `Ctx.BeginDescendClimb(chain)` and `Machine.ChangeState(Machine.Climb)`.
- `PlayerFallState.cs`:
  - In `Tick()` after `TryAirActions()`: allow catching a chain while falling with `S`:
    ```csharp
    if (Ctx.IsDownHeld && !Ctx.IsDroppingThrough && Ctx.TryFindClimbable()
        && Ctx.Position.y < Ctx.ClimbTarget.bounds.max.y - 0.25f)
    {
        Machine.ChangeState(Machine.Climb);
        return;
    }
    ```

**Design Interface — Part B: 45° Slope Grounding & Traversal (`P0-01` Runtime):**
- `PlayerContext.cs`:
  - Change `MinGroundNormalY` from `0.7f` → `0.62f` (~51.7° max slope; rock-solid margin for 45° `0.7071` normal).
  - Change `FeetProbeWidthRatio` from `0.9f` → `0.96f`.
  - Track `public Vector2 GroundNormal { get; private set; } = Vector2.up;` and `public bool IsOnSlope => IsGrounded && GroundNormal.y < 0.98f && GroundNormal.y >= MinGroundNormalY;`.
  - In `RefreshFrame()`:
    - Use `float probeDist = _wasOnSlope ? Mathf.Max(Config.GroundCheckDistance, 0.22f) : Config.GroundCheckDistance;`
    - Rising guard: if `hit.collider != null`:
      - If `IsOneWayPlatform(hit.collider)` OR `hit.normal.y >= 0.98f`: require `_body.linearVelocity.y <= MaxGroundedRiseSpeed` (100% preserves jumping up through `OneWayPlatform`!).
      - Else (solid slope): require `Vector2.Dot(_body.linearVelocity, hit.normal) <= 0.15f` (walking UP a 45° slope has `Dot(v, n) ≈ 0 <= 0.15f` -> stays `IsGrounded = true`; jumping off a slope has `Dot(v, n) > 4.0f` -> immediately leaves ground!).
    - Update `GroundNormal = IsGrounded ? hit.normal : Vector2.up; _wasOnSlope = IsOnSlope;`.
  - Add `public void ApplyGroundedMove(float speedX)`:
    - If `IsOnSlope`:
      - If `Mathf.Abs(speedX) < 0.01f`: `_body.gravityScale = 0f; _body.linearVelocity = Vector2.zero;` (zero downhill sliding while idle on stairs!).
      - Else: `_body.gravityScale = Config.GravityScale; Vector2 tangent = new Vector2(GroundNormal.y, -GroundNormal.x).normalized; _body.linearVelocity = tangent * speedX;` (smooth constant speed up AND down 45° stairs!).
    - Else: `_body.gravityScale = Config.GravityScale; SetHorizontalVelocity(speedX);`.
- `PlayerRunState.cs`, `PlayerIdleState.cs`, `PlayerCrouchState.cs`:
  - Call `Ctx.ApplyGroundedMove(...)` in `FixedTick()`, and ensure `Exit()` restores `Ctx.SetGravityScale(Ctx.Config.GravityScale)`.

**Steps:**
- [x] **Step 1:** Add EditMode unit tests to `Round9CombatPhysicsTests.cs`:
  - `T02_ClimbState_RejectsUpGrabAtDeckTopWhetherGroundedOrAirborne`
  - `T03_ClimbState_TopDismountSnapsFlushAtDeckWithoutMidAirFloat`
  - `T04_CrouchState_PreservesFrame0CrouchAndMountsChainAfter018sHold`
  - `T04b_SlopeGrounding_StaysGroundedWalkingUp45DegreeRampAndZeroIdleSlide`
- [x] **Step 2:** Run `Round9CombatPhysicsTests` -> verify **RED**.
- [x] **Step 3:** Implement Part A and Part B across the 7 Player scripts.
- [x] **Step 4:** Re-run full `Core.Tests.EditMode` -> verify **GREEN** (zero regressions in `Round8Phase1CombatAiTests` or `Round8Phase2FeelMovementTests`!).
- [x] **Step 5:** Commit: `fix(player): add 45-degree slope grounding, flush chain dismount, block top W air-grab, add S descend`

---

### Task 1.3: Dead Cells Hack-and-Slash Melee Overhaul — Persistent Lunge, Continuous 3→1 Combo Loop, Multi-Frame Active Window (Single VFX/SFX) & Air Hit-Stall (`P0-05` Part A)

**Skills:** `craft-game-floors`, `ccgs-team-combat`, `ccgs-balance-check`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerCombat.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerAttackState.cs`
- Modify: `Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset`
- Modify: `Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset`
- Modify: `Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.Feel.cs` (update `StartSwordAsset_HasDeadCellsCadence_Knockback_AndFinisherCrit` to Round 9 tuned values!)
- Modify: `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs`

**Design Interface (Dead Cells Combat Feel Bar):**
- **`PlayerCombat.cs`:**
  - Change signature (backwards-compatible default):
    `public int PerformStrike(in WeaponData.Strike strike, int facing, bool emitSwingEvent = true)`
  - Guard `StrikePerformed`: `if (emitSwingEvent) StrikePerformed?.Invoke(strike, new Vector2(bounds.center.x, bounds.min.y), facing);`
  - Guarantees slash VFX & sword-whoosh SFX fire **EXACTLY ONCE per swing**, while `HitQuery._alreadyHit` guarantees each enemy takes damage **AT MOST ONCE per swing** across the entire active window!
- **`PlayerAttackState.cs`:**
  1. **Persistent Forward Strike Lunge (fixes `L73/L109` overwrite):** `LungeDuration = 0.095f`; `_lungeSpeed = Ctx.IsGrounded ? (_comboIndex >= Weapon.ComboLength ? 9.5f : 7.2f) : 2.8f`. On `_timer >= _hitTime`, set `_lungeTimer = LungeDuration` and immediately apply `Ctx.SetHorizontalVelocity(Ctx.FacingDirection * _lungeSpeed)`. In `FixedTick()`, while `_lungeTimer > 0f`, decay `_lungeTimer = Mathf.Max(0f, _lungeTimer - Time.fixedDeltaTime)` and compute `lungeVx = Ctx.FacingDirection * Mathf.Lerp(0f, _lungeSpeed, _lungeTimer / LungeDuration)`; blend against `inputVx = Ctx.Input.MoveX * baseSpeed * Ctx.Config.AttackMoveSpeedMultiplier` by taking whichever has greater magnitude (preserving forward momentum without wiping it!).
  2. **Multi-Frame Active Hitbox Window (`ActiveHitWindow = 0.065f`):** On first frame `_timer >= _hitTime` (`!_hitDone`), set `_hitDone = true` and call `PerformStrike(_strike, Ctx.FacingDirection, emitSwingEvent: true)`. On subsequent ticks while `_hitDone && _timer <= _hitTime + ActiveHitWindow`, call `PerformStrike(_strike, Ctx.FacingDirection, emitSwingEvent: false)`.
  3. **Continuous Hit 3 → Hit 1 Combo Loop (`L65` & `L82`):** Change line 65 from `if (Ctx.Input.AttackPressed && _comboIndex < Weapon.ComboLength)` to `if (Ctx.Input.AttackPressed) _nextQueued = true;`, and line 82 to `Ctx.PendingComboIndex = (_comboIndex % Weapon.ComboLength) + 1;`.
  4. **Air-Attack Hit-Stall:** On first hit landed while airborne (`!Ctx.IsGrounded && hits > 0 && !_airStallApplied`), set `_airStallApplied = true`, `_airStallTimer = 0.12f`, `Ctx.SetVerticalVelocity(Mathf.Max(Ctx.Velocity.y, 2.2f))`. In `FixedTick()`, while `_airStallTimer > 0f`, decrement `_airStallTimer -= Time.fixedDeltaTime` and set `Ctx.SetGravityScale(Ctx.Config.GravityScale * 0.35f)`.
- **`PlayerMovementConfig.asset` (exact serialized fields):**
  - `_runSpeed: 7 → 8.6`
  - `_airSpeed: 6.5 → 7.4`
  - `_comboChainNormalizedTime: 0.6 → 0.42`
  - `_attackMoveSpeedMultiplier: 0.4 → 0.5`
- **`Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset` (exact `WeaponData.Strike` fields):**
  - Hit 1 (`AttackA`): `Damage: 25`, `HitFrame: 5`, `AnimSpeed: 1.40`, `RangeWidthCells: 2.9`, `Knockback: 5.2`, `CanCrit: 0`
  - Hit 2 (`AttackB`): `Damage: 30`, `HitFrame: 7`, `AnimSpeed: 1.48`, `RangeWidthCells: 3.0`, `Knockback: 6.2`, `CanCrit: 0`
  - Hit 3 (`AttackC` Finisher): `Damage: 45`, `HitFrame: 9`, `AnimSpeed: 1.05`, `RangeWidthCells: 3.85`, `RangeHeightCells: 2.7`, `Knockback: 10.5`, `CanCrit: 1`

**Steps:**
- [x] **Step 1:** Read `craft-game-floors.md` and `ccgs-balance-check/SKILL.md`.
- [x] **Step 2:** Add EditMode tests to `Round9CombatPhysicsTests.cs`:
  - `T05_AttackState_PreservesLungeAcrossFixedTick_AndEmitsSwingEventOnlyOnceAcrossActiveWindow`
  - `T06_AttackState_ChainsHit3BackIntoHit1WhenBuffered_AndAppliesAirHitStall`
  - `T07_MovementConfigAndSwordAsset_MeetDeadCellsHackAndSlashTargets`
- [x] **Step 3:** Run `Round9CombatPhysicsTests` -> verify **RED**.
- [x] **Step 4:** Implement `PlayerCombat.cs` and `PlayerAttackState.cs`, tune `PlayerMovementConfig.asset` and `Weapon_StartSword.asset`, and update `Round8Phase2FeelMovementTests.Feel.cs` assertions (`L35-50`) to match the new Round 9 sword tuning.
- [x] **Step 5:** Re-run full `Core.Tests.EditMode` -> verify **GREEN**.
- [x] **Step 6:** Commit: `feat(combat,player): add persistent strike lunge, multi-frame active arc, 3-to-1 combo loop, air hit-stall`

---

### Task 1.4: Visceral Enemy Knockback Pop & Smooth Slide Recoil (`P0-05` Part B)

**Skills:** `craft-game-floors`, `ccgs-team-combat`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/Enemies/Scripts/States/EnemyStunState.cs`
- Modify: `Assets/_Project/Core/Tests/EditMode/Round9CombatPhysicsTests.cs`

**Design Interface:**
- `EnemyStunState.Enter()`:
  - Compute `float push = hit.Knockback * (1f - Ctx.Data.KnockbackResistance);`
  - When `hit.Direction != 0`:
    - `Ctx.Face(-hit.Direction);`
    - If `Ctx.IsGrounded && push >= 3.5f`: `float popY = Mathf.Max(Ctx.Velocity.y, hit.IsCritical ? 3.8f : 2.2f); Ctx.SetVelocity(new Vector2(hit.Direction * push, popY));`
    - Else: `Ctx.SetHorizontalVelocity(hit.Direction * push);`
- `EnemyStunState.FixedTick()`:
  - Lower horizontal recoil deceleration from `30f` → `18f * Time.fixedDeltaTime` so knockback carry reads crisply after hitstop ends.

**Steps:**
- [x] **Step 1:** Add EditMode test `T08_EnemyStunState_AppliesUpwardPopAnd18uSlideDamping` to `Round9CombatPhysicsTests.cs`.
- [x] **Step 2:** Run `Round9CombatPhysicsTests` -> verify **RED**.
- [x] **Step 3:** Update `EnemyStunState.cs`.
- [x] **Step 4:** Re-run full `Core.Tests.EditMode` -> verify **GREEN**.
- [x] **Step 5:** Commit: `feat(enemies): add upward knockback pop and smoother slide recoil in EnemyStunState`

> **Phase 1 Review Gate (`ccgs-code-review` + `verification-before-completion`):**
> Run full `Core.Tests.EditMode`:
> ```powershell
> rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -runTests -testPlatform EditMode -assemblyNames Core.Tests.EditMode -testResults "Logs/r9-p1-core.xml" -logFile "Logs/r9-p1-core.log"
> ```
> Must pass `100%` (`0` failed, `0` skipped, `0` compiler errors/warnings in modified files) before Phase 2 starts.

---

## Phase 2 — Level Architecture: 45° `Stairs_H`, Trimmed Chains, Purposeful Platforms, Room N/T Payoffs & Core Spine Packs (`P0-01`, `P0-02` Geometry, `P0-04`, `P0-05b`)

> **Phase 2 Mandatory Skills (read before editing):**
> - Unity Plugin: `create-tile-palette-or-rule-tiles`, `create-2d-physics`, `unity-cli`
> - CCGS: `ccgs-team-level`, `ccgs-balance-check`, `ccgs-consistency-check`
> - Superpowers: `test-driven-development`, `verification-before-completion`

### Task 2.1: Rebuild `Stairs_H` as True 45° Dead Cells Staircase + Fitted Polygon `Caster_StairsH` (`P0-01`)

**Skills:** `create-tile-palette-or-rule-tiles`, `create-2d-physics`, `ccgs-team-level`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`BuildRoomHJK`, `PaintPrisonStairsH`, `BuildRound5SceneAtmosphereAndLighting`, add `CreatePolygonShadowCaster`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/HJKGeometryTests.cs` (`A04`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapGeometryTests.cs` (`A06/T3`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A14`, `A18`)

**Design Interface:**
- **45° Stair Collision (`Stairs_H` in `BuildRoomHJK`):**
  - Replace the 12-vertex blocky staircase polyline with a clean 6-vertex continuous 45° ramp rising `8u` over `8u` (`(18, 8) → (10, 16)`), flanked by flat bottom landing `[18..22]@y=8` and flat top landing `[6..10]@y=16`:
    `{ (6, 7), (22, 7), (22, 8), (18, 8), (10, 16), (6, 16) }`
  - Assign zero-friction `PhysicsMaterial2D` (same material referenced by Player or zero-friction material if needed) so slope transition from `Floor_D` (`y=8`) onto the 45° ramp and up onto `Floor_KJ` (`y=16`) is completely seamless.
- **45° Stair Visuals (`PaintPrisonStairsH`):**
  - Flat bottom landing `[18..22]@y=8` (base `y=7`): `PaintPrisonSolidFloor(..., 18f, 22f, 7f, 8f)` (`floorStamp_*` at `y=7`, `ground_*` at `y=8`).
  - Flat top landing `[6..10]@y=16` (base `y=7`): `PaintPrisonSolidFloor(..., 6f, 10f, 7f, 16f)` (`floorStamp_*` at `y=15`, `dirt_00` at `y=7..14`, `ground_*` at `y=16`).
  - Diagonal 45° run `x = 10..17` (8 single cells along `yTop = 25 - x`):
    - Fill solid stone core `TM_SolidVisual` with `dirt_00` for `cy = 7 .. (yTop - 1)` (preserving `(10, 11)` as `dirt_00` for `A10:474`, and keeping `(15, 11)` & `(12, 14)` as open air!).
    - Paint **EXACTLY ONE clean diagonal row of 8 flipped 45° stair tiles** (`m00 = -1f`) on `TM_SolidRim` at `(17, 8), (16, 9), (15, 10), (14, 11), (13, 12), (12, 13), (11, 14), (10, 15)` alternating `pal.Stairs0` / `pal.Stairs1`. **ZERO double-stacked tiles directly below any diagonal stair cell!**
  - Keep `Art_H_StairCoreStone_0..3` stone stamps strictly inside solid `dirt_00` core cells `(x, y)` satisfying `y <= 24 - x`.
- **Fitted Polygon `Caster_StairsH` (`Builder:4874`):**
  - Add `CreatePolygonShadowCaster(Transform parent, string name, Vector3[] worldVertices, int[] targetSortingLayerIds)` computing local center + `m_ShapePath` + `m_ShapePathHash`.
  - Replace the floating `6×6u` box `Caster_StairsH` with a fitted 6-vertex polygon matching the exact stone mass `{ (6,7,0), (22,7,0), (22,8,0), (18,8,0), (10,16,0), (6,16,0) }` -> **ZERO floating black shadow box in mid-air!**
- **Declutter Stair Landings & Reposition Stair Torches (`BuildRoomHJK:742-754`):**
  - **Delete** `Art_H_StepJunkMid` (was sitting on open slope!).
  - Move `Art_H_LandingJunk` to `x = 20.4f, y = 8f` (clear of `Art_H_TorchLanding`), move `Art_H_StepJunkTop` to `x = 6.8f, y = 16f` (cleanly on flat top landing `[6..10]@16`, clear of `Art_H_TorchTop`).
  - Reposition the 3 Room H wall lanterns cleanly along the backwall ABOVE the 45° diagonal slope (so no lantern sits inside stone steps!):
    - `Art_H_TorchLanding`: `x = 18.8f, floorY = 8f` (`flameY = 10.2f`, `1.8u` clear of `LandingJunk`)
    - `Art_H_TorchMid`: `x = 13.8f, floorY = 12.5f` (`flameY = 14.7f`, wall-mounted cleanly `2.5u` above ramp surface `y=12.2`!)
    - `Art_H_TorchTop`: `x = 8.4f, floorY = 16f` (`flameY = 18.2f`, cleanly on flat top landing `[6..10]@16`!)

**Steps:**
- [x] **Step 1:** Read `create-tile-palette-or-rule-tiles.md` and `HJKGeometryTests.cs`.
- [x] **Step 2:** Update tests first:
  - `HJKGeometryTests.A04`: assert 6-vertex 45° `Stairs_H` polyline (`[18..22]@8`, diagonal `(18,8)→(10,16)`, `[6..10]@16`, bottom `y=7`).
  - `MapGeometryTests.A06/T3`: update `AssertFloorUnionCovers(..., "A06/T3", 16f, -102f, 10f)` (was `9f`).
  - `MapWiringTests.A14:1115`: update `roomHStairAirCells` from `(15,10)/(12,12)` to open-air cells above 45° ramp `(15,11)/(12,14)`.
  - `MapWiringTests.A18`: assert 8 single-row diagonal flipped stair tiles at `(17,8)..(10,15)` with ZERO stair tile directly below any of them, flat landing caps at `(18,7)` & `(7,15)`, remove deleted `"Art_H_StepJunkMid"` from `newJunkClusters`, and assert every vertex of `Caster_StairsH` satisfies `y <= 26f - x + 0.05f` (zero floating shadow box above the 45° slope!).
- [x] **Step 3:** Implement `BuildRoomHJK`, `PaintPrisonStairsH`, and `CreatePolygonShadowCaster` in `NhaGiamGiengChuongBuilder.cs`.
- [x] **Step 4:** Run `BuildAll`, then run `HJKGeometryTests`, `MapGeometryTests`, `A14`, `A18` -> verify **GREEN**.
- [x] **Step 5:** Commit: `fix(nggc): rebuild Stairs_H as 45-degree Dead Cells ramp with fitted polygon ShadowCaster`

---

### Task 2.2: Trim All 7 Chain Triggers + Flush `ladderHole` + Relocate `Art_G_Winch` + Prune 6 Nonsense `OneWay_*` Platforms (`P0-02` Geometry & `P0-04` Part A)

**Skills:** `create-2d-physics`, `ccgs-team-level`, `ccgs-consistency-check`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/ABGeometryTests.cs` (`A02`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/CDGeometryTests.cs` (`A01`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/GGeometryTests.cs` (`A03`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/HJKGeometryTests.cs` (`A04`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/WingGeometryTests.cs` (`A05b..A05e`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapGeometryTests.cs` (`A06`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A10`, `A11`, `A14`, `A15`)

**Design Interface:**
1. **All 7 `Climbable` Triggers Trimmed to `[floorY - 0.20, deckTopY + 0.08]` (eliminates `+0.50u` air overhang!):**
   - `I1_Chain_C`: `x=46`, `y=[-0.20..8.08]` -> `center=(46, 3.94)`, `size=(0.5, 8.28)`
   - `I2_Chain_G`: `x=68`, `y=[-0.20..8.08]` -> `center=(68, 3.94)`, `size=(0.5, 8.28)`
   - `I6_Chain_K`: `x=-24`, `y=[15.80..24.08]` -> `center=(-24, 19.94)`, `size=(0.5, 8.28)`
   - `I7_Chain_MN`: `x=104`, `y=[-0.20..8.08]` -> `center=(104, 3.94)`, `size=(0.5, 8.28)`
   - `I8_Chain_MO`: `x=118`, `y=[-16.20..0.08]` -> `center=(118, -8.06)`, `size=(0.5, 16.28)`
   - `I9_Chain_QR`: `x=-66`, `y=[15.80..24.08]` -> `center=(-66, 19.94)`, `size=(0.5, 8.28)`
   - `I10_Chain_ST`: `x=80`, `y=[23.80..40.08]` -> `center=(80, 31.94)`, `size=(0.5, 16.28)`
2. **Flush `ladderHole_00` Socket (`AddPrisonChainArt` `Builder:4001-4009`):**
   - Set `AnchorTop` position to `new Vector3(x, deckTopY - 0.50f, 0f)` (`bounds.max.y == deckTopY`, flush underside iron collar directly beneath the wooden deck, `sortingLayer = Ground`, `sortingOrder = -3` behind planks at `-2`) -> **ZERO black horns poking up above bridges!**
3. **Relocate `Art_G_Winch` (`Builder:632`) — Verified Clear of BOTH `I2_Chain_G` AND `Art_G_Alcove`:**
   - Move from `(68, 8)` (blocking `I2_Chain_G`!) to **`(73.6, 8)`** on solid `Floor_G_UpperEast` (`[72..76]@8`) -> `5.6u` clear of `I2_Chain_G (68)`, `1.8u` clear of `Art_G_TorchUpperEast (71.8)`, `8.7u` clear of `Art_G_Alcove ([56..65])`!
4. **Remove 6 Zero-Purpose `OneWay_*` Platforms (`31 → 25` total):**
   - `OneWay_A_Practice` (`[9..14]@2.0`) + `Art_A_PracticePlanks` (also frees Bay #0 completely!).
   - `OneWay_C_Rest` (`[44..49]@4.5`) + `Art_C_RestPlanks` (clean Bell-Well shaft!).
   - `OneWay_N_High` (`[136..150]@16.5`) + `Art_N_HighPlanks`.
   - `OneWay_O_West` (`[84..96]@-11.5`) + `Art_O_WestPlanks`.
   - `OneWay_S_Gallery` (`[44..58]@28.5`) + `Art_S_GalleryPlanks` + `Art_S_GalleryBraceL/R` (frees Room S for native-scale `AlchemyBibli1/2`!).
   - `OneWay_T_High` (`[112..126]@49.0`) + `Art_T_HighPlanks` + `Art_T_HighBraceL/R` (frees East Room T for `I17_Scroll` & Promenade Exit climax!).
5. **Give `OneWay_E_Combat` (`[-4..2]@26.0`) Genuine Tactical Purpose (Preserves `P02` & `P05`):**
   - Keep `OneWay_E_Combat` at `[-4..2]@26.0` so `ELFGeometryTests.A05`, `P02`, and `P05` stay 100% green, and pair it with `Enemy_Rat_E2` below it (Task 2.4) so vaulting onto `OneWay_E_Combat` dodges the ground rush while exposing the player to `Enemy_Archer_E`'s horizontal firing lane!

**Steps:**
- [x] **Step 1:** Update geometry & wiring tests:
  - `ABGeometryTests.A02`: remove `OneWay_A_Practice` assertions; assert zero `OneWay_*` in `AB`.
  - `CDGeometryTests.A01`: remove `OneWay_C_Rest`; update `I1` to `(-0.20f, 8.08f)`.
  - `GGeometryTests.A03`: update `I2` to `(-0.20f, 8.08f)`.
  - `HJKGeometryTests.A04`: update `I6` to `(15.80f, 24.08f)`.
  - `WingGeometryTests.A05b..A05e`: remove `OneWay_N_High`, `OneWay_O_West`, `OneWay_S_Gallery`, `OneWay_T_High`; update `I7`, `I8`, `I9`, `I10` bounds to `[floorY - 0.20, deckTopY + 0.08]`.
  - `MapGeometryTests.A06`: update all 7 `AssertClimbableTrigger` bounds.
  - `MapWiringTests`: `A10` `plankRowNames` (`31 → 25`), `A11` `PlatformEffector2D` count (`31 → 25`), `A14` `oneWayBoxes` count (`31 → 25`), `A15` remove `"Art_S_GalleryBraceL"` & `"Art_T_HighBraceL"` from `requiredWingStorytellingProps`.
- [x] **Step 2:** Apply the edits in `NhaGiamGiengChuongBuilder.cs`.
- [x] **Step 3:** Run `BuildAll`, then run `NGGC Unit`, `Wiring1`, `Wiring2` -> verify **GREEN**.
- [x] **Step 4:** Commit: `fix(nggc): trim 7 chain triggers, flush ladderHole collars, move Art_G_Winch to Floor_G_UpperEast, prune 6 nonsense platforms`

---

### Task 2.3: Eliminate Dead-End Wings — Turn Room N & Room T into High-Value Dead Cells Payoffs (`P0-04` Part B)

**Skills:** `ccgs-team-level`, `ccgs-balance-check`, `ccgs-consistency-check`, `test-driven-development`, `unity-cli`

**Files:**
- Modify: `Assets/_Project/Features/Currency/Scripts/GoldPickup.cs` (add `[SerializeField]` static scene pickup support!)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`BuildScene`, `BuildRoomW1_EastCourtyard`, `BuildRoomU1_UpperPromenade`, `CloneMainLevelScrollsAndGoldEconomy`, `CreateLoreTablet`, `CreateSecondaryBiomeExitTrigger`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A07`, `A08`, `A13`, `A14`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/ExpansionPlayTests.cs` (`P08`: disambiguate `I16_LoreTablet`, `I15_SewerExitTrigger`, and dropped `GoldPickup`!)

**Design Interface:**
1. **Static Scene `GoldPickup` Support (`GoldPickup.cs`):**
   - Mark `_wallet`, `_player`, `_value` with `[SerializeField]` and add `[SerializeField] private bool _staticPlacement;` (default `false` so `EnemyGoldDrops.Launch(...)` behaves 100% identically!).
   - In `Awake()`: if `_staticPlacement && _body != null`: set `_body.bodyType = RigidbodyType2D.Kinematic; _body.linearVelocity = Vector2.zero;` so placed vault coins rest cleanly at their authored position until the player enters magnet range and collects them into `_wallet`!
2. **Room T (`102×22.5u` Upper Promenade) — Live Biome Exit + 3rd Power Scroll (Zero Prop Overlap!):**
   - Convert `Art_T_CourtyardArch` at `(126, 40)` into a live **`I18_PromenadeExitTrigger`** (`SecondaryBiomeExitTrigger`, `biomeName = "Promenade of the Condemned"`, `_switchDoor = null`, guardians = `Enemy_Shield_T1` & `Enemy_Grenadier_T1`, center `(126, 41.2)`, size `(2.2, 2.4)`), plus a warm golden **`Art_T_ExitGlow`** (`PortalBackOut_0_00`, `Mat_Emissive_Exit.mat`, `Ground / -4` in front of `Art_T_CourtyardArch` at `Background/-13`, + Point `Light2D` matching `Art_P_ExitGlow`!).
   - Update `CreateSecondaryBiomeExitTrigger` locked badge text: when `switchDoor == null`, display `"Khóa: Cần diệt 2 quái canh cổng Promenade"`.
   - Clone a **3rd `PowerScrollPickup` (`I17_Scroll_HighBastion`)** at **`(114.0, 40.0)`** (`ScrollAura_R5_2` under `_icon`), register all 3 scrolls in `PowerScrollInteraction._scrolls` (`arraySize = 3`), and **shift `Art_T_Junk3`** from `114.0` → **`102.0`** and **`Art_T_JunkEast3`** from `128.0` → **`136.5`** so BOTH `I17_Scroll_HighBastion` (`114.0`) and `I18_PromenadeExitTrigger` (`126.0`) have `>= 5.0u` clear breathing room from `TP_5_HighBastion (132.0)` and all floor junk!
3. **Room N (`88×14.5u` North Gallery) — Narrative Lore + 4-Coin Treasure Cache (Zero Prop Overlap!):**
   - Parameterize `CreateLoreTablet(..., string title, string body, string promptLabel)` cleanly.
   - Add **2nd `LoreTablet` (`I19_LoreTablet_NorthGallery`)** at **`(152.5, 8.0)`** (`"Nhật Ký Cai Ngục"`, explaining the Bell-Well uprising and pointing to the Promenade exit above Room S/T; prompt `"[E] Đọc nhật ký Cai Ngục"`).
   - Add **4 static `GoldPickup` treasure coins (`GoldCache_N_0..3`)** at `x = 158.0, 159.0, 160.0, 161.0`, `y = 8.45` (`value = 25` each -> `100` gold total, wired to `goldWallet` + `playerHealth`, `_staticPlacement = true`, `VFX` sorting layer!).
   - **Relocate overlapping Room N props:** shift `Art_N_JunkEast2` from `150.5` → **`145.0`** and `Art_N_JunkEast3` from `158.5` → **`139.0`** so `Art_N_Alcove2 (148.0)`, `I19_LoreTablet (152.5)`, `Art_N_Torch_4 (156.0)`, and `GoldCache_N_0..3 (158..161)` have zero overlap!

**Steps:**
- [x] **Step 1:** Update `MapWiringTests.A13` (expect `3` `PowerScrollPickup`s including `I17_Scroll_HighBastion`, `2` `SecondaryBiomeExitTrigger`s [`I15_SewerExitTrigger` & `I18_PromenadeExitTrigger`], `2` `LoreTablet`s [`I16_LoreTablet` & `I19_LoreTablet_NorthGallery`], and `4` static `GoldCache_N_*` `GoldPickup`s wired to `GoldWallet`), and update `ExpansionPlayTests.P08` to find `"I16_LoreTablet"` and `"I15_SewerExitTrigger"` by name and assert dropped `GoldPickup` near `(92, 3.5)`.
- [x] **Step 2:** Update `GoldPickup.cs` and `NhaGiamGiengChuongBuilder.cs`.
- [x] **Step 3:** Run `BuildAll`, then run `Wiring1` + `Wiring2` + `ExpansionPlayTests` -> verify **GREEN**.
- [x] **Step 4:** Commit: `feat(currency,nggc): add Promenade secondary biome exit, High Bastion PowerScroll, and Room N lore/gold payoff`

---

### Task 2.4: Enrich Core Spine Combat Pacing (`5 → 9` Core Spine Enemies, `30 → 34` Total, `11 → 12` Zones) (`P0-05b`)

**Skills:** `ccgs-team-combat`, `ccgs-team-level`, `ccgs-balance-check`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`BuildScene` enemy spawns, zones, `allEnemies` -> auto-wires `EnemyGoldDrops` + `EnemyImpactFeedback`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A07`, `A13`, `A17`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/RunPlayTests.cs` (`P07`: `30 → 34` enemies, `11 → 12` zones)

**Design Interface (Dead Cells Encounter Pacing):**
- Add **4 Core Spine enemies** (preserving all existing spawn points & `I4_EncounterGate` contract 100%!):
  1. `Enemy_Rat_B2` at `(35f, 0f)` in **Zone B1** (flanks `Enemy_Zombie_B` at `(29, 0)` — immediate multi-target cleave & lunge pack!).
  2. `Enemy_Zombie_D1` at `(31f, 8f)` in new **Zone D1** (`min=(23, 8), max=(41, 13.5)`) (pressures Room D corridor right after climbing `I1`).
  3. `Enemy_Rat_J2` at `(-10f, 16f)` in **Zone J1** (paired with `Enemy_Shield_J` at `(-4, 16)` — classic Dead Cells shield + flanker pack!).
  4. `Enemy_Rat_E2` at `(0f, 24f)` in **Zone E1** (directly beneath `OneWay_E_Combat [-4..2]@26.0`, giving `OneWay_E_Combat` genuine tactical combat value while **`I4_EncounterGate` remains bound to `Enemy_Zombie_E` & `Enemy_Archer_E`** so `P04`/`P05`/`P06`/`P07` gate rules stay 100% preserved!).
- All 4 new enemies automatically join `allEnemies` (`34/34` `EnemyImpactFeedback`, `34/34` `EnemyGoldDrops`, dormant boot, serialized `_player`/`_projectiles`/`_vfx`).

**Steps:**
- [x] **Step 1:** Update `MapWiringTests` (`A07`, `A13`, `A17`: `34` enemies, `12` zones including `D1`) and `RunPlayTests.P07` (`34` enemies, `12` zones).
- [x] **Step 2:** Add the 4 Spine enemies + Zone `D1` in `NhaGiamGiengChuongBuilder.cs`.
- [x] **Step 3:** Run `BuildAll`, then run `Wiring1` + `Wiring2` + `PlayMode` (`P01..P08`) -> verify **GREEN**.
- [x] **Step 4:** Commit: `feat(nggc): add 4 Core Spine combat pack enemies (30->34) and Zone D1 for Dead Cells pacing`

> **Phase 2 Review Gate (`ccgs-consistency-check` + `ccgs-gate-check`):** Run `BuildAll`, then `NGGC Unit`, `Wiring1`, `Wiring2`, and `PlayMode` (`P01..P08`). All tests must pass `100%` before Phase 3 begins.

---

## Phase 3 — Visual & Atmosphere Overhaul: Gothic Weathered Windows, Silky Visible Fog, Zero Overlaps, Grounded Architecture, True 24-PPU Cohesion & Readability (`P1-01..P1-08`)

> **Phase 3 Mandatory Skills (read before editing):**
> - Unity Plugin: `2d-pixel-perfect`, `sprite-editor`, `urp-postprocessing`, `craft-game-floors`, `unity-cli`
> - CCGS: `ccgs-team-polish`, `ccgs-asset-audit`, `ccgs-perf-profile`
> - Superpowers: `test-driven-development`, `verification-before-completion`

### Task 3.1: Organic Carved Gothic Outdoor Windows — Eliminate Pasted-Sticker Look & Black Corner Ears (`P1-07`)

**Skills:** `2d-pixel-perfect`, `sprite-editor`, `ccgs-team-polish`, `ccgs-asset-audit`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs` (upgrade `prisonArchSill_00` & `prisonArchJamb_00` IN-PLACE keeping `24×8`/`8×24` 24-PPU for `MainLevelIntegrityTests`, add `prisonArchSill_01/02`, `prisonArchJamb_01/02`, `prisonArchSpandrel_00` `24×24`, `prisonArchMullion_00` `8×24`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`PrisonPalette`, `CarveAndFrameArchBay`)
- Modify: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A15` arch corner assertions)

**Design Interface (Carved Castle Stone Arch Opening — Zero Sticker Outline, Zero Black Ears):**
1. **Weathered 24-PPU Masonry Variants (`PrisonPropSpriteGenerator.cs`):**
   - **Backward-compat lock:** Keep `prisonArchSill_00.png` (`24×8`, 24 PPU) and `prisonArchJamb_00.png` (`8×24`, 24 PPU) at exact paths/dimensions so `MainLevelIntegrityTests` stays 100% green, while upgrading their pixel art IN-PLACE to match the dark moss-slate `backWall_*` palette (`#1A2322 / #26332F / #35453E / #485A50`) with **chipped/stepped outer edges (transparent/blended notches along the wall-facing side — ZERO continuous harsh black sticker rectangle border!)** and subtle darkened inner reveal bevel facing the sky!
   - Add 2 weathered masonry variants each:
     - `prisonArchSill_01.png` (`24×8`, mossy chipped blocks) & `prisonArchSill_02.png` (`24×8`, cracked stepped blocks).
     - `prisonArchJamb_01.png` (`8×24`, mossy chipped courses) & `prisonArchJamb_02.png` (`8×24`, broken stepped courses).
   - Add **`prisonArchSpandrel_00.png` (`24×24`, 24 PPU)**: curved **Gothic stone arch top-corner spandrel/voussoir** (solid weathered moss-slate castle stone in upper-left triangle matching `backWall_*`, curving smoothly into a recessed dark-shadow arch intrados facing the opening — transparent inside the curved arch void!). Replaces the giant pitch-black `LittleBeamTL/TR` bat-ears!
   - Add **`prisonArchMullion_00.png` (`8×24`, 24 PPU)**: slender 3px-wide oxidized dark wrought-iron vertical gothic window mullion bar (`#141B20 / #27333C / #3E4F5A`) with riveted cross-collar every 12px.
   - Auto-generate Sobel normal maps (`_n.png`) and bind `_NormalMap` secondary texture for every single generated sprite via `PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(...)`.
2. **Depth Layering, Curved Arch Framing & Organic Per-Bay Dressing (`CarveAndFrameArchBay` `Builder:3590-3645`):**
   - Keep `isTopCorner` solid `backWall` cells carved cleanly, and place **`ArchSpandrel_TL_{bayIndex}`** (`prisonArchSpandrel_00` at `(xMin + 0.5f, yMax - 0.5f)`, `Background / -16`) & **`ArchSpandrel_TR_{bayIndex}`** (flipped X at `(xMax - 0.5f, yMax - 0.5f)`, `Background / -16`) — **completely removing `LittleBeamTL/TR` black ears!**
   - **Trim lintel & jamb spans so they NEVER double-draw over corners:**
     - `ArchLintel_{bayIndex}_{sx}` spans `sx = xMin + 1 .. xMax - 2` (between the two arch spandrels!).
     - `ArchJamb_L/R_{bayIndex}_{sy}` spans `sy = yMin .. yMax - 2` (cleanly meeting the bottom of `ArchSpandrel_TL/TR`!).
     - `ArchSill_{bayIndex}_{sx}` spans `sx = xMin .. xMax - 1` cleanly along the sill.
     - Select variants deterministically per `(bayIndex + sx/sy) % 3` across `00/01/02` (with alternating `flipX`/`flipY` on stone strips) so zero adjacent bays repeat identical pixel strips!
   - **Wrought-Iron Gothic Mullions:** Inside every bay (`width = xMax - xMin`), place `1` vertical mullion column (if `width <= 5`) or `2` evenly spaced columns (if `width >= 6`) using `prisonArchMullion_00` (`Background / -14`, `y = yMin .. yMax - 1`, leaving 1 weathered broken segment on `bayIndex % 4 == 2`) so exterior parallax castles sit naturally *behind* real prison window bars!
   - **Varied Per-Bay Organic Dressing (No More 16 Cloned Cobweb Pairs!):**
     - `bayIndex % 3 == 0`: single asymmetric cobweb (`WebSmallTL` OR `WebSmallTR` alternating, soft alpha `0.48f`).
     - `bayIndex % 3 == 1`: subtle hanging exterior/reveal vine (`VineParallaxA/B` with `Mat_Vine_Sway`, tinted muted moss `(0.52, 0.66, 0.58, 0.78)` on `Background / -15`, clipped neatly within bay height).
     - `bayIndex % 3 == 2`: stark weathered bare masonry (no cobweb clutter!).
   - **Cool Exterior Moonlight Rim Bleed:** Place 1 subtle `Point Light2D` (`ArchMoonSpill_{bayIndex}`, `#74B4CC`, `intensity = 0.32f`, `innerRadius = 0.8f`, `outerRadius = 5.4f`, `falloffIntensity = 0.72f`, `enableVolumetric = false`, strictly gameplay sorting layers via `ConfigureGameplayPointLight`) centered near the sill/opening `(midX, yMin + 1.2f)` so cool night sky light gently kisses the interior stone bricks around every outdoor window!

**Steps:**
- [x] **Step 1:** Read `2d-pixel-perfect.md`, `sprite-editor.md`, and `PrisonPropSpriteGenerator.cs`.
- [x] **Step 2:** Extend `VisualShaderAndNormalTests.cs` (`PrisonGeneratedProps_Are24PpuPointUncompressedWithBoundNormalMaps`) to verify all 7 new/updated window sprites (`prisonArchSill_00..02`, `prisonArchJamb_00..02`, `prisonArchSpandrel_00`, `prisonArchMullion_00`) at 24 PPU Point filter with bound `_NormalMap`, AND assert `prisonArchSill_00..02` / `prisonArchJamb_00..02` outer wall-facing rows contain organic transparent/stepped notches (not a solid opaque dark sticker bar) and `prisonArchSpandrel_00` has a transparent curved arch cutout!
- [x] **Step 3:** Update `MapWiringTests.A15:1280-1283` to assert `ArchSpandrel_TL_{0..15}` & `ArchSpandrel_TR_{0..15}` (and zero `LittleBeamTL/TR` on bays!).
- [x] **Step 4:** Run `VisualShaderAndNormalTests` -> verify **RED**.
- [x] **Step 5:** Implement `PrisonPropSpriteGenerator.cs` and `CarveAndFrameArchBay` in `NhaGiamGiengChuongBuilder.cs`.
- [x] **Step 6:** Run `BuildAll` (which runs `PrisonPropSpriteGenerator.EnsureAll()` automatically), then run `Core.Tests.EditMode` (`VisualShaderAndNormalTests` + `MainLevelIntegrityTests`) and `Wiring1`/`Wiring2` (`A12`, `A15`) -> verify **GREEN**.
- [x] **Step 7:** Commit: `feat(art,nggc): replace sticker windows with curved stone spandrels, weathered variants, iron mullions, and moonlight spill`

---

### Task 3.2: Smooth AND Unmistakably Visible Dungeon Fog (`P1-08`)

**Skills:** `urp-postprocessing`, `craft-game-floors`, `ccgs-team-polish`, `ccgs-perf-profile`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Art/Shaders/GroundMist2D.shader`
- Modify: `Assets/_Project/Tools/Editor/EnvironmentMaterialBuilder.cs` (`EnsureGroundMistMaterial`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`BuildRound5SceneAtmosphereAndLighting` `GroundMist_R5`)
- Modify: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A11`, `A18`)

**Design Interface (Silky Dual-Layer FBM Mist — Target `0.26..0.44` Core Pixel Alpha, 0 Sharp Edges):**
- **`GroundMist2D.shader` (exact HLSL variable names `input.uv`, `input.worldXY`, `input.color`, preserving all 4 Properties `_MainTex`, `_MistColor`, `_ScrollSpeed`, `_NoiseScale`):**
  - Dual counter-drifting 3-octave quintic FBM layers (`Blend SrcAlpha OneMinusSrcAlpha`, `TransformObjectToWorld`):
    ```hlsl
    float2 pA = input.worldXY * float2(0.18, 0.55) * (_NoiseScale * 0.28);
    float2 pB = input.worldXY * float2(0.31, 0.88) * (_NoiseScale * 0.28);
    float t = _Time.y * _ScrollSpeed * 4.0;
    float fbmA = ValueNoise2D(pA + float2(t, 0.0)) * 0.52
               + ValueNoise2D(pA * 2.03 + float2(-t * 0.70, 1.73)) * 0.33
               + ValueNoise2D(pA * 4.17 + float2(t * 0.42, 3.41)) * 0.15;
    float fbmB = ValueNoise2D(pB + float2(-t * 0.62, t * 0.14 + 4.11)) * 0.55
               + ValueNoise2D(pB * 2.11 + float2(t * 0.38, -t * 0.09 + 2.37)) * 0.30
               + ValueNoise2D(pB * 4.05 + float2(-t * 0.25, 5.19)) * 0.15;
    half fbm = (half)(fbmA * 0.62 + fbmB * 0.38);
    half density = lerp(0.45h, 1.0h, smoothstep(0.18h, 0.82h, fbm));

    half vBottom = smoothstep(0.0h, 0.16h, (half)input.uv.y);
    half vTop = 1.0h - smoothstep(0.24h, 1.0h, (half)input.uv.y);
    half vFade = vBottom * pow(max(vTop, 0.0001h), 1.25h);
    half hFade = smoothstep(0.0h, 0.08h, (half)input.uv.x) * (1.0h - smoothstep(0.92h, 1.0h, (half)input.uv.x));

    half4 col = _MistColor * input.color;
    col.a = saturate(col.a * density * vFade * hFade * tex.a);
    return col;
    ```
- **`EnvironmentMaterialBuilder.cs:175-183`:**
  - `Mat_GroundMist_Prison.mat`: `_MistColor = new Color(0.52f, 0.80f, 0.76f, 0.62f)`, `_ScrollSpeed = 0.04f`, `_NoiseScale = 3.5f`.
- **`NhaGiamGiengChuongBuilder.cs` (`GroundMist_R5` `L4853-4869`):**
  - Raise `mistTint = new Color(0.84f, 0.90f, 0.92f, 0.86f)`.
  - Expand from `6 → 9` damp-floor strips at `2.8u` height (`pos.y = floorY + 1.4f`, `scale.y = 2.8f * 24f`):
    1. `Mist_RoomAB` `(21f, 1.4f)` width `38f` (visible right at Spawn/Room A/B!)
    2. `Mist_RoomC` `(49f, 1.4f)` width `14f`
    3. `Mist_RoomG` `(66f, 1.4f)` width `20f`
    4. `Mist_RoomK` `(-23f, 17.4f)` width `14f`
    5. `Mist_RoomE` `(3f, 25.4f)` width `36f`
    6. `Mist_RoomO` `(120f, -14.6f)` width `88f`
    7. `Mist_RoomP` `(198f, -14.6f)` width `70f`
    8. `Mist_RoomQ` `(-66f, 17.4f)` width `72f`
    9. `Mist_RoomT` `(89f, 41.4f)` width `96f`

**Steps:**
- [x] **Step 1:** Read `urp-postprocessing.md` and `GroundMist2D.shader`.
- [x] **Step 2:** Extend `VisualShaderAndNormalTests.cs` (`_MistColor.a >= 0.55f`, dual-layer `fbmA`/`fbmB` + `lerp` + `hFade` in `GroundMist2D.shader`), and update `MapWiringTests.A11:838` + `A18:1810` (`6 → 9` strips, vertex `color.a >= 0.80f`).
- [x] **Step 3:** Run `VisualShaderAndNormalTests` -> verify **RED**.
- [x] **Step 4:** Update `GroundMist2D.shader`, `EnvironmentMaterialBuilder.cs`, and `NhaGiamGiengChuongBuilder.cs`; run `BuildAll`.
- [x] **Step 5:** Re-run `Core.Tests.EditMode` + `Wiring1`/`Wiring2` (`A11`, `A18`) -> verify **GREEN**.
- [x] **Step 6:** Commit: `fix(shader,vfx): upgrade GroundMist2D to visible silky dual-layer FBM across 9 dungeon mist strips`

---

### Task 3.3: Eliminate All Floor Junk Self-Overlaps & Landmark / POI / Torch Collisions (`P1-01`)

**Skills:** `2d-pixel-perfect`, `ccgs-asset-audit`, `ccgs-team-polish`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`AddFloorJunkCluster` + offending call sites across Rooms H, M, N, O, S, T)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (add `A20_Round9_ZeroOverlapsGroundedPilastersAnchoredChainedAndMixelDiscipline`)

**Design Interface:**
- **Width-Aware `AddFloorJunkCluster` (`Builder:4565-4572`):**
  - Replace hardcoded `+ 0.65f` with exact sprite-width-derived spacing:
    ```csharp
    const float scaleA = 0.90f;
    const float scaleB = 0.85f;
    float halfA = spA.bounds.extents.x * scaleA;
    float halfB = spB.bounds.extents.x * scaleB;
    float offsetX = halfA + halfB + 0.16f;
    ```
  - Also lower bright barrel/crate specular pop via cohesive dungeon dust tint `new Color(0.70f, 0.74f, 0.76f, 1f)` so background floor junk never visually outshines enemies or the Player!
- **Explicit Call-Site Clearances (Zero Overlap With Other Junk, Landmarks, POIs, or Torches):**
  - `Art_O_JunkEast4`: shift from `130.5` (dx=0 on `Art_O_MonsterDoor`!) → **`136.5`** (clear of `Art_O_MonsterDoor [125..136]` and `Art_O_Torch_3` shifted to `139.0`? Verify exact X via `A20`!).
  - `Art_O_Junk1` (`95.0`) vs `Art_O_JunkMid1` (`96.0`): separate to `93.2` and `100.5` (`Art_O_JunkMid3` -> `105.5`).
  - `Art_O_JunkEast2` (`146.5`) vs `Art_O_Junk2` (`148.0`): separate to `143.5` and `149.0`.
  - `Art_S_JunkEast2`: shift from `86.5` (was overlapping `Art_S_AlchemyBibli2`) → **`85.0`** (cleanly in Room S east bay `[83..92]`, clear of `Art_S_AlchemyBibli2` relocated to `62.5` in Task 3.5!).
  - Audit and offset EVERY cluster clashing with a `LanternFixture_R5`, `TP_*`, `I*_Scroll_*`, `I16/I19_LoreTablet`, `SD_*`, `I3_HealingChest`, or architectural landmark until **`A20` reports ZERO overlaps across the entire 340u × 86u map**!

**Steps:**
- [x] **Step 1:** Read `ccgs-asset-audit/SKILL.md`.
- [x] **Step 2:** Create `A20_Round9_ZeroOverlapsGroundedPilastersAnchoredChainedAndMixelDiscipline` in `MapWiringTests.cs` (starting with the zero-overlap audit):
  - Every `*_Junk*_A` / `*_Junk*_B` pair: `overlapX <= 0.02f` (0 self-overlaps!).
  - Every distinct floor junk pair across the entire scene (`junk_i` vs `junk_j`): zero AABB overlap (`overlapX && overlapY == false` with `-0.04f` tolerance).
  - Zero floor junk overlaps any landmark (`Art_*Door*`, `Art_*Bibli*`, `Art_*Alcove*`, `Art_*Arch*`, `Art_*Fountain*`, `Art_*Pillar*`, `Art_*Dummy*`, `Art_*JudasSeat*`, `Art_*Billot*`), interactive POI (`TP_*`, `I*_Scroll_*`, `I3_HealingChest`, `I16/I19_LoreTablet`, `SD_*`), or `LanternFixture_R5`.
- [x] **Step 3:** Run `Wiring2` (`A20`) -> verify **RED** (enumerates every existing overlap!).
- [x] **Step 4:** Rewrite `AddFloorJunkCluster` and adjust every flagged call site in `NhaGiamGiengChuongBuilder.cs`; run `BuildAll`.
- [x] **Step 5:** Re-run `Wiring2` (`A14..A20`) -> verify **GREEN**.
- [x] **Step 6:** Commit: `fix(nggc): compute width-aware floor junk spacing and clear all landmark, POI, and torch overlaps`

---

### Task 3.4: Ground 7 Tall Pilasters to Ceiling, Remove Mid-Wall Black Blobs, Anchor 8 `ChainedPrisoners`, Remove Floating Doorway Webs & Clean `Foreground_Silhouettes` (`P1-02`, `P1-03`)

**Skills:** `2d-pixel-perfect`, `ccgs-team-polish`, `ccgs-asset-audit`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`PaintPrisonBackWall`, `AddCornerBeamsAndWebs`, add `AddFullHeightStonePilaster`, add `AddAnchoredChainedPrisoner`, update `Foreground_Silhouettes`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A16`, `A18`, `A20`)

**Design Interface:**
1. **Replace 7 Floating `WoodenBeamTruss` with Full Floor-to-Ceiling Slender Stone Pilasters (`P1-02`):**
   - Add `AddFullHeightStonePilaster(GameObject artRoot, string name, PrisonPalette pal, Material mat, float x, float floorY, float ceilingY)` that creates container `name` with stacked vertical segments of **`pal.SpawnerPillar`** (`spawnerPillar_00.png`, `2.62u` wide × `5.04u` tall at **`0.85f`** scale => `2.23u` wide × `4.28u` tall per segment, `Background / -13`, weathered stone tint `(0.84, 0.88, 0.86, 1f)`), repeating cleanly upward until `topBounds.max.y >= ceilingY - 0.10f` (final top segment neatly tucked under ceiling rim so nothing protrudes into upper rooms!).
   - Place all 7 pilasters (`Art_N_Truss1` at `108.5`, `Art_N_Truss2` at `135.5`, `Art_O_Truss1` at `110.5`, `Art_P_Truss1` at `199.8`, `Art_R_Truss1` moved to **`-84.0`** clear of `Art_R_Window2 (-33.5)` & `OneWay_R_Mid2 [-82..-70]` with `Art_R_Lantern_0` at `-87.0`, `Art_T_Truss1` moved to **`50.2`** clear of Bay #8 & `OneWay_T_Mid1` with `Art_T_TorchMid_0` at `45.5` & `Art_T_Lantern_0` at `46.0`, `Art_T_Truss2` at **`119.5`** with `Art_T_Cage3` at `124.5` & `Art_T_TorchLow_3` at `116.5`).
   - Update `A16:1593-1600`: assert all 7 pilasters use `spawnerPillar_00` on `Background / -13`, grounded `min.y <= floorY + 0.12f`, reaching `max.y >= ceilingY - 0.15f`, and zero `PrisonwoodenBeam_00` anywhere in the scene!
2. **Eliminate Random Mid-Wall Black Blobs (`P1-03` Part A):**
   - In `PaintPrisonBackWall` (`L3581-3586`), remove the `else if (roll < 19)` `pal.WallJunks` (`wallJunk_10..13`) scatter branch; keep subtle `pal.StampWalls` masonry texture (`roll < 18`).
3. **Anchor All 8 `ChainedPrisoners` to Room Ceiling (`P1-03` Part B):**
   - Replace bare `AddPrisonSpriteProp` calls for all 8 `Art_*_Chained*` (`M`, `N`, `O`, `P`, `Q`, `R`, `T1`, `T2`) with `AddAnchoredChainedPrisoner(artRoot, name, sprite, pal, mat, pos, ceilingY)` at `scale = 0.85f`, lifted value tint `(0.90, 0.94, 0.98, 0.95)` on `Background / -14`, PLUS sibling vertical dark-iron anchor chain segments (`{name}_Anchor_{k}`, `pal.ChainTile`, `Background / -15`, tint `(0.58, 0.64, 0.68, 0.92)`) extending continuously from the top of the chained silhouette cleanly up to `ceilingY` (`max.y >= ceilingY - 0.12f`).
4. **Remove Floating Open-Doorway Top Cobweb Bowties (`gpu_r6_ng_01`/`02` Find):**
   - In `AddCornerBeamsAndWebs` (`L4136-4146`), place `WebSmallTL` ONLY when `hasLeftBottomWall` (solid left corner!) and `WebSmallTR` ONLY when `hasRightBottomWall` (solid right corner!) -> eliminates floating doorway cobweb bowties at `(17,4)/(18,4)` and `(42,4)` while preserving `A12`'s solid-corner `(0,1)` `webSmallBL_00`!
5. **Clean Full-Height Peripheral `Foreground_Silhouettes` (`P1-03` Part C):**
   - Replace the 9 severed top-stub `FgBeam_i` + `FgChain_i` clusters (`Builder:4795-4812`) with **5 full-viewport-height dark architectural silhouette columns** (`FgColumn_0..4` at `x = -36, -18, 0, +18, +36`, each stacking 4 vertical `spawnerPillar_00` silhouette pieces spanning `localY = -6.5 .. +9.5` — exceeding the entire `14u` camera height so ZERO cut-off top/bottom edges ever appear on screen!), `sortingLayer = Foreground / 20`, `unlitMat`, soft atmospheric indigo silhouette tint `new Color(4f / 255f, 8f / 255f, 14f / 255f, 0.34f)` (keeps `A11` `_lockYToCameraStartOffset` and `A18` `[-32..+32]` span 100% green!).

**Steps:**
- [x] **Step 1:** Update `A16` (pilasters check) and extend `A20` (`0` `wallJunk_10..13` tiles on any `TM_WallDecals`, `0` `PrisonwoodenBeam_00` renderers, every `Art_*_Chained*` has anchor chain reaching `ceilingY - 0.15f`, zero top cobwebs at open doorway `(17,4)`/`(18,4)`, and every `Foreground_Silhouettes` column spans `min.y <= -5.8f && max.y >= 8.8f` with zero cut-off top stubs).
- [x] **Step 2:** Run `Wiring2` (`A16`, `A20`) -> verify **RED**.
- [x] **Step 3:** Implement edits in `NhaGiamGiengChuongBuilder.cs`; run `BuildAll`.
- [x] **Step 4:** Re-run `Wiring2` (`A14..A20`) -> verify **GREEN**.
- [x] **Step 5:** Commit: `fix(nggc): replace floating trusses with ceiling-grounded pilasters, scrub wall blobs, anchor chained prisoners, clean foreground columns`

---

### Task 3.5: Resolve All 12 Mixel Scale Violations (Zero Room Overflow!) & Unify Room G/C Dead Cells Prison Palette (`P1-04`)

**Skills:** `2d-pixel-perfect`, `sprite-editor`, `ccgs-asset-audit`, `ccgs-team-polish`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs` (add 24-PPU `prisonShelfVial_00..02.png` `10×14px` & 24-PPU `prisonBell_00.png` `48×56px` + auto normal maps)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (Rooms B, C, G, K, L, M, N, O, Q, R, S, T prop scaling/selection + `AddShelfWithFioles` + `AddCornerBeamsAndWebs` + `AddPlatformCornerBrace`)
- Modify: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs` (verify `prisonShelfVial_00..02` & `prisonBell_00` 24-PPU + bound normal maps)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A08`, `A10`, `A15`, `A16`, `A18`, `A20`)

**Design Interface (Strict 24-PPU Pixel-Density Discipline — Every Prop Verified Against Room Dimensions):**
1. **True 24-PPU Replacements for Oversized Sprites That Cannot Fit Short Rooms (`6–7u` tall / `14u` wide):**
   - `Art_M_WeaponDeco` (Room M East Vault `14×7u`): replace `weaponDeco_00` (`16×5u` @ `0.24x`) with native 24-PPU **`spawnerJunk_00.png`** (`2.62×2.62u` at **`0.95x`**) at `x = 152.5, y = 0` (`2.8u` clear of `I11_Scroll` at `156.5`, `2.5u` clear of `SD_1` at `150`).
   - `Art_Q_SnakeLandmark` (Room Q `7u` tall): replace `snakeLandmark_00` (`23.5×19.4u` @ `0.28x`) with native 24-PPU **`archeSmall_00.png`** (`5.0×5.0u` at **`1.00x`**) at `x = -91.5, y = 16` (`2.0u` clear of `TP_4` at `-96`, `1.0u` clear of `OneWay_Q_Mid` at `-88`, `2.0u` below `Ceiling_Q`).
   - `Art_Q_SkeletonSpawn` + `Art_Q_SkeletonPier` (Room Q `7u` tall): replace `skeletonSpawn_00` (`12×13u` @ `0.48x`) with native 24-PPU **`prisonBigAlcove_00.png`** (`4.71×4.96u` at **`1.00x`**) at `x = -49.0, y = 16` (`bounds.min.x ≈ -51.35f`), flanked cleanly on left by `Art_Q_SkeletonPier` (`spawnerPillar_00` at **`0.92x`**, `x = -52.2`). Update `A18:1821` expected `min.x` to `-51.35f`.
   - `Art_B_Banner` (Room B `6u` tall): replace `bannerBig_00` (`3×9u` @ `0.50x`) with a **2-piece native `1.00x` `AddVerticalRoomBanner`** (`BannerRoomTop` + `BannerRoomBot` = `1.33×2.67u`, `Mat_RoomBanner_Sway` + `VertexSwayPin2D`, top `y = 5.6f` at `x = 20.5f`).
   - `Art_K_BannerBig` (Room K upper space above `OneWay_K_Top@24` is `6.5u` tall): replace `bannerBig_00` (`0.60x`) with a **3-piece native `1.00x` `AddVerticalRoomBanner`** (`4.0u` tall, top `y = 30.0f` at `x = -27.6f`, strictly above `OneWay_K_Top@24`!).
   - `Art_G_Alcove` (Room G Upper West `9×6.5u`): replace `alchemyAlcove_00` (`15×7u` @ `0.58x`) with native 24-PPU **`prisonBigAlcove_00.png`** (`4.71×4.96u` at **`1.00x`**) at `(60.5, 8)` -> fits `Floor_G_UpperWest [56..65]` with `2.1u` margin on both sides!
   - `AddShelfWithFioles` (13 shelves across all rooms — previously `fioleA/B/C` at **`0.28x`**!): generate **`prisonShelfVial_00..02.png`** (`10×14px` = `0.42×0.58u` at 24 PPU, crisp emerald/violet/amber alchemy glass vials + `_NormalMap`) in `PrisonPropSpriteGenerator.EnsureAll()`, and render at native **`1.00x`** on `shelf_0_00` at **`0.85x`**!
2. **Restore Native `[0.80x..1.00x]` Scale on Tall-Room Landmarks (Verified Zero Overflow):**
   - `Art_R_BigSword` (`bigSword_00`, `17.8×6.9u`): raise from `0.55x` → **`0.80x`** (`14.26×5.54u`) mounted high on Room R East open wall at `(-45.5, 34.4)` (`x=[-52.6..-38.4], y=[31.6..37.2]`, strictly clear of Bay #7 `[-60..-54]`, `OneWay_R_Mid1 [-54..-42]@28.5`, and `I12_Scroll (-95, 24)`! Shift `Art_R_Banner` to `-36.5`, `Art_R_Cage` to `-57.0` or `-39.5` clear of sword, `Art_R_Lantern_2` to `-36.0` — verified by `A12`/`A14`/`A16`/`A20`!).
   - `Art_S_AlchemyBibli1` & `Art_S_AlchemyBibli2` (`alchemyBibli_00`, `15.5×11.29u`): raise from `0.58x` → **`0.80x`** (`12.4×9.03u`). Elevate Room S Bay #14 to clerestory `[47..55, 34..38]` (`y >= 34` above `Bibli1` top `33.03`!), place `Art_S_AlchemyBibli1` at `(46.0, 24)` (`[39.8..52.2]`) and `Art_S_AlchemyBibli2` at `(62.5, 24)` (`[56.3..68.7]`), place `Art_S_AspectsDesk` (`0.82x`) + `Art_S_TableauNoir` (`0.82x`) + `I16_LoreTablet` at `x = 73.0` (`[71.1..74.9]`, clear of shaft `77`!), shift `Art_S_Shelf1/2` to `(86.5, 26.2)` & `(89.5, 26.2)` in the clean east bay! Update `A15:1293` Bay #14 interior sample to `(51, 35, 0)` and `A16:1494` Bay #14 rect to `(14, 47f, 55f, 34f, 38f)`.
   - Tall-room `BannerBig` (`C`, `L`, `N`, `R`, `S`, `T1`, `T2`): raise from `0.65–0.70x` → **`0.82x`** (verified clear of OneWay/bays).
   - Structural trims: `AddPlatformCornerBrace` `0.55f → 0.78f`, `AddCornerBeamsAndWebs` `beamScale` `0.72f → 0.80f`, `CeilStamp`/`RoofMoss` `0.72–0.75f → 0.80f`, `Art_O_MonsterDoor` `0.78f → 0.80f`, `AddPrisonChainArt` `CapBottom` `0.70f → 0.80f` (`y = floorTopY + 0.45f`).
3. **Cohesive Dead Cells Prison Palette in Rooms G & C (Zero GothicDungeon Visual Clash):**
   - `Art_C_Bell`: generate **`prisonBell_00.png`** (`48×56px` = `2.0×2.33u` at 24 PPU, weathered dark iron & oxidized verdigris bronze dungeon bell + `_NormalMap`) in `PrisonPropSpriteGenerator.EnsureAll()`, hang at native **`1.00x`** from `Ceiling_C` `(49, 18.5)`!
   - `Art_G_Winch`: replace `MachineryAndBell_012` (`0.58x`) with native 24-PPU **`chainAnchor_00.png`** (`2.0×2.0u` at **`0.90x`**) at `(73.6, 8)` on `Floor_G_UpperEast`!
   - `Art_G_Barrel`: replace `StorageAndVessel_014` (`0.58x`) with native 24-PPU **`floorjunk_0_00.png`** (`2.04×1.38u` at **`0.90x`**, height `1.24u <= 1.35u`!) at `(60.0, 0)`, tint `(0.76, 0.80, 0.82, 1f)`.
   - `Art_G_Crate`: replace `StorageAndVessel_010` (`0.55x`) with native 24-PPU **`floorjunk_2_00.png`** (`2.25×1.21u` at **`0.92x`**, height `1.11u <= 1.35u`!) at `(64.5, 0)`, tint `(0.76, 0.80, 0.82, 1f)`.
   - `I3_HealingChest/Visual`: keep golden `StorageAndVessel_003` for instant gameplay reward readability (`A08` preserved!), calibrated with warm dungeon-lit tint `new Color(0.86f, 0.80f, 0.66f, 1f)` so it integrates with Prison chiaroscuro lighting without looking pasted!
   - Update `A08:282-286` to expect `floorjunk_0_00` on `Art_G_Barrel` (and zero `StorageAndVessel_014` / `StorageAndVessel_010` / `MachineryAndBell_001` / `MachineryAndBell_012` in Rooms C/G!).

**Steps:**
- [x] **Step 1:** Extend `A20` with the global **Mixel & Pixel-Density Guard**: every `SpriteRenderer` under `Rooms/*/Art` (excluding `GodRay`, `Glow`, `Beam_*` procedural solid fills, `Plank_*` `0.75y` wood boards) MUST satisfy `abs(localScale.x) in [0.78f, 1.25f]` AND `abs(localScale.y) in [0.78f, 1.25f]` AND `sprite.pixelsPerUnit == 24f` (effective PPU strictly in `[19.2, 30.8]`) AND zero prop protrudes outside its owning room's `[floorY - 0.15, ceilingY + 0.15]`!
- [x] **Step 2:** Run `Wiring2` (`A20`) -> verify **RED** (lists all 12+ mixel violations!).
- [x] **Step 3:** Add `prisonShelfVial_00..02` & `prisonBell_00` to `PrisonPropSpriteGenerator.cs`, apply all prop updates in `NhaGiamGiengChuongBuilder.cs`, update `VisualShaderAndNormalTests` + `A08`/`A15`/`A16`/`A18`; run `BuildAll`.
- [x] **Step 4:** Re-run `Core.Tests.EditMode` + `Wiring1` + `Wiring2` (`A07..A20`) -> verify **GREEN**.
- [x] **Step 5:** Commit: `fix(art,nggc): eliminate all mixel scale violations, add 24-PPU shelf vials & Prison bell, unify Room G/C palette`

---

### Task 3.6: Player Figure-Ground Readability Rim Light (`P1-05`), Room A Torch Clearance, Inter-Tier Stone Slab Strata & Room G Alcove Fill (`P1-06`)

**Skills:** `urp-postprocessing`, `craft-game-floors`, `ccgs-team-polish`, `test-driven-development`

**Files:**
- Modify: `Assets/_Project/Features/Player/Prefabs/Player.prefab`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`BuildRoomAB`, `BuildRoomG`, add `AddInterTierSlabStrata` across inter-tier slabs)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (add `A21_Round9_PlayerReadabilityWindowDepthFogAndPolish`)

**Design Interface:**
1. **Persistent Beheaded Head-Flame Rim `Light2D` (`P1-05` — Safe Across BOTH `NhaGiamGiengChuong` AND `Main_Level`!):**
   - Add child GameObject `PlayerRimLight` to `Assets/_Project/Features/Player/Prefabs/Player.prefab` at local `(0f, 1.25f, 0f)` with `Light2D`:
     - `m_LightType: 3` (`Point`)
     - `m_Color: {r: 1.0, g: 0.89, b: 0.66, a: 1}` (`#FFE3A8`, warm subtle head-flame rim)
     - `m_Intensity: 0.56`
     - `m_PointLightInnerRadius: 0.35`, `m_PointLightOuterRadius: 2.6`, `m_FalloffIntensity: 0.65`
     - `m_LightVolumeEnabled: 0` (no distracting volumetric fog bubble around Player!)
     - `m_NormalMapQuality: 1` (`Fast`), `m_NormalMapDistance: 3.2`
     - **CRITICAL FOR `MainLevelIntegrityTests`:** `m_ApplyToSortingLayers` serialized directly in `Player.prefab` to the exact 5 gameplay TagManager IDs: `[0, 989596349, 1656045982, 2034603147, 219262143]` (`Default`, `Background`, `Ground`, `Actors`, `VFX`) — strictly excluding `ParallaxFar (548426159)`, `ParallaxMid (1193262008)`, `Foreground (1500198624)`!
2. **Room A Torch Spacing (`P1-06a`):**
   - Move `Art_A_Torch` from `x = 14.3` (crammed `0.3u` from Bay #0 right edge `14.0` and `1.2u` from `TP_0_Spawn` at `15.5`) to **`x = 7.4`? Wait: `Art_A_Window` is at `7.6`! Check open solid brick in Room A `[0..18]`:**
     - `Art_A_Bed (1.8)`, `Art_A_Fountain (4.3)`, `Art_A_Window (7.6)`, Bay #0 `[9..14, 2..5]`, `TP_0_Spawn (15.5)`.
     - Shift `TP_0_Spawn`? NO! `P08` hardcodes `TP_0_Spawn` at `(15.5, 0)`!
     - Shift Bay #0 left 1 cell from `[9..14]` → **`[8..12, 2..5]`** (with `Art_A_Window` at `6.2`, `Art_A_Cage` at `5.4` or `13.6`, `Art_A_Torch` at **`13.6`** -> `1.6u` clear of Bay #0 right edge `12.0` AND `1.9u` clear of `TP_0_Spawn` at `15.5`! Verify `A14`/`A16` Room A bay & prop clearances: `Art_A_Fountain (4.3)` -> `Art_A_Window (6.2)` -> Bay #0 `[8..12]` -> `Art_A_Torch (13.6)` -> `TP_0_Spawn (15.5)` — clean rhythmic museum spacing across Room A!). Update `A16:1491` Bay #0 rect to `(0, 8f, 12f, 2f, 5f)`!
3. **Inter-Tier Carved Stone Slab Strata (`P1-06b` — Safe With `A16` `_BedrockStone_`!):**
   - Add `AddInterTierSlabStrata(artRoot, pal, mat, tag, xMin, xMax, slabCenterY)` placing `stoneWall_0..3_00` (`3×1u` horizontal stone masonry courses at `0.90x`, `Ground / -4`, cool slate tint `new Color(0.56f, 0.64f, 0.64f, 0.56f)`, named **`Art_{tag}_SlabStrata_{i}`** — NEVER `_BedrockStone_`!) along every flat black `1.0–2.5u` inter-tier floor/ceiling slab:
     - `CD_BtoD` (`x=22..42, y=7.15`), `G_LowerToUpper` (`x=56..65, y=7.45`), `HJK_DtoJ` (`x=-16..6, y=15.35`), `ELF_JtoE` (`x=-16..22, y=23.35`), `ELF_EtoF` (`x=0..22, y=31.45`), `W1_MtoN` (`x=76..101 & 107..164, y=7.45`), `W2_OtoM` (`x=76..115 & 121..164, y=-0.55`), `W3_QtoR` (`x=-102..-69 & -63..-30, y=23.45`), `U1_StoT` (`x=38..77 & 83..140, y=39.45`)!
   - Breaks EVERY monotonous black bar between dungeon tiers into subtle carved brickwork!
4. **Room G `Art_G_Alcove` Fill Light (`P1-06c`):**
   - Add warm accent sconce `Art_G_AlcoveLight` (`Point Light2D`, `#FFCA7A`, `intensity = 0.92f`, `innerRadius = 0.6f`, `outerRadius = 4.6f`, `falloffIntensity = 0.62f`, `enableVolumetric = false`, gameplay layers only) centered at `(60.5f, 10.4f)` inside `Art_G_Alcove` so the alcove masonry reads clearly!

**Steps:**
- [x] **Step 1:** Add `A21_Round9_PlayerReadabilityWindowDepthFogAndPolish` to `MapWiringTests.cs` verifying:
  - `Player/PlayerRimLight` `Light2D` exists, enabled, `intensity in [0.45, 0.75]`, excludes `ParallaxFar`/`ParallaxMid`/`Foreground`.
  - `Art_A_Torch` clears Bay #0 right edge (`x=12`) and `TP_0_Spawn` (`x=15.5`) by `>= 1.2u` each.
  - `>= 40` `Art_*_SlabStrata_*` renderers exist across inter-tier slabs on `Ground / -4`.
  - `Art_G_Alcove` center `(60.5, 10.4)` has a Point `Light2D` within `<= 0.50 * outerRadius`.
  - Organic window variants (`prisonArchJamb_00..02`, `prisonArchSill_00..02`, `prisonArchSpandrel_00`, `prisonArchMullion_00`) + `ArchMoonSpill_*` are actively placed across all 16 bays.
  - All 9 `GroundMist_R5` strips use vertex alpha `>= 0.80f` and `Mat_GroundMist_Prison.mat` `_MistColor.a >= 0.55f`.
- [x] **Step 2:** Run `Wiring2` (`A21`) -> verify **RED**.
- [x] **Step 3:** Add `PlayerRimLight` to `Player.prefab` (via Unity Editor script or safe prefab serialization with exact TagManager layer IDs) and apply Room A/G/SlabStrata polish in `NhaGiamGiengChuongBuilder.cs`; run `BuildAll`.
- [x] **Step 4:** Re-run `Core.Tests.EditMode` (verifying `MainLevelIntegrityTests` STILL PASSES 100% with new `Player.prefab`!) + `Wiring1` + `Wiring2` (`A07..A21`) -> verify **GREEN**.
- [x] **Step 5:** Commit: `feat(player,nggc): add PlayerRimLight readability beacon, inter-tier stone slab strata, and Room A/G polish`

---

### Task 3.7: 15-Shot Round 9 D3D12 GPU Verification Harness & Mandatory Human-Eye Visual Audit Gate

**Skills:** `unity-cli`, `2d-pixel-perfect`, `urp-postprocessing`, `ccgs-team-polish`, `verification-before-completion`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (add `CaptureRound9GpuVerificationFrames()` preserving `CaptureRound6GpuVerificationFrames()`)

**Design Interface (`CaptureRound9GpuVerificationFrames` -> `Logs/r9-gpu-captures/gpu_r9_ng_01..15.png`):**
- Reuses `CaptureGpuVerificationFramesCore` with `enforceStrictGates: true` (`shaderErrors == 0`, `preFlight == 5/5`, `defaultMaterialRenderers == 0`, `maxClipPct <= 0.500%`, target `< 0.15%`).
- **15 Targeted Camera Shots (9 baseline angles + 6 dedicated Round 9 close-up proof shots):**
  1. `gpu_r9_ng_01_room_ab.png` `(18f, 3.0f)` ortho `5.8f` — Room A/B overview (verify `OneWay_A_Practice` gone, no floating doorway cobweb bowtie, `Art_B_Banner` native scale, visible Room A/B fog, Room A torch clear)
  2. `gpu_r9_ng_02_room_cd.png` `(45f, 6.5f)` ortho `6.5f` — Room C/D Bell-Well (verify `OneWay_C_Rest` gone, flush `ladderHole_00` zero horns, 24-PPU `prisonBell_00`, zero wall blobs, visible mist, zero overlapping barrels)
  3. `gpu_r9_ng_03_room_g.png` `(66f, 5.5f)` ortho `6.2f` — Room G (verify `Art_G_Winch` off chain at `(73.6,8)`, illuminated native `Art_G_Alcove`, cohesive Prison barrel/crate/chest, curved gothic Bay #2 with iron bars & moonlight spill, visible mist)
  4. `gpu_r9_ng_04_room_hjk.png` `(-8f, 19.5f)` ortho `6.0f` — Room J/K (verify inter-tier stone slab strata breaking black bar, curved Bay #3 with iron bars, clean ceiling/floor transitions)
  5. `gpu_r9_ng_05_room_elf.png` `(8f, 29.5f)` ortho `6.5f` — Room E/L/F (verify curved Bay #4 with iron bars, `OneWay_E_Combat` + `Enemy_Rat_E2`, visible Room E mist, clean Exit Arch glow)
  6. `gpu_r9_ng_06_wing_w1_mn.png` `(122f, 11.5f)` ortho `7.0f` — Wing W1 M/N (verify zero top-edge severed foreground stubs, anchored `Art_N_Chained1`, zero wall blobs, floor-to-ceiling stone pilasters)
  7. `gpu_r9_ng_07_wing_w2_op.png` `(198f, -10.5f)` ortho `6.8f` — Wing W2 O/P (verify `Art_P_Truss1` floor-to-ceiling stone pilaster, zero wall blobs, curved Bay #12 with iron bars, visible sewer mist)
  8. `gpu_r9_ng_08_wing_w3_qr.png` `(-66f, 27.5f)` ortho `7.0f` — Wing W3 Q/R (verify `Art_R_Chained1` chain-anchored to ceiling, flush `I9` `ladderHole`, curved Bay #7 & #13, zero floating blobs)
  9. `gpu_r9_ng_09_wing_u1_st.png` `(72f, 38.0f)` ortho `7.0f` — Wing U1 S/T junction (verify `OneWay_S_Gallery` gone, flush `I10` `ladderHole`, inter-tier slab strata, zero severed foreground stubs)
  10. `gpu_r9_ng_10_stairs_h_45deg.png` `(14.0f, 12.5f)` ortho `5.8f` — **CLOSE-UP `Stairs_H`** (verify 8 single-row 45° diagonal stair tiles, zero double risers, fitted polygon shadow with zero floating black box, clean landings & wall-mounted torches)
  11. `gpu_r9_ng_11_player_spawn_window_mist.png` `(9.0f, 2.8f)` ortho `5.2f` — **CLOSE-UP Spawn + Bay #0 + Mist** (verify `Player` + `PlayerRimLight` crisp readability, curved weathered Bay #0 with iron bars & moonlight spill — NOT a sticker!, `Art_A_Torch` clear of Bay #0 & `TP_0`, visibly silky ground mist)
  12. `gpu_r9_ng_12_room_n_payoff_pilasters.png` `(148.0f, 13.0f)` ortho `6.8f` — **CLOSE-UP East Room N** (verify `OneWay_N_High` gone, `I19_LoreTablet_NorthGallery`, 4 gleaming `GoldCache_N_*` coins, `Art_N_Truss2` floor-to-ceiling pilaster, zero junk overlaps)
  13. `gpu_r9_ng_13_room_t_scroll_promenade.png` `(119.0f, 45.0f)` ortho `6.8f` — **CLOSE-UP East Room T** (verify `OneWay_T_High` gone, `I17_Scroll_HighBastion`, glowing live `I18_PromenadeExitTrigger` at `Art_T_CourtyardArch`, `Art_T_Truss2` floor-to-ceiling pilaster, visible Room T mist)
  14. `gpu_r9_ng_14_room_qr_mixels_sword.png` `(-52.0f, 25.0f)` ortho `8.0f` — **CLOSE-UP Room Q/R Mixels** (verify native `Art_Q_SkeletonSpawn` + `Art_Q_SkeletonPier`, `Art_R_BigSword` at `0.80x`, 24-PPU shelf vials, anchored `Art_Q/R_Chained1`)
  15. `gpu_r9_ng_15_room_s_alchemy_library.png` `(60.0f, 30.0f)` ortho `7.0f` — **CLOSE-UP Room S Library** (verify `Art_S_AlchemyBibli1/2` at `0.80x` cleanly beneath elevated clerestory Bay #14, `AspectsDesk` + `I16_LoreTablet`, zero junk/shelf overlaps)

**Steps:**
- [x] **Step 1:** Add `CaptureRound9GpuVerificationFrames()` to `NhaGiamGiengChuongBuilder.cs`.
- [x] **Step 2:** Run D3D12 GPU capture (**NO `-nographics`**):
  ```powershell
  rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.CaptureRound9GpuVerificationFrames -logFile "Logs/r9-gpu-verify.log"
  ```
- [x] **Step 3:** Inspect `Logs/r9-gpu-verify.log`: confirm `shaderErrors=0`, `URP_PreFlight 5/5 True`, `defaultMaterialRenderers=0`, `maxClipPct < 0.15%`.
- [x] **Step 4:** **MANDATORY VISUAL AUDIT:** Call `read_file` on **ALL 15 PNGs** (`Logs/r9-gpu-captures/gpu_r9_ng_01..15.png`) and grade each shot explicitly against its checklist above! **STOP RULE:** If ANY single shot shows a sticker window, invisible fog, floating prop, overlap, mixel artifact, shadow box, or lighting defect -> fix it in Phase 3, re-run `BuildAll`, re-capture, and re-inspect before entering Phase 4!
- [x] **Step 5:** Commit: `chore(nggc): add 15-shot CaptureRound9GpuVerificationFrames visual audit harness`

---

## Phase 4 — PlayMode Runtime Proof, Full QA Gate, Standalone `.exe`, Git Hygiene, Push & PR

> **Phase 4 Mandatory Skills (read before editing):**
> - Unity Plugin: `unity-cli`, `unity-pipeline`
> - CCGS: `ccgs-team-qa`, `ccgs-smoke-check`, `ccgs-regression-suite`, `ccgs-test-evidence-review`, `ccgs-code-review`, `ccgs-team-release`, `ccgs-release-checklist`, `ccgs-changelog`
> - Superpowers: `requesting-code-review`, `verification-before-completion`, `finishing-a-development-branch`

### Task 4.1: Round 9 End-to-End PlayMode Regression Suite (`P09` & `P10`)

**Skills:** `ccgs-team-qa`, `ccgs-regression-suite`, `ccgs-test-evidence-review`, `test-driven-development`, `unity-cli`

**Files:**
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/Round9PlayTests.cs`

**Design Interface (Runtime Proofs of All P0 Gameplay Fixes — Named `P09` & `P10` so `P08` in `ExpansionPlayTests.cs` is untouched!):**
- **`P09_Round9_StairsH45SmoothWalk_ChainFlushDismountNoAirGrabAndSDescend_AndEnemyPassThrough`:**
  1. **45° `Stairs_H` walk up, idle hold, walk down:** Position Player at `(20f, 8f)`, hold `A` -> assert Player walks continuously up `Stairs_H` to Room J (`x < 7f, y = 16.0 ± 0.15`) WITHOUT ever pressing Jump; release `A` midway on slope `(14f, 12f)` for `0.35s` -> assert zero downhill slide (`|dx| < 0.08u`); hold `D` back down to `(x > 19f, y = 8.0 ± 0.15)`.
  2. **Chain climb flush dismount + no `W` air re-grab + `S` crouch-descend:** Climb `I1_Chain_C` with `W` -> assert feet land flush at `y = 8.0 ± 0.12` (never hovering at `8.5`); continue holding / re-tapping `W` while grounded on `OneWay_C_Top` -> assert Player stays grounded (NEVER re-enters `PlayerClimbState`!); hold `S` (no `Space`) for `0.30s` -> assert Player smoothly enters `PlayerClimbState` descending below `y = 7.4f`!
  3. **No standing on `Enemy_Shield` head:** Wake `J1`, drop Player directly from `(-4f, 18.5f)` onto `Enemy_Shield_J` at `(-4f, 16.0f)` -> assert Player falls straight through to floor `y = 16.0 ± 0.15` within `0.8s` (NEVER perched at `y ~ 17.6`!), while a rear sword strike still deals damage!
- **`P10_Round9_HackAndSlashFeel_PersistentLunge_ContinuousCombo_KnockbackPop_RoomNGold_AndPromenadeExit`:**
  1. **Persistent lunge + continuous 3→1 combo + single swing event per strike:** Subscribe to `player.Context.Combat.StrikePerformed` counter; trigger grounded Hit 1 → Hit 2 → Hit 3 → buffered 4th press (wrapping to Hit 1) -> assert forward lunge displacement during Hit 1 (`dx > 0.35u`), assert `StrikePerformed` count == `4` (1 per swing, zero multi-frame VFX/SFX spam!), assert Hit 3 chains back into Hit 1 without dropping to `PlayerIdleState`, and verify striking `Enemy_Zombie_B` produces measurable horizontal + upward recoil velocity (`vy > 1.0f`)!
  2. **Room N Lore & Gold Cache:** Position Player at `I19_LoreTablet_NorthGallery (152.5, 8)` -> `TryInteract()` opens lore banner; move to `GoldCache_N (159, 8)` -> assert `GoldWallet.Balance` increases by `>= 100`!
  3. **Room T Promenade Exit:** Verify `I18_PromenadeExitTrigger` stays locked while `Enemy_Shield_T1` & `Enemy_Grenadier_T1` live, unlocks after both die, and submits clean `LevelOutcome.Win` (`ResultPanel.WinTitle`)!

**Steps:**
- [x] **Step 1:** Read `ccgs-team-qa/SKILL.md` and `ccgs-regression-suite/SKILL.md`.
- [x] **Step 2:** Create `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/Round9PlayTests.cs` with `P09` and `P10`.
- [x] **Step 3:** Run full `NhaGiamGiengChuong.Tests.PlayMode` via Unity batchmode -> verify `10/10` (`P01..P10`) **GREEN**.
- [x] **Step 4:** Commit: `test(nggc): add P09 and P10 PlayMode proofs for 45-deg stairs, chain climb, enemy pass-through, combat feel, and Room N/T payoffs`

---

### Task 4.2: Final Full-Game Verification Gate (5 Test Processes + 15-Shot GPU Audit + Win64 `.exe` + CodeGraph Sync)

**Skills:** `unity-cli`, `unity-pipeline`, `ccgs-smoke-check`, `ccgs-test-evidence-review`, `ccgs-gate-check`, `verification-before-completion`

**Steps:**
- [x] **Step 1:** Run fresh `NhaGiamGiengChuongBuilder.BuildAll` via Unity batchmode (`Logs/r9-final-buildall.log`) -> verify `0` errors, `0` missing scripts.
- [x] **Step 2:** Run all 5 test processes sequentially (fresh batchmode process per assembly/group for strict isolation):
  1. `Core.Tests.EditMode` (`Logs/r9-final-core.xml` — covers `MainLevelIntegrityTests`, `VisualShaderAndNormalTests`, `ParallaxAndAtmosphereTests`, `Round8*`, `Round9CombatPhysicsTests`, `RunFlowAndHudTests`, etc.)
  2. `NhaGiamGiengChuong.Tests.EditMode` unit & geometry (`-testFilter "!DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests"`, `Logs/r9-final-nggc-unit.xml`)
  3. `MapWiringTests` Part 1 (`A07..A13`, `Logs/r9-final-wiring1.xml`)
  4. `MapWiringTests` Part 2 (`A14..A21`, `Logs/r9-final-wiring2.xml`)
  5. `NhaGiamGiengChuong.Tests.PlayMode` (`P01..P10`, `Logs/r9-final-playmode.xml`)
  - Parse every XML result file and record exact `passed / total / failed / skipped` counts (must be **100% pass, 0 failed, 0 skipped**!).
- [x] **Step 3:** Run final `CaptureRound9GpuVerificationFrames` (D3D12 windowed batchmode, **NO `-nographics`**, `Logs/r9-final-gpu.log`), verify `shaderErrors=0`, `URP_PreFlight 5/5`, `defaultMaterialRenderers=0`, `maxClip < 0.15%`, and visually inspect all 15 final PNGs (`Logs/r9-gpu-captures/gpu_r9_ng_01..15.png`) via `read_file`.
- [x] **Step 4:** Run Win64 standalone build:
  ```powershell
  rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildStandaloneWin64 -logFile "Logs/r9-final-win64.log"
  ```
  Verify `0` build errors and non-empty `Build/NhaGiamGiengChuong/DeadCells_NG.exe` on disk.
- [x] **Step 5:** Run `rtk proxy cmd /c codegraph sync` -> verify `[OK] Index is up to date`.

---

### Task 4.3: Pre-PR Studio Code Review, Git Branch Hygiene Audit, Push & Pull Request Creation

**Skills:** `ccgs-code-review`, `ccgs-team-release`, `ccgs-release-checklist`, `ccgs-changelog`, `requesting-code-review`, `finishing-a-development-branch`

**Steps:**
- [x] **Step 1:** Read `ccgs-release-checklist/SKILL.md`, `ccgs-changelog/SKILL.md`, and `finishing-a-development-branch/SKILL.md`.
- [x] **Step 2:** Perform **Strict Git Hygiene Audit** on `map-feat-designing`:
  - Run `rtk git status --short`.
  - **CRITICAL:** Add `/[Aa]ssets/_Recovery/` and `/[Aa]ssets/_Recovery.meta` to `.gitignore` so Unity autosave backups are NEVER committed (do NOT delete user files inside `Assets/_Recovery/` without explicit permission; keeping them git-ignored keeps the repo 100% clean and zero-risk!).
  - Inspect all pre-existing modified/untracked project files (`Assets/Settings/...`, `Assets/_Project/...`, `ProjectSettings/...`, `docs/superpowers/...`, `docs/Fortress/...`):
    - Verify every `.cs`/`.shader`/`.asset`/`.prefab`/`.unity`/`.mat`/`.png` has its matching Unity-generated `.meta` file staged alongside it!
    - Exclude any machine-local scratch files, logs (`Logs/`), builds (`Build/`), `.codegraph/`, `graphify-out/`, or `Assets/_ThirdParty/`.
    - Check for any single file `> 50 MB` (`rtk proxy python` size check across staged files) -> zero oversized binaries!
  - Commit cleanly in logical conventional commits (`feat(...)` / `fix(...)` / `chore(...)` / `docs(...)`) so a fresh clone of `map-feat-designing` builds and passes `100%` of tests!
- [x] **Step 3:** Run pre-push sanity gates:
  - `rtk git status --short` -> clean working tree (zero unintended untracked/modified files).
  - `rtk git diff --check origin/main...HEAD` -> zero conflict markers or bad whitespace issues.
  - `rtk git log origin/main..HEAD --oneline` -> clean commit narrative.
- [x] **Step 4:** Push `map-feat-designing` to `origin`:
  ```powershell
  rtk git push -u origin map-feat-designing
  ```
- [x] **Step 5:** Confirm no existing open PR for `map-feat-designing` (`rtk proxy gh pr list --head map-feat-designing`), then open the Pull Request targeting `main` (`origin` default branch verified via `gh repo view`):
  ```powershell
  rtk proxy gh pr create --base main --head map-feat-designing --title "..." --body-file "..."
  ```
  - PR Body (structured per `ccgs-changelog` + `ccgs-release-checklist`):
    1. Executive Summary (Prisoner's Quarters `NhaGiamGiengChuong` 19-room full-scale map, Dead Cells visual/lighting/shader overhaul, combat feel & physics overhaul, audio/UI/pause menu, Round 9 E2E remediation).
    2. Complete 13-Item + 2 GPU-Find Remediation Checklist (`P0-01..P0-05`, `P1-01..P1-08`) with exact file links & behavioral proof.
    3. Automated Verification Table (exact `passed/total` from all 5 Unity test XML reports, `15/15` D3D12 GPU capture telemetry: `shaderErrors=0`, `URP_PreFlight=5/5`, `defaultMaterialRenderers=0`, `maxClip`, Win64 `.exe` build size/status, `codegraph sync`).
    4. Reviewer Playtest Checklist (`Stairs_H` 45° walk, chain climb/descend, `Enemy_Shield` pass-through, 3→1 sword combo + lunge, Room N lore/gold, Room T Scroll + Promenade Exit, outdoor windows & silky fog).
- [x] **Step 6:** Return the clickable PR URL, commit range (`origin/main..HEAD`), and complete verification evidence ledger to Master.

---

## 4. Swarm / Execution Topology (Conflict-Free 3-Wave Plan)

To avoid Unity `.unity` / `.prefab` / `Library/` write races while maximizing speed:

- **Wave 1 (Parallel isolated `share` worktrees -> merge cleanly to main workspace, NO concurrent Unity Editor runs on main workspace):**
  - `combat-worker` (Tasks 1.1, 1.2, 1.3, 1.4 — `PlayerContext`, Player states, `PlayerCombat`, `EnemyController`, `EnemyStunState`, `GoldPickup`, `PlayerMovementConfig.asset`, `Weapon_StartSword.asset`, 5 `Enemy_*.prefab`, `Round9CombatPhysicsTests.cs`, `Round8Phase2FeelMovementTests.Feel.cs`; skills: `create-2d-physics`, `craft-game-floors`, `ccgs-team-combat`, `ccgs-balance-check`, `test-driven-development`).
  - `art-shader-worker` (Tasks 3.1a + 3.2a + 3.5a — `PrisonPropSpriteGenerator.cs`, `GroundMist2D.shader`, `EnvironmentMaterialBuilder.cs`, `VisualShaderAndNormalTests.cs` — 100% file-disjoint from `combat-worker` and `NhaGiamGiengChuongBuilder.cs`!; skills: `2d-pixel-perfect`, `sprite-editor`, `urp-postprocessing`, `ccgs-team-polish`).
- **Wave 2 (Single `nggc-builder-worker` on merged main workspace — owns `NhaGiamGiengChuongBuilder.cs`, `Player.prefab` `PlayerRimLight`, `NhaGiamGiengChuong.unity`, room prefabs, and geometry/wiring tests):**
  - Runs Phase 1 gate (`Core.Tests.EditMode`), then executes Tasks 2.1, 2.2, 2.3, 2.4, 3.1b, 3.2b, 3.3, 3.4, 3.5b, 3.6, 3.7 (`BuildAll`, `A01..A21`, `CaptureRound9GpuVerificationFrames`, and **all 15 GPU PNG visual inspections** by Coordinator + Worker!; skills: `create-tile-palette-or-rule-tiles`, `2d-pixel-perfect`, `ccgs-team-level`, `ccgs-asset-audit`, `ccgs-team-polish`, `unity-cli`).
- **Wave 3 (`qa-release-worker` + Coordinator Sign-Off on main workspace):**
  - Runs Tasks 4.1 (`P09`/`P10`), 4.2 (all 5 fresh Unity test processes + final 15-shot D3D12 visual gate + `BuildStandaloneWin64` `.exe` + `codegraph sync`), and 4.3 (Git hygiene audit, `.gitignore` `_Recovery/`, clean commits, `git push -u origin map-feat-designing`, `gh pr create --base main`; skills: `ccgs-team-qa`, `ccgs-regression-suite`, `ccgs-gate-check`, `ccgs-team-release`, `ccgs-release-checklist`, `finishing-a-development-branch`).
