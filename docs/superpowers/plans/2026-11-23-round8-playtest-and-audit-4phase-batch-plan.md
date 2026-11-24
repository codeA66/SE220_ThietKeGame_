# Round 8: Playtest Feedback & Whole-Game Audit — 4-Phase Batch Implementation Plan (High-Detail Specification)

> **For agentic workers:** REQUIRED SUB-SKILL: Execute strictly phase-by-phase using `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans`.
> **MANDATORY EXECUTION & REVIEW LOOP:**
> 1. Implement Phase `k` tasks in TDD order (`Write failing test → Verify FAIL → Implement → Verify PASS → Commit`).
> 2. Run the **Phase `k` Review Gate** (all automated test batches + independent spec/code/asset review). If any defect is found, fix and re-verify inside Phase `k` before advancing.
> 3. Repeat for Phases `1 → 2 → 3 → 4`.
> 4. Run the **Whole-Plan Master Review** across all 4 phases (`27/27` items + cross-phase interactions).
> 5. Only after the Whole-Plan Master Review passes, execute the **Final Whole-Game Audit** (4-batch EditMode + PlayMode + `9/9` D3D12 GPU captures + standalone Windows `.exe` build + `codegraph sync`).
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve all **27 verified defects** across `NhaGiamGiengChuong` and core gameplay systems — **10 confirmed Master playtest items (8 original + 2 follow-ups)** and **17 newly audited codebase/scene/prefab/UX issues** — in 4 self-contained, mathematically pre-verified phases with zero regressions across the existing 78 tests.

**Architecture:**
1. **Phase 1 (P0 Combat, Projectile, AI & Interaction Correctness):**
   - Split `RecoverableArrow` hit detection into a thin wall `Raycast` (excluding `OneWayPlatform`) and a `0.24×0.28u` target `BoxCast`, add low-target auto-deflection in `PlayerBow.TryFire` + lower crouch muzzle (`y = 0.50f`) so **both standing and crouching** bow shots hit `Enemy_Rat` (`[0..0.70u]`).
   - Deflect/recall arrows on blocked hits (`TakeDamage == false`), shorten wall-impact lifetime (`0.6s`) and embedded lifetime (`4.5s`), mirror `_victimOffset.x` when enemies turn, recall on inactive victims, and extract embedded arrows on melee hit (`PlayerBow.ExtractArrowsFrom`).
   - Separate `_solidWallMask` (`groundMask & ~OneWayPlatform`) in `EnemyContext` for line-of-sight/wall/teleport checks, guard `EnemyLungeAttackState` against ledges (`CanWalk`), let enemy arrows pass through invulnerable rolling players, and make Grenadier `Explode()` circular (`OverlapCircle`) with solid-wall linecast occlusion and directional knockback.
   - Set `_rollCancelNormalizedTime = 0.55f`, add full `JumpHoldBoost` in `PlayerBowAttackState`, block pre-hit weapon-swap cancel exploit, allow post-hit Jump cancel in `PlayerAttackState`, scale Stomp damage with `PlayerRunStats` + allow Roll cancel on Stomp landing, wire `Crouch (C)` + Gamepad `EquipSword`/`EquipBow`.
   - Expand `Zone_B1/J1/E1` to full room bounds, separate `I11_Scroll_EastCourtyard` (`156.5f`), `Art_M_WeaponDeco` (`152.2f`), and `TP_1_EastVault` (`161.0f`) with a mutual-exclusion code guard, add `CameraFollow2D.SnapToTarget()` on teleport, and add locked/opened visual feedback on `TeleportGate`, `HealingChest`, `SwitchDoor`, and `SecondaryBiomeExitTrigger`.
2. **Phase 2 (60fps Dead Cells Game Feel, Wall-Cling/Ledge & Chain Climb):**
   - Offset chain-climb body X by `-FacingDirection * ClimbHandOffsetX` (`0.24f`) so hands grip the chain links.
   - Add `PlayerWallClingState` (wall-cling, capped `2.8 u/s` wall-slide, `(-wallDir * 9.5, 13.2)` wall-jump up to `3` times per airtime with `0.12s` horizontal re-grab lock, using existing `wallClimbA_00..11.png` sprites) and widen `_oneWayLedgeGrabTolerance` from `0.15f → 0.42f`.
   - Upgrade 60fps combat cadence (`Weapon_StartSword`: `AnimSpeed 1.25/1.35/0.95`, `Knockback 3.5/4.5/8.0`, Hit 3 `CanCrit = true`; grounded strike micro-lunge `3.8 u/s`; `CameraFollow2D.Punch` directional kick + tighter `_smoothTime = 0.09f`, `_deadzone = (0.55, 0.35)`; tiered hitstop `0.045 / 0.075 / 0.095 / 0.06s`; Shieldbearer stagger & Perfect-Dodge feedback).
   - Clone `Main_Level.unity`'s battle-tested `"Enemy_Shield hit feedback"` (`EnemyImpactFeedback` + `Enemy health` bar hierarchy) onto all **30/30 enemies** in `NhaGiamGiengChuongBuilder.cs`, tighten `EnemyDeadState` (`0.07s` flash + recoil pop + `0.18s` vanish), wire `Vfx_HeadSlime.asset` on `Player.prefab`, add roll/stomp/dodge VFX in `PlayerVfx`, and prewarm all 11 `Vfx_*.asset` in `VfxSpawner._prewarm`.
3. **Phase 3 (Visual Polish, Map Density, Lighting, Fog, Stairs & Parallax):**
   - Flip `Stairs_H` `stairs_0_00`/`stairs_1_00` tiles horizontally (`Matrix4x4.Scale(new Vector3(-1f, 1f, 1f))`) without overwriting lower tread caps.
   - Upgrade `GroundMist2D.shader` to world-space `positionWS.xy` 3-octave quintic FBM with `Blend SrcAlpha OneMinusSrcAlpha` and quadratic vertical feathering.
   - Socket `Art_Q_SkeletonSpawn`'s hard-cut left edge (`x = -51.88f`) behind a foreground masonry pier `Art_Q_SkeletonPier` (`pal.SpawnerPillar` at `x = -51.88f, y = 16f, scale = 0.92f, Background / -12`) while keeping `Art_Q_SkeletonSpawn` at `-49.0f`, and add **44 collision-verified grounded floor props** shrinking every room's max floor prop gap to **`<= 9.50u`**.
   - Lift effective point-light coverage (`<= 0.85 * outerRadius`) to **`93.1%–100.0%` across all 19 rooms** by tuning existing beacon radii (`8.8f` spine, `13.5–14.5f` wings, odd intensity `0.94f`, even `1.34f`) and adding `WindowMoonLight` (`r = 9.5f`) + `TeleportLight` (`r = 4.5f`) — creating **zero** new `LanternFixture_R5` sprite clashes; fix `ErrorIfDuplicateGlobalLight` during `BuildAll` by toggling `globalLight.enabled = false` around additive `Main_Level` load; expand `ShadowCasters_R5` from `6 → 12`.
   - Symmetrize `Foreground_Silhouettes` across `[-32..+32]` (9 anchors at `8u` spacing) and attach `CameraFollowEmitter2D` (`28×16u`) to `AmbientDustMotes` and `AmbientDrippingWater`.
4. **Phase 4 (Audio Hub, Pause/Quit Shell, Controls HUD & Final `.exe` Release):**
   - Clone `"Combat and Pickup Feedback"` (`ItemPickupFeedback` with `gold_1.wav` + `Vfx_CollectedGlint`) from `Main_Level.unity`, and add zero-allocation `CombatAudioHub` + `ProceduralSfxLibrary` in `Core` (`21` mono PCM SFX clips with `±8%` pitch jitter + looping dark dungeon ambient bed).
   - Add `LevelRunController.TryRestartFromPause()` and uGUI `PauseMenuController` (`Esc` during `Playing && !ScrollChoosing`, `sortingOrder = 90`, Resume / Restart / Quit + 2-column Vietnamese controls legend + subtle bottom-right HUD micro-hint).
   - Rebuild `NhaGiamGiengChuong.unity` + standalone `Build/NhaGiamGiengChuong/DeadCells_NG.exe`, complete Whole-Plan Master Review, and execute Final Whole-Game Audit.

**Tech Stack:** Unity `6000.6.9f1` (URP 2D, Legacy 2D Physics `Rigidbody2D`/`Physics2D`, uGUI, Input System, `Tilemap`, `Light2D`), C#, NUnit EditMode/PlayMode, `rtk` CLI wrapper.

**Spec / Evidence Baseline:**
- Master playtest observations (`M-01..M-09`) & whole-game read-only audit (`A-01..A-17`), verified 2026-11-23.
- Mandatory project rules: `D:/thietkegame/AGENTS.md` (§0–§10).

---

## Traceability Matrix (All 27 Verified Items → Phase & Task)

| ID | Source | Verified Defect | Phase / Task |
| --- | --- | --- | --- |
| M-01 | Master #1 | `Stairs_H` inverted diagonal texture (`\` slope vs `/` sprite) & rim overwrite | Phase 3 / Task 3.1 |
| M-02 | Master #2 | `GroundMist2D` 44:1 stretched UV & additive hard bottom band | Phase 3 / Task 3.2 |
| M-03 | Master #3 | Map emptiness (floor prop gaps `14.7–22.0u` across rooms) | Phase 3 / Task 3.3 |
| M-04 | Master #4 | `Art_Q_SkeletonSpawn` vertical hard-cut left edge at `x = -51.88f` looks like a sticker | Phase 3 / Task 3.3 |
| M-05 | Master #5 | Dark zones in 8 rooms (`57.4–65.0%` effective point-light coverage) | Phase 3 / Task 3.4 |
| M-06 | Master #6 | Chain climb aligns chain through head center (`0.5` pivot) instead of hands | Phase 2 / Task 2.1 |
| M-07 | Master #7+9a | Game feels too smooth/chill at 60fps; missing Dead Cells combat crunch & VFX | Phase 2 / Tasks 2.3–2.4 |
| M-08 | Master #8 | Bow arrows stick 12s on enemies/shield/inactive; world offset never flips | Phase 1 / Task 1.1 |
| M-09 | Master #9b | Missing wall-cling/wall-jump ("bám vách") + tiny `±0.15u` OneWay ledge window | Phase 2 / Task 2.2 |
| A-01 | Audit P0 #1 | Bow cannot hit `Enemy_Rat` (`[0..0.70u]`) on flat ground (standing `1.4375` or crouching `0.71875`) | Phase 1 / Task 1.1 |
| A-02 | Audit P0 #2 | 99% silent gameplay (only 1 `AudioSource`; `EnemyImpactFeedback=0`, `ItemPickupFeedback=0`) | Phase 2 / Task 2.4 + Phase 4 / Task 4.1 |
| A-03 | Audit P0 #3 | `[E]` trigger collision between `I11_Scroll_EastCourtyard (158.8)` & `TP_1_EastVault (161.0)` | Phase 1 / Task 1.4 |
| A-04 | Audit P0 #4 | `Zone_B1 [20..23]`, `Zone_J1 [2..5]`, `Zone_E1 [-15..-12]` doorway strips leave visible enemies frozen & invulnerable | Phase 1 / Task 1.4 |
| A-05 | Audit P1 #5 | Player arrows collide with `OneWayPlatform` (`_groundMask=576`) & lock quiver ammo 4s in walls | Phase 1 / Task 1.1 |
| A-06 | Audit P1 #6 | `_rollCancelNormalizedTime: 0` cancels Roll on frame 1 while burning cooldown & i-frames | Phase 1 / Task 1.3 |
| A-07 | Audit P1 #7 | Jumping during Bow draw (`PlayerBowAttackState`) skips `JumpHoldBoost` (tiny hop) | Phase 1 / Task 1.3 |
| A-08 | Audit P1 #8 | Enemy `_groundMask=576` treats OneWay as solid wall; `EnemyLungeAttackState` ignores ledges | Phase 1 / Task 1.2 |
| A-09 | Audit P1 #9 | Enemy straight arrows vanish on rolling player; Grenade blasts square `5.8×5.8u` through walls | Phase 1 / Task 1.2 |
| A-10 | Audit P1 #10 | Weapon-swap zero-recovery cancel exploit; Attack no jump-cancel; Stomp unscaled & roll-locked | Phase 1 / Task 1.3 |
| A-11 | Audit P1 #11 | `TeleportGate` 145–229u warp lacks `CameraFollow2D.SnapToTarget()` & lacks undiscovered-pair prompt | Phase 1 / Task 1.5 |
| A-12 | Audit P1 #12 | Silent/no visual feedback on Shield stagger, HealingChest open, SwitchDoor open, SewerExit lock | Phase 1 / Task 1.5 + Phase 2 / Task 2.3 + Phase 4 / Task 4.1 |
| A-13 | Audit P2 #13 | `Foreground_Silhouettes` `[-21.54..+38.75]` wrap causes `6.90u` left-edge pop-in | Phase 3 / Task 3.5 |
| A-14 | Audit P2 #14 | `AmbientDustMotes` (`10/s` over `336×80u`) & `AmbientDrippingWater` (`12/s` over `320×76u`) invisible | Phase 3 / Task 3.5 |
| A-15 | Audit P2 #15 | `PlayerSpawnHead._slimeVfx` is `null`; `VfxSpawner._prewarm` is empty `[]` | Phase 2 / Task 2.4 |
| A-16 | Audit P2 #16 | No `Esc` Pause/Quit menu or controls HUD; `Crouch (C)` & Gamepad weapon bindings unwired | Phase 1 / Task 1.3 + Phase 4 / Task 4.2 |
| A-17 | Audit P2 #17 | `BuildAll` logs 16 `ErrorIfDuplicateGlobalLight` errors; only 6 `ShadowCaster2D`s for 19 rooms | Phase 3 / Task 3.4 |

---

## Global Constraints

- **Shell Wrapper (`AGENTS.md` §0):** Run every shell command via `rtk` or `rtk proxy <command>`. Read/write all UTF-8 files cleanly.
- **Zero `.asmdef` Modifications (`AGENTS.md` §0):** All new types sit inside existing assemblies respecting their exact reference graph (`CombatAudioHub`/`ProceduralSfxLibrary`/`SfxId` in `Core`; `CameraFollowEmitter2D` in `CameraSystem`; `PlayerWallClingState` in `Player`; `PauseMenuController` in `NhaGiamGiengChuong`).
- **Zero Manual `.meta` Edits (`AGENTS.md` §2):** Never create, edit, or delete `.meta` files manually.
- **Low-RAM Batchmode Gate Protocol:**
  - Always kill lingering `Unity*`, `bee_backend*`, `AssetImportWorker*` before launching batchmode Unity.
  - Pass `-batchmode -nographics -projectPath "D:/thietkegame" -job-worker-count 1 -disable-assembly-updater`.
  - Split EditMode into **4 separate processes** so Unity never reloads `NhaGiamGiengChuong.unity` more than 6 times in one process:
    1. `Core.Tests.EditMode` (`-assemblyNames Core.Tests.EditMode`)
    2. `NhaGiamGiengChuong` Unit + Geometry (`-assemblyNames NhaGiamGiengChuong.Tests.EditMode -testFilter "!MapWiringTests"`)
    3. `MapWiringTests` Part 1 (`-testFilter "A07|A08|A09|A10"`)
    4. `MapWiringTests` Part 2 (`-testFilter "A11|A12|A13|A14|A15|A16|A17|A18|A19"`)
  - Run PlayMode in its own process (`-testPlatform PlayMode -assemblyNames NhaGiamGiengChuong.Tests.PlayMode`).
