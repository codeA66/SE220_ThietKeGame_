# Round 7 (Phase 2): NhaGiamGiengChuong P1 Architectural Rhythm, Carved Parallax Bays, Storytelling Clusters & Chiaroscuro Lighting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remediate all five `P1` architectural rhythm, empty-wall, storytelling, and chiaroscuro lighting defects (`P1-01` through `P1-05`) across the 4 expansion wings (`W1_EastCourtyard`, `W2_SewerCistern`, `W3_WestBastion`, `U1_UpperPromenade`) and scene lighting of `NhaGiamGiengChuong.unity` while preserving 100% pass rate across all 76 existing EditMode and PlayMode tests.

**Architecture:**
1. Extend `PrisonPropSpriteGenerator.SanitizeChromakeyPrisonSprites()` to scrub the 113 residual `#FF00FF` magenta pixels from `alchemyBibli_00.png` (used in Room S) and lock it in `VisualShaderAndNormalTests.PrisonChromakeyProps_HaveZeroRawMagentaPixels()`.
2. Expand `NhaGiamGiengChuongBuilder.cs` across `BuildRoomW1_EastCourtyard`, `BuildRoomW2_SewerCistern`, `BuildRoomW3_WestBastion`, `BuildRoomU1_UpperPromenade`, `BuildScene`, `AddStandingTorchBeacon`, `AddCeilingLanternBeacon`, and `BuildWallLanternBeacon`:
   - Carve 7 new parallax arch bays (`Bay #9`..`Bay #15`, bringing total carved bays from 9 to 16) so every long wing room (`M, N, O, P, Q, R, S, T`) opens onto multi-layer parallax depth.
   - Populate all 8 expansion wing rooms (`M..T`) with gothic windows + additive god-rays (`AddWindowWithGodray`), stone buttress piers (`pal.SpawnerPillar`, `pal.ArcheSmall`), hanging cages (`pal.SuspendedCage`), swaying wall banners (`pal.BannerBig`), cell bunks (`pal.WallBed`), potion shelves (`AddShelfWithFioles`), floor debris clusters (`AddFloorJunkCluster`), and platform corner braces (`AddPlatformCornerBrace`).
   - Replace the flat `Mathf.Max(intensity, 1.40f)` streetlight clamp with Dead Cells chiaroscuro modulation (`globalLight.intensity = 0.44f`, `color = (0.56f, 0.70f, 0.84f)`, `falloffIntensity = 0.62f`, `volumeIntensity = 0.065f`, alternating focal `1.34f` and secondary fill `0.88f` wing beacons) while keeping all 88 `LanternFixture_R5` fixtures and all `pointLightOuterRadius` values intact (`A10` coverage `96.5% >= 75%`).
3. Lock all `P1-01`..`P1-05` invariants with EditMode test `A15_Round7Phase2_SixteenArchBaysWingWindowsStorytellingClustersAndChiaroscuroLighting` in `MapWiringTests.cs`.

**Tech Stack:** Unity `6000.6.9f1` (URP 2D Renderer, `Tilemap`, `Light2D`), C# Editor Builders (`PrisonPropSpriteGenerator`, `NhaGiamGiengChuongBuilder`), NUnit EditMode & PlayMode Test Runner, `rtk` CLI wrapper.

**Spec:** `D:/thietkegame/docs/superpowers/specs/2026-11-22-round7-phase2-p1-architectural-rhythm-and-chiaroscuro-design.md`

---

## Global Constraints

- **Shell Wrapper (`AGENTS.md` §0):** Run all shell commands via `rtk` or `rtk proxy <command>`. Read/write files using UTF-8.
- **Low-RAM Batchmode Execution:** Always pass `-job-worker-count 1 -disable-assembly-updater` to `D:/Unity/6000.6.9f1/Editor/Unity.exe`, kill lingering `Unity*`, `bee_backend*`, `AssetImportWorker*` before each launch, and run EditMode tests in the 4 assembly/class batches (`Core.Tests.EditMode`, `NhaGiamGiengChuong` Unit+Geometry, `MapWiringTests` `A07..A10`, `MapWiringTests` `A11..A15`) so Unity never reloads `NhaGiamGiengChuong.unity` > 5 times in a single process.
- **No `.meta` Fabrication (`AGENTS.md` §2):** Never manually create, edit, or delete `.meta` files.
- **100% Compatibility with Existing 76 Tests:** Do not alter any physical `BoxCollider2D` / `PolygonCollider2D`, `EncounterZone`, `TP_0..5`, `SD_1..3`, `I1..I16`, `A08` grounded prop floorY, `A10` `pointLightOuterRadius`, `A11` `_PinTopWorldY`, or `PrisonNormalMapGenerator.CoreArchitectureSpriteNames.Length` (`36`).

