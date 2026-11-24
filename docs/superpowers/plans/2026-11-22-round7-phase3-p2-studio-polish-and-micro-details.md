# Round 7 (Phase 3): NhaGiamGiengChuong P2 Studio Polish, Micro-Fixes, Weathered Masonry & Architectural Depth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the final `P2` studio-polish pass (`P2-01` through `P2-06`) across `NhaGiamGiengChuong.unity` and its 9 room prefabs — eliminating the 3 GPU-observed micro-blemishes (lavender chain caps, Room G upper-west torch inside alcove, Room J bed chains crossing `Bay #3`), breaking `backWall_*` and flat ceiling/bedrock repetition via unused 24-PPU masonry/moss/strata assets, and layering 36 clash-free architectural depth props (timber trusses, ornate iron cages, chained prisoner silhouettes, and floor-lip foliage) while preserving 100% pass rate across all 77 existing tests (`78/78` with new `A16`).

**Architecture:**
1. Extend `MapWiringTests.cs` with `A16_Round7Phase3_ChainCapMicroFixesWeatheredMasonryCeilingRhythmDepthPropsAndBedrockStrata` to lock all `P2-01`..`P2-05` visual, sorting, containment, and zero-overlap invariants before regenerating assets.
2. Extend `NhaGiamGiengChuongBuilder.cs`:
   - Load 24-PPU unused Prison sprites (`stampWallBig_0..5_00`, `stampWallMedium_0..5_00`, `stampCeilingBig_0_00`, `stampCeilingSmall_1_00`, `PrisonwoodenBeam_00`, `chainBot_0..2_00`, `wallJunk_14/15/17`, `groundFoliage_0..1_00`) into `PrisonPalette` / `LoadPrisonPalette()` (all verified `24 PPU`, `0` raw `#FF00FF` magenta pixels).
   - Fix `AddPrisonChainArt` (`CapBottom` uses dark iron `pal.ChainTile` at `floorTopY + 0.45f`, scale `(1f, 0.70f, 1f)`), relocate `Art_G_TorchUpperWest` to `x = 65.6f`, and relocate `Art_J_Bed` to `x = 3.6f`.
   - Add `AddWeatheredMasonryOverlays` (`Background / -19`, strict 5×5 `bgTm` non-null guard against all 16 carved bays) and `AddBedrockStrataStamps` (`Ground / -4`, strict 3×3 `pal.Dirt` + `Matrix4x4.identity` guard).
   - Extend `AddCornerBeamsAndWebs` for all 9 long rooms (`width > 28u`: `E, M, N, O, P, Q, R, S, T`) to place multi-span `Art_{roomTag}_CeilStamp_{k}` and `Art_{roomTag}_RoofMoss_{k}` on `Background / -11` (strictly behind `LanternFixture_R5 -9`, cages `Ground / -7`, and `TM_SolidRim -3`), skipping open shafts.
   - Wire all 9 room builders (`CD, AB, G, HJK, ELF, W1, W2, W3, U1`) with weathered wall patches (`19/19` rooms), bedrock strata (`11` skirt calls), and `36` verified clash-free `P2-04` depth props.
3. Rebuild all 9 room prefabs + `NhaGiamGiengChuong.unity` via `NhaGiamGiengChuongBuilder.BuildAll` and run the full low-RAM verification gate (`78/78` EditMode + PlayMode + `9/9` D3D12 GPU captures + visual inspection + `codegraph sync`).

**Tech Stack:** Unity `6000.6.9f1` (URP 2D Renderer, `Tilemap`, `Light2D`), C# Editor Builder (`NhaGiamGiengChuongBuilder`), NUnit EditMode & PlayMode Test Runner, `rtk` CLI wrapper.

**Spec:** `D:/thietkegame/docs/superpowers/specs/2026-11-22-round7-phase3-p2-studio-polish-and-micro-details-design.md`

---

## Global Constraints

- **Shell Wrapper (`AGENTS.md` §0):** Run every shell command via `rtk` or `rtk proxy <command>`. Read/write all files in UTF-8.
- **Low-RAM Unity Batchmode Execution:** Always use `D:/Unity/6000.6.9f1/Editor/Unity.exe`, kill lingering `Unity*`, `bee_backend*`, `AssetImportWorker*` before each launch, pass `-job-worker-count 1 -disable-assembly-updater`, and split EditMode into **4 batches** (`Core.Tests.EditMode`, `NhaGiamGiengChuong` Unit+Geometry, `MapWiringTests` `A07..A10`, `MapWiringTests` `A11..A16`) so Unity never reloads `NhaGiamGiengChuong.unity` more than 6 times in a single process.
- **No `.meta` Fabrication (`AGENTS.md` §2):** Never create, edit, or delete `.meta` files manually.
- **100% Compatibility with Existing 77 Tests:** Do not modify physical colliders, `EncounterZone`, enemies, `I1..I16`, `SD_1..3`, `TP_0..5`, `GroundMist_R5` (`== 6`), `ShadowCasters_R5` (`== 6`), `GlowingMushroom_0..7` (`== 8`), `TM_Background` tile types (`backWall_*` only), `TM_SolidVisual` tiles, `A11` `_PinTopWorldY`, `A15` chiaroscuro lighting parameters, or `PrisonNormalMapGenerator.CoreArchitectureSpriteNames.Length` (`36`).

## Review Focus

- Verify `AddPrisonChainArt` `CapBottom` uses `pal.ChainTile` at `floorTopY + 0.45f` with scale `(1f, 0.70f, 1f)` so world Y spans `[floorTopY + 0.10 .. floorTopY + 0.80]` — strictly above `floorTopY` and overlapping `Link_0` (`[+0.50 .. +1.50]`) seamlessly with zero gap.
- Verify `Art_G_TorchUpperWest` at `65.6f` clears `Art_G_Alcove` (`max.x = 64.85`, `+0.33u`), `Art_G_Winch`, and `Art_I2_Chain`, and keeps Room G `A10` point-light coverage well above `50%` (`~96.4%`).
- Verify `AddWeatheredMasonryOverlays` is called **after** `CarveAndFrameArchBay` in every room builder and checks all 25 cells `[jx-2..jx+2] × [jy-2..jy+2]` on `bgTm` so zero `Art_*_WallPatch_*` sprites ever bleed into any of the 16 carved bays (`Bay #0..#15`).
- Verify `AddCornerBeamsAndWebs` multi-span `CeilStamp_*` and `RoofMoss_*` render on `Background / -11` (strictly behind `LanternFixture_R5` at `Background / -9`, cages at `Ground / -7`, and `TM_SolidRim` at `Ground / -3`), use `mossSpanX <= xMax - 4.0f` (so Room E cleanly gets `17.5f`), and never float over open shafts (`M:[101..107]`, `O:[115..121]`, `Q:[-69..-63]`, `S:[77..83]`).
- Verify `AddBedrockStrataStamps` checks all 9 neighborhood cells `[cx-1..cx+1] × [cy-1..cy+1]` for `solidTm.GetTile(c) == pal.Dirt && solidTm.GetTransformMatrix(c) == Matrix4x4.identity` so zero bedrock rock stamps touch `floorStamp_*` walkable caps, thin `0.5u` ceiling slabs, or playable room air.
- Verify all 36 `P2-04` curated depth props have zero overlap with carved bays, `OneWay_*` platforms (including `ob.min.y - 0.35f .. ob.max.y + 0.10f` padding), `LanternFixture_R5`, and `Window` sprites, and every truss/foliage sits on a solid floor box while every ornate cage hangs from a solid ceiling box.