- **Locked Regression Contracts (`A07..A16` + PlayMode):**
  - `TP_1_EastVault` stays at `(161.0f, 0.0f)` (`TeleportGateTests` & `ExpansionPlayTests` line 72).
  - `Art_Q_SkeletonSpawn` stays at `(-49.0f, 16.0f)` with its exact name (`A12` line 889, `A14` lines 1151 & 1195).
  - `{ "I11_Scroll_EastCourtyard", "Art_M_WeaponDeco" }` visual sprite bounds remain strictly disjoint (`A14` line 1221).
  - `GroundMist_R5` keeps exactly `6` child `SpriteRenderer`s (`A11` line 817).
  - `Global Light 2D` intensity stays in `[0.35f, 0.48f]` (`0.45f`) and `color.r <= 0.62f` (`A15` line 1370).
  - `FlameLight` / `LanternLight` intensity spread `maxInt - minInt >= 0.40f` and `falloffIntensity >= 0.58f` (`A15` lines 1378–1388).

---

## Review Focus (Top 5 High-Risk Failure Modes Pinned to Owning Task Tests)

1. **Standing bow shot over `Enemy_Rat` or arrow clipping wall corners:** A thicker target cast that also applies to `_ground` would snag platform corners, while a crouch-only fix would still leave standing bow shots missing Rats (`1.4375 > 0.70`). Owning test in **Task 1.1** verifies standing AND crouching bow shots hit `Enemy_Rat` on flat ground AND thin wall raycast clears a `0.05u`-adjacent wall corner.
2. **Simultaneous `[E]` press on `I11_Scroll_EastCourtyard` + `TP_1_EastVault` or collision with `Art_M_WeaponDeco`:** Moving `I11` onto `Art_M_WeaponDeco (153.8)` breaks `A14` (`P0-04c`), while leaving it near `161.0` triggers dual warp + scroll cutscene. Owning test in **Task 1.4** verifies `Art_M_WeaponDeco (152.2)`, `I11 (156.5)`, and `TP_1 (161.0)` are mutually disjoint AND `TeleportGate.TryTeleport()` returns `false` whenever `PowerScrollInteraction.HasFocusedScroll` is true.
3. **Grenade detonation across solid ceiling/wall (`_seeThroughWalls = 1`):** Grenadier can aggro across floors/walls; `Explode()` must verify `!Physics2D.Linecast(center, targetCenter, _straightObstacles)` so bombs never damage the player through solid masonry. Pinned in **Task 1.2**.
4. **Wall-Cling vs Ledge-Mantle oscillation & single-wall infinite climb:** Entering `WallCling` when `TryFindLedge` is valid or re-sticking on frame 1 after a wall-jump breaks platforming. Owning test in **Task 2.2** verifies `TryFindLedge` preempts `WallCling`, wall-jump applies `0.12s` re-grab lock, and `WallJumpsRemaining` caps at `3` per airtime.
5. **`Esc` Pause Menu conflicting with `ScrollUpgradeMenu` or `LevelPhase.Ended`, or `RestartRun()` failing during `LevelPhase.Playing`:** `LevelRunController.TryReplay()` rejects calls outside `LevelPhase.Ended`; `PauseMenuController` must use `LevelRunController.TryRestartFromPause()` and refuse `Esc` whenever `!IsPlaying || RunStats.IsChoosing`. Pinned in **Task 4.2**.

---

# PHASE 1 — P0 Combat, Projectile, AI & Interaction Correctness

### Task 1.1: Fix Player Bow & `RecoverableArrow` (Standing & Crouching Rat Hit, Shield Deflect, OneWay Pass, Wall/Enemy Cleanup)

**Files:**
- Modify: `Assets/_Project/Features/Combat/Scripts/RecoverableArrow.cs:10-143`
- Modify: `Assets/_Project/Features/Combat/Data/Bow_StartBow.asset`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerBow.cs:10-132`
- Modify: `Assets/_Project/Features/Combat/Scripts/HitQuery.cs:10-44`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerCombat.cs:10-58`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerController.cs:40-105`
- Create: `Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`

**Interfaces:**
- Consumes: `Health.TakeDamage(DamageInfo)`, `Health.IsAlive`, `LayerMask.NameToLayer("OneWayPlatform")`.
- Produces:
  - `RecoverableArrow.WallImpactLifetime = 0.6f` (constant), `RecoverableArrow.TargetBoxSize = new Vector2(0.24f, 0.28f)` (static readonly).
  - `RecoverableArrow.Launch(Vector2 origin, Vector2 direction, BowWeaponData data, int damage, LayerMask targets, LayerMask ground, GameObject owner)` (overload accepting normalized `Vector2 direction` alongside existing `int direction` overload so `PlayerBow` can fire with slight downward auto-deflection toward low targets while backward-compatible with existing tests).
  - `RecoverableArrow.IsEmbeddedIn(Health victim)` (`bool`).
  - `PlayerBow.ExtractArrowsFrom(Health victim)` (`int`).
  - `PlayerCombat.TargetHit` (`event Action<Health, DamageInfo>`).

- [ ] **Step 1: Write failing EditMode tests in `Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`**
  - `Bow_StandingAndCrouchingShots_BothHitRatOnFlatGround`: create Player at `(0, 0)` and `Enemy_Rat` (`BoxCollider2D` + `Hurtbox` `off=(0,0.35), size=(1,0.7)`) at `(4, 0)` on `Enemy` layer; call `PlayerBow.TryFire(Vector2.zero, 1, crouching: false, shot)` and step physics → assert standing arrow deflects slightly downward and damages Rat; reset and call with `crouching: true` → assert horizontal arrow at `y = 0.50f` damages Rat.
  - `Arrow_IgnoresOneWayPlatform_ClearsAdjacentWallCorner_AndRecallsFromWallIn06s`: place `OneWayPlatform` across `(2, 0..2)` and a solid `Ground` block with bottom edge at `y = 1.55f` (just `0.05u` above a `y = 1.50f` arrow line) and a solid wall at `x = 6f` → assert arrow passes `OneWayPlatform`, does NOT snag the `y = 1.55f` wall corner (thin ground raycast), embeds into `x = 6f` wall (`WorldImpact`), and recalls within `0.65s`.
  - `Arrow_BlockedByShield_RecallsImmediately_WithoutEmbedding`: target `Health.DamageFilter = _ => false` → fire arrow → assert `TakeDamage` returns `false`, arrow is inactive (`!arrow.gameObject.activeSelf`), and `PlayerBow.Ammo == PlayerBow.Data.AmmoCapacity`.
  - `Arrow_EmbeddedOnEnemy_FlipsWithVictim_AndRecallsOnMeleeExtractOrVictimDeactivate`: embed arrow into alive enemy (`offset.x < 0`), flip enemy `SpriteRenderer.flipX = true` and tick `Update()` → assert arrow world X is now `> enemy.position.x` (`offset.x` mirrored); call `PlayerBow.ExtractArrowsFrom(enemyHealth)` → assert arrow recalled and ammo incremented; embed second arrow, set `enemy.gameObject.SetActive(false)` and tick `Update()` → assert recalled.

- [ ] **Step 2: Run EditMode test batch to confirm FAIL**
  - Run: `rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/thietkegame' -runTests -testPlatform EditMode -assemblyNames Core.Tests.EditMode -testFilter Round8Phase1CombatAiTests -testResults 'Logs/r8-p1-t1-red.xml' -logFile 'Logs/r8-p1-t1-red.log' -job-worker-count 1 -disable-assembly-updater"`
  - Expected: FAIL on all 4 tests.

- [ ] **Step 3: Implement `RecoverableArrow`, `Bow_StartBow.asset`, `PlayerBow`, `HitQuery`, `PlayerCombat`, `PlayerController`**
  - In `Bow_StartBow.asset`: set `_embeddedLifetime: 4.5` and `_crouchingMuzzleOffset: {x: 0.8125, y: 0.50}`.
  - In `RecoverableArrow.cs`:
    - Add `public const float WallImpactLifetime = 0.6f;` and `private static readonly Vector2 TargetBoxSize = new Vector2(0.24f, 0.28f);`.
    - In `Launch(...)`: strip `OneWayPlatform` bit from `ground` (`int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform"); int oneWayBit = oneWayLayer >= 0 ? (1 << oneWayLayer) : (1 << 9); LayerMask solidGround = ground.value & ~oneWayBit;`). Store separate `_groundFilter` (`solidGround`, `useTriggers = false`) and `_targetFilter` (`targets`, `useTriggers = true`).
    - In `FixedUpdate()`:
      1. Cast thin `Physics2D.Raycast(origin, dir, _groundFilter, _hits, dist)` to find `nearestGroundDist`.
      2. Cast `Physics2D.BoxCast(origin, TargetBoxSize, 0f, dir, _targetFilter, _hits, dist)` to find `nearestTarget` (`distance <= nearestGroundDist` and `collider.gameObject != _damage.Source`).
      3. If `nearestTarget` hits a `Hurtbox` with `Health health`:
         - `bool damaged = health.TakeDamage(_damage);`
         - `if (!damaged || !health.IsAlive) { if (damaged) HitDealt?.Invoke(_damage); Recall(); return; }`
         - `HitDealt?.Invoke(_damage);`
         - Record `_victim = health`, `_victimSprite = health.GetComponentInChildren<SpriteRenderer>()`, `_victimInitialFlipX = _victimSprite != null && _victimSprite.flipX`, `_victimOffset = (Vector2)transform.position - (Vector2)health.transform.position`, `_remainingLifetime = _embeddedLifetime`, `_phase = Phase.Embedded`.
      4. Else if `nearestGround` hit: move to `groundHit.point`, `_remainingLifetime = Mathf.Min(_remainingLifetime, WallImpactLifetime)`, `_phase = Phase.WorldImpact`.
    - In `Update()`:
      - Check `if (_remainingLifetime <= 0f || (_phase == Phase.Embedded && (_victim == null || !_victim.IsAlive || !_victim.gameObject.activeInHierarchy))) { Recall(); return; }`.
      - When `_phase == Phase.Embedded`: `bool flipped = _victimSprite != null && _victimSprite.flipX != _victimInitialFlipX; Vector2 offset = flipped ? new Vector2(-_victimOffset.x, _victimOffset.y) : _victimOffset; transform.position = (Vector2)_victim.transform.position + offset;`.
  - In `PlayerBow.cs`:
    - Add `ResolveAimDirection(Vector2 origin, Vector2 feet, int facing, bool crouching)`: when `!crouching`, scan `Physics2D.OverlapBoxAll(new Vector2(feet.x + facing * 5.0f, feet.y + 0.65f), new Vector2(9.6f, 1.3f), 0f, _targets)`. If any alive enemy `Hurtbox` lies in front (`(hb.bounds.center.x - origin.x) * facing > 0.2f`) and no enemy in front already overlaps `[origin.y - 0.14f .. origin.y + 0.14f]`, pick the closest low target (`hb.bounds.max.y < origin.y - 0.14f`) with unobstructed line-of-sight (`!Physics2D.Linecast(origin, hb.bounds.center, _solidGround)`) and return `((Vector2)hb.bounds.center - origin).normalized`; otherwise return `Vector2.right * facing`.
    - Add `public int ExtractArrowsFrom(Health victim)` recalling all active arrows where `arrow.IsEmbeddedIn(victim)`.
  - In `HitQuery.cs` & `PlayerCombat.cs` & `PlayerController.cs`:
    - Add optional `Action<IDamageable, DamageInfo> onHit = null` parameter to `HitQuery.Strike(...)`; `PlayerCombat.PerformStrike` passes a callback raising `TargetHit?.Invoke(target as Health, info)`; `PlayerController.Awake` wires `_context.Combat.TargetHit += (victim, _) => { if (victim != null) _bow.ExtractArrowsFrom(victim); };`.

