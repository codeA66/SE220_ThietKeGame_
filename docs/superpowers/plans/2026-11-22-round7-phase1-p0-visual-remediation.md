# Round 7 (Phase 1): NhaGiamGiengChuong P0 Visual & Spatial Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remediate all four `P0` gameplay-breaking visual & spatial defects (`P0-01` Spawn/Room AB outer camera void, `P0-02` unbaked magenta chromakey `#FF00FF` on exit doors/switches/teleporters + exit glow layer ordering, `P0-03` shrunk toy-scale `0.28×` landmarks, and `P0-04` prop/platform/torch/scroll/chain AABB collisions across all 9 room prefabs) in `NhaGiamGiengChuong.unity` while preserving 100% compatibility with all 74 existing EditMode/PlayMode tests.

**Architecture:**
1. Extend `PrisonPropSpriteGenerator.EnsureAll()` (invoked at the top of `NhaGiamGiengChuongBuilder.BuildAll()`) with a deterministic chromakey sanitizer `SanitizeChromakeyPrisonSprites()` that scrubs raw `#FF00FF` / hot-magenta chromakey pixels from `exitDoorSewerShort_00.png`, `exitDoorPrisonCourtyard_00.png`, `switchCopper_00.png`, `tpStoneUp_00.png`, and `tpStoneDown_00.png`, re-generating their normal maps and reimporting cleanly.
2. Update `NhaGiamGiengChuongBuilder.cs` to:
   - Paint outer bedrock skirts on `TM_SolidVisual` (`[-16, -2] × [-8, 14]`, `[-1, 17] × [6, 14]`, `[18, 21] × [6, 7]`) and backwall extension on `TM_Background` (`[-16, -2] × [-8, 14]`) inside `BuildPrefabAB`.
   - Render `Art_P_ExitGlow` on `Ground` sorting order `-4` (in front of `Art_P_SewerExitDoor` at `Ground/-5`).
   - Replace shrunk `0.28×` `armory_dome_00` / `armory_corps_00` placements with proper 24-PPU 377-catalog architectural sprites (`alcovePillar_00` at `0.85×`, `archeSmall_00` at `1.0×`, `prisonBigAlcove_00` at `0.90×`) and relocate `Art_A_*`, `Art_G_*`, `Art_E_*`, `Art_M_*`, `Art_N_*`, `Art_O_*`, `Art_P_*`, `Art_Q_*`, `Art_R_*`, `Art_S_*`, `Art_T_*`, `GlowingMushroom_4`, and `AddRoomWarmSpotlights()` coordinates so zero props/torches/scrolls/chains collide with any of the 31 `OneWay` platforms, windows, chains, or interactive items.
3. Lock every `P0-01`..`P0-04` invariant with two new EditMode tests (`PrisonChromakeyProps_HaveZeroRawMagentaPixels` in `VisualShaderAndNormalTests.cs` and `A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes` in `MapWiringTests.cs`).

**Tech Stack:** Unity `6000.6.9f1` (URP 2D Renderer, `Tilemap`, `Light2D`), C# Editor Builders (`PrisonPropSpriteGenerator`, `NhaGiamGiengChuongBuilder`), NUnit EditMode & PlayMode Test Runner, `rtk` CLI wrapper.

**Spec:** `D:/thietkegame/docs/superpowers/specs/2026-11-22-round7-phase1-p0-visual-remediation-design.md`

---

## Global Constraints

- **Shell Wrapper (`AGENTS.md` §0):** Run all shell commands via `rtk` or `rtk proxy <command>`. Read/write files using UTF-8.
- **No `.meta` Fabrication (`AGENTS.md` §2):** Never manually create, edit, or delete `.meta` files; let Unity `AssetDatabase.ImportAsset` manage them.
- **No Unrelated Structural Edits:** Keep all physical `BoxCollider2D` geometry (`Ground`, `OneWayPlatform`, `Climbable`), `RoomBounds_A..T`, `TP_0..5`, `SD_0..2`, `I1..I16`, and enemy spawn coordinates untouched so all 74 existing tests (`A01`–`A13`, `A05b`–`A05e`, `P01`–`P08`) continue to pass.
- **D3D12 Batchmode Rule:** When running Unity batchmode CLI commands, always ensure no other Unity instance holds the project lock (`unity status --project-path D:/thietkegame --json`).

## Review Focus

- Verify that `PrisonPropSpriteGenerator.SanitizeChromakeyPrisonSprites()` is idempotent (only writes PNG bytes and reimports when `modified > 0`).
- Verify that every one of the 17 grounded props checked by `A08` (`MapWiringTests.cs:303-331`) still has `sr.bounds.min.y == floorY ± 0.12f`.
- Verify that all 8 `RoomBanner` shaders (`Art_C/J/E/F/N/P/R/T_Banner`) preserve their exact `_PinTopWorldY` values (`16.5f, 21.8f, 30.2f, 37.2f, 21.8f, -2.2f, 37.8f, 61.8f`) required by `A11`.
- Verify that `GlowingMushroom_0..7` preserve their exact floor Y array `{ 0f, 0f, 16f, 24f, 0f, -16f, -16f, 16f }` required by `A11`.
- Verify that `A10` lighting coverage remains `>= 50%` per room and `>= 75%` overall.

## File Structure Map

