# Gothic Dungeon & Nhà Giam Giếng Chuông — Map, Tileset, Visual, UI/UX & Gameplay Remediation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate all verified technical, tileset/autotile, visual/VFX, UI/UX, and level-design defects across the root workspace (`D:/thietkegame`), the Fortress Authoring workspace (`D:/thietkegame/.fortress-workspaces/antigravity`), and the `Nhà Giam Giếng Chuông` v7 runtime/level workstream, backed by discriminating EditMode and PlayMode NUnit tests.

**Architecture:** Remediation is partitioned into six file-disjoint tasks aligned with the repository's multi-worktree topology and `.asmdef` boundaries:
1. **Task 1 (`G3` — Editor Tileset Importer):** Atomic pre-validation and incremental palette append in `GothicDungeonImporter` (`TilesetTools.Editor.asmdef`) so adding paintable entries to existing manifest groups preserves existing palette cells and `.prefab` GUIDs while passing `Verify()`.
2. **Task 2 (`A1-RuleTile` — Fortress Authoring RuleTile Alignment):** Surface-aware `RuleTile` vertical alignment in `FortressRoomDescriptor` (`FortressPresentation.asmdef`) and `FortressCollisionValidator` / `FortressRuleTileGenerator` (`FortressProduction.Editor.asmdef`) so `RT_Stone_*` (`128 PPU`, `2.00×2.19u`) and `RT_Wood` (`32 PPU`, `3.44×2.03u`) align flush (`<= 0.05u`) with `1×1u` `TilemapCollider2D` surfaces without altering `.asset` script GUIDs or creating new `.meta` files.
3. **Task 3 (`A1-VFX-Prefab` — Fortress Authoring Ambient VFX & Visual Prefab Fixes):** Transparent URP particle material setup with an embedded point-filtered `4×4` pixel particle sub-asset texture (`Tex_FortressPixelParticle`), `sizeOverLifetime` decay, corrected sprite bindings for `PF_Lever` (`MachineryAndBell_006`) and `PF_SpikeTrap` (`CrystalAndSpike_013`), and grounded non-overlapping prop coordinates in `FortressArtPreviewBuilder`.
4. **Task 4 (`G1-Code` — Root Runtime RunFlow & HUD Fixes):** Unscaled-time reveal (`WaitForSecondsRealtime`) in `DefeatScreen`, victory completion state (`CompleteRun()`) in `RunSession`, and unscaled damage-trail interpolation (`_trailFill`) in `HealthBarView`.
5. **Task 5 (`G1-Scene` — `Main_Level.unity` Visual, Physics & Encounter Remediation):** Editor remediation tool and scene verification test fixing `Scroll vat` materials (`Sprite-Lit-Default.mat`), `Climbable chain` vertical span (`y = -1.0 .. 2.0`), `Archive gallery` 3-slice plank assignment (`left` / `middle` / `right`), `Power Scroll HUD/Instruction` activation, East Wing enemy pacing (including `Enemy_Rat`), and `Exit Door` victory trigger wiring.
6. **Task 6 (`NG-Arrows` — `Nhà Giam Giếng Chuông` v7 Room E One-Way Platform Arrow Contract):** Separate straight-projectile obstacle mask (`_straightObstacleMask`) in `Projectile` / `ProjectileSpawner` (`Enemies.asmdef`) so horizontal Archer arrows pass across Room `E`'s center `OneWayPlatform` (`layer 9`) while grenades (`Mode.Lob`) continue landing and bouncing on both `Ground` (`layer 6`) and `OneWayPlatform` (`layer 9`).

**Tech Stack:** Unity `6000.6.9f1`, C# 9, Universal Render Pipeline (URP 2D `17.6.0`), `com.unity.2d.tilemap.extras` (`9.0.1`), Unity Input System (`1.20.1`), Unity Test Framework (`1.8.0`, NUnit 3.5), `rtk` CLI wrapper.

**Spec:**
- `d:\thietkegame\AGENTS.md`
- `d:\thietkegame\docs\MapDesign.md`
- `d:\thietkegame\docs\NhaGiamGiengChuong.md`
- `d:\thietkegame\docs\Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md`
- `d:\thietkegame\docs\Tests-Nha-Giam-Gieng-Chuong.md`
- `d:\thietkegame\docs\Fortress\Prompts-Sau-Merge-2026-11-19.md`

## Global Constraints

- **Shell & Encoding:** Run all shell commands via `rtk` (or `rtk proxy <command>`). Read and write all files in UTF-8 without BOM.
- **No `.meta` Mutations (`AGENTS.md` §2):** Never create, edit, or delete `.meta` files manually. All new test cases in `.fortress-workspaces/antigravity` MUST be added to the existing `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`. In the root repo (`D:/thietkegame`), if Unity Editor is running when adding a new `.cs` file under `Assets/_Project/Core/Tests/EditMode/`, let Unity generate/manage `.meta` only when explicitly permitted, or run via the existing tracked test assembly when executing in batchmode.
- **Preserve Dirty User Files (`AGENTS.md` §2):** Do not revert or overwrite pre-existing uncommitted changes in `D:/thietkegame` (`ProjectSettings/EditorBuildSettings.asset`, `docs/Fortress/Prompts-Sau-Merge-2026-11-19.md`).
- **Assembly Boundaries (`AGENTS.md` §0):**
  - `Core`: references `Unity.InputSystem` only.
  - `VFX`: no internal references.
  - `CameraSystem`: no internal references.
  - `Combat`: `Core`, `VFX`.
  - `Enemies`: `Core`, `Combat`, `VFX`.
  - `Player`: `Core`, `VFX`, `Combat`, `CameraSystem`, `Unity.InputSystem`.
  - `RunFlow`: `Combat`, `UnityEngine.UI`.
  - `HUD`: `Combat`, `Currency`, `Player`, `VFX`, `UnityEngine.UI`, `Unity.RenderPipelines.Universal.2D.Runtime`.
- **Single-Unity-Process Rule:** Never run two `Unity.exe` processes against the same checkout simultaneously. On the `7498 MB` RAM host, run EditMode tests individually with `--filter` or `-nographics -job-worker-count 1` and `$env:DOTNET_GCServer='0'`.

## Review Focus

- **Root-Cause Traceability:** Every task must directly fix the exact line numbers and serialized YAML fields identified in the Phase 1 Root-Cause Matrix below.
- **Test Discrimination (`test-value` & `writing-good-tests.md`):** Every unit test must fail against the unpatched code for the exact production bug it targets, use hand-derived literal expectations (never copying production formulas into assertions), exercise real Unity components (`Tilemap`, `CompositeCollider2D`, `ParticleSystem`, `Coroutine`, `Rigidbody2D`, `Physics2D.Simulate`), and clean up all temporary assets/scenes in `finally`.
- **Idempotence & GUID Stability:** Running `GothicDungeonImporter.Import()`, `FortressRuleTileGenerator.GenerateAll()`, `FortressPaletteGenerator.GenerateAll()`, or `FortressPrefabGenerator.GenerateAll()` twice in a row must produce zero asset GUID drift and zero duplicate tiles.

---

## Phase 1 Root-Cause Diagnosis Matrix (`systematic-debugging`)

| Bug ID | Layer / Checkout | Location (File & Lines / YAML IDs) | Verified Root Cause | Production Impact |
|---|---|---|---|---|
| **RC-1** | Layer A (`Root`) & Layer B (`Authoring`) | `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs:98-122, 183-188` (Root) & `:315-344` (Authoring) | `Import()` creates new `.asset` tiles when `import-manifest.json` adds a paintable sprite (`referenceOnly == false`) to an existing group, then executes `if (File.Exists(path)) continue;` at line `120` (`342` in Authoring), skipping the existing palette `.prefab`. `Verify()` (`line 186`) then compares `Tilemap.GetUsedTilesCount() == group.Value` and throws `InvalidOperationException("Palette is incomplete: " + group.Key)`. | Adding any new paintable sprite to an existing manifest group (`Terrain`, `Architecture`, `Props`, `Nature`) hard-crashes `GothicDungeonImporter.Import()`. |
| **RC-2** | Layer B (`Authoring`) | `.fortress-workspaces/antigravity/Assets/_Project/Tools/Editor/FortressProduction/FortressRuleTileAndPaletteGenerator.cs:476-595` & `FortressRoomAndCollisionTools.cs:385-386, 490-543` | `MasonryRow1_001` is `256×280 px @ 128 PPU = 2.00×2.1875u` (`bounds.max.y = +1.09375u`) and `WoodPlatform_001` is `110×65 px @ 32 PPU = 3.4375×2.03125u` (`bounds.max.y = +1.015625u`). Single `ColliderTile`s apply `Matrix4x4.Translate(0, yOffset, 0)` (`lines 458-470`), but `RT_Stone_*` and `RT_Wood` are stock `UnityEngine.RuleTile` assets whose `GetTileData` resets cell transforms to `Matrix4x4.identity` on `tm.RefreshAllTiles()`. In addition, `ValidateTilemapVisualAlignment` checks sub-surface interior rows of multi-row blocks, and `BuildStoneRuleTile` (`line 538`) fails to pass `mirrorX: true` on the right-edge rule. | Painting `RT_Stone_*` on `TM_Floors`/`TM_Ceilings` or `RT_Wood` on `TM_Platforms` leaves a `0.515u–0.594u` (`16–19 px`) gap between visual surface and `TilemapCollider2D`, and fails `FortressCollisionValidator`. |
| **RC-3** | Layer B (`Authoring`) | `.fortress-workspaces/antigravity/Assets/_Project/Tools/Editor/FortressProduction/FortressAnimationAndPrefabGenerator.cs:1197-1315` | `FortressAmbientGenerator` binds `RockDebris_001` (`137×107 px` boulder pile), `CrystalAndSpike_006` (`31×50 px` hanging stalactite), and `RockDebris_010` (`48×41 px` 3D rocks) to `ps.textureSheetAnimation`, leaves `sizeOverLifetime` disabled, and in `EnsurePixelParticleMaterial` (`lines 1248-1270`) sets `_Surface = 1f` on `Universal Render Pipeline/Particles/Unlit` without enabling `_SURFACE_TYPE_TRANSPARENT`, alpha blend factors (`SrcAlpha` / `OneMinusSrcAlpha`), or `_ZWrite = 0`. | All 9 `PF_Ambient_*` prefabs emit miniature 3D boulders and ceiling stalactites instead of pixel particles, and material transparency state is incomplete. |
| **RC-4** | Layer B (`Authoring`) | `FortressAnimationAndPrefabGenerator.cs:1598-1623` & `FortressDashboardAndPipelineRunner.cs:1040-1064` | `PF_Lever` binds `MachineryAndBell_001` (hanging church bell) as primary candidate instead of lever mechanism `MachineryAndBell_006`; `PF_SpikeTrap` binds `CrystalAndSpike_001` (downward ceiling stalactite) instead of upward floor spikes `CrystalAndSpike_013`. In `FortressArtPreviewBuilder.BuildAndSavePreviewScene()`, `PF_MagicalPortal_Cyan` at `(0, 3.0)` overlaps `BG_GothicWindow_Center` at `(0, 6.0)`, and `BG_Arch_Left` at `(-7, 4.0)` / `FG_OverlapPillar` at `(-11, 3.2)` float above the `y = 1.0` floor surface. | `PF_Lever` renders as a church bell, `PF_SpikeTrap` points downward from floor, and `GothicFortress_ArtPreview.unity` screenshots show floating pillars/arches and portal-window collision. |
| **RC-5** | Layer A (`Root`) | `Assets/_Project/Features/RunFlow/Scripts/DefeatScreen.cs:47-53`, `RunSession.cs:8-31`, `Assets/_Project/Features/HUD/Scripts/HealthBarView.cs:7-20` | `DefeatScreen.Reveal()` (`line 49`) yields `new WaitForSeconds(_revealDelay)` (scaled time), stalling forever if `Time.timeScale == 0f` (e.g., during `HitStop` on fatal hit). `RunSession` has `IsDefeated` / `Defeated` only and no win/completion contract (`IsCompleted` / `Completed` / `CompleteRun()`). `HealthBarView.Render()` snaps `_fill.anchorMax.x` in 1 frame with no secondary damage-trail bar (`_trailFill`). | Fatal hits during `HitStop` (`Time.timeScale = 0`) can freeze before showing Defeat UI; reaching the level exit cannot complete the run; HUD lacks Dead Cells damage-trail feedback. |
| **RC-6** | Layer A (`Root`) | `Assets/_Project/Scenes/Main_Level.unity` (`fileID: 378536169`, `996582151`, `Climbable chain`, `Archive gallery`, `Power Scroll HUD/Instruction`, `East wing`, `Exit Door`) | Static YAML inspection of `Main_Level.unity` verified: (a) both `Scroll vat` objects use `Player_PaletteSwap.mat` instead of `Sprite-Lit-Default.mat`; (b) `Climbable chain` at `(18.5, 1.5)` spans `y = [0.0..3.0]` instead of `y = [-1.0..2.0]` (leaving a `1.0u` bottom gap above `Floor 15` at `y = -1.0`); (c) all 40 `Board` children under `Archive gallery` repeat left end-cap `woodenPlatform_left_00.png` (`guid: 4a49e728a44b17f43ac9f05d82e67203`); (d) `Power Scroll HUD/Instruction` has `m_IsActive: 0`; (e) `East wing` (`x = 52..112`) has `0` enemies and `Enemy_Rat` is unused; (f) `Exit Door (visual only)` at `(108, 0)` has no trigger or `RunSession` victory hook. | Broken scroll-vat shader, unreachable chain bottom from `Floor 15`, 40 repeated left end-caps in `Archive gallery`, hidden scroll tutorial prompt, 60-unit empty East Wing, and non-functional exit door. |
| **RC-7** | Layer A (`Root`) & Layer C (`Nhà Giam Giếng Chuông` v7) | `Assets/_Project/Features/Enemies/Scripts/Projectile.cs:28, 43-52, 115-143` & `ProjectileSpawner.cs:17-18, 50-58` | `NhaGiamGiengChuong.md:154` requires Room `E`'s center `OneWayPlatform` (`layer 9`) to serve solely for elevation switching and **never** block horizontal Archer arrows (`Mode.Straight`). Currently `Projectile.OnTriggerEnter2D` (`line 139`) checks `else if ((_ground.value & layerBit) != 0) Finish();` using the same `_ground` mask (`Ground | OneWayPlatform = 576`) that `Mode.Lob` grenades need in `OnCollisionEnter2D` (`line 117`) to land on one-way platforms. | Horizontal Archer arrows fired across Room `E`'s center one-way platform are destroyed on contact with `layer 9`, violating `NhaGiamGiengChuong.md:154`. |