## Review Focus

- Verify `CarveAndFrameArchBay` bays `9..15` (`[97..102, 2..6]`, `[138..144, 18..22]`, `[140..148, -9..-3]`, `[187..195, -9..-3]`, `[-75..-69, 18..22]`, `[47..55, 31..37]`, `[96..104, 47..55]`) have zero AABB overlap with all 41 `landmarkNames` in `A12` (`MapWiringTests.cs:879-912`).
- Verify `mFloorXs[1] = 96.2f` and `qFloorXs[1] = -76.2f` mount on solid brick piers outside `Bay #9` (`97..102`) and `Bay #13` (`-75..-69`).
- Verify every new `Art_*_Window*` GameObject (matched by `n.Contains("Window")` in `A14` `P0-04b`) has zero AABB overlap with any `LanternFixture_R5`.
- Verify `AddStandingTorchBeacon` and `AddCeilingLanternBeacon` clamp `intensity` via `Mathf.Clamp(intensity, 0.75f, 1.45f)` so modulated `0.88f` and `1.34f` intensities are preserved (`A12` requires `intensity <= 1.50f` and `volumeIntensity <= 0.10f`).
- Verify `AddPlatformCornerBrace` creates `Art_*_BraceL/R` siblings outside `Art_*_Planks` containers so `A10`'s `woodenPlatform_*` assertion on `Art_*_Planks` children remains untouched.

## File Structure Map

| Action | File Path | Responsibility |
| --- | --- | --- |
| Modify | `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs` | Add `("alchemyBibli_00.png", new Color32(14, 20, 28, 255))` to `SanitizeChromakeyPrisonSprites()`. |
| Modify | `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs` | Add `"Assets/_Project/Art/Environment/Prison/Sprites/alchemyBibli_00.png"` to `PrisonChromakeyProps_HaveZeroRawMagentaPixels()`. |
| Modify | `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` | Add `[Test] A15_Round7Phase2_SixteenArchBaysWingWindowsStorytellingClustersAndChiaroscuroLighting()` enforcing `P1-01`..`P1-05`. |
| Modify | `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` | Implement 7 new carved arch bays (`Bay #9..#15`), 20 wing windows with god-rays, wing storytelling clusters, platform braces, and chiaroscuro lighting modulation. |
| Regenerate (via `BuildAll`) | `Assets/_Project/Art/Environment/Prison/Sprites/alchemyBibli_00.png`, `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/*.prefab`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` | Sanitized `alchemyBibli_00.png`, rebuilt room prefabs, and rebuilt scene. |

---

### Task 1: Scrub Residual Chromakey Magenta from `alchemyBibli_00.png` (`P1-05a`)

**Files:**
- Modify: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs:448-490`
- Modify: `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs:62-75`

**Interfaces:**
- Consumes: `PrisonPropSpriteGenerator.IsChromakeyMagenta(Color32)`
- Produces: `PrisonPropSpriteGenerator.SanitizeChromakeyPrisonSprites()` covering 6 Prison sprites including `alchemyBibli_00.png`.

- [ ] **Step 1: Update `PrisonChromakeyProps_HaveZeroRawMagentaPixels` in `VisualShaderAndNormalTests.cs` to include `alchemyBibli_00.png`**

In `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`, add `"Assets/_Project/Art/Environment/Prison/Sprites/alchemyBibli_00.png"` to `chromakeyAuditPaths`:

```csharp
            string[] chromakeyAuditPaths =
            {
                "Assets/_Project/Art/Environment/Prison/Sprites/exitDoorSewerShort_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/exitDoorPrisonCourtyard_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/switchCopper_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/tpStoneUp_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/tpStoneDown_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/alchemyBibli_00.png",
            };
```

- [ ] **Step 2: Add `alchemyBibli_00.png` to `SanitizeChromakeyPrisonSprites()` in `PrisonPropSpriteGenerator.cs`**

In `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs`, add `("alchemyBibli_00.png", new Color32(14, 20, 28, 255))` to the `targets` array in `SanitizeChromakeyPrisonSprites()`:

```csharp
            var targets = new (string fileName, Color32 replacementRgb)[]
            {
                ("exitDoorSewerShort_00.png",      new Color32(14, 20, 28, 255)),
                ("exitDoorPrisonCourtyard_00.png", new Color32(14, 20, 28, 255)),
                ("switchCopper_00.png",            new Color32(36, 204, 218, 255)),
                ("tpStoneUp_00.png",               new Color32(36, 204, 218, 255)),
                ("tpStoneDown_00.png",             new Color32(36, 204, 218, 255)),
                ("alchemyBibli_00.png",            new Color32(14, 20, 28, 255)),
            };
```

---

### Task 2: Carved Arch Bays (`Bay #9..#15`), Wing Windows with God-Rays, Storytelling Clusters, Platform Braces & Chiaroscuro Lighting (`P1-01`..`P1-05`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs:1240-1269`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs:804-1226, 1426-1433, 3174-3238`

**Interfaces:**
- Consumes: `CarveAndFrameArchBay`, `AddWindowWithGodray`, `AddShelfWithFioles`, `AddFloorJunkCluster`, `AddPlatformCornerBrace`, `AddGroundedPrisonSprite`, `AddPrisonSpriteProp`, `AddScaledCeilingProp`, `AddStandingTorchBeacon`, `AddCeilingLanternBeacon` in `NhaGiamGiengChuongBuilder`.
- Produces: Rebuilt 9 room prefabs and `NhaGiamGiengChuong.unity` passing `A15_Round7Phase2_SixteenArchBaysWingWindowsStorytellingClustersAndChiaroscuroLighting`.

- [ ] **Step 1: Add EditMode test `A15` to `MapWiringTests.cs`**

Append `A15_Round7Phase2_SixteenArchBaysWingWindowsStorytellingClustersAndChiaroscuroLighting` right after `A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes` in `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs`:

```csharp
        [Test]
        public void A15_Round7Phase2_SixteenArchBaysWingWindowsStorytellingClustersAndChiaroscuroLighting()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

            // 1. P1-01: All 16 carved arch bays (Bay #0..#15) exist and their interior cells are carved (null) on TM_Background
            var bgTilemaps = Object.FindObjectsByType<Tilemap>()
                .Where(t => t.name == "TM_Background")
                .ToArray();
            Assert.AreEqual(9, bgTilemaps.Length, "[A15/BgTilemaps] Expected 9 TM_Background tilemaps.");

            for (int bayIndex = 0; bayIndex <= 15; bayIndex++)
            {
                Assert.IsNotNull(GameObject.Find($"ArchBeam_TL_{bayIndex}"),
                    $"P1-01: Missing carved arch bay frame 'ArchBeam_TL_{bayIndex}'.");
                Assert.IsNotNull(GameObject.Find($"ArchBeam_TR_{bayIndex}"),
                    $"P1-01: Missing carved arch bay frame 'ArchBeam_TR_{bayIndex}'.");
            }

            Vector3Int[] carvedWingBayInteriorSamples =
            {
                new Vector3Int(99, 3, 0),    // Bay #9  (Room M)
                new Vector3Int(141, 19, 0),  // Bay #10 (Room N)
                new Vector3Int(144, -6, 0),  // Bay #11 (Room O)
                new Vector3Int(191, -6, 0),  // Bay #12 (Room P)
                new Vector3Int(-72, 19, 0),  // Bay #13 (Room Q)
                new Vector3Int(51, 33, 0),   // Bay #14 (Room S)
                new Vector3Int(100, 50, 0),  // Bay #15 (Room T)
            };
            foreach (var cell in carvedWingBayInteriorSamples)
            {
                Assert.IsTrue(bgTilemaps.All(tm => tm.GetTile(cell) == null),
                    $"P1-01: Wing arch bay interior cell {cell} must be carved (null) on all TM_Background tilemaps.");
            }

            // 2. P1-02 & P1-03: Every expansion wing room (M, N, O, P, Q, R, S, T) has windows with additive GodRays, shelves, and storytelling props
            string[] requiredWingWindows =
            {
                "Art_M_Window1", "Art_M_Window2", "Art_M_Window3",
                "Art_N_Window1", "Art_N_Window2", "Art_N_Window3",
                "Art_O_Window1", "Art_O_Window2",
                "Art_P_Window1", "Art_P_Window2",
                "Art_Q_Window1", "Art_Q_Window2",
                "Art_R_Window1", "Art_R_Window2",
                "Art_S_Window1", "Art_S_Window2",
                "Art_T_Window1", "Art_T_Window2", "Art_T_Window3", "Art_T_Window4",
            };
            foreach (string winName in requiredWingWindows)
            {
                var winGo = GameObject.Find(winName);
                Assert.IsNotNull(winGo, $"P1-02: Missing wing window '{winName}'.");
                var rayTr = winGo.transform.Find("GodRay");
                Assert.IsNotNull(rayTr, $"P1-02: Missing child 'GodRay' under '{winName}'.");
                var raySr = rayTr.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(raySr?.sharedMaterial, $"P1-02: Missing GodRay material under '{winName}'.");
                Assert.AreEqual("DeadCells/AdditiveGodRay2D", raySr.sharedMaterial.shader.name,
                    $"P1-02: '{winName}/GodRay' must use DeadCells/AdditiveGodRay2D.");
            }

            string[] requiredWingStorytellingProps =
            {
                "Art_M_Bed1", "Art_M_Bed2", "Art_M_Shelf1", "Art_M_Junk1_A", "Art_M_Junk2_A",
                "Art_N_Pillar1", "Art_N_Pillar2", "Art_N_BannerBig1", "Art_N_Shelf1", "Art_N_Junk1_A", "Art_N_Junk2_A",
                "Art_O_Pillar1", "Art_O_Cage1", "Art_O_Cage2", "Art_O_Bed1", "Art_O_Shelf1", "Art_O_Junk1_A", "Art_O_Junk2_A",
                "Art_P_Arch1", "Art_P_Cage1", "Art_P_Cage2", "Art_P_Shelf1", "Art_P_Junk1_A", "Art_P_Junk2_A",
                "Art_Q_Shelf1", "Art_Q_Bed1", "Art_Q_Junk1_A", "Art_Q_Junk2_A",
                "Art_R_BannerBig1", "Art_R_Pillar1", "Art_R_Shelf1", "Art_R_Junk1_A", "Art_R_Junk2_A",
                "Art_S_BannerBig1", "Art_S_Shelf1", "Art_S_Shelf2", "Art_S_Cage1", "Art_S_Junk1_A", "Art_S_Junk2_A",
                "Art_T_BannerBig1", "Art_T_BannerBig2", "Art_T_Arch1", "Art_T_Cage1", "Art_T_Cage2", "Art_T_Cage3", "Art_T_Shelf1", "Art_T_Shelf2", "Art_T_Junk1_A", "Art_T_Junk2_A", "Art_T_Junk3_A",
                "Art_O_ShaftStep1BraceL", "Art_O_ShaftStep2BraceL", "Art_S_GalleryBraceL", "Art_T_Mid1BraceL", "Art_T_Mid2BraceL", "Art_T_HighBraceL",
            };
            foreach (string propName in requiredWingStorytellingProps)
            {
                Assert.IsNotNull(GameObject.Find(propName), $"P1-03/P1-05b: Missing wing storytelling/brace prop '{propName}'.");
            }

            // 3. P1-03b: Zero AABB overlaps between any new wing structural prop and any carved arch bay frame (ArchSill_* / ArchJamb_*) or OneWay_* platform
            var bayFrames = allRenderers
                .Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal) ||
                             sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal))
                .ToArray();
            var oneWayBoxes = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include)
                .Where(box => box.gameObject.layer == GeometryAuditHelper.LayerOneWay &&
                              box.gameObject.name.StartsWith("OneWay_", StringComparison.Ordinal))
                .ToArray();

            string[] phase2StructuralProps =
            {
                "Art_M_Bed1", "Art_M_Bed2", "Art_M_Shelf1",
                "Art_N_Pillar1", "Art_N_Pillar2", "Art_N_BannerBig1", "Art_N_Shelf1",
                "Art_O_Pillar1", "Art_O_Bed1", "Art_O_Shelf1",
                "Art_P_Arch1", "Art_P_Shelf1",
                "Art_Q_Shelf1", "Art_Q_Bed1",
                "Art_R_BannerBig1", "Art_R_Pillar1", "Art_R_Shelf1",
                "Art_S_BannerBig1", "Art_S_Shelf1", "Art_S_Shelf2",
                "Art_T_BannerBig1", "Art_T_BannerBig2", "Art_T_Arch1", "Art_T_Shelf1", "Art_T_Shelf2",
            };
            foreach (string propName in phase2StructuralProps)
            {
                var propGo = GameObject.Find(propName);
                Assert.IsNotNull(propGo, $"Missing '{propName}'.");
                Bounds pb = GetVisualSpriteBounds(propGo);

                foreach (var frame in bayFrames)
                {
                    bool overlapX = frame.bounds.max.x > pb.min.x + 0.05f && frame.bounds.min.x < pb.max.x - 0.05f;
                    bool overlapY = frame.bounds.max.y > pb.min.y + 0.05f && frame.bounds.min.y < pb.max.y - 0.05f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P1-01/P1-03: Arch frame '{frame.name}' overlaps new wing prop '{propName}' ({pb.min}..{pb.max}).");
                }

                foreach (var ow in oneWayBoxes)
                {
                    Bounds ob = GeometryAuditHelper.GetWorldBounds(ow);
                    bool overlapX = pb.min.x < ob.max.x - 0.05f && pb.max.x > ob.min.x + 0.05f;
                    bool overlapY = pb.min.y < ob.max.y + 0.10f && pb.max.y > ob.min.y - 0.35f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P1-03: Wing prop '{propName}' ({pb.min.x:F2}..{pb.max.x:F2}, {pb.min.y:F2}..{pb.max.y:F2}) overlaps OneWay '{ow.name}' ({ob.min.x:F2}..{ob.max.x:F2}, {ob.min.y:F2}..{ob.max.y:F2}).");
                }
            }

            // 4. P1-04: Chiaroscuro lighting calibration (Global Light 2D <= 0.48f cool slate, Point Light intensity modulation spread >= 0.40f, falloff >= 0.58f)
            var globalLight = Object.FindObjectsByType<Light2D>().Single(l => l.lightType == Light2D.LightType.Global);
            Assert.That(globalLight.intensity, Is.InRange(0.35f, 0.48f),
                $"P1-04: Global Light 2D intensity ({globalLight.intensity:F2}) must be in [0.35, 0.48] for Dead Cells chiaroscuro contrast.");
            Assert.LessOrEqual(globalLight.color.r, 0.62f,
                $"P1-04: Global Light 2D red channel ({globalLight.color.r:F2}) must be <= 0.62 for cool indigo-slate shadows.");

            var lanternLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .Where(l => l.lightType == Light2D.LightType.Point &&
                            (l.gameObject.name == "FlameLight" || l.gameObject.name == "LanternLight"))
                .ToArray();
            Assert.GreaterOrEqual(lanternLights.Length, 80, "[A15/LanternLights] Expected >= 80 wall/ceiling lantern point lights.");

            float minInt = lanternLights.Min(l => l.intensity);
            float maxInt = lanternLights.Max(l => l.intensity);
            Assert.GreaterOrEqual(maxInt - minInt, 0.40f,
                $"P1-04: Lantern point light intensities must be modulated (spread={maxInt - minInt:F2}, min={minInt:F2}, max={maxInt:F2}), not clamped to uniform 1.40.");
            foreach (var l in lanternLights)
            {
                Assert.GreaterOrEqual(l.falloffIntensity, 0.58f - 1e-4f,
                    $"P1-04: Lantern light '{l.transform.parent?.name}' falloffIntensity ({l.falloffIntensity:F2}) must be >= 0.58 for soft chiaroscuro roll-off.");
            }
        }
```