| Action | File Path | Responsibility |
| --- | --- | --- |
| Modify | `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs` | Add `SanitizeChromakeyPrisonSprites()` + `IsChromakeyMagenta(Color32)` and invoke from `EnsureAll()`. |
| Modify | `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs` | Add `[Test] PrisonChromakeyProps_HaveZeroRawMagentaPixels()` verifying `0` hot-magenta pixels in the 5 sanitized textures. |
| Modify | `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` | Add `[Test] A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes()` enforcing `P0-01`..`P0-04`. |
| Modify | `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` | Implement `P0-01`..`P0-04` visual/spatial layout fixes across `BuildPrefabAB`..`BuildPrefabT`, `AddAtmosphericEffects`, and `AddRoomWarmSpotlights`. |
| Regenerate (via `BuildAll`) | `Assets/_Project/Art/Environment/Prison/Sprites/{exitDoorSewerShort_00,exitDoorPrisonCourtyard_00,switchCopper_00,tpStoneUp_00,tpStoneDown_00}.png` + `Normals/*_n.png` | Sanitized chromakey-free PNG textures and updated normal maps. |
| Regenerate (via `BuildAll`) | `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{AB,CD,IJ,EF,G,K,L,MNOP,QRS,T}.prefab` & `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` | Rebuilt room prefabs and scene with all `P0` defects eliminated. |

---

### Task 1: Deterministic Chromakey Magenta Sanitizer in `PrisonPropSpriteGenerator` (`P0-02`)

**Files:**
- Modify: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`
- Modify: `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs`

- [ ] **Step 1: Write the failing EditMode test in `VisualShaderAndNormalTests.cs`**

Append the test `PrisonChromakeyProps_HaveZeroRawMagentaPixels` right before the closing brace of `VisualShaderAndNormalTests` in `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`:

```csharp
        [Test]
        public void PrisonChromakeyProps_HaveZeroRawMagentaPixels()
        {
            PrisonPropSpriteGenerator.EnsureAll();

            string[] chromakeyAuditPaths =
            {
                "Assets/_Project/Art/Environment/Prison/Sprites/exitDoorSewerShort_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/exitDoorPrisonCourtyard_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/switchCopper_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/tpStoneUp_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/tpStoneDown_00.png",
            };

            foreach (string assetPath in chromakeyAuditPaths)
            {
                Assert.IsTrue(File.Exists(assetPath), $"Expected sprite PNG at '{assetPath}'.");
                byte[] rawBytes = File.ReadAllBytes(assetPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: true);
                try
                {
                    Assert.IsTrue(ImageConversion.LoadImage(tex, rawBytes, markNonReadable: false),
                        $"Failed to decode PNG at '{assetPath}'.");
                    Color32[] pixels = tex.GetPixels32();
                    int magentaCount = 0;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color32 c = pixels[i];
                        if (c.a > 16 && c.r >= 200 && c.b >= 190 && c.g <= 175 && (c.r - c.g) >= 45 && (c.b - c.g) >= 35)
                        {
                            magentaCount++;
                        }
                    }

                    Assert.AreEqual(0, magentaCount,
                        $"Sprite '{assetPath}' still contains {magentaCount} unbaked hot-magenta chromakey pixels.");
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }
            }
        }
```

- [ ] **Step 2: Run the new test to verify it fails (RED)**

Run via `rtk proxy`:
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests.PrisonChromakeyProps_HaveZeroRawMagentaPixels" -testResults D:/thietkegame/Logs/test-p0-task1-red.xml -logFile D:/thietkegame/Logs/test-p0-task1-red.log
```
Expected: FAIL (`exitDoorSewerShort_00.png` still contains `4433` unbaked hot-magenta chromakey pixels).

- [ ] **Step 3: Implement `SanitizeChromakeyPrisonSprites()` in `PrisonPropSpriteGenerator.cs`**

In `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs`:
1. Call `SanitizeChromakeyPrisonSprites();` inside `EnsureAll()` after `EnsureNormalAndBinding(ArchJambAssetPath, ArchJambNormalPath);` and before `AssetDatabase.SaveAssets();`.
2. Add `IsChromakeyMagenta(Color32 c)` and `SanitizeChromakeyPrisonSprites()` inside `PrisonPropSpriteGenerator`:

```csharp
        public static bool IsChromakeyMagenta(Color32 c)
        {
            return c.a > 16
                && c.r >= 200
                && c.b >= 190
                && c.g <= 175
                && (c.r - c.g) >= 45
                && (c.b - c.g) >= 35;
        }

        public static void SanitizeChromakeyPrisonSprites()
        {
            var targets = new (string fileName, Color32 replacementRgb)[]
            {
                ("exitDoorSewerShort_00.png",      new Color32(14, 20, 28, 255)),
                ("exitDoorPrisonCourtyard_00.png", new Color32(14, 20, 28, 255)),
                ("switchCopper_00.png",            new Color32(36, 204, 218, 255)),
                ("tpStoneUp_00.png",               new Color32(36, 204, 218, 255)),
                ("tpStoneDown_00.png",             new Color32(36, 204, 218, 255)),
            };

            foreach (var target in targets)
            {
                string spritePath = $"{SpriteDir}/{target.fileName}";
                if (!File.Exists(spritePath))
                {
                    continue;
                }

                byte[] rawBytes = File.ReadAllBytes(spritePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: true);
                try
                {
                    if (!ImageConversion.LoadImage(tex, rawBytes, markNonReadable: false))
                    {
                        continue;
                    }

                    Color32[] pixels = tex.GetPixels32();
                    int modified = 0;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color32 p = pixels[i];
                        if (IsChromakeyMagenta(p))
                        {
                            float detail = Mathf.Clamp01((255f - p.g) / 255f);
                            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(target.replacementRgb.r * (0.75f + 0.25f * detail)), 0, 255);
                            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(target.replacementRgb.g * (0.75f + 0.25f * detail)), 0, 255);
                            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(target.replacementRgb.b * (0.75f + 0.25f * detail)), 0, 255);
                            pixels[i] = new Color32(r, g, b, p.a);
                            modified++;
                        }
                    }

                    if (modified > 0)
                    {
                        tex.SetPixels32(pixels);
                        tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                        File.WriteAllBytes(spritePath, tex.EncodeToPNG());
                        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                        string stem = Path.GetFileNameWithoutExtension(target.fileName);
                        string normalPath = $"{NormalDir}/{stem}_n.png";
                        if (File.Exists(normalPath))
                        {
                            EnsureNormalAndBinding(spritePath, normalPath);
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }
            }
        }
```