## File Structure Map

| Action | File Path | Responsibility |
| --- | --- | --- |
| Modify | `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` | Add `[Test] A16_Round7Phase3_ChainCapMicroFixesWeatheredMasonryCeilingRhythmDepthPropsAndBedrockStrata()` enforcing `P2-01`..`P2-05`. |
| Modify | `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` | Load Phase 3 24-PPU Prison sprites, apply 3 `P2-01` micro-fixes, add `AddWeatheredMasonryOverlays` (`P2-02`) and `AddBedrockStrataStamps` (`P2-05`), extend `AddCornerBeamsAndWebs` (`P2-03`), and wire all 9 room builders with `P2-02`/`P2-04`/`P2-05`. |
| Regenerate (via `BuildAll`) | `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/*.prefab`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` | Rebuilt 9 modular room prefabs and scene containing all Phase 3 visual polish. |

---

### Task 1: Add EditMode Regression Gate `A16` in `MapWiringTests.cs` (`P2-06`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs:1385-1395`

**Interfaces:**
- Consumes: `GeometryAuditHelper.ScenePath`, `GeometryAuditHelper.LayerOneWay`, `GeometryAuditHelper.GetWorldBounds`, `GetVisualSpriteBounds` in `MapWiringTests`.
- Produces: `[Test] A16_Round7Phase3_ChainCapMicroFixesWeatheredMasonryCeilingRhythmDepthPropsAndBedrockStrata()`.

- [ ] **Step 1: Append `A16` right after `A15` (before `GetVisualSpriteBounds`) in `MapWiringTests.cs`**

Insert the following test method directly after line `1389` in `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs`:

```csharp
        [Test]
        public void A16_Round7Phase3_ChainCapMicroFixesWeatheredMasonryCeilingRhythmDepthPropsAndBedrockStrata()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

            // 1. P2-01a: All 7 climbing chains use unified dark-iron chainTile_00 CapBottom strictly above floor; zero lavender chainDown_00 anywhere
            foreach (var sr in allRenderers)
            {
                if (sr != null && sr.sprite != null)
                {
                    Assert.AreNotEqual("chainDown_00", sr.sprite.name,
                        $"P2-01a: '{sr.gameObject.name}' must not use mismatched lavender chainDown_00.");
                }
            }

            (string chainName, float floorY)[] expectedChains =
            {
                ("Art_I1_Chain", 0f),
                ("Art_I2_Chain", 0f),
                ("Art_I6_Chain", 16f),
                ("Art_I7_Chain", 0f),
                ("Art_I8_Chain", -16f),
                ("Art_I9_Chain", 16f),
                ("Art_I10_Chain", 24f),
            };
            foreach (var (chainName, floorY) in expectedChains)
            {
                var chainGo = GameObject.Find(chainName);
                Assert.IsNotNull(chainGo, $"P2-01a: Missing climbing chain '{chainName}'.");
                var capSr = chainGo.transform.Find("CapBottom")?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(capSr?.sprite, $"P2-01a: Missing CapBottom SpriteRenderer under '{chainName}'.");
                Assert.AreEqual("chainTile_00", capSr.sprite.name,
                    $"P2-01a: '{chainName}/CapBottom' must use dark-iron chainTile_00, got '{capSr.sprite.name}'.");
                Assert.Greater(capSr.bounds.min.y, floorY + 0.02f,
                    $"P2-01a: '{chainName}/CapBottom' bottom ({capSr.bounds.min.y:F2}) must remain strictly above floorY={floorY:F2}.");
            }

            // 2. P2-01b & P2-01c: Art_G_TorchUpperWest clears Art_G_Alcove; Art_J_Bed clears carved Bay #3 (all ArchSill_*/ArchJamb_*)
            var gTorchFixture = GameObject.Find("Art_G_TorchUpperWest")?.transform.Find("LanternFixture_R5")?.GetComponent<SpriteRenderer>();
            var gAlcove = GameObject.Find("Art_G_Alcove")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(gTorchFixture, "P2-01b: Missing Art_G_TorchUpperWest/LanternFixture_R5.");
            Assert.IsNotNull(gAlcove, "P2-01b: Missing Art_G_Alcove.");
            bool gOverlapX = gTorchFixture.bounds.min.x < gAlcove.bounds.max.x - 0.02f && gTorchFixture.bounds.max.x > gAlcove.bounds.min.x + 0.02f;
            bool gOverlapY = gTorchFixture.bounds.min.y < gAlcove.bounds.max.y - 0.02f && gTorchFixture.bounds.max.y > gAlcove.bounds.min.y + 0.02f;
            Assert.IsFalse(gOverlapX && gOverlapY,
                $"P2-01b: Art_G_TorchUpperWest ({gTorchFixture.bounds.min}..{gTorchFixture.bounds.max}) must not overlap Art_G_Alcove ({gAlcove.bounds.min}..{gAlcove.bounds.max}).");

            var bayFrames = allRenderers
                .Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal) ||
                             sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal))
                .ToArray();
            var jBed = GameObject.Find("Art_J_Bed")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(jBed, "P2-01c: Missing Art_J_Bed.");
            foreach (var frame in bayFrames)
            {
                bool ox = frame.bounds.max.x > jBed.bounds.min.x + 0.05f && frame.bounds.min.x < jBed.bounds.max.x - 0.05f;
                bool oy = frame.bounds.max.y > jBed.bounds.min.y + 0.05f && frame.bounds.min.y < jBed.bounds.max.y - 0.05f;
                Assert.IsFalse(ox && oy,
                    $"P2-01c: Art_J_Bed ({jBed.bounds.min}..{jBed.bounds.max}) must not overlap carved bay frame '{frame.name}'.");
            }

            // 3. P2-02: Weathered masonry wall patches (Background / -19, >= 120 across all 19 rooms, zero overlap with any carved bay)
            var wallPatches = allRenderers
                .Where(sr => sr.gameObject.name.Contains("_WallPatch_"))
                .ToArray();
            Assert.GreaterOrEqual(wallPatches.Length, 120,
                $"P2-02: Expected >= 120 weathered stone wall patches across 19 rooms, found {wallPatches.Length}.");

            string[] allRoomTags = { "A", "B", "C", "D", "G", "H", "J", "K", "E", "L", "F", "M", "N", "O", "P", "Q", "R", "S", "T" };
            foreach (string tag in allRoomTags)
            {
                Assert.IsTrue(wallPatches.Any(sr => sr.gameObject.name.StartsWith($"Art_{tag}_WallPatch_", StringComparison.Ordinal)),
                    $"P2-02: Room {tag} must contain at least 1 weathered stone wall patch.");
            }

            (int bayIdx, float xMin, float xMax, float yMin, float yMax)[] carvedBays =
            {
                (0, 9f, 14f, 2f, 5f), (1, 46f, 52f, 11f, 16f), (2, 69f, 74f, 2f, 6f), (3, -15f, -9f, 18f, 21f),
                (4, 6f, 10f, 25f, 30f), (5, 118f, 126f, 14f, 19f), (6, 100f, 106f, -13f, -8f), (7, -60f, -54f, 30f, 35f),
                (8, 66f, 74f, 46f, 52f), (9, 97f, 102f, 2f, 6f), (10, 138f, 144f, 18f, 22f), (11, 140f, 148f, -9f, -3f),
                (12, 187f, 195f, -9f, -3f), (13, -75f, -69f, 18f, 22f), (14, 47f, 55f, 31f, 37f), (15, 96f, 104f, 47f, 55f),
            };
            foreach (var wp in wallPatches)
            {
                Assert.AreEqual("Background", wp.sortingLayerName, $"P2-02: '{wp.name}' must sort on Background.");
                Assert.AreEqual(-19, wp.sortingOrder, $"P2-02: '{wp.name}' must sort at order -19 (above TM_Background -20, behind TM_WallDecals -18).");
                Assert.IsTrue(wp.sprite != null &&
                              (wp.sprite.name.StartsWith("stampWallBig_", StringComparison.Ordinal) ||
                               wp.sprite.name.StartsWith("stampWallMedium_", StringComparison.Ordinal)),
                    $"P2-02: '{wp.name}' must use stampWallBig_* or stampWallMedium_*, got '{wp.sprite?.name}'.");

                Bounds b = wp.bounds;
                foreach (var bay in carvedBays)
                {
                    bool ox = b.min.x < bay.xMax - 0.05f && b.max.x > bay.xMin + 0.05f;
                    bool oy = b.min.y < bay.yMax - 0.05f && b.max.y > bay.yMin + 0.05f;
                    Assert.IsFalse(ox && oy,
                        $"P2-02: WallPatch '{wp.name}' ({b.min}..{b.max}) bleeds into carved Bay #{bay.bayIdx} ([{bay.xMin}..{bay.xMax}], [{bay.yMin}..{bay.yMax}]).");
                }
            }

            // 4. P2-03: All 9 long rooms (> 28u wide) have multi-span ceiling stamps & roof moss on Background / -11, never floating over open shafts
            (string tag, float shaftMinX, float shaftMaxX)[] longRoomShafts =
            {
                ("E", float.NaN, float.NaN),
                ("M", 101f, 107f),
                ("N", float.NaN, float.NaN),
                ("O", 115f, 121f),
                ("P", float.NaN, float.NaN),
                ("Q", -69f, -63f),
                ("R", float.NaN, float.NaN),
                ("S", 77f, 83f),
                ("T", float.NaN, float.NaN),
            };
            foreach (var (tag, sMin, sMax) in longRoomShafts)
            {
                var extraStamps = allRenderers
                    .Where(sr => sr.gameObject.name.StartsWith($"Art_{tag}_CeilStamp_", StringComparison.Ordinal))
                    .ToArray();
                var extraMoss = allRenderers
                    .Where(sr => sr.gameObject.name.StartsWith($"Art_{tag}_RoofMoss_", StringComparison.Ordinal))
                    .ToArray();
                Assert.IsNotEmpty(extraStamps, $"P2-03: Long room {tag} must have >= 1 multi-span Art_{tag}_CeilStamp_*.");
                Assert.IsNotEmpty(extraMoss, $"P2-03: Long room {tag} must have >= 1 multi-span Art_{tag}_RoofMoss_*.");

                foreach (var sr in extraStamps.Concat(extraMoss))
                {
                    Assert.IsNotNull(sr.sprite, $"P2-03: '{sr.name}' is missing its sprite.");
                    Assert.AreEqual("Background", sr.sortingLayerName, $"P2-03: '{sr.name}' must sort on Background.");
                    Assert.AreEqual(-11, sr.sortingOrder, $"P2-03: '{sr.name}' must sort at -11 (behind LanternFixture_R5 -9 & Cages Ground/-7).");
                    if (!float.IsNaN(sMin))
                    {
                        bool overShaft = sr.bounds.max.x > sMin + 0.05f && sr.bounds.min.x < sMax - 0.05f;
                        Assert.IsFalse(overShaft,
                            $"P2-03: Ceiling accent '{sr.name}' ({sr.bounds.min.x:F2}..{sr.bounds.max.x:F2}) floats over open shaft [{sMin}..{sMax}].");
                    }
                }
            }

            // 5. P2-04: All 36 curated depth props exist on exact sorting layers and have zero clashes with carved bays, OneWay platforms, lanterns, or windows
            string[] phase3Trusses = { "Art_N_Truss1", "Art_N_Truss2", "Art_O_Truss1", "Art_P_Truss1", "Art_R_Truss1", "Art_T_Truss1", "Art_T_Truss2" };
            string[] phase3Cages = { "Art_G_OrnateCage1", "Art_K_OrnateCage1", "Art_N_OrnateCage1", "Art_O_OrnateCage1", "Art_P_OrnateCage1", "Art_R_OrnateCage1", "Art_S_OrnateCage1", "Art_T_OrnateCage1", "Art_T_OrnateCage2" };
            string[] phase3Chained = { "Art_M_Chained1", "Art_N_Chained1", "Art_O_Chained1", "Art_P_Chained1", "Art_Q_Chained1", "Art_R_Chained1", "Art_T_Chained1", "Art_T_Chained2" };
            string[] phase3Foliage =
            {
                "Art_C_Foliage1", "Art_G_Foliage1", "Art_G_Foliage2", "Art_M_Foliage1",
                "Art_O_Foliage1", "Art_O_Foliage2", "Art_O_Foliage3",
                "Art_P_Foliage1", "Art_P_Foliage2", "Art_Q_Foliage1",
                "Art_S_Foliage1", "Art_S_Foliage2",
            };

            var oneWayBoxes = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include)
                .Where(box => box.gameObject.layer == GeometryAuditHelper.LayerOneWay &&
                              box.gameObject.name.StartsWith("OneWay_", StringComparison.Ordinal))
                .ToArray();
            var lanternFixtures = allRenderers.Where(sr => sr.gameObject.name == "LanternFixture_R5").ToArray();
            var windowRenderers = allRenderers.Where(sr => sr.gameObject.name.Contains("Window") && sr.gameObject.name != "GodRay").ToArray();

            foreach (string name in phase3Trusses)
            {
                var sr = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite, $"P2-04: Missing timber truss '{name}'.");
                Assert.AreEqual("PrisonwoodenBeam_00", sr.sprite.name);
                Assert.AreEqual("Background", sr.sortingLayerName);
                Assert.AreEqual(-13, sr.sortingOrder);
            }
            foreach (string name in phase3Cages)
            {
                var sr = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite, $"P2-04: Missing ornate cage '{name}'.");
                StringAssert.StartsWith("chainBot_", sr.sprite.name);
                Assert.AreEqual("Ground", sr.sortingLayerName);
                Assert.AreEqual(-7, sr.sortingOrder);
            }
            foreach (string name in phase3Chained)
            {
                var sr = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite, $"P2-04: Missing chained silhouette '{name}'.");
                StringAssert.StartsWith("wallJunk_", sr.sprite.name);
                Assert.AreEqual("Background", sr.sortingLayerName);
                Assert.AreEqual(-14, sr.sortingOrder);
            }
            foreach (string name in phase3Foliage)
            {
                var sr = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite, $"P2-04: Missing floor-lip foliage '{name}'.");
                StringAssert.StartsWith("groundFoliage_", sr.sprite.name);
                Assert.AreEqual("Ground", sr.sortingLayerName);
                Assert.AreEqual(-2, sr.sortingOrder);
            }

            foreach (string propName in phase3Trusses.Concat(phase3Cages).Concat(phase3Chained).Concat(phase3Foliage))
            {
                var propGo = GameObject.Find(propName);
                Bounds pb = GetVisualSpriteBounds(propGo);
                bool isFoliage = propName.Contains("_Foliage");

                foreach (var frame in bayFrames)
                {
                    bool ox = frame.bounds.max.x > pb.min.x + 0.05f && frame.bounds.min.x < pb.max.x - 0.05f;
                    bool oy = frame.bounds.max.y > pb.min.y + 0.05f && frame.bounds.min.y < pb.max.y - 0.05f;
                    Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps bay frame '{frame.name}'.");
                }

                foreach (var ow in oneWayBoxes)
                {
                    Bounds ob = GeometryAuditHelper.GetWorldBounds(ow);
                    bool ox = pb.min.x < ob.max.x - 0.05f && pb.max.x > ob.min.x + 0.05f;
                    bool oy = pb.min.y < ob.max.y + 0.10f && pb.max.y > ob.min.y - 0.35f;
                    Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps OneWay platform '{ow.name}'.");
                }

                if (!isFoliage)
                {
                    foreach (var lf in lanternFixtures)
                    {
                        bool ox = pb.min.x < lf.bounds.max.x - 0.04f && pb.max.x > lf.bounds.min.x + 0.04f;
                        bool oy = pb.min.y < lf.bounds.max.y - 0.04f && pb.max.y > lf.bounds.min.y + 0.04f;
                        Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps lantern '{lf.transform.parent?.name}'.");
                    }

                    foreach (var wr in windowRenderers)
                    {
                        bool ox = pb.min.x < wr.bounds.max.x - 0.04f && pb.max.x > wr.bounds.min.x + 0.04f;
                        bool oy = pb.min.y < wr.bounds.max.y - 0.04f && pb.max.y > wr.bounds.min.y + 0.04f;
                        Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps window '{wr.name}'.");
                    }
                }
            }

            // 6. P2-05: Subterranean bedrock rock strata stamps (Ground / -4, >= 60, every stamp center inside opaque dirt_00 on TM_SolidVisual)
            var solidTilemaps = Object.FindObjectsByType<Tilemap>()
                .Where(t => t.name == "TM_SolidVisual")
                .ToArray();
            var bedrockStones = allRenderers
                .Where(sr => sr.gameObject.name.Contains("_BedrockStone_"))
                .ToArray();
            Assert.GreaterOrEqual(bedrockStones.Length, 60,
                $"P2-05: Expected >= 60 subterranean bedrock rock strata stamps, found {bedrockStones.Length}.");
            foreach (var bs in bedrockStones)
            {
                Assert.IsNotNull(bs.sprite, $"P2-05: '{bs.name}' is missing its stoneStamp sprite.");
                Assert.AreEqual("Ground", bs.sortingLayerName, $"P2-05: '{bs.name}' must sort on Ground.");
                Assert.AreEqual(-4, bs.sortingOrder, $"P2-05: '{bs.name}' must sort at -4 (above TM_SolidVisual -5, behind TM_SolidRim -3).");
                StringAssert.StartsWith("stoneStamp_", bs.sprite.name, $"P2-05: '{bs.name}' must use stoneStamp_*.");
                Vector3Int cell = new Vector3Int(Mathf.FloorToInt(bs.transform.position.x), Mathf.FloorToInt(bs.transform.position.y), 0);
                var sp = solidTilemaps.Select(tm => tm.GetSprite(cell)).FirstOrDefault(s => s != null);
                Assert.IsNotNull(sp, $"P2-05: Bedrock stamp '{bs.name}' at cell {cell} sits outside TM_SolidVisual.");
                Assert.AreEqual("dirt_00", sp.name, $"P2-05: Bedrock stamp '{bs.name}' at cell {cell} must sit on opaque dirt_00, got '{sp.name}'.");
            }
        }
```