- [ ] **Step 2: Implement `P1-01`..`P1-05` in `NhaGiamGiengChuongBuilder.cs`**

Update `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` per Spec §3.2–§3.4:

1. **In `AddStandingTorchBeacon`, `AddCeilingLanternBeacon`, and `BuildWallLanternBeacon` (`L3174-3238`):**
   - Replace `Mathf.Max(intensity, 1.40f)` with `Mathf.Clamp(intensity, 0.75f, 1.45f)` in both `AddStandingTorchBeacon` and `AddCeilingLanternBeacon`.
   - In `BuildWallLanternBeacon`, set `light.falloffIntensity = 0.62f;` (was `0.45f`) and pass `volumeIntensity: 0.065f` (was `0.09f`) to `ConfigureGameplayPointLight`.

2. **In `BuildScene` (`L1426-1432`):**
   - Set `globalLight.intensity = 0.44f;` and `globalLight.color = new Color(0.56f, 0.70f, 0.84f, 1f);`.

3. **In `BuildRoomW1_EastCourtyard` (`L836-912`):**
   - After `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 5, 118, 126, 14, 19);`, add:
     ```csharp
     CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 9, 97, 102, 2, 6);
     CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 10, 138, 144, 18, 22);
     ```
   - After Room N Hero Landmarks (`L895`), add Room M & Room N windows with god-rays and storytelling clusters from Spec §3.3:
     ```csharp
     // Phase 2 (P1-02 & P1-03): Room M & N Windows with GodRays, Buttress Piers & Storytelling Clusters
     AddWindowWithGodray(artRoot, "Art_M_Window1", mat, unlitMat, pal, new Vector2(80.5f, 4.2f));
     AddWindowWithGodray(artRoot, "Art_M_Window2", mat, unlitMat, pal, new Vector2(130.5f, 4.2f));
     AddWindowWithGodray(artRoot, "Art_M_Window3", mat, unlitMat, pal, new Vector2(158.0f, 4.2f));
     AddGroundedPrisonSprite(artRoot, "Art_M_Bed1", pal.WallBed, mat, 88.5f, 0f, 0.90f, -11, Color.white);
     AddShelfWithFioles(artRoot, "Art_M_Shelf1", pal, mat, 92.5f, 1.4f, -11);
     AddGroundedPrisonSprite(artRoot, "Art_M_Bed2", pal.WallBed, mat, 138.0f, 0f, 0.90f, -11, Color.white);
     AddFloorJunkCluster(artRoot, "Art_M_Junk1", pal, mat, 86.0f, 0f, 1);
     AddFloorJunkCluster(artRoot, "Art_M_Junk2", pal, mat, 131.5f, 0f, 4);

     AddWindowWithGodray(artRoot, "Art_N_Window1", mat, unlitMat, pal, new Vector2(80.5f, 18.2f));
     AddWindowWithGodray(artRoot, "Art_N_Window2", mat, unlitMat, pal, new Vector2(99.5f, 18.2f));
     AddWindowWithGodray(artRoot, "Art_N_Window3", mat, unlitMat, pal, new Vector2(158.0f, 18.2f));
     AddGroundedPrisonSprite(artRoot, "Art_N_Pillar1", pal.SpawnerPillar, mat, 98.5f, 8f, 0.90f, -13, Color.white);
     AddGroundedPrisonSprite(artRoot, "Art_N_Pillar2", pal.SpawnerPillar, mat, 131.5f, 8f, 0.90f, -13, Color.white);
     AddPrisonSpriteProp(artRoot, "Art_N_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(91.0f, 17.5f), 0.65f, -12, Color.white);
     AddShelfWithFioles(artRoot, "Art_N_Shelf1", pal, mat, 113.0f, 10.6f, -11);
     AddFloorJunkCluster(artRoot, "Art_N_Junk1", pal, mat, 78.5f, 8f, 2);
     AddFloorJunkCluster(artRoot, "Art_N_Junk2", pal, mat, 129.0f, 8f, 5);
     ```
   - Update `mFloorXs` to `{ 84.5f, 96.2f, 123.0f, 146.5f, 162.8f }` and modulate `mFloorXs`, `nFloorXs`, `nCeilXs` intensities (`(i % 2 == 0) ? 1.34f : 0.88f`, color `(i % 2 == 0) ? warmAmber/warmGold : deepEmber` where `var deepEmber = new Color(0.95f, 0.70f, 0.40f, 1f);`).