---

## Task 1: Incremental Palette Update & Manifest Pre-Validation in `GothicDungeonImporter` (`G3` / RC-1)

**Checkout:** `D:/thietkegame` (Root Repo — and cherry-pickable to `.fortress-workspaces/antigravity`)

**Files:**
- Modify: `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`
- Modify: `Assets/_Project/Core/Tests/EditMode/Core.Tests.EditMode.asmdef`
- Create: `Assets/_Project/Core/Tests/EditMode/GothicDungeonImporterIncrementalTests.cs`

**Interfaces:**
```csharp
namespace DeadCells.Tools.Editor
{
    public static class GothicDungeonImporter
    {
        public static void Import();
        public static void Import(string manifestPathOverride, string rootOverride = null);
        public static void Verify();
        public static void Verify(string manifestPathOverride, string rootOverride = null);
    }
}
```

- [ ] **Step 1: Update `Core.Tests.EditMode.asmdef` and write the failing EditMode unit tests in `GothicDungeonImporterIncrementalTests.cs`**

Update `Assets/_Project/Core/Tests/EditMode/Core.Tests.EditMode.asmdef` so EditMode tests in `D:/thietkegame` can reference `TilesetTools.Editor`, `Combat`, `Currency`, `Enemies`, `HUD`, `Player`, `RunFlow`, `VFX`, and `UnityEngine.UI`:

```json
{
    "name": "Core.Tests.EditMode",
    "rootNamespace": "DeadCells.Core.Tests",
    "references": [
        "Core",
        "Combat",
        "Currency",
        "Enemies",
        "HUD",
        "Player",
        "RunFlow",
        "VFX",
        "TilesetTools.Editor",
        "UnityEngine.UI",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ]
}
```

Create `Assets/_Project/Core/Tests/EditMode/GothicDungeonImporterIncrementalTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeadCells.Tools.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadCells.Core.Tests
{
    public sealed class GothicDungeonImporterIncrementalTests
    {
        private const string TempRoot = "Assets/_Project/Core/Tests/EditMode/__TempGothicImportTest";

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
                AssetDatabase.DeleteAsset(TempRoot);
            if (Directory.Exists(TempRoot))
                Directory.Delete(TempRoot, recursive: true);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Import_WhenNewPaintableSpriteAddedToExistingGroup_AppendsTilePreservingExistingCellsAndPaletteGuid()
        {
            Directory.CreateDirectory(TempRoot + "/Textures");
            WriteSolidPng(TempRoot + "/Textures/TestSheet.png", 64, 32);
            AssetDatabase.Refresh();

            string manifestPath = TempRoot + "/import-manifest.json";
            string initialManifest =
                "{\"sheets\":[{\"file\":\"TestSheet.png\",\"width\":64,\"height\":32,\"pixelsPerUnit\":32,\"sprites\":[" +
                "{\"name\":\"Block_001\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":0,\"y\":0,\"width\":32,\"height\":32,\"referenceOnly\":false,\"outlines\":[]}," +
                "{\"name\":\"Block_002\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":32,\"y\":0,\"width\":32,\"height\":32,\"referenceOnly\":true,\"outlines\":[]}" +
                "]}]}";
            File.WriteAllText(manifestPath, initialManifest);

            GothicDungeonImporter.Import(manifestPath, TempRoot);

            string palettePath = TempRoot + "/Palettes/GothicDungeon_Terrain.prefab";
            string initialGuid = AssetDatabase.AssetPathToGUID(palettePath);
            Assert.IsFalse(string.IsNullOrEmpty(initialGuid));

            var initialPalette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
            var initialMap = initialPalette.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(1, initialMap.GetUsedTilesCount());

            Vector3Int block1Cell = FindTileCell(initialMap, "Block_001");

            // Flip Block_002 from referenceOnly:true to referenceOnly:false in existing group "Terrain"
            string updatedManifest = initialManifest.Replace("\"referenceOnly\":true", "\"referenceOnly\":false");
            File.WriteAllText(manifestPath, updatedManifest);

            GothicDungeonImporter.Import(manifestPath, TempRoot);

            string updatedGuid = AssetDatabase.AssetPathToGUID(palettePath);
            Assert.AreEqual(initialGuid, updatedGuid, "Palette prefab GUID must not change on incremental update.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GridPalette>(palettePath), "GridPalette sub-asset must be preserved after incremental palette update.");

            var updatedPalette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
            var updatedMap = updatedPalette.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(2, updatedMap.GetUsedTilesCount(), "Existing palette must include newly added tile in group.");
            Assert.AreEqual(block1Cell, FindTileCell(updatedMap, "Block_001"), "Existing tile cell coordinates must not shift.");
            Assert.AreNotEqual(block1Cell, FindTileCell(updatedMap, "Block_002"), "Appended tile must occupy a distinct non-overlapping cell.");

            // Running Import a second time with the updated manifest must be idempotent
            GothicDungeonImporter.Import(manifestPath, TempRoot);
            var rerunPalette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
            Assert.AreEqual(2, rerunPalette.GetComponentInChildren<Tilemap>().GetUsedTilesCount());
        }

        [Test]
        public void Import_WhenManifestHasDuplicateSpriteNameOrOutOfBoundsRect_ThrowsBeforeMutatingAssets()
        {
            Directory.CreateDirectory(TempRoot + "/Textures");
            WriteSolidPng(TempRoot + "/Textures/TestSheet.png", 32, 32);
            AssetDatabase.Refresh();

            string manifestPath = TempRoot + "/import-manifest.json";
            string invalidBoundsManifest =
                "{\"sheets\":[{\"file\":\"TestSheet.png\",\"width\":32,\"height\":32,\"pixelsPerUnit\":32,\"sprites\":[" +
                "{\"name\":\"OutBounds_001\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":16,\"y\":16,\"width\":32,\"height\":32,\"referenceOnly\":false,\"outlines\":[]}" +
                "]}]}";
            File.WriteAllText(manifestPath, invalidBoundsManifest);

            var ex = Assert.Throws<InvalidOperationException>(() => GothicDungeonImporter.Import(manifestPath, TempRoot));
            StringAssert.Contains("OutBounds_001", ex.Message);
            Assert.IsFalse(File.Exists(TempRoot + "/Tiles/Terrain/OutBounds_001.asset"), "Pre-validation failure must not write partial tile assets.");
        }

        private static Vector3Int FindTileCell(Tilemap map, string tileName)
        {
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                var tile = map.GetTile(pos);
                if (tile != null && tile.name == tileName)
                    return pos;
            }
            Assert.Fail($"Tile '{tileName}' not found in palette Tilemap.");
            return default;
        }

        private static void WriteSolidPng(string path, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                var pixels = Enumerable.Repeat(new Color32(180, 180, 200, 255), width * height).ToArray();
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
```