---

### Task 2: Extend `PrisonPalette`, Apply `P2-01` Micro-Fixes, and Add Core Helpers (`P2-02`, `P2-03`, `P2-05`) in `NhaGiamGiengChuongBuilder.cs`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs:93-98, 150-158, 294-298, 355-363, 595-598, 678-680, 2955-2960, 3171-3248`

**Interfaces:**
- Produces:
  - `PrisonPalette` fields: `StampCeilingBig`, `StampCeilingSmall1`, `StampWallBig`, `StampWallMedium`, `WoodenBeamTruss`, `OrnateCages`, `ChainedPrisoners`, `GroundFoliage`.
  - `AddWeatheredMasonryOverlays(GameObject, Tilemap, PrisonPalette, Material, string, float, float, float, float)`
  - `AddBedrockStrataStamps(GameObject, Tilemap, PrisonPalette, Material, string, int, int, int, int, int, int)`
  - Updated `AddPrisonChainArt(...)` (`P2-01a`) and `AddCornerBeamsAndWebs(...)` (`P2-03`).

- [ ] **Step 1: Add Phase 3 sprite fields to `PrisonPalette` (`NhaGiamGiengChuongBuilder.cs:93-96` and `155-157`)**

Under `StampCeilingMedium` (line `94`):
```csharp
            public Sprite StampCeilingSmall;
            public Sprite StampCeilingSmall1;
            public Sprite StampCeilingMedium;
            public Sprite StampCeilingBig;
            public Sprite[] RoofMoss;
            public Sprite[] StampWallBig;
            public Sprite[] StampWallMedium;
            public Sprite[] StoneStamps;
```

Right after `public Sprite[] TpRocks;` (line `156`):
```csharp
            public Sprite[] TpRocks;
            public Sprite WoodenBeamTruss;
            public Sprite[] OrnateCages;
            public Sprite[] ChainedPrisoners;
            public Sprite[] GroundFoliage;
```

- [ ] **Step 2: Load Phase 3 sprites in `LoadPrisonPalette()` (`NhaGiamGiengChuongBuilder.cs:294-297` and `361-363`)**

Under `StampCeilingSmall` (line `294`):
```csharp
                StampCeilingSmall = LoadPrisonSprite("stampCeilingSmall_0_00.png"),
                StampCeilingSmall1 = LoadPrisonSprite("stampCeilingSmall_1_00.png"),
                StampCeilingMedium = LoadPrisonSprite("stampCeilingMedium_0_00.png"),
                StampCeilingBig = LoadPrisonSprite("stampCeilingBig_0_00.png"),
                RoofMoss = Enumerable.Range(0, 3).Select(i => LoadPrisonSprite($"roofMoss_{i}_00.png")).ToArray(),
                StampWallBig = Enumerable.Range(0, 6).Select(i => LoadPrisonSprite($"stampWallBig_{i}_00.png")).ToArray(),
                StampWallMedium = Enumerable.Range(0, 6).Select(i => LoadPrisonSprite($"stampWallMedium_{i}_00.png")).ToArray(),
                StoneStamps = Enumerable.Range(0, 6).Select(i => LoadPrisonSprite($"stoneStamp_{i}_00.png")).ToArray(),
```

Right after `TpRocks = ...` (line `362`):
```csharp
                TpRocks = Enumerable.Range(0, 4).Select(i => LoadPrisonSprite($"tpRock_{i}_00.png")).ToArray(),
                WoodenBeamTruss = LoadPrisonSprite("PrisonwoodenBeam_00.png"),
                OrnateCages = new[]
                {
                    LoadPrisonSprite("chainBot_0_00.png"),
                    LoadPrisonSprite("chainBot_1_00.png"),
                    LoadPrisonSprite("chainBot_2_00.png"),
                },
                ChainedPrisoners = new[]
                {
                    LoadPrisonSprite("wallJunk_14.png"),
                    LoadPrisonSprite("wallJunk_15.png"),
                    LoadPrisonSprite("wallJunk_17.png"),
                },
                GroundFoliage = new[]
                {
                    LoadPrisonSprite("groundFoliage_0_00.png"),
                    LoadPrisonSprite("groundFoliage_1_00.png"),
                },
```

