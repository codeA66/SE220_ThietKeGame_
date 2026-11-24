# Round 7 (Phase 2): NhaGiamGiengChuong P1 Architectural Rhythm, Carved Parallax Bays, Storytelling Clusters & Chiaroscuro Lighting — Design Spec

- **Date:** 2026-11-22
- **Target Scene:** `Assets/_Project/Scenes/NhaGiamGiengChuong.unity`
- **Target Builders:** `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`, `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs`
- **Target Prefabs:** `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{CD,AB,G,HJK,ELF,W1_EastCourtyard,W2_SewerCistern,W3_WestBastion,U1_UpperPromenade}.prefab`
- **Prerequisite:** Round 7 Phase 1 (`P0-01`..`P0-04`, `76/76` EditMode & PlayMode tests green).

---

## 1. Problem Statement & Scope (`P1-01` through `P1-05`)

Phase 1 eliminated all `P0` defects (outer spawn void, chromakey `#FF00FF` on doors/switches/teleporters, toy-scale `0.28×` domes, and prop/platform/torch AABB collisions). Phase 2 targets the five **`P1` Visual & Spatial Architecture defects** identified in the Dead Cells *Prisoners' Quarters (`PrisonStart`)* audit:

1. **`P1-01` — Empty Wall Syndrome in Expansion Wings (`54%–71.8%` bare green `backWall_*`):**
   - Rooms `M` (`88u`), `P` (`70u`), `Q` (`72u`), and `S` (`54u`) currently have **0 carved parallax arch bays**, and `N` (`88u`), `O` (`88u`), `R` (`72u`), `T` (`102u`) have only **1 bay each** (total 9 bays across the entire 340u × 86u map).
2. **`P1-02` — Missing Architectural Pier Rhythm, Windows & God-Rays in Expansion Wings (`M..T`):**
   - While the Core Spine (`AB, CD, G, HJK, ELF`) uses `AddWindowWithGodray`, `AddShelfWithFioles`, `AddFloorJunkCluster`, `pal.WallBed`, and `pal.BannerBig`, the 4 Expansion Wings (`W1_EastCourtyard`, `W2_SewerCistern`, `W3_WestBastion`, `U1_UpperPromenade`) have **0 windows with god-rays**, **0 potion shelves**, **0 cell beds**, **0 `BannerBig` wall banners**, and **0 floor junk clusters**.
3. **`P1-03` — Sparse Environmental Storytelling Clusters in Wings (`M..T`):**
   - Long wing corridors lack thematic Dead Cells storytelling clusters (barracks bunks + shelves in `M`, upper gallery buttress piers + banners in `N`, crypt pillars + hanging cages in `O/P`, torture cell bunks + shelves in `Q/R`, double alchemy apothecary shelves in `S`, and high bastion arches + cages + banners in `T`).
4. **`P1-04` — Flat Streetlight Uniformity (`88` lights clamped to `intensity >= 1.40f`):**
   - `AddStandingTorchBeacon` and `AddCeilingLanternBeacon` currently enforce `Mathf.Max(intensity, 1.40f)` with wide `falloffIntensity = 0.45f` and `Global Light 2D` `intensity = 0.60f`, forcing all 88 beacons across the map to blast at uniform `1.40f` intensity and washing out Dead Cells chiaroscuro contrast.
5. **`P1-05` — Residual Magenta Chromakey on `alchemyBibli_00.png` & Unbraced Wing Platforms:**
   - `alchemyBibli_00.png` (used by `Art_S_AlchemyBibli1` and `Art_S_AlchemyBibli2`) still contains `113` hot-magenta chromakey pixels (`0.1%`).
   - Six elevated wing `OneWay` platforms (`OneWay_O_ShaftStep1`, `OneWay_O_ShaftStep2`, `OneWay_S_Gallery`, `OneWay_T_Mid1`, `OneWay_T_Mid2`, `OneWay_T_High`) lack `AddPlatformCornerBrace` supports.

---

## 2. Non-Breaking Invariants (100% Backward Compatibility with `76/76` Tests)