- [ ] **Step 2: Run the EditMode test to verify it fails against unpatched `GothicDungeonImporter.cs`**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.GothicDungeonImporterIncrementalTests" -testResults D:/thietkegame/Logs/g3-red.xml -logFile D:/thietkegame/Logs/g3-red.log
```
Expected failure: `GothicDungeonImporter.Import(string, string)` overload does not exist yet (or once overload is forwarded to existing logic, `Import_WhenNewPaintableSpriteAddedToExistingGroup_AppendsTilePreservingExistingCellsAndPaletteGuid` fails at `Verify()` with `System.InvalidOperationException: Palette is incomplete: Terrain`).

- [ ] **Step 3: Implement pre-validation and incremental palette upsert in `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`**

Update `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs` so:
1. `Import()` delegates to `Import(ManifestPath, Root)` and `Verify()` delegates to `Verify(ManifestPath, Root)`.
2. `ValidateManifestBeforeImport(Manifest manifest, string activeRoot)` validates all sheets, texture files, non-empty group names, duplicate sprite names across sheets, and rect bounds (`entry.x >= 0`, `entry.y >= 0`, `entry.width > 0`, `entry.height > 0`, `entry.x + entry.width <= sheet.width`, `entry.y + entry.height <= sheet.height`) **before** any texture reimport, tile asset creation, or palette modification occurs.
3. `UpsertPalette(string rootFolder, string groupKey, List<Tile> groupTiles)` checks if `GothicDungeon_<groupKey>.prefab` already exists:
   - If it does not exist, it creates the palette via `GridPaletteUtility.CreateNewPalette` and places all `groupTiles`.
   - If it already exists, it loads the prefab asset, collects all currently placed `TileBase` references in the `Tilemap`, and computes `missingTiles = groupTiles.Where(t => !existingTiles.Contains(t)).ToList()`.
   - If `missingTiles.Count == 0`, it returns immediately without re-saving the prefab (preserving byte-for-byte identity on idempotent reruns).
   - If `missingTiles.Count > 0`, it loads the prefab via `PrefabUtility.LoadPrefabContents(path)`, starts a new row below the lowest occupied visual row (`rowY = minOccupiedY - maxExistingHeight`), places each missing tile using display-bounds spacing (`width = Mathf.CeilToInt(tile.sprite.bounds.size.x) + 2`, `height = Mathf.CeilToInt(tile.sprite.bounds.size.y) + 2`, wrapping at column `56`), and saves via `PrefabUtility.SaveAsPrefabAsset(contents, path)`.

Complete replacement for `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadCells.Tools.Editor
{
    public static class GothicDungeonImporter
    {
        private const string Root = "Assets/_Project/Art/Environment/GothicDungeon";
        private const string ManifestPath = "SourceAssets/Tilesets/GothicDungeon/import-manifest.json";
        private const string MaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [Serializable] private class Manifest { public Sheet[] sheets; }
        [Serializable] private class Sheet
        {
            public string file;
            public int width, height, pixelsPerUnit;
            public Entry[] sprites;
        }
        [Serializable] private class Entry
        {
            public string name, group, category;
            public int x, y, width, height;
            public bool referenceOnly;
            public Outline[] outlines;
        }
        [Serializable] private class Outline { public Vector2[] points; }

        [MenuItem("Tools/DeadCells/Tilesets/Import Gothic Dungeon")]
        public static void Import() => Import(ManifestPath, Root);

        public static void Import(string manifestPathOverride, string rootOverride = null)
        {
            string activeManifestPath = string.IsNullOrWhiteSpace(manifestPathOverride) ? ManifestPath : manifestPathOverride;
            string activeRoot = string.IsNullOrWhiteSpace(rootOverride) ? Root : rootOverride;

            Check(File.Exists(activeManifestPath), "Missing manifest: " + activeManifestPath);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(activeManifestPath));
            ValidateManifestBeforeImport(manifest, activeRoot);

            var groups = new Dictionary<string, List<Tile>>(StringComparer.Ordinal);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            foreach (var sheet in manifest.sheets)
            {
                var path = activeRoot + "/Textures/" + sheet.file;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Check(importer != null, "Missing texture: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = sheet.pixelsPerUnit;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.Tight;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 8192;
                importer.npotScale = TextureImporterNPOTScale.None;

                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var rects = sheet.sprites.Select(entry => new SpriteRect
                {
                    name = entry.name,
                    rect = new Rect(entry.x, entry.y, entry.width, entry.height),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                    spriteID = new GUID(Hash128.Compute(sheet.file + ":" + entry.name).ToString())
                }).ToArray();
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                    rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
                var outlineProvider = provider.GetDataProvider<ISpriteOutlineDataProvider>();
                for (int i = 0; i < rects.Length; i++)
                {
                    var entry = sheet.sprites[i];
                    if (entry.outlines != null && entry.outlines.Length > 0)
                        outlineProvider.SetOutlines(rects[i].spriteID, entry.outlines.Select(outline => outline.points).ToList());
                }
                provider.Apply();
                importer.SaveAndReimport();
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
                Check(sprites.Count == sheet.sprites.Length, "Sprite count mismatch: " + path);
                foreach (var entry in sheet.sprites)
                {
                    var sprite = sprites[entry.name];
                    Check(sprite.vertices.Length >= 3, "Empty sprite mesh: " + entry.name);
                    if (entry.referenceOnly)
                        continue;
                    EnsureFolder(activeRoot + "/Tiles/" + entry.group);
                    var tilePath = activeRoot + "/Tiles/" + entry.group + "/" + entry.name + ".asset";
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                    if (tile == null)
                    {
                        Check(!File.Exists(tilePath), "Existing file is not a Tile: " + tilePath);
                        tile = ScriptableObject.CreateInstance<Tile>();
                        tile.name = entry.name;
                        tile.sprite = sprite;
                        tile.colliderType = Tile.ColliderType.None;
                        AssetDatabase.CreateAsset(tile, tilePath);
                    }
                    else if (tile.sprite != sprite || tile.colliderType != Tile.ColliderType.None)
                    {
                        tile.sprite = sprite;
                        tile.colliderType = Tile.ColliderType.None;
                        EditorUtility.SetDirty(tile);
                    }
                    if (!groups.TryGetValue(entry.group, out var tiles))
                        groups.Add(entry.group, tiles = new List<Tile>());
                    tiles.Add(tile);
                }
                Console.WriteLine("GOTHIC_SHEET_IMPORTED " + sheet.file + " sprites=" + sprites.Count);
            }

            EnsureFolder(activeRoot + "/Palettes");
            foreach (var group in groups)
                UpsertGroupPalette(activeRoot, group.Key, group.Value);

            AssetDatabase.SaveAssets();
            Verify(activeManifestPath, activeRoot);
        }

        public static void Verify() => Verify(ManifestPath, Root);

        public static void Verify(string manifestPathOverride, string rootOverride = null)
        {
            string activeManifestPath = string.IsNullOrWhiteSpace(manifestPathOverride) ? ManifestPath : manifestPathOverride;
            string activeRoot = string.IsNullOrWhiteSpace(rootOverride) ? Root : rootOverride;

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(activeManifestPath));
            int spriteCount = 0, tileCount = 0;
            var expectedGroups = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var sheet in manifest.sheets)
            {
                var path = activeRoot + "/Textures/" + sheet.file;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Check(texture.width == sheet.width && texture.height == sheet.height, "Texture was downscaled: " + path);
                Check(importer.spritePixelsPerUnit == sheet.pixelsPerUnit && !importer.mipmapEnabled
                    && importer.filterMode == FilterMode.Point && !importer.isReadable, "Invalid import settings: " + path);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
                Check(sprites.Count == sheet.sprites.Length, "Unexpected sprite count: " + path);
                foreach (var entry in sheet.sprites)
                {
                    var sprite = sprites[entry.name];
                    Check(sprite.rect == new Rect(entry.x, entry.y, entry.width, entry.height)
                        && sprite.vertices.Length >= 3 && sprite.triangles.Length >= 3, "Invalid sprite: " + entry.name);
                    spriteCount++;
                    if (entry.referenceOnly)
                        continue;
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(activeRoot + "/Tiles/" + entry.group + "/" + entry.name + ".asset");
                    Check(tile != null && tile.sprite == sprite && tile.colliderType == Tile.ColliderType.None,
                        "Invalid tile reference/collider: " + entry.name);
                    tileCount++;
                    expectedGroups[entry.group] = expectedGroups.TryGetValue(entry.group, out int count) ? count + 1 : 1;
                }
            }
            foreach (var group in expectedGroups)
            {
                var palette = AssetDatabase.LoadAssetAtPath<GameObject>(activeRoot + "/Palettes/GothicDungeon_" + group.Key + ".prefab");
                Check(palette != null && palette.GetComponentInChildren<Tilemap>().GetUsedTilesCount() == group.Value,
                    "Palette is incomplete: " + group.Key);
            }
            Console.WriteLine("GOTHIC_TILESET_VERIFIED sprites=" + spriteCount + " tiles=" + tileCount + " palettes=" + expectedGroups.Count);
        }

        private static void ValidateManifestBeforeImport(Manifest manifest, string activeRoot)
        {
            Check(manifest != null && manifest.sheets != null && manifest.sheets.Length > 0, "Manifest contains no sheets.");
            var seenSprites = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sheet in manifest.sheets)
            {
                Check(sheet != null && !string.IsNullOrWhiteSpace(sheet.file), "Manifest sheet file name is empty.");
                Check(sheet.width > 0 && sheet.height > 0 && sheet.pixelsPerUnit > 0, "Invalid dimensions/PPU on sheet: " + sheet.file);
                string texPath = activeRoot + "/Textures/" + sheet.file;
                Check(File.Exists(texPath), "Missing texture: " + texPath);
                Check(sheet.sprites != null && sheet.sprites.Length > 0, "Sheet has no sprites: " + sheet.file);

                foreach (var entry in sheet.sprites)
                {
                    Check(entry != null && !string.IsNullOrWhiteSpace(entry.name), "Empty sprite name in sheet: " + sheet.file);
                    Check(seenSprites.Add(entry.name), "Duplicate sprite name in manifest: " + entry.name);
                    Check(entry.x >= 0 && entry.y >= 0 && entry.width > 0 && entry.height > 0
                        && entry.x + entry.width <= sheet.width && entry.y + entry.height <= sheet.height,
                        $"Sprite rect out of bounds for {entry.name} ({entry.x},{entry.y},{entry.width},{entry.height}) on {sheet.file} ({sheet.width}x{sheet.height}).");
                    if (!entry.referenceOnly)
                        Check(!string.IsNullOrWhiteSpace(entry.group), "Paintable sprite missing group: " + entry.name);
                }
            }
        }

        private static void UpsertGroupPalette(string activeRoot, string groupKey, List<Tile> groupTiles)
        {
            var name = "GothicDungeon_" + groupKey;
            var path = activeRoot + "/Palettes/" + name + ".prefab";
            bool existed = File.Exists(path);

            if (existed)
            {
                var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var existingMap = existingPrefab != null ? existingPrefab.GetComponentInChildren<Tilemap>() : null;
                if (existingMap != null)
                {
                    var placedSet = CollectPlacedTiles(existingMap);
                    if (groupTiles.All(t => placedSet.Contains(t)))
                        return;
                }
            }
            else
            {
                GridPaletteUtility.CreateNewPalette(activeRoot + "/Palettes", name, GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Manual, new Vector3(1f, 1f, 0f), GridLayout.CellSwizzle.XYZ);
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var map = contents.GetComponentInChildren<Tilemap>();
                map.name = groupKey;
                map.GetComponent<TilemapRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

                var alreadyPlaced = CollectPlacedTiles(map);
                var tilesToPlace = existed
                    ? groupTiles.Where(t => !alreadyPlaced.Contains(t)).ToList()
                    : groupTiles;

                int column = 0;
                int rowY = 0;
                int rowHeight = 0;
                if (existed && alreadyPlaced.Count > 0)
                    rowY = ComputeNextAppendRowY(map);

                foreach (var tile in tilesToPlace)
                {
                    int width = Mathf.CeilToInt(tile.sprite.bounds.size.x) + 2;
                    int height = Mathf.CeilToInt(tile.sprite.bounds.size.y) + 2;
                    if (column + width > 56 && column > 0)
                    {
                        column = 0;
                        rowY -= rowHeight;
                        rowHeight = 0;
                    }
                    map.SetTile(new Vector3Int(column + width / 2, rowY - height / 2, 0), tile);
                    column += width;
                    rowHeight = Mathf.Max(rowHeight, height);
                }
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(contents, path);
                EnsureGridPaletteSubAsset(savedPrefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureGridPaletteSubAsset(GameObject savedPrefab, string prefabPath)
        {
            if (savedPrefab == null || AssetDatabase.LoadAssetAtPath<GridPalette>(prefabPath) != null)
                return;

            var gridPalette = ScriptableObject.CreateInstance<GridPalette>();
            gridPalette.name = "Palette Settings";
            gridPalette.cellSizing = GridPalette.CellSizing.Manual;
            gridPalette.transparencySortMode = TransparencySortMode.Default;
            gridPalette.transparencySortAxis = new Vector3(0f, 0f, 1f);
            AssetDatabase.AddObjectToAsset(gridPalette, savedPrefab);
            EditorUtility.SetDirty(savedPrefab);
        }

        private static HashSet<TileBase> CollectPlacedTiles(Tilemap map)
        {
            var set = new HashSet<TileBase>();
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                var t = map.GetTile(pos);
                if (t != null)
                    set.Add(t);
            }
            return set;
        }

        private static int ComputeNextAppendRowY(Tilemap map)
        {
            int minBottomY = 0;
            bool any = false;
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                if (map.GetTile(pos) is not Tile tile || tile.sprite == null)
                    continue;
                int halfH = (Mathf.CeilToInt(tile.sprite.bounds.size.y) + 2) / 2 + 1;
                int bottomY = pos.y - halfH;
                if (!any || bottomY < minBottomY)
                {
                    minBottomY = bottomY;
                    any = true;
                }
            }
            return any ? minBottomY : 0;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            Check(!string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path))), "Could not create " + path);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
```

- [ ] **Step 4: Run the EditMode unit tests and full tileset verification**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.GothicDungeonImporterIncrementalTests" -testResults D:/thietkegame/Logs/g3-green.xml -logFile D:/thietkegame/Logs/g3-green.log
```
Expected output: `total="2" passed="2" failed="0"` in `Logs/g3-green.xml`, and `GothicDungeonImporter.Verify()` passes with `GOTHIC_TILESET_VERIFIED sprites=675 tiles=612 palettes=4`.

- [ ] **Step 5: Commit Task 1 on the active workstream branch**

```powershell
rtk git status --short
rtk git add Assets/_Project/Tools/Editor/GothicDungeonImporter.cs Assets/_Project/Core/Tests/EditMode/Core.Tests.EditMode.asmdef Assets/_Project/Core/Tests/EditMode/GothicDungeonImporterIncrementalTests.cs
rtk git commit -m "fix(tileset): support incremental GothicDungeon palette updates and manifest pre-validation"
```

---

## Task 2: `RuleTile` (`RT_Stone_*`, `RT_Wood`) Surface Alignment on `1×1u` Collision Tilemaps (`A1-RuleTile` / RC-2)

**Checkout:** `D:/thietkegame/.fortress-workspaces/antigravity` (branch `fortress/antigravity-clean`)

**Files:**
- Modify: `Assets/_Project/Features/FortressPresentation/Scripts/FortressRoomDescriptor.cs`
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressRuleTileAndPaletteGenerator.cs`
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressRoomAndCollisionTools.cs`
- Modify: `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`

**Interfaces:**
```csharp
namespace DeadCells.FortressPresentation
{
    public class FortressRoomDescriptor : MonoBehaviour
    {
        public void AlignRoomRuleTileSurfaces();
        public static void AlignTilemapRuleTileSurfaces(UnityEngine.Tilemaps.Tilemap tm);
    }
}
```

- [ ] **Step 1: Add failing EditMode test `R11_RuleTiles_PaintedOnFloorsPlatformsAndCeilings_AlignVisualSurfaceWithCollider` to `FortressAuthoringPipelineTests.cs`**

Append to `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs` (inside `class FortressAuthoringPipelineTests`):

```csharp
        [Test]
        public void R11_RuleTiles_PaintedOnFloorsPlatformsAndCeilings_AlignVisualSurfaceWithCollider()
        {
            var rtStone = AssetDatabase.LoadAssetAtPath<RuleTile>($"{FortressRuleTileGenerator.TerrainRoot}/RT_Stone_Normal.asset");
            var rtWood = AssetDatabase.LoadAssetAtPath<RuleTile>($"{FortressRuleTileGenerator.TerrainRoot}/RT_Wood.asset");
            var templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FortressRoomTemplateBuilder.TemplatePath);
            Assert.IsNotNull(rtStone, "RT_Stone_Normal must exist.");
            Assert.IsNotNull(rtWood, "RT_Wood must exist.");
            Assert.IsNotNull(templatePrefab, "Fortress_Room_Template must exist.");

            // Verify right-edge rule on RT_Stone_Normal uses MirrorX so WallTrim_001 faces outward on right walls
            var rightEdgeRule = rtStone.m_TilingRules.FirstOrDefault(r =>
                r.m_NeighborPositions.Count == 2 &&
                r.m_NeighborPositions[0] == new Vector3Int(1, 0, 0) && r.m_Neighbors[0] == 2 &&
                r.m_NeighborPositions[1] == new Vector3Int(-1, 0, 0) && r.m_Neighbors[1] == 1);
            Assert.IsNotNull(rightEdgeRule, "RT_Stone_Normal must contain right-edge rule.");
            Assert.AreEqual(RuleTile.TilingRuleOutput.Transform.MirrorX, rightEdgeRule.m_RuleTransform,
                "Right-edge rule on RT_Stone_Normal must mirror horizontal trim sprite via MirrorX.");

            GameObject roomInst = UnityEngine.Object.Instantiate(templatePrefab);
            try
            {
                var tmFloors = roomInst.transform.Find("Grid/TM_Floors").GetComponent<Tilemap>();
                var tmCeilings = roomInst.transform.Find("Grid/TM_Ceilings").GetComponent<Tilemap>();
                var tmPlatforms = roomInst.transform.Find("Grid/TM_Platforms").GetComponent<Tilemap>();
                tmFloors.ClearAllTiles();
                tmCeilings.ClearAllTiles();
                tmPlatforms.ClearAllTiles();

                // Paint a 3x2 block of RT_Stone_Normal on TM_Floors at x = [-1..1], y = [0..1]
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                        tmFloors.SetTile(new Vector3Int(x, y, 0), rtStone);
                }

                // Paint a 3x2 block of RT_Stone_Normal on TM_Ceilings at x = [-1..1], y = [8..9]
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = 8; y <= 9; y++)
                        tmCeilings.SetTile(new Vector3Int(x, y, 0), rtStone);
                }

                // Paint a 3x1 span of RT_Wood on TM_Platforms at x = [-1..1], y = 4
                for (int x = -1; x <= 1; x++)
                    tmPlatforms.SetTile(new Vector3Int(x, 4, 0), rtWood);

                tmFloors.RefreshAllTiles();
                tmCeilings.RefreshAllTiles();
                tmPlatforms.RefreshAllTiles();
                var roomDesc = roomInst.GetComponent<FortressRoomDescriptor>();
                Assert.IsNotNull(roomDesc, "Fortress_Room_Template must have FortressRoomDescriptor.");
                roomDesc.AlignRoomRuleTileSurfaces();

                var report = new FortressCollisionValidator.CollisionValidationReport();
                FortressCollisionValidator.ValidateRoomRoot(roomInst, "TempRuleTileAlignmentRoom", report);

                var misalignments = report.findings.Where(f => f.code == "VISUAL_COLLIDER_MISALIGNMENT").ToList();
                Assert.AreEqual(0, misalignments.Count,
                    "RuleTiles painted on TM_Floors, TM_Ceilings, and TM_Platforms must align with collider surfaces (<= 0.05u), found: " +
                    string.Join(" | ", misalignments.Select(m => m.message)));

                // Verify literal top/bottom world alignment on exposed surface cells
                Vector3 floorTopCellCenter = tmFloors.GetCellCenterLocal(new Vector3Int(0, 1, 0));
                Sprite floorTopSprite = tmFloors.GetSprite(new Vector3Int(0, 1, 0));
                Matrix4x4 floorTopMatrix = tmFloors.GetTransformMatrix(new Vector3Int(0, 1, 0));
                float floorVisualTopY = floorTopCellCenter.y + floorTopMatrix.MultiplyPoint3x4(floorTopSprite.bounds.max).y;
                Assert.AreEqual(2.0f, floorVisualTopY, 0.01f, "Top row (y=1) of TM_Floors must have visual top at y=2.0u.");

                Vector3 platCellCenter = tmPlatforms.GetCellCenterLocal(new Vector3Int(0, 4, 0));
                Sprite platSprite = tmPlatforms.GetSprite(new Vector3Int(0, 4, 0));
                Matrix4x4 platMatrix = tmPlatforms.GetTransformMatrix(new Vector3Int(0, 4, 0));
                float platVisualTopY = platCellCenter.y + platMatrix.MultiplyPoint3x4(platSprite.bounds.max).y;
                Assert.AreEqual(5.0f, platVisualTopY, 0.01f, "Platform row (y=4) of TM_Platforms must have visual top at y=5.0u.");

                Vector3 ceilBottomCellCenter = tmCeilings.GetCellCenterLocal(new Vector3Int(0, 8, 0));
                Sprite ceilBottomSprite = tmCeilings.GetSprite(new Vector3Int(0, 8, 0));
                Matrix4x4 ceilBottomMatrix = tmCeilings.GetTransformMatrix(new Vector3Int(0, 8, 0));
                float ceilVisualBottomY = ceilBottomCellCenter.y + ceilBottomMatrix.MultiplyPoint3x4(ceilBottomSprite.bounds.min).y;
                Assert.AreEqual(8.0f, ceilVisualBottomY, 0.01f, "Bottom row (y=8) of TM_Ceilings must have visual bottom at y=8.0u.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roomInst);
            }
        }
```