- [ ] **Step 3: Apply the 3 `P2-01` micro-fixes (`BuildRoomG:596`, `BuildRoomHJK:679`, `AddPrisonChainArt:3171-3179`)**

1. In `BuildRoomG` (line `596`), relocate `Art_G_TorchUpperWest` out of `Art_G_Alcove`:
```csharp
            AddStandingTorchBeacon(artRoot, "Art_G_TorchUpperWest", mat, 65.6f, 8f, warmAmber, 6.0f, 1.25f);
```

2. In `BuildRoomHJK` (line `679`), relocate `Art_J_Bed` clear of carved `Bay #3`:
```csharp
            AddGroundedPrisonSprite(artRoot, "Art_J_Bed", pal.WallBed, mat, 3.6f, 16f, 1f, -11, Color.white);
```

3. In `AddPrisonChainArt` (lines `3171-3179`), replace mismatched lavender `pal.ChainDown` with dark-iron `pal.ChainTile`:
```csharp
            // Bottom cap cleanly above floor using dark-iron chainTile_00 (zero floor stab, no lavender chainDown_00 mismatch)
            var capGo = CreateChild(container, "CapBottom");
            capGo.transform.position = new Vector3(x, floorTopY + 0.45f, 0f);
            capGo.transform.localScale = new Vector3(1f, 0.70f, 1f);
            var capSr = capGo.AddComponent<SpriteRenderer>();
            capSr.sprite = pal.ChainTile;
            capSr.sharedMaterial = mat;
            capSr.sortingLayerID = groundLayerId;
            capSr.sortingOrder = -2;
```

- [ ] **Step 4: Add `AddWeatheredMasonryOverlays` (`P2-02`) and `AddBedrockStrataStamps` (`P2-05`) right after `PaintVisualDirtSkirt` (`NhaGiamGiengChuongBuilder.cs:2958`)**

```csharp
        private static void AddWeatheredMasonryOverlays(
            GameObject artRoot,
            Tilemap bgTm,
            PrisonPalette pal,
            Material mat,
            string roomTag,
            float xMin,
            float xMax,
            float yMin,
            float yMax)
        {
            float width = xMax - xMin;
            float height = yMax - yMin;
            int strideX = width >= 30f ? 7 : 6;
            const int strideY = 5;
            int cxMin = Mathf.CeilToInt(xMin) + 2;
            int cxMax = Mathf.FloorToInt(xMax) - 3;
            int cyMin = Mathf.CeilToInt(yMin) + 2;
            int cyMax = Mathf.FloorToInt(yMax) - 3;
            var patchTint = new Color(0.84f, 0.88f, 0.86f, 0.72f);

            int idx = 0;
            for (int cx = cxMin; cx <= cxMax; cx += strideX)
            {
                for (int cy = cyMin; cy <= cyMax; cy += strideY)
                {
                    int jx = cx + (CellHash(cx, cy) % 3) - 1;
                    int jy = cy + (height >= 7f ? (CellHash(cx + 17, cy + 31) % 3) - 1 : 0);
                    bool useBig = height >= 9f && (CellHash(jx * 5, jy * 11) % 2 == 0);
                    float halfExt = useBig ? 1.5f : 1.0f;
                    float centerX = jx + 0.5f;
                    float centerY = jy + 0.5f;

                    if (centerX - halfExt < xMin + 0.6f || centerX + halfExt > xMax - 0.6f)
                        continue;
                    if (centerY - halfExt < yMin + 0.6f || centerY + halfExt > yMax - 0.6f)
                        continue;

                    // Strict 5x5 solid backWall guard so zero masonry patches bleed into carved parallax bays
                    bool clearOfBays = true;
                    for (int nx = jx - 2; nx <= jx + 2 && clearOfBays; nx++)
                    {
                        for (int ny = jy - 2; ny <= jy + 2; ny++)
                        {
                            if (bgTm.GetTile(new Vector3Int(nx, ny, 0)) == null)
                            {
                                clearOfBays = false;
                                break;
                            }
                        }
                    }
                    if (!clearOfBays)
                        continue;

                    Sprite sp = useBig
                        ? pal.StampWallBig[CellHash(jx, jy) % pal.StampWallBig.Length]
                        : pal.StampWallMedium[CellHash(jx, jy) % pal.StampWallMedium.Length];
                    AddPrisonSpriteProp(artRoot, $"Art_{roomTag}_WallPatch_{idx++}", sp, mat, new Vector2(centerX, centerY), 1.0f, -19, patchTint);
                }
            }
        }

        private static void AddBedrockStrataStamps(
            GameObject artRoot,
            Tilemap solidTm,
            PrisonPalette pal,
            Material mat,
            string regionTag,
            int xMin,
            int xMax,
            int yMin,
            int yMax,
            int strideX = 6,
            int strideY = 4)
        {
            var strataTint = new Color(0.46f, 0.50f, 0.56f, 0.40f);
            int idx = 0;

            for (int cx = xMin + 2; cx <= xMax - 3; cx += strideX)
            {
                for (int cy = yMin + 2; cy <= yMax - 3; cy += strideY)
                {
                    bool allOpaqueDirt = true;
                    for (int dx = -1; dx <= 1 && allOpaqueDirt; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            var cell = new Vector3Int(cx + dx, cy + dy, 0);
                            if (solidTm.GetTile(cell) != pal.Dirt || solidTm.GetTransformMatrix(cell) != Matrix4x4.identity)
                            {
                                allOpaqueDirt = false;
                                break;
                            }
                        }
                    }
                    if (!allOpaqueDirt)
                        continue;

                    var sp = pal.StoneStamps[CellHash(cx, cy) % pal.StoneStamps.Length];
                    AddPrisonSpriteProp(artRoot, $"Art_{regionTag}_BedrockStone_{idx++}", sp, mat, new Vector2(cx + 0.5f, cy + 0.5f), 0.90f, -4, strataTint);
                }
            }
        }
```

- [ ] **Step 5: Extend `AddCornerBeamsAndWebs` (`NhaGiamGiengChuongBuilder.cs:3228-3230`) for multi-span ceiling stamps & moss (`P2-03`)**

Insert right after the single `AddScaledCeilingProp(artRoot, $"Art_{roomTag}_RoofMoss", ...)` line (line `3228`):