- [ ] **Step 4: Run `Round8Phase1CombatAiTests` to verify PASS**
  - Expected: All 4 tests PASS, 0 console errors.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Combat/ Assets/_Project/Features/Player/Scripts/PlayerBow.cs Assets/_Project/Features/Player/Scripts/PlayerCombat.cs Assets/_Project/Features/Player/Scripts/PlayerController.cs Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`
  - `rtk git commit -m "fix(combat): fix standing/crouching bow vs rat, shield deflect, wall/oneway arrow rules, and melee extraction"`

---

### Task 1.2: Fix Enemy AI (`_solidWallMask`, Lunge Ledge Guard) & Enemy Projectiles (Roll Pass-Through, Circular Wall-Occluded Blast)

**Files:**
- Modify: `Assets/_Project/Features/Enemies/Scripts/EnemyContext.cs:17-167`
- Modify: `Assets/_Project/Features/Enemies/Scripts/States/Attacks/EnemyLungeAttackState.cs:17-39`
- Modify: `Assets/_Project/Features/Enemies/Scripts/Projectile.cs:146-173`
- Modify: `Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`

**Interfaces:**
- Consumes: `EnemyContext.CanWalk(int dir)`, `Health.TakeDamage(DamageInfo)`.
- Produces:
  - `EnemyContext` internal `_solidWallMask` (`_groundMask & ~(1 << OneWayPlatform)`): used by `CanSeePlayer` (`L98`), `IsWallAhead` (`L145`), and `CanStandAt` body overlap (`L165`), while `RefreshFrame` (`L61`), `IsGroundAhead` (`L153`), and `CanStandAt` downward floor check (`L163`) keep `_groundMask` so enemies walk and stand on OneWay platforms normally.

- [ ] **Step 1: Append 4 failing EditMode tests to `Round8Phase1CombatAiTests.cs`**
  - `EnemyContext_OneWayPlatform_SupportsStanding_ButDoesNotBlockSightWallOrTeleportOverlap`: verify `RefreshFrame` / `IsGroundAhead` / `CanStandAt` recognize a `OneWayPlatform` beneath feet, while a `OneWayPlatform` at torso height does NOT trigger `IsWallAhead`, does NOT block `CanSeePlayer`, and does NOT block `CanStandAt`.
  - `EnemyLunge_StopsAtPlatformLedge_WhenGroundedOrDescending`: place enemy at ledge with no floor ahead (`IsGroundAhead == false`) → tick `EnemyLungeAttackState.ExecuteFixedTick()` → assert `Velocity.x == 0f`.
  - `EnemyArrow_PassesThroughInvulnerableRollingPlayer`: player `Health.DamageFilter = _ => false` → trigger `Projectile.OnTriggerEnter2D` (`Mode.Straight`) → assert `proj.IsLive == true` and `proj.gameObject.activeSelf == true`.
  - `GrenadeExplode_IsCircular_BlockedBySolidWall_AndAppliesDirectionalKnockback`: test bomb at `(0, 0)` with `blastRadius = 2.9f`:
    - Target A at `(2.5f, 2.5f)` (dist `3.53f > 2.9f`, inside old square box) → assert `0` damage.
    - Target B at `(1.8f, 0f)` behind solid `Ground` wall at `x = 1.0f` → assert `0` damage.
    - Target C at `(-1.8f, 0f)` unobstructed → assert damaged with `LastDamage.Direction == -1` and `LastDamage.Knockback >= 4f`.

- [ ] **Step 2: Run `Round8Phase1CombatAiTests` to confirm FAIL**
  - Expected: FAIL on all 4 new enemy/projectile tests.

- [ ] **Step 3: Implement fixes in `EnemyContext.cs`, `EnemyLungeAttackState.cs`, and `Projectile.cs`**
  - In `EnemyContext.cs`:
    - Add `private readonly LayerMask _solidWallMask;` initialized in constructor as `int ow = LayerMask.NameToLayer("OneWayPlatform"); int owBit = ow >= 0 ? (1 << ow) : (1 << 9); _solidWallMask = groundMask.value & ~owBit;`.
    - Replace `_groundMask` with `_solidWallMask` in `CanSeePlayer` (`L98`), `IsWallAhead` (`L146`), and `CanStandAt` `OverlapBox` (`L165`).
  - In `EnemyLungeAttackState.cs`:
    - In `ExecuteFixedTick()`: `bool canAdvance = ExecuteTime < Skill.TravelDuration && !Ctx.IsWallAhead(Ctx.Facing) && (Ctx.IsGroundAhead(Ctx.Facing) || (!Ctx.IsGrounded && Ctx.Velocity.y > 0.5f && Ctx.CanStandAt(Ctx.Position + new Vector2(Ctx.Facing * 0.6f, 0f)))); Ctx.SetHorizontalVelocity(canAdvance ? _speedX : 0f);`.
  - In `Projectile.cs`:
    - In `OnTriggerEnter2D` (`L155-L157`): `bool hit = hurtbox.Target.TakeDamage(new DamageInfo(_damage, dir, _knockback, 0f, false, _owner, isProjectile: true)); if (hit) Finish();`.
    - In `Explode()` (`L164-L172`): query `Physics2D.OverlapCircle(center, _blastRadius, filter, _results)` using preallocated `Collider2D[16]` and `HashSet<IDamageable>`; for each valid `Hurtbox` target, check `Vector2 targetPos = _results[i].bounds.center; if (Physics2D.Linecast(center, targetPos, _straightObstacles)) continue; int dir = targetPos.x >= center.x ? 1 : -1; target.TakeDamage(new DamageInfo(_damage, dir, 5f, 0f, false, _owner, isProjectile: true));`.

- [ ] **Step 4: Run `Round8Phase1CombatAiTests` + `ProjectileOneWayPlatformTests` to verify PASS**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Enemies/Scripts/ Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`
  - `rtk git commit -m "fix(enemies): exclude OneWay from wall/sight mask, guard lunge ledges, fix arrow roll-pass and circular wall-occluded blast"`

---

### Task 1.3: Fix Player Movement/Combat Edge Cases (Roll Cancel `0.55`, Bow Jump Hold, Swap Exploit, Stomp, Crouch/Gamepad Bindings)

**Files:**
- Modify: `Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerBowAttackState.cs:10-122`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerAttackState.cs:20-84`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerStompState.cs:28-45`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerCombat.cs:60-73`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerController.cs:147-170`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerInputReader.cs:14-65`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerContext.cs:96-105`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerStateBase.cs:65-70`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerCrouchState.cs`
- Modify: `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`
- Modify: `Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`

**Interfaces:**
- Consumes: `PlayerMovementConfig.RollCancelNormalizedTime`, `PlayerMovementConfig.JumpHoldBoost`, `PlayerMovementConfig.JumpHoldTime`, `PlayerRunStats.ScaleDamage`.
- Produces:
  - `PlayerInputReader.CrouchHeld` (`bool`), `PlayerContext.IsCrouchHeld` (`bool => IsDownHeld || Input.CrouchHeld`).
  - `PlayerAttackState.CanWeaponSwapCancel` (`bool => _hitDone`).

- [ ] **Step 1: Append 5 failing EditMode tests to `Round8Phase1CombatAiTests.cs`**
  - `RollConfig_HasRollCancelNormalizedTime055`: load `PlayerMovementConfig.asset` → assert `Mathf.Approximately(config.RollCancelNormalizedTime, 0.55f)`.
  - `BowAttackState_HeldJump_AppliesFullJumpHoldBoost`: enter `PlayerBowAttackState` grounded, buffer jump + hold jump across `FixedTick` for `JumpHoldTime` → assert `Velocity.y >= Config.JumpBaseVelocity + Config.JumpHoldBoost * 0.8f`.
  - `AttackState_BlocksPreHitWeaponSwapCancel_AndAllowsPostHitJumpCancel`: enter `PlayerAttackState`, verify `CanWeaponSwapCancel == false` before `_hitTime` and `true` after `_hitTime`; verify pressing Jump after `_hitDone && normalized >= ComboChainNormalizedTime` transitions to `Machine.Jump`.
  - `Stomp_ScalesDamageWithBrutality_AndAllowsRollCancelOnLanding`: invest 1 Brutality scroll → assert `PerformStomp(30, ...)` deals `35` (`RoundToInt(30 * 1.15f)`); after `_landed == true` in `PlayerStompState`, press Roll → assert transition to `Machine.Roll`.
  - `InputActions_CrouchAndGamepadEquipBindings_AreConfigured`: parse `InputSystem_Actions.inputactions` → verify `Crouch` action exists in `Player` map and `EquipSword`/`EquipBow` include `<Gamepad>/dpad/left` and `<Gamepad>/dpad/right`.

- [ ] **Step 2: Run `Round8Phase1CombatAiTests` to confirm FAIL**
  - Expected: FAIL on all 5 tests.

- [ ] **Step 3: Implement fixes**
  - `PlayerMovementConfig.asset`: change `_rollCancelNormalizedTime: 0` → `_rollCancelNormalizedTime: 0.55`.
  - `PlayerBowAttackState.cs`: add `private float _jumpHoldTimer; private bool _holdingJump;`. In `Enter()` reset `_holdingJump = false; _jumpHoldTimer = 0f;`. In `TickJumpAndLanding()`: when `Ctx.StartJump()` fires, set `_holdingJump = true; _jumpHoldTimer = 0f;`; when `Ctx.StartAirJump()` fires or `!Ctx.Input.JumpHeld`, set `_holdingJump = false;`. In `FixedTick()`: if `_holdingJump && _jumpHoldTimer < Ctx.Config.JumpHoldTime`, add `float step = Mathf.Min(Time.fixedDeltaTime, Ctx.Config.JumpHoldTime - _jumpHoldTimer); Ctx.AddVerticalVelocity(Ctx.Config.JumpHoldBoost * step / Ctx.Config.JumpHoldTime); _jumpHoldTimer += step;`.
  - `PlayerAttackState.cs`: add `public bool CanWeaponSwapCancel => _hitDone;`. In `Tick()` right after `if (_nextQueued && _hitDone && normalized >= Ctx.Config.ComboChainNormalizedTime)` (`L79`): if `_hitDone && normalized >= Ctx.Config.ComboChainNormalizedTime && Ctx.Input.JumpPressed && !Ctx.IsDownHeld`: if `Ctx.IsGrounded || Ctx.WithinCoyoteTime` → `Machine.ChangeState(Machine.Jump); return;`, else if `TryAirJump()` → `return;`.
  - `PlayerController.cs`: in `UpdateWeaponSelection()` (`L162`), if `_machine.Current is PlayerAttackState attackState && !attackState.CanWeaponSwapCancel`, return without calling `ForceChangeState(next)`.
  - `PlayerCombat.cs` & `PlayerStompState.cs`: in `PerformStomp`, compute `int scaledDamage = _stats != null ? _stats.ScaleDamage(damage, MeleeWeapon != null ? MeleeWeapon.ScalingStat : CombatStat.Brutality) : damage;`. In `PlayerStompState.Tick()`, when `_landed == true`, check `if (TryRoll()) return;` before incrementing `_landTimer`.
  - `PlayerInputReader.cs`, `PlayerContext.cs`, `PlayerStateBase.cs`, `PlayerCrouchState.cs`, `InputSystem_Actions.inputactions`: bind optional `_crouch = map.FindAction("Crouch", throwIfNotFound: false)` in `PlayerInputReader`, set `CrouchHeld = _crouch != null && _crouch.IsPressed()`, expose `IsCrouchHeld => IsDownHeld || Input.CrouchHeld` on `PlayerContext`, use `Ctx.IsCrouchHeld` in `TryCrouch()` and `PlayerCrouchState`, and add `<Gamepad>/dpad/left` (`EquipSword`) + `<Gamepad>/dpad/right` (`EquipBow`) to `InputSystem_Actions.inputactions`.

- [ ] **Step 4: Run `Round8Phase1CombatAiTests` to verify PASS**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Player/ Assets/_Project/Settings/Input/InputSystem_Actions.inputactions Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`
  - `rtk git commit -m "fix(player): set roll cancel 0.55, add bow jump hold-boost, block swap-cancel exploit, scale stomp, wire crouch/gamepad"`

---

### Task 1.4: Fix EncounterZone Full-Room Bounds (`B1`, `J1`, `E1`) & Resolve East Vault `[E]` Overlap (`I11_Scroll`, `Art_M_WeaponDeco`, `TP_1_EastVault`)

**Files:**
- Modify: `Assets/_Project/Features/Player/Scripts/Progression/PowerScrollInteraction.cs:8-25`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/TeleportGate.cs:278-284`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs:956, 1853-1856, 2318`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs:167-178`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/EncounterZoneTests.cs:45-51`

**Interfaces:**
- Consumes: `PowerScrollInteraction`, `TeleportGate`, `EncounterZone`.
- Produces:
  - `PowerScrollInteraction.HasFocusedScroll` (`bool => _focused != null && _focused.IsAvailable`).
  - `TeleportGate.IsInputBlocked()` returns `true` when `p.RunStats.BlocksGameplayInput` OR `p.GetComponent<PowerScrollInteraction>()?.HasFocusedScroll == true` (or serialized/resolved `PowerScrollInteraction` has focused scroll).
  - Exact collision-verified East Vault positions:
    - `Art_M_WeaponDeco`: `x = 152.2f, y = 0f` (`2.2u` east of `SD_1_EastVault` barrier at `150.0f`).
    - `I11_Scroll_EastCourtyard` (and `ScrollAura_R5_0`): `x = 156.5f, y = 0f` (`4.3u` east of `Art_M_WeaponDeco`, `4.5u` west of `TP_1_EastVault`).
    - `TP_1_EastVault`: `x = 161.0f, y = 0f` (unchanged!).
  - Full-room `EncounterZone` bounds:
    - `Zone_B1`: `min = (20f, 0f), max = (42f, 6f)` (contains `Enemy_Zombie_B` at `(29, 0)`).
    - `Zone_J1`: `min = (-16f, 16f), max = (6f, 22.5f)` (contains `Enemy_Shield_J` at `(-4, 16)`).
    - `Zone_E1`: `min = (-16f, 24f), max = (22f, 31f)` (contains `Enemy_Zombie_E` `(-9, 24)` & `Enemy_Archer_E` `(10, 24)`).

- [ ] **Step 1: Update `EncounterZoneTests.cs` & `MapWiringTests.cs` assertions**
  - Update `B1`, `J1`, `E1` rects in `EncounterZoneTests.cs#L46-L49` and `MapWiringTests.cs#L167-L170`.
  - In `MapWiringTests.cs` (`A07`), add assertions that: (a) every one of the 30 enemies has its spawn `(x, y)` inside its assigned `EncounterZone` (`zone.ContainsPoint(enemy.transform.position)`), and (b) `I11_Scroll_EastCourtyard` and `TP_1_EastVault` are `>= 4.0f` units apart (`Vector2.Distance >= 4.0f`).
  - In `Round8Phase1CombatAiTests.cs`, add `TeleportGate_WhenPowerScrollIsFocused_BlocksTeleportOnSameFrameEPress`.

- [ ] **Step 2: Run `EncounterZoneTests` & `MapWiringTests A07` to confirm FAIL**
  - Expected: FAIL on `B1/J1/E1` bounds and `I11/TP_1` `2.20u < 4.0u` distance.

- [ ] **Step 3: Implement `PowerScrollInteraction.HasFocusedScroll`, `TeleportGate` mutual exclusion, and `NhaGiamGiengChuongBuilder.cs` coordinates**
  - In `PowerScrollInteraction.cs`: add `public bool HasFocusedScroll => _focused != null && _focused.IsAvailable;`.
  - In `TeleportGate.cs`: in `IsInputBlocked()`, also check if `p.TryGetComponent<PowerScrollInteraction>(out var psi) && psi.HasFocusedScroll` (or `p.GetComponentInChildren<PowerScrollInteraction>()`) or any `PowerScrollPickup` in scene has `TryGetDistance(p.transform.position, out _)`.
  - In `NhaGiamGiengChuongBuilder.cs`:
    - Line `956`: change `Art_M_WeaponDeco` X from `153.8f` → `152.2f`.
    - Lines `1853, 1855, 1856`: update `Zone_B1` to `(20f, 0f)..(42f, 6f)`, `Zone_J1` to `(-16f, 16f)..(6f, 22.5f)`, `Zone_E1` to `(-16f, 24f)..(22f, 31f)`.
    - Line `2318`: change `I11_Scroll_EastCourtyard` X from `158.8f` → `156.5f`.