- **Physical Colliders (`A01`–`A06`, `A05b`–`A05e`, `P01`–`P08`):** Zero changes to any `BoxCollider2D` or `PolygonCollider2D` on layers `6` (`Ground`), `9` (`OneWayPlatform`), or `10` (`Climbable`), zero changes to `EncounterZone` bounds, and zero changes to interactive object coordinates (`TP_0..5`, `SD_1..3`, `I1..I16`).
- **`A08` Grounded Landmarks:** All 17 grounded props checked by `A08` remain at their exact `floorY`.
- **`A10` Point-Light Coverage:** All 88 point lights keep their exact `pointLightOuterRadius` (`>= 6.0f..10.5f`) and `intensity > 0.01f`, preserving `19/19` rooms `>= 50%` coverage and `96.5% >= 75%` total coverage.
- **`A11` & `A12` Arch Bays, Banners, Mushrooms & Lighting:**
  - `ArchSill_* >= 45` and `ArchJamb_* >= 70` (increases from 9 bays to 16 bays: `99` sills/lintels each, `154` jambs).
  - None of the 16 bays' `ArchSill_*` or `ArchJamb_*` overlap any of the 41 `landmarkNames` in `A12`.
  - `Global Light 2D` `color.r <= 0.76f`, point light `volumeIntensity <= 0.10f`, `intensity <= 1.50f`.
- **`A14` Prop/Platform/Torch Disjointness:**
  - None of the new `Art_*_Window*` (`n.Contains("Window")`) overlap any `LanternFixture_R5`.
  - None of the new props overlap any of the 31 `OneWay_*` platforms or `ArchSill_*`/`ArchJamb_*`.

---

## 3. Exact Design Specifications

### 3.1 `P1-05a`: Scrub Residual Chromakey Magenta from `alchemyBibli_00.png`
- In `PrisonPropSpriteGenerator.SanitizeChromakeyPrisonSprites()`, add:
  `("alchemyBibli_00.png", new Color32(14, 20, 28, 255))`
  so all 113 hot-magenta pixels in `alchemyBibli_00.png` are converted to deep shelf shadow `(14, 20, 28, a)` and its normal map is updated if present.
- In `VisualShaderAndNormalTests.PrisonChromakeyProps_HaveZeroRawMagentaPixels()`, add `"Assets/_Project/Art/Environment/Prison/Sprites/alchemyBibli_00.png"` to `chromakeyAuditPaths` (6 sprites total).

### 3.2 `P1-01`: Seven Additional Carved Parallax Arch Bays (`Bay #9`..`Bay #15`, Total 16 Bays)
Add `CarveAndFrameArchBay` calls in the 4 expansion wing builders (every bay mathematically clear of all 41 `A12` landmarks, 31 `OneWay` platforms, chains, and torches):
1. **`Bay #9` (`Room M` in `BuildRoomW1_EastCourtyard`):**
   - `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 9, 97, 102, 2, 6);`
   - Shift `mFloorXs[1]` from `100.5f` to `96.2f` (`mFloorXs = { 84.5f, 96.2f, 123.0f, 146.5f, 162.8f }`) so `Art_M_Torch_1` mounts on the solid brick pier at `x = 96.2f` outside `Bay #9` `[97..102]`.
2. **`Bay #10` (`Room N` in `BuildRoomW1_EastCourtyard`):**
   - `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 10, 138, 144, 18, 22);`
3. **`Bay #11` (`Room O` in `BuildRoomW2_SewerCistern`):**
   - `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 11, 140, 148, -9, -3);`
4. **`Bay #12` (`Room P` in `BuildRoomW2_SewerCistern`):**
   - `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 12, 187, 195, -9, -3);`
5. **`Bay #13` (`Room Q` in `BuildRoomW3_WestBastion`):**
   - `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 13, -75, -69, 18, 22);`
   - Shift `qFloorXs[1]` from `-73.0f` to `-76.2f` (`qFloorXs = { -98.5f, -76.2f, -56.0f, -34.0f }`) so `Art_Q_Torch_1` mounts on solid brick outside `Bay #13` `[-75..-69]`.