- [ ] **Step 4: Run `VisualShaderAndNormalTests` to verify it passes (GREEN)**

Run via `rtk proxy`:
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/test-p0-task1-green.xml -logFile D:/thietkegame/Logs/test-p0-task1-green.log
```
Expected: PASS (`6/6` tests in `VisualShaderAndNormalTests` green, `0` magenta pixels across all 5 textures).

---

### Task 2: Spawn Bedrock Skirts (`P0-01`), Exit Glow Layering (`P0-02`), Proper 24-PPU Landmark Proportions (`P0-03`), and Zero Platform/Torch/Scroll/Chain Clashes (`P0-04`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`

- [ ] **Step 1: Write the failing EditMode test `A14` in `MapWiringTests.cs`**

Append `A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes` right after `A13_Round6FullScaleExpansion_AllNineRoomPrefabsAndgpuCapturesPass` in `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs`:

```csharp
        [Test]
        public void A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "NhaGiamGiengChuong scene must open cleanly for A14.");

            // 1. P0-01: Outer bedrock skirt around Spawn / Room AB so orthoSize=7 camera at (3, 1.5) sees zero void
            var solidVisGo = FindInScene(scene, "TM_SolidVisual");
            Assert.IsNotNull(solidVisGo, "Missing TM_SolidVisual in Room AB.");
            var solidVisTm = solidVisGo.GetComponent<Tilemap>();
            Assert.IsNotNull(solidVisTm, "Missing Tilemap on TM_SolidVisual.");

            for (int x = -15; x <= -2; x++)
            {
                for (int y = -6; y <= 12; y++)
                {
                    Assert.IsNotNull(solidVisTm.GetTile(new Vector3Int(x, y, 0)),
                        $"P0-01: Outer west bedrock cell ({x},{y}) must be filled on TM_SolidVisual.");
                }
            }

            for (int x = 0; x <= 17; x++)
            {
                for (int y = 6; y <= 12; y++)
                {
                    Assert.IsNotNull(solidVisTm.GetTile(new Vector3Int(x, y, 0)),
                        $"P0-01: Outer upper bedrock cell ({x},{y}) above Room A must be filled on TM_SolidVisual.");
                }
            }

            // 2. P0-02:Art_P_ExitGlow renders in front of Art_P_SewerExitDoor, and door sprites have 0 chromakey pixels
            var pDoor = FindInScene(scene, "Art_P_SewerExitDoor");
            var pGlow = FindInScene(scene, "Art_P_ExitGlow");
            Assert.IsNotNull(pDoor, "Missing Art_P_SewerExitDoor.");
            Assert.IsNotNull(pGlow, "Missing Art_P_ExitGlow.");
            var pDoorSr = pDoor.GetComponent<SpriteRenderer>();
            var pGlowSr = pGlow.GetComponent<SpriteRenderer>();
            Assert.AreEqual(pDoorSr.sortingLayerName, pGlowSr.sortingLayerName,
                "P0-02: Art_P_ExitGlow and Art_P_SewerExitDoor must share sorting layer 'Ground'.");
            Assert.Greater(pGlowSr.sortingOrder, pDoorSr.sortingOrder,
                "P0-02: Art_P_ExitGlow sortingOrder must be strictly greater than Art_P_SewerExitDoor so glow is not occluded.");

            // 3. P0-03: Zero shrunk toy-scale (scale < 0.70) armory_dome_00 or armory_corps_00 in the scene
            var allRenderers = new List<SpriteRenderer>();
            foreach (var root in scene.GetRootGameObjects())
            {
                allRenderers.AddRange(root.GetComponentsInChildren<SpriteRenderer>(true));
            }

            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.sprite == null) continue;
                string sName = sr.sprite.name;
                if (sName.StartsWith("armory_dome_00") || sName.StartsWith("armory_corps_00"))
                {
                    Assert.GreaterOrEqual(sr.transform.localScale.x, 0.70f,
                        $"P0-03: '{sr.gameObject.name}' uses '{sName}' at toy scale {sr.transform.localScale.x:F2} (< 0.70).");
                }
            }

            // 4. P0-04a: Zero AABB overlaps between any of the 31 OneWay platforms and architectural landmarks/doors/shelves/statues/banners
            var oneWayBoxes = new List<BoxCollider2D>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var box in root.GetComponentsInChildren<BoxCollider2D>(true))
                {
                    if (box.gameObject.layer == 9 && box.gameObject.name.StartsWith("OneWay_"))
                    {
                        oneWayBoxes.Add(box);
                    }
                }
            }
            Assert.AreEqual(31, oneWayBoxes.Count, "Expected exactly 31 OneWay platforms.");

            string[] platformForbiddenProps =
            {
                "Art_G_Gate", "Art_M_ArmoryDome1", "Art_M_ArmoryCorps", "Art_M_WeaponDeco",
                "Art_N_SnakeStat", "Art_N_Banner", "Art_O_MonsterDoor", "Art_P_SewerExitDoor", "Art_P_Banner",
                "Art_Q_JudasSeat", "Art_Q_SkeletonSpawn", "Art_Q_Trophy",
                "Art_R_ArmoryDome", "Art_R_BigSword", "Art_R_Banner",
                "Art_S_AlchemyBibli1", "Art_S_AlchemyBibli2", "Art_S_BlacksmithFurnace", "Art_S_Collector",
                "Art_T_CourtyardArch", "Art_T_AlcovePillar1", "Art_T_AlcovePillar2", "Art_T_CellDoor1", "Art_T_CellDoor2", "Art_T_Banner"
            };

            foreach (string propName in platformForbiddenProps)
            {
                var propGo = FindInScene(scene, propName);
                Assert.IsNotNull(propGo, $"Missing expected landmark '{propName}'.");
                var propSr = propGo.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(propSr, $"Missing SpriteRenderer on '{propName}'.");
                Bounds pb = propSr.bounds;

                foreach (var ow in oneWayBoxes)
                {
                    Bounds ob = ow.bounds;
                    bool overlapX = pb.min.x < ob.max.x - 0.05f && pb.max.x > ob.min.x + 0.05f;
                    bool overlapY = pb.min.y < ob.max.y + 0.10f && pb.max.y > ob.min.y - 0.35f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P0-04a: Prop '{propName}' bounds ({pb.min.x:F2}..{pb.max.x:F2}, {pb.min.y:F2}..{pb.max.y:F2}) collide with OneWay platform '{ow.name}' ({ob.min.x:F2}..{ob.max.x:F2}, {ob.min.y:F2}..{ob.max.y:F2}).");
                }
            }

            // 5. P0-04b: Zero AABB overlaps between any LanternFixture_R5 and windows, climbing chains, exit doors, or landmarks
            var lanterns = new List<SpriteRenderer>();
            var lanternBlockers = new List<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.sprite == null) continue;
                string n = sr.gameObject.name;
                if (n == "LanternFixture_R5")
                {
                    lanterns.Add(sr);
                }
                else if (n.Contains("Window") ||
                         (n.StartsWith("Art_I") && n.EndsWith("_Chain")) ||
                         n == "Art_F_ExitArch" || n == "Art_P_SewerExitDoor" || n == "Art_T_CourtyardArch" ||
                         n == "Art_E_Pillar1" || n == "Art_E_Pillar2" ||
                         n == "Art_R_BigSword" || n == "Art_Q_SkeletonSpawn" ||
                         n == "Art_S_AlchemyBibli1" || n == "Art_S_AlchemyBibli2" ||
                         n == "Art_T_Banner")
                {
                    lanternBlockers.Add(sr);
                }
            }

            foreach (var lantern in lanterns)
            {
                Bounds lb = lantern.bounds;
                string lanternLabel = lantern.transform.parent != null ? lantern.transform.parent.name : lantern.name;
                foreach (var blocker in lanternBlockers)
                {
                    Bounds bb = blocker.bounds;
                    bool overlapX = lb.min.x < bb.max.x - 0.04f && lb.max.x > bb.min.x + 0.04f;
                    bool overlapY = lb.min.y < bb.max.y - 0.04f && lb.max.y > bb.min.y + 0.04f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P0-04b: Torch '{lanternLabel}' at ({lantern.transform.position.x:F2},{lantern.transform.position.y:F2}) overlaps '{blocker.name}' ({bb.min.x:F2}..{bb.max.x:F2}, {bb.min.y:F2}..{bb.max.y:F2}).");
                }
            }

            // 6. P0-04c: Zero overlap between PowerScroll pedestals and weapon wall props, and disjoint Room A props
            string[][] disjointPairs =
            {
                new[] { "I11_Scroll_EastCourtyard", "Art_M_WeaponDeco" },
                new[] { "I12_Scroll_WestArmory", "Art_R_BigSword" },
                new[] { "Art_A_Bed", "Art_A_Fountain" },
                new[] { "Art_A_Fountain", "Art_A_Window" },
                new[] { "Art_A_Window", "TP_0_Spawn" },
            };

            foreach (var pair in disjointPairs)
            {
                var goA = FindInScene(scene, pair[0]);
                var goB = FindInScene(scene, pair[1]);
                Assert.IsNotNull(goA, $"Missing '{pair[0]}'.");
                Assert.IsNotNull(goB, $"Missing '{pair[1]}'.");
                var srA = goA.GetComponent<SpriteRenderer>();
                var srB = goB.GetComponent<SpriteRenderer>();
                Bounds bA = srA.bounds;
                Bounds bB = srB.bounds;
                bool overlapX = bA.min.x < bB.max.x - 0.05f && bA.max.x > bB.min.x + 0.05f;
                bool overlapY = bA.min.y < bB.max.y - 0.05f && bA.max.y > bB.min.y + 0.05f;
                Assert.IsFalse(overlapX && overlapY,
                    $"P0-04c: '{pair[0]}' ({bA.min.x:F2}..{bA.max.x:F2}, {bA.min.y:F2}..{bA.max.y:F2}) overlaps '{pair[1]}' ({bB.min.x:F2}..{bB.max.x:F2}, {bB.min.y:F2}..{bB.max.y:F2}).");
            }
        }
```