- [ ] **Step 4: Run `NhaGiamGiengChuongBuilder.BuildAll` + `EncounterZoneTests` + `MapWiringTests A07..A16`**
  - Expected: All PASS (including `A14` `P0-04c` disjoint check between `I11_Scroll_EastCourtyard` and `Art_M_WeaponDeco`).

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Player/Scripts/Progression/PowerScrollInteraction.cs Assets/_Project/Features/NhaGiamGiengChuong/ Assets/_Project/Scenes/NhaGiamGiengChuong.unity Assets/_Project/Core/Tests/EditMode/Round8Phase1CombatAiTests.cs`
  - `rtk git commit -m "fix(nggc): expand EncounterZones B1/J1/E1 to full rooms and resolve I11/TP_1 interact overlap"`

---

### Task 1.5: Add `CameraFollow2D.SnapToTarget()` on Teleport & Locked/Opened Feedback on Interactables

**Files:**
- Modify: `Assets/_Project/Features/CameraSystem/Scripts/CameraFollow2D.cs:47-84`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/TeleportGate.cs:20-210`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/HealingChest.cs:20-206`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SwitchDoor.cs:15-160`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SecondaryBiomeExitTrigger.cs:15-145`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs:1872-2200`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/TeleportGateTests.cs`, `HealingChestTests.cs`, `SecondaryBiomeExitTests.cs`

**Interfaces:**
- Consumes: `CameraFollow2D`, `VfxSpawner`, `Vfx_CollectedGlint.asset`, `Props/StorageAndVessel_004` (open/empty vessel sprite).
- Produces:
  - `CameraFollow2D.SnapToTarget()` (`public void`):
    ```csharp
    public void SnapToTarget()
    {
        if (_target == null) return;
        if (_camera == null) _camera = GetComponent<Camera>();
        _focus = (Vector2)_target.position + _offset;
        _lastTargetPosition = _target.position;
        _lookAheadCurrent = 0f;
        _lookAheadVelocity = 0f;
        _velocity = Vector3.zero;
        var desired = new Vector3(_focus.x, _focus.y, transform.position.z);
        if (_useBounds && _camera != null) desired = ClampToBounds(desired);
        _basePosition = desired;
        transform.position = _basePosition + ShakeOffset();
    }
    ```
  - `TeleportGate`: `[SerializeField] private CameraFollow2D _camera;`, `[SerializeField] private GameObject _lockedHintIndicator;`, `public bool LockedHintVisible => _lockedHintVisible;`.
  - `HealingChest`: `[SerializeField] private SpriteRenderer _visualRenderer;`, `[SerializeField] private Sprite _openedSprite;`, `[SerializeField] private Color _openedTint = new Color(0.55f, 0.58f, 0.62f, 1f);`, `[SerializeField] private VfxSpawner _vfx;`, `[SerializeField] private VfxDefinition _healVfx;`.
  - `SwitchDoor`: `[SerializeField] private CameraFollow2D _camera;`, `[SerializeField] private VfxSpawner _vfx;`, `[SerializeField] private VfxDefinition _openVfx;`.
  - `SecondaryBiomeExitTrigger`: `[SerializeField] private GameObject _lockedPromptIndicator;`, `public bool LockedPromptVisible => _lockedPromptVisible;`.

- [ ] **Step 1: Write failing EditMode tests in `TeleportGateTests.cs`, `HealingChestTests.cs`, `SecondaryBiomeExitTests.cs`**
  - `TeleportGateTests`: `TryTeleport_SnapsCameraToArrival_AndShowsLockedHintWhenPairNotDiscovered` (assert camera position immediately equals clamped arrival focus + offset with zero `SmoothDamp` lag, and `_lockedHintIndicator.activeSelf == true` when standing in a discovered gate whose pair is undiscovered).
  - `HealingChestTests`: `TryInteract_WhenHealed_SwapsVisualSpriteAndTint` (assert `_visualRenderer.sprite == _openedSprite` and `_visualRenderer.color == _openedTint` after heal).
  - `SecondaryBiomeExitTests`: `LockedSewerExit_ShowsLockedPromptWhenPlayerOverlaps_AndHidesWhenUnlocked` (assert `_lockedPromptIndicator.activeSelf == true` when player collider overlaps trigger while `!IsUnlocked`, and `false` once unlocked or outside).

- [ ] **Step 2: Run `NhaGiamGiengChuong.Tests.EditMode` Unit batch to confirm FAIL**
  - Expected: FAIL on all 3 new tests.

- [ ] **Step 3: Implement `CameraFollow2D.SnapToTarget`, interactable visual/prompt updates, and builder wiring**
  - In `TeleportGate.cs`:
    - In `RefreshVisualState()`: `bool showLockedHint = playingAndAlive && inTrigger && _isDiscovered && !CanTeleport; _lockedHintVisible = showLockedHint; if (_lockedHintIndicator != null) _lockedHintIndicator.SetActive(showLockedHint);`.
    - In `TryTeleport(float currentTime)`: after `Physics2D.SyncTransforms();`, call `_camera?.SnapToTarget();`.
  - In `HealingChest.cs`:
    - In `RefreshVisualState()` / `TryInteract()`: when `_consumed && _visualRenderer != null`, if `_openedSprite != null` set `_visualRenderer.sprite = _openedSprite`, and set `_visualRenderer.color = _openedTint`. On successful heal, call `_vfx?.Play(_healVfx, transform.position + Vector3.up * 0.8f)`.
  - In `SwitchDoor.cs`:
    - On successful `TryInteract()`: call `_camera?.Shake(0.10f, 0.14f);` and `if (_vfx != null && _doorBarrierRoot != null) _vfx.Play(_openVfx, _doorBarrierRoot.transform.position);`.
  - In `SecondaryBiomeExitTrigger.cs`:
    - Add `Update()` + `RefreshVisualState()` checking `bool inTrigger = IsPlayerInTrigger(); _lockedPromptVisible = _run != null && _run.IsPlaying && !IsUnlocked && inTrigger; if (_lockedPromptIndicator != null) _lockedPromptIndicator.SetActive(_lockedPromptVisible);`.
  - In `NhaGiamGiengChuongBuilder.cs`:
    - On `I3_HealingChest`: wire `_visualRenderer = chestVisualSr`, `_openedSprite = LoadTileSprite("Props/StorageAndVessel_004")`, `_vfx = vfxSpawner`, `_healVfx = AssetDatabase.LoadAssetAtPath<VfxDefinition>("Assets/_Project/Features/VFX/Data/Vfx_CollectedGlint.asset")`. (Note: keep `PromptIndicator` at `y = 1.85f` so `A10` `PromptGap >= 0.20f` stays identical!).
    - On all 6 `TeleportGate`s: create child `LockedHintIndicator` at `localPosition = new Vector3(0f, 2.35f, 0f)` via `BuildWorldPromptBadge(lockedGo, "Chưa mở cổng cặp", new Color(0.75f, 0.82f, 0.88f, 1f), 280f)`, `SetActive(false)`, and wire `_camera = camFollow` + `_lockedHintIndicator = lockedGo`.
    - On all 3 `SwitchDoor`s: wire `_camera = camFollow`, `_vfx = vfxSpawner`, `_openVfx = Vfx_CollectedGlint`.
    - On `I15_SewerExitTrigger`: create child `LockedPromptIndicator` at `localPosition = new Vector3(0f, 1.45f, 0f)` via `BuildWorldPromptBadge(lockedGo, "Khóa: Cần mở công tắc & diệt 2 quái canh", new Color(1f, 0.68f, 0.42f, 1f), 420f)`, `SetActive(false)`, and wire `_lockedPromptIndicator = lockedGo`.

- [ ] **Step 4: Run `BuildAll` + all 4 EditMode batches + PlayMode suite**
  - Expected: 100% PASS, 0 console errors.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/CameraSystem/ Assets/_Project/Features/NhaGiamGiengChuong/ Assets/_Project/Scenes/NhaGiamGiengChuong.unity`
  - `rtk git commit -m "feat(nggc): snap camera on teleport and add locked/opened feedback on chest, doors, teleporters, and sewer exit"`

### PHASE 1 REVIEW GATE (STOP & REVIEW BEFORE PHASE 2)
- [ ] Run all 4 EditMode batches + PlayMode batch (`100% PASS, 0 console errors`).
- [ ] Perform full Phase 1 Review (or subagent reviewer check) against `M-08`, `A-01`, `A-03..A-12`, `A-16b`, and Review Focus #1–#3.
- [ ] Resolve any findings before starting Phase 2.

---

# PHASE 2 — Dead Cells 60fps Hack-and-Slash Game Feel, Wall-Cling/Ledge & Chain Climb

### Task 2.1: Fix Chain Climb Hand Alignment (`M-06`)

**Files:**
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerMovementConfig.cs:105-108, 179`
- Modify: `Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerContext.cs:319-332`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerClimbState.cs:14-61`
- Create: `Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.cs`

**Interfaces:**
- Consumes: `PlayerContext.ClimbTarget`, `PlayerContext.FacingDirection`.
- Produces:
  - `PlayerMovementConfig.ClimbHandOffsetX` (`float`, serialized `_climbHandOffsetX = 0.24f`).
  - `PlayerContext.AlignToClimbTarget()` (`public void`): sets `_body.position = new Vector2(ClimbTarget.bounds.center.x - FacingDirection * Config.ClimbHandOffsetX, _body.position.y);`.

- [ ] **Step 1: Write failing EditMode test in `Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.cs`**
  - `ChainClimb_AlignsHandsToChainCenter_AndFlipsSideWhenTurningOnChain`: place `Climbable` trigger (`size=(0.5, 9)`) at `x = 10f`; face `+1` and call `BeginClimb()` → assert `Position.x == 9.76f ± 0.01f` (body shifted left `-0.24u` so forward-reaching right hands at `+0.24u` sit at `x = 10.0f`) AND `TryFindClimbable()` still overlaps the `0.5u` chain trigger (`|9.76 - 10.0| = 0.24 < 0.40 + 0.25`); press left (`MoveX = -1`) in `PlayerClimbState.Tick()` → assert `FacingDirection == -1` and `Position.x == 10.24f ± 0.01f`.

- [ ] **Step 2: Run `Round8Phase2FeelMovementTests` to confirm FAIL**
  - Expected: FAIL (`Position.x == 10f`, `ClimbHandOffsetX` missing).

- [ ] **Step 3: Implement `ClimbHandOffsetX` & `AlignToClimbTarget()`**
  - In `PlayerMovementConfig.cs` & `PlayerMovementConfig.asset`: add `[SerializeField] private float _climbHandOffsetX = 0.24f;` and `public float ClimbHandOffsetX => _climbHandOffsetX;`.
  - In `PlayerContext.cs`: add `public void AlignToClimbTarget() { if (ClimbTarget != null) _body.position = new Vector2(ClimbTarget.bounds.center.x - FacingDirection * Config.ClimbHandOffsetX, _body.position.y); }` and call it inside `BeginClimb()`.
  - In `PlayerClimbState.cs`: in `Tick()`, if `Ctx.HasMoveInput`, check `int inputDir = Ctx.Input.MoveX > 0f ? 1 : -1; if (inputDir != Ctx.FacingDirection) { Ctx.Face(inputDir); Ctx.AlignToClimbTarget(); }`.