6. **`Bay #14` (`Room S` in `BuildRoomU1_UpperPromenade`):**
   - `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 14, 47, 55, 31, 37);`
7. **`Bay #15` (`Room T` in `BuildRoomU1_UpperPromenade`):**
   - `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 15, 96, 104, 47, 55);`

### 3.3 `P1-02`, `P1-03` & `P1-05b`: Wing Windows with God-Rays, Buttress Piers, Storytelling Clusters & Platform Braces
Add the following non-overlapping 24-PPU props across `BuildRoomW1_EastCourtyard`, `BuildRoomW2_SewerCistern`, `BuildRoomW3_WestBastion`, and `BuildRoomU1_UpperPromenade`:

- **`BuildRoomW1_EastCourtyard` (`Rooms M & N`):**
  - **Room M Windows & GodRays:**
    - `AddWindowWithGodray(artRoot, "Art_M_Window1", mat, unlitMat, pal, new Vector2(80.5f, 4.2f));`
    - `AddWindowWithGodray(artRoot, "Art_M_Window2", mat, unlitMat, pal, new Vector2(130.5f, 4.2f));`
    - `AddWindowWithGodray(artRoot, "Art_M_Window3", mat, unlitMat, pal, new Vector2(158.0f, 4.2f));`
  - **Room M Storytelling Clusters (Guard Barracks):**
    - `AddGroundedPrisonSprite(artRoot, "Art_M_Bed1", pal.WallBed, mat, 88.5f, 0f, 0.90f, -11, Color.white);`
    - `AddShelfWithFioles(artRoot, "Art_M_Shelf1", pal, mat, 92.5f, 1.4f, -11);`
    - `AddGroundedPrisonSprite(artRoot, "Art_M_Bed2", pal.WallBed, mat, 138.0f, 0f, 0.90f, -11, Color.white);`
    - `AddFloorJunkCluster(artRoot, "Art_M_Junk1", pal, mat, 86.0f, 0f, 1);`
    - `AddFloorJunkCluster(artRoot, "Art_M_Junk2", pal, mat, 131.5f, 0f, 4);`
  - **Room N Windows & GodRays:**
    - `AddWindowWithGodray(artRoot, "Art_N_Window1", mat, unlitMat, pal, new Vector2(80.5f, 18.2f));`
    - `AddWindowWithGodray(artRoot, "Art_N_Window2", mat, unlitMat, pal, new Vector2(99.5f, 18.2f));`
    - `AddWindowWithGodray(artRoot, "Art_N_Window3", mat, unlitMat, pal, new Vector2(158.0f, 18.2f));`
  - **Room N Piers, Banners & Storytelling:**
    - `AddGroundedPrisonSprite(artRoot, "Art_N_Pillar1", pal.SpawnerPillar, mat, 98.5f, 8f, 0.90f, -13, Color.white);`
    - `AddGroundedPrisonSprite(artRoot, "Art_N_Pillar2", pal.SpawnerPillar, mat, 131.5f, 8f, 0.90f, -13, Color.white);`
    - `AddPrisonSpriteProp(artRoot, "Art_N_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(91.0f, 17.5f), 0.65f, -12, Color.white);`
    - `AddShelfWithFioles(artRoot, "Art_N_Shelf1", pal, mat, 113.0f, 10.6f, -11);`
    - `AddFloorJunkCluster(artRoot, "Art_N_Junk1", pal, mat, 78.5f, 8f, 2);`
    - `AddFloorJunkCluster(artRoot, "Art_N_Junk2", pal, mat, 129.0f, 8f, 5);`