4. **In `BuildRoomW2_SewerCistern` (`L943-1021`):**
   - After `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 6, 100, 106, -13, -8);`, add:
     ```csharp
     CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 11, 140, 148, -9, -3);
     CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 12, 187, 195, -9, -3);
     ```
   - Add corner braces on `OneWay_O_ShaftStep1` and `OneWay_O_ShaftStep2` (`Art_O_ShaftStep1BraceL/R`, `Art_O_ShaftStep2BraceL/R`) per Spec §3.3.
   - Add Room O & Room P windows with god-rays, piers, arches, cages, beds, shelves, and junk clusters per Spec §3.3.
   - Modulate `oFloorXs`, `oCeilXs`, `pFloorXs`, `pCeilXs` intensities (`(i % 2 == 0) ? 1.34f : 0.88f`, alternating `warmAmber`/`warmGold` with `deepEmber`).

5. **In `BuildRoomW3_WestBastion` (`L1053-1117`):**
   - After `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 7, -60, -54, 30, 35);`, add:
     ```csharp
     CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 13, -75, -69, 18, 22);
     ```
   - Add Room Q & Room R windows with god-rays, banners, piers, beds, shelves, and junk clusters per Spec §3.3.
   - Update `qFloorXs` to `{ -98.5f, -76.2f, -56.0f, -34.0f }` and modulate `qFloorXs`, `rFloorXs`, `rCeilXs` intensities (`(i % 2 == 0) ? 1.34f : 0.88f`).