- [ ] **Step 4: Run `Round8Phase2FeelMovementTests` to verify PASS**
  - Expected: PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Player/ Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.cs`
  - `rtk git commit -m "fix(player): offset chain climb body X by ClimbHandOffsetX so hands grip chain links"`

---

### Task 2.2: Implement Wall-Cling / Wall-Slide / Wall-Jump (`PlayerWallClingState`) & Widen Ledge Mantle Window (`M-09`)

**Files:**
- Create: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerWallClingState.cs`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerMovementConfig.cs` + `PlayerMovementConfig.asset`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerContext.cs:20-95, 140-210, 355-433`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/PlayerStateMachine.cs:14-53`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerStateBase.cs:45-58`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerJumpState.cs:46-56`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerFallState.cs:28-33`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerAnimatorBridge.cs:10-95`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (add `EnsurePlayerWallClingAnimation()` creating `Assets/_Project/Features/Player/Animations/Player_WallCling.anim` from `wallClimbA_00..11.png` and wiring `WallClinging` bool transition in `Player.controller` via `AnimatorController` API)
- Modify: `Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.cs`

**Interfaces:**
- Consumes: `Assets/_Project/Art/Characters/Player/Sprites/wallClimbA_00.png` .. `wallClimbA_11.png` (already in repo at `24 PPU`).
- Produces:
  - `PlayerMovementConfig`:
    - `WallClingEnabled` (`bool = true`)
    - `WallSlideMaxSpeed` (`float = 2.8f`)
    - `WallJumpVelocity` (`Vector2 = new Vector2(9.5f, 13.2f)`)
    - `MaxWallJumpsPerAir` (`int = 3`)
    - `WallCheckDistance` (`float = 0.14f`)
    - `WallJumpHorizontalLockTime` (`float = 0.12f`)
    - `WallRegrabCooldown` (`float = 0.14f`)
    - `OneWayLedgeGrabTolerance` (`float = 0.42f` in `PlayerMovementConfig.asset`)
  - `PlayerContext`:
    - `WallJumpsRemaining` (`int`, refilled to `Config.MaxWallJumpsPerAir` in `RefillAirMoves()`)
    - `WallDirection` (`int`, `-1` or `+1`)
    - `IsWallJumpHorizontalLocked` (`bool => Time.time < _wallJumpLockUntil`)
    - `TryFindWall(out int wallDir)` (`bool`)
    - `StartWallJump(int awayDir)` (`void`, sets velocity `(awayDir * Config.WallJumpVelocity.x, Config.WallJumpVelocity.y)`, faces `awayDir`, decrements `WallJumpsRemaining`, sets `_wallJumpLockUntil = Time.time + Config.WallJumpHorizontalLockTime` and `_wallRegrabReadyAt = Time.time + Config.WallRegrabCooldown`, invokes `WallJumped?.Invoke(Position, awayDir)`)
    - `WallJumped` (`event Action<Vector2, int>`)
  - `PlayerStateMachine.WallCling` (`IPlayerState`)
  - `PlayerAnimatorBridge.SetWallClinging(bool clinging)` (`void`, sets `WallClinging` bool on Animator; safe if parameter absent in unit test stubs).

- [ ] **Step 1: Append 3 failing EditMode tests to `Round8Phase2FeelMovementTests.cs`**
  - `WallCling_GripsSolidWall_SlidesAt28_WallJumpsAway_AndEnforcesRegrabLockAnd3JumpCap`:
    - Place vertical `Ground` wall at `x = 1.0f`; airborne player at `(0.52f, 5.0f)` holding `MoveX = +1f`, `Velocity.y = -6f` → assert `TryFindWall(out int dir)` is `true` (`dir == 1`), `TryAirActions()` transitions to `Machine.WallCling`, `FixedTick()` clamps `Velocity.y >= -2.8f`.
    - Press Jump → assert `StartWallJump(-1)` sets `Velocity == (-9.5f, 13.2f)`, `FacingDirection == -1`, `WallJumpsRemaining == 2`, `IsWallJumpHorizontalLocked == true`, and `TryFindWall` returns `false` during `WallRegrabCooldown`.
    - Exhaust all 3 wall jumps without touching ground → assert 4th wall jump is blocked (`WallJumpsRemaining == 0`).
  - `WallCling_IgnoresOneWayPlatforms_AndYieldsPriorityToLedgeClimb`:
    - Place `OneWayPlatform` adjacent to player → assert `TryFindWall == false`.
    - Place solid ledge top at `feetY + 1.25f` (`TryFindLedge == true`) → assert `TryAirActions()` enters `Machine.LedgeClimb` (NOT `WallCling`).
  - `LedgeClimb_With042Tolerance_CatchesOneWayEdgeAtMaxFallSpeed`:
    - Verify `PlayerMovementConfig.asset` has `OneWayLedgeGrabTolerance == 0.42f`, and `TryFindLedge` catches a `OneWayPlatform` edge across a `0.33u` single-frame drop at `-20 u/s` (`60fps`), and also catches when `HasMoveInput` OR horizontal velocity `Velocity.x * FacingDirection > 0.5f`.

- [ ] **Step 2: Run `Round8Phase2FeelMovementTests` to confirm FAIL**
  - Expected: FAIL (`WallCling`, `TryFindWall`, `StartWallJump` undefined).

- [ ] **Step 3: Implement `PlayerWallClingState`, `PlayerContext` wall/ledge probes, `PlayerStateBase` priority, and `Player_WallCling.anim`**
  - In `PlayerContext.cs`:
    - Derive `_solidWallMask = _groundMask.value & ~(1 << (LayerMask.NameToLayer("OneWayPlatform") >= 0 ? LayerMask.NameToLayer("OneWayPlatform") : 9));`.
    - In `TryFindLedge`: allow triggering when `(HasMoveInput && Mathf.Sign(Input.MoveX) == FacingDirection) || (!HasMoveInput && Velocity.x * FacingDirection > 0.5f)` so releasing the key 1 frame before the edge still mantles!
    - Implement `TryFindWall(out int wallDir)`:
      ```csharp
      wallDir = 0;
      if (!Config.WallClingEnabled || IsGrounded || Time.time < _wallRegrabReadyAt || Velocity.y > 2.0f || IsDownHeld)
          return false;
      int inputDir = Mathf.Abs(Input.MoveX) > 0.1f ? (Input.MoveX > 0f ? 1 : -1) : 0;
      if (inputDir == 0)
          return false;
      var b = _collider.bounds;
      float dist = b.extents.x + Config.WallCheckDistance;
      var dir = new Vector2(inputDir, 0f);
      var lowHit = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.45f), dir, dist, _solidWallMask);
      var highHit = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 1.25f), dir, dist, _solidWallMask);
      if (lowHit.collider == null || highHit.collider == null)
          return false;
      if (Mathf.Abs(lowHit.normal.x) < 0.9f || Mathf.Abs(highHit.normal.x) < 0.9f)
          return false;
      wallDir = inputDir;
      WallDirection = inputDir;
      return true;
      ```
  - In `PlayerStateBase.cs`:
    - Update `TryAirActions()` in strict priority order:
      `1. Stomp → 2. Air Attack → 3. Roll → 4. Climb → 5. LedgeClimb → 6. TryWallCling() → 7. AirJump`.
    - Add `protected bool TryWallCling() => Ctx.TryFindWall(out _) && Machine.ChangeState(Machine.WallCling);`.
    - In `ApplyAirControl()`: if `Ctx.IsWallJumpHorizontalLocked`, keep current `Ctx.Velocity.x` instead of overwriting with `Input.MoveX * AirSpeed` so wall-jumps always kick cleanly away from the wall even while the player holds the key toward the wall!
  - Implement `PlayerWallClingState.cs`:
    - `Enter()`: `Ctx.Face(Ctx.WallDirection); Ctx.SetHorizontalVelocity(0f); if (Ctx.Velocity.y < -Ctx.Config.WallSlideMaxSpeed) Ctx.SetVerticalVelocity(-Ctx.Config.WallSlideMaxSpeed); Ctx.Animator.SetWallClinging(true);`
    - `Exit()`: `Ctx.Animator.SetWallClinging(false); Ctx.SetGravityScale(Ctx.Config.GravityScale);`
    - `Tick()`:
      - If `Ctx.IsGrounded` → `ReturnToDefault(); return;`
      - If `Ctx.Input.JumpPressed && !Ctx.IsDownHeld && Ctx.WallJumpsRemaining > 0`: `Ctx.StartWallJump(-Ctx.WallDirection); Machine.ChangeState(Machine.Jump); return;` (note: in `PlayerJumpState.Enter()`, if `Ctx.IsWallJumpHorizontalLocked`, skip `ApplyJumpImpulse()` so `StartWallJump`'s `(±9.5, 13.2)` impulse is preserved!).
      - If `TryRoll() || TryLedgeClimb()` → `return;`
      - If `Ctx.IsDownHeld || !Ctx.TryFindWall(out int dir) || dir != Ctx.WallDirection`: `Machine.ChangeState(Machine.Fall); return;`
    - `FixedTick()`: `Ctx.SetHorizontalVelocity(0f); if (Ctx.Velocity.y < -Ctx.Config.WallSlideMaxSpeed) Ctx.SetVerticalVelocity(-Ctx.Config.WallSlideMaxSpeed);`
  - Add `EnsurePlayerWallClingAnimation()` in `NhaGiamGiengChuongBuilder.cs` (called at the start of `BuildAll`): generates `Assets/_Project/Features/Player/Animations/Player_WallCling.anim` from `wallClimbA_00..11.png` (`12 fps`, loop) if missing, adds `WallClinging` bool parameter and `WallCling` state (`AnyState -> WallCling` when `WallClinging && !Dead && !Hurt`, `WallCling -> Fall` when `!WallClinging`) to `Player.controller`.

- [ ] **Step 4: Run `Round8Phase2FeelMovementTests` + all `Core.Tests.EditMode` to verify PASS**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Player/ Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.cs`
  - `rtk git commit -m "feat(player): add WallCling/WallJump state with wallClimbA animation and widen OneWay ledge tolerance"`

---

### Task 2.3: Dead Cells 60fps Combat Cadence, Tiered Hitstop, Directional Camera Punch & Shield/Perfect-Dodge Feel (`M-07`, `A-12`)

**Files:**
- Modify: `Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerCombat.cs:25-73`
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerAttackState.cs:64-75`
- Modify: `Assets/_Project/Features/CameraSystem/Scripts/CameraFollow2D.cs:35-107`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerController.cs:190-202`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerFeedback.cs:13-95`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs:1650-1662` (`CameraFollow2D` `_smoothTime = 0.09f`, `_deadzone = (0.55f, 0.35f)`)
- Modify: `Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.cs`

**Interfaces:**
- Consumes: `HitStop.Trigger(float)`, `PlayerContext.PerfectDodged`, `WeaponData.Strike.CanCrit`.
- Produces:
  - `CameraFollow2D.Punch(Vector2 direction, float amplitude, float duration)` (`public void`, directional kick decaying quadratically `(1 - t)^2` over unscaled time, added in `LateUpdate` alongside `ShakeOffset()`).
  - `CameraFollow2D.CurrentPunchOffset` (`Vector3`, for deterministic unit test inspection).
  - `PlayerCombat.ImpactDealt` (`event Action<int, bool, bool, int>` — `hits, isCritical, anyFatal, direction`, keeping `StrikeHit` intact).
  - `PlayerController.Staggered` (`event Action<int>` — `direction`, raised in `PlayerController.Stagger(float, int)`).

- [ ] **Step 1: Append 3 failing EditMode tests to `Round8Phase2FeelMovementTests.cs`**
  - `StartSwordAsset_HasDeadCellsCadence_Knockback_AndFinisherCrit`: load `Weapon_StartSword.asset` → assert Strike 0 (`AnimSpeed == 1.25f, Knockback == 3.5f, CanCrit == false`), Strike 1 (`AnimSpeed == 1.35f, Knockback == 4.5f, CanCrit == false`), Strike 2 (`AnimSpeed == 0.95f, Knockback == 8.0f, CanCrit == true`).
  - `CameraFollow2D_Punch_ProducesDirectionalKickInPunchDirection`: call `cameraFollow.Punch(Vector2.right, 0.24f, 0.12f)` → assert `cameraFollow.CurrentPunchOffset.x > 0.15f` and `Mathf.Approximately(cameraFollow.CurrentPunchOffset.y, 0f)`.
  - `PlayerFeedback_TiersHitstopAndPunch_ForLightCritKillStaggerAndPerfectDodge`: trigger light hit, critical hit, fatal kill, `PlayerController.Stagger`, and `PlayerContext.TryRegisterPerfectDodge` → assert `HitStop.IsPaused == true` and `CameraFollow2D.CurrentPunchOffset.sqrMagnitude > 0f` on directional impacts.

- [ ] **Step 2: Run `Round8Phase2FeelMovementTests` to confirm FAIL**
  - Expected: FAIL (`Knockback == 0`, `Punch` missing, `Staggered` missing).

- [ ] **Step 3: Implement 60fps combat feel upgrades**
  - In `Weapon_StartSword.asset`:
    - Strike 0 (`AttackA`): `Damage: 25`, `HitFrame: 5`, `AnimSpeed: 1.25`, `Knockback: 3.5`, `CanCrit: 0`
    - Strike 1 (`AttackB`): `Damage: 30`, `HitFrame: 7`, `AnimSpeed: 1.35`, `Knockback: 4.5`, `CanCrit: 0`
    - Strike 2 (`AttackC`): `Damage: 45`, `HitFrame: 9`, `AnimSpeed: 0.95`, `Knockback: 8.0`, `CanCrit: 1`
  - In `PlayerCombat.cs`:
    - Pass `strike.CanCrit` to `new DamageInfo(damage, facing, strike.Knockback, strike.CanCrit ? 0.35f : 0f, strike.CanCrit, _owner.gameObject)`.
    - Track `bool anyFatal = false` inside the `HitQuery.Strike` callback (`if (!target.IsAlive) anyFatal = true;`).
    - When `hits > 0`, invoke `StrikeHit?.Invoke(hits, info.IsCritical);` AND `ImpactDealt?.Invoke(hits, info.IsCritical, anyFatal, facing);`.
  - In `PlayerAttackState.cs`:
    - On the strike frame (`!_hitDone && _timer >= _hitTime`), when `Ctx.IsGrounded`, apply a crisp forward step `Ctx.SetHorizontalVelocity(Ctx.FacingDirection * 3.8f);` before `PerformStrike`.
  - In `CameraFollow2D.cs`:
    - Add `_punchDir`, `_punchAmplitude`, `_punchDuration`, `_punchUntil = -1f`, `public Vector3 CurrentPunchOffset => PunchOffset();`, and `public void Punch(Vector2 direction, float amplitude, float duration)`.
    - In `LateUpdate`: `transform.position = _basePosition + ShakeOffset() + PunchOffset();`.
  - In `PlayerController.cs`:
    - Add `public event Action<int> Staggered;` and invoke `Staggered?.Invoke(direction);` inside `Stagger(float duration, int direction)`.
  - In `PlayerFeedback.cs`:
    - Subscribe to `_combat.ImpactDealt`, `_controller.Staggered`, `_controller.Context.PerfectDodged`:
      - Light hit (`!critical && !fatal`): `hitStop = 0.045s`, `Punch(dir, 0.12f, 0.08f)`, `Shake(0.06f, 0.08f)`.
      - Critical / Finisher (`critical && !fatal`): `hitStop = 0.075s`, `Punch(dir, 0.22f, 0.11f)`, `Shake(0.14f, 0.12f)`.
      - Fatal Kill (`fatal`): `hitStop = 0.095s`, `Punch(dir, 0.26f, 0.13f)`, `Shake(0.18f, 0.14f)`.
      - Arrow Hit (`OnArrowHit`): `hitStop = damage.IsCritical ? 0.065s : 0.035s`, `Punch(dir, 0.10f, 0.07f)`.
      - Player Hurt (`OnDamageTaken`): `hitStop = 0.060s`, `Shake(0.24f, 0.18f)`, white flash `0.10s`.
      - Shield Stagger (`OnStaggered(int dir)`): `hitStop = 0.055s`, `Punch(new Vector2(dir, 0f), 0.18f, 0.10f)`, `Shake(0.10f, 0.10f)`, white flash `0.08s`.
      - Perfect Dodge (`OnPerfectDodged()`): `hitStop = 0.055s`, white flash `0.06s`.
  - In `NhaGiamGiengChuongBuilder.cs`: set `Main Camera` `CameraFollow2D` `_smoothTime = 0.09f`, `_deadzone = new Vector2(0.55f, 0.35f)`.