- [ ] **Step 2: Run test `A14` to verify it fails (RED)**

Run via `rtk proxy`:
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.EditorTests.MapWiringTests.A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes" -testResults D:/thietkegame/Logs/test-p0-task2-red.xml -logFile D:/thietkegame/Logs/test-p0-task2-red.log
```
Expected: FAIL (`P0-01: Outer west bedrock cell (-15,-6) must be filled on TM_SolidVisual`).

- [ ] **Step 3: Update `NhaGiamGiengChuongBuilder.cs` to fix `P0-01` through `P0-04`**

Apply the following exact updates in `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`:

1. **`BuildPrefabAB` (`P0-01` & `P0-04` Room A/B):**
   - After painting `FillBackgroundRect(bgTm, pal, 0, 0, 56, 7);`, paint outer bedrock skirts on `solidVis` and `bgTm` so the spawn camera (`orthoSize = 7`) sees deep dungeon earth instead of void:
     ```csharp
     // P0-01: Outer bedrock visual skirt around Spawn / Room A so camera at (3, 1.5) sees zero void
     FillSolidVisual(solidVis, pal, -16, -8, 15, 22);
     FillBackgroundRect(bgTm, pal, -16, -8, 15, 22);
     FillSolidVisual(solidVis, pal, -1, 6, 19, 8);
     FillSolidVisual(solidVis, pal, 18, 6, 4, 2);
     ```
   - Update Room A props (`lines 393-399`) so `Art_A_Bed`, `Art_A_Fountain`, `Art_A_Window`, `Art_A_Skeleton`, and `TP_0_Spawn` (`x=15.5`) have zero overlap:
     ```csharp
     CreateGroundedSprite(props, "Art_A_Bed", pal.PrisonBed, new Vector2(1.5f, 0f), 1.0f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_A_Fountain", pal.EmptyFountain, new Vector2(5.0f, 0f), 1.0f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_A_Window", pal.WindowSimple, new Vector2(10.5f, 3.2f), 1.0f, "Ground", -6, LitMat(mats));
     CreateGodRay(props, "Art_A_GodRay", pal.LightRay, new Vector2(10.5f, 2.2f), new Vector2(1.4f, 1.8f), -22f, mats);
     CreateWallSprite(props, "Art_A_Chain1", pal.BigChain, new Vector2(4.5f, 4.4f), 0.7f, "Ground", -4, LitMat(mats));
     CreateWallSprite(props, "Art_A_Chain2", pal.SmallChain, new Vector2(13.2f, 4.4f), 0.7f, "Ground", -4, LitMat(mats));
     CreateGroundedSprite(props, "Art_A_Skeleton", pal.SkelHanging, new Vector2(13.2f, 2.5f), 0.85f, "Ground", -4, LitMat(mats));
     ```

2. **`BuildPrefabEF` (`P0-04` Room E banner clearance):**
   - Move `Art_E_Banner` X from `40f` to `42.5f` (preserving `pinTopY = 30.2f` and `scale = 0.85f`) so it hangs cleanly between `Bay #2` (`x=37..41`) and `Bay #3` (`x=45..49`) without overlapping `Art_E_Pillar1` at `x=39f`:
     ```csharp
     CreateVerticalBanner(props, "Art_E_Banner", pal, new Vector2(42.5f, 30.2f), 0.85f, mats);
     ```