6. **In `BuildRoomU1_UpperPromenade` (`L1150-1226`):**
   - After `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 8, 66, 74, 46, 52);`, add:
     ```csharp
     CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 14, 47, 55, 31, 37);
     CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 15, 96, 104, 47, 55);
     ```
   - Add corner braces on `OneWay_S_Gallery`, `OneWay_T_Mid1`, `OneWay_T_Mid2`, `OneWay_T_High` per Spec §3.3.
   - Add Room S & Room T windows with god-rays, banners, arches, cages, shelves, and junk clusters per Spec §3.3.
   - Modulate `sFloorXs`, `sCeilXs`, `tFloorXs`, `tCeilXs`, `tMidXs` intensities (`(i % 2 == 0) ? 1.34f : 0.88f`).

---

### Task 3: Low-RAM Sequential Build, EditMode (4 Batches), D3D12 GPU Captures & PlayMode Verification Gate

**Files:**
- Verify: `Logs/round7-phase2-*.xml`, `Logs/round7-phase2-gpu-capture.log`, `Logs/r6-gpu-captures/*.png`

**Interfaces:**
- Consumes: `PrisonPropSpriteGenerator.EnsureAll()`, `NhaGiamGiengChuongBuilder.BuildAll()`, `NhaGiamGiengChuongBuilder.CaptureRound6GpuVerificationFrames()`, `VisualShaderAndNormalTests`, `MapWiringTests.A15`.
- Produces: Verified 77/77 passing EditMode & PlayMode tests + 9/9 GPU captures (`maxClip <= 0.500%`) + synced CodeGraph index.

- [ ] **Step 1: Execute `NhaGiamGiengChuongBuilder.BuildAll` (low-RAM single job worker)**

Clean any lingering Unity/Bee processes, then run:
```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force"
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -quit -logFile D:/thietkegame/Logs/round7-phase2-buildall.log
```
Expected: Exit code `0`, `[NhaGiamGiengChuongBuilder] BuildAll completed cleanly.` in `Logs/round7-phase2-buildall.log`.

