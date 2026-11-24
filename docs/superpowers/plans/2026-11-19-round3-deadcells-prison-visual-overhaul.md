# Round 3 Remediation Plan — Dead Cells *Prison* Visual Standard Overhaul, Lighting, Ambient VFX & Post-Processing

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring all three visual surfaces (`Main_Level.unity`, `NhaGiamGiengChuong.unity`, and `.fortress-workspaces/antigravity` `GothicFortress_ArtPreview.unity`) up to the verified **Dead Cells Map 1 (*Prisoners' Quarters*)** visual standard (`>= 4.0 / 5` across the `craft-game-floors` 6-axis 2D Visual Scorecard) while maintaining `100%` test pass rates (`46/46` Root tests + `25/25` AG Authoring tests), `0` `CS0618` / `CS0414` compiler warnings, and `shaderErrors == 0`.

**Architecture:** Remediation follows the pixel-verified **Dead Cells *Prison* 6-Layer Standard** (`24 PPU`, `1.0u` grid, Inverted Value Hierarchy: near-black solid `dirt_00` core + `floorStamp_0..9` top cap + `ground_0..3` / `ceiling_0..3` 2–3px rims < mossy slate-green `backWall_0..9` + `stampWall`/`wallJunk` < warm `woodenPlatform_*` < bright `Player`/`Enemies`/beacons), organized into 3 file-disjoint tasks across 2 waves:
1. **Task 1 (`R3-A1-AG-Preview`, Wave 1 on `.fortress-workspaces/antigravity`):** Fix `P0-A1` (`Sprite-Lit-Default` / `Hidden/Universal/CoreBlit` OOM `shaderErrors=25` silent unlit fallback in `FortressDashboardAndPipelineRunner.cs`) by calling `FortressRoomBuilder.GenerateAllPresets()` inside headless `RunRound2Step3Batch()` and moving GPU screenshot capture to a fresh single-pass `RenderRound3ScreenshotsCleanBatch()` (clearing stale shader errors via `ShaderUtil.ClearShaderErrors`/`ClearShaderMessages`, warming `Sprite-Lit-Default` with 1 discarded `cam.Render()` frame without `CompilePass` on `CoreBlit`, enforcing `shaderErrors == 0`, writing `"shaderErrors": 0` to both metadata JSON files); fix `P1-A2` (paint recessed `TM_Background` backdrop + 4-tile collider-valid floor variation on `TM_Floors` via `AlignRoomRuleTileSurfaces()` in `Room_CathedralHall`, dynamically ground all preview props to `floorTopY = 1.0f`, separate `PF_WallTorch` from `PF_MagicalPortal_Cyan` and `BG_Arch_Left` from `PF_Chest`, anchor `BG_WallBanner_Right` to ceiling beam); add `R15` (`25/25` EditMode tests) and tag `fortress-authoring-r3-visual-ready`.
2. **Task 2 (`R3-MainLevel-Prison-Polish`, Wave 1 on `D:/thietkegame`):** Create shared URP Post-Processing profile `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset` (`Bloom` + `Vignette` + `ACES` Tonemapping with `overrideState = true`); clear and repaint `Main_Level.unity` `Stone visuals` & `East wing stone` (removing the orphaned `x = 52, y = 0..9` wall column, painting `floorStamp_0..9` on `y = floorTop - 1`, opaque `dirt_00` on `y <= floorTop - 2`, and new visual-only `TM_SolidRim` with `ground_0..3` on `y = floorTop`); extend West Wing `Background` up to `y = 15` (removing the `x = 52` void cliff) + add `TM_WallDecals`; raise sunken East Wing pillars (`Cell block pillar` → `y = 1.79f`, `Exit hall pillar` → `y = 2.79f`); unify West Wing `One Way Platform 16/21/28` boards to `scale.y = 0.75f` at `y = 0f` and move `Enemy_Shield - Exit Guard` to `x = 100.5f` (both spawn & reposition); add Dead Cells anti-box dressing (`littleBlackBeam_topLeft/Right_0_00`, `webBigTL_00`/`webMediumTR_00`, `woodenCornerLeft/Right_00`, `floorjunk_*`, `bannerBig_00`, explicit-coordinate `godraysArcheTriainingRoom_00`), lower `Ambient light` (`Global Light 2D`) `0.92f` → `0.56f`, enable `renderPostProcessing = true` + `Global Volume` + `AmbientDustMotes`, polish `Defeat Screen` (`Backdrop/Frame`), and add automated visual gate test (`13/13` `DeadCells.Core.Tests`).
3. **Task 3 (`R3-NG-Prison-Overhaul`, Wave 2 Sequential after Task 2 on `D:/thietkegame`):** Overhaul `NhaGiamGiengChuongBuilder.cs` to the full **Dead Cells *Prison* (`24 PPU`, `1.0u` grid)** 4-Tilemap-per-room architecture (`TM_Background`, `TM_WallDecals`, `TM_SolidVisual`, `TM_SolidRim` — `0` `TilemapCollider2D`, zero cell-overwrite gaps at wall/ceiling/header junctions, eliminating the `16:1` `512 PPU` mixel explosion and inverted lighting!); convert all 10 one-way platforms to `Prison/Sprites/woodenPlatform_*` (`scaleY = 0.75f`, flush with collider, zero floating 4-legged dining tables) + `woodenCornerLeft/Right_00`; cap all 3 chains (`I1`, `I2`, `I6`) with `ladderHole_00`/`chainDown_00` (zero floor stab/sky spike); replace `4.47u` `IronGate_001` on `I4_EncounterGate` with vertical `Prison/Sprites/hingeSmallDoor_00.png` (`localScale = (0.85f, 4f/3f, 1f)` + compensated `gateCol.size = (0.5f / 0.85f, 3f)` preserving exact `0.5u × 4.0u` `A06` collider bounds!); normalize hero props to `Player (1.66u)` scale (`I3_HealingChest` `0.55×`, `Art_G_Barrel` `0.58×`, `Art_G_Crate` `0.55×`, `Art_G_Winch` `0.58×`, `Art_C_Bell` `0.65×`), replace `3.88u` cage with `Prison/Sprites/suspendedCage_00.png` (`1.12×2.0u`), lift `PromptIndicator` to `y = 1.85f` (`+0.36u` above chest lid), use `exitDoor_00.png` + grounded `PortalBackOut_0_00.png` unlit glow & `Light2D` for `Art_F_ExitGlow` in Room F; add anti-box corner framing, alcoves/windows/`godrays`/banners/`floorjunk`; calibrate `Global Light 2D = 0.54f` and place grounded standing torches / ceiling-anchored lanterns (incl. Room L anchored to `Ceiling_L` `y = 38.5f`) across **all 11/11 rooms (`87.1%` total, `>= 66%` every room)**; wire `renderPostProcessing = true` + shared `Global Volume` + ambient VFX (`Dust`, `DrippingWater`); frame `ResultPanel.prefab`; remove unused `_pendingHasWin` (`CS0414`) in `LevelRunController.cs`; update `A08`/`A09` and add `A10_DeadCellsPrisonVisualStandard` (`26/26` NG EditMode + `7/7` NG PlayMode + `13/13` Core = **`46/46` Root Tests Passed**).

**Tech Stack:** Unity `6000.6.9f1`, C# 9, URP 2D `17.6.0` (`Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Universal.2D.Runtime`), `Prison` atlas (`374` sprites + `59` Tile assets @ `24 PPU`), Unity Test Framework `1.8.0`, `rtk` CLI wrapper.

**Specs & Benchmarks:**
- `d:\thietkegame\AGENTS.md` (especially §0, §1, §2, §9, §10)
- `Assets/_Project/Art/Environment/Prison/source_manifest.json` (`374` sprites, `59` `.asset` tiles @ `24 PPU`)
- `docs/NhaGiamGiengChuong.md` & `docs/Tests-Nha-Giam-Gieng-Chuong.md` (v7 collision/gameplay invariants preserved 100%)
- `C:\Users\habayakka\.gemini\config\plugins\unity\skills\urp-postprocessing\SKILL.md` (`overrideState = true` rule)

---

## Global Constraints

- **Shell & Encoding:** Run every shell command through `rtk` (or `rtk proxy <command>`). Read/write text files in UTF-8 without BOM.
- **No Manual `.meta` Edits (`AGENTS.md` §2):** Never hand-create, hand-edit, or delete `.meta` files. Let Unity generate the `.meta` automatically when `AssetDatabase.CreateAsset` writes `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset` (keep `0` `.meta` changes on `.fortress-workspaces/antigravity`). Add new tests into existing test files (`MainLevelIntegrityTests.cs`, `MapWiringTests.cs`, `FortressAuthoringPipelineTests.cs`).
- **Unity `6000.6.9f1` Zero-Warning Policy:** Use `Object.FindObjectsByType<T>()` / `Object.FindObjectsByType<T>(FindObjectsInactive.Include)` and `Object.FindAnyObjectByType<T>()` (never `FindObjectsSortMode` / `FindFirstObjectByType<T>()`). Zero `CS0618`, zero `CS0414`.
- **Assembly Boundaries (`AGENTS.md` §0):**
  - `TilesetTools.Editor.asmdef`: add `"Unity.RenderPipelines.Core.Runtime"`, `"Unity.RenderPipelines.Universal.Runtime"`, `"Unity.RenderPipelines.Universal.2D.Runtime"`.
  - `Core.Tests.EditMode.asmdef`: add `"Unity.RenderPipelines.Core.Runtime"`, `"Unity.RenderPipelines.Universal.Runtime"`, `"Unity.RenderPipelines.Universal.2D.Runtime"`.
  - `NhaGiamGiengChuong.Editor.asmdef`: add `"Unity.RenderPipelines.Core.Runtime"` (already has Universal Runtime + 2D Runtime).
  - `NhaGiamGiengChuong.Tests.EditMode.asmdef`: add `"Unity.RenderPipelines.Core.Runtime"` (already has Universal Runtime + 2D Runtime).
- **Single-Unity-Process Lock (`7.5 GB` RAM Host) & Post-Run `ProjectSettings` Hygiene:**
  - Wave 1 Sessions 1 & 2 edit code in separate checkouts in parallel, **but EVERY `Unity.exe` invocation MUST be strictly serialized across the entire machine** — before launching `Unity.exe`, check `Get-Process Unity -ErrorAction SilentlyContinue` is empty (wait if another session's `Unity.exe` is running!).
  - Pass `$env:DOTNET_GCServer='0'` and `-job-worker-count 1` on every Unity invocation; use `-batchmode -nographics` for tests/rebuilds, and omit `-nographics` ONLY for GPU screenshot capture in Task 1 (add `-force-d3d11` fallback if D3D12 OOMs on the `7.5 GB` host).
  - Restore unintended `ProjectSettings/{TimeManager,ProjectSettings}.asset` auto-churn **after** the very last Unity invocation in each checkout (keep `ProjectSettings/EditorBuildSettings.asset` intact on Root).
- **Exact Verified Asset Names (`Prison` & `GothicDungeon`):**
  - `Prison/Tiles/*.asset` (`59` tiles): `backWall_0_00..9_00`, `dirt_00`, `floorStamp_0_00..9_00`, `ground_0_00..3_00`, `ground_left_0_00..1_00`, `ground_right_0_00..1_00`, `ceiling_0_00..3_00`, `stairs_00`, `stairs_0_00`, `stairs_1_00`, `stampWallSmall_0_00..5_00`, `wallJunk_10..13`, `webSmallTL_00`, `webSmallTR_00`, `webSmallBL_00`, `webSmallBR_00`, `chainTile_00`, `chainDown_00`, `ladderHole_00`.
  - `Prison/Sprites/*.png`: `woodenPlatform_left_00.png`, `woodenPlatform_0_00..4_00.png`, `woodenPlatform_right_00.png`, `woodenCornerLeft_00.png`, `woodenCornerRight_00.png`, `littleBlackBeam_topLeft_0_00.png`, `littleBlackBeam_topRight_0_00.png`, `blackBeam_topLeft_0_00.png`, `blackBeam_topRight_0_00.png`, `stampCeilingSmall_0_00.png`, `stampCeilingSmall_1_00.png`, `stampCeilingMedium_0_00.png`, `stampCeilingBig_0_00.png`, `roofMoss_0_00.png..2_00.png`, `stoneStamp_0_00.png..5_00.png`, `webBigTL_00.png`, `webMediumTR_00.png`, `webSmallTL_00.png`, `webSmallTR_00.png`, `hingeSmallDoor_00.png`, `suspendedCage_00.png`, `exitDoor_00.png`, `PortalBackOut_0_00.png`, `godraysArcheTriainingRoom_00.png`, `alcovePillar_00.png`, `archeSmall_00.png`, `prisonBigAlcove_00.png`, `alchemyAlcove_00.png`, `prionSmallWindow_00.png`, `bannerBig_00.png`, `bannerRoomTop_00.png`, `bannerRoomTiled_00.png`, `bannerRoomBot_00.png`, `wallBed_00.png`, `shelf_0_00.png`, `fioleA_00.png`, `fioleB_00.png`, `fioleC_00.png`, `ladderHole_00.png`, `chainTile_00.png`, `chainDown_00.png`, `floorjunk_0_00.png..9_00.png` (and `floorjunk_10.png..42.png` without `_00`).
  - `GothicDungeon/Tiles/Props/*.asset`: load via `LoadTileSprite("Props/LightAndBanner_001")`, `"Props/LightAndBanner_002"`, `"Props/LightAndBanner_005"`, `"Props/StorageAndVessel_003"`, `"Props/StorageAndVessel_010"`, `"Props/StorageAndVessel_014"`, `"Props/MachineryAndBell_001"`, `"Props/MachineryAndBell_012"`.
- **Preserved Invariants:**
  - `NhaGiamGiengChuong` v7 collision geometry (`A01`–`A06`), camera (`orthoSize = 7f`, `bounds = Rect(-32, -4, 110, 47)` per `A07`), all 5 enemies, 4 zones, `I1`–`I6` triggers, `U01`–`U16`, and `P01`–`P07` MUST remain 100% intact.
  - `Main_Level.unity` `Archive gallery` 40 boards, `Upgrade panel` `(1000, 476)`, `Defeat Screen` Victory flow, `PlayerHUD.prefab` `_trailFill`, and all `12/12` existing `DeadCells.Core.Tests` MUST remain 100% intact.

---

## Task 1: AG Fortress Authoring Preview — Fix Shader OOM Unlit Fallback (`shaderErrors == 0`), Dynamic Grounding & Composition (`R3-A1-AG-Preview`)

**Checkout:** `D:/thietkegame/.fortress-workspaces/antigravity` (branch `fortress/antigravity-clean`) — **Wave 1 (Code parallel with Task 2; serialize `Unity.exe`!)**

**Files:**
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressDashboardAndPipelineRunner.cs` (`BuildAndSavePreviewScene`, `RenderPreviewScreenshots`, `RunRound2Step3Batch`, new `RenderRound3ScreenshotsCleanBatch`)
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressRoomAndCollisionTools.cs` (`Room_CathedralHall` preset: paint `TM_Background` stone backdrop + 4-tile collider-valid floor variation on `TM_Floors` + `AlignRoomRuleTileSurfaces()`)
- Modify: `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs` (add `R15_PreviewScene_GroundedPropsNonOverlappingAndLitZeroShaderErrors`)
- Regenerate: `Room_CathedralHall.prefab`, `GothicFortress_ArtPreview.unity`, `docs/Fortress/Reports/antigravity/{screenshots,round2/screenshots}/*.png`, `docs/Fortress/Reports/antigravity/{screenshots/pixel-perfect-capture-metadata.json,round2/screenshots/pixelperfect-capture.json}`, `docs/Fortress/Reports/antigravity/round2/per-test/*.xml`, `acceptance-matrix.md`, `authoring-handoff.md`

- [ ] **Step 1: Add failing EditMode test `R15` in `FortressAuthoringPipelineTests.cs` (Red — compiles cleanly)**

Append `R15_PreviewScene_GroundedPropsNonOverlappingAndLitZeroShaderErrors` in `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`:
1. Open `FortressEditorHelpers.PreviewScenePath` (`GothicFortress_ArtPreview.unity`).
2. Locate `Room_CathedralHall`'s existing Tilemaps **`TM_Floors`** and **`TM_Background`** (preserving all `12` exact `FortressRoomTemplateBuilder.TilemapSpecs` Tilemaps required by `A08_A11`). Use `floorTopY = 1.0f` (verified top surface of `TM_Floors` row `y = 0`).
3. Assert every grounded prop in the preview scene (`PF_BreakableCrate`, `PF_Chest`, `PF_Brazier`, `PF_SpikeTrap`, `PF_Lever`, `PF_KnightStatue`, `PF_Door`, `PF_PrisonGate`, `FG_OverlapPillar`, `BG_Arch_Left`) has its `SpriteRenderer.bounds.min.y` within `±0.10u` of `floorTopY` (zero sinking `6–16 px`, zero floating `6–8 px`!).
4. Assert horizontal non-overlap spacing (iterating **ALL** instances named `"PF_WallTorch"` since `BuildAndSavePreviewScene` spawns two!):
   - Every `PF_WallTorch` instance has `Mathf.Abs(torch.position.x - cyanPortal.position.x) >= 2.0f` (preventing torch embers bleeding over shrine runes).
   - `Mathf.Abs(bgArchLeft.position.x - chest.position.x) >= 2.5f` (preventing stone arch rubble clipping behind chest lid).
5. Assert **`TM_Background`** inside `Room_CathedralHall` has painted tiles behind `BG_GothicWindow_Center` (`TM_Background.GetTile(TM_Background.WorldToCell(new Vector3(0f, 6f, 0f))) != null`) so the gothic window no longer floats in empty void.
6. Assert **`TM_Floors`** row `y = 0` uses `>= 2` distinct sprite names across `x = -12..12` (ending 1-tile monotonous floor repetition).
7. Read BOTH `docs/Fortress/Reports/antigravity/round2/screenshots/pixelperfect-capture.json` AND `docs/Fortress/Reports/antigravity/screenshots/pixel-perfect-capture-metadata.json` and assert BOTH contain `"shaderErrors": 0` (currently missing / `25` in log — fails Red cleanly!).

- [ ] **Step 2: Run `R15` to confirm Red failure**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -runTests -testPlatform EditMode -testFilter "R15_PreviewScene" -testResults D:/thietkegame/.fortress-workspaces/antigravity/Logs/r3-r15-red.xml -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r3-r15-red.log
```
Expected failure: `PF_Chest`/`PF_SpikeTrap`/`PF_Lever` sink below `1.0f`, `FG_OverlapPillar` floats above `1.0f`, right `PF_WallTorch` is `0.7u` from `PF_MagicalPortal_Cyan`, `BG_Arch_Left` is `0.5u` from `PF_Chest`, `TM_Background` has no tile at `(0, 6)`, `TM_Floors` repeats 1 sprite, and metadata JSON lacks `"shaderErrors": 0`.

- [ ] **Step 3: Implement `Room_CathedralHall` backdrop & collider-valid floor variation, dynamic grounding, and `shaderErrors == 0` GPU capture**

1. **`FortressRoomAndCollisionTools.cs` (`Room_CathedralHall` preset inside `FortressRoomBuilder`):**
   - Paint existing **`TM_Background`** (`TM_Background.color = new Color(0.36f, 0.38f, 0.46f, 1f)`) across `x = -17..16, y = 1..16` with `MasonryRow3_001` / `MasonryRow3_003` (calling `tmBg.SetTileFlags(pos, TileFlags.None); tmBg.SetTransformMatrix(pos, Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 1f)));` so each `2u×2u` masonry block aligns cleanly to `1u×1u` joints with zero gaps behind `BG_GothicWindow_Center` and `BG_WallBanner_Right`).
   - Paint existing **`TM_Floors`** row `y = 0` (`x = -18..17`) cycling `(x & 3)` across **4 collider-enabled assets** (all `ColliderType.Grid`, never raw `ColliderType.None` `GothicDungeon/Tiles/*`!) so no identical `RuleTile`s collide horizontally:
     - `0`: `Tile_StoneFloor_Collider` (`MasonryRow1_001`)
     - `1`: `RT_Stone_Normal` (`MasonryRow2_001`)
     - `2`: `RT_Stone_Reinforced` (`MasonryRow2_014`)
     - `3`: `RT_Stone_Cracked` (`MasonryRow2_010`)
     Then call `tmFloors.RefreshAllTiles(); root.GetComponent<FortressRoomDescriptor>().AlignRoomRuleTileSurfaces();` before saving `Room_CathedralHall.prefab` (zero `TILE_COLLIDER_TYPE_NONE`, zero `VISUAL_COLLIDER_MISALIGNMENT`, preserving `A08_A11`, `A12`, `R03`, `R11`, `R12`!).
2. **`FortressDashboardAndPipelineRunner.cs` (`BuildAndSavePreviewScene`):**
   - Add helper `GroundOnFloor(GameObject go, float floorTopY)` reading `SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>(true);` and adjusting `go.transform.position += new Vector3(0f, floorTopY - sr.bounds.min.y, 0f);`. Apply `GroundOnFloor(inst, 1.0f)` to `PF_Door`, `PF_PrisonGate`, `PF_BreakableCrate`, `PF_Chest`, `PF_Brazier`, `PF_SpikeTrap`, `PF_Lever`, `PF_KnightStatue`, `BG_Arch_Left`, and `FG_OverlapPillar`!
   - Non-overlapping readable X positions:
     - `FG_OverlapPillar`: `x = -11.8f` (grounded at `1.0f`), `BG_Arch_Left`: `x = -10.5f` (`4.5u` clear of `PF_Chest`), `PF_BreakableCrate`: `x = -8.5f`, `PF_Chest`: `x = -6.0f`, `PF_WallTorch` (left): `x = -4.2f, y = 3.2f`, `PF_Brazier`: `x = -1.8f`, `PF_SpikeTrap`: `x = 1.0f`, `PF_MagicalPortal_Cyan`: `x = 3.2f`, `PF_WallTorch` (right): `x = 5.6f, y = 3.2f` (`2.4u` clear from portal!), `PF_Lever`: `x = 7.4f`, `PF_KnightStatue`: `x = 9.8f`.
     - Anchor `BG_WallBanner_Right` (`LightAndBanner_005` hanging lantern) directly beneath the ceiling/beam underside (e.g. `y = ceilingBottomY - sr.bounds.extents.y` against the backdrop wall so its chain never terminates in empty void).
3. **`FortressDashboardAndPipelineRunner.cs` (`RunRound2Step3Batch` & new `RenderRound3ScreenshotsCleanBatch`):**
   - In `RunRound2Step3Batch()`:
     1. Run generators at top: `FortressRuleTileGenerator.GenerateAll(); FortressAnimationBuilder.BuildAll(); FortressPrefabGenerator.GenerateAll();`
     2. Run palettes & **rebuild room presets**: `FortressPaletteGenerator.GenerateAll(); FortressPaletteGenerator.GenerateAll(); FortressRoomBuilder.GenerateAllPresets();`
     3. Export catalog & rebuild preview scene: `FortressAuthoringCatalogExporter.ExportCatalog(); FortressArtPreviewBuilder.BuildAndSavePreviewScene();`
     4. Run headless validators ONLY (remove duplicate GPU screenshot capture from `RunRound2Step3Batch()` so Step 4 command #1 runs cleanly in `-nographics` low-RAM mode!).
   - Add `public int shaderErrors;` to `PixelPerfectCaptureReport`.
   - Add dedicated **`RenderRound3ScreenshotsCleanBatch()`** (run in its own fresh GPU Unity process — NO `-nographics`!):
     1. Open `FortressEditorHelpers.PreviewScenePath` and call `EditorUtility.UnloadUnusedAssetsImmediate(); GC.Collect();`.
     2. Clear stale shader error state on `litMat.shader` (`Universal Render Pipeline/2D/Sprite-Lit-Default`): `ShaderUtil.ClearShaderErrors(litShader); ShaderUtil.ClearShaderMessages(litShader);`. Do **NOT** call `ShaderUtil.CompilePass` across `Hidden/Universal/CoreBlit` (the exact trigger of the 22 D3D12 OOM errors in `r2-a1-step3.log`!).
     3. Render 1 warm-up frame via `cam.Render()` (discarded per `visual-scorecard.md` Step 2) + `WaitForShaderCompilation()`, then check `ShaderUtil.ShaderHasError(litShader)` and count `ShaderCompilerMessageSeverity.Error` messages on `litShader`; if `shaderErrors > 0`, **throw `InvalidOperationException`** (hard fail — never silently ship unlit fallback screenshots!).
     4. Capture all 6 shots ONCE into `docs/Fortress/Reports/antigravity/round2/screenshots/` writing `pixelperfect-capture.json` (with `"shaderErrors": 0`), then copy the 6 PNGs into `docs/Fortress/Reports/antigravity/screenshots/` and write `pixel-perfect-capture-metadata.json` (with `"shaderErrors": 0`) so BOTH `R06` and `R15` read verified lit captures!

- [ ] **Step 4: Run headless asset rebuild, run fresh GPU `RenderRound3ScreenshotsCleanBatch`, visually inspect lit PNGs, run ALL `25/25` EditMode tests, clean `ProjectSettings`, commit & tag**

```powershell
# Ensure NO other Unity.exe process is running first!
# 1. Headless rebuild of Room_CathedralHall + GothicFortress_ArtPreview + catalog
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -executeMethod DeadCells.Tools.Editor.FortressProduction.FortressAuthoringPipelineRunner.RunRound2Step3Batch -quit -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r3-a1-build.log

# 2. Fresh GPU process solely for lit 2D screenshot capture (NO -nographics; asserts shaderErrors == 0! If D3D12 hits VRAM/RAM OOM on 7.5GB host, retry with -force-d3d11)
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -executeMethod DeadCells.Tools.Editor.FortressProduction.FortressAuthoringPipelineRunner.RenderRound3ScreenshotsCleanBatch -quit -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r3-a1-render.log
```
Visually open `round2/screenshots/preview_overview.png`, `preview_pixelperfect_props.png`, and `preview_lighting_vfx.png` with `read_file` and verify:
- Real warm orange 2D light pools glow on the stone floor & `TM_Background` wall around torches/brazier, and real cyan glow illuminates around `PF_MagicalPortal_Cyan` (`"shaderErrors": 0` in BOTH JSON metadata files!).
- `PF_Chest`, `PF_SpikeTrap`, `PF_Lever`, `FG_OverlapPillar`, `BG_Arch_Left` sit flush on the stone floor (`y = 1.0f`) with zero sinking/floating and zero overlap between `PF_WallTorch` and `PF_MagicalPortal_Cyan`.
- Floor shows multi-tile stone variation and `TM_Background` masonry sits behind window/lantern.
Run all **`25/25` EditMode tests (`A02`–`A13`, `R01`–`R15`)** individually via `-testFilter` (`-batchmode -nographics -job-worker-count 1`, `$env:DOTNET_GCServer='0'`) into `docs/Fortress/Reports/antigravity/round2/per-test/*.xml`, run `SanitizeReportLogs()`, update `acceptance-matrix.md` & `authoring-handoff.md`, restore any unintended `ProjectSettings/*` churn after the final Unity run, verify `0` `.meta` changes, commit on `fortress/antigravity-clean`, and create local tag `fortress-authoring-r3-visual-ready`.

---

## Task 2: Shared URP Post-Processing Profile & `Main_Level.unity` Dead Cells *Prison* Standard Overhaul (`R3-MainLevel-Prison-Polish`)

**Checkout:** `D:/thietkegame` (Root Repo) — **Wave 1 (Code parallel with Task 1; serialize `Unity.exe`!)**

**Files:**
- Modify: `Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef` (add `"Unity.RenderPipelines.Core.Runtime"`, `"Unity.RenderPipelines.Universal.Runtime"`, `"Unity.RenderPipelines.Universal.2D.Runtime"`)
- Modify: `Assets/_Project/Core/Tests/EditMode/Core.Tests.EditMode.asmdef` (add `"Unity.RenderPipelines.Core.Runtime"`, `"Unity.RenderPipelines.Universal.Runtime"`, `"Unity.RenderPipelines.Universal.2D.Runtime"`)
- Modify: `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs` (add `EnsureSharedPostProcessProfile()` + Dead Cells *Prison* floor/backdrop/pillar/plank/dressing/lighting/post-processing/VFX/frame passes inside `RemediateMainLevel()`)
- Create (via Unity `AssetDatabase.CreateAsset`): `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset` (Unity auto-generates `.meta`; never write `.meta` manually!)
- Modify (via `RemediateMainLevel`): `Assets/_Project/Scenes/Main_Level.unity`
- Modify: `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs` (add `MainLevel_DeadCellsPrisonVisualStandard_ValidatesFloorsPillarsLightingAndPostProcess`)

- [ ] **Step 1: Update `.asmdef` references and add failing visual-standard test to `MainLevelIntegrityTests.cs` (Red — compiles cleanly)**

1. Add `"Unity.RenderPipelines.Core.Runtime"`, `"Unity.RenderPipelines.Universal.Runtime"`, and `"Unity.RenderPipelines.Universal.2D.Runtime"` to BOTH `Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef` and `Assets/_Project/Core/Tests/EditMode/Core.Tests.EditMode.asmdef`.
2. Append `MainLevel_DeadCellsPrisonVisualStandard_ValidatesFloorsPillarsLightingAndPostProcess` to `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs`:

```csharp
        [Test]
        public void MainLevel_DeadCellsPrisonVisualStandard_ValidatesFloorsPillarsLightingAndPostProcess()
        {
            // 1. Solid floors: East wing stone has floorStamp_* caps at top row, dirt_00 at y <= -2 on BOTH wings, no orphaned x=52 wall, plus TM_SolidRim
            var tilemaps = UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>();
            var westStone = tilemaps.SingleOrDefault(t => t.gameObject.name == "Stone visuals");
            var eastStone = tilemaps.SingleOrDefault(t => t.gameObject.name == "East wing stone");
            var westBg = tilemaps.SingleOrDefault(t => t.gameObject.name == "Background");
            var solidRim = tilemaps.SingleOrDefault(t => t.gameObject.name == "TM_SolidRim");
            Assert.IsNotNull(westStone, "Stone visuals tilemap missing.");
            Assert.IsNotNull(eastStone, "East wing stone tilemap missing.");
            Assert.IsNotNull(westBg, "Background tilemap missing.");
            Assert.IsNotNull(solidRim, "TM_SolidRim tilemap missing.");
            Assert.IsNull(solidRim.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>(), "TM_SolidRim must be visual-only.");

            // Check East wing top floor caps: (56, -1) on Floor 45 [top=0], (75, -2) on Lower cell block [top=-1], (100, -1) on Exit hall [top=0]
            foreach (var cell in new[] { new Vector3Int(56, -1, 0), new Vector3Int(75, -2, 0), new Vector3Int(100, -1, 0) })
            {
                var sp = eastStone.GetSprite(cell);
                Assert.IsNotNull(sp, $"East wing stone missing top cap tile at {cell}.");
                StringAssert.StartsWith("floorStamp_", sp.name, $"East wing stone top cap at {cell} must use floorStamp_*, got '{sp.name}'.");
            }
            // Check underground solid core uses opaque dirt_00 (never transparent ground_* rim!) and no orphaned x=52 wall in Stone visuals
            Assert.AreEqual("dirt_00", westStone.GetSprite(new Vector3Int(6, -2, 0))?.name, "West wing underground row y=-2 must use opaque dirt_00.");
            Assert.AreEqual("dirt_00", eastStone.GetSprite(new Vector3Int(75, -3, 0))?.name, "East wing underground row y=-3 must use opaque dirt_00.");
            Assert.AreEqual("dirt_00", eastStone.GetSprite(new Vector3Int(100, -2, 0))?.name, "East wing underground row y=-2 must use opaque dirt_00.");
            Assert.IsNull(westStone.GetSprite(new Vector3Int(52, 4, 0)), "Orphaned wall column at x=52, y=0..9 in Stone visuals must be cleared.");

            // 2. West wing Background extended to y >= 14 (no void cliff at x=52)
            Assert.IsNotNull(westBg.GetSprite(new Vector3Int(40, 14, 0)), "West wing Background must extend up to y=14..15 to eliminate the x=52 void cliff.");

            // 3. East wing pillars grounded flush on their floors (never sunken 4u underground)
            var cellPillar = GameObject.Find("Cell block pillar")?.GetComponent<SpriteRenderer>();
            var exitPillar = GameObject.Find("Exit hall pillar")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(cellPillar);
            Assert.IsNotNull(exitPillar);
            Assert.AreEqual(-1.0f, cellPillar.bounds.min.y, 0.12f, "Cell block pillar bottom must rest flush on Lower cell block floor (y=-1.0).");
            Assert.AreEqual(0.0f, exitPillar.bounds.min.y, 0.12f, "Exit hall pillar bottom must rest flush on Exit hall floor (y=0.0).");

            // 4. West wing One Way Platform 16 / 21 / 28 unified to scale.y >= 0.70 and localPosition.y == 0
            foreach (var platName in new[] { "One Way Platform 16", "One Way Platform 21", "One Way Platform 28" })
            {
                var plat = GameObject.Find(platName);
                Assert.IsNotNull(plat, $"Missing '{platName}'.");
                var boards = plat.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(sr => sr.transform.parent == plat.transform && sr.gameObject.name.StartsWith("Board", StringComparison.Ordinal))
                    .ToArray();
                Assert.GreaterOrEqual(boards.Length, 3, $"'{platName}' must have >= 3 boards.");
                foreach (var b in boards)
                {
                    Assert.GreaterOrEqual(b.transform.localScale.y, 0.70f, $"West wing '{platName}' board must match 0.75 scale.");
                    Assert.LessOrEqual(Mathf.Abs(b.transform.localPosition.y), 0.05f, $"West wing '{platName}' board must align at y=0.");
                }
            }

            // 5. Exit Guard clear of Return to main route overhang (x >= 99)
            var exitGuard = GameObject.Find("Enemy_Shield - Exit Guard");
            Assert.IsNotNull(exitGuard);
            Assert.GreaterOrEqual(exitGuard.transform.position.x, 99.0f, "Exit Guard must stand clear of Return to main route platform overhang.");

            // 6. Dead Cells lighting (Global Light 2D in [0.48, 0.62]), URP Post-Processing (Bloom + Vignette + ACES), Ambient VFX
            var globalLight = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>()
                .FirstOrDefault(l => l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global);
            Assert.IsNotNull(globalLight, "Main_Level missing Global Light 2D.");
            Assert.GreaterOrEqual(globalLight.intensity, 0.48f, "Global Light 2D too dark.");
            Assert.LessOrEqual(globalLight.intensity, 0.62f, "Global Light 2D too flat/bright (must be <= 0.62 for Dead Cells contrast).");

            var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            Assert.IsNotNull(cam);
            var uacd = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            Assert.IsNotNull(uacd);
            Assert.IsTrue(uacd.renderPostProcessing, "Main Camera must enable renderPostProcessing = true.");

            var vol = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>().FirstOrDefault(v => v.enabled && v.isGlobal);
            Assert.IsNotNull(vol, "Main_Level must contain an enabled Global Volume.");
            Assert.IsNotNull(vol.sharedProfile, "Global Volume must reference DeadCells_PostProcessProfile.asset.");
            Assert.IsTrue(vol.sharedProfile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom) && bloom.intensity.overrideState && bloom.intensity.value > 0f, "Bloom override missing.");
            Assert.IsTrue(vol.sharedProfile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette) && vignette.intensity.overrideState && vignette.intensity.value > 0f, "Vignette override missing.");
            Assert.IsTrue(vol.sharedProfile.TryGet<UnityEngine.Rendering.Universal.Tonemapping>(out var tone) && tone.mode.overrideState && tone.mode.value == UnityEngine.Rendering.Universal.TonemappingMode.ACES, "ACES Tonemapping override missing.");

            var ambientVfx = UnityEngine.Object.FindObjectsByType<ParticleSystem>();
            Assert.GreaterOrEqual(ambientVfx.Length, 1, "Main_Level must include ambient ParticleSystem (Dust Motes).");
        }
```

- [ ] **Step 2: Run `MainLevelIntegrityTests` to confirm Red failure**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests" -testResults D:/thietkegame/Logs/r3-mainlevel-red.xml -logFile D:/thietkegame/Logs/r3-mainlevel-red.log
```
Expected failure: `4` existing tests pass; new `MainLevel_DeadCellsPrisonVisualStandard_ValidatesFloorsPillarsLightingAndPostProcess` fails on `TM_SolidRim` missing / `East wing stone` using `ground_*` / orphaned `x = 52` wall / `Ambient light` `0.92f` / `renderPostProcessing == false`.

- [ ] **Step 3: Implement `EnsureSharedPostProcessProfile()` and full Dead Cells *Prison* overhaul in `MainLevelSceneRemediator.cs`**

In `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`:
1. Add `public const string SharedPostProcessProfilePath = "Assets/_Project/Settings/DeadCells_PostProcessProfile.asset";` and `public static VolumeProfile EnsureSharedPostProcessProfile()`:
   - Ensure folder `Assets/_Project/Settings` exists.
   - Load or create `VolumeProfile` at `SharedPostProcessProfilePath` via `ScriptableObject.CreateInstance<VolumeProfile>()` + `AssetDatabase.CreateAsset(...)`.
   - Configure (checking `profile.TryGet<T>` before `profile.Add<T>(true)` for idempotency, setting **`overrideState = true`** on every `VolumeParameter` per `urp-postprocessing` skill!):
     - `Bloom`: `threshold.overrideState = true; threshold.value = 1.05f; intensity.overrideState = true; intensity.value = 0.35f; scatter.overrideState = true; scatter.value = 0.55f;`
     - `Vignette`: `intensity.overrideState = true; intensity.value = 0.24f; smoothness.overrideState = true; smoothness.value = 0.45f; color.overrideState = true; color.value = new Color(0.03f, 0.05f, 0.07f, 1f);`
     - `Tonemapping`: `mode.overrideState = true; mode.value = TonemappingMode.ACES;`
   - `EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();` and return `profile`.
2. Update Pass 6 (`line 234`): set `Enemy_Shield - Exit Guard` default spawn position to `new Vector3(100.5f, 0.05f, 0f)` (matching Pass 11 repositioning cleanly!).
3. Inside `RemediateMainLevel()`, add idempotent Dead Cells *Prison* visual passes (preserving all existing passes 1–8):
   - **Pass 9 — Clear & Repaint Solid Floors (`Stone visuals` + `East wing stone` + `TM_SolidRim` + `TM_WallDecals`):**
     - Load verified `Prison/Tiles/*.asset`: `dirt_00.asset`, `floorStamp_0_00..9_00.asset`, `ground_0_00..3_00.asset`, `backWall_0_00..9_00.asset`, `stampWallSmall_0_00..5_00.asset`, `wallJunk_10..13.asset`.
     - Ensure sibling visual-only Tilemaps under the Grid parent of `Stone visuals`: `TM_SolidRim` (`sortingOrder = -3`, `Sprite-Lit-Default.mat`, `0` colliders) and `TM_WallDecals` (`sortingOrder = -18`, `Sprite-Lit-Default.mat`, `0` colliders).
     - Call **`westStone.ClearAllTiles(); eastStone.ClearAllTiles(); solidRim.ClearAllTiles(); wallDecals.ClearAllTiles();`** (eliminating the orphaned pre-East-Wing wall column at `x = 52, y = 0..9` in `Stone visuals` as well as all 91.5%-transparent `ground_*` underground cells!).
     - Paint the 8 exact verified floor spans `(xMin, xMax, floorTop)`: `(0, 15, 0)`, `(15, 28, -1)`, `(28, 37, 0)`, `(37, 41, 1)`, `(41, 45, 2)`, `(45, 68, 0)`, `(68, 94, -1)`, `(94, 112, 0)`:
       - For every `x` in `[xMin, xMax - 1]`: `targetTm = x < 52 ? westStone : eastStone`. Paint `(x, floorTop - 1, 0)` with `floorStamp[Hash(x, floorTop) % 10]`; paint `(x, y, 0)` for `y = -4 .. floorTop - 2` with `dirt_00`; paint `solidRim` `(x, floorTop, 0)` with `ground[Hash(x, floorTop) % 4]`.
       - Paint boundary walls: `westStone` `x = -1, y = -4..15` with `dirt_00`, `eastStone` `x = 112, y = -4..15` with `dirt_00`.
   - **Pass 10 — Extend West Wing `Background` (`x = -1..52, y = -4..15`) & Paint `TM_WallDecals`:**
     - Fill any missing cell in `Background` across `x = -1..52, y = -4..15` with deterministic `backWall[Hash(x, y) % 10]` (eliminating the `x = 52` void cliff!).
     - Deterministically scatter `stampWallSmall_0_00..5_00` (~14% density) and `wallJunk_10..13` (~4% density) across `TM_WallDecals` (`x = 1..110, y = 1..11`).
   - **Pass 11 — Ground Sunken East Wing Pillars, Unify West Wing Platforms, Shift Exit Guard:**
     - Move `Cell block pillar` → `new Vector3(71f, 1.79f, 0f)` and `Exit hall pillar` → `new Vector3(98f, 2.79f, 0f)` (verified `alcovePillar_00` half-height `2.7917u`, parent at `(0,0,0)`).
     - For `One Way Platform 16`, `One Way Platform 21`, and `One Way Platform 28`: set every `Board` child to `localScale = new Vector3(1f, 0.75f, 1f)` and `localPosition = new Vector3(lp.x, 0f, lp.z)`.
     - Move `Enemy_Shield - Exit Guard` → `new Vector3(100.5f, 0.05f, 0f)`.
   - **Pass 12 — Dead Cells Anti-Box Dressing, Explicit-Coordinate God-Rays, Lighting, Post-Processing, Ambient Dust & Framed Defeat Screen:**
     - Destroy & recreate idempotent container `Prison_Dressing_R3` under `Prison/Decoration` containing verified `Prison/Sprites/*.png` assets:
       - Corner framing & cobwebs (`littleBlackBeam_topLeft_0_00.png`, `littleBlackBeam_topRight_0_00.png`, `webBigTL_00.png`, `webMediumTR_00.png`, `woodenCornerLeft_00.png`, `woodenCornerRight_00.png`),
       - Saturated `bannerBig_00.png` (`x = 4, 48, 92`) & `bannerRoomTop_00.png`/`bannerRoomTiled_00.png`/`bannerRoomBot_00.png` accents (`sort = -12`),
       - Low-contrast `floorjunk_0_00.png..9_00.png` clusters (`sort = -8`, tint `(0.78f, 0.80f, 0.82f)`) grounded near pillar bases & walls (`x = 2, 14, 30, 52, 70, 95` — leaving combat centers 70% clear!),
       - Soft angled **God-rays** (`godraysArcheTriainingRoom_00.png`, `Sprite-Unlit-Default.mat`, `sortingOrder = 10`, tint `new Color(1f, 0.92f, 0.65f, 0.20f)`, scale `(0.55f, 0.55f, 1f)`, rotation `-18°`) placed by **explicit coordinates** matching the verified prison windows: `(7.8f, 5.0f)`, `(25.8f, 5.0f)`, `(45.8f, 5.0f)`, `(64.0f, 6.0f)`, `(79.0f, 8.0f)`, `(104.0f, 5.0f)` (never `GameObject.Find("Window")` since 5 West Wing windows share that name!).
     - Lower `Ambient light` (`Global Light 2D`) to `intensity = 0.56f`, `color = new Color(0.76f, 0.86f, 0.92f, 1f)`.
     - Enable `uacd.renderPostProcessing = true` on `Main Camera`; create/update `Global Volume` (`Volume`, `isGlobal = true`, `gameObject.layer = 0`, `sharedProfile = EnsureSharedPostProcessProfile()`).
     - Create/update `AmbientDustMotes` (`ParticleSystem` using `Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat`, `maxParticles = 24`, box shape covering `x = [0, 112], y = [0, 10]`, size `0.06..0.11`, speed `0.15..0.30`, soft warm-pale alpha `0.24`, `sortingOrder = 12`).
     - Style Defeat Screen frame via exact path `defeatScreen.transform.Find("Backdrop/Frame")` → set `Image.color = new Color(0.72f, 0.56f, 0.28f, 0.92f)` (keeping `Heading`, `Description`, `Retry` RectTransforms untouched!).

- [ ] **Step 4: Run `RemediateMainLevel` and verify `DeadCells.Core.Tests` (`13/13 Passed`, `0` `CS0618`)**

```powershell
# Ensure NO other Unity.exe process is running first!
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.RemediateMainLevel -quit -logFile D:/thietkegame/Logs/r3-mainlevel-remediate.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests" -testResults D:/thietkegame/Logs/r3-core-green.xml -logFile D:/thietkegame/Logs/r3-core-green.log
```
Expected output: `total="13" passed="13" failed="0"`, `0` `CS0618` warnings, `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset` created cleanly.

---

## Task 3: `NhaGiamGiengChuong.unity` Full Dead Cells *Prison* Standard Overhaul (6-Layer `24 PPU` Tiles, Normalized Props, 11/11 Room Lighting, Ambient VFX & Post-Processing) (`R3-NG-Prison-Overhaul`)

**Checkout:** `D:/thietkegame` (Root Repo) — **Wave 2 (Sequential AFTER Task 2 finishes!)**

**Prerequisite Check:** Verify `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset` exists before starting (created by Task 2).

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/LevelRunController.cs` (remove unused `_pendingHasWin` field to eliminate final `CS0414` warning)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef` (add `"Unity.RenderPipelines.Core.Runtime"`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/NhaGiamGiengChuong.Tests.EditMode.asmdef` (add `"Unity.RenderPipelines.Core.Runtime"`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (full Dead Cells *Prison* `24 PPU` visual overhaul + 11/11 room lighting + God-rays + Ambient VFX + Post-Processing + framed `ResultPanel.prefab`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (update `A08`/`A09` + add `A10_DeadCellsPrisonVisualStandard`)
- Regenerate (via `NhaGiamGiengChuongBuilder.BuildAll`): `Rooms/{AB,CD,ELF,G,HJK}.prefab`, `UI/ResultPanel.prefab`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity`

- [ ] **Step 1: Update `.asmdef` references, update `A08`/`A09` and add failing `A10_DeadCellsPrisonVisualStandard` in `MapWiringTests.cs` (Red — compiles cleanly)**

1. Add `"Unity.RenderPipelines.Core.Runtime"` to `NhaGiamGiengChuong.Editor.asmdef` and `NhaGiamGiengChuong.Tests.EditMode.asmdef`.
2. In `MapWiringTests.cs`:
   - In `A08_VisualCoverageGroundedPropsAndCorrectChestSprite`: keep `I3_HealingChest/Visual` (`StorageAndVessel_003`, grounded `<= 0.08u`), `Art_G_Barrel` (`StorageAndVessel_014`), `5` `TM_SolidVisual` tilemaps (`0` `TilemapCollider2D`), all `10` solid sample points covered (`TM_SolidVisual.GetTile(WorldToCell(pt)) != null`), and grounded hero landmarks (`Art_B_Arch1`, `Art_B_Pillar`, `Art_B_Arch2`, `Art_G_Winch`, `Art_J_Arch`, `Art_E_Pillar1`, `Art_E_Pillar2`, `Art_F_ExitArch`, `Art_F_ExitGlow` with `SpriteRenderer` grounded at `32f ± 0.12f` + `Light2D`).
   - In `A09_SceneHudAndChestWorldUiAreWiredAndVisible`: update step 3 `Global Light 2D` check to Dead Cells range `Assert.GreaterOrEqual(globalLight.intensity, 0.48f)` and `Assert.LessOrEqual(globalLight.intensity, 0.62f)`.
   - Append **`A10_DeadCellsPrisonVisualStandard`**:
     1. **Native `1.0u` `Prison` Grid & Zero Mixel Explosion:** Every room `VisualGrid` has `cellSize == (1f, 1f, 0f)`, `>= 20` visual-only Tilemaps (`5` rooms × `4`: `TM_Background`, `TM_WallDecals`, `TM_SolidVisual`, `TM_SolidRim`), `TM_Background` uses `backWall_*` tiles (`scale == 1.0`, `mixelRatio <= 1.5f`), walkable solid floors (`(10, -1)`, `(60, 7)`, `(74, 7)`, `(10, 31)`) use `floorStamp_*`, and `Stairs_H` interior `(10, 11)` uses opaque `dirt_00` (not mossy `MasonryRow1_008`!).
     2. **Prop Proportion Discipline (`Player = 1.66u`):** `I3_HealingChest/Visual`, `Art_G_Barrel`, and `Art_G_Crate` all have `sr.bounds.size.y <= 1.35f`; `I4_EncounterGate/GateBarrier` `sr.bounds.size.x <= 1.25f` (no `4.47u` protruding double gate!); `Art_A_Cage` `sr.bounds.min.y >= 3.0f` (well above Player head!); `PromptIndicator` bottom (`localPosition.y - 0.36f`) sits strictly above `I3_HealingChest/Visual` top (`>= +0.20u` gap!).
     3. **No Floating Dining Tables on One-Way Platforms:** Zero `SpriteRenderer`s in the scene use `WoodPlatform_007` or `WoodPlatform_008`; all 10 plank rows (`Art_A_PracticePlanks`, `Art_C_RestPlanks`, `Art_C_TopPlanks`, `Art_G_UpperPlanks`, `Art_K_TopPlanks`, `Art_E_CombatPlanks`, `Art_L1..L4_Planks`) use `woodenPlatform_*` sprites.
     4. **Full 11/11 Room Point-Light Coverage (`>= 75%` total, `>= 50%` in every single room!):** Sample `1×1u` grid points inside all 11 room rects (`A`, `B`, `C`, `D`, `G`, `H`, `J`, `K`, `E`, `L`, `F`) against all `Point` `Light2D` components (`Vector2.Distance(pt, lightPos) <= light.pointLightOuterRadius`). Assert total coverage `>= 0.75f` AND every individual room `>= 0.50f`!
     5. **URP 2D Post-Processing & Ambient VFX:** `Main Camera` `UniversalAdditionalCameraData.renderPostProcessing == true`; active global `Volume` references `DeadCells_PostProcessProfile.asset` with `Bloom`, `Vignette`, and `Tonemapping (ACES)` overrides enabled; `>= 2` ambient `ParticleSystem`s exist in the scene.

- [ ] **Step 2: Run `A08`, `A09`, `A10` to verify Red failure**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "A08_VisualCoverage|A09_SceneHud|A10_DeadCellsPrison" -testResults D:/thietkegame/Logs/r3-ng-red.xml -logFile D:/thietkegame/Logs/r3-ng-red.log
```
Expected failure: `A09` fails on `Global Light = 0.38 < 0.48`; `A10` fails on `0.5u` grid, `MasonryRow1_008`, giant `2.06u` chest, `4.47u` gate, `WoodPlatform_007/008`, `20.4%` light coverage, and missing Post-Processing/Volume.

- [ ] **Step 3: Clean `CS0414` in `LevelRunController.cs` and implement full Dead Cells *Prison* Standard in `NhaGiamGiengChuongBuilder.cs`**

1. **`LevelRunController.cs`:** Remove the unused private field `_pendingHasWin` (line `54`) and its dead assignments in `RequestWin`/`RequestLoss`/`ResolvePendingOutcome` (`ResolvePendingOutcome` already decides `LevelOutcome finalOutcome = _pendingHasLoss ? LevelOutcome.Loss : LevelOutcome.Win;` when `_hasPendingCandidate` is true — identical behavior, `0` warnings!).
2. **`NhaGiamGiengChuongBuilder.cs` — Core Dead Cells *Prison* Helpers:**
   - Constants:
     - `PrisonTilesRoot = "Assets/_Project/Art/Environment/Prison/Tiles"`
     - `PrisonSpritesRoot = "Assets/_Project/Art/Environment/Prison/Sprites"`
     - `SpriteUnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat"`
     - `ParticlesUnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat"`
     - `SharedPostProcessProfilePath = "Assets/_Project/Settings/DeadCells_PostProcessProfile.asset"`
   - **`CreateRoomVisualGrid` (native `1.0u` grid, 4 visual-only Tilemaps, `0` `TilemapCollider2D`):**
     - `VisualGrid`: `Grid.cellSize = new Vector3(1f, 1f, 0f)`.
     - `TM_Background` (`sortingOrder = -20`, `Color.white`, `Sprite-Lit-Default`).
     - `TM_WallDecals` (`sortingOrder = -18`, `new Color(0.88f, 0.92f, 0.90f, 1f)`, `Sprite-Lit-Default`).
     - `TM_SolidVisual` (`sortingOrder = -5`, `Color.white`, `Sprite-Lit-Default`).
     - `TM_SolidRim` (`sortingOrder = -3`, `Color.white`, `Sprite-Lit-Default`).
   - **Exact 1.0u Tile Painters (Zero Overwrite Collisions at All 9 Wall/Ceiling/Header Junctions!):**
     - Always call `tm.SetTileFlags(cell, TileFlags.None);` defensively before `tm.SetTransformMatrix(cell, matrix);`.
     - `PaintPrisonBackWall(bgTm, decalTm, xMin, xMax, yMin, yMax)`: fills `[Mathf.FloorToInt(xMin) - 1 .. Mathf.CeilToInt(xMax)] × [Mathf.FloorToInt(yMin) - 1 .. Mathf.CeilToInt(yMax)]` (1-cell bleed behind solid walls) with deterministic `backWall_0_00..9_00.asset` at identity matrix, and paints `decalTm` strictly inside interior `[xMin+1..xMax-2] × [yMin+1..yMax-2]` with `stampWallSmall_0_00..5_00.asset` (~15% hash) & `wallJunk_10..13.asset` (~4% hash).
     - `PaintPrisonSolidFloor(solidTm, rimTm, xMin, xMax, yMin, floorTop)`: integer cells `cx` from `Floor(xMin)` to `Ceil(xMax) - 1`: top row `cy = Round(floorTop) - 1` gets `floorStamp_0_00..9_00.asset` (identity); deeper rows `cy < Round(floorTop) - 1` get `dirt_00.asset`; rim row `cy = Round(floorTop)` on `rimTm` gets `ground_0_00..3_00.asset`.
     - **Outer Walls & Ceilings (Zero Corner Overwrite):**
       - Extend the 4 outer walls + `Wall_F_West` all the way to their ceiling `yMax` (`Wall_A_West` `[-0.5, 0]×[-1, 6.0]`, `Wall_G_East` `[76, 76.5]×[-1, 15.0]`, `Wall_K_West` `[-30.5, -30]×[15, 31.0]`, `Wall_L_East` `[38, 38.5]×[23, 39.0]`, `Wall_F_West` `[-0.5, 0]×[31, 38.5]`) using `dirt_00.asset` with `scaleX = 0.5f, offsetX = ±0.25f` (`WorldToCell(-0.25, 2.5) = (-1, 2)` and `(76.25, 5.0) = (76, 5)` both covered cleanly!).
       - Trim ceilings to **inner integer X spans** (`Ceiling_A [0, 18]`, `Ceiling_G [56, 76]`, `Ceiling_K [-30, -16]`, `Ceiling_L [22, 38]`, `Ceiling_F [0, 22]`) so ceilings NEVER collide with outer wall cells! Paint `dirt_00.asset` (`scaleY = ceilingTop - ceilingBottom`, `offsetY` aligned to `[ceilingBottom, ceilingTop]`) on `TM_SolidVisual` + `ceiling_0_00..3_00.asset` on `TM_SolidRim` cleanly aligned to `ceilingBottom`.
     - **Interior Doorway Headers (Zero Ceiling Overwrite + Exact Fractional Alignment):**
       - In `PaintPrisonInteriorHeader(solidTm, artRoot, mat, name, centerX, yMin, yMax)`:
         1. Paint `dirt_00.asset` (`scaleX = 0.5f`, `offsetX = centerX - (cx + 0.5f)`) on `TM_SolidVisual` ONLY for integer cells `(cx, cy)` (`cx = Mathf.FloorToInt(centerX)`) that do **not** already contain a ceiling tile (`solidTm.GetTile(cell) == null`) — so `(18, 5)` paints on `TM_SolidVisual` and `(-16, 22)` keeps `Ceiling_HJ` on `TM_SolidVisual` (both pass `A08`!).
         2. For any header touching a ceiling cell or fractional height (`Wall_CD_Upper_42`, `Wall_DH_Header_22`, `Wall_HJ_Header_6`, `Wall_JK_Mid_Neg16`, `Wall_LF_Header_22`, `Wall_H_EastUpper_22`, `Wall_AB_Header_18`, `Wall_BC_Header_42`, `Wall_CG_Mid_56`, `Wall_CG_Upper_56`, `Wall_EL_Header_22`, `Wall_KE_Header_Neg16`), render the exact `[centerX - 0.25f, centerX + 0.25f] × [yMin, yMax]` solid black beam via a `dirt_00.png` `SpriteRenderer` (`sort = -5`, `Sprite-Lit-Default`) so every single junction is **100% seamless with zero `0.5u` notches or missing corners**!
     - `PaintPrisonStairsH(solidTm, rimTm, artRoot, mat)`: paints `floorStamp_0_00..9_00` on landing `[18, 22]×[7, 8]` and top of each step (`[15,18]@9`, `[12,15]@11`, `[9,12]@13`, `[6,9]@15`), **`dirt_00`** across the entire stair under-core (`y = 7` up to step top - 1), `stairs_0_00`/`stairs_1_00` accents along step faces on `rimTm`, and subtle `stoneStamp_0_00..5_00.png` sprites (`sort = -4`) inside the black stair core!
   - **Prison Walkable Planks (`AddPrisonPlankRow`):**
     - Uses `Prison/Sprites/woodenPlatform_left_00.png`, `woodenPlatform_0_00..4_00.png`, `woodenPlatform_right_00.png` at `localScale = (1f, 0.75f, 1f)` placed at `y = topY - 0.125f` (`1.0u` spacing across `[xMin, xMax]`) — applied to **ALL 10 one-way platforms** (zero `WoodPlatform_007/008`!). Attach `woodenCornerLeft_00.png` / `woodenCornerRight_00.png` (`sort = -3`) where platforms meet walls!
   - **Prison Chains (`AddPrisonChainArt`):**
     - Places `Prison/Sprites/ladderHole_00.png` at `(x, deckTopY)`, repeats `Prison/Sprites/chainTile_00.png` across `[floorTopY + 1.0f, deckTopY - 0.5f]`, and caps bottom with `Prison/Sprites/chainDown_00.png` at `floorTopY + 0.35f` (zero floor stab, zero sky poke!).
   - **Anti-Box Framing & Room-by-Room Prison Dressing + 11/11 Room Lighting (`87.1%` verified coverage!):**
     - Add `AddCornerBeamsAndWebs(artRoot, rimTm, mat, xMin, xMax, ceilingBottom, isTallRoom)` using verified filenames: `littleBlackBeam_topLeft_0_00.png` / `littleBlackBeam_topRight_0_00.png` (rooms A, B, D, E, F, J), `blackBeam_topLeft_0_00.png` / `blackBeam_topRight_0_00.png` (tall rooms C, G, H, K, L), `stampCeilingSmall_0_00.png` / `stampCeilingMedium_0_00.png`, `roofMoss_0_00..2_00.png`, and `webSmallTL_00.asset`/`webSmallTR_00.asset` on `rimTm`!
     - Add `AddStandingTorchBeacon(artRoot, name, mat, x, floorY, lightColor, outerRadius, intensity)` using **`LoadTileSprite("Props/LightAndBanner_001")`** (or `_002`) scaled **`0.55×`** (`0.67u × 1.79u`, grounded at `y = floorY - sprite.bounds.min.y * 0.55f`) + `Point Light2D` (`intensity = 1.25f`, `outerRadius = 6.0f`, `innerRadius = 2.0f`)!
     - Add `AddCeilingLanternBeacon(artRoot, name, mat, x, ceilingBottomY, lightColor, outerRadius, intensity)` using **`LoadTileSprite("Props/LightAndBanner_005")`** scaled **`0.55×`** anchored flush to `ceilingBottomY` (`y = ceilingBottomY - sprite.bounds.max.y * 0.55f`) + `Point Light2D`!
     - Add `AddWindowWithGodray(artRoot, name, matLit, matUnlit, pos)` placing `Prison/Sprites/prionSmallWindow_00.png` (`sort = -15`) + soft angled `godraysArcheTriainingRoom_00.png` (`sort = 10`, `Sprite-Unlit-Default`, color `(1f, 0.92f, 0.68f, 0.18f)`, scale `0.42f`, rotation `-18°`)!
     - Place lights across **ALL 11 rooms** (verified `87.1%` total, `66.7%–98.0%` per room at `outerRadius = 6.0f`):
       - Room A: standing torch `(11, 0)`
       - Room B: standing torches `(24, 0)` & `(36, 0)`
       - Room C: standing torches `(49, 0)` & `(49, 8)` + ceiling lantern `(49, ceilingBottomY = 18.5f)`
       - Room D: standing torches `(27, 8)` & `(36, 8)` + 3 windows with god-rays
       - Room G: standing torch `(60, 0)` + chest emerald `GlowHighlight` `(72, 1)` + upper deck torches `(62, 8)` & `(73, 8)`
       - Room H: standing torches `(19, 8)`, `(12, 12)`, `(8, 16)`
       - Room J: standing torches `(-10, 16)` & `(0, 16)`
       - Room K: standing torches `(-24, 16)` & `(-22, 24)` + ceiling lantern `(-23, ceilingBottomY = 30.5f)`
       - Room E: standing torches `(-10, 24)`, `(3, 24)`, `(16, 24)`
       - Room L: standing torches `(35, 26)` & `(27, 30)` + ceiling lantern **`(32, ceilingBottomY = 38.5f)`** (strictly anchored to `Ceiling_L`!)
       - Room F: `Art_F_ExitGlow` `(5, 33.375)` (`outerRadius = 6.5f`) + standing torch `(15, 32)`!
     - **Hero Props Normalized (`Player = 1.66u`) & Exact `A06`/`A08` Compatibility:**
       - `I3_HealingChest/Visual`: `LoadTileSprite("Props/StorageAndVessel_003")` at `scale = 0.55f`, grounded on `y = 0`; `PromptIndicator` at `localPosition = (0f, 1.85f, 0f)` (`+0.36u` above chest lid!); `FullHealthFeedback` at `(0f, 2.20f, 0f)`.
       - `Art_G_Barrel` (`x = 60, y = 0`): `LoadTileSprite("Props/StorageAndVessel_014")` at `scale = 0.58f`; `Art_G_Crate` (`x = 64.5, y = 0`): `LoadTileSprite("Props/StorageAndVessel_010")` at `scale = 0.55f`; `Art_G_Winch` (`x = 68, y = 8`): `LoadTileSprite("Props/MachineryAndBell_012")` at `scale = 0.58f`, `sort = -1`.
       - `Art_A_Cage`: `Prison/Sprites/suspendedCage_00.png` (`1.12u × 2.0u`) ceiling-anchored at `y = 5.5` (`bottom = 3.5u`).
       - `Art_C_Bell`: `LoadTileSprite("Props/MachineryAndBell_001")` at `scale = 0.65f` ceiling-anchored at `y = 18.5`.
       - **`I4_EncounterGate/GateBarrier` (`A06`/`A07`/`A10` exact math):** Use `Prison/Sprites/hingeSmallDoor_00.png` (`1.0u × 3.0u`), set `gateBarrier.transform.localScale = new Vector3(0.85f, 4f / 3f, 1f)` (`sr.bounds.size = (0.85u, 4.0u)`) AND compensate `gateCol.size = new Vector2(0.5f / 0.85f, 3f)` → world `BoxCollider2D` bounds are exact `0.5u × 4.0u` (`min.y == 24.0f`, `max.y == 28.0f`, `A06` passes with `0.000u` error!).
       - **Room F (`Art_F_ExitArch` + `Art_F_ExitGlow`):** `Art_F_ExitArch` = `Prison/Sprites/exitDoor_00.png` grounded at `(5, 32)` (`scale = 0.80f`, `sort = -5`); `Art_F_ExitGlow` = **`Prison/Sprites/PortalBackOut_0_00.png`** (`Sprite-Unlit-Default.mat`, soft golden glow tint `new Color(1f, 0.92f, 0.65f, 0.45f)`, `sort = -4`, `scale = 0.55f`, positioned at `(5f, 32f - glowSprite.bounds.min.y * 0.55f)` = `(5f, 33.375f)` so `sr.bounds.min.y == 32.0f` passes `A08` unchanged!) + `Light2D` (`intensity = 1.35f`, `outerRadius = 6.5f`)!
       - Grounded architectural props keeping `A08` names cleanly: `Art_B_Arch1` (`archeSmall_00.png`), `Art_B_Pillar` (`alcovePillar_00.png` scaled `0.85f`), `Art_B_Arch2` (`archeSmall_00.png`), `Art_J_Arch` (`prisonBigAlcove_00.png`), `Art_E_Pillar1`/`2` (`alcovePillar_00.png`), plus `bannerBig_00.png`, 3-piece `bannerRoomTop/Tiled/Bot_00.png`, `wallBed_00.png`, `shelf_0_00.png`, `fioleA_00.png`/`fioleB_00.png`/`fioleC_00.png`, `alchemyAlcove_00.png`, and low-contrast `floorjunk_0_00..9_00.png` corner clusters!
   - **Scene Lighting, URP Post-Processing, Ambient VFX & Framed `ResultPanel.prefab`:**
     - `Global Light 2D`: `intensity = 0.54f`, `color = new Color(0.76f, 0.85f, 0.92f, 1f)`.
     - Enable `uacd.renderPostProcessing = true` on `Main Camera` (keeping `cam.orthographicSize = 7f` and `CameraFollow2D._bounds = Rect(-32, -4, 110, 47)` per `A07`!).
     - Add `Global Volume` (`Volume`, `isGlobal = true`, layer `0`) wired to `AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedPostProcessProfilePath)`.
     - Add `AmbientVFX` container with `AmbientDustMotes` (`maxParticles = 45`, `ParticlesUnlit.mat`, `sort = 12`) and `AmbientDrippingWater` (vertical drips in Rooms C/G/K, `ParticlesUnlit.mat`, `sort = 11`).
     - In `BuildResultPanelPrefab()`: wrap `TitleText`, `SubtitleText`, and `ReplayButton` inside a styled Dead Cells dark-gothic `CardBorder` (`780×360`, `(0.68f, 0.54f, 0.28f, 0.95f)`) + `CardFill` (`772×352`, `(0.07f, 0.08f, 0.12f, 0.96f)`) + thin horizontal `Divider` (`560×2`), keeping serialized `_panelRoot`, `_titleText`, `_subtitleText`, `_replayButton` references 100% wired!

- [ ] **Step 4: Rebuild `NhaGiamGiengChuong`, run ALL `46/46` Root Tests, verify `0` warnings, and restore `ProjectSettings` churn**

```powershell
# Ensure NO other Unity.exe process is running first!
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -quit -logFile D:/thietkegame/Logs/r3-ng-build.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests" -testResults D:/thietkegame/Logs/r3-ng-editmode.xml -logFile D:/thietkegame/Logs/r3-ng-editmode.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform PlayMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.Play" -testResults D:/thietkegame/Logs/r3-ng-playmode.xml -logFile D:/thietkegame/Logs/r3-ng-playmode.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests" -testResults D:/thietkegame/Logs/r3-all-core-green.xml -logFile D:/thietkegame/Logs/r3-all-core-green.log
# After the VERY LAST Unity invocation on D:/thietkegame, restore unintended Unity auto-churn (keep EditorBuildSettings.asset intact):
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
```
Expected output:
- `r3-ng-editmode.xml`: `total="26" passed="26" failed="0"` (`U01`–`U16` + `A01`–`A10`)
- `r3-ng-playmode.xml`: `total="7" passed="7" failed="0"` (`P01`–`P07`)
- `r3-all-core-green.xml`: `total="13" passed="13" failed="0"`
- Total Root: **`46/46 Passed`**, `0` `CS0618`, `0` `CS0414` across all `r3-*.log` files.

---

## Wave Dispatch Matrix (Parallel Code Edits / Serialized `Unity.exe` + Sequential Wave 2)

| Session / Prompt | Execution Order | Checkout | Assigned Task | Owned Files |
|---|---|---|---|---|
| **Session 1 (`R3-A1-AG-Preview`)** | **Wave 1 (Code parallel w/ Session 2; serialize `Unity.exe`!)** | `D:/thietkegame/.fortress-workspaces/antigravity` | **Task 1** | `FortressDashboardAndPipelineRunner.cs`, `FortressRoomAndCollisionTools.cs`, `FortressAuthoringPipelineTests.cs`, `Room_CathedralHall.prefab`, `GothicFortress_ArtPreview.unity`, refreshed lit PNGs & reports (`25/25` tests, `shaderErrors == 0`, tag `fortress-authoring-r3-visual-ready`) |
| **Session 2 (`R3-MainLevel-Prison-Polish`)** | **Wave 1 (Code parallel w/ Session 1; serialize `Unity.exe`!)** | `D:/thietkegame` | **Task 2** | `TilesetTools.Editor.asmdef`, `Core.Tests.EditMode.asmdef`, `MainLevelSceneRemediator.cs`, `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset`, `Main_Level.unity`, `MainLevelIntegrityTests.cs` (`13/13` Core tests) |
| **Session 3 (`R3-NG-Prison-Overhaul`)** | **Wave 2 (Sequential AFTER Session 2)** | `D:/thietkegame` | **Task 3** | `LevelRunController.cs`, `NhaGiamGiengChuong.Editor.asmdef`, `NhaGiamGiengChuong.Tests.EditMode.asmdef`, `NhaGiamGiengChuongBuilder.cs`, `MapWiringTests.cs`, `Rooms/*.prefab`, `UI/ResultPanel.prefab`, `NhaGiamGiengChuong.unity` (`46/46` Root tests + final `ProjectSettings` cleanup) |