3. **`BuildPrefabG` (`P0-04` Room G gate vs `OneWay_G_Mid`):**
   - Move `Art_G_Gate` from `(68f, 8f)` to `(61.5f, 8f)` (`scale = 0.85f`) so it stands between `Art_G_Winch` (`x=59.5`) and `OneWay_G_Mid` (`x=63..69`):
     ```csharp
     CreateGroundedSprite(props, "Art_G_Gate", pal.Gate, new Vector2(61.5f, 8f), 0.85f, "Ground", -5, LitMat(mats));
     ```

4. **`BuildPrefabMNOP` (`P0-02`, `P0-03`, `P0-04` across Rooms M, N, O, P):**
   - **Room M (`y = 0`):** Replace shrunk `0.28×` `armory_dome_00` / `armory_corps_00` with proper 24-PPU `pal.AlcovePillar` (`scale = 0.85f`) and `pal.ArcheSmall` (`scale = 1.0f`) in the inter-platform bays, and shift `Art_M_WeaponDeco` to `(137.0f, 3.2f)` clear of `I11_Scroll_EastCourtyard` (`x=140.5f`):
     ```csharp
     // Room M decorations: East Prison Wing (Armory & Barracks)
     CreateGroundedSprite(props, "Art_M_ArmoryDome1", pal.AlcovePillar, new Vector2(109.2f, 0f), 0.85f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_M_ArmoryCorps", pal.ArcheSmall, new Vector2(127.5f, 0f), 1.0f, "Ground", -6, LitMat(mats));
     CreateWallSprite(props, "Art_M_WeaponDeco", pal.WeaponDeco, new Vector2(137.0f, 3.2f), 0.80f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_M_Bed1", pal.PrisonBed, new Vector2(88f, 0f), 1.0f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_M_Bed2", pal.PrisonBed, new Vector2(146f, 0f), 1.0f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_M_Window1", pal.WindowSimple, new Vector2(96f, 4.5f), 1.0f, "Ground", -6, LitMat(mats));
     CreateGodRay(props, "Art_M_GodRay1", pal.LightRay, new Vector2(96f, 3.2f), new Vector2(1.4f, 1.8f), -22f, mats);
     CreateWallSprite(props, "Art_M_Window2", pal.WindowSimple, new Vector2(138f, 4.5f), 1.0f, "Ground", -6, LitMat(mats));
     CreateGodRay(props, "Art_M_GodRay2", pal.LightRay, new Vector2(138f, 3.2f), new Vector2(1.4f, 1.8f), -22f, mats);
     ```
   - **Room N (`y = 8`):** Move `Art_N_SnakeStat` to `(108.5f, 8f)` (`scale = 0.70f`) and `Art_N_Banner` to `(127.5f, 21.8f)` (`scale = 0.85f`), both in open inter-platform bays:
     ```csharp
     // Room N decorations: East Upper Gallery (y = 8..22)
     CreateGroundedSprite(props, "Art_N_SnakeStat", pal.SnakeLandmark, new Vector2(108.5f, 8f), 0.70f, "Ground", -6, LitMat(mats));
     CreateVerticalBanner(props, "Art_N_Banner", pal, new Vector2(127.5f, 21.8f), 0.85f, mats);
     CreateGroundedSprite(props, "Art_N_Table", pal.TableSmall, new Vector2(96f, 8f), 1.0f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_N_Chair", pal.Chair, new Vector2(98.5f, 8f), 1.0f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_N_Cage1", pal.SuspendedCage, new Vector2(107.0f, 19.2f), 0.80f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_N_Cage2", pal.SuspendedCage, new Vector2(142.0f, 19.2f), 0.80f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_N_Window1", pal.WindowLarge, new Vector2(108.5f, 15.0f), 0.85f, "Ground", -6, LitMat(mats));
     CreateGodRay(props, "Art_N_GodRay1", pal.LightRay, new Vector2(108.5f, 12.5f), new Vector2(1.6f, 2.2f), -22f, mats);
     ```
   - **Room O (`y = -16`):** Move `Art_O_MonsterDoor` to `(119.0f, -16f)` (`scale = 0.80f`) in the wide `x ∈ [112, 126]` floor bay between `OneWay_O_2` and `OneWay_O_3`, and move `Art_O_Cage` to `(103.5f, -3.5f)`:
     ```csharp
     // Room O decorations: Under-prison Crypt (y = -16..-2)
     CreateGroundedSprite(props, "Art_O_MonsterDoor", pal.ExitDoorMonster, new Vector2(119.0f, -16f), 0.80f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_O_Skeleton1", pal.SkelSitting, new Vector2(94f, -16f), 1.0f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_O_Skeleton2", pal.SkelHanging, new Vector2(138f, -12.5f), 0.85f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_O_Cage", pal.SuspendedCage, new Vector2(103.5f, -3.5f), 0.80f, "Ground", -5, LitMat(mats));
     ```
   - **Room P (`y = -16`):** Render `Art_P_ExitGlow` on sorting order `-4` (above `Art_P_SewerExitDoor` at `-5`) and shift `Art_P_Banner` to `(190.0f, -2.2f)` clear of `OneWay_P_1` (`x=176..184`) and `OneWay_P_2` (`x=196..204`):
     ```csharp
     // Room P decorations: Sewer Cistern Approach (y = -16..-2)
     CreateGroundedSprite(props, "Art_P_SewerExitDoor", pal.ExitDoorSewer, new Vector2(223f, -16f), 0.85f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_P_ExitGlow", pal.LightRay, new Vector2(223f, -16f), 1.3f, "Ground", -4, ExitMat(mats));
     CreateVerticalBanner(props, "Art_P_Banner", pal, new Vector2(190.0f, -2.2f), 0.85f, mats);
     CreateGroundedSprite(props, "Art_P_Pipe1", pal.AlchemyMachine, new Vector2(174f, -16f), 0.85f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_P_Pipe2", pal.WaterPipe, new Vector2(212f, -16f), 0.85f, "Ground", -5, LitMat(mats));
     ```