- [ ] **Step 4: Run `Round8Phase2FeelMovementTests` to verify PASS**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Combat/ Assets/_Project/Features/CameraSystem/ Assets/_Project/Features/Player/ Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs Assets/_Project/Core/Tests/EditMode/Round8Phase2FeelMovementTests.cs`
  - `rtk git commit -m "feat(combat): tune 60fps sword cadence/knockback/crit, add directional camera punch and tiered hitstop"`

---

### Task 2.4: Clone `EnemyImpactFeedback` onto All 30 Enemies, Upgrade `EnemyDeadState`, Wire `PlayerVfx`, `Vfx_HeadSlime` & `VfxSpawner._prewarm` (`M-07`, `A-02`, `A-15`)

**Files:**
- Modify: `Assets/_Project/Features/Enemies/Scripts/States/EnemyDeadState.cs:10-37`
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerContext.cs:65-75, 200-204` (add `event Action<Vector2> StompLanded` + `NotifyStompLanded()`)
- Modify: `Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerStompState.cs:32-38` (call `Ctx.NotifyStompLanded()`)
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerVfx.cs:10-63`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`EnsurePlayerPrefabVfxWiring()`, `VfxSpawner._prewarm` 11 definitions, and cloning `"Enemy_Shield hit feedback"` from `Main_Level.unity` for all 30 enemies)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (add `A17`)

**Interfaces:**
- Consumes: `Main_Level.unity`'s `"Combat and Pickup Feedback"` (`1959056382`) and `"Enemy_Shield hit feedback"` (`163309823`, containing child `"Enemy health"` with `"Outline"`, `"Track"`, `"Left edge/Remaining health"`), `Vfx_HeadSlime.asset`, `Vfx_ContactGlint.asset`, `Vfx_Land.asset`, `11` `Vfx_*.asset` files in `Assets/_Project/Features/VFX/Data/`.
- Produces:
  - `PlayerContext.StompLanded` (`event Action<Vector2>`) + `PlayerContext.NotifyStompLanded()` (`void`).
  - `[Test] A17_Round8Phase2_EnemyImpactFeedbackPrewarmAndPlayerVfxWiring()` in `MapWiringTests.cs`.

- [ ] **Step 1: Add `A17` in `MapWiringTests.cs`**
  - Assert `Object.FindObjectsByType<EnemyImpactFeedback>(FindObjectsInactive.Include).Length == 30`, and every instance has non-null `_target`, `_body`, `_vfx`, `_impactBlood`, `_defeatBlood`, `_impactFlash`, `_audio`, `_hitSound`, `_fatalSound`, `_numberPrefab`, `_bar`, `_fill`, with all bar `SpriteRenderer`s on `"VFX"` sorting layer.
  - Assert `VfxSpawner._prewarm` has `11` non-null `VfxDefinition`s.
  - Assert `Player`'s `PlayerSpawnHead._slimeVfx != null` (`Vfx_HeadSlime`).

- [ ] **Step 2: Run `MapWiringTests A17` to confirm FAIL**
  - Expected: FAIL (`0` `EnemyImpactFeedback`, `0` prewarm, null `_slimeVfx`).

- [ ] **Step 3: Implement `EnemyDeadState`, `PlayerVfx`, `EnsurePlayerPrefabVfxWiring`, `VfxSpawner._prewarm`, and 30× `EnemyImpactFeedback` cloning**
  - In `EnemyDeadState.cs`:
    - Change `FlashTime = 0.07f`, `FadeTime = 0.18f`.
    - Record `_startPos = Ctx.Owner.transform.position`, `_pushDir = Ctx.LastDamage.Direction != 0 ? Ctx.LastDamage.Direction : -Ctx.Facing`.
    - During `FadeTime`: ease position by `_pushDir * 0.35f * t * (2f - t)` and scale by `new Vector3(1f + 0.15f * t, 1f - 0.25f * t, 1f)` (resetting scale to `Vector3.one` before `SetActive(false)`).
  - In `PlayerContext.cs` & `PlayerStompState.cs` & `PlayerVfx.cs`:
    - Add `public event Action<Vector2> StompLanded;` and `public void NotifyStompLanded() => StompLanded?.Invoke(Position);`; call `Ctx.NotifyStompLanded();` in `PlayerStompState.Tick()` when `_landed` transitions to `true`.
    - In `PlayerVfx.cs`: add `[SerializeField] private VfxDefinition _dodgeGlintVfx;`, null-guard `_spawner`, and subscribe to `_context.RollStarted` (plays `_landVfx` at feet with `facing < 0`), `_context.PerfectDodged` (plays `_dodgeGlintVfx != null ? _dodgeGlintVfx : _airJumpVfx` at `_context.PerfectDodgeGhostPosition + Vector2.up * 0.8f`), and `_context.StompLanded` (plays `_landVfx` at `feet + (-0.4f, 0f)` and `feet + (+0.4f, 0f)` + `_dodgeGlintVfx` at `feet + (0f, 0.3f)`).
  - In `NhaGiamGiengChuongBuilder.cs`:
    - Add `EnsurePlayerPrefabVfxWiring()` (using `PrefabUtility.LoadPrefabContents("Assets/_Project/Features/Player/Prefabs/Player.prefab")`): assigns `Vfx_HeadSlime.asset` to `PlayerSpawnHead._slimeVfx` and `Vfx_ContactGlint.asset` to `PlayerVfx._dodgeGlintVfx` if null, saving only when changed.
    - When creating `VfxSpawner`, populate `_prewarm` with all 11 assets in `Assets/_Project/Features/VFX/Data/` (`Vfx_AirJump`, `Vfx_CollectedGlint`, `Vfx_ContactBlood`, `Vfx_ContactGlint`, `Vfx_Explosion`, `Vfx_FatalBlood`, `Vfx_HeadSlime`, `Vfx_Land`, `Vfx_SwingStartSwordA`, `Vfx_SwingStartSwordB`, `Vfx_SwingStartSwordC`).
    - Inside `WireProgressionAndEconomyFromMainLevel` (while `Main_Level.unity` is loaded additively at `L2291`):
      - Find `"Combat and Pickup Feedback"` in `mainLevelScene` and locate its first child with `EnemyImpactFeedback` (`"Enemy_Shield hit feedback"`).
      - Create `"Combat and Pickup Feedback"` root in `targetScene` with an `AudioSource` (`playOnAwake = false`, `spatialBlend = 0f`).
      - For each of the 30 `EnemyController`s in `allEnemies`: instantiate a clone of `"Enemy_Shield hit feedback"` under `"Combat and Pickup Feedback"`, name it `$"{enemy.name} hit feedback"`, set its `EnemyImpactFeedback` serialized fields `_target = enemy.GetComponent<Health>()`, `_body = enemy.GetComponent<BoxCollider2D>()`, `_vfx = vfxSpawner`, `_audio = feedbackAudioSource`, and set all child `SpriteRenderer`s under `_bar` to `sortingLayerID = vfxLayerId` (`Outline` order `8`, `Track` order `9`, `Remaining health` order `10`).

- [ ] **Step 4: Run `BuildAll` + `MapWiringTests A11..A17` + `Round8Phase2FeelMovementTests`**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/Enemies/ Assets/_Project/Features/Player/ Assets/_Project/Features/NhaGiamGiengChuong/ Assets/_Project/Scenes/NhaGiamGiengChuong.unity`
  - `rtk git commit -m "feat(feel): wire EnemyImpactFeedback on all 30 enemies, prewarm all 11 VFX, wire Vfx_HeadSlime and stomp/roll/dodge VFX"`

### PHASE 2 REVIEW GATE (STOP & REVIEW BEFORE PHASE 3)
- [ ] Run all 4 EditMode batches + PlayMode batch (`100% PASS, 0 console errors`).
- [ ] Perform full Phase 2 Review against `M-06`, `M-07`, `M-09`, `A-02 (combat feedback)`, `A-12 (stagger)`, `A-15`, and Review Focus #4.
- [ ] Resolve any findings before starting Phase 3.

---

# PHASE 3 — Visual Polish, Map Density, Lighting, Fog, Stairs & Parallax

### Task 3.1: Fix `Stairs_H` Inverted Slope Texture (`M-01`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs:3302-3324`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (add `A18` section 1)

**Interfaces:**
- Consumes: `pal.Stairs0`, `pal.Stairs1`, `SetTileWithMatrix`, `PaintPrisonSolidFloor`.
- Produces: Horizontally flipped `Matrix4x4.Scale(new Vector3(-1f, 1f, 1f))` stair riser tiles on `(xMax - 1, topY - 1)` matching the west→east descending `\` step corners (`x = 18@10, 15@12, 12@14, 9@16`), without overwriting the lower horizontal tread caps `(18, 7), (15, 9), (12, 11), (9, 13)`.

- [ ] **Step 1: Add `A18` section 1 in `MapWiringTests.cs`**
  - Assert that on `Room_HJK`'s `TM_SolidRim`:
    - Every cell containing `stairs_0_00` or `stairs_1_00` has `rimTm.GetTransformMatrix(cell).m00 < 0f` (flipped horizontally to match `\` descent).
    - The lower tread walkable cells `(18, 7), (15, 9), (12, 11), (9, 13)` still have `floorStamp_*` on `TM_SolidVisual` and `TM_SolidRim` (not overwritten by riser bevels).

- [ ] **Step 2: Run `MapWiringTests A18` to confirm FAIL**
  - Expected: FAIL (`m00 == +1f` and lower tread cells `(18, 7), (15, 9), (12, 11), (9, 13)` overwritten at `topY - 2`).

- [ ] **Step 3: Fix `PaintPrisonStairsH` in `NhaGiamGiengChuongBuilder.cs:3302-3324`**
  - Replace lines `3317-3322` with:
    ```csharp
    var flipX = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
    foreach (var (xMin, xMax, topY) in steps)
    {
        PaintPrisonSolidFloor(solidTm, rimTm, pal, xMin, xMax, 7f, topY);
    }
    foreach (var (xMin, xMax, topY) in steps)
    {
        if (topY > 8)
        {
            // Place flipped diagonal step nose on the upper step's east corner cell (xMax - 1, topY - 1)
            // and mid-riser bevel at (xMax - 1, topY - 2), preserving lower tread caps at x >= xMax.
            SetTileWithMatrix(rimTm, new Vector3Int(xMax - 1, topY - 1, 0), pal.Stairs0, flipX);
            SetTileWithMatrix(rimTm, new Vector3Int(xMax - 1, topY - 2, 0), pal.Stairs1, flipX);
        }
    }
    ```
  - Note: `solidTm` at `(xMax - 1, topY - 1)` retains its `floorStamp_*` tile from `PaintPrisonSolidFloor` (satisfying `A10`), while `rimTm` overlays the flipped `stairs_0_00`/`stairs_1_00` diagonal step nose on top of the step corner!

- [ ] **Step 4: Run `BuildAll` + `A10` + `A14` + `A18` section 1**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/NhaGiamGiengChuong/`
  - `rtk git commit -m "fix(visual): flip Stairs_H step tiles horizontally to match descending slope without clobbering treads"`

---

### Task 3.2: Upgrade `GroundMist2D.shader` to Silky World-Space 3-Octave FBM (`M-02`)

**Files:**
- Modify: `Assets/_Project/Art/Shaders/GroundMist2D.shader:1-118`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`MatGroundMist` color/alpha calibration)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A18` section 2)

**Interfaces:**
- Consumes: `TransformObjectToWorld(input.positionOS)` in `Core2D.hlsl`.
- Produces: World-space `worldXY` 3-octave quintic FBM in `GroundMist2D.shader` with `Blend SrcAlpha OneMinusSrcAlpha` and quadratic vertical feathering.

- [ ] **Step 1: Add `A18` section 2 in `MapWiringTests.cs`**
  - Verify `GroundMist2D.shader` source contains `TransformObjectToWorld` and `Blend SrcAlpha OneMinusSrcAlpha`, and verify all 6 `GroundMist_R5` renderers use `DeadCells/GroundMist2D`.

- [ ] **Step 2: Run `MapWiringTests A18` to confirm FAIL**
  - Expected: FAIL.

- [ ] **Step 3: Implement world-space 3-octave FBM in `GroundMist2D.shader`**
  - Change `Blend SrcAlpha One` → `Blend SrcAlpha OneMinusSrcAlpha`.
  - Add `float2 worldXY : TEXCOORD1;` to `Varyings`; in `Vert`: `float3 posWS = TransformObjectToWorld(input.positionOS); output.worldXY = posWS.xy;`.
  - In `ValueNoise2D(float2 p)`: use quintic interpolation `float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);`.
  - In `Frag`:
    ```hlsl
    float2 p = input.worldXY * float2(0.22, 0.65) * (_NoiseScale * 0.28);
    float t = _Time.y * _ScrollSpeed * 4.0;
    float f1 = ValueNoise2D(p + float2(t, 0.0));
    float f2 = ValueNoise2D(p * 2.03 + float2(-t * 0.72, 1.73));
    float f3 = ValueNoise2D(p * 4.17 + float2(t * 0.45, 3.41));
    half fbm = (half)(f1 * 0.52 + f2 * 0.33 + f3 * 0.15);
    half noise = smoothstep(0.22h, 0.88h, fbm);

    half vBottom = smoothstep(0.0h, 0.38h, (half)input.uv.y);
    half vTop = 1.0h - smoothstep(0.32h, 1.0h, (half)input.uv.y);
    half vFade = vBottom * vTop * vTop;
    half hFade = smoothstep(0.0h, 0.10h, (half)input.uv.x) * (1.0h - smoothstep(0.90h, 1.0h, (half)input.uv.x));
    ```
  - In `NhaGiamGiengChuongBuilder.cs`: set `MatGroundMist` `_MistColor` alpha to `0.24f` (`new Color(0.34f, 0.52f, 0.54f, 0.24f)`).

- [ ] **Step 4: Run `BuildAll` + `A11` + `A18` section 2**
  - Expected: PASS, `shaderErrors = 0`.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Art/Shaders/GroundMist2D.shader Assets/_Project/Features/NhaGiamGiengChuong/`
  - `rtk git commit -m "fix(shader): rewrite GroundMist2D with world-space 3-octave quintic FBM and soft alpha feathering"`

---

### Task 3.3: Socket `Art_Q_SkeletonSpawn` Left Hard-Cut Behind `Art_Q_SkeletonPier` & Add 44 Pre-Verified Grounded Floor Props (`M-03`, `M-04`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (add `Art_Q_SkeletonPier` in `BuildRoomW3_WestBastion` + 44 pre-verified grounded floor props across `Room A, D, H, K, E, L, F, M, N, O, P, Q, R, S, T`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A18` section 3)