- [ ] **Step 2: Run `R11` to verify it fails against unpatched code**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -runTests -testPlatform EditMode -testFilter "DeadCells.FortressPresentation.Tests.FortressAuthoringPipelineTests.R11_RuleTiles_PaintedOnFloorsPlatformsAndCeilings_AlignVisualSurfaceWithCollider" -testResults D:/thietkegame/.fortress-workspaces/antigravity/Logs/r11-red.xml -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r11-red.log
```
Expected failure: `Right-edge rule on RT_Stone_Normal must mirror horizontal trim sprite via MirrorX` (currently `Fixed`) AND `VISUAL_COLLIDER_MISALIGNMENT` findings (`0.516u–0.594u > 0.05u`).

- [ ] **Step 3: Implement right-edge `mirrorX: true`, `FortressRoomDescriptor.AlignTilemapRuleTileSurfaces`, and surface-only `ValidateTilemapVisualAlignment`**

1. In `Assets/_Project/Tools/Editor/FortressProduction/FortressRuleTileAndPaletteGenerator.cs:538`, update the right-edge rule in `BuildStoneRuleTile`:
```csharp
            // Cardinal edges (Top, Bottom, Left, Right)
            existing.m_TilingRules.Add(MakeRule(top, solidCollider, new[] { (0, 1, 2), (0, -1, 1) }));
            existing.m_TilingRules.Add(MakeRule(bottom, solidCollider, new[] { (0, -1, 2), (0, 1, 1) }));
            existing.m_TilingRules.Add(MakeRule(edge, solidCollider, new[] { (-1, 0, 2), (1, 0, 1) }));
            existing.m_TilingRules.Add(MakeRule(edge, solidCollider, new[] { (1, 0, 2), (-1, 0, 1) }, mirrorX: true));
```

2. In `Assets/_Project/Features/FortressPresentation/Scripts/FortressRoomDescriptor.cs`, call `AlignRoomRuleTileSurfaces()` inside `Awake()` and `OnEnable()`, and add `AlignRoomRuleTileSurfaces()` + `AlignTilemapRuleTileSurfaces(Tilemap tm)`:
```csharp
        public void AlignRoomRuleTileSurfaces()
        {
            var tilemaps = GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true);
            for (int i = 0; i < tilemaps.Length; i++)
                AlignTilemapRuleTileSurfaces(tilemaps[i]);
        }

        public static void AlignTilemapRuleTileSurfaces(UnityEngine.Tilemaps.Tilemap tm)
        {
            if (tm == null)
                return;

            string n = tm.gameObject.name;
            bool isFloorOrPlatform = n == "TM_Floors" || n == "TM_Platforms";
            bool isCeiling = n == "TM_Ceilings";
            if (!isFloorOrPlatform && !isCeiling)
                return;

            Vector3Int surfaceOutwardDir = isCeiling ? Vector3Int.down : Vector3Int.up;
            var bounds = tm.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                var tileBase = tm.GetTile(pos);
                if (tileBase is not RuleTile)
                    continue;

                // Only align exposed surface cells (cells without an adjacent solid cell in the surface normal direction)
                if (tm.GetTile(pos + surfaceOutwardDir) != null)
                    continue;

                Sprite sprite = tm.GetSprite(pos);
                if (sprite == null)
                    continue;

                float yOffset = isCeiling
                    ? (-0.5f - sprite.bounds.min.y)
                    : (0.5f - sprite.bounds.max.y);

                // Preserve 3x3 rotation/reflection/scale columns (including MirrorX negative determinant) and update only Y translation (m13)
                Matrix4x4 current = tm.GetTransformMatrix(pos);
                current.m03 = 0f;
                current.m13 = yOffset;
                current.m23 = 0f;
                tm.SetTileFlags(pos, UnityEngine.Tilemaps.TileFlags.None);
                tm.SetTransformMatrix(pos, current);
            }
        }
```

3. In `Assets/_Project/Tools/Editor/FortressProduction/FortressRoomAndCollisionTools.cs`:
   - At lines `385-387` inside `ValidateRoomHierarchy`:
```csharp
                if (refreshTilemaps && isActive)
                    tm.RefreshAllTiles();
                if (isActive && (n == "TM_Floors" || n == "TM_Platforms" || n == "TM_Ceilings"))
                    FortressRoomDescriptor.AlignTilemapRuleTileSurfaces(tm);
```
   - At lines `498-510` inside `ValidateTilemapVisualAlignment`:
```csharp
            Vector3Int outwardNormal = checkBottom ? Vector3Int.down : Vector3Int.up;
            var bounds = tm.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                var tileBase = tm.GetTile(pos);
                if (tileBase == null)
                    continue;

                Tile.ColliderType colType = ResolveColliderType(tm, pos, tileBase);
                if (colType != Tile.ColliderType.Grid)
                    continue;

                // Interior cells covered by another active collision cell in the surface normal direction are not exposed floor/ceiling surfaces
                var neighborTile = tm.GetTile(pos + outwardNormal);
                if (neighborTile != null && ResolveColliderType(tm, pos + outwardNormal, neighborTile) != Tile.ColliderType.None)
                    continue;
```

- [ ] **Step 4: Regenerate RuleTiles and run `R11`, `A04`, `R03`, and `R10` to verify Green**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -executeMethod DeadCells.Tools.Editor.FortressProduction.FortressRuleTileGenerator.GenerateAllMenu -quit -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r11-gen.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -runTests -testPlatform EditMode -testFilter "R11_RuleTiles|A04_RuleTiles|R03_Rooms|R10_PaletteDisplaySpacing" -testResults D:/thietkegame/.fortress-workspaces/antigravity/Logs/r11-green.xml -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r11-green.log
```
Expected output: `total="4" passed="4" failed="0"` in `Logs/r11-green.xml`.

- [ ] **Step 5: Commit Task 2 in `.fortress-workspaces/antigravity`**

```powershell
rtk proxy git -C D:/thietkegame/.fortress-workspaces/antigravity add Assets/_Project/Features/FortressPresentation/Scripts/FortressRoomDescriptor.cs Assets/_Project/Tools/Editor/FortressProduction/FortressRuleTileAndPaletteGenerator.cs Assets/_Project/Tools/Editor/FortressProduction/FortressRoomAndCollisionTools.cs Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs Assets/_Project/Art/Environment/FortressGenerated/Terrain/RT_Stone_*.asset
rtk proxy git -C D:/thietkegame/.fortress-workspaces/antigravity commit -m "fix(authoring): align RuleTile floor/platform/ceiling surfaces and mirror right-edge stone rules"
```

---

## Task 3: Fortress Ambient VFX Pixel Material & Visual Prefab / Preview Scene Remediation (`A1-VFX-Prefab` / RC-3 & RC-4)

**Checkout:** `D:/thietkegame/.fortress-workspaces/antigravity` (branch `fortress/antigravity-clean`)

