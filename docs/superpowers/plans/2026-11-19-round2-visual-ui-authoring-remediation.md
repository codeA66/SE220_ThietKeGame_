# Round 2 Remediation Plan — Visual Architecture, UI/UX, Victory Flow & Authoring Refresh

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate every P0, P1, and P2 defect identified in the post-Prompt-1–5 deep review (`deep_review_post_prompts.md`) across `NhaGiamGiengChuong.unity`, `Main_Level.unity`, `PlayerHUD.prefab`, `RunFlow`, and `.fortress-workspaces/antigravity` without regressing any of the existing `40` Root tests or `23` Authoring tests.

**Architecture:** Remediation is split into four file-disjoint tasks organized into two waves:
1. **Task 1 (`R2-A1` — Fortress Authoring `.fortress-workspaces/antigravity`):** Fix `PF_SpikeTrap`, `Anim_Spike_Static`, and `Tile_SpikeHazard_Collider` (`CrystalAndSpike_012` upward floor spikes) plus `PF_Lever` and `Anim_Lever_StaticIdle` (`MachineryAndBell_011` mechanical control mechanism); add `manifestPathOverride` / `rootOverride` + incremental palette upsert (`UpsertGroupPalette` + `EnsureGridPaletteSubAsset`) to AG's `GothicDungeonImporter.cs`; rebuild `GothicFortress_ArtPreview.unity` before rendering screenshots so both `round2/screenshots/*.png` and `screenshots/*.png` show the fixed props and transparent `4×4` pixel particles; run all `24/24` EditMode tests (`A02`–`A13`, `R01`–`R14`); update handoff docs and tag `fortress-authoring-r2-ready-v2`.
2. **Task 2 (`R2-HUD-RunFlow` — Root `PlayerHUD.prefab` & `RunFlow` Victory Wiring):** Add and wire a real `TrailFill` `Image` inside `PlayerHUD.prefab` so `HealthBarView._trailFill` runs in live gameplay; wire `DefeatScreen` to listen to `RunSession.Completed` (rendering Victory heading/description, applying a cached `BlockAllDamage` filter in `RunSession.CompleteRun()` cleanly restored in `OnDisable()`, and reusing `RunSceneRestart` so touching `Exit Door` in `Main_Level.unity` completes the run cleanly with zero soft-lock); add discriminating prefab/victory EditMode tests.
3. **Task 3 (`R2-MainLevel` — `Main_Level.unity` UI Overlap, Platform Aspect Ratio & Hygiene):** Resize `Upgrade panel` to `(1000, 476)` and reposition `Title` → `y = +200`, `Instruction` → `y = +158` (strictly inside `Upgrade panel` `[-238, +238]` and strictly above `Brutality`/`Tactics`/`Survival` top edge `y = +137`); resize `Defeat Screen/.../Description` to `(400, 54)` at `y = +4` and wire `DefeatScreen._heading` + `_description`; restore `Archive gallery` 40 boards to `localScale = (1f, 0.75f, 1f)` at **`localPosition.y = 0f`** (matching the verified `+4/24 * 0.75 = +0.125u` opaque plank top to `BoxCollider2D` top `+0.125u` with `0.000u` offset); clear Unity `6000.6.9f1` `CS0618` warnings in touched files.
4. **Task 4 (`R2-NG-Visual-HUD` — `NhaGiamGiengChuong` Tilemap Visual Architecture, Grounded Props, Chest Swap, HUD & World-Space Prompts):** Upgrade `NhaGiamGiengChuongBuilder.cs` to generate visual-only `Grid` (`0.5u` cells) + `2` `Tilemap` layers (`TM_Background` and `TM_SolidVisual`, `0` `TilemapCollider2D`) using per-tile exact scale (`0.5f / sprite.bounds.size`) and a dedicated half-offset vertical wall painter (`PaintHalfOffsetVerticalWall`) covering 100% of walls, ceilings, all `12` interior doorway headers, `3` previously invisible floors (`Floor_G_UpperWest`, `Floor_G_UpperEast`, `Stairs_H` landing), and the `Stairs_H` stone core; swap `I3_HealingChest/Visual` (`StorageAndVessel_003` golden chest) ↔ `Art_G_Barrel` (`StorageAndVessel_014` barrel); ground every floating prop (keeping `Art_F_ExitGlow`'s `Light2D` via grounded `AddGuideBeacon`) and ceiling-anchor hanging props; align `WoodPlatform_001` walkable deck to `topY - 0.02f` and clamp `WoodPlatform_007`/`008` height to `2.0u` clearance; instantiate and wire `PlayerHUD.prefab`; create visible World-Space UI canvases (`LegacyRuntime.ttf`, explicit `sizeDelta`, `raycastTarget = false`) for `PromptIndicator` (`"[E] Hồi đầy máu"`) and `FullHealthFeedback` (`"Đầy máu"`) and auto-hide `_fullHealthFeedback` on exit/damage/death in `HealingChest.cs`; tune Dead Cells contrast lighting (`Global Light 2D` `0.38f`, warm beacons `1.25f`); polish `ResultPanel.prefab` (Victory/Defeat title tints, elapsed run time, `[R]` shortcut hint); call `_hitStop.SetMenuPaused(false)` **before** `Time.timeScale = 1f` in `LevelRunController.TryReplay()`; clear `CS0618` warnings across all NG runtime/editor/EditMode/PlayMode files; add `A08` & `A09` while preserving `30/30` existing tests (`U01`–`U16`, `A01`–`A07`, `P01`–`P07`); and restore unintended `ProjectSettings/{TimeManager,ProjectSettings}.asset` churn after the final Unity run.

**Tech Stack:** Unity `6000.6.9f1`, C# 9, URP 2D `17.6.0`, `com.unity.2d.tilemap.extras` `9.0.1`, Unity Input System `1.20.1`, Unity Test Framework `1.8.0` (NUnit 3.5), `rtk` CLI wrapper.

**Spec:**
- `d:\thietkegame\AGENTS.md`
- `d:\thietkegame\docs\NhaGiamGiengChuong.md`
- `d:\thietkegame\docs\Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md`
- `d:\thietkegame\docs\Tests-Nha-Giam-Gieng-Chuong.md`
- `d:\thietkegame\docs\superpowers\plans\2026-11-19-map-tileset-visual-gameplay-remediation.md`

## Global Constraints

- **Shell & Encoding:** Run every shell command through `rtk` (or `rtk proxy <command>`). Read/write all text files in UTF-8 without BOM.
- **No Manual `.meta` Edits (`AGENTS.md` §2):** Never create, edit, or delete `.meta` files manually. On `.fortress-workspaces/antigravity`, keep `0` `.meta` changes. On `D:/thietkegame`, add new test methods into existing test files (`RunFlowAndHudTests.cs`, `MainLevelIntegrityTests.cs`, `MapWiringTests.cs`).
- **Unity `6000.6.9f1` Zero-CS0618 Policy:** In every file touched by Tasks 1–4, use `Object.FindObjectsByType<T>()` / `Object.FindObjectsByType<T>(FindObjectsInactive.Include)` and `Object.FindAnyObjectByType<T>()` instead of deprecated `FindObjectsSortMode` / `FindFirstObjectByType<T>()`.
- **Assembly Boundaries (`AGENTS.md` §0):**
  - `RunFlow.asmdef`: `Combat`, `UnityEngine.UI` only (do NOT add `Player`).
  - `TilesetTools.Editor.asmdef`: add explicit `"UnityEngine.UI"` reference alongside `Core`, `Combat`, `Currency`, `Enemies`, `HUD`, `Player`, `RunFlow`, `VFX`, `Unity.2D.Sprite.Editor`, `Unity.2D.Tilemap.Editor`.
  - `NhaGiamGiengChuong.Editor.asmdef`: add `"HUD"`.
  - `NhaGiamGiengChuong.Tests.EditMode.asmdef`: add `"HUD"`, `"Unity.RenderPipelines.Universal.Runtime"`, and `"Unity.RenderPipelines.Universal.2D.Runtime"` (required for `Light2D` check in `A09`).
- **Single-Unity-Process & Post-Run `ProjectSettings` Hygiene:** Never run two `Unity.exe` processes concurrently on the `7498 MB` host. Always pass `-batchmode -nographics -job-worker-count 1` with `$env:DOTNET_GCServer='0'` for EditMode/PlayMode tests, and omit `-nographics` only for GPU screenshot rendering (`RunRound2Step3Batch` / `RenderPreviewScreenshots`). Restore unintended `ProjectSettings/*` auto-churn **after** the very last Unity invocation in each checkout (never before a subsequent Unity batch run).

## Review Focus

- **TDD Compilability:** Every Red step compiles cleanly and fails specifically on runtime/asset assertions (never on missing symbols `CS0117`/`CS1061`/`CS0234`).
- **Visual Completeness (`A08`):** Every solid floor, outer wall, ceiling, stair core, and interior header wall (`12` doorway headers) in `NhaGiamGiengChuong.unity` has painted `TM_SolidVisual` cells (`0` `TilemapCollider2D`); zero props float `> 0.12u`; `Art_F_ExitGlow` retains its `Light2D`; `I3_HealingChest` uses `StorageAndVessel_003` and `Art_G_Barrel` uses `StorageAndVessel_014`.
- **Live UI/UX Completeness (`A09` & `MainLevelIntegrityTests`):** `PlayerHUD.prefab` wires a live `TrailFill` sibling behind `Fill`; `Main_Level.unity` `Upgrade panel` contains `Title` and `Instruction` strictly inside its bounds with zero overlap on `Brutality`/`Tactics`/`Survival`; `Defeat Screen/.../Description` fits 2 lines without truncation; `RunSession.Completed` reveals Victory UI and blocks late damage; `NhaGiamGiengChuong.unity` wires `PlayerHUD`, visible World-Space UI `Text` (`LegacyRuntime.ttf`) on `PromptIndicator` & `FullHealthFeedback`, and auto-hides `_fullHealthFeedback` when leaving trigger or damaged.
- **Authoring Visual Proof (`R13`, `R14` & Fresh PNGs):** `PF_SpikeTrap`, `Anim_Spike_Static`, and `Tile_SpikeHazard_Collider` bind `CrystalAndSpike_012`; `PF_Lever` and `Anim_Lever_StaticIdle` bind `MachineryAndBell_011`; `GothicFortress_ArtPreview.unity` is rebuilt before screenshot capture; all `24/24` Authoring EditMode tests pass (`A02`–`A13`, `R01`–`R14`).

---

## Task 1: Fortress Authoring Sprite/Anim/Collider Fixes, Importer Port, Screenshot Refresh & 24/24 Verification (`R2-A1`)

**Checkout:** `D:/thietkegame/.fortress-workspaces/antigravity` (branch `fortress/antigravity-clean`)

**Files:**
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressAnimationAndPrefabGenerator.cs` (`lines 203, 207, 1631-1638, 1649-1656`)
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressRuleTileAndPaletteGenerator.cs` (`line 426`)
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressDashboardAndPipelineRunner.cs` (`RunRound2Step3Batch` rebuilds `GothicFortress_ArtPreview.unity` before capture and also refreshes `RenderPreviewScreenshots()`)
- Modify: `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs` (`FortressImportOptions` overrides + `UpsertGroupPalette` + `EnsureGridPaletteSubAsset`)
- Modify: `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs` (`R13` + `R14`)
- Regenerate: `Tile_SpikeHazard_Collider.asset`, `Anim_Lever_StaticIdle.anim`, `Anim_Spike_Static.anim`, `PF_Lever.prefab`, `PF_SpikeTrap.prefab`, `GothicFortress_ArtPreview.unity`, `docs/Fortress/Reports/antigravity/{screenshots,round2/screenshots}/*.png`, `asset-catalog.json`, `round2/per-test/*.xml`, `round2/acceptance-matrix.md`, `authoring-handoff.md`

- [ ] **Step 1: Wire importer override seam (`activeManifest` / `activeRoot`) + update `R13` & add `R14` in `FortressAuthoringPipelineTests.cs` (Red)**

1. In `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`:
   - Add `manifestPathOverride` and `rootOverride` fields to `FortressImportOptions` (`lines 38-45`):
```csharp
        [Serializable]
        public class FortressImportOptions
        {
            public int defaultPixelsPerUnit = 32;
            public bool forceOverrideSheetPpu;
            public bool scanExtraManifests = true;
            public bool verifyAfterImport = true;
            public string optionalManifestRootOverride;
            public string manifestPathOverride;
            public string rootOverride;
        }
```
   - Wire `string activeManifest = string.IsNullOrWhiteSpace(options.manifestPathOverride) ? ManifestPath : options.manifestPathOverride;` and `string activeRoot = string.IsNullOrWhiteSpace(options.rootOverride) ? Root : options.rootOverride;` through `ImportAllFortressAssets` and `Verify(string manifestPathOverride = null, string rootOverride = null)` right now (keeping `if (File.Exists(path)) continue;` at line `342` intact for the Red step!) so `R14` executes strictly inside `__TempIncrementalPalette` without touching production assets and fails Red specifically on `Palette is incomplete: Terrain`.
2. In `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`, update `R13` to verify `PF_Lever` (`MachineryAndBell_011`), `PF_SpikeTrap` (`CrystalAndSpike_012`), `Anim_Lever_StaticIdle.anim` keyframe sprite (`MachineryAndBell_011`), `Anim_Spike_Static.anim` keyframe sprite (`CrystalAndSpike_012`), and `Tile_SpikeHazard_Collider.asset` sprite (`CrystalAndSpike_012`):
```csharp
            Assert.AreEqual("MachineryAndBell_011", leverSprite.name,
                "PF_Lever must use mechanical control mechanism sprite MachineryAndBell_011.");
            Assert.AreEqual("CrystalAndSpike_012", spikeSprite.name,
                "PF_SpikeTrap must use upward floor spike sprite CrystalAndSpike_012.");

            var leverClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FortressAnimationBuilder.AnimationsRoot + "/Clips/Anim_Lever_StaticIdle.anim");
            var spikeClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FortressAnimationBuilder.AnimationsRoot + "/Clips/Anim_Spike_Static.anim");
            var spikeTile = AssetDatabase.LoadAssetAtPath<Tile>(FortressRuleTileGenerator.ColliderTilesRoot + "/Tile_SpikeHazard_Collider.asset");
            Assert.IsNotNull(leverClip);
            Assert.IsNotNull(spikeClip);
            Assert.IsNotNull(spikeTile?.sprite);
            var spriteBinding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            Assert.AreEqual("MachineryAndBell_011", (AnimationUtility.GetObjectReferenceCurve(leverClip, spriteBinding)?[0].value as Sprite)?.name);
            Assert.AreEqual("CrystalAndSpike_012", (AnimationUtility.GetObjectReferenceCurve(spikeClip, spriteBinding)?[0].value as Sprite)?.name);
            Assert.AreEqual("CrystalAndSpike_012", spikeTile.sprite.name);
```
3. Add `R14_GothicDungeonImporter_IncrementalPaletteUpdate_PreservesCellsAndGuid` in `FortressAuthoringPipelineTests.cs`: creates a temporary folder `Assets/_Project/Features/FortressPresentation/Tests/__TempIncrementalPalette` with a `64×32` PNG and 2-sprite manifest (`Block_001` paintable, `Block_002` initially `referenceOnly: true`), calls `GothicDungeonImporter.ImportAllFortressAssets(new FortressImportOptions { manifestPathOverride = ..., rootOverride = ..., scanExtraManifests = false, verifyAfterImport = true })`, flips `Block_002` to `referenceOnly: false`, runs a second import, and asserts: (a) no exception thrown by `Verify`, (b) `GothicDungeon_Terrain.prefab` GUID unchanged, (c) `GridPalette` sub-asset non-null, (d) `GetUsedTilesCount() == 2`, (e) `Block_001` kept its exact cell coordinates. Clean up temp folder in `finally`.

- [ ] **Step 2: Run `R13` and `R14` to verify Red failure**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -runTests -testPlatform EditMode -testFilter "R13_VisualPrefabsAndArtPreview|R14_GothicDungeonImporter" -testResults D:/thietkegame/.fortress-workspaces/antigravity/Logs/r2-r13-r14-red.xml -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r2-r13-r14-red.log
```
Expected failure: `R13` fails on `"MachineryAndBell_006"` vs `"MachineryAndBell_011"` (and bell/stalactite anim clips + crystal shard collider tile); `R14` fails because `GothicDungeonImporter` skips existing palettes (`Palette is incomplete: Terrain`).

- [ ] **Step 3: Implement fixes in `FortressAnimationAndPrefabGenerator.cs`, `FortressRuleTileAndPaletteGenerator.cs`, `FortressDashboardAndPipelineRunner.cs`, and AG's `GothicDungeonImporter.cs`**

1. **`FortressAnimationAndPrefabGenerator.cs`:**
   - Line `203` (`Anim_Lever_StaticIdle`): change `"MachineryAndBell_001"` to `"MachineryAndBell_011"`.
   - Line `207` (`Anim_Spike_Static`): change `"CrystalAndSpike_001"` to `"CrystalAndSpike_012"`.
   - Lines `1631–1638` (`PF_Lever`): change `FindFirstSprite(sprites, "MachineryAndBell_006", "MachineryAndBell_012")` to `FindFirstSprite(sprites, "MachineryAndBell_011", "MachineryAndBell_012")` and update `missingNote` to note dedicated pull-lever art is pending (using `MachineryAndBell_011` proxy).
   - Lines `1649–1656` (`PF_SpikeTrap`): change `FindFirstSprite(sprites, "CrystalAndSpike_013", "CrystalAndSpike_014")` to `FindFirstSprite(sprites, "CrystalAndSpike_012", "CrystalAndSpike_014")`.
2. **`FortressRuleTileAndPaletteGenerator.cs` (`line 426`):**
   - Change `Tile_SpikeHazard_Collider` sprite lookup to `FortressEditorHelpers.FindFirstSprite(sprites, "CrystalAndSpike_012", "CrystalAndSpike_014")`.
3. **`FortressDashboardAndPipelineRunner.cs` (`RunRound2Step3Batch`, lines `1867-1885`):**
   - Place the asset generators at the **TOP** of `RunRound2Step3Batch()` (strictly **before** the two `FortressPaletteGenerator.GenerateAll()` passes and `FortressAuthoringCatalogExporter.ExportCatalog()` so `Fortress_Hazard_Palette` computes `CrystalAndSpike_012` bounds and `asset-catalog.json` exports updated `PF_Lever` / `PF_SpikeTrap` metadata):
     ```csharp
     FortressRuleTileGenerator.GenerateAll();
     FortressAnimationBuilder.BuildAll();
     FortressPrefabGenerator.GenerateAll();
     ```
   - Then, after `ExportCatalog()` and immediately **before** `RunRound2PreviewAndValidators()`, invoke:
     ```csharp
     FortressArtPreviewBuilder.BuildAndSavePreviewScene();
     FortressArtPreviewBuilder.RenderPreviewScreenshots(); // keeps Round 1 R06 screenshots/ fresh too
     ```
     so `GothicFortress_ArtPreview.unity` is freshly rebuilt with the updated `PF_Lever`, `PF_SpikeTrap`, `Anim_*`, and `Mat_FortressPixelParticle` before capturing all 6 `round2/screenshots/*.png` (and `screenshots/*.png`)!
4. **AG `GothicDungeonImporter.cs` (`line 342`):**
   - Replace `if (File.Exists(path)) continue;` with `UpsertGroupPalette(activeRoot, group.Key, group.Value)` + `EnsureGridPaletteSubAsset(savedPrefab, path)` (idempotent no-op when all group tiles are already placed; appends missing tiles below existing rows when new entries are added, preserving existing cell positions, `GridPalette` sub-asset, and `.prefab` GUID).

- [ ] **Step 4: Run `RunRound2Step3Batch` (GPU screenshots), run ALL `24/24` EditMode tests (`A02`–`A13`, `R01`–`R14`), visually inspect screenshots, clean `ProjectSettings` auto-churn, commit & tag**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -executeMethod DeadCells.Tools.Editor.FortressProduction.FortressAuthoringPipelineRunner.RunRound2Step3Batch -quit -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r2-a1-step3.log
```
Visually verify `round2/screenshots/preview_pixelperfect_lighting_vfx.png`, `preview_pixelperfect_props.png`, and `preview_overview.png` (confirm timestamps are `2026-11-19`, clean pixel particles without boulders/stalactites, upward floor spikes, mechanical lever proxy, grounded props).
Run all `24/24` EditMode tests (`A02`–`A13`, `R01`–`R14`) individually via `-testFilter` (`-batchmode -nographics -job-worker-count 1`, `$env:DOTNET_GCServer='0'`) into `docs/Fortress/Reports/antigravity/round2/per-test/*.xml`, run `SanitizeReportLogs()`, update `round2/acceptance-matrix.md` & `authoring-handoff.md`, verify `0` `.meta` changes, restore any unstaged `Unity 6000.6.9f1` `ProjectSettings/*` auto-deltas **after** the final Unity run, commit on `fortress/antigravity-clean`, and create local tag `fortress-authoring-r2-ready-v2`.

---

## Task 2: `PlayerHUD.prefab` Damage Trail & `Main_Level` Victory Outcome Flow (`R2-HUD-RunFlow`)

**Checkout:** `D:/thietkegame` (Root Repo)

**Files:**
- Modify: `Assets/_Project/Features/RunFlow/Scripts/RunSession.cs`
- Modify: `Assets/_Project/Features/RunFlow/Scripts/DefeatScreen.cs`
- Modify: `Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef` (add explicit `"UnityEngine.UI"`)
- Modify: `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs` (add `EnsurePlayerHudTrailFill()`)
- Modify: `Assets/_Project/Features/HUD/Prefabs/PlayerHUD.prefab` (via `EnsurePlayerHudTrailFill()`)
- Modify: `Assets/_Project/Core/Tests/EditMode/RunFlowAndHudTests.cs`

**Interfaces:**
```csharp
namespace DeadCells.RunFlow
{
    public sealed class DefeatScreen : MonoBehaviour
    {
        public const string DefeatHeadingText = "Lượt chơi kết thúc";
        public const string DefeatDescriptionText = "Quay lại điểm xuất phát để bắt đầu lượt mới.";
        public const string VictoryHeadingText = "Hoàn thành màn!";
        public const string VictoryDescriptionText = "Bạn đã vượt qua khu giam giữ. Chọn Chơi lại để bắt đầu lượt mới.";

        public void SetRetryAvailable(bool available);
        public void TickUnscaled(float unscaledDeltaTime);
    }
}
```

- [ ] **Step 1: Add failing EditMode tests to `Assets/_Project/Core/Tests/EditMode/RunFlowAndHudTests.cs` (Red — compiles cleanly using string literals / reflection)**

Append two discriminating tests to `RunFlowAndHudTests.cs`:

```csharp
        [Test]
        public void DefeatScreen_WhenRunCompleted_RevealsVictoryPanelAndBlocksSubsequentPlayerDamage()
        {
            var healthGo = new GameObject("PlayerHealth");
            healthGo.transform.SetParent(_root.transform);
            var health = healthGo.AddComponent<Health>();
            health.RestoreFull();

            var runGo = new GameObject("RunSession");
            runGo.transform.SetParent(_root.transform);
            var run = runGo.AddComponent<RunSession>();
            SetPrivateField(run, "_playerHealth", health);
            InvokePrivateMethod(run, "OnEnable");

            var panel = new GameObject("EndPanel");
            panel.transform.SetParent(_root.transform);
            panel.SetActive(false);

            var heading = new GameObject("Heading", typeof(RectTransform)).AddComponent<Text>();
            heading.transform.SetParent(panel.transform);
            var description = new GameObject("Description", typeof(RectTransform)).AddComponent<Text>();
            description.transform.SetParent(panel.transform);
            var retryButton = new GameObject("Retry", typeof(RectTransform)).AddComponent<Button>();
            retryButton.transform.SetParent(panel.transform);

            var screen = new GameObject("DefeatScreen").AddComponent<DefeatScreen>();
            screen.transform.SetParent(_root.transform);
            SetPrivateField(screen, "_run", run);
            SetPrivateField(screen, "_panel", panel);
            SetPrivateField(screen, "_retry", retryButton);
            TrySetPrivateField(screen, "_heading", heading);
            TrySetPrivateField(screen, "_description", description);
            SetPrivateField(screen, "_revealDelay", 0.4f);
            InvokePrivateMethod(screen, "OnEnable");

            Assert.IsTrue(run.CompleteRun());
            Assert.IsTrue(panel.activeSelf, "Victory must immediately reveal the end-of-run panel with Retry available.");
            Assert.AreEqual("Hoàn thành màn!", heading.text);
            Assert.AreEqual("Bạn đã vượt qua khu giam giữ. Chọn Chơi lại để bắt đầu lượt mới.", description.text);

            // Late damage after Victory must be blocked by RunSession's DamageFilter
            Assert.IsFalse(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, _root)),
                "Player must be damage-immune once RunSession completes.");
            Assert.IsTrue(health.IsAlive);

            InvokePrivateMethod(screen, "OnDisable");
            InvokePrivateMethod(run, "OnDisable");
        }

        [Test]
        public void PlayerHudPrefab_WiresHealthBarTrailFillImageBehindPrimaryFill()
        {
            var hudPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Features/HUD/Prefabs/PlayerHUD.prefab");
            Assert.IsNotNull(hudPrefab, "PlayerHUD.prefab must exist.");

            var view = hudPrefab.GetComponentInChildren<HealthBarView>(true);
            Assert.IsNotNull(view, "PlayerHUD.prefab must contain HealthBarView.");

            var fillField = typeof(HealthBarView).GetField("_fill", BindingFlags.Instance | BindingFlags.NonPublic);
            var trailField = typeof(HealthBarView).GetField("_trailFill", BindingFlags.Instance | BindingFlags.NonPublic);
            var fillRect = fillField.GetValue(view) as RectTransform;
            var trailRect = trailField.GetValue(view) as RectTransform;

            Assert.IsNotNull(fillRect, "HealthBarView._fill must be assigned.");
            Assert.IsNotNull(trailRect, "PlayerHUD.prefab must wire HealthBarView._trailFill to a live TrailFill RectTransform.");
            Assert.AreNotEqual(fillRect, trailRect, "_trailFill must be a distinct RectTransform from _fill.");
            Assert.AreEqual(fillRect.parent, trailRect.parent, "TrailFill must be a sibling of Fill under Track.");
            Assert.Less(trailRect.GetSiblingIndex(), fillRect.GetSiblingIndex(),
                "TrailFill must render BEHIND Fill (lower sibling index).");
            Assert.IsNotNull(trailRect.GetComponent<Image>(), "TrailFill must have a UI Image component.");
        }
```
(Add helper `TrySetPrivateField(object target, string fieldName, object value)` so the Red test compiles cleanly and fails on `Assert.IsTrue(panel.activeSelf)` and `Assert.IsNotNull(trailRect)`!)

- [ ] **Step 2: Run `RunFlowAndHudTests` to verify Red failure**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.RunFlowAndHudTests" -testResults D:/thietkegame/Logs/r2-runflow-red.xml -logFile D:/thietkegame/Logs/r2-runflow-red.log
```
Expected failure: `3` existing tests pass, `2` new tests fail on assertions (`panel.activeSelf` is `false` on Victory; `trailRect` is `null` in `PlayerHUD.prefab`).

- [ ] **Step 3: Implement Victory handling in `RunSession.cs` & `DefeatScreen.cs`, add `"UnityEngine.UI"` to `TilesetTools.Editor.asmdef`, and wire `TrailFill` into `PlayerHUD.prefab`**

1. **`Assets/_Project/Features/RunFlow/Scripts/RunSession.cs`:**
   - Add `private static readonly Func<DamageInfo, bool> BlockAllDamage = static _ => false;`
   - In `CompleteRun()`: after setting `IsCompleted = true;`, set `if (_playerHealth != null) _playerHealth.DamageFilter = BlockAllDamage;`.
   - In `OnDisable()`: if `_playerHealth != null`, unsubscribe `_playerHealth.Died -= OnPlayerDied;` and clear `if (ReferenceEquals(_playerHealth.DamageFilter, BlockAllDamage)) _playerHealth.DamageFilter = null;` (never clobbering `PlayerController`'s roll filter).
2. **`Assets/_Project/Features/RunFlow/Scripts/DefeatScreen.cs`:**
   - Add the 4 public heading/description constants + `[SerializeField] private Text _heading;` and `[SerializeField] private Text _description;`.
   - Subscribe `_run.Completed += ShowAfterVictory;` in `OnEnable()` (unsubscribe in `OnDisable()`); if `_run != null && _run.IsCompleted`, call `ShowAfterVictory()`.
   - In `ShowAfterDeath()`: keep existing unscaled `_revealDelay` timer (`TickUnscaled`); upon reveal, apply `DefeatHeadingText` & `DefeatDescriptionText` (when `_heading` / `_description` are non-null), activate `_panel`, and select `_retry`.
   - In `ShowAfterVictory()`: clear `_pendingReveal = false`, apply `VictoryHeadingText` & `VictoryDescriptionText`, activate `_panel`, and select `_retry` immediately so `RunSceneRestart` handles Replay identically for Victory and Defeat!
3. **`Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef`:** Add `"UnityEngine.UI"` explicitly to `"references"`.
4. **`Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`:** Add `EnsurePlayerHudTrailFill()` (`[MenuItem("Tools/DeadCells/HUD/Wire PlayerHUD TrailFill")]`):
   - Open `Assets/_Project/Features/HUD/Prefabs/PlayerHUD.prefab` with `PrefabUtility.LoadPrefabContents`.
   - Locate `HealthBarView`, read `_fill` (`RectTransform`), locate or create child `"TrailFill"` under `_fill.parent` (`Track`) at `SetSiblingIndex(fillRect.GetSiblingIndex())` (strictly behind `Fill`), configure full-stretch `RectTransform` (`anchorMin = Vector2.zero`, `anchorMax = Vector2.one`, `offsetMin = offsetMax = Vector2.zero`) + `UnityEngine.UI.Image` (`color = new Color(0.98f, 0.78f, 0.30f, 0.92f)`, `raycastTarget = false`), assign `_trailFill` via `SerializedObject(view)`, and save via `PrefabUtility.SaveAsPrefabAsset`.

- [ ] **Step 4: Run `EnsurePlayerHudTrailFill` and verify `RunFlowAndHudTests` (`5/5 Passed`)**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.EnsurePlayerHudTrailFill -quit -logFile D:/thietkegame/Logs/r2-hud-wire.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.RunFlowAndHudTests" -testResults D:/thietkegame/Logs/r2-runflow-green.xml -logFile D:/thietkegame/Logs/r2-runflow-green.log
```
Expected output: `total="5" passed="5" failed="0"`.

---

## Task 3: `Main_Level.unity` UI Overlap Fix, Victory Wiring, Archive Plank Alignment & CS0618 Cleanup (`R2-MainLevel`)

**Checkout:** `D:/thietkegame` (Root Repo — runs after Task 2)

**Files:**
- Modify: `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`
- Modify: `Assets/_Project/Scenes/Main_Level.unity`
- Modify: `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs`

- [ ] **Step 1: Extend `MainLevelIntegrityTests.cs` with failing assertions for Upgrade Panel layout, Defeat Screen Victory wiring/sizing, and Archive Gallery plank scale+alignment (Red)**

In `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs`, replace all deprecated `FindObjectsSortMode` / `FindFirstObjectByType` calls with `Object.FindObjectsByType<T>()` / `Object.FindAnyObjectByType<T>()` and add assertions verifying:
1. **Upgrade Panel non-overlap & containment:** Under `Power Scroll HUD/Choice backdrop/Upgrade panel`:
   - `Upgrade panel` vertical half-height (`upgradeRect.sizeDelta.y * 0.5f`) contains `Title` top (`titleRect.anchoredPosition.y + titleRect.sizeDelta.y * 0.5f <= upgradeRect.sizeDelta.y * 0.5f`).
   - `Instruction` bottom (`instRect.anchoredPosition.y - instRect.sizeDelta.y * 0.5f`) is strictly greater than `Brutality`, `Tactics`, and `Survival` top (`card.anchoredPosition.y + card.sizeDelta.y * 0.5f`).
   - `Instruction` top is strictly less than or equal to `Title` bottom.
2. **DefeatScreen Victory wiring & non-truncating Description:** `DefeatScreen._heading` and `DefeatScreen._description` are non-null, and `Description` `RectTransform.sizeDelta.y >= 50f` (preventing 2-line truncation of `VictoryDescriptionText`).
3. **Archive gallery proportional scale & exact collider alignment:** All `40` `Board` children have `localScale.y >= 0.70f` and `Mathf.Abs(localPosition.y) <= 0.05f` (so at `0.75f` scale, the opaque plank top at `+4/24 * 0.75 = +0.125u` matches `BoxCollider2D` top `+0.125u`!).

- [ ] **Step 2: Run `MainLevelIntegrityTests` to verify Red failure**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests" -testResults D:/thietkegame/Logs/r2-mainlevel-red.xml -logFile D:/thietkegame/Logs/r2-mainlevel-red.log
```
Expected failure: `Instruction` overlaps `Brutality`/`Tactics`/`Survival`, `DefeatScreen._heading` is `null`, `Description.sizeDelta.y == 35 < 50`, and `Archive gallery` boards have `localScale.y == 0.25f`.

- [ ] **Step 3: Update `MainLevelSceneRemediator.cs` and run remediation on `Main_Level.unity`**

In `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs` (replacing all `FindObjectsSortMode` / `FindFirstObjectByType` with Unity 6.6 `FindObjectsByType<T>()` / `FindAnyObjectByType<T>()`):
1. Call `EnsurePlayerHudTrailFill()`.
2. **Archive gallery boards:** For each of the 6 child platforms under `Archive gallery`, set every `Board` child's `transform.localScale = new Vector3(1f, 0.75f, 1f)` and `transform.localPosition = new Vector3(board.transform.localPosition.x, 0f, board.transform.localPosition.z)` (since all 7 `woodenPlatform_*.png` sprites are `24×48 px @ 24 PPU` FullRect with opaque top wood pixels at `+4/24 u` above center, `0f + (4f/24f) * 0.75f = +0.125u`, matching `BoxCollider2D` top `+0.125u` to `0.000u` precision!).
3. **Power Scroll HUD / Upgrade panel:** Locate `Power Scroll HUD/Choice backdrop/Upgrade panel`:
   - Set `Upgrade panel` `sizeDelta = new Vector2(1000f, 476f)` (bounds `[-238, +238]`).
   - Set `Title` `anchoredPosition = new Vector2(0f, 200f)` (`[+176, +224]`, `14px` top margin inside panel).
   - Set `Instruction` `anchoredPosition = new Vector2(0f, 158f)` (`[+142, +174]`, strictly above cards `[−183, +137]` and below `Title` `[+176, +224]`), and `activeSelf = true`.
4. **Defeat Screen Victory wiring & Description height:**
   - Locate `DefeatScreen`, find child `Backdrop/Frame/Card/Heading` (`Text`) and `Backdrop/Frame/Card/Description` (`Text`).
   - Resize `Description` `RectTransform` to `sizeDelta = new Vector2(400f, 54f)` and `anchoredPosition = new Vector2(0f, 4f)`.
   - Wire `_heading` and `_description` on `DefeatScreen` via `SerializedObject`.

- [ ] **Step 4: Run `MainLevelSceneRemediator.RemediateMainLevel` and verify all `DeadCells.Core.Tests` (`12/12 Passed`)**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.RemediateMainLevel -quit -logFile D:/thietkegame/Logs/r2-mainlevel-remediate.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests" -testResults D:/thietkegame/Logs/r2-core-green.xml -logFile D:/thietkegame/Logs/r2-core-green.log
```
Expected output: `total="12" passed="12" failed="0"`, `0` `CS0618` warnings in `MainLevelSceneRemediator.cs` and `MainLevelIntegrityTests.cs`.

---

## Task 4: `NhaGiamGiengChuong` Tilemap Visual Overhaul (Option 1A), Chest Fix, Grounded Props, HUD & World-Space Prompts (`R2-NG-Visual-HUD`)

**Checkout:** `D:/thietkegame` (Root Repo — runs sequentially after Task 2 & Task 3)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/HealingChest.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/LevelRunController.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/ResultPanel.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/NhaGiamGiengChuong.Tests.EditMode.asmdef`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapGeometryTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/ChestPlayTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/PilotPlayTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/EncounterPlayTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/RunPlayTests.cs`
- Regenerate: `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{AB,CD,ELF,G,HJK}.prefab`, `UI/ResultPanel.prefab`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity`

- [ ] **Step 1: Update `.asmdef` references (`HUD` + URP) and add failing EditMode tests `A08` & `A09` in `MapWiringTests.cs` (Red — compiles cleanly)**

1. Update `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef` to add `"HUD"`.
2. Update `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/NhaGiamGiengChuong.Tests.EditMode.asmdef` to add `"HUD"`, `"Unity.RenderPipelines.Universal.Runtime"`, and `"Unity.RenderPipelines.Universal.2D.Runtime"`.
3. Append `A08_VisualCoverageGroundedPropsAndCorrectChestSprite` and `A09_SceneHudAndChestWorldUiAreWiredAndVisible` to `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (reading `_fullHealthFeedback` via `new SerializedObject(chest).FindProperty("_fullHealthFeedback").objectReferenceValue as GameObject` so Step 2 compiles cleanly before Step 3 adds `FullHealthFeedback` getter!):

```csharp
        [Test]
        public void A08_VisualCoverageGroundedPropsAndCorrectChestSprite()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. Swapped chest/barrel fix: I3_HealingChest uses golden chest StorageAndVessel_003; Art_G_Barrel uses StorageAndVessel_014
            var chest = Object.FindAnyObjectByType<HealingChest>();
            Assert.IsNotNull(chest);
            var chestSr = chest.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(chestSr?.sprite, "[A08/ChestSprite] Missing I3_HealingChest/Visual sprite.");
            Assert.AreEqual("StorageAndVessel_003", chestSr.sprite.name,
                "[A08/ChestSprite] I3_HealingChest must display golden chest StorageAndVessel_003, not barrel StorageAndVessel_014.");
            Assert.LessOrEqual(Mathf.Abs(chestSr.bounds.min.y - 0f), 0.08f,
                "[A08/ChestGrounded] Chest visual base must sit flush on floor y=0.");

            var barrelGo = GameObject.Find("Art_G_Barrel");
            Assert.IsNotNull(barrelGo, "[A08/Barrel] Missing Art_G_Barrel.");
            var barrelSr = barrelGo.GetComponent<SpriteRenderer>();
            Assert.AreEqual("StorageAndVessel_014", barrelSr.sprite.name,
                "[A08/BarrelSprite] Art_G_Barrel must display barrel StorageAndVessel_014, not chest StorageAndVessel_003.");

            // 2. Every room prefab must contain visual-only Tilemaps (0 TilemapCollider2D): 5 rooms * 2 (TM_Background + TM_SolidVisual) >= 10
            var tilemaps = Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>();
            Assert.GreaterOrEqual(tilemaps.Length, 10, "[A08/Tilemaps] All 5 rooms must render TM_Background + TM_SolidVisual tilemaps.");
            foreach (var tm in tilemaps)
            {
                Assert.IsNull(tm.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>(),
                    $"[A08/TilemapNoCol] Visual Tilemap '{tm.name}' must not carry TilemapCollider2D.");
            }

            // 3. Previously invisible solid floors, outer walls, ceilings, stair core, AND interior doorway headers must have painted Tilemap cells
            var solidTilemaps = tilemaps.Where(t => t.name == "TM_SolidVisual").ToArray();
            Assert.AreEqual(5, solidTilemaps.Length, "[A08/SolidTilemaps] Expected 1 TM_SolidVisual per room prefab.");

            Vector2[] requiredSolidSamples =
            {
                new Vector2(60.5f, 7.5f),   // Floor_G_UpperWest [56,65]x[7,8]
                new Vector2(74.0f, 7.5f),   // Floor_G_UpperEast [72,76]x[7,8]
                new Vector2(20.0f, 7.5f),   // Stairs_H landing [18,22]x[7,8]
                new Vector2(10.5f, 11.5f),  // Stairs_H solid stone core
                new Vector2(-0.25f, 2.5f),  // Wall_A_West outer wall
                new Vector2(76.25f, 5.0f),  // Wall_G_East outer wall
                new Vector2(10.0f, 5.75f),  // Ceiling_A
                new Vector2(11.0f, 31.5f),  // Floor_F_Ceiling_E
                new Vector2(18.10f, 5.0f),  // Interior doorway header Wall_AB_Header_18 (half-offset wall)
                new Vector2(-15.90f, 22.0f),// Interior doorway header Wall_JK_Mid_Neg16 (negative X half-offset wall)
            };
            foreach (var pt in requiredSolidSamples)
            {
                bool covered = solidTilemaps.Any(tm => tm.GetTile(tm.WorldToCell(pt)) != null);
                Assert.IsTrue(covered, $"[A08/SolidCoverage] World point ({pt.x:F2}, {pt.y:F2}) has no painted TM_SolidVisual tile.");
            }

            // 4. Key architectural props must be grounded on their room floor (delta <= 0.12u), and Art_F_ExitGlow must keep its Light2D
            (string name, float expectedFloorY)[] groundedProps =
            {
                ("Art_B_Arch1", 0f),
                ("Art_B_Pillar", 0f),
                ("Art_B_Arch2", 0f),
                ("Art_G_Winch", 8f),
                ("Art_J_Arch", 16f),
                ("Art_E_Pillar1", 24f),
                ("Art_E_Pillar2", 24f),
                ("Art_F_ExitArch", 32f),
                ("Art_F_ExitGlow", 32f),
            };
            foreach (var (propName, floorY) in groundedProps)
            {
                var go = GameObject.Find(propName);
                Assert.IsNotNull(go, $"[A08/Grounded/{propName}] Missing prop.");
                var sr = go.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite);
                Assert.AreEqual(floorY, sr.bounds.min.y, 0.12f,
                    $"[A08/Grounded/{propName}] Visual bottom ({sr.bounds.min.y:F2}) must rest on floor y={floorY:F2}, not float.");
            }
            var exitGlow = GameObject.Find("Art_F_ExitGlow");
            Assert.IsNotNull(exitGlow.GetComponent<UnityEngine.Rendering.Universal.Light2D>(),
                "[A08/ExitGlowLight] Art_F_ExitGlow must retain its warm Light2D beacon.");
        }

        [Test]
        public void A09_SceneHudAndChestWorldUiAreWiredAndVisible()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. PlayerHUD instantiated and wired to Player Health + Equipment + TrailFill
            var player = Object.FindAnyObjectByType<PlayerController>();
            var playerHealth = player.GetComponent<Health>();
            var playerBow = player.GetComponentInChildren<PlayerBow>(true);

            var healthHud = Object.FindAnyObjectByType<DeadCells.HUD.PlayerHealthHud>();
            var equipHud = Object.FindAnyObjectByType<DeadCells.HUD.PlayerEquipmentHud>();
            var barView = Object.FindAnyObjectByType<DeadCells.HUD.HealthBarView>();
            Assert.IsNotNull(healthHud, "[A09/HUD] NhaGiamGiengChuong.unity must contain PlayerHealthHud.");
            Assert.IsNotNull(equipHud, "[A09/HUD] NhaGiamGiengChuong.unity must contain PlayerEquipmentHud.");
            Assert.IsNotNull(barView, "[A09/HUD] NhaGiamGiengChuong.unity must contain HealthBarView.");
            Assert.AreEqual(playerHealth, new SerializedObject(healthHud).FindProperty("_health").objectReferenceValue, "[A09/HUD.Health]");
            Assert.AreEqual(player, new SerializedObject(equipHud).FindProperty("_player").objectReferenceValue, "[A09/HUD.Player]");
            Assert.AreEqual(playerBow, new SerializedObject(equipHud).FindProperty("_bow").objectReferenceValue, "[A09/HUD.Bow]");
            Assert.IsNotNull(new SerializedObject(barView).FindProperty("_trailFill").objectReferenceValue, "[A09/HUD.TrailFill]");

            // 2. I3_HealingChest PromptIndicator and FullHealthFeedback must contain real visible UI Text with valid font
            var chest = Object.FindAnyObjectByType<HealingChest>();
            Assert.IsNotNull(chest);
            var fullFeedbackGo = new SerializedObject(chest).FindProperty("_fullHealthFeedback").objectReferenceValue as GameObject;
            Assert.IsNotNull(fullFeedbackGo);
            var promptText = chest.PromptIndicator.GetComponentInChildren<UnityEngine.UI.Text>(true);
            var fullText = fullFeedbackGo.GetComponentInChildren<UnityEngine.UI.Text>(true);
            Assert.IsNotNull(promptText, "[A09/ChestPrompt] PromptIndicator must render real UI Text.");
            Assert.IsNotNull(promptText.font, "[A09/ChestPromptFont] World-Space UI Text must assign LegacyRuntime.ttf.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(promptText.text));
            Assert.IsNotNull(fullText, "[A09/ChestFull] FullHealthFeedback must render real UI Text.");
            Assert.IsNotNull(fullText.font, "[A09/ChestFullFont] World-Space UI Text must assign LegacyRuntime.ttf.");
            Assert.AreEqual(HealingChest.FullHealthMessage, fullText.text);

            // 3. Global Light 2D intensity calibrated for Dead Cells contrast (<= 0.50f)
            var globalLight = Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>()
                .FirstOrDefault(l => l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global);
            Assert.IsNotNull(globalLight);
            Assert.LessOrEqual(globalLight.intensity, 0.50f,
                "[A09/Lighting] Global Light 2D intensity must be <= 0.50f so warm beacons and silhouettes pop.");
        }
```

- [ ] **Step 2: Run `A08` and `A09` to confirm Red failure**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "A08_VisualCoverage|A09_SceneHud" -testResults D:/thietkegame/Logs/r2-ng-red.xml -logFile D:/thietkegame/Logs/r2-ng-red.log
```
Expected failure: `A08` fails on `StorageAndVessel_014` chest sprite, `0` Tilemaps, missing solid visuals, and floating props; `A09` fails on missing `PlayerHealthHud` and empty `PromptIndicator`.

- [ ] **Step 3: Implement Runtime Fixes (`HealingChest.cs`, `LevelRunController.cs`, `ResultPanel.cs`), Builder Overhaul (`NhaGiamGiengChuongBuilder.cs`), and CS0618 Cleanup**

1. **`HealingChest.cs`:**
   - Add `public GameObject FullHealthFeedback => _fullHealthFeedback;`
   - Inside `RefreshVisualState()`:
     ```csharp
     bool inTrigger = IsColliderInTrigger(col);
     _promptVisible = !_consumed
         && _run != null && _run.IsPlaying
         && health != null && health.IsAlive
         && health.Current < health.Max
         && inTrigger;
     if (_fullHealthFeedback != null &&
         (!inTrigger || _consumed || _run == null || !_run.IsPlaying || health == null || !health.IsAlive || health.Current < health.Max))
     {
         _fullHealthFeedback.SetActive(false);
     }
     ```
     (Auto-hides `"Đầy máu"` when player exits trigger, takes damage, dies, or consumes reward — preventing badge overlap while preserving `U04`–`U07` and `P03`.)
2. **`LevelRunController.cs`:**
   - Track `_endTime = -1f;` set inside `ResolvePendingOutcome` and expose `public float ElapsedRunTime => _clockStarted ? Mathf.Max(0f, (_endTime >= 0f ? _endTime : Time.time) - _runStartTime) : 0f;`.
   - Inside `TryReplay()`: call `if (_hitStop != null) _hitStop.SetMenuPaused(false);` **BEFORE** `Time.timeScale = 1f;` (so an active HitStop cannot re-freeze `Time.timeScale` back to `0f`).
   - Replace `FindObjectsByType<T>(FindObjectsSortMode.None)` with `FindObjectsByType<T>()`.
3. **`ResultPanel.cs`:**
   - Add optional `[SerializeField] private UnityEngine.UI.Text _subtitleText;` (displays formatted run elapsed time `mm:ss` + `" — Nhấn [R] hoặc Enter để chơi lại"` when wired) and tint `_titleText.color` warm gold `(1f, 0.86f, 0.35f)` on `LevelOutcome.Win` vs crimson `(0.95f, 0.28f, 0.28f)` on `LevelOutcome.Loss` while keeping `TitleText.text` strictly `"Thắng"` / `"Thua"`.
4. **`NhaGiamGiengChuongBuilder.cs` (Option 1A Tilemap Overhaul):**
   - Add `CreateRoomVisualGrid(GameObject artRoot, Material spriteLit, out Tilemap bgTm, out Tilemap solidTm)` creating `VisualGrid` (`Grid` `cellSize = new Vector3(0.5f, 0.5f, 0f)`) with 2 visual-only `Tilemap` children (`0` `TilemapCollider2D`!):
     - `TM_Background` (`sortingOrder = -12`, `Tilemap.color = new Color(0.22f, 0.24f, 0.31f, 1f)`).
     - `TM_SolidVisual` (`sortingOrder = -3`, `Tilemap.color = new Color(0.84f, 0.86f, 0.92f, 1f)`).
   - Add exact per-tile helpers (never mutating shared `Tile` assets; `MasonryRow3_001/003` and `MasonryRow1_008` have `m_Flags: 1` `LockColor`, so `SetTransformMatrix` works cleanly):
     - `PaintRect(Tilemap tm, Tile primaryTile, Tile altTile, float xMin, float xMax, float yMin, float yMax)`: iterates integer `0.5u` cells `cx` from `Mathf.RoundToInt(xMin / 0.5f)` to `Mathf.RoundToInt(xMax / 0.5f) - 1` and `cy` from `Mathf.RoundToInt(yMin / 0.5f)` to `Mathf.CeilToInt(yMax / 0.5f) - 1`, applying exact scale `Matrix4x4.Scale(new Vector3(0.5f / tile.sprite.bounds.size.x, 0.5f / tile.sprite.bounds.size.y, 1f))` so zero hairline seams or overhang occur!
     - `PaintHalfOffsetVerticalWall(Tilemap tm, Tile tile, float centerX, float yMin, float yMax)`: paints column `cx = Mathf.FloorToInt(centerX / 0.5f)` (covers both positive seams `18, 22, 42, 56, 6` and negative seam `-16`) for `cy` from `Mathf.RoundToInt(yMin / 0.5f)` to `Mathf.CeilToInt(yMax / 0.5f) - 1` with `Matrix4x4.TRS(new Vector3(-0.25f, 0f, 0f), Quaternion.identity, new Vector3(0.5f / tile.sprite.bounds.size.x, 0.5f / tile.sprite.bounds.size.y, 1f))`!
   - Paint `TM_Background` across all 11 room interiors and `TM_SolidVisual` across EVERY solid floor (including `Floor_G_UpperWest`, `Floor_G_UpperEast`, `Stairs_H` landing `[18,22]×[7,8]` and all 4 stair masonry blocks `[15,18]×[7,10]`, `[12,15]×[7,12]`, `[9,12]×[7,14]`, `[6,9]×[7,16]`), EVERY ceiling, EVERY outer wall, and all `12` interior doorway headers! Remove overlapping `AddFloorTrimRow` strips that bled `0.59u–1.16u` below `1.0u` ceiling slabs.
   - **Swap Room `G` Chest ↔ Barrel:** `Art_G_Barrel` (`x = 60`) = `"Props/StorageAndVessel_014"` grounded on `y = 0`; `I3_HealingChest/Visual` (`x = 72`) = `"Props/StorageAndVessel_003"` grounded on `y = 0` (`localPosition.y = sprite.bounds.extents.y`).
   - **Ground ALL Floating Props & Anchor Ceiling Props:** Use `AddGroundedProp(parent, name, tilePath, mat, x, floorY, sortingOrder, color)` (`y = floorY + sprite.bounds.extents.y`) on `Art_B_Arch1/2`, `Art_B_Pillar`, `Art_G_Barrel`, `Art_G_Crate`, `Art_G_Winch` (`floorY = 8f`), `Art_J_Arch` (`floorY = 16f`), `Art_E_Pillar1/2` (`floorY = 24f`), `Art_F_ExitArch` (`floorY = 32f`). Keep `Art_F_ExitGlow` built with `AddGuideBeacon` (so it retains its `Light2D`!) grounded at `y = 32f + exitGlowSprite.bounds.extents.y` (`intensity = 1.25f`). Use `AddCeilingProp` (`y = ceilingBottomY - sprite.bounds.extents.y`) for `Art_A_Cage` (`ceilingBottomY = 5.5f`) and `Art_C_Bell` (`ceilingBottomY = 18.5f`).
   - **Align `AddPlankRow`:** For `WoodPlatform_001` (railing bridge, walkable deck at pixel row `32`), place `y = topY - 0.02f` so the plank sits flush on `topY` and the railing stands cleanly behind the player; for `WoodPlatform_007`/`008` on `2.0u` decks, clamp `scaleY = Mathf.Min(1f, 2.0f / sprite.bounds.size.y)` and `y = topY - sprite.bounds.max.y * scaleY`.
   - **Instantiate & Wire `PlayerHUD.prefab`:** In `BuildScene`, instantiate `Assets/_Project/Features/HUD/Prefabs/PlayerHUD.prefab`, wire `PlayerHealthHud._health = playerHealth`, `PlayerEquipmentHud._player = playerCtrl`, `PlayerEquipmentHud._bow = playerGo.GetComponentInChildren<PlayerBow>(true)`.
   - **Build Visible World-Space UI for `PromptIndicator` & `FullHealthFeedback`:** Create helper `BuildWorldPromptBadge(GameObject targetGo, string label, Color textColor)` adding `Canvas` (`RenderMode.WorldSpace`, `sortingOrder = 25`, `localScale = Vector3.one * 0.015f`, `RectTransform.sizeDelta = new Vector2(260f, 48f)`), dark backdrop `Image` (`new Color(0.08f, 0.08f, 0.12f, 0.86f)`, `raycastTarget = false`), and `UnityEngine.UI.Text` (`font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`, `fontSize = 22`, `alignment = TextAnchor.MiddleCenter`, `color = textColor`, `text = label`, `raycastTarget = false`). Apply `"[E] Hồi đầy máu"` (gold) to `PromptIndicator` and `HealingChest.FullHealthMessage` (pale cyan) to `FullHealthFeedback`.
   - **Dead Cells Lighting & ResultPanel Subtitle:** Set `Global Light 2D` `intensity = 0.38f`, `color = new Color(0.76f, 0.80f, 0.92f, 1f)`, guide beacons `intensity = 1.25f`, and add `SubtitleText` wired to `ResultPanel._subtitleText` inside `ResultPanel.prefab`.
   - **Clear `CS0618` Warnings:** Replace deprecated `FindFirstObjectByType` / `FindObjectsSortMode` with `FindAnyObjectByType<T>()` / `FindObjectsByType<T>()` in `NhaGiamGiengChuongBuilder.cs`, `MapWiringTests.cs`, `MapGeometryTests.cs`, `ChestPlayTests.cs`, `PilotPlayTests.cs`, `EncounterPlayTests.cs`, and `RunPlayTests.cs`.

- [ ] **Step 4: Rebuild `NhaGiamGiengChuong`, run ALL 44 Root tests (`12` Core + `25` NG EditMode + `7` NG PlayMode), then restore unintended `ProjectSettings` churn**

```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -quit -logFile D:/thietkegame/Logs/r2-ng-build.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests" -testResults D:/thietkegame/Logs/r2-ng-editmode.xml -logFile D:/thietkegame/Logs/r2-ng-editmode.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform PlayMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.Play" -testResults D:/thietkegame/Logs/r2-ng-playmode.xml -logFile D:/thietkegame/Logs/r2-ng-playmode.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests" -testResults D:/thietkegame/Logs/r2-all-core-green.xml -logFile D:/thietkegame/Logs/r2-all-core-green.log
# After the VERY LAST Unity invocation on D:/thietkegame, restore unintended Unity auto-churn (keep EditorBuildSettings.asset intact):
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
```
Expected output:
- `r2-ng-editmode.xml`: `total="25" passed="25" failed="0"` (`U01`–`U16` + `A01`–`A09`)
- `r2-ng-playmode.xml`: `total="7" passed="7" failed="0"` (`P01`–`P07`)
- `r2-all-core-green.xml`: `total="12" passed="12" failed="0"`

---

## Wave Dispatch Matrix (Parallel vs. Sequential)

| Session / Prompt | Execution Order | Checkout | Assigned Task | Owned Files |
|---|---|---|---|---|
| **Session 1 (`R2-A1-Authoring`)** | **Wave 1 (Parallel)** | `D:/thietkegame/.fortress-workspaces/antigravity` | **Task 1** | `FortressAnimationAndPrefabGenerator.cs`, `FortressRuleTileAndPaletteGenerator.cs`, `FortressDashboardAndPipelineRunner.cs`, `GothicDungeonImporter.cs` (AG), `FortressAuthoringPipelineTests.cs`, regenerated prefabs/clips/tiles/preview/PNGs/reports |
| **Session 2 (`R2-MainLevel-HUD-RunFlow`)** | **Wave 1 (Parallel w/ Session 1)** | `D:/thietkegame` | **Task 2 & Task 3** | `RunSession.cs`, `DefeatScreen.cs`, `PlayerHUD.prefab`, `TilesetTools.Editor.asmdef`, `MainLevelSceneRemediator.cs`, `Main_Level.unity`, `RunFlowAndHudTests.cs`, `MainLevelIntegrityTests.cs` |
| **Session 3 (`R2-NG-Visual-HUD`)** | **Wave 2 (Sequential after Session 2)** | `D:/thietkegame` | **Task 4** | `Assets/_Project/Features/NhaGiamGiengChuong/**`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` (depends on Session 2's wired `PlayerHUD.prefab` `_trailFill`, avoids `D:/thietkegame` Unity lock, and runs final `ProjectSettings` cleanup) |