5. **`BuildPrefabQRS` (`P0-03` & `P0-04` across Rooms Q, R, S):**
   - **Room Q (`y = 16`):** Place `Art_Q_JudasSeat` at `(-45.5f, 16f)` (`scale = 0.80f`), `Art_Q_SkeletonSpawn` at `(-65.5f, 16f)` (`scale = 0.65f`), and `Art_Q_Trophy` at `(-82.0f, 16f)` (`scale = 0.80f`), all in the open floor bays between `OneWay_Q_1..4`:
     ```csharp
     // Room Q decorations: West Torture Wing (y = 16..30)
     CreateGroundedSprite(props, "Art_Q_JudasSeat", pal.JudasCradle, new Vector2(-45.5f, 16f), 0.80f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_Q_SkeletonSpawn", pal.SkeletonSpawn, new Vector2(-65.5f, 16f), 0.65f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_Q_Trophy", pal.Trophy, new Vector2(-82.0f, 16f), 0.80f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_Q_Cage1", pal.SuspendedCage, new Vector2(-38f, 27.5f), 0.85f, "Ground", -5, LitMat(mats));
     CreateWallSprite(props, "Art_Q_Cage2", pal.SuspendedCage, new Vector2(-78f, 27.5f), 0.85f, "Ground", -5, LitMat(mats));
     ```
   - **Room R (`y = 24`):** Replace shrunk `0.28×` `armory_dome_00` on `Art_R_ArmoryDome` with `pal.PrisonBigAlcove` (`scale = 0.90f`) at `(-60.0f, 24f)`, mount `Art_R_BigSword` on the upper West Armory vault wall at `(-95.5f, 31.5f)` (`scale = 0.55f`) above `I12_Scroll_WestArmory` (`(-95f, 24f)`), and place `Art_R_Banner` at `(-47.5f, 37.8f)`:
     ```csharp
     // Room R decorations: West Armory (y = 24..38)
     CreateGroundedSprite(props, "Art_R_ArmoryDome", pal.PrisonBigAlcove, new Vector2(-60.0f, 24f), 0.90f, "Ground", -6, LitMat(mats));
     CreateWallSprite(props, "Art_R_BigSword", pal.BigSwordLandmark, new Vector2(-95.5f, 31.5f), 0.55f, "Ground", -6, LitMat(mats));
     CreateVerticalBanner(props, "Art_R_Banner", pal, new Vector2(-47.5f, 37.8f), 0.85f, mats);
     CreateGroundedSprite(props, "Art_R_ArmorStand1", pal.ArmorStatue, new Vector2(-44f, 24f), 0.95f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_R_ArmorStand2", pal.ArmorStatue, new Vector2(-82f, 24f), 0.95f, "Ground", -5, LitMat(mats));
     ```
   - **Room S (`y = 24`):** Place `Art_S_AlchemyBibli1` (`scale = 0.80f`) at `(72.0f, 24f)`, `Art_S_BlacksmithFurnace` (`scale = 0.70f`) at `(87.5f, 24f)`, `Art_S_AlchemyBibli2` (`scale = 0.80f`) at `(102.0f, 24f)`, and `Art_S_Collector` (`scale = 0.75f`) at `(114.5f, 24f)` in the 4 open inter-platform floor bays of Room S:
     ```csharp
     // Room S decorations: East Alchemy Lab (y = 24..38)
     CreateGroundedSprite(props, "Art_S_AlchemyBibli1", pal.AlchemyBibli, new Vector2(72.0f, 24f), 0.80f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_S_AlchemyBibli2", pal.AlchemyBibli, new Vector2(102.0f, 24f), 0.80f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_S_BlacksmithFurnace", pal.BlacksmithFurnace, new Vector2(87.5f, 24f), 0.70f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_S_Collector", pal.CollectorFront, new Vector2(114.5f, 24f), 0.75f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_S_Table", pal.AlchemyTable, new Vector2(73.5f, 24f), 0.95f, "Ground", -5, LitMat(mats));
     ```