**Files:**
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressAnimationAndPrefabGenerator.cs`
- Modify: `Assets/_Project/Tools/Editor/FortressProduction/FortressDashboardAndPipelineRunner.cs`
- Modify: `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`

**Interfaces:**
- `FortressAmbientGenerator.GenerateAll()` (`Assets/_Project/Tools/Editor/FortressProduction/FortressAnimationAndPrefabGenerator.cs:1182`)
- `FortressPrefabGenerator.GenerateAll()` (`Assets/_Project/Tools/Editor/FortressProduction/FortressAnimationAndPrefabGenerator.cs:1370`)
- `FortressArtPreviewBuilder.BuildAndSavePreviewScene()` (`Assets/_Project/Tools/Editor/FortressProduction/FortressDashboardAndPipelineRunner.cs:1017`)

- [ ] **Step 1: Add failing EditMode tests `R12` and `R13` to `FortressAuthoringPipelineTests.cs`**

Append to `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`:

```csharp
        [Test]
        public void R12_AmbientParticleMaterial_IsTransparentWithCleanPixelParticlesAndShrinkingCurve()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FortressAmbientGenerator.MaterialsRoot + "/Mat_FortressPixelParticle.mat");
            Assert.IsNotNull(mat, "Mat_FortressPixelParticle.mat must exist.");
            Assert.AreEqual(3000, mat.renderQueue, "Particle material must use Transparent renderQueue (3000).");
            if (mat.HasProperty("_Surface"))
                Assert.AreEqual(1f, mat.GetFloat("_Surface"), 0.001f, "URP particle material must set _Surface=1 (Transparent).");
            if (mat.HasProperty("_ZWrite"))
                Assert.AreEqual(0f, mat.GetFloat("_ZWrite"), 0.001f, "Transparent particle material must disable _ZWrite.");

            var tex = (mat.GetTexture("_BaseMap") ?? mat.mainTexture) as Texture2D;
            Assert.IsNotNull(tex, "Particle material must bind a point-filtered texture.");
            Assert.AreEqual(FilterMode.Point, tex.filterMode);
            Assert.LessOrEqual(tex.width, 8, "Pixel particle texture must be a compact pixel cluster (<= 8x8 px), not a full boulder atlas.");
            Assert.LessOrEqual(tex.height, 8);

            string[] ambientNames =
            {
                "PF_Ambient_FloatingDust", "PF_Ambient_RisingEmbers", "PF_Ambient_GroundFog",
                "PF_Ambient_DrippingWater", "PF_Ambient_Steam", "PF_Ambient_Smoke",
                "PF_Ambient_MagicalParticles", "PF_Ambient_WaterSplashes", "PF_Ambient_EnvironmentalSparks"
            };
            foreach (var name in ambientNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FortressAmbientGenerator.EffectsPrefabsRoot}/{name}.prefab");
                Assert.IsNotNull(prefab, $"Missing ambient prefab: {name}");
                var ps = prefab.GetComponent<ParticleSystem>();
                Assert.IsNotNull(ps);
                Assert.IsFalse(ps.textureSheetAnimation.enabled,
                    $"{name} must not bind boulder/stalactite sprites (RockDebris_001/CrystalAndSpike_006) via TextureSheetAnimation.");
                Assert.IsTrue(ps.sizeOverLifetime.enabled, $"{name} must enable sizeOverLifetime decay curve.");
            }
        }

        [Test]
        public void R13_VisualPrefabsAndArtPreview_UseCorrectLeverSpikeSpritesAndNonOverlappingGroundedProps()
        {
            var leverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FortressEditorHelpers.VisualPrefabsRoot}/Interactive/PF_Lever.prefab");
            var spikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FortressEditorHelpers.VisualPrefabsRoot}/Hazards/PF_SpikeTrap.prefab");
            Assert.IsNotNull(leverPrefab);
            Assert.IsNotNull(spikePrefab);

            var leverSprite = leverPrefab.GetComponent<SpriteRenderer>().sprite;
            var spikeSprite = spikePrefab.GetComponent<SpriteRenderer>().sprite;
            Assert.IsNotNull(leverSprite);
            Assert.IsNotNull(spikeSprite);
            Assert.AreEqual("MachineryAndBell_006", leverSprite.name, "PF_Lever must use lever sprite MachineryAndBell_006, not hanging church bell MachineryAndBell_001.");
            Assert.AreEqual("CrystalAndSpike_013", spikeSprite.name, "PF_SpikeTrap must use upward floor spikes CrystalAndSpike_013, not downward ceiling stalactite CrystalAndSpike_001.");

            string prevScenePath = SceneManager.GetActiveScene().path;
            try
            {
                var scene = FortressArtPreviewBuilder.BuildAndSavePreviewScene();
                Assert.IsTrue(scene.IsValid() && scene.isLoaded);

                var window = GameObject.Find("BG_GothicWindow_Center");
                var portal = GameObject.Find("PF_MagicalPortal_Cyan");
                var fgPillar = GameObject.Find("FG_OverlapPillar");
                var bgArch = GameObject.Find("BG_Arch_Left");
                Assert.IsNotNull(window);
                Assert.IsNotNull(portal);
                Assert.IsNotNull(fgPillar);
                Assert.IsNotNull(bgArch);

                float horizontalSeparation = Mathf.Abs(window.transform.position.x - portal.transform.position.x);
                Assert.GreaterOrEqual(horizontalSeparation, 3.0f,
                    "PF_MagicalPortal_Cyan and BG_GothicWindow_Center must be horizontally separated so the portal does not occlude the center window.");

                float pillarBottomY = fgPillar.GetComponent<SpriteRenderer>().bounds.min.y;
                float archBottomY = bgArch.GetComponent<SpriteRenderer>().bounds.min.y;
                Assert.LessOrEqual(pillarBottomY, 1.05f, "FG_OverlapPillar base must sit on or below the floor top surface (y=1.0u).");
                Assert.LessOrEqual(archBottomY, 1.05f, "BG_Arch_Left base must sit on or below the floor top surface (y=1.0u).");
            }
            finally
            {
                if (!string.IsNullOrEmpty(prevScenePath) && File.Exists(prevScenePath))
                    EditorSceneManager.OpenScene(prevScenePath, OpenSceneMode.Single);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
```

- [ ] **Step 2: Run `R12` and `R13` to verify they fail against unpatched code**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -runTests -testPlatform EditMode -testFilter "R12_AmbientParticleMaterial|R13_VisualPrefabsAndArtPreview" -testResults D:/thietkegame/.fortress-workspaces/antigravity/Logs/r12-r13-red.xml -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r12-r13-red.log
```
Expected failure:
- `R12`: `tex.width` is `2048` (`T_GothicDungeon_DebrisAndEffects.png`) instead of `<= 8`, `ps.textureSheetAnimation.enabled` is `True`, and `ps.sizeOverLifetime.enabled` is `False`.
- `R13`: `PF_Lever` sprite is `"MachineryAndBell_001"` instead of `"MachineryAndBell_006"`, `PF_SpikeTrap` sprite is `"CrystalAndSpike_001"` instead of `"CrystalAndSpike_013"`, and `horizontalSeparation` between `BG_GothicWindow_Center` and `PF_MagicalPortal_Cyan` is `0.0f < 3.0f`.

- [ ] **Step 3: Implement transparent pixel particle sub-asset texture, `sizeOverLifetime`, corrected prefab sprites, and grounded preview layout**

1. In `Assets/_Project/Tools/Editor/FortressProduction/FortressAnimationAndPrefabGenerator.cs` (`lines 1196-1325`):
   - Update `EnsurePixelParticleMaterial` to create/load a `4×4` `Texture2D` sub-asset (`name = "Tex_FortressPixelParticle"`, `filterMode = FilterMode.Point`, `wrapMode = TextureWrapMode.Clamp`, crisp 4×4 soft-corner pixel diamond/square stored inside `Mat_FortressPixelParticle.mat` via `AssetDatabase.AddObjectToAsset` so zero new `.meta` files are created!) and configure transparent URP render state (`_Surface = 1f`, `_Blend = 0f`, `_SrcBlend = (float)UnityEngine.Rendering.BlendMode.SrcAlpha`, `_DstBlend = (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha`, `_ZWrite = 0f`, `mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT")`, `mat.renderQueue = 3000`).
   - In `CreateAmbientPrefab`: disable `ps.textureSheetAnimation` (`var tsa = ps.textureSheetAnimation; tsa.enabled = false;`) and enable `ps.sizeOverLifetime` with a smooth shrink curve (`AnimationCurve.EaseInOut(0f, 1f, 1f, 0f)`).
   - At lines `1598-1623`:
     - Change `PF_Lever` sprite lookup from `FindFirstSprite(sprites, "MachineryAndBell_001", "MachineryAndBell_006")` to `FindFirstSprite(sprites, "MachineryAndBell_006", "MachineryAndBell_012")`.
     - Change `PF_SpikeTrap` sprite lookup from `FindFirstSprite(sprites, "CrystalAndSpike_001", "CrystalAndSpike_002")` to `FindFirstSprite(sprites, "CrystalAndSpike_013", "CrystalAndSpike_014")`.

Exact code for `EnsurePixelParticleMaterial` and `CreateAmbientPrefab` in `FortressAnimationAndPrefabGenerator.cs`:

```csharp
        private static Material EnsurePixelParticleMaterial(Texture2D unusedDebrisTex = null)
        {
            string matPath = MaterialsRoot + "/Mat_FortressPixelParticle.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Standard Unlit")
                    ?? Shader.Find("Sprites/Default");
                mat = new Material(shader) { name = "Mat_FortressPixelParticle" };
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D pixelTex = AssetDatabase.LoadAllAssetsAtPath(matPath)
                .OfType<Texture2D>()
                .FirstOrDefault(t => t != null && t.name == "Tex_FortressPixelParticle");
            if (pixelTex == null)
            {
                pixelTex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false)
                {
                    name = "Tex_FortressPixelParticle",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                AssetDatabase.AddObjectToAsset(pixelTex, mat);
            }

            pixelTex.filterMode = FilterMode.Point;
            pixelTex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[16];
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    bool isCorner = (x == 0 || x == 3) && (y == 0 || y == 3);
                    bool isCenter = (x == 1 || x == 2) && (y == 1 || y == 2);
                    byte a = isCorner ? (byte)0 : (isCenter ? (byte)255 : (byte)170);
                    px[y * 4 + x] = new Color32(255, 255, 255, a);
                }
            }
            pixelTex.SetPixels32(px);
            pixelTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            EditorUtility.SetDirty(pixelTex);

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", pixelTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", pixelTex);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            return mat;
        }
```

And inside `CreateAmbientPrefab` (`lines 1303-1315`):

```csharp
                var tsa = ps.textureSheetAnimation;
                tsa.enabled = false;

                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
```

2. In `Assets/_Project/Tools/Editor/FortressProduction/FortressDashboardAndPipelineRunner.cs:1040-1064`, update `BuildAndSavePreviewScene()` so background arches, foreground overlap pillars, and the cyan magical portal are grounded on the `y = 1.0u` floor and horizontally separated from `BG_GothicWindow_Center`:

```csharp
            Sprite archSpr = FortressEditorHelpers.FindFirstSprite(sprites, "Arch_008", "Arch_001");
            float archHalfH = archSpr != null ? archSpr.bounds.extents.y : 3.0f;
            SpawnSpriteProp("BG_Arch_Left", archSpr, litMat,
                FortressSortingLayers.LayerBackground, FortressSortingLayers.SortBackgroundArchitecture, 0, new Vector3(-7f, 1f + archHalfH, 0f));

            SpawnSpriteProp("BG_GothicWindow_Center", FortressEditorHelpers.FindFirstSprite(sprites, "GothicWindow_003", "GothicWindow_001"), litMat,
                FortressSortingLayers.LayerBackground, FortressSortingLayers.SortBackgroundArchitecture, 1, new Vector3(0f, 6f, 0f));

            SpawnSpriteProp("BG_WallBanner_Right", FortressEditorHelpers.FindFirstSprite(sprites, "LightAndBanner_005", "LightAndBanner_006"), litMat,
                FortressSortingLayers.LayerBackground, FortressSortingLayers.SortBackgroundDetails, 2, new Vector3(6.5f, 5.5f, 0f));

            Sprite pillarSpr = FortressEditorHelpers.FindFirstSprite(sprites, "Pillar_002", "Pillar_001");
            float pillarHalfH = pillarSpr != null ? pillarSpr.bounds.extents.y : 2.2f;
            SpawnSpriteProp("FG_OverlapPillar", pillarSpr, litMat,
                FortressSortingLayers.LayerForeground, FortressSortingLayers.SortForeground, 2, new Vector3(-11f, 1f + pillarHalfH, 0f));

            // Instantiate authored visual prefabs at readable, non-overlapping grounded coordinates
            PlaceVisualPrefab("Animated/PF_WallTorch", new Vector3(-4.5f, 3.2f, 0f));
            PlaceVisualPrefab("Animated/PF_WallTorch", new Vector3(4.5f, 3.2f, 0f));
            PlaceVisualPrefab("Animated/PF_Brazier", new Vector3(-2.0f, 1.85f, 0f));
            PlaceVisualPrefab("Animated/PF_MagicalPortal_Cyan", new Vector3(3.8f, 2.6f, 0f));
            PlaceVisualPrefab("Interactive/PF_Door", new Vector3(-13f, 2.5f, 0f));
            PlaceVisualPrefab("Interactive/PF_PrisonGate", new Vector3(13f, 2.5f, 0f));
            PlaceVisualPrefab("Interactive/PF_Chest", new Vector3(-6.5f, 1.45f, 0f));
            PlaceVisualPrefab("Interactive/PF_Lever", new Vector3(6.5f, 1.45f, 0f));
            PlaceVisualPrefab("Hazards/PF_SpikeTrap", new Vector3(1.5f, 1.35f, 0f));
            PlaceVisualPrefab("Destructibles/PF_BreakableCrate", new Vector3(-9.0f, 1.55f, 0f));
            PlaceVisualPrefab("Architecture/PF_KnightStatue", new Vector3(8.5f, 2.65f, 0f));
            PlaceVisualPrefab("Effects/PF_Ambient_FloatingDust", new Vector3(0f, 5.5f, 0f));
            PlaceVisualPrefab("Effects/PF_Ambient_MagicalParticles", new Vector3(3.8f, 2.8f, 0f));
            PlaceVisualPrefab("Effects/PF_Ambient_RisingEmbers", new Vector3(-2.0f, 2.3f, 0f));
```

- [ ] **Step 4: Regenerate prefabs & preview screenshots and run `R12`, `R13`, `A07`, `A09_A10`, `R05`, and `R06`**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -executeMethod DeadCells.Tools.Editor.FortressProduction.FortressPrefabGenerator.GenerateAllMenu -quit -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r12-gen.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame/.fortress-workspaces/antigravity -runTests -testPlatform EditMode -testFilter "R12_AmbientParticleMaterial|R13_VisualPrefabsAndArtPreview|A07_VisualPrefabs|A09_A10_LightingAndAmbientPrefabs|R05_AmbientEffects" -testResults D:/thietkegame/.fortress-workspaces/antigravity/Logs/r12-r13-green.xml -logFile D:/thietkegame/.fortress-workspaces/antigravity/Logs/r12-r13-green.log
```
Expected output: `total="5" passed="5" failed="0"` in `Logs/r12-r13-green.xml`.

- [ ] **Step 5: Commit Task 3 in `.fortress-workspaces/antigravity`**

```powershell
rtk proxy git -C D:/thietkegame/.fortress-workspaces/antigravity add Assets/_Project/Tools/Editor/FortressProduction/FortressAnimationAndPrefabGenerator.cs Assets/_Project/Tools/Editor/FortressProduction/FortressDashboardAndPipelineRunner.cs Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs Assets/_Project/Art/Environment/FortressGenerated/Materials/Mat_FortressPixelParticle.mat Assets/_Project/Features/FortressPresentation/Prefabs/Visual/** Assets/_Project/Scenes/FortressAuthoring/GothicFortress_ArtPreview.unity
rtk proxy git -C D:/thietkegame/.fortress-workspaces/antigravity commit -m "fix(authoring): use transparent pixel particle sub-texture, fix PF_Lever/PF_SpikeTrap sprites, and ground preview props"
```

---

## Task 4: Runtime `DefeatScreen` Unscaled Time, `RunSession` Victory Outcome & `HealthBarView` Damage Trail (`G1-Code` / RC-5)

**Checkout:** `D:/thietkegame` (Root Repo)

**Files:**
- Modify: `Assets/_Project/Features/RunFlow/Scripts/DefeatScreen.cs`
- Modify: `Assets/_Project/Features/RunFlow/Scripts/RunSession.cs`
- Modify: `Assets/_Project/Features/HUD/Scripts/HealthBarView.cs`
- Create: `Assets/_Project/Core/Tests/EditMode/RunFlowAndHudTests.cs`

**Interfaces:**
```csharp
namespace DeadCells.RunFlow
{
    public sealed class RunSession : MonoBehaviour
    {
        public bool IsDefeated { get; }
        public bool IsCompleted { get; }
        public bool IsEnded => IsDefeated || IsCompleted;
        public event Action Defeated;
        public event Action Completed;
        public bool CompleteRun();
    }

    public sealed class DefeatScreen : MonoBehaviour
    {
        public float RevealDelay => _revealDelay;
        public void SetRetryAvailable(bool available);
        public void TickUnscaled(float unscaledDeltaTime);
    }
}

namespace DeadCells.HUD
{
    public sealed class HealthBarView : MonoBehaviour
    {
        public float CurrentFillFraction { get; }
        public float TrailFillFraction { get; }
        public void Render(int current, int max);
        public void TickTrail(float unscaledDeltaTime);
    }
}
```

- [ ] **Step 1: Write the failing EditMode unit tests in `Assets/_Project/Core/Tests/EditMode/RunFlowAndHudTests.cs`**

Create `Assets/_Project/Core/Tests/EditMode/RunFlowAndHudTests.cs`:

```csharp
using System.Reflection;
using DeadCells.Combat;
using DeadCells.HUD;
using DeadCells.RunFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.Core.Tests
{
    public sealed class RunFlowAndHudTests
    {
        private GameObject _root;
        private float _savedTimeScale;

        [SetUp]
        public void SetUp()
        {
            _savedTimeScale = Time.timeScale;
            _root = new GameObject("__RunFlowHudTestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _savedTimeScale;
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void DefeatScreen_WhenTimeScaleIsZero_RevealsPanelAfterUnscaledDelay()
        {
            var healthGo = new GameObject("PlayerHealth");
            healthGo.transform.SetParent(_root.transform);
            var health = healthGo.AddComponent<Health>();
            health.RestoreFull(); // Awake() does not run in EditMode; initialize Current = Max (100)

            var runGo = new GameObject("RunSession");
            runGo.transform.SetParent(_root.transform);
            var run = runGo.AddComponent<RunSession>();
            SetPrivateField(run, "_playerHealth", health);
            InvokePrivateMethod(run, "OnEnable");

            var panel = new GameObject("DefeatPanel");
            panel.transform.SetParent(_root.transform);
            panel.SetActive(false);

            var retryGo = new GameObject("RetryButton");
            retryGo.transform.SetParent(panel.transform);
            var retryButton = retryGo.AddComponent<Button>();

            var defeatGo = new GameObject("DefeatScreen");
            defeatGo.transform.SetParent(_root.transform);
            var defeatScreen = defeatGo.AddComponent<DefeatScreen>();
            SetPrivateField(defeatScreen, "_run", run);
            SetPrivateField(defeatScreen, "_panel", panel);
            SetPrivateField(defeatScreen, "_retry", retryButton);
            SetPrivateField(defeatScreen, "_revealDelay", 0.4f);
            InvokePrivateMethod(defeatScreen, "OnEnable");

            // Simulate fatal hit occurring while HitStop holds Time.timeScale == 0f
            Time.timeScale = 0f;
            health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, _root));
            Assert.IsTrue(run.IsDefeated, "Fatal damage must mark RunSession as defeated.");
            Assert.IsFalse(panel.activeSelf, "Panel must wait for reveal delay before becoming active.");

            defeatScreen.TickUnscaled(0.25f);
            Assert.IsFalse(panel.activeSelf, "Panel must still be hidden at 0.25s < 0.40s unscaled delay.");

            defeatScreen.TickUnscaled(0.20f);
            Assert.IsTrue(panel.activeSelf, "Panel must reveal once unscaled elapsed time reaches 0.40s even when Time.timeScale == 0.");

            InvokePrivateMethod(defeatScreen, "OnDisable");
            InvokePrivateMethod(run, "OnDisable");
        }

        [Test]
        public void RunSession_CompleteRun_LocksOutcomeAndPreventsSubsequentDefeat()
        {
            var health = _root.AddComponent<Health>();
            health.RestoreFull(); // Awake() does not run in EditMode; initialize Current = Max (100)

            var runGo = new GameObject("RunSession");
            runGo.transform.SetParent(_root.transform);
            var run = runGo.AddComponent<RunSession>();
            SetPrivateField(run, "_playerHealth", health);
            InvokePrivateMethod(run, "OnEnable");

            int completedCount = 0;
            int defeatedCount = 0;
            run.Completed += () => completedCount++;
            run.Defeated += () => defeatedCount++;

            Assert.IsTrue(run.CompleteRun(), "First CompleteRun() on live session must succeed.");
            Assert.IsTrue(run.IsCompleted);
            Assert.IsFalse(run.IsDefeated);
            Assert.AreEqual(1, completedCount);

            // Duplicate CompleteRun or late damage after victory must be ignored
            Assert.IsFalse(run.CompleteRun(), "Second CompleteRun() must return false.");
            health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, _root));
            Assert.IsFalse(run.IsDefeated, "Player dying after CompleteRun() must not flip session to Defeated.");
            Assert.AreEqual(0, defeatedCount);
            Assert.AreEqual(1, completedCount);

            InvokePrivateMethod(run, "OnDisable");
        }

        [Test]
        public void HealthBarView_WhenDamaged_SnapsPrimaryFillAndTrailsSecondaryFillSmoothly()
        {
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(_root.transform);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMax = Vector2.one;

            var trailGo = new GameObject("TrailFill", typeof(RectTransform));
            trailGo.transform.SetParent(_root.transform);
            var trailRect = trailGo.GetComponent<RectTransform>();
            trailRect.anchorMax = Vector2.one;

            var textGo = new GameObject("ValueText", typeof(RectTransform));
            textGo.transform.SetParent(_root.transform);
            var valueText = textGo.AddComponent<Text>();

            var view = _root.AddComponent<HealthBarView>();
            SetPrivateField(view, "_fill", fillRect);
            SetPrivateField(view, "_trailFill", trailRect);
            SetPrivateField(view, "_value", valueText);
            SetPrivateField(view, "_trailHoldDuration", 0.15f);
            SetPrivateField(view, "_trailShrinkSpeed", 2.0f);

            view.Render(100, 100);
            Assert.AreEqual(1.0f, fillRect.anchorMax.x, 0.001f);
            Assert.AreEqual(1.0f, trailRect.anchorMax.x, 0.001f);

            // Take damage from 100 -> 40
            view.Render(40, 100);
            Assert.AreEqual(0.40f, fillRect.anchorMax.x, 0.001f, "Primary fill must snap immediately to 0.40.");
            Assert.AreEqual(1.00f, trailRect.anchorMax.x, 0.001f, "Damage trail fill must hold at 1.00 initially.");
            Assert.AreEqual("40 / 100", valueText.text);

            // During hold window (0.10s < 0.15s), trail remains at 1.00
            view.TickTrail(0.10f);
            Assert.AreEqual(1.00f, trailRect.anchorMax.x, 0.001f);

            // Past hold window (+0.10s => 0.05s of shrink at 2.0/s => 1.00 - 0.10 = 0.90)
            view.TickTrail(0.10f);
            Assert.AreEqual(0.90f, trailRect.anchorMax.x, 0.001f);

            // Healing from 40 -> 80 while trail is at 0.90 snaps primary to 0.80; healing above trail snaps both
            view.Render(95, 100);
            Assert.AreEqual(0.95f, fillRect.anchorMax.x, 0.001f);
            Assert.AreEqual(0.95f, trailRect.anchorMax.x, 0.001f, "Healing above current trail must pull trail up to match current fill.");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method '{methodName}' on {target.GetType().Name}");
            method.Invoke(target, null);
        }
    }
}
```

- [ ] **Step 2: Run `RunFlowAndHudTests` to verify Red failure**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.RunFlowAndHudTests" -testResults D:/thietkegame/Logs/g1-code-red.xml -logFile D:/thietkegame/Logs/g1-code-red.log
```
Expected failure: `DefeatScreen.TickUnscaled`, `RunSession.CompleteRun` / `IsCompleted`, and `HealthBarView.TickTrail` / `_trailFill` do not exist yet.

- [ ] **Step 3: Implement unscaled timer in `DefeatScreen.cs`, victory state in `RunSession.cs`, and damage trail in `HealthBarView.cs`**

1. Replace `Assets/_Project/Features/RunFlow/Scripts/RunSession.cs`:

```csharp
using System;
using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    public sealed class RunSession : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;

        public bool IsDefeated { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsEnded => IsDefeated || IsCompleted;

        public event Action Defeated;
        public event Action Completed;

        private void OnEnable()
        {
            if (_playerHealth != null)
                _playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
                _playerHealth.Died -= OnPlayerDied;
        }

        public bool CompleteRun()
        {
            if (IsEnded || (_playerHealth != null && !_playerHealth.IsAlive))
                return false;

            IsCompleted = true;
            Completed?.Invoke();
            return true;
        }

        private void OnPlayerDied(DamageInfo damage)
        {
            if (IsEnded)
                return;

            IsDefeated = true;
            Defeated?.Invoke();
        }
    }
}
```

2. Replace `Assets/_Project/Features/RunFlow/Scripts/DefeatScreen.cs` (uses unscaled time accumulation via `Update()` + `TickUnscaled(float)` so it works deterministically both in Play Mode even when `Time.timeScale == 0f` and in EditMode tests):

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    public sealed class DefeatScreen : MonoBehaviour
    {
        [SerializeField] private RunSession _run;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _retry;
        [SerializeField, Min(0f)] private float _revealDelay = 0.4f;

        private bool _pendingReveal;
        private float _revealElapsedUnscaled;

        public event Action RetryRequested;

        private void OnEnable()
        {
            if (_run != null)
                _run.Defeated += ShowAfterDeath;
            if (_retry != null)
                _retry.onClick.AddListener(RequestRetry);
            if (_run != null && _run.IsDefeated)
                ShowAfterDeath();
        }

        private void OnDisable()
        {
            if (_run != null)
                _run.Defeated -= ShowAfterDeath;
            if (_retry != null)
                _retry.onClick.RemoveListener(RequestRetry);
            _pendingReveal = false;
            _revealElapsedUnscaled = 0f;
        }

        private void Update()
        {
            if (_pendingReveal)
                TickUnscaled(Time.unscaledDeltaTime);
        }

        public void TickUnscaled(float unscaledDeltaTime)
        {
            if (!_pendingReveal)
                return;

            _revealElapsedUnscaled += Mathf.Max(0f, unscaledDeltaTime);
            if (_revealElapsedUnscaled < _revealDelay)
                return;

            _pendingReveal = false;
            if (_panel != null)
                _panel.SetActive(true);
            if (_retry != null && _retry.interactable)
                _retry.Select();
        }

        public void SetRetryAvailable(bool available)
        {
            if (_retry == null)
                return;

            _retry.interactable = available;
            if (available && _panel != null && _panel.activeInHierarchy)
                _retry.Select();
        }

        private void ShowAfterDeath()
        {
            if (_panel != null && _panel.activeSelf)
                return;

            _pendingReveal = true;
            _revealElapsedUnscaled = 0f;
            if (_revealDelay <= 0f)
                TickUnscaled(0f);
        }

        private void RequestRetry() => RetryRequested?.Invoke();
    }
}
```

3. Replace `Assets/_Project/Features/HUD/Scripts/HealthBarView.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    [DisallowMultipleComponent]
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private RectTransform _trailFill;
        [SerializeField] private Text _value;
        [SerializeField, Min(0f)] private float _trailHoldDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float _trailShrinkSpeed = 1.5f;

        private float _currentFraction = 1f;
        private float _trailFraction = 1f;
        private float _holdTimer;
        private bool _initialized;

        public float CurrentFillFraction => _currentFraction;
        public float TrailFillFraction => _trailFraction;

        public void Render(int current, int max)
        {
            float fraction = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (!_initialized)
            {
                _initialized = true;
                _currentFraction = fraction;
                _trailFraction = fraction;
                _holdTimer = 0f;
            }
            else if (fraction < _currentFraction)
            {
                _currentFraction = fraction;
                _holdTimer = _trailHoldDuration;
            }
            else
            {
                _currentFraction = fraction;
                if (_currentFraction >= _trailFraction)
                {
                    _trailFraction = _currentFraction;
                    _holdTimer = 0f;
                }
            }

            ApplyAnchor(_fill, _currentFraction);
            ApplyAnchor(_trailFill, _trailFraction);
            if (_value != null)
                _value.text = $"{current} / {max}";
        }

        private void Update() => TickTrail(Time.unscaledDeltaTime);

        public void TickTrail(float unscaledDeltaTime)
        {
            if (!_initialized || _trailFraction <= _currentFraction)
                return;

            float dt = Mathf.Max(0f, unscaledDeltaTime);
            if (_holdTimer > 0f)
            {
                float consumed = Mathf.Min(_holdTimer, dt);
                _holdTimer -= consumed;
                dt -= consumed;
            }

            if (dt > 0f)
            {
                _trailFraction = Mathf.MoveTowards(_trailFraction, _currentFraction, _trailShrinkSpeed * dt);
                ApplyAnchor(_trailFill, _trailFraction);
            }
        }

        private static void ApplyAnchor(RectTransform target, float fraction)
        {
            if (target == null)
                return;
            Vector2 end = target.anchorMax;
            end.x = fraction;
            target.anchorMax = end;
        }
    }
}
```

- [ ] **Step 4: Run `RunFlowAndHudTests` to verify Green**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.RunFlowAndHudTests" -testResults D:/thietkegame/Logs/g1-code-green.xml -logFile D:/thietkegame/Logs/g1-code-green.log
```
Expected output: `total="3" passed="3" failed="0"` in `Logs/g1-code-green.xml`.

- [ ] **Step 5: Commit Task 4**

```powershell
rtk git add Assets/_Project/Features/RunFlow/Scripts/DefeatScreen.cs Assets/_Project/Features/RunFlow/Scripts/RunSession.cs Assets/_Project/Features/HUD/Scripts/HealthBarView.cs Assets/_Project/Core/Tests/EditMode/RunFlowAndHudTests.cs
rtk git commit -m "fix(runflow,hud): use unscaled reveal delay on DefeatScreen, add RunSession completion, and add HealthBarView damage trail"
```

---

## Task 5: `Main_Level.unity` Visual, Physics, UI & Encounter Remediation (`G1-Scene` / RC-6)

**Checkout:** `D:/thietkegame` (Root Repo)

**Files:**
- Create: `Assets/_Project/Features/RunFlow/Scripts/LevelExitTrigger.cs`
- Create: `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`
- Modify: `Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef`
- Modify: `Assets/_Project/Scenes/Main_Level.unity`
- Create: `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs`

**Interfaces:**
```csharp
namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LevelExitTrigger : MonoBehaviour
    {
        public RunSession Session { get; }
        public void Configure(RunSession session);
    }
}

namespace DeadCells.Tools.Editor
{
    public static class MainLevelSceneRemediator
    {
        [MenuItem("Tools/DeadCells/Scenes/Remediate Main_Level")]
        public static void RemediateMainLevel();
    }
}
```

- [ ] **Step 1: Write the failing EditMode scene integrity tests in `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs`**

Create `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs`:

```csharp
using System;
using System.Linq;
using System.Reflection;
using DeadCells.Currency;
using DeadCells.Enemies;
using DeadCells.Player;
using DeadCells.RunFlow;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadCells.Core.Tests
{
    public sealed class MainLevelIntegrityTests
    {
        private const string MainLevelPath = "Assets/_Project/Scenes/Main_Level.unity";
        private string _previousScenePath;

        [SetUp]
        public void SetUp()
        {
            _previousScenePath = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(MainLevelPath, OpenSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_previousScenePath) && System.IO.File.Exists(_previousScenePath))
                EditorSceneManager.OpenScene(_previousScenePath, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void MainLevel_ScrollVats_UseSpriteLitDefaultInsteadOfPlayerPaletteSwap()
        {
            var scrollVats = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(sr => sr.gameObject.name == "Scroll vat")
                .ToArray();
            Assert.GreaterOrEqual(scrollVats.Length, 2, "Main_Level must contain the 2 Scroll vat renderers.");

            foreach (var sr in scrollVats)
            {
                Assert.IsNotNull(sr.sharedMaterial, "Scroll vat must have a valid sharedMaterial.");
                StringAssert.DoesNotContain("Player_PaletteSwap", sr.sharedMaterial.name,
                    $"Scroll vat '{sr.transform.GetSiblingIndex()}' must not use Player_PaletteSwap material.");
            }
        }

        [Test]
        public void MainLevel_ClimbableChain_ConnectsFloor15TopToOneWayPlatform16()
        {
            var chain = GameObject.Find("Climbable chain");
            Assert.IsNotNull(chain, "Climbable chain must exist in Main_Level.");
            var box = chain.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(box);

            Bounds worldBounds = box.bounds;
            Assert.LessOrEqual(worldBounds.min.y, -0.5f,
                "Climbable chain trigger bottom must reach down to Floor 15 (y=-1.0u) so a grounded player can grab it.");
            Assert.GreaterOrEqual(worldBounds.max.y, 2.0f,
                "Climbable chain trigger top must reach One Way Platform 16 (y=2.0u).");
        }

        [Test]
        public void MainLevel_ArchiveGalleryPlatforms_UseLeftMiddleAndRightPlankSpritesPerPlatform()
        {
            var archiveGallery = GameObject.Find("Archive gallery");
            Assert.IsNotNull(archiveGallery, "Archive gallery parent container must exist in Main_Level.");
            Assert.AreEqual(6, archiveGallery.transform.childCount, "Archive gallery must contain 6 child one-way platforms.");

            int totalBoards = 0;
            foreach (Transform platform in archiveGallery.transform)
            {
                var boards = platform.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(sr => sr.transform.parent == platform && sr.gameObject.name.StartsWith("Board", StringComparison.Ordinal) && sr.sprite != null)
                    .OrderBy(sr => sr.transform.localPosition.x)
                    .ToArray();
                Assert.GreaterOrEqual(boards.Length, 3, $"Platform '{platform.name}' must have at least 3 boards.");
                totalBoards += boards.Length;

                StringAssert.Contains("left", boards[0].sprite.name,
                    $"Leftmost board of '{platform.name}' must use woodenPlatform_left sprite.");
                StringAssert.Contains("right", boards[boards.Length - 1].sprite.name,
                    $"Rightmost board of '{platform.name}' must use woodenPlatform_right sprite.");
                for (int i = 1; i < boards.Length - 1; i++)
                {
                    StringAssert.Contains("middle", boards[i].sprite.name,
                        $"Interior board {i} of '{platform.name}' must use woodenPlatform_middle sprite.");
                }
            }
            Assert.AreEqual(40, totalBoards, "Archive gallery must contain 40 total boards across its 6 platforms.");
        }

        [Test]
        public void MainLevel_EastWing_ContainsEnemiesIncludingRatActiveHudInstructionAndFunctionalExitTrigger()
        {
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            Assert.IsTrue(enemies.Any(e => e.transform.position.x >= 55f),
                "East wing (x >= 55) must contain active combat encounters.");
            Assert.IsTrue(enemies.Any(e => e.gameObject.name.Contains("Enemy_Rat")),
                "Main_Level must include at least one Enemy_Rat instance.");

            var goldDrops = UnityEngine.Object.FindFirstObjectByType<EnemyGoldDrops>();
            Assert.IsNotNull(goldDrops, "EnemyGoldDrops component must exist in Main_Level.");
            var enemiesField = typeof(EnemyGoldDrops).GetField("_enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(enemiesField);
            var registeredEnemies = enemiesField.GetValue(goldDrops) as EnemyController[];
            Assert.IsNotNull(registeredEnemies);
            foreach (var enemy in enemies)
                CollectionAssert.Contains(registeredEnemies, enemy, $"Enemy '{enemy.name}' must be registered in EnemyGoldDrops._enemies.");

            var scrolls = UnityEngine.Object.FindObjectsByType<PowerScrollPickup>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(scrolls.Length, 2, "Main_Level must contain both PowerScrollPickup instances.");
            float firstEnemyX = enemies.Min(e => e.transform.position.x);
            Assert.Greater(scrolls.Min(s => s.transform.position.x), firstEnemyX,
                "All Power Scrolls must be positioned after the first enemy encounter (not given for free at x=13.5).");

            var entranceScroll = GameObject.Find("Scroll of Power - Entrance");
            Assert.IsNotNull(entranceScroll);
            Assert.AreEqual(-1.0f, entranceScroll.transform.position.y, 0.05f,
                "Scroll of Power - Entrance at x=25.5 must sit grounded on Floor 15 - Zombie hall (y=-1.0).");

            var scrollHud = GameObject.Find("Power Scroll HUD");
            Assert.IsNotNull(scrollHud, "Power Scroll HUD must exist in Main_Level.");
            var instruction = scrollHud.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Instruction");
            Assert.IsNotNull(instruction, "Instruction child must exist under Power Scroll HUD.");
            Assert.IsTrue(instruction.gameObject.activeSelf, "Instruction under Power Scroll HUD/Dark overlay/Upgrade panel must have activeSelf == true.");

            var exitTrigger = UnityEngine.Object.FindFirstObjectByType<LevelExitTrigger>();
            Assert.IsNotNull(exitTrigger, "Main_Level Exit Door must have a LevelExitTrigger component.");
            Assert.IsNotNull(exitTrigger.Session, "LevelExitTrigger must be wired to RunSession.");
            var exitCol = exitTrigger.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(exitCol);
            Assert.IsTrue(exitCol.isTrigger, "LevelExitTrigger collider must be a trigger.");
        }
    }
}
```

- [ ] **Step 2: Run `MainLevelIntegrityTests` to verify Red failure**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests" -testResults D:/thietkegame/Logs/g1-scene-red.xml -logFile D:/thietkegame/Logs/g1-scene-red.log
```
Expected failure: `LevelExitTrigger` does not exist yet, and once added, all 4 tests fail against the unpatched `Main_Level.unity` (`Scroll vat` uses `Player_PaletteSwap`, `Climbable chain` `min.y` is `0.00 > -0.50`, `Archive gallery` has 0 middle/right sprites, and East Wing has 0 enemies / no `LevelExitTrigger`).

- [ ] **Step 3: Implement `LevelExitTrigger.cs` and `MainLevelSceneRemediator.cs`, and run the remediator on `Main_Level.unity`**

1. Create `Assets/_Project/Features/RunFlow/Scripts/LevelExitTrigger.cs`:

```csharp
using UnityEngine;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LevelExitTrigger : MonoBehaviour
    {
        [SerializeField] private RunSession _session;

        public RunSession Session => _session;

        public void Configure(RunSession session) => _session = session;

        private void Reset()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
                box.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_session == null || _session.IsEnded)
                return;

            // Player layer is index 7
            if (other.gameObject.layer == 7 || other.CompareTag("Player"))
                _session.CompleteRun();
        }
    }
}
```

2. Update `Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef` to reference `Core`, `Combat`, `Currency`, `Enemies`, `HUD`, `Player`, `RunFlow`, and `VFX` so the Editor scene remediator can wire scene components cleanly:

```json
{
    "name": "TilesetTools.Editor",
    "rootNamespace": "DeadCells.Tools.Editor",
    "references": [
        "Core",
        "Combat",
        "Currency",
        "Enemies",
        "HUD",
        "Player",
        "RunFlow",
        "VFX",
        "Unity.2D.Sprite.Editor",
        "Unity.2D.Tilemap.Editor"
    ],
    "includePlatforms": ["Editor"],
    "autoReferenced": true
}
```

3. Create `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`:

```csharp
using System;
using System.Linq;
using DeadCells.Currency;
using DeadCells.Enemies;
using DeadCells.Player;
using DeadCells.RunFlow;
using DeadCells.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadCells.Tools.Editor
{
    public static class MainLevelSceneRemediator
    {
        private const string MainLevelPath = "Assets/_Project/Scenes/Main_Level.unity";
        private const string SpriteLitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string PlankLeftPath = "Assets/_Project/Art/Environment/Prison/Sprites/woodenPlatform_left_00.png";
        private const string PlankMid1Path = "Assets/_Project/Art/Environment/Prison/Sprites/woodenPlatform_middle1_00.png";
        private const string PlankMid2Path = "Assets/_Project/Art/Environment/Prison/Sprites/woodenPlatform_middle2_00.png";
        private const string PlankRightPath = "Assets/_Project/Art/Environment/Prison/Sprites/woodenPlatform_right_00.png";

        [MenuItem("Tools/DeadCells/Scenes/Remediate Main_Level")]
        public static void RemediateMainLevel()
        {
            var scene = EditorSceneManager.OpenScene(MainLevelPath, OpenSceneMode.Single);
            var litMat = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMatPath);

            // 1. Fix Scroll vat material from Player_PaletteSwap to Sprite-Lit-Default
            foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr.gameObject.name == "Scroll vat" && litMat != null)
                {
                    sr.sharedMaterial = litMat;
                    EditorUtility.SetDirty(sr);
                }
            }

            // 2. Fix Climbable chain at x=18.5 to connect Floor 15 (y=-1.0) to One Way Platform 16 (y=2.0)
            var chain = GameObject.Find("Climbable chain");
            if (chain != null)
            {
                chain.transform.position = new Vector3(18.5f, 0.5f, 0f);
                var box = chain.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.offset = Vector2.zero;
                    box.size = new Vector2(0.75f, 3.2f); // spans y = [-1.1 .. +2.1]
                    EditorUtility.SetDirty(box);
                }
                EditorUtility.SetDirty(chain);
            }

            // 3. Fix Archive gallery's 6 child one-way platforms (40 Board children total) to use left/middle1/middle2/right 3-slice plank sprites per platform
            var archiveGallery = GameObject.Find("Archive gallery");
            if (archiveGallery != null)
            {
                Sprite sLeft = AssetDatabase.LoadAssetAtPath<Sprite>(PlankLeftPath);
                Sprite sMid1 = AssetDatabase.LoadAssetAtPath<Sprite>(PlankMid1Path);
                Sprite sMid2 = AssetDatabase.LoadAssetAtPath<Sprite>(PlankMid2Path);
                Sprite sRight = AssetDatabase.LoadAssetAtPath<Sprite>(PlankRightPath);

                foreach (Transform platform in archiveGallery.transform)
                {
                    var boards = platform.GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(sr => sr.transform.parent == platform && sr.gameObject.name.StartsWith("Board", StringComparison.Ordinal))
                        .OrderBy(sr => sr.transform.localPosition.x)
                        .ToArray();

                    for (int i = 0; i < boards.Length; i++)
                    {
                        if (i == 0 && sLeft != null)
                            boards[i].sprite = sLeft;
                        else if (i == boards.Length - 1 && sRight != null)
                            boards[i].sprite = sRight;
                        else if (sMid1 != null && sMid2 != null)
                            boards[i].sprite = (i % 2 == 0) ? sMid1 : sMid2;
                        EditorUtility.SetDirty(boards[i]);
                    }
                }
            }

            // 4. Enable Power Scroll HUD/Dark overlay/Upgrade panel/Instruction child so players see upgrade instructions when menu opens
            foreach (var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tr.name == "Instruction" && tr.parent != null &&
                    (tr.parent.name == "Upgrade panel" || tr.root.name.Contains("Scroll", StringComparison.Ordinal)))
                {
                    tr.gameObject.SetActive(true);
                    EditorUtility.SetDirty(tr.gameObject);
                }
            }

            // 5. Move Scroll of Power - Entrance from (13.5, 0) to (25.5, -1.0) grounded on Floor 15 - Zombie hall after the x=21 Zombie
            var entranceScroll = GameObject.Find("Scroll of Power - Entrance");
            if (entranceScroll != null && entranceScroll.transform.position.x < 20f)
            {
                entranceScroll.transform.position = new Vector3(25.5f, -1.0f, 0f);
                EditorUtility.SetDirty(entranceScroll.transform);
            }

            // 6. Populate East Wing encounters under "Encounters" (including Enemy_Rat) at exact floor elevations & register in EnemyGoldDrops._enemies
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var projectiles = UnityEngine.Object.FindFirstObjectByType<ProjectileSpawner>();
            var vfx = UnityEngine.Object.FindFirstObjectByType<VfxSpawner>();
            var encountersRoot = GameObject.Find("Encounters")?.transform;

            // Lower cell block floor spans x=[68, 94], top y=-1.0
            SpawnEnemyIfMissing("Assets/_Project/Features/Enemies/Prefabs/Enemy_Rat.prefab", "Enemy_Rat - Lower Cell Block",
                new Vector3(74f, -0.95f, 0f), encountersRoot, player != null ? player.transform : null, projectiles, vfx);
            // Archive overlook spans x=[75, 83], top y=5.25
            SpawnEnemyIfMissing("Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab", "Enemy_Archer - Archive Gallery",
                new Vector3(78f, 5.30f, 0f), encountersRoot, player != null ? player.transform : null, projectiles, vfx);
            // Exit corridor floor spans x=[94, 112], top y=0.0
            SpawnEnemyIfMissing("Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab", "Enemy_Shield - Exit Guard",
                new Vector3(96f, 0.05f, 0f), encountersRoot, player != null ? player.transform : null, projectiles, vfx);

            var goldDrops = UnityEngine.Object.FindFirstObjectByType<EnemyGoldDrops>();
            if (goldDrops != null)
            {
                var allEnemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                    .OrderBy(e => e.transform.position.x)
                    .ToArray();
                var soGold = new SerializedObject(goldDrops);
                var enemiesProp = soGold.FindProperty("_enemies");
                if (enemiesProp != null)
                {
                    enemiesProp.arraySize = allEnemies.Length;
                    for (int i = 0; i < allEnemies.Length; i++)
                        enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = allEnemies[i];
                    soGold.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(goldDrops);
                }
            }

            // 7. Attach LevelExitTrigger to Exit Door at (108, 0) and bind RunSession
            var runSession = UnityEngine.Object.FindFirstObjectByType<RunSession>();
            var exitDoor = GameObject.Find("Exit Door (visual only)") ?? GameObject.Find("Exit Door");
            if (exitDoor != null && runSession != null)
            {
                exitDoor.name = "Exit Door";
                var box = exitDoor.GetComponent<BoxCollider2D>() ?? exitDoor.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(2.0f, 3.5f);
                var trigger = exitDoor.GetComponent<LevelExitTrigger>() ?? exitDoor.AddComponent<LevelExitTrigger>();
                trigger.Configure(runSession);
                EditorUtility.SetDirty(box);
                EditorUtility.SetDirty(trigger);
                EditorUtility.SetDirty(exitDoor);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SpawnEnemyIfMissing(
            string prefabPath,
            string instanceName,
            Vector3 position,
            Transform parent,
            Transform playerTarget,
            ProjectileSpawner projectiles,
            VfxSpawner vfx)
        {
            if (GameObject.Find(instanceName) != null)
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            var inst = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (inst == null)
                return;

            inst.name = instanceName;
            inst.transform.position = position;
            var ctrl = inst.GetComponent<EnemyController>();
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                if (playerTarget != null) so.FindProperty("_player").objectReferenceValue = playerTarget;
                if (projectiles != null) so.FindProperty("_projectiles").objectReferenceValue = projectiles;
                if (vfx != null) so.FindProperty("_vfx").objectReferenceValue = vfx;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ctrl);
            }
            EditorUtility.SetDirty(inst);
        }
    }
}
```

- [ ] **Step 4: Execute `MainLevelSceneRemediator.RemediateMainLevel` and run `MainLevelIntegrityTests` to verify Green**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.RemediateMainLevel -quit -logFile D:/thietkegame/Logs/g1-scene-remediate.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests" -testResults D:/thietkegame/Logs/g1-scene-green.xml -logFile D:/thietkegame/Logs/g1-scene-green.log
```
Expected output: `total="4" passed="4" failed="0"` in `Logs/g1-scene-green.xml`.

- [ ] **Step 5: Commit Task 5**

```powershell
rtk git add Assets/_Project/Features/RunFlow/Scripts/LevelExitTrigger.cs Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs Assets/_Project/Scenes/Main_Level.unity Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs
rtk git commit -m "fix(main-level): fix scroll vat material, chain bounds, archive gallery planks, east wing encounters, and exit trigger"
```

---

## Task 6: `Nhà Giam Giếng Chuông` v7 Room E Arrow vs. One-Way Platform Physics Contract (`NG-Arrows` / RC-7)

**Checkout:** `D:/thietkegame` (Root Repo)

**Files:**
- Modify: `Assets/_Project/Features/Enemies/Scripts/Projectile.cs`
- Modify: `Assets/_Project/Features/Enemies/Scripts/ProjectileSpawner.cs`
- Create: `Assets/_Project/Core/Tests/EditMode/ProjectileOneWayPlatformTests.cs`

**Interfaces:**
```csharp
namespace DeadCells.Enemies
{
    public class Projectile : MonoBehaviour
    {
        public bool IsLive => _live;
        public void Setup(Action<Projectile> release, VfxSpawner vfx, LayerMask targets, LayerMask ground);
        public void Setup(Action<Projectile> release, VfxSpawner vfx, LayerMask targets, LayerMask ground, LayerMask straightObstacleMask);
    }
}
```

- [ ] **Step 1: Write the failing EditMode unit test in `Assets/_Project/Core/Tests/EditMode/ProjectileOneWayPlatformTests.cs`**

Create `Assets/_Project/Core/Tests/EditMode/ProjectileOneWayPlatformTests.cs` (maps directly to `NhaGiamGiengChuong.md:154` Room `E` rule: *"Platform giữa (`x = -4 .. 2, y = 26`, one-way) chỉ để đổi cao độ, không làm vật chắn tên"*):

```csharp
using System.Reflection;
using DeadCells.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class ProjectileOneWayPlatformTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp() => _root = new GameObject("__ProjectileOneWayTestRoot");

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void StraightArrow_WhenEnteringOneWayPlatformTrigger_DoesNotDespawn_ButDespawnsOnSolidGround()
        {
            int groundLayer = 6;         // Ground
            int playerLayer = 7;         // Player
            int oneWayPlatformLayer = 9; // OneWayPlatform
            LayerMask targetMask = 1 << playerLayer;
            LayerMask combinedGroundMask = (1 << groundLayer) | (1 << oneWayPlatformLayer); // 576 in Main_Level / Sandbox_Player

            var arrowGo = new GameObject("ArrowProjectile");
            arrowGo.transform.SetParent(_root.transform);
            var rb = arrowGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var arrowCol = arrowGo.AddComponent<BoxCollider2D>();
            arrowCol.isTrigger = true;
            var sr = arrowGo.AddComponent<SpriteRenderer>();
            var projectile = arrowGo.AddComponent<Projectile>();
            SetPrivateField(projectile, "_mode", Projectile.Mode.Straight);
            SetPrivateField(projectile, "_renderer", sr);

            bool released = false;
            projectile.Setup(_ => released = true, vfx: null, targets: targetMask, ground: combinedGroundMask);
            projectile.LaunchStraight(Vector2.zero, new Vector2(12f, 0f), damage: 15, knockback: 2f, owner: _root);
            Assert.IsTrue(projectile.IsLive);

            // 1. Passing across Room E center OneWayPlatform (layer 9) MUST NOT destroy Straight arrow
            var oneWayGo = new GameObject("RoomE_CenterOneWayPlatform");
            oneWayGo.transform.SetParent(_root.transform);
            oneWayGo.layer = oneWayPlatformLayer;
            var oneWayCol = oneWayGo.AddComponent<BoxCollider2D>();

            InvokeOnTriggerEnter2D(projectile, oneWayCol);
            Assert.IsFalse(released, "Horizontal Straight arrow must pass through OneWayPlatform (layer 9) without despawning.");
            Assert.IsTrue(projectile.IsLive, "Straight arrow must remain live after intersecting OneWayPlatform.");

            // 2. Hitting solid Ground wall (layer 6) MUST destroy Straight arrow
            var solidWallGo = new GameObject("SolidStoneWall");
            solidWallGo.transform.SetParent(_root.transform);
            solidWallGo.layer = groundLayer;
            var solidWallCol = solidWallGo.AddComponent<BoxCollider2D>();

            InvokeOnTriggerEnter2D(projectile, solidWallCol);
            Assert.IsTrue(released, "Straight arrow hitting solid Ground (layer 6) must despawn and return to pool.");
            Assert.IsFalse(projectile.IsLive);
        }

        private static void InvokeOnTriggerEnter2D(Projectile projectile, Collider2D other)
        {
            var method = typeof(Projectile).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(projectile, new object[] { other });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }
    }
}
```

- [ ] **Step 2: Run `ProjectileOneWayPlatformTests` to verify Red failure**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.ProjectileOneWayPlatformTests" -testResults D:/thietkegame/Logs/ng-arrow-red.xml -logFile D:/thietkegame/Logs/ng-arrow-red.log
```
Expected failure: `Horizontal Straight arrow must pass through OneWayPlatform (layer 9) without despawning` fails because `Projectile.OnTriggerEnter2D` checks `(_ground.value & layerBit) != 0` where `_ground` includes `OneWayPlatform` (`layer 9`).

- [ ] **Step 3: Implement `_straightObstacleMask` filtering in `Projectile.cs` and `ProjectileSpawner.cs`**

1. In `Assets/_Project/Features/Enemies/Scripts/Projectile.cs`:
   - Add `private LayerMask _straightObstacles;` and `public bool IsLive => _live;`.
   - In `Setup(Action<Projectile> release, VfxSpawner vfx, LayerMask targets, LayerMask ground)`: strip `OneWayPlatform` (`layer 9`) from the default straight-arrow obstacle mask (`int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform"); int oneWayBit = oneWayLayer >= 0 ? (1 << oneWayLayer) : (1 << 9); LayerMask defaultStraightMask = ground.value & ~oneWayBit;`) and delegate to `Setup(release, vfx, targets, ground, defaultStraightMask)`.
   - Keep `_ground` (which includes `OneWayPlatform`) for `Mode.Lob` grenades in `OnCollisionEnter2D` (`line 117`), and use `_straightObstacles` in `OnTriggerEnter2D` (`line 139`) for `Mode.Straight` arrows!

Exact changes in `Assets/_Project/Features/Enemies/Scripts/Projectile.cs`:

```csharp
        private LayerMask _targets;
        private LayerMask _ground;
        private LayerMask _straightObstacles;
        ...
        public Mode Kind => _mode;
        public bool IsLive => _live;

        public void Setup(Action<Projectile> release, VfxSpawner vfx, LayerMask targets, LayerMask ground)
        {
            int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
            int oneWayBit = oneWayLayer >= 0 ? (1 << oneWayLayer) : (1 << 9);
            LayerMask straightObstacles = ground.value & ~oneWayBit;
            Setup(release, vfx, targets, ground, straightObstacles);
        }

        public void Setup(Action<Projectile> release, VfxSpawner vfx, LayerMask targets, LayerMask ground, LayerMask straightObstacles)
        {
            _body = GetComponent<Rigidbody2D>();
            _release = release;
            _vfx = vfx;
            _targets = targets;
            _ground = ground;
            _straightObstacles = straightObstacles;
            _query = new HitQuery(targets);
            _baseColor = _renderer != null ? _renderer.color : Color.white;
        }
```

And in `OnTriggerEnter2D` (`lines 127-143`):

```csharp
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_live || _mode != Mode.Straight)
                return;

            int layerBit = 1 << other.gameObject.layer;
            if ((_targets.value & layerBit) != 0 && other.TryGetComponent<Hurtbox>(out var hurtbox) && hurtbox.Target != null)
            {
                int dir = _body.linearVelocity.x >= 0f ? 1 : -1;
                hurtbox.Target.TakeDamage(new DamageInfo(_damage, dir, _knockback, 0f, false, _owner, isProjectile: true));
                Finish();
            }
            else if ((_straightObstacles.value & layerBit) != 0)
            {
                Finish();
            }
        }
```

- [ ] **Step 4: Run `ProjectileOneWayPlatformTests` and the full Root EditMode test suite to verify Green**

Run via `rtk`:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests" -testResults D:/thietkegame/Logs/all-root-green.xml -logFile D:/thietkegame/Logs/all-root-green.log
```
Expected output: `total="10" passed="10" failed="0"` across `GothicDungeonImporterIncrementalTests` (2), `RunFlowAndHudTests` (3), `MainLevelIntegrityTests` (4), and `ProjectileOneWayPlatformTests` (1).

- [ ] **Step 5: Commit Task 6**

```powershell
rtk git add Assets/_Project/Features/Enemies/Scripts/Projectile.cs Assets/_Project/Core/Tests/EditMode/ProjectileOneWayPlatformTests.cs
rtk git commit -m "fix(enemies): allow straight arrows to pass across OneWayPlatform while keeping lob grenade bounce"
```

---

## Parallel Agent Dispatch Tickets (Ready to Hand Off)

These four file-disjoint agent tickets align directly with `docs/Fortress/Prompts-Sau-Merge-2026-11-19.md` and can be dispatched in parallel without merge conflicts:

| Ticket ID | Target Workspace & Branch | Assigned Tasks | Owned Files (Strict Allowlist) | Verification Gate |
|---|---|---|---|---|
| **Agent-1 (`G3`)** | `D:/thietkegame` (`local/unity-sync-2026-11-19` or branch `fix/gothic-importer-palettes`) | **Task 1** | `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`, `Assets/_Project/Core/Tests/EditMode/Core.Tests.EditMode.asmdef`, `Assets/_Project/Core/Tests/EditMode/GothicDungeonImporterIncrementalTests.cs` | `GothicDungeonImporterIncrementalTests` (`2/2 Passed`) + `GothicDungeonImporter.Verify()` (`sprites=675 tiles=612 palettes=4`) |
| **Agent-2 (`A1-Authoring`)** | `D:/thietkegame/.fortress-workspaces/antigravity` (`fortress/antigravity-clean`) | **Task 2 & Task 3** | `Assets/_Project/Features/FortressPresentation/Scripts/FortressRoomDescriptor.cs`, `Assets/_Project/Tools/Editor/FortressProduction/{FortressRuleTileAndPaletteGenerator,FortressRoomAndCollisionTools,FortressAnimationAndPrefabGenerator,FortressDashboardAndPipelineRunner}.cs`, `Assets/_Project/Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`, regenerated `FortressGenerated/**` & `GothicFortress_ArtPreview.unity` | `23/23` EditMode tests (`A02`–`A13`, `R01`–`R13`) Passed + `COLLISION_VALIDATOR errors=0` + `0` `.meta` edits |
| **Agent-3 (`G1-Runtime-HUD`)** | `D:/thietkegame` (`local/unity-sync-2026-11-19`) | **Task 4 & Task 6** | `Assets/_Project/Features/RunFlow/Scripts/{DefeatScreen,RunSession}.cs`, `Assets/_Project/Features/HUD/Scripts/HealthBarView.cs`, `Assets/_Project/Features/Enemies/Scripts/Projectile.cs`, `Assets/_Project/Core/Tests/EditMode/{RunFlowAndHudTests,ProjectileOneWayPlatformTests}.cs` | `RunFlowAndHudTests` (`3/3 Passed`) + `ProjectileOneWayPlatformTests` (`1/1 Passed`) |
| **Agent-4 (`G1-Scene-MainLevel`)** | `D:/thietkegame` (`local/unity-sync-2026-11-19`, runs after Agent-3 lands `RunSession.CompleteRun()`) | **Task 5** | `Assets/_Project/Features/RunFlow/Scripts/LevelExitTrigger.cs`, `Assets/_Project/Tools/Editor/{TilesetTools.Editor.asmdef,MainLevelSceneRemediator.cs}`, `Assets/_Project/Scenes/Main_Level.unity`, `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs` | `MainLevelIntegrityTests` (`4/4 Passed`) |