- [ ] **Step 2: Run EditMode tests in 4 low-RAM batches (`Core.Tests`, `Unit+Geometry`, `A07..A10`, `A11..A15`)**

Run sequentially (cleaning lingering processes before each launch):
```powershell
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames Core.Tests.EditMode -testResults D:/thietkegame/Logs/round7-phase2-editmode-core.xml -logFile D:/thietkegame/Logs/round7-phase2-editmode-core.log

rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames NhaGiamGiengChuong.Tests.EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.Unit.HealingChestTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.EncounterGateTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.EncounterZoneTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.ExitTriggerTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.LevelRunTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.LoreTabletTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.SecondaryBiomeExitTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.SwitchDoorTests;DeadCells.NhaGiamGiengChuong.Tests.Unit.TeleportGateTests;DeadCells.NhaGiamGiengChuong.Tests.Assets.ABGeometryTests;DeadCells.NhaGiamGiengChuong.Tests.Assets.CDGeometryTests;DeadCells.NhaGiamGiengChuong.Tests.Assets.ELFGeometryTests;DeadCells.NhaGiamGiengChuong.Tests.Assets.GGeometryTests;DeadCells.NhaGiamGiengChuong.Tests.Assets.HJKGeometryTests;DeadCells.NhaGiamGiengChuong.Tests.Assets.WingGeometryTests;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapGeometryTests" -testResults D:/thietkegame/Logs/round7-phase2-editmode-geom.xml -logFile D:/thietkegame/Logs/round7-phase2-editmode-geom.log

rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames NhaGiamGiengChuong.Tests.EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A07_AssembledScene_ZeroMissingScriptsAndValidatesAllSerializedWiringAndBuildSettings;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A08_VisualCoverageGroundedPropsAndCorrectChestSprite;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A09_SceneHudAndChestWorldUiAreWiredAndVisible;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A10_DeadCellsPrisonVisualStandard" -testResults D:/thietkegame/Logs/round7-phase2-editmode-wiring1.xml -logFile D:/thietkegame/Logs/round7-phase2-editmode-wiring1.log

rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames NhaGiamGiengChuong.Tests.EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A11_Round5_ParallaxShadersNormalMapsZeroMixelPropsAndOneWayArc;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A12_Round5FinalReview_SubFloorDirtSkirtsBayClearanceAndCalibratedLighting;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A13_FullScalePrisonersQuarters_ScrollsGoldTeleportersSwitchDoorsLoreAndDualExits;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes;DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A15_Round7Phase2_SixteenArchBaysWingWindowsStorytellingClustersAndChiaroscuroLighting" -testResults D:/thietkegame/Logs/round7-phase2-editmode-wiring2.xml -logFile D:/thietkegame/Logs/round7-phase2-editmode-wiring2.log
```
Expected: `30/30` (`core`), `30/30` (`geom`), `4/4` (`wiring1`), `5/5` (`wiring2`) = `69/69` EditMode tests PASS (`0` failed).

- [ ] **Step 3: Refresh D3D12 GPU verification captures (`CaptureRound6GpuVerificationFrames`)**

Run without `-nographics`:
```powershell
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -job-worker-count 1 -disable-assembly-updater -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.CaptureRound6GpuVerificationFrames -quit -logFile D:/thietkegame/Logs/round7-phase2-gpu-capture.log
```
Expected: Exit code `0`, `captured=9/9 shaderErrors=0 preFlight=5/5 defaultMaterialRenderers=0 maxClip <= 0.500%`.

- [ ] **Step 4: Run PlayMode test suite (`NhaGiamGiengChuong.Tests.PlayMode`) & sync CodeGraph**

```powershell
rtk proxy "D:/Unity/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath D:/thietkegame -runTests -testPlatform PlayMode -assemblyNames NhaGiamGiengChuong.Tests.PlayMode -testResults D:/thietkegame/Logs/round7-phase2-playmode.xml -logFile D:/thietkegame/Logs/round7-phase2-playmode.log
rtk proxy cmd /c codegraph sync
```
Expected: `8/8` PlayMode tests PASS (`77/77` total tests green), CodeGraph synced.
