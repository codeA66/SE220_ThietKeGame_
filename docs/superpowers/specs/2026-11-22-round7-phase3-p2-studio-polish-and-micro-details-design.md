# Round 7 (Phase 3): NhaGiamGiengChuong P2 Studio Polish, Micro-Fixes, Weathered Masonry & Architectural Depth Design Spec

**Date:** 2026-11-22  
**Target Scene & Prefabs:** `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` (built deterministically by `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` and 9 room prefabs in `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/*.prefab`).  
**Baseline:** Phase 1 (`P0-01`..`P0-04`) and Phase 2 (`P1-01`..`P1-05`) are complete and verified green (`77/77` EditMode + PlayMode tests PASS, `9/9` D3D12 GPU captures `maxClip=0.081%`, `shaderErrors=0`, `defaultMaterialRenderers=0`).

---

## 1. Objective

Close the final visual gap between `NhaGiamGiengChuong.unity` and *Dead Cells — Prisoners' Quarters (`PrisonStart`)* by addressing all remaining `P2` micro-visual blemishes and architectural texture/depth opportunities identified directly in the Phase 2 GPU captures (`Logs/r6-gpu-captures/gpu_r6_ng_01..09_*.png`) and the 377-sprite Prison asset audit:

1. **`P2-01` (3 GPU-Observed Micro-Fixes):**
   - Replace the mismatched lilac/lavender `chainDown_00.png` bottom cap on all 7 climbing chains (`I1, I2, I6, I7, I8, I9, I10`) with dark iron `chainTile_00.png` (`floorTopY + 0.45f`, scale `(1f, 0.70f, 1f)`).
   - Relocate `Art_G_TorchUpperWest` in Room G from `x = 62.0f` (overlapping `Art_G_Alcove`'s painted red banner) to `x = 65.6f` (`+0.33u` clear of `Art_G_Alcove` `max.x = 64.85`, `+0.78u` clear of `Art_G_Winch`, `+1.48u` clear of `Art_I2_Chain`).
   - Relocate `Art_J_Bed` in Room J from `x = -13.5f` (hanging chains crossing carved `Bay #3` `ArchSill_3_*`) to `x = 3.6f` on solid brick between `Art_J_Banner` (`x = 1.5f`) and `Beam_HJ_Header_6` (`x = 6.0f`).
2. **`P2-02` (Weathered Stone Wall Masonry Overlays):**
   - Break large-area `backWall_*` tile repetition across all 19 rooms (`A..T`) using unused 24-PPU `stampWallBig_0..5_00.png` (`3.0u × 3.0u`) and `stampWallMedium_0..5_00.png` (`2.0u × 2.0u`) rendered on `Background / -19` (directly over `TM_Background -20`, strictly behind `TM_WallDecals -18`, `ArchSill/Jamb -17`, windows, landmarks, torches, and actors), strictly guarded against all 16 carved parallax arch bays (`Bay #0..#15`).
3. **`P2-03` (Multi-Span Ceiling Masonry & Roof Moss Rhythm):**
   - Eliminate flat, single-stamp ceilings across all 9 long rooms (`width > 28u`: `E, M, N, O, P, Q, R, S, T`) by extending `AddCornerBeamsAndWebs` to place repeating `stampCeilingBig_0_00.png` (`5.0u × 2.0u`, tall rooms) / `stampCeilingSmall_1_00.png` (`2.0u × 2.0u`, low rooms) and `roofMoss_0..2_00.png` every `18u` on `Background / -11` (strictly behind `LanternFixture_R5 -9`, hanging cages `Ground / -7`, and `TM_SolidRim -3`), skipping open vertical shafts.
4. **`P2-04` (Architectural Depth Props — Timber Trusses, Ornate Iron Cages, Chained Silhouettes & Floor-Lip Foliage):**
   - Activate four high-value unused 24-PPU Prison asset families (0 raw `#FF00FF` magenta pixels verified across all files):
     - **7 Structural Timber Trusses** (`PrisonwoodenBeam_00.png`, `3.33u × 6.00u` @ `0.85x` → `2.83u × 5.10u`, `Background / -13`, grounded on solid floors in `N, O, P, R, T`).
     - **9 Ornate Hanging Iron Cages** (`chainBot_0..2_00.png`, `2.0u × 3.0–4.0u` @ `0.90x`, `Ground / -7`, ceiling-anchored in tall rooms `G, K, N, O, P, R, S, T`).
     - **8 Chained Prisoner Wall Silhouettes** (`wallJunk_14/15/17.png`, `3.0u × 3.0–5.0u` @ `0.78x`, `Background / -14`, dungeon storytelling on solid piers in `M, N, O, P, Q, R, T`).
     - **12 Damp Floor-Lip Foliage Clusters** (`groundFoliage_0..1_00.png`, `2.0u × 2.0–2.38u` @ `0.85x`, `Ground / -2`, centered at `y = floorTopY` so moss crowns peek `+0.13u` and drape `-0.70u..-1.01u` down solid stone floor lips in `C, G, M, O, P, Q, S`).
5. **`P2-05` (Subterranean Bedrock Rock Strata Stamps):**
   - Break flat black `dirt_00` bedrock borders across all 10 wide sub-floor & outer bedrock skirts (`AB_West`, `AB_Sub`, `AB_UpperA`, `CD_Sub`, `G_Sub`, `HJK_Sub`, `W2_Sub`, `W2_CeilP`, `W3_Sub`, `U1_SubE`, `U1_Overhang`) using `pal.StoneStamps` (`stoneStamp_0..5_00.png`) on `Ground / -4` (`scale = 0.90f`, `Color(0.46f, 0.50f, 0.56f, 0.40f)`), reusing the proven technique from `PaintPrisonStairsH` with strict 3×3 opaque `pal.Dirt` (`Matrix4x4.identity`) neighborhood gating.
6. **`P2-06` (Automated EditMode Regression Lock `A16` + Full Low-RAM Verification Gate):**
   - Add `A16_Round7Phase3_ChainCapMicroFixesWeatheredMasonryCeilingRhythmDepthPropsAndBedrockStrata` in `MapWiringTests.cs` and verify `78/78` EditMode + PlayMode PASS + `9/9` D3D12 GPU captures (`maxClip <= 0.500%`, `shaderErrors=0`, `defaultMaterialRenderers=0`).

---

## 2. Non-Goals & Strict Invariants

- **Zero gameplay/collision/layout changes:** No `BoxCollider2D`, `PolygonCollider2D`, `PlatformEffector2D`, `EncounterZone`, enemy placement, `I1..I16`, `SD_1..3`, `TP_0..5`, `GroundMist_R5` (`== 6`), `ShadowCasters_R5` (`== 6`), `GlowingMushroom_0..7` (`== 8`), or `PrisonNormalMapGenerator.CoreArchitectureSpriteNames` (`Length == 36`) may be altered.
- **Zero `.meta` fabrication:** All Phase 3 sprites already exist in `Assets/_Project/Art/Environment/Prison/Sprites/` with `spritePixelsToUnits = 24` and `0` raw chromakey `#FF00FF` pixels.
- **Zero regressions across all 77 existing tests:** `Core.Tests.EditMode` (30), `NhaGiamGiengChuong` Unit + Geometry (30), `MapWiringTests` `A07..A15` (9), `PlayMode` (8).

---

## 3. Detailed Technical Design

### 3.1 `P2-01`: Three GPU-Observed Micro-Fixes

| ID | Target Method & Line | Current Behavior | Phase 3 Fix | Verified Clearance / Coverage |
| --- | --- | --- | --- | --- |
| `P2-01a` | `AddPrisonChainArt` (`NhaGiamGiengChuongBuilder.cs:3171-3179`) | `CapBottom` uses `pal.ChainDown` (`chainDown_00.png`, bright lavender metal) at `(x, floorTopY + 0.35f)`, scale `(1, 1, 1)`. | Switch `capSr.sprite = pal.ChainTile` (`chainTile_00.png`, dark iron), `pos = (x, floorTopY + 0.45f)`, `localScale = (1f, 0.70f, 1f)`. | World Y spans `[floorTopY + 0.10 .. floorTopY + 0.80]`: strictly above `floorTopY`, overlaps `Link_0` (`[+0.50 .. +1.50]`) seamlessly, unifies all 7 climbing chains (`I1, I2, I6, I7, I8, I9, I10`). |
| `P2-01b` | `BuildRoomG` (`NhaGiamGiengChuongBuilder.cs:596`) | `Art_G_TorchUpperWest` at `(62f, 8f)` overlaps `Art_G_Alcove` (`X=[56.15..64.85], Y=[8.00..12.06]`). | Move `Art_G_TorchUpperWest` to `x = 65.6f, floorY = 8f`. | Fixture AABB `X=[65.18..66.02], Y=[9.70..10.95]`: `+0.33u` clear of `Art_G_Alcove` (`64.85`), `+0.78u` clear of `Art_G_Winch` (`66.8`), `+1.48u` clear of `Art_I2_Chain` (`67.5`), `+1.70u` above `OneWay_G_UpperMid` (`8.0`). Room G `A10` point-light coverage = `96.4%` (`>= 50%`). |
| `P2-01c` | `BuildRoomHJK` (`NhaGiamGiengChuongBuilder.cs:679`) | `Art_J_Bed` (`wallBed_00`, `2.08u × 2.00u`) at `(-13.5f, 16f)` overlaps `Bay #3` `ArchSill_3_*` (`[-15..-9, 18..21]`). | Move `Art_J_Bed` to `x = 3.6f, floorY = 16f`. | AABB `X=[2.56..4.64], Y=[16.00..18.00]`: `+0.39u` clear of `Art_J_Banner` (`max.x=2.17`), `+1.11u` west of `Beam_HJ_Header_6` (`5.75..6.25, Y=[20..22.5]`), `+11.56u` clear of `Bay #3`. |

### 3.2 `P2-02`: Weathered Stone Masonry Overlays (`AddWeatheredMasonryOverlays`)

- **Palette Additions (`PrisonPalette` & `LoadPrisonPalette()`):**
  - `Sprite[] StampWallBig`: `stampWallBig_0_00.png` .. `stampWallBig_5_00.png` (`72×72 px` @ 24 PPU = `3.00u × 3.00u`)
  - `Sprite[] StampWallMedium`: `stampWallMedium_0_00.png` .. `stampWallMedium_5_00.png` (`48×48 px` @ 24 PPU = `2.00u × 2.00u`)
- **Algorithm (`AddWeatheredMasonryOverlays`):**
  - Signature: `private static void AddWeatheredMasonryOverlays(GameObject artRoot, Tilemap bgTm, PrisonPalette pal, Material mat, string roomTag, float xMin, float xMax, float yMin, float yMax)`
  - Invoked **after** `CarveAndFrameArchBay` in all 9 room builders for all 19 rooms (`A..T`).
  - Iterates `cx` from `Mathf.CeilToInt(xMin) + 2` to `Mathf.FloorToInt(xMax) - 3` (stride `7` if `width >= 30f` else `6`) and `cy` from `Mathf.CeilToInt(yMin) + 2` to `Mathf.FloorToInt(yMax) - 3` (stride `5`).
  - Deterministic jitter: `jx = cx + (CellHash(cx, cy) % 3) - 1`; `jy = cy + (roomHeight >= 7f ? (CellHash(cx + 17, cy + 31) % 3) - 1 : 0)` (no Y jitter in low `5.5u..6.5u` rooms so Rooms `A, B, D, F, J` are guaranteed clean centered patches).
  - Selects `StampWallBig` (`half = 1.5f`) when `roomHeight >= 9f && (CellHash(jx * 5, jy * 11) % 2 == 0)`, else `StampWallMedium` (`half = 1.0f`).
  - **Strict Containment & Carved-Bay Guard:**
    - Rejects if `centerX - half < xMin + 0.6f || centerX + half > xMax - 0.6f || centerY - half < yMin + 0.6f || centerY + half > yMax - 0.6f`.
    - Rejects if ANY cell `(nx, ny)` in `[jx - 2 .. jx + 2] × [jy - 2 .. jy + 2]` has `bgTm.GetTile(new Vector3Int(nx, ny, 0)) == null`.
  - **Rendering:** `AddPrisonSpriteProp(artRoot, $"Art_{roomTag}_WallPatch_{idx++}", sp, mat, new Vector2(centerX, centerY), 1.0f, -19, new Color(0.84f, 0.88f, 0.86f, 0.72f))` → sorts on `Background / -19` (strictly above `TM_Background -20`, strictly behind `TM_WallDecals -18` and `ArchSill/Jamb -17`).
  - **Simulated Output:** `168` weathered stone patches across all `19/19` rooms (`>= 1` in every room), `0` carved bay overlaps.

### 3.3 `P2-03`: Multi-Span Ceiling Masonry & Roof Moss (`AddCornerBeamsAndWebs`)

- **Palette Additions:**
  - `Sprite StampCeilingBig`: `stampCeilingBig_0_00.png` (`120×48 px` = `5.00u × 2.00u`)
  - `Sprite StampCeilingSmall1`: `stampCeilingSmall_1_00.png` (`48×48 px` = `2.00u × 2.00u`)
- **Extension to `AddCornerBeamsAndWebs`:**
  - Keeps existing single `Art_{roomTag}_CeilStamp` and `Art_{roomTag}_RoofMoss` untouched for all 19 rooms.
  - When `xMax - xMin > 28f` (applies to all 9 long rooms: `E, M, N, O, P, Q, R, S, T`):
    - Loops `float spanX = xMin + 9f; spanX <= xMax - 9f + 0.01f; spanX += 18f`:
      - Ceiling stone stamp: `sp = isTallRoom ? pal.StampCeilingBig : pal.StampCeilingSmall1`, `scale = isTallRoom ? 0.72f : 0.75f`, `halfW = sp.bounds.extents.x * scale`. Skips if overlapping `[shaftMinX - 0.5f, shaftMaxX + 0.5f]` or `Mathf.Abs(spanX - midX) < 4.0f`. Places `Art_{roomTag}_CeilStamp_{stampIdx++}` via `AddScaledCeilingProp(..., sortingOrder: -11, new Color(0.90f, 0.92f, 0.90f, 0.92f))`.
      - Ceiling moss: `mossSpanX = spanX + 6.5f`, `mHalfW = mossSp.bounds.extents.x * 0.75f`. Skips if `mossSpanX > xMax - 4.0f`, overlapping `[shaftMinX - 0.5f, shaftMaxX + 0.5f]`, or `Mathf.Abs(mossSpanX - mossX) < 3.0f`. Places `Art_{roomTag}_RoofMoss_{mossIdx++}` via `AddScaledCeilingProp(..., sortingOrder: -11, new Color(0.86f, 0.94f, 0.86f, 0.90f))`.
  - **Why `Background / -11`:** `AddPrisonSpriteProp` assigns `sortingOrder <= -9` to `Background`. `Background / -11` renders in front of `TM_Background (-20)`, `WallPatch (-19)`, `TM_WallDecals (-18)`, `ArchSill (-17)`, `Window (-15)`, landmarks (`-14..-13`), and vertical banners (`-12`), while tucking strictly behind `LanternFixture_R5 (Background / -9)`, hanging cages (`Ground / -7`), `TM_SolidVisual (Ground / -5)`, and `TM_SolidRim (Ground / -3)`. Zero lantern or cage occlusion is possible.
  - **Simulated Output:** `27` multi-span `CeilStamp_*` + `26` multi-span `RoofMoss_*` across all `9/9` long rooms, `0` floating over open vertical shafts.

### 3.4 `P2-04`: Curated Architectural Depth Props (36 Verified Clash-Free Placements)

- **Palette Additions:**
  - `Sprite WoodenBeamTruss`: `PrisonwoodenBeam_00.png` (`80×144 px` = `3.33u × 6.00u`)
  - `Sprite[] OrnateCages`: `chainBot_0_00.png` (`2.0u × 4.0u`), `chainBot_1_00.png` (`2.0u × 3.0u`), `chainBot_2_00.png` (`2.0u × 4.0u`)
  - `Sprite[] ChainedPrisoners`: `wallJunk_14.png` (`3.0u × 5.0u`), `wallJunk_15.png` (`3.0u × 3.0u`), `wallJunk_17.png` (`3.0u × 5.0u`)
  - `Sprite[] GroundFoliage`: `groundFoliage_0_00.png` (`2.0u × 2.38u`, opaque localY `[-1.19..+0.15]`), `groundFoliage_1_00.png` (`2.0u × 2.00u`, opaque localY `[-0.83..+0.12]`)
- **Verified Placement Table (0/36 overlaps with all 16 bays, 31 OneWays, 88 lanterns, 29 windows, 7 chains, 8 banners, headers, scene interactables, mushrooms, junk, landmarks, and each other):**

| Category | Name | Prefab / Room | Position / Anchor | Sprite & Scale | Layer / Order & Tint | Support |
| --- | --- | --- | --- | --- | --- | --- |
| **Timber Truss** (7) | `Art_N_Truss1` | `W1` / Room N | Grounded `(109.5f, 8f)` | `WoodenBeamTruss` @ `0.85f` | `Background / -13`, `(0.88, 0.86, 0.84, 1)` | `Floor_N_East` |
| | `Art_N_Truss2` | `W1` / Room N | Grounded `(135.5f, 8f)` | `WoodenBeamTruss` @ `0.85f` | `Background / -13`, `(0.88, 0.86, 0.84, 1)` | `Floor_N_East` |
| | `Art_O_Truss1` | `W2` / Room O | Grounded `(110.5f, -16f)` | `WoodenBeamTruss` @ `0.85f` | `Background / -13`, `(0.88, 0.86, 0.84, 1)` | `Floor_O` |
| | `Art_P_Truss1` | `W2` / Room P | Grounded `(199.8f, -16f)` | `WoodenBeamTruss` @ `0.85f` | `Background / -13`, `(0.88, 0.86, 0.84, 1)` | `Floor_P` |
| | `Art_R_Truss1` | `W3` / Room R | Grounded `(-98.5f, 24f)` | `WoodenBeamTruss` @ `0.85f` | `Background / -13`, `(0.88, 0.86, 0.84, 1)` | `Floor_R_West` |
| | `Art_T_Truss1` | `U1` / Room T | Grounded `(65.8f, 40f)` | `WoodenBeamTruss` @ `0.85f` | `Background / -13`, `(0.88, 0.86, 0.84, 1)` | `Floor_T_West` |
| | `Art_T_Truss2` | `U1` / Room T | Grounded `(120.2f, 40f)` | `WoodenBeamTruss` @ `0.85f` | `Background / -13`, `(0.88, 0.86, 0.84, 1)` | `Floor_T_East` |
| **Ornate Cage** (9) | `Art_G_OrnateCage1` | `G` / Room G | Ceiling `(69.5f, 14.5f)` | `OrnateCages[1]` @ `0.90f` | `Ground / -7`, `Color.white` | `Ceiling_G` |
| | `Art_K_OrnateCage1` | `HJK` / Room K | Ceiling `(-27.5f, 30.5f)` | `OrnateCages[1]` @ `0.90f` | `Ground / -7`, `Color.white` | `Ceiling_K` |
| | `Art_N_OrnateCage1` | `W1` / Room N | Ceiling `(95.5f, 22.5f)` | `OrnateCages[0]` @ `0.90f` | `Ground / -7`, `Color.white` | `Ceiling_N` |
| | `Art_O_OrnateCage1` | `W2` / Room O | Ceiling `(102.5f, -1f)` | `OrnateCages[2]` @ `0.90f` | `Ground / -7`, `Color.white` | `Floor_M_West` |
| | `Art_P_OrnateCage1` | `W2` / Room P | Ceiling `(169.5f, -1.5f)` | `OrnateCages[1]` @ `0.90f` | `Ground / -7`, `Color.white` | `Ceiling_P` |
| | `Art_R_OrnateCage1` | `W3` / Room R | Ceiling `(-69.5f, 38.5f)` | `OrnateCages[2]` @ `0.90f` | `Ground / -7`, `Color.white` | `Ceiling_R` |
| | `Art_S_OrnateCage1` | `U1` / Room S | Ceiling `(69.5f, 39f)` | `OrnateCages[0]` @ `0.90f` | `Ground / -7`, `Color.white` | `Floor_T_West` |
| | `Art_T_OrnateCage1` | `U1` / Room T | Ceiling `(76.5f, 62.5f)` | `OrnateCages[2]` @ `0.90f` | `Ground / -7`, `Color.white` | `Ceiling_T` |
| | `Art_T_OrnateCage2` | `U1` / Room T | Ceiling `(127.0f, 62.5f)` | `OrnateCages[0]` @ `0.90f` | `Ground / -7`, `Color.white` | `Ceiling_T` |
| **Chained Silhouette** (8) | `Art_M_Chained1` | `W1` / Room M | Center `(114.8f, 3.8f)` | `ChainedPrisoners[1]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| | `Art_N_Chained1` | `W1` / Room N | Center `(128.0f, 17.5f)` | `ChainedPrisoners[0]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| | `Art_O_Chained1` | `W2` / Room O | Center `(157.5f, -7.2f)` | `ChainedPrisoners[2]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| | `Art_P_Chained1` | `W2` / Room P | Center `(182.0f, -7.2f)` | `ChainedPrisoners[0]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| | `Art_Q_Chained1` | `W3` / Room Q | Center `(-53.8f, 19.6f)` | `ChainedPrisoners[1]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| | `Art_R_Chained1` | `W3` / Room R | Center `(-64.0f, 32.5f)` | `ChainedPrisoners[2]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| | `Art_T_Chained1` | `U1` / Room T | Center `(63.5f, 55.5f)` | `ChainedPrisoners[0]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| | `Art_T_Chained2` | `U1` / Room T | Center `(109.5f, 55.5f)` | `ChainedPrisoners[2]` @ `0.78f` | `Background / -14`, `(0.76, 0.80, 0.86, 0.92)` | Solid brick |
| **Floor-Lip Foliage** (12) | `Art_C_Foliage1` | `CD` / Room C | Center `(48.2f, 0f)` | `GroundFoliage[0]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_C` |
| | `Art_G_Foliage1` | `G` / Room G | Center `(62.2f, 0f)` | `GroundFoliage[1]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_G_Lower` |
| | `Art_G_Foliage2` | `G` / Room G | Center `(57.5f, 8f)` | `GroundFoliage[1]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_G_UpperWest` (`1.0u` slab; opaque bottom `7.29 > 7.00`) |
| | `Art_M_Foliage1` | `W1` / Room M | Center `(111.5f, 0f)` | `GroundFoliage[1]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_M_West` |
| | `Art_O_Foliage1` | `W2` / Room O | Center `(93.0f, -16f)` | `GroundFoliage[0]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_O` |
| | `Art_O_Foliage2` | `W2` / Room O | Center `(122.5f, -16f)` | `GroundFoliage[1]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_O` |
| | `Art_O_Foliage3` | `W2` / Room O | Center `(145.5f, -16f)` | `GroundFoliage[0]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_O` |
| | `Art_P_Foliage1` | `W2` / Room P | Center `(180.5f, -16f)` | `GroundFoliage[1]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_P` |
| | `Art_P_Foliage2` | `W2` / Room P | Center `(203.2f, -16f)` | `GroundFoliage[0]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_P` |
| | `Art_Q_Foliage1` | `W3` / Room Q | Center `(-64.2f, 16f)` | `GroundFoliage[1]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_Q` |
| | `Art_S_Foliage1` | `U1` / Room S | Center `(42.2f, 24f)` | `GroundFoliage[0]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_S` |
| | `Art_S_Foliage2` | `U1` / Room S | Center `(75.5f, 24f)` | `GroundFoliage[1]` @ `0.85f` | `Ground / -2`, `(0.84, 0.94, 0.86, 0.95)` | `Floor_S` |

### 3.5 `P2-05`: Subterranean Bedrock Rock Strata Stamps (`AddBedrockStrataStamps`)

- **Helper:**
  `private static void AddBedrockStrataStamps(GameObject artRoot, Tilemap solidTm, PrisonPalette pal, Material mat, string regionTag, int xMin, int xMax, int yMin, int yMax, int strideX = 6, int strideY = 4)`
- **Placement & Strict Guard:**
  - Loops `for (int cx = xMin + 2; cx <= xMax - 3; cx += strideX)` and `for (int cy = yMin + 2; cy <= yMax - 3; cy += strideY)`.
  - Verifies ALL 9 neighborhood cells `(cx + dx, cy + dy)` (`dx, dy ∈ {-1, 0, +1}`) satisfy `solidTm.GetTile(c) == pal.Dirt && solidTm.GetTransformMatrix(c) == Matrix4x4.identity`.
  - Places `Art_{regionTag}_BedrockStone_{idx++}` at `(cx + 0.5f, cy + 0.5f)` with `pal.StoneStamps[CellHash(cx, cy) % pal.StoneStamps.Length]`, `scale = 0.90f` (`1.80u × 1.80u`), `sortingOrder = -4` (`Ground / -4`, identical to `PaintPrisonStairsH`), `tint = new Color(0.46f, 0.50f, 0.56f, 0.40f)`.
- **Target Bedrock Regions (11 calls → `101` strata stamps):**
  - `AB`: `("AB_West", -16, 0, -8, 15)`, `("AB_Sub", 0, 42, -8, -1)`, `("AB_UpperA", 0, 6, 6, 15)`
  - `CD`: `("CD_Sub", 42, 56, -8, -1)`
  - `G`: `("G_Sub", 56, 76, -21, -1)`
  - `HJK`: `("HJK_Sub", -30, 6, 7, 15)`
  - `W2`: `("W2_Sub", 75, 238, -22, -17)`, `("W2_CeilP", 164, 238, -1, 7)`
  - `W3`: `("W3_Sub", -106, -30, 7, 15)`
  - `U1`: `("U1_SubE", 56, 76, 15, 23)`, `("U1_Overhang", 92, 144, 31, 39)`

### 3.6 `P2-06`: Automated EditMode Verification (`A16`)

`MapWiringTests.A16_Round7Phase3_ChainCapMicroFixesWeatheredMasonryCeilingRhythmDepthPropsAndBedrockStrata` verifies:
1. All 7 climbing chains' `CapBottom` use `chainTile_00` (zero `chainDown_00` in scene) and sit strictly above their floor (`bounds.min.y > floorY`).
2. `Art_G_TorchUpperWest/LanternFixture_R5` has zero AABB overlap with `Art_G_Alcove`, and `Art_J_Bed` has zero AABB overlap with any `ArchSill_*` or `ArchJamb_*`.
3. `>= 120` weathered wall patches (`Art_*_WallPatch_*`) on `Background / -19` using `stampWallBig_*` or `stampWallMedium_*`, with zero AABB overlap against any carved arch bay interior (`Bay #0..#15`).
4. All 9 long rooms (`E, M, N, O, P, Q, R, S, T`) contain `>= 1` multi-span `Art_{tag}_CeilStamp_*` and `>= 1` `Art_{tag}_RoofMoss_*` on `Background / -11`, with zero floating over open shafts (`M:[101..107]`, `O:[115..121]`, `Q:[-69..-63]`, `S:[77..83]`).
5. All 36 curated `P2-04` depth props (7 trusses, 9 ornate cages, 8 chained silhouettes, 12 floor-lip foliage clusters) exist on their exact sorting layers/orders, and all 24 wall/ceiling/truss props have zero AABB overlap with `ArchSill_*`/`ArchJamb_*`, `OneWay_*`, `LanternFixture_R5`, or `Window` sprites (and all 12 foliage clusters have zero overlap with `OneWay_*` or `ArchSill_*`/`ArchJamb_*`).
6. `>= 60` subterranean bedrock strata stamps (`Art_*_BedrockStone_*`) on `Ground / -4` using `stoneStamp_*`, with every stamp's center cell verified as opaque `dirt_00` on `TM_SolidVisual`.