**Interfaces:**
- Consumes: `AddGroundedPrisonSprite`, `AddFloorJunkCluster`, `pal.SpawnerPillar`, `pal.WallBed`.
- Produces:
  - `Art_Q_SkeletonPier`: `AddGroundedPrisonSprite(artRoot, "Art_Q_SkeletonPier", pal.SpawnerPillar, mat, -51.88f, 16f, 0.92f, -12, Color.white)` — sorts on `Background / -12` (1 order IN FRONT of `Art_Q_SkeletonSpawn` at `Background / -13`), covering `x = [-52.45 .. -51.31], y = [16.0 .. 22.3]` so `Art_Q_SkeletonSpawn`'s `x = 0` vertical hard-cut edge at `x = -51.88f` is physically socketed behind a masonry pier while `Art_Q_SkeletonSpawn` stays at `(-49.0f, 16f)` (passing `A12`/`A14` 100%).
  - **44 Pre-Verified Grounded Floor Props** (proven `0` bay clashes, `0` OneWay clashes, reducing max floor prop gap across all 19 rooms to **`<= 9.50u`**):
    - **Room A (`y = 0f`):** `AddFloorJunkCluster(artRoot, "Art_A_JunkEast", pal, mat, 10.0f, 0f, 3);`
    - **Room D (`y = 8f`):** `AddFloorJunkCluster(artRoot, "Art_D_JunkWest", pal, mat, 26.5f, 8f, 2);`, `AddGroundedPrisonSprite(artRoot, "Art_D_Bed1", pal.WallBed, mat, 31.5f, 8f, 0.85f, -11, Color.white);`, `AddFloorJunkCluster(artRoot, "Art_D_JunkEast2", pal, mat, 39.8f, 8f, 5);`
    - **Room H (`Stairs_H`):** `AddFloorJunkCluster(artRoot, "Art_H_LandingJunk", pal, mat, 19.8f, 8f, 1);`, `AddFloorJunkCluster(artRoot, "Art_H_StepJunkMid", pal, mat, 13.5f, 12f, 4);`, `AddFloorJunkCluster(artRoot, "Art_H_StepJunkTop", pal, mat, 7.5f, 16f, 7);`
    - **Room K (`y = 16f`):** `AddFloorJunkCluster(artRoot, "Art_K_JunkMid", pal, mat, -23.0f, 16f, 6);`
    - **Room E (`y = 24f`):** `AddFloorJunkCluster(artRoot, "Art_E_JunkWest", pal, mat, -5.5f, 24f, 1);`, `AddFloorJunkCluster(artRoot, "Art_E_JunkMid", pal, mat, 1.5f, 24f, 6);`, `AddFloorJunkCluster(artRoot, "Art_E_JunkEast", pal, mat, 18.0f, 24f, 9);`
    - **Room L (`y = 24f`):** `AddFloorJunkCluster(artRoot, "Art_L_JunkMid", pal, mat, 29.8f, 24f, 3);`, `AddFloorJunkCluster(artRoot, "Art_L_JunkEast", pal, mat, 35.2f, 24f, 5);`
    - **Room F (`y = 32f`):** `AddFloorJunkCluster(artRoot, "Art_F_JunkMid", pal, mat, 11.5f, 32f, 2);`, `AddFloorJunkCluster(artRoot, "Art_F_JunkEast", pal, mat, 18.5f, 32f, 7);`
    - **Room M (`y = 0f`):** `AddFloorJunkCluster(artRoot, "Art_M_JunkWest2", pal, mat, 95.0f, 0f, 3);`, `AddFloorJunkCluster(artRoot, "Art_M_JunkMid", pal, mat, 104.5f, 0f, 6);`, `AddFloorJunkCluster(artRoot, "Art_M_JunkMid2", pal, mat, 122.2f, 0f, 8);`, `AddFloorJunkCluster(artRoot, "Art_M_JunkEast2", pal, mat, 125.5f, 0f, 2);`, `AddFloorJunkCluster(artRoot, "Art_M_JunkEast3", pal, mat, 147.5f, 0f, 9);`
    - **Room N (`y = 8f`):** `AddFloorJunkCluster(artRoot, "Art_N_JunkWest2", pal, mat, 85.0f, 8f, 1);`, `AddFloorJunkCluster(artRoot, "Art_N_JunkMid1", pal, mat, 93.0f, 8f, 4);`, `AddFloorJunkCluster(artRoot, "Art_N_JunkShaftEdge", pal, mat, 99.8f, 8f, 7);`, `AddFloorJunkCluster(artRoot, "Art_N_JunkMid2", pal, mat, 115.5f, 8f, 3);`, `AddFloorJunkCluster(artRoot, "Art_N_JunkMid3", pal, mat, 122.5f, 8f, 8);`, `AddFloorJunkCluster(artRoot, "Art_N_JunkEast1", pal, mat, 142.5f, 8f, 2);`, `AddFloorJunkCluster(artRoot, "Art_N_JunkEast2", pal, mat, 150.5f, 8f, 6);`, `AddFloorJunkCluster(artRoot, "Art_N_JunkEast3", pal, mat, 158.5f, 8f, 9);`
    - **Room O (`y = -16f`):** `AddFloorJunkCluster(artRoot, "Art_O_JunkMid1", pal, mat, 96.0f, -16f, 2);`, `AddFloorJunkCluster(artRoot, "Art_O_JunkMid3", pal, mat, 103.0f, -16f, 5);`, `AddFloorJunkCluster(artRoot, "Art_O_JunkMid2", pal, mat, 116.5f, -16f, 8);`, `AddFloorJunkCluster(artRoot, "Art_O_JunkEast4", pal, mat, 130.5f, -16f, 1);`, `AddFloorJunkCluster(artRoot, "Art_O_JunkEast2", pal, mat, 146.5f, -16f, 4);`, `AddFloorJunkCluster(artRoot, "Art_O_JunkEast3", pal, mat, 160.0f, -16f, 7);`
    - **Room P (`y = -16f`):** `AddFloorJunkCluster(artRoot, "Art_P_JunkWest2", pal, mat, 168.0f, -16f, 3);`, `AddFloorJunkCluster(artRoot, "Art_P_JunkMid1", pal, mat, 188.5f, -16f, 6);`, `AddFloorJunkCluster(artRoot, "Art_P_JunkMid2", pal, mat, 195.0f, -16f, 9);`, `AddFloorJunkCluster(artRoot, "Art_P_JunkEast2", pal, mat, 221.0f, -16f, 2);`, `AddFloorJunkCluster(artRoot, "Art_P_JunkEast3", pal, mat, 229.0f, -16f, 5);`
    - **Room Q (`y = 16f`):** `Art_Q_SkeletonPier` at `-51.88f`, `AddFloorJunkCluster(artRoot, "Art_Q_JunkMid1", pal, mat, -73.0f, 16f, 4);`, `AddFloorJunkCluster(artRoot, "Art_Q_JunkMid2", pal, mat, -65.5f, 16f, 8);`, `AddFloorJunkCluster(artRoot, "Art_Q_JunkEast2", pal, mat, -36.0f, 16f, 1);`
    - **Room R (`y = 24f`):** `AddFloorJunkCluster(artRoot, "Art_R_JunkWest2", pal, mat, -98.0f, 24f, 2);`, `AddFloorJunkCluster(artRoot, "Art_R_JunkWest3", pal, mat, -90.0f, 24f, 6);`, `AddFloorJunkCluster(artRoot, "Art_R_JunkMid1", pal, mat, -76.5f, 24f, 9);`, `AddFloorJunkCluster(artRoot, "Art_R_JunkMid2", pal, mat, -70.2f, 24f, 3);`, `AddFloorJunkCluster(artRoot, "Art_R_JunkShaftEdge", pal, mat, -61.8f, 24f, 5);`, `AddFloorJunkCluster(artRoot, "Art_R_JunkEast2", pal, mat, -52.5f, 24f, 8);`
    - **Room S (`y = 24f`):** `AddFloorJunkCluster(artRoot, "Art_S_JunkMid1", pal, mat, 52.0f, 24f, 1);`, `AddFloorJunkCluster(artRoot, "Art_S_JunkMid2", pal, mat, 60.0f, 24f, 4);`, `AddFloorJunkCluster(artRoot, "Art_S_JunkMid3", pal, mat, 68.0f, 24f, 7);`, `AddFloorJunkCluster(artRoot, "Art_S_JunkEast2", pal, mat, 86.5f, 24f, 9);`
    - **Room T (`y = 40f`):** `AddFloorJunkCluster(artRoot, "Art_T_JunkWest2", pal, mat, 43.0f, 40f, 2);`, `AddFloorJunkCluster(artRoot, "Art_T_JunkMid1", pal, mat, 58.0f, 40f, 6);`, `AddFloorJunkCluster(artRoot, "Art_T_JunkShaft", pal, mat, 75.8f, 40f, 4);`, `AddFloorJunkCluster(artRoot, "Art_T_JunkMid2", pal, mat, 84.8f, 40f, 8);`, `AddFloorJunkCluster(artRoot, "Art_T_JunkMid3", pal, mat, 90.5f, 40f, 3);`, `AddFloorJunkCluster(artRoot, "Art_T_JunkEast1", pal, mat, 105.5f, 40f, 7);`, `AddFloorJunkCluster(artRoot, "Art_T_JunkEast3", pal, mat, 128.0f, 40f, 1);`, `AddFloorJunkCluster(artRoot, "Art_T_JunkEast2", pal, mat, 135.5f, 40f, 5);`

- [ ] **Step 1: Add `A18` section 3 in `MapWiringTests.cs`**
  - Assert `Art_Q_SkeletonPier` exists on `Background / -12` and its X bounds cover `Art_Q_SkeletonSpawn.bounds.min.x` (`-51.88f ± 0.15f`).
  - Assert all 44 new floor dressing objects exist and none overlap any `OneWay_*` platform or carved bay frame.

- [ ] **Step 2: Run `MapWiringTests A18` to confirm FAIL**
  - Expected: FAIL (`Art_Q_SkeletonPier` and new props missing).

- [ ] **Step 3: Add `Art_Q_SkeletonPier` and the 44 floor dressing calls in `NhaGiamGiengChuongBuilder.cs`**

- [ ] **Step 4: Run `BuildAll` + `A08..A18`**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/NhaGiamGiengChuong/`
  - `rtk git commit -m "fix(visual): frame Art_Q_SkeletonSpawn left edge with stone pier and add 44 floor props across sparse rooms"`

---

### Task 3.4: Lift All 19 Rooms to `>= 93%` Effective Light Coverage, Fix `DuplicateGlobalLight` Build Error & Expand `ShadowCasters_R5` to `12` (`M-05`, `A-17`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A11` line 823 caster count `6 → 12`, `A18` section 4)

**Interfaces:**
- Consumes: `AddStandingTorchBeacon`, `AddCeilingLanternBeacon`, `AddWindowWithGodray`, `CreateTeleportGate`, `CreateBoxShadowCaster`.
- Produces:
  - **2-line `ErrorIfDuplicateGlobalLight` elimination in `WireProgressionAndEconomyFromMainLevel` (`L2290`):**
    - Before `EditorSceneManager.OpenScene(MainLevelScenePath, OpenSceneMode.Additive)`: find `targetScene`'s `Global Light 2D` and set `globalLight.enabled = false;`.
    - Inside `finally` right after `EditorSceneManager.CloseScene(mainLevelScene, true)`: restore `globalLight.enabled = true;`.
  - **Pre-Verified 19-Room Lighting Upgrade (`93.1%–100.0%` effective coverage at `<= 0.85 * outerRadius`, `0` new `LanternFixture_R5` sprite clashes, `A15` compliant):**
    - In spine rooms (`A, B, C, D, G, H, J, K, E, L, F`): increase existing `AddStandingTorchBeacon` / `AddCeilingLanternBeacon` `outerRadius` from `6.0f → 8.8f`.
    - In wing rooms (`M, N, O, P, Q, R, S, T`): increase `outerRadius` from `9.5–10.5f → 13.5f` (`14.5f` in Room T), and raise odd-beacon intensity from `0.88f → 0.94f` (even-beacon stays `1.34f`, preserving `maxInt - minInt = 0.40f` and `falloffIntensity = 0.62f >= 0.58f` for `A15`!).
    - In `AddWindowWithGodray`: attach a child `WindowMoonLight` (`Light2D.LightType.Point`, `color = new Color(0.55f, 0.74f, 0.96f, 1f)`, `intensity = 0.72f`, `pointLightOuterRadius = 9.5f`, `pointLightInnerRadius = 1.2f`, `falloffIntensity = 0.58f`, `volumetricEnabled = false`).
    - In `CreateTeleportGate`: attach a child `TeleportLight` (`Light2D.LightType.Point`, `color = new Color(0.38f, 0.88f, 1.0f, 1f)`, `intensity = 0.85f`, `pointLightOuterRadius = 4.5f`, `pointLightInnerRadius = 0.8f`, `falloffIntensity = 0.58f`, `volumetricEnabled = false`).
  - **6 Additional Cross-Room `ShadowCaster2D` Dividers under `ShadowCasters_R5` (`12` total):**
    1. `Caster_FloorM_CeilingO` at `(95.0f, -0.5f, 0f)`, `width = 38f, height = 1f`
    2. `Caster_FloorM_East_CeilingO` at `(142.5f, -0.5f, 0f)`, `width = 43f, height = 1f`
    3. `Caster_FloorR_West_CeilingQ` at `(-85.5f, 23.5f, 0f)`, `width = 33f, height = 1f`
    4. `Caster_FloorD_CeilingB` at `(32.0f, 7.5f, 0f)`, `width = 20f, height = 1f`
    5. `Caster_FloorJ_West` at `(-5.0f, 15.5f, 0f)`, `width = 22f, height = 1f`
    6. `Caster_FloorT_East_CeilingS` at `(111.5f, 39.5f, 0f)`, `width = 57f, height = 1f`

- [ ] **Step 1: Update `A11` line 823 (`12` casters) and add `A18` section 4 in `MapWiringTests.cs`**
  - In `A18` section 4: sample every one of the 19 room rects on a `1.0u` grid and assert **effective point-light coverage (`distance <= 0.85f * l.pointLightOuterRadius`) is `>= 0.80f` (`80%`) in every single room**.
  - Assert `ShadowCasters_R5` has `12` `ShadowCaster2D` children.

- [ ] **Step 2: Run `MapWiringTests A11` & `A18` to confirm FAIL**
  - Expected: FAIL (`Room A 57.4%`, `ShadowCasters == 6`).

- [ ] **Step 3: Apply the lighting, `WindowMoonLight`, `TeleportLight`, 12 `ShadowCasters_R5`, and 2-line `globalLight.enabled` guard in `NhaGiamGiengChuongBuilder.cs`**

- [ ] **Step 4: Run `BuildAll` (verify `0` `ErrorIfDuplicateGlobalLight` lines in build log!) + `A09..A18`**
  - Expected: All PASS, `0` `ErrorIfDuplicateGlobalLight` in build log.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/NhaGiamGiengChuong/`
  - `rtk git commit -m "fix(lighting): lift all 19 rooms >=93% effective coverage, add 6 shadow casters, fix duplicate global light log"`

---

### Task 3.5: Symmetrize `Foreground_Silhouettes` Wrap (`[-32..+32]`) & Add Camera-Attached Ambient Particles (`A-13`, `A-14`)

**Files:**
- Create: `Assets/_Project/Features/CameraSystem/Scripts/CameraFollowEmitter2D.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs:4177-4195` (`Foreground_Silhouettes` + `AmbientDustMotes` / `AmbientDrippingWater`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A18` section 5)

**Interfaces:**
- Consumes: `ParallaxLayer2D` (`_wrapWidthUnits = 32f`), `Camera.main`.
- Produces:
  - `CameraFollowEmitter2D` (`[DefaultExecutionOrder(60)]` in `DeadCells.CameraSystem`):
    - `[SerializeField] private Transform _cameraTransform;`, `[SerializeField] private Vector2 _offset;`, `public void Configure(Transform cam, Vector2 offset)`, `LateUpdate()` moves `transform.position` to `(cam.x + _offset.x, cam.y + _offset.y, transform.position.z)`.
  - `Foreground_Silhouettes`: `float[] fgAnchorXs = { -32f, -24f, -16f, -8f, 0f, 8f, 16f, 24f, 32f };` (`9` beams at `x` + `9` chains at `x + 2.0f`, covering `[-33.54 .. +34.55]`, `leftUncovered = -5.10u < 0`, `rightUncovered = -6.11u < 0` — zero pop-in at any wrap phase!).
  - `AmbientDustMotes` & `AmbientDrippingWater`: attached `CameraFollowEmitter2D` with `shape.scale = (28f, 16f, 0.1f)` (`rate = 18/s`, `simulationSpace = World`) and `(26f, 1.5f, 0.1f)` (`offset = (0f, 6.0f)`, `rate = 7/s`, `simulationSpace = World`).

- [ ] **Step 1: Add `A18` section 5 in `MapWiringTests.cs`**
  - Assert `Foreground_Silhouettes` combined child sprite bounds span `min.x <= -32.0f` and `max.x >= +32.0f`.
  - Assert `AmbientDustMotes` and `AmbientDrippingWater` both have `CameraFollowEmitter2D` and `shape.scale.x <= 32f`.

- [ ] **Step 2: Run `MapWiringTests A18` to confirm FAIL**
  - Expected: FAIL (`min.x == -21.54`, no `CameraFollowEmitter2D`).