- **`BuildRoomW2_SewerCistern` (`Rooms O & P`):**
  - **Platform Corner Braces (`P1-05b`):**
    - `AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep1BraceL", pal.WoodenCornerLeft, mat, 112f, -11f, true);`
    - `AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep1BraceR", pal.WoodenCornerRight, mat, 124f, -11f, false);`
    - `AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep2BraceL", pal.WoodenCornerLeft, mat, 112f, -5.5f, true);`
    - `AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep2BraceR", pal.WoodenCornerRight, mat, 124f, -5.5f, false);`
  - **Room O Windows, Piers, Cages & Storytelling:**
    - `AddWindowWithGodray(artRoot, "Art_O_Window1", mat, unlitMat, pal, new Vector2(86.0f, -6.5f));`
    - `AddWindowWithGodray(artRoot, "Art_O_Window2", mat, unlitMat, pal, new Vector2(150.5f, -6.5f));`
    - `AddGroundedPrisonSprite(artRoot, "Art_O_Pillar1", pal.SpawnerPillar, mat, 98.0f, -16f, 0.85f, -13, Color.white);`
    - `AddScaledCeilingProp(artRoot, "Art_O_Cage1", pal.SuspendedCage, mat, 84.0f, -1f, 1.0f, -7, Color.white);`
    - `AddScaledCeilingProp(artRoot, "Art_O_Cage2", pal.SuspendedCage, mat, 126.0f, -1f, 1.0f, -7, Color.white);`
    - `AddGroundedPrisonSprite(artRoot, "Art_O_Bed1", pal.WallBed, mat, 84.5f, -16f, 0.90f, -11, Color.white);`
    - `AddShelfWithFioles(artRoot, "Art_O_Shelf1", pal, mat, 143.5f, -13.5f, -11);`
    - `AddFloorJunkCluster(artRoot, "Art_O_Junk1", pal, mat, 95.0f, -16f, 3);`
    - `AddFloorJunkCluster(artRoot, "Art_O_Junk2", pal, mat, 148.0f, -16f, 6);`
  - **Room P Windows, Arches, Cages & Storytelling:**
    - `AddWindowWithGodray(artRoot, "Art_P_Window1", mat, unlitMat, pal, new Vector2(173.5f, -6.5f));`
    - `AddWindowWithGodray(artRoot, "Art_P_Window2", mat, unlitMat, pal, new Vector2(207.0f, -6.5f));`
    - `AddGroundedPrisonSprite(artRoot, "Art_P_Arch1", pal.ArcheSmall, mat, 207.0f, -16f, 0.90f, -14, Color.white);`
    - `AddScaledCeilingProp(artRoot, "Art_P_Cage1", pal.SuspendedCage, mat, 186.0f, -1.5f, 1.0f, -7, Color.white);`
    - `AddScaledCeilingProp(artRoot, "Art_P_Cage2", pal.SuspendedCage, mat, 206.0f, -1.5f, 1.0f, -7, Color.white);`
    - `AddShelfWithFioles(artRoot, "Art_P_Shelf1", pal, mat, 189.5f, -13.5f, -11);`
    - `AddFloorJunkCluster(artRoot, "Art_P_Junk1", pal, mat, 176.5f, -16f, 1);`
    - `AddFloorJunkCluster(artRoot, "Art_P_Junk2", pal, mat, 213.5f, -16f, 8);`

- **`BuildRoomW3_WestBastion` (`Rooms Q & R`):**
  - **Room Q Windows & Storytelling:**
    - `AddWindowWithGodray(artRoot, "Art_Q_Window1", mat, unlitMat, pal, new Vector2(-63.5f, 20.4f));`
    - `AddWindowWithGodray(artRoot, "Art_Q_Window2", mat, unlitMat, pal, new Vector2(-42.5f, 20.4f));`
    - `AddShelfWithFioles(artRoot, "Art_Q_Shelf1", pal, mat, -84.5f, 17.3f, -11);`
    - `AddGroundedPrisonSprite(artRoot, "Art_Q_Bed1", pal.WallBed, mat, -80.0f, 16f, 0.85f, -11, Color.white);`
    - `AddFloorJunkCluster(artRoot, "Art_Q_Junk1", pal, mat, -86.5f, 16f, 2);`
    - `AddFloorJunkCluster(artRoot, "Art_Q_Junk2", pal, mat, -44.0f, 16f, 7);`
  - **Room R Windows, Banners, Piers & Storytelling:**
    - `AddWindowWithGodray(artRoot, "Art_R_Window1", mat, unlitMat, pal, new Vector2(-73.5f, 33.5f));`
    - `AddWindowWithGodray(artRoot, "Art_R_Window2", mat, unlitMat, pal, new Vector2(-33.5f, 33.5f));`
    - `AddPrisonSpriteProp(artRoot, "Art_R_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(-80.0f, 34.0f), 0.65f, -12, Color.white);`
    - `AddGroundedPrisonSprite(artRoot, "Art_R_Pillar1", pal.SpawnerPillar, mat, -38.5f, 24f, 0.90f, -13, Color.white);`
    - `AddShelfWithFioles(artRoot, "Art_R_Shelf1", pal, mat, -49.0f, 25.8f, -11);`
    - `AddFloorJunkCluster(artRoot, "Art_R_Junk1", pal, mat, -84.5f, 24f, 4);`
    - `AddFloorJunkCluster(artRoot, "Art_R_Junk2", pal, mat, -45.0f, 24f, 9);`