```csharp
            // Phase 3 (P2-03): Multi-span ceiling masonry & roof moss across long rooms (> 28u) on Background / -11 (strictly behind lanterns & cages)
            if (xMax - xMin > 28f)
            {
                var multiStampSp = isTallRoom ? pal.StampCeilingBig : pal.StampCeilingSmall1;
                float multiStampScale = isTallRoom ? 0.72f : 0.75f;
                float stampHalfW = multiStampSp.bounds.extents.x * multiStampScale;
                int stampIdx = 0;
                int mossIdx = 0;

                for (float spanX = xMin + 9f; spanX <= xMax - 9f + 0.01f; spanX += 18f)
                {
                    bool stampHitsShaft = !float.IsNaN(shaftMinX) &&
                                          spanX + stampHalfW + 0.5f > shaftMinX &&
                                          spanX - stampHalfW - 0.5f < shaftMaxX;
                    if (!stampHitsShaft && Mathf.Abs(spanX - midX) >= 4.0f)
                    {
                        AddScaledCeilingProp(
                            artRoot,
                            $"Art_{roomTag}_CeilStamp_{stampIdx++}",
                            multiStampSp,
                            mat,
                            spanX,
                            ceilingBottom,
                            multiStampScale,
                            -11,
                            new Color(0.90f, 0.92f, 0.90f, 0.92f));
                    }

                    float mossSpanX = spanX + 6.5f;
                    if (mossSpanX <= xMax - 4.0f)
                    {
                        var spanMossSp = pal.RoofMoss[CellHash(Mathf.RoundToInt(mossSpanX), Mathf.RoundToInt(ceilingBottom)) % pal.RoofMoss.Length];
                        float mossHalfW = spanMossSp.bounds.extents.x * 0.75f;
                        bool mossHitsShaft = !float.IsNaN(shaftMinX) &&
                                             mossSpanX + mossHalfW + 0.5f > shaftMinX &&
                                             mossSpanX - mossHalfW - 0.5f < shaftMaxX;
                        if (!mossHitsShaft && Mathf.Abs(mossSpanX - mossX) >= 3.0f)
                        {
                            AddScaledCeilingProp(
                                artRoot,
                                $"Art_{roomTag}_RoofMoss_{mossIdx++}",
                                spanMossSp,
                                mat,
                                mossSpanX,
                                ceilingBottom,
                                0.75f,
                                -11,
                                new Color(0.86f, 0.94f, 0.86f, 0.90f));
                        }
                    }
                }
            }
```

---

### Task 3: Wire All 9 Room Builders with `P2-02` Masonry Patches, `P2-04` Depth Props (36), and `P2-05` Bedrock Strata

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`BuildRoomCD`, `BuildRoomAB`, `BuildRoomG`, `BuildRoomHJK`, `BuildRoomELF`, `BuildRoomW1_EastCourtyard`, `BuildRoomW2_SewerCistern`, `BuildRoomW3_WestBastion`, `BuildRoomU1_UpperPromenade`)

**Interfaces:**
- Consumes: `AddWeatheredMasonryOverlays`, `AddBedrockStrataStamps`, `AddGroundedPrisonSprite`, `AddScaledCeilingProp`, `AddPrisonSpriteProp`, `PrisonPalette`.
- Produces: All 19 rooms dressed with weathered stone overlays, 11 bedrock skirts dressed with strata stamps, and 36 verified clash-free architectural depth props.

- [ ] **Step 1: Wire `BuildRoomCD` (`NhaGiamGiengChuongBuilder.cs:430-472`)**

1. Directly after `CarveAndFrameArchBay(..., 1, 46, 52, 11, 16);` (line `432`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "C", 42f, 56f, 0f, 18.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "D", 22f, 42f, 8f, 13.5f);
```
2. Directly after all `solidTm` painting completes (after `PaintPrisonInteriorHeader(..., "Beam_DH_Header_22", ...);` at line `444`):
```csharp
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "CD_Sub", 42, 56, -8, -1);
```
3. Directly after `AddFloorJunkCluster(artRoot, "Art_D_Junk", ...);` (line `471`):
```csharp
            // Phase 3 (P2-04): Damp floor-lip moss foliage on Floor_C
            AddPrisonSpriteProp(artRoot, "Art_C_Foliage1", pal.GroundFoliage[0], mat, new Vector2(48.2f, 0f), 0.85f, -2, new Color(0.84f, 0.94f, 0.86f, 0.95f));
```

- [ ] **Step 2: Wire `BuildRoomAB` (`NhaGiamGiengChuongBuilder.cs:499-512`)**

1. Directly after `CarveAndFrameArchBay(..., 0, 9, 14, 2, 5);` (line `501`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "A", 0f, 18f, 0f, 5.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "B", 18f, 42f, 0f, 6f);
```
2. Directly after all `solidTm` painting completes (after `PaintPrisonInteriorHeader(..., "Beam_AB_Header_18", ...);` at line `511`):
```csharp
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "AB_West", -16, 0, -8, 15);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "AB_Sub", 0, 42, -8, -1);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "AB_UpperA", 0, 6, 6, 15);
```

- [ ] **Step 3: Wire `BuildRoomG` (`NhaGiamGiengChuongBuilder.cs:560-592`)**