6. **`BuildPrefabT` (`P0-04` across Room T):**
   - Place `Art_T_CourtyardArch` at `(45.0f, 40f)` (`scale = 0.80f`), `Art_T_AlcovePillar1` at `(-9.0f, 40f)` (`scale = 0.80f`), `Art_T_AlcovePillar2` at `(21.0f, 40f)` (`scale = 0.80f`), `Art_T_CellDoor1` at `(-24.0f, 40f)` (`scale = 0.85f`), `Art_T_CellDoor2` at `(6.0f, 40f)` (`scale = 0.85f`), `Art_T_Banner` at `(6.0f, 61.8f)` (`scale = 0.85f`), `Art_T_Window1` at `(-9.0f, 52.0f)` (`scale = 0.90f`), and `Art_T_Window2` at `(35.5f, 52.0f)` (`scale = 0.90f`):
     ```csharp
     // Room T decorations: Upper Warden Citadel & Courtyard Approach (y = 40..62)
     CreateGroundedSprite(props, "Art_T_CourtyardArch", pal.ExitDoorCourtyard, new Vector2(45.0f, 40f), 0.80f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_T_AlcovePillar1", pal.AlcovePillar, new Vector2(-9.0f, 40f), 0.80f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_T_AlcovePillar2", pal.AlcovePillar, new Vector2(21.0f, 40f), 0.80f, "Ground", -6, LitMat(mats));
     CreateGroundedSprite(props, "Art_T_CellDoor1", pal.CellDoor, new Vector2(-24.0f, 40f), 0.85f, "Ground", -5, LitMat(mats));
     CreateGroundedSprite(props, "Art_T_CellDoor2", pal.CellDoor, new Vector2(6.0f, 40f), 0.85f, "Ground", -5, LitMat(mats));
     CreateVerticalBanner(props, "Art_T_Banner", pal, new Vector2(6.0f, 61.8f), 0.85f, mats);
     CreateWallSprite(props, "Art_T_Window1", pal.WindowLarge, new Vector2(-9.0f, 52.0f), 0.90f, "Ground", -6, LitMat(mats));
     CreateGodRay(props, "Art_T_GodRay1", pal.LightRay, new Vector2(-9.0f, 48.0f), new Vector2(1.8f, 2.6f), -22f, mats);
     CreateWallSprite(props, "Art_T_Window2", pal.WindowLarge, new Vector2(35.5f, 52.0f), 0.90f, "Ground", -6, LitMat(mats));
     CreateGodRay(props, "Art_T_GodRay2", pal.LightRay, new Vector2(35.5f, 48.0f), new Vector2(1.8f, 2.6f), -22f, mats);
     ```