- [ ] **Step 3: Create `CameraFollowEmitter2D.cs` & update `NhaGiamGiengChuongBuilder.cs`**

- [ ] **Step 4: Run `BuildAll` + `A11` + `A18`**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/CameraSystem/Scripts/CameraFollowEmitter2D.cs Assets/_Project/Features/NhaGiamGiengChuong/ Assets/_Project/Scenes/NhaGiamGiengChuong.unity`
  - `rtk git commit -m "fix(atmosphere): symmetrize Foreground_Silhouettes [-32..+32] and attach ambient particles to camera"`

### PHASE 3 REVIEW GATE (STOP & REVIEW BEFORE PHASE 4)
- [ ] Run all 4 EditMode batches + PlayMode batch (`100% PASS, 0 console errors`).
- [ ] Run **`9/9` D3D12 GPU visual capture** and inspect rendered PNGs (`Stairs_H`, `GroundMist2D`, `Art_Q_SkeletonPier`, floor density, room brightness, `maxClip < 0.25%`).
- [ ] Perform full Phase 3 Review against `M-01..M-05`, `A-13`, `A-14`, `A-17`.
- [ ] Resolve any findings before starting Phase 4.

---

# PHASE 4 — Complete Audio Hub, Pause/Quit Menu Shell, Controls HUD & Final `.exe`

### Task 4.1: Build `CombatAudioHub` + `ProceduralSfxLibrary` & Clone `ItemPickupFeedback` (`A-02`, `A-12`)

**Files:**
- Create: `Assets/_Project/Core/Scripts/Audio/SfxId.cs`
- Create: `Assets/_Project/Core/Scripts/Audio/ProceduralSfxLibrary.cs`
- Create: `Assets/_Project/Core/Scripts/Audio/CombatAudioHub.cs`
- Modify: `Assets/_Project/Features/HUD/Scripts/Feedback/ItemPickupFeedback.cs:19-33` (null-safe `_weapons` loop when `_weapons == null`)
- Modify: `Assets/_Project/Features/Player/Scripts/PlayerFeedback.cs`, `PlayerVfx.cs`, `PlayerBow.cs`
- Modify: `Assets/_Project/Features/Enemies/Scripts/Projectile.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/HealingChest.cs`, `SwitchDoor.cs`, `TeleportGate.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (wire `ItemPickupFeedback` on `"Combat and Pickup Feedback"` + add `"CombatAudioHub"` GameObject)
- Create: `Assets/_Project/Core/Tests/EditMode/Round8Phase4AudioTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (add `A19` section 1)

**Interfaces:**
- Consumes: `Main_Level.unity`'s `"Combat and Pickup Feedback"` (`ItemPickupFeedback` with `gold_1.wav` + `Vfx_CollectedGlint.asset`), `AudioClip.Create`.
- Produces:
  - `DeadCells.Core.Audio.SfxId` enum (`21` IDs: `SwordSlash1`, `SwordSlash2`, `SwordSlash3`, `BowDraw`, `BowRelease`, `ArrowHit`, `Jump`, `AirJump`, `Land`, `Roll`, `StompImpact`, `WallJump`, `PlayerHurt`, `PlayerDeath`, `ShieldClang`, `PerfectDodge`, `ChestHeal`, `SwitchDoorOpen`, `TeleportWarp`, `GrenadeBounce`, `GrenadeExplode`).
  - `ProceduralSfxLibrary.GetClip(SfxId id)` (`AudioClip`, deterministic mono `22050 Hz` PCM synthesis cached once) + `ProceduralSfxLibrary.GetAmbientLoopClip()` (`4.0s` seamless mono low-drone loop).
  - `CombatAudioHub.Play(SfxId id, float volumeScale = 1f)` (static null-safe call dispatching to 8 pooled 2D `AudioSource`s with `Random.Range(0.93f, 1.07f)` pitch jitter + dedicated looping ambient `AudioSource` at `volume = 0.18f`).

- [ ] **Step 1: Write failing EditMode tests in `Round8Phase4AudioTests.cs` & `A19` section 1 in `MapWiringTests.cs`**
  - `ProceduralSfxLibrary_GeneratesValidNonSilentMonoClips_ForAll21SfxIdsAndAmbientLoop`.
  - `CombatAudioHub_Play_IsNullSafeWithoutInstance_AndCyclesPooledSourcesWithInstance`.
  - `A19` section 1: assert `NhaGiamGiengChuong.unity` contains `1` `CombatAudioHub` and `1` `ItemPickupFeedback` with non-null `_player`, `_wallet`, `_effects`, `_glint`, `_audio`, `_goldSound`.

- [ ] **Step 2: Run `Round8Phase4AudioTests` & `MapWiringTests A19` to confirm FAIL**
  - Expected: FAIL.

- [ ] **Step 3: Implement `SfxId.cs`, `ProceduralSfxLibrary.cs`, `CombatAudioHub.cs`, gameplay hooks, and builder wiring**
  - Null-guard `if (_weapons != null)` in `ItemPickupFeedback.OnEnable`/`OnDisable`.
  - Clone or attach `ItemPickupFeedback` on `"Combat and Pickup Feedback"` in `NhaGiamGiengChuongBuilder.cs` (copying `_glint`, `_goldSound`, `_weaponSound` from `Main_Level.unity`'s `"Combat and Pickup Feedback"` and binding `_player = playerGo.transform`, `_wallet = goldWallet`, `_effects = vfxSpawner`, `_audio = feedbackAudioSource`, `_weapons = Array.Empty<StarterWeaponPickup>()`).
  - Add `"CombatAudioHub"` GameObject with `CombatAudioHub` component in `NhaGiamGiengChuongBuilder.cs`.

- [ ] **Step 4: Run `BuildAll` + `Round8Phase4AudioTests` + `A19` section 1**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Core/Scripts/Audio/ Assets/_Project/Features/ Assets/_Project/Scenes/NhaGiamGiengChuong.unity Assets/_Project/Core/Tests/EditMode/Round8Phase4AudioTests.cs`
  - `rtk git commit -m "feat(audio): add CombatAudioHub with 21 procedural SFX, dungeon ambient loop, and wire ItemPickupFeedback"`

---

### Task 4.2: Add Standalone Pause/Quit Menu (`Esc`) & Minimal Controls Legend (uGUI) (`A-16a`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/LevelRunController.cs:257-299`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/PauseMenuController.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (build `PauseMenu` uGUI Canvas `sortingOrder = 90` + bottom-right HUD micro-hint `"[Esc] Tạm dừng  ·  [1/2] Vũ khí"`)
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/PauseMenuTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A19` section 2)

**Interfaces:**
- Consumes: `LevelRunController.IsPlaying`, `PlayerController.RunStats.IsChoosing`, `HitStop.SetMenuPaused(bool)`, `PlayerInputReader`.
- Produces:
  - `LevelRunController.TryRestartFromPause()` (`public bool`): accepts `LevelPhase.Playing` (whereas `TryReplay()` only accepts `LevelPhase.Ended`), resets `_hitStop?.SetMenuPaused(false)` and `Time.timeScale = 1f`, transitions `_phase = LevelPhase.Reloading`, fires `ReplayRequested`, and executes the shared scene reload helper.
  - `PauseMenuController` (`DeadCells.NhaGiamGiengChuong`):
    - Serialized: `_run`, `_player`, `_playerInput`, `_hitStop`, `_panelRoot`, `_resumeButton`, `_restartButton`, `_quitButton`.
    - Public API: `bool IsPaused { get; }`, `bool TogglePause()`, `bool OpenPause()`, `bool Resume()`, `bool RestartRun()`, `void QuitGame()`, `Action QuitHandler { get; set; }`.
    - Guard: `CanPause => _run != null && _run.IsPlaying && (_player == null || _player.RunStats == null || !_player.RunStats.IsChoosing)`.
    - Polls `Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame` (and `Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame`) in `Update()`.
    - Auto-closes cleanly if `_run` transitions to `Ended` (`_run.Finished += _ => Resume()`).

- [ ] **Step 1: Write failing EditMode tests in `PauseMenuTests.cs` & `A19` section 2**
  - `PauseMenu_RejectsToggleDuringIntroEndedAndScrollChoosing_AndPausesDuringPlaying`: verify `TogglePause()` returns `false` in `Intro`, `Ended`, and while `RunStats.IsChoosing == true`; during `Playing`, `TogglePause()` sets `IsPaused == true`, `_panelRoot.activeSelf == true`, `Time.timeScale == 0f`, `_playerInput.enabled == false`; second `TogglePause()` restores `IsPaused == false`, `_panelRoot.activeSelf == false`, `Time.timeScale == 1f`, `_playerInput.enabled == true`.
  - `PauseMenu_RestartFromPause_UnpausesAndReloadsDuringPlaying_AndQuitInvokesHandler`: verify `RestartRun()` calls `_run.TryRestartFromPause()`, restores `Time.timeScale == 1f`, and enters `LevelPhase.Reloading`; verify `QuitGame()` calls `QuitHandler`.
  - `A19` section 2: assert `PauseMenuController` exists in `NhaGiamGiengChuong.unity` with wired buttons, hidden `_panelRoot` at startup, controls legend text, and bottom-right HUD micro-hint under `PlayerHUD`.

- [ ] **Step 2: Run `PauseMenuTests` & `MapWiringTests A19` to confirm FAIL**
  - Expected: FAIL.

- [ ] **Step 3: Implement `LevelRunController.TryRestartFromPause()`, `PauseMenuController.cs`, and uGUI builder setup**
  - Build `PauseMenu` Canvas in `NhaGiamGiengChuongBuilder.cs` (`sortingOrder = 90`, `ScaleWithScreenSize 1920×1080`, dark modal backdrop `(0.03, 0.04, 0.06, 0.86)`, title `"TẠM DỪNG"`, 2-column Vietnamese controls table covering `A/D`, `Space (Nhảy / Nhảy đôi / Bám tường)`, `Chuột trái / Enter (Tấn công)`, `Shift (Lăn né i-frame)`, `S + Space (Đập đất / Tụt sàn)`, `W / S (Leo xích)`, `1 / 2 (Đổi Kiếm / Cung)`, `E (Tương tác)`, and 3 styled buttons `"Tiếp tục [Esc]"`, `"Chơi lại"`, `"Thoát game"`).
  - Add compact bottom-right `ControlsHintBadge` (`"[Esc] Tạm dừng  ·  [1/2] Vũ khí  ·  [Shift] Lăn  ·  [E] Tương tác"`) anchored at bottom-right `(1f, 0f)` offset `(-20f, 18f)` under `PlayerHUD` Canvas.

- [ ] **Step 4: Run `BuildAll` + `PauseMenuTests` + `A19`**
  - Expected: All PASS.

- [ ] **Step 5: Commit**
  - `rtk git add Assets/_Project/Features/NhaGiamGiengChuong/ Assets/_Project/Scenes/NhaGiamGiengChuong.unity`
  - `rtk git commit -m "feat(ui): add Esc Pause/Quit menu with controls legend, TryRestartFromPause, and HUD controls hint"`

---

### Task 4.3: Rebuild Standalone Windows `.exe` & Package Verification

**Files:**
- Output binary: `Build/NhaGiamGiengChuong/DeadCells_NG.exe`

- [ ] **Step 1: Execute `NhaGiamGiengChuongBuilder.BuildAll` and verify `0` console/build errors**
- [ ] **Step 2: Build standalone Windows `.exe` (`NhaGiamGiengChuongBuilder.BuildStandaloneWindows` / `BuildPipeline.BuildPlayer` targeting `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` → `Build/NhaGiamGiengChuong/DeadCells_NG.exe`)**
- [ ] **Step 3: Run `rtk proxy cmd /c codegraph sync`**

### PHASE 4 REVIEW GATE (STOP & REVIEW BEFORE WHOLE-PLAN REVIEW)
- [ ] Run all 4 EditMode batches + PlayMode batch (`100% PASS, 0 console errors`).
- [ ] Perform full Phase 4 Review against `A-02`, `A-12`, `A-16a`, and Review Focus #5.
- [ ] Resolve any findings before advancing to Whole-Plan Master Review.

---

# WHOLE-PLAN MASTER REVIEW & FINAL WHOLE-GAME AUDIT

### Stage A: Whole-Plan Master Review
- [ ] Verify all **27/27 Traceability IDs (`M-01..M-09`, `A-01..A-17`)** line-by-line against on-disk code, prefabs, shaders, and `NhaGiamGiengChuong.unity`.
- [ ] Verify cross-phase integration invariants:
  1. Standing & crouching bow shots hit `Enemy_Rat`, trigger `EnemyImpactFeedback` blood/damage-number/HP-bar/SFX, and melee hits extract embedded arrows.
  2. `Zone_B1/J1/E1` wake all enemies from every room entrance; `I11_Scroll (156.5)`, `Art_M_WeaponDeco (152.2)`, and `TP_1 (161.0)` are disjoint and mutually exclusive on `[E]`.
  3. `TeleportGate` warps snap `CameraFollow2D`, `ParallaxLayer2D`, `Foreground_Silhouettes`, and `CameraFollowEmitter2D` immediately without visual tearing or pop-in.
  4. `WallCling`, `LedgeClimb`, and `ChainClimb` (`ClimbHandOffsetX = 0.24f`) transition cleanly without oscillation.
  5. `HitStop`, `ScrollUpgradeMenu`, `PauseMenuController`, and `LevelRunController` never deadlock `Time.timeScale`.

### Stage B: Final Whole-Game Audit
- [ ] **Full 5-Process Automated Test Suite (`100% PASS`):**
  1. `Core.Tests.EditMode` (legacy + `Round8Phase1CombatAiTests` + `Round8Phase2FeelMovementTests` + `Round8Phase4AudioTests`)
  2. `NhaGiamGiengChuong.Tests.EditMode` Unit + Geometry (`!MapWiringTests`, including `PauseMenuTests`)
  3. `MapWiringTests` `A07..A10`
  4. `MapWiringTests` `A11..A19`
  5. `NhaGiamGiengChuong.Tests.PlayMode` (`RunPlayTests`, `ExpansionPlayTests`)
- [ ] **9/9 D3D12 GPU Visual Capture & Inspection:**
  - Confirm `captured=9/9`, `shaderErrors=0`, `preFlight=5/5`, `defaultMaterialRenderers=0`, `maxClip < 0.25%`.
  - Visually inspect all 9 captures confirming `Stairs_H` slope, `GroundMist2D` softness, `Art_Q_SkeletonPier` framing, room prop density (`maxGap <= 9.5u`), room lighting (`>= 93.1%`), and HUD polish.
- [ ] **Standalone `.exe` & Repository Hygiene:**
  - Confirm `Build/NhaGiamGiengChuong/DeadCells_NG.exe` is freshly built with `0` errors.
  - Run `rtk proxy cmd /c codegraph sync`, update `task.md`, and deliver the final evidence report to Master.