- **`BuildRoomU1_UpperPromenade` (`Rooms S & T`):**
  - **Platform Corner Braces (`P1-05b`):**
    - `AddPlatformCornerBrace(artRoot, "Art_S_GalleryBraceL", pal.WoodenCornerLeft, mat, 44f, 28.5f, true);`
    - `AddPlatformCornerBrace(artRoot, "Art_S_GalleryBraceR", pal.WoodenCornerRight, mat, 58f, 28.5f, false);`
    - `AddPlatformCornerBrace(artRoot, "Art_T_Mid1BraceL", pal.WoodenCornerLeft, mat, 52f, 44.5f, true);`
    - `AddPlatformCornerBrace(artRoot, "Art_T_Mid1BraceR", pal.WoodenCornerRight, mat, 64f, 44.5f, false);`
    - `AddPlatformCornerBrace(artRoot, "Art_T_Mid2BraceL", pal.WoodenCornerLeft, mat, 92f, 44.5f, true);`
    - `AddPlatformCornerBrace(artRoot, "Art_T_Mid2BraceR", pal.WoodenCornerRight, mat, 106f, 44.5f, false);`
    - `AddPlatformCornerBrace(artRoot, "Art_T_HighBraceL", pal.WoodenCornerLeft, mat, 112f, 49f, true);`
    - `AddPlatformCornerBrace(artRoot, "Art_T_HighBraceR", pal.WoodenCornerRight, mat, 126f, 49f, false);`
  - **Room S Windows, Shelves, Banners, Cages & Debris:**
    - `AddWindowWithGodray(artRoot, "Art_S_Window1", mat, unlitMat, pal, new Vector2(60.0f, 34.0f));`
    - `AddWindowWithGodray(artRoot, "Art_S_Window2", mat, unlitMat, pal, new Vector2(89.5f, 34.0f));`
    - `AddPrisonSpriteProp(artRoot, "Art_S_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(75.0f, 34.0f), 0.65f, -12, Color.white);`
    - `AddShelfWithFioles(artRoot, "Art_S_Shelf1", pal, mat, 48.0f, 25.8f, -11);`
    - `AddShelfWithFioles(artRoot, "Art_S_Shelf2", pal, mat, 54.0f, 25.8f, -11);`
    - `AddScaledCeilingProp(artRoot, "Art_S_Cage1", pal.SuspendedCage, mat, 58.0f, 39f, 1.0f, -7, Color.white);`
    - `AddFloorJunkCluster(artRoot, "Art_S_Junk1", pal, mat, 45.0f, 24f, 3);`
    - `AddFloorJunkCluster(artRoot, "Art_S_Junk2", pal, mat, 78.0f, 24f, 6);`
  - **Room T Windows, Arches, Banners, Cages, Shelves & Debris:**
    - `AddWindowWithGodray(artRoot, "Art_T_Window1", mat, unlitMat, pal, new Vector2(55.0f, 56.5f));`
    - `AddWindowWithGodray(artRoot, "Art_T_Window2", mat, unlitMat, pal, new Vector2(82.0f, 56.5f));`
    - `AddWindowWithGodray(artRoot, "Art_T_Window3", mat, unlitMat, pal, new Vector2(116.0f, 56.5f));`
    - `AddWindowWithGodray(artRoot, "Art_T_Window4", mat, unlitMat, pal, new Vector2(135.5f, 56.5f));`
    - `AddPrisonSpriteProp(artRoot, "Art_T_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(58.5f, 55.5f), 0.70f, -12, Color.white);`
    - `AddPrisonSpriteProp(artRoot, "Art_T_BannerBig2", pal.BannerBig, pal.MatBannerSway, new Vector2(123.0f, 55.5f), 0.70f, -12, Color.white);`
    - `AddGroundedPrisonSprite(artRoot, "Art_T_Arch1", pal.ArcheSmall, mat, 71.8f, 40f, 0.90f, -14, Color.white);`
    - `AddScaledCeilingProp(artRoot, "Art_T_Cage1", pal.SuspendedCage, mat, 58.0f, 62.5f, 1.0f, -7, Color.white);`
    - `AddScaledCeilingProp(artRoot, "Art_T_Cage2", pal.SuspendedCage, mat, 102.0f, 62.5f, 1.0f, -7, Color.white);`
    - `AddScaledCeilingProp(artRoot, "Art_T_Cage3", pal.SuspendedCage, mat, 122.0f, 62.5f, 1.0f, -7, Color.white);`
    - `AddShelfWithFioles(artRoot, "Art_T_Shelf1", pal, mat, 56.0f, 41.8f, -11);`
    - `AddShelfWithFioles(artRoot, "Art_T_Shelf2", pal, mat, 99.0f, 41.8f, -11);`
    - `AddFloorJunkCluster(artRoot, "Art_T_Junk1", pal, mat, 50.0f, 40f, 1);`
    - `AddFloorJunkCluster(artRoot, "Art_T_Junk2", pal, mat, 96.5f, 40f, 5);`
    - `AddFloorJunkCluster(artRoot, "Art_T_Junk3", pal, mat, 114.0f, 40f, 9);`