7. **`AddAtmosphericEffects` & `AddRoomWarmSpotlights` (`P0-04` mushroom & torch collision fixes):**
   - In `AddAtmosphericEffects`, move `GlowingMushroom_4` X from `(108f, 0f)` to `(113.8f, 0f)` so it sits on the floor beside `Art_M_ArmoryDome1` (`109.2f`) instead of overlapping it.
   - In `AddRoomWarmSpotlights`, update the colliding torch coordinates in `torchSpecs` so zero `LanternFixture_R5` fixtures overlap windows, chains, pillars, exit doors, or landmarks:
     - `("Light_A_Warm", new Vector2(7.8f, 3.8f), 9.0f, 0.95f)` (was `10f, 3.8f` on `Art_A_Window`)
     - `("Light_C_Warm2", new Vector2(46.0f, 13.5f), 8.5f, 0.90f)` (was `48f, 13.5f` on `Art_C_Window`)
     - `("Light_E_Warm", new Vector2(35.5f, 27.5f), 9.5f, 0.95f)` (was `39f, 27.5f` on `Art_E_Pillar1`)
     - `("Light_E_Warm2", new Vector2(43.5f, 27.5f), 8.5f, 0.90f)` (was `46f, 27.5f` on `Art_I2_Chain`)
     - `("Light_F_Warm2", new Vector2(49.5f, 35.5f), 8.5f, 0.85f)` (was `52f, 35.5f` on `Art_F_ExitArch`)
     - `("Light_I_Warm", new Vector2(14.5f, 19.5f), 8.5f, 0.90f)` (was `12f, 19.5f` on `Art_I_Window`)
     - `("Light_G_Warm2", new Vector2(63.5f, 12.5f), 8.5f, 0.90f)` (was `66f, 12.5f` on `Art_G_Window`)
     - `("Light_N_Warm2", new Vector2(112.5f, 12.5f), 9.0f, 0.90f)` (was `108f, 12.5f` on `Art_N_Window1`)
     - `("Light_O_Warm3", new Vector2(124.0f, -11.5f), 9.0f, 0.85f)` (was `128f, -11.5f` on `Art_I7_Chain`)
     - `("Light_P_Warm4", new Vector2(218.5f, -11.5f), 9.0f, 0.95f)` (was `222f, -11.5f` on `Art_P_SewerExitDoor`)
     - `("Light_Q_Warm3", new Vector2(-62.0f, 20.5f), 9.0f, 0.90f)` (was `-66f, 20.5f` on `Art_Q_SkeletonSpawn`)
     - `("Light_R_Warm2", new Vector2(-46.0f, 28.5f), 9.0f, 0.90f)` (was `-48f, 28.5f` on `Art_I9_Chain`)
     - `("Light_R_Warm5", new Vector2(-93.0f, 28.5f), 8.5f, 0.90f)` (was `-96f, 28.5f` on `Art_R_BigSword`)
     - `("Light_S_Warm1", new Vector2(68.5f, 28.5f), 9.0f, 0.90f)` (was `72f, 28.5f` on `Art_S_AlchemyBibli1`)
     - `("Light_S_Warm3", new Vector2(106.0f, 28.5f), 9.0f, 0.90f)` (was `104f, 28.5f` on `Art_S_AlchemyBibli2`)
     - `("Light_T_Warm2", new Vector2(-12.5f, 44.5f), 9.5f, 0.90f)` (was `-16f, 44.5f` on `Art_I10_Chain`)
     - `("Light_T_Warm3", new Vector2(3.0f, 44.5f), 9.5f, 0.90f)` (was `2f, 44.5f` on `Art_T_Window1`)
     - `("Light_T_Warm4", new Vector2(17.5f, 44.5f), 9.5f, 0.90f)` (was `20f, 44.5f` on `Art_I5_Chain`)
     - `("Light_T_Warm7", new Vector2(4.0f, 54.5f), 9.0f, 0.85f)` (was `8f, 54.5f` on `Art_T_Banner`)
     - `("Light_T_Warm8", new Vector2(31.0f, 54.5f), 9.0f, 0.85f)` (was `32f, 54.5f` on `Art_T_Window2`)

- [ ] **Step 4: Execute `NhaGiamGiengChuongBuilder.BuildAll` to regenerate all 9 room prefabs and `NhaGiamGiengChuong.unity`**

Run via `rtk proxy`:
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -quit -logFile D:/thietkegame/Logs/round7-phase1-buildall.log
```
Expected: Exit code `0`, `[NhaGiamGiengChuongBuilder] Built 9 room prefabs and NhaGiamGiengChuong.unity successfully.` in `Logs/round7-phase1-buildall.log`.

- [ ] **Step 5: Run test `A14` to verify it passes (GREEN)**

Run via `rtk proxy`:
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.EditorTests.MapWiringTests.A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes" -testResults D:/thietkegame/Logs/test-p0-task2-green.xml -logFile D:/thietkegame/Logs/test-p0-task2-green.log
```
Expected: PASS (`result="Passed"`).

---

### Task 3: Full Regression Verification (`Core` EditMode + `NhaGiamGiengChuong` EditMode + PlayMode + GPU Captures)

**Files:**
- Verify: `Logs/r6-gpu-captures/*.png` and `Logs/round7-phase1-*.xml`

- [ ] **Step 1: Refresh GPU verification captures (`CaptureRound6GpuVerificationFrames`)**

Run via `rtk proxy` (without `-nographics` so D3D12 renders all 9 GPU cameras):
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.CaptureRound6GpuVerificationFrames -quit -logFile D:/thietkegame/Logs/round7-phase1-gpu-capture.log
```
Expected: Exit code `0`, all 9 PNGs in `Logs/r6-gpu-captures/` updated with `maxClip <= 0.500%`.

- [ ] **Step 2: Run full EditMode test suite (`DeadCells.Core.Tests` + `DeadCells.NhaGiamGiengChuong.EditorTests`)**

Run via `rtk proxy`:
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -projectPath D:/thietkegame -runTests -testPlatform EditMode -testResults D:/thietkegame/Logs/round7-phase1-editmode.xml -logFile D:/thietkegame/Logs/round7-phase1-editmode.log
```
Expected: `100%` PASS across all EditMode tests (including the 2 new tests).

- [ ] **Step 3: Run full PlayMode test suite (`DeadCells.NhaGiamGiengChuong.PlayTests`)**

Run via `rtk proxy`:
```powershell
rtk proxy "C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe" -batchmode -nographics -projectPath D:/thietkegame -runTests -testPlatform PlayMode -testFilter "DeadCells.NhaGiamGiengChuong.PlayTests" -testResults D:/thietkegame/Logs/round7-phase1-playmode.xml -logFile D:/thietkegame/Logs/round7-phase1-playmode.log
```
Expected: `14/14` PASS (`P01`–`P08` all green).