1. Directly after `CarveAndFrameArchBay(..., 2, 69, 74, 2, 6);` (line `561`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "G", 56f, 76f, 0f, 14.5f);
```
2. Directly after all `solidTm` painting completes (after `PaintPrisonCeiling(solidTm, rimTm, pal, 56f, 76f, 14.5f, 15f);` at line `569`):
```csharp
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "G_Sub", 56, 76, -21, -1);
```
3. Directly after `AddFloorJunkCluster(artRoot, "Art_G_Junk", ...);` (line `591`) — note `Art_G_Foliage2` uses `GroundFoliage[1]` (opaque bottom `y = 7.29 > 7.00`) so zero pixels hang below `1.0u`-thick `Floor_G_UpperWest`:
```csharp
            // Phase 3 (P2-04): Ornate iron hanging cage & damp floor-lip moss foliage
            AddScaledCeilingProp(artRoot, "Art_G_OrnateCage1", pal.OrnateCages[1], mat, 69.5f, 14.5f, 0.90f, -7, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_G_Foliage1", pal.GroundFoliage[1], mat, new Vector2(62.2f, 0f), 0.85f, -2, new Color(0.84f, 0.94f, 0.86f, 0.95f));
            AddPrisonSpriteProp(artRoot, "Art_G_Foliage2", pal.GroundFoliage[1], mat, new Vector2(57.5f, 8f), 0.85f, -2, new Color(0.84f, 0.94f, 0.86f, 0.95f));
```

- [ ] **Step 4: Wire `BuildRoomHJK` (`NhaGiamGiengChuongBuilder.cs:642-682`)**

1. Directly after `CarveAndFrameArchBay(..., 3, -15, -9, 18, 21);` (line `645`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "H", 6f, 22f, 8f, 22.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "J", -16f, 6f, 16f, 22.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "K", -30f, -16f, 16f, 30.5f);
```
2. Directly after all `solidTm` painting completes (after `PaintPrisonInteriorHeader(..., "Beam_KE_Header_Neg16", ...);` at line `659`):
```csharp
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "HJK_Sub", -30, 6, 7, 15);
```
3. Directly after `AddFloorJunkCluster(artRoot, "Art_K_Junk", ...);` (line `681`):
```csharp
            // Phase 3 (P2-04): Ornate iron hanging cage in Room K
            AddScaledCeilingProp(artRoot, "Art_K_OrnateCage1", pal.OrnateCages[1], mat, -27.5f, 30.5f, 0.90f, -7, Color.white);
```

- [ ] **Step 5: Wire `BuildRoomELF` (`NhaGiamGiengChuongBuilder.cs:721-726`)**

Directly after `CarveAndFrameArchBay(..., 4, 6, 10, 25, 30);` (line `724`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "E", -16f, 22f, 24f, 31f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "L", 22f, 38f, 24f, 38.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "F", 0f, 22f, 32f, 38f);
```

- [ ] **Step 6: Wire `BuildRoomW1_EastCourtyard` (`NhaGiamGiengChuongBuilder.cs:836-918`)**

1. Directly after `CarveAndFrameArchBay(..., 10, 138, 144, 18, 22);` (line `840`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "M", 76f, 164f, 0f, 7f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "N", 76f, 164f, 8f, 22.5f);
```
2. Directly after `AddFloorJunkCluster(artRoot, "Art_N_Junk2", ...);` (line `917`):
```csharp
            // Phase 3 (P2-04): Structural timber trusses, ornate iron cage, chained wall silhouettes & floor-lip foliage
            var trussTint = new Color(0.88f, 0.86f, 0.84f, 1f);
            var chainedTint = new Color(0.76f, 0.80f, 0.86f, 0.92f);
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddPrisonSpriteProp(artRoot, "Art_M_Chained1", pal.ChainedPrisoners[1], mat, new Vector2(114.8f, 3.8f), 0.78f, -14, chainedTint);
            AddPrisonSpriteProp(artRoot, "Art_M_Foliage1", pal.GroundFoliage[1], mat, new Vector2(111.5f, 0f), 0.85f, -2, foliageTint);
            AddGroundedPrisonSprite(artRoot, "Art_N_Truss1", pal.WoodenBeamTruss, mat, 109.5f, 8f, 0.85f, -13, trussTint);
            AddGroundedPrisonSprite(artRoot, "Art_N_Truss2", pal.WoodenBeamTruss, mat, 135.5f, 8f, 0.85f, -13, trussTint);
            AddScaledCeilingProp(artRoot, "Art_N_OrnateCage1", pal.OrnateCages[0], mat, 95.5f, 22.5f, 0.90f, -7, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_N_Chained1", pal.ChainedPrisoners[0], mat, new Vector2(128.0f, 17.5f), 0.78f, -14, chainedTint);
```

- [ ] **Step 7: Wire `BuildRoomW2_SewerCistern` (`NhaGiamGiengChuongBuilder.cs:964-1051`)**

1. Directly after `CarveAndFrameArchBay(..., 12, 187, 195, -9, -3);` (line `968`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "O", 76f, 164f, -16f, -1f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "P", 164f, 234f, -16f, -1.5f);
```
2. Directly after all `solidTm` painting completes (after `PaintPrisonInteriorHeader(..., "Beam_P_SewerHeader_212", ...);` at line `981`):
```csharp
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "W2_Sub", 75, 238, -22, -17);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "W2_CeilP", 164, 238, -1, 7);
```
3. Directly after `AddFloorJunkCluster(artRoot, "Art_P_Junk2", ...);` (line `1050`):
```csharp
            // Phase 3 (P2-04): Structural timber trusses, ornate iron cages, chained silhouettes & damp sewer floor-lip foliage
            var trussTint = new Color(0.88f, 0.86f, 0.84f, 1f);
            var chainedTint = new Color(0.76f, 0.80f, 0.86f, 0.92f);
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddGroundedPrisonSprite(artRoot, "Art_O_Truss1", pal.WoodenBeamTruss, mat, 110.5f, -16f, 0.85f, -13, trussTint);
            AddScaledCeilingProp(artRoot, "Art_O_OrnateCage1", pal.OrnateCages[2], mat, 102.5f, -1f, 0.90f, -7, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_O_Chained1", pal.ChainedPrisoners[2], mat, new Vector2(157.5f, -7.2f), 0.78f, -14, chainedTint);
            AddPrisonSpriteProp(artRoot, "Art_O_Foliage1", pal.GroundFoliage[0], mat, new Vector2(93.0f, -16f), 0.85f, -2, foliageTint);
            AddPrisonSpriteProp(artRoot, "Art_O_Foliage2", pal.GroundFoliage[1], mat, new Vector2(122.5f, -16f), 0.85f, -2, foliageTint);
            AddPrisonSpriteProp(artRoot, "Art_O_Foliage3", pal.GroundFoliage[0], mat, new Vector2(145.5f, -16f), 0.85f, -2, foliageTint);

            AddGroundedPrisonSprite(artRoot, "Art_P_Truss1", pal.WoodenBeamTruss, mat, 199.8f, -16f, 0.85f, -13, trussTint);
            AddScaledCeilingProp(artRoot, "Art_P_OrnateCage1", pal.OrnateCages[1], mat, 169.5f, -1.5f, 0.90f, -7, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_P_Chained1", pal.ChainedPrisoners[0], mat, new Vector2(182.0f, -7.2f), 0.78f, -14, chainedTint);
            AddPrisonSpriteProp(artRoot, "Art_P_Foliage1", pal.GroundFoliage[1], mat, new Vector2(180.5f, -16f), 0.85f, -2, foliageTint);
            AddPrisonSpriteProp(artRoot, "Art_P_Foliage2", pal.GroundFoliage[0], mat, new Vector2(203.2f, -16f), 0.85f, -2, foliageTint);
```

- [ ] **Step 8: Wire `BuildRoomW3_WestBastion` (`NhaGiamGiengChuongBuilder.cs:1101-1168`)**

1. Directly after `CarveAndFrameArchBay(..., 13, -75, -69, 18, 22);` (line `1104`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "Q", -102f, -30f, 16f, 23f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "R", -102f, -30f, 24f, 38.5f);
```
2. Directly after all `solidTm` painting completes (after `PaintPrisonInteriorHeader(..., "Beam_R_ArmoryHeader_Neg88", ...);` at line `1122`):
```csharp
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "W3_Sub", -106, -30, 7, 15);
```
3. Directly after `AddFloorJunkCluster(artRoot, "Art_R_Junk2", ...);` (line `1167`):
```csharp
            // Phase 3 (P2-04): Structural timber truss, ornate iron cage, chained silhouettes & floor-lip foliage
            var trussTint = new Color(0.88f, 0.86f, 0.84f, 1f);
            var chainedTint = new Color(0.76f, 0.80f, 0.86f, 0.92f);
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddPrisonSpriteProp(artRoot, "Art_Q_Chained1", pal.ChainedPrisoners[1], mat, new Vector2(-53.8f, 19.6f), 0.78f, -14, chainedTint);
            AddPrisonSpriteProp(artRoot, "Art_Q_Foliage1", pal.GroundFoliage[1], mat, new Vector2(-64.2f, 16f), 0.85f, -2, foliageTint);
            AddGroundedPrisonSprite(artRoot, "Art_R_Truss1", pal.WoodenBeamTruss, mat, -98.5f, 24f, 0.85f, -13, trussTint);
            AddScaledCeilingProp(artRoot, "Art_R_OrnateCage1", pal.OrnateCages[2], mat, -69.5f, 38.5f, 0.90f, -7, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_R_Chained1", pal.ChainedPrisoners[2], mat, new Vector2(-64.0f, 32.5f), 0.78f, -14, chainedTint);
```

- [ ] **Step 9: Wire `BuildRoomU1_UpperPromenade` (`NhaGiamGiengChuongBuilder.cs:1216-1305`)**

1. Directly after `CarveAndFrameArchBay(..., 15, 96, 104, 47, 55);` (line `1220`):
```csharp
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "S", 38f, 92f, 24f, 39f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "T", 38f, 140f, 40f, 62.5f);
```
2. Directly after all `solidTm` painting completes (after `PaintPrisonOuterWall(solidTm, pal, 140f, 140.5f, 39f, 63f);` at line `1239`):
```csharp
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "U1_SubE", 56, 76, 15, 23);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "U1_Overhang", 92, 144, 31, 39);
```
3. Directly after `AddFloorJunkCluster(artRoot, "Art_T_Junk3", ...);` (line `1304`):
```csharp
            // Phase 3 (P2-04): Structural timber trusses, ornate iron cages, chained silhouettes & floor-lip foliage
            var trussTint = new Color(0.88f, 0.86f, 0.84f, 1f);
            var chainedTint = new Color(0.76f, 0.80f, 0.86f, 0.92f);
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddScaledCeilingProp(artRoot, "Art_S_OrnateCage1", pal.OrnateCages[0], mat, 69.5f, 39f, 0.90f, -7, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_S_Foliage1", pal.GroundFoliage[0], mat, new Vector2(42.2f, 24f), 0.85f, -2, foliageTint);
            AddPrisonSpriteProp(artRoot, "Art_S_Foliage2", pal.GroundFoliage[1], mat, new Vector2(75.5f, 24f), 0.85f, -2, foliageTint);

            AddGroundedPrisonSprite(artRoot, "Art_T_Truss1", pal.WoodenBeamTruss, mat, 65.8f, 40f, 0.85f, -13, trussTint);
            AddGroundedPrisonSprite(artRoot, "Art_T_Truss2", pal.WoodenBeamTruss, mat, 120.2f, 40f, 0.85f, -13, trussTint);
            AddScaledCeilingProp(artRoot, "Art_T_OrnateCage1", pal.OrnateCages[2], mat, 76.5f, 62.5f, 0.90f, -7, Color.white);
            AddScaledCeilingProp(artRoot, "Art_T_OrnateCage2", pal.OrnateCages[0], mat, 127.0f, 62.5f, 0.90f, -7, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_T_Chained1", pal.ChainedPrisoners[0], mat, new Vector2(63.5f, 55.5f), 0.78f, -14, chainedTint);
            AddPrisonSpriteProp(artRoot, "Art_T_Chained2", pal.ChainedPrisoners[2], mat, new Vector2(109.5f, 55.5f), 0.78f, -14, chainedTint);
```

---

### Task 4: Rebuild Scene & Prefabs, Run Low-RAM Full Verification (`78/78` + `9/9` GPU Captures), Visually Inspect Frames & Sync CodeGraph

**Files:**
- Regenerated: `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/*.prefab`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity`
- Output Logs & Captures: `Logs/round7-phase3-*.{log,xml}`, `Logs/r6-gpu-captures/gpu_r6_ng_01..09_*.png`

- [ ] **Step 1: Rebuild all 9 room prefabs & `NhaGiamGiengChuong.unity` (low-RAM batchmode)**

```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -quit -job-worker-count 1 -disable-assembly-updater -projectPath 'D:/thietkegame' -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -logFile 'D:/thietkegame/Logs/round7-phase3-buildall.log'; exit $LASTEXITCODE"
```
Expected: Exit code `0`; `Logs/round7-phase3-buildall.log` ends with `[NhaGiamGiengChuongBuilder] BuildAll completed cleanly.`

- [ ] **Step 2: Run EditMode Batch 1 — `Core.Tests.EditMode` (`30/30` expected)**

```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath 'D:/thietkegame' -runTests -testPlatform EditMode -assemblyNames 'DeadCells.Core.Tests.EditMode' -testResults 'D:/thietkegame/Logs/round7-phase3-editmode-core.xml' -logFile 'D:/thietkegame/Logs/round7-phase3-editmode-core.log'; exit $LASTEXITCODE"
```

- [ ] **Step 3: Run EditMode Batch 2 — `NhaGiamGiengChuong` Unit + Geometry (`30/30` expected)**

```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath 'D:/thietkegame' -runTests -testPlatform EditMode -testFilter 'DeadCells.NhaGiamGiengChuong.Tests.EditMode.MapGeometryTests;DeadCells.NhaGiamGiengChuong.Tests.EditMode.ProgressionLogicTests' -testResults 'D:/thietkegame/Logs/round7-phase3-editmode-geom.xml' -logFile 'D:/thietkegame/Logs/round7-phase3-editmode-geom.log'; exit $LASTEXITCODE"
```

- [ ] **Step 4: Run EditMode Batch 3 — `MapWiringTests` `A07..A10` (`4/4` expected)**

```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath 'D:/thietkegame' -runTests -testPlatform EditMode -testFilter 'A07_;A08_;A09_;A10_' -testResults 'D:/thietkegame/Logs/round7-phase3-editmode-wiring1.xml' -logFile 'D:/thietkegame/Logs/round7-phase3-editmode-wiring1.log'; exit $LASTEXITCODE"
```

- [ ] **Step 5: Run EditMode Batch 4 — `MapWiringTests` `A11..A16` (`6/6` expected)**

```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath 'D:/thietkegame' -runTests -testPlatform EditMode -testFilter 'A11_;A12_;A13_;A14_;A15_;A16_' -testResults 'D:/thietkegame/Logs/round7-phase3-editmode-wiring2.xml' -logFile 'D:/thietkegame/Logs/round7-phase3-editmode-wiring2.log'; exit $LASTEXITCODE"
```

- [ ] **Step 6: Run D3D12 GPU Verification Capture (`9/9` frames, WITHOUT `-nographics`) & Visually Inspect PNGs**

```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -quit -job-worker-count 1 -disable-assembly-updater -projectPath 'D:/thietkegame' -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.CaptureRound6GpuVerificationFrames -logFile 'D:/thietkegame/Logs/round7-phase3-gpu-capture.log'; exit $LASTEXITCODE"
```
Expected: `captured=9/9 shaderErrors=0 preFlight=5/5 defaultMaterialRenderers=0 maxClip <= 0.500%`.  
Open and visually inspect `Logs/r6-gpu-captures/gpu_r6_ng_01..09_*.png` via `read_file` to confirm:
- Zero lavender chain bottom caps in `02_room_cd` and `03_room_g`.
- `Art_G_TorchUpperWest` sits cleanly right of `Art_G_Alcove` in `03_room_g`.
- `Art_J_Bed` sits cleanly on solid east brick wall, zero chains over `Bay #3` in `04_room_hjk`.
- Weathered stone wall patches (`Background/-19`), multi-span ceiling stamps/moss (`Background/-11`), trusses, ornate iron cages, chained silhouettes, floor-lip foliage, and bedrock rock strata (`Ground/-4`) read crisply with zero z-fighting or clutter.

- [ ] **Step 7: Run PlayMode Verification (`8/8` expected → `78/78` Total)**

```powershell
rtk proxy pwsh -NoProfile -Command "Get-Process Unity*,bee_backend*,AssetImportWorker* -ErrorAction SilentlyContinue | Stop-Process -Force; & 'D:/Unity/6000.6.9f1/Editor/Unity.exe' -batchmode -nographics -job-worker-count 1 -disable-assembly-updater -projectPath 'D:/thietkegame' -runTests -testPlatform PlayMode -assemblyNames 'DeadCells.NhaGiamGiengChuong.Tests.PlayMode' -testResults 'D:/thietkegame/Logs/round7-phase3-playmode.xml' -logFile 'D:/thietkegame/Logs/round7-phase3-playmode.log'; exit $LASTEXITCODE"
```

- [ ] **Step 8: Sync CodeGraph Index**

```powershell
rtk proxy cmd /c codegraph sync
```