### 3.4 `P1-04`: Dead Cells Chiaroscuro Lighting Calibration
1. **`BuildScene` (`Global Light 2D`):**
   - Set `globalLight.intensity = 0.44f;` (down from `0.60f`) and `globalLight.color = new Color(0.56f, 0.70f, 0.84f, 1f);` (`color.r = 0.56f <= 0.76f` required by `A12`).
2. **`AddStandingTorchBeacon`, `AddCeilingLanternBeacon`, and `BuildWallLanternBeacon`:**
   - Replace `Mathf.Max(intensity, 1.40f)` with `Mathf.Clamp(intensity, 0.75f, 1.45f)` so per-beacon intensity modulation is respected instead of being forced to `1.40f`.
   - Set `light.falloffIntensity = 0.62f;` (up from `0.45f`) and `volumeIntensity = 0.065f;` (down from `0.09f`) inside `BuildWallLanternBeacon`.
3. **Chiaroscuro Alternation Across Wing Loops (`W1`, `W2`, `W3`, `U1`):**
   - Instead of passing constant `1.40f` to every torch/lantern in `mFloorXs`, `nFloorXs`, `nCeilXs`, `oFloorXs`, `oCeilXs`, `pFloorXs`, `pCeilXs`, `qFloorXs`, `rFloorXs`, `rCeilXs`, `sFloorXs`, `sCeilXs`, `tFloorXs`, `tCeilXs`, `tMidXs`, modulate intensities deterministically:
     - Focal/even beacons (`(i % 2) == 0`): `intensity = 1.34f` (`warmAmber` / `warmGold`)
     - Secondary/odd mid-corridor fill beacons (`(i % 2) == 1`): `intensity = 0.88f` (`deepEmber = new Color(0.95f, 0.70f, 0.40f, 1f)`)
   - This creates a `0.46f` peak-to-valley intensity contrast between focal pools and mid-corridor shadow pockets while keeping all 88 lights active and all `outerRadius` values unchanged (`A10` coverage `96.5%`).
