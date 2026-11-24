# Round 7 (Phase 1): `NhaGiamGiengChuong` P0 Visual & Spatial Remediation Design Spec

**Date:** 2026-11-22  
**Target Scene:** `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` & 9 Room Prefabs (`Assets/_Project/Features/NhaGiamGiengChuong/Rooms/*.prefab`)  
**Scope:** Phase 1 of the Deep Visual & Spatial Review — eliminating 100% of `P0` visual defects (`P0-01` through `P0-04`) while preserving 100% of existing physical colliders, gameplay wiring, and automated tests (`A01`–`A13`, `A05b`–`A05e`, `U01`–`U20`, `P01`–`P08`, Core EditMode `29/29`).

---

## 1. Intent & Why

Although Round 6 passed 74/74 automated tests and the 9 verification shots in `Logs/r6-gpu-captures/`, capturing 22 real player-camera gameplay views (`orthoSize = 7.0f`, `1280×720`) exposed four classes of `P0` visual defects that break immersion immediately:
1. **`P0-01` (Spawn & Room A/B Camera Bounds Void Leak & Spawn Prop Pile-up):** Expanding `CameraFollow2D._bounds` to `Rect(-104, -20, 340, 86)` removed the old `x = 0` camera clamp at player spawn `(3, 0)`. At `(3.0, 1.5)` with `orthoSize = 7.0`, the camera views `x ∈ [-9.45, +15.45], y ∈ [-5.50, +8.50]`, exposing `40%` of the frame left of `Wall_A_West` (`x = -0.5`) and above `Ceiling_A/B` (`y = 5.5..6.5`) to floating outdoor parallax (`ParallaxFar_Backdrop`, `ParallaxMid_Alcoves`) and a jagged sub-floor dirt cutoff at `x = -4`. Meanwhile, `Art_A_Bed` (`x=1.8`), `Player` (`x=3.0`), and `Art_A_Fountain` (`x=3.2`) are piled on top of each other, and `Art_A_Window` (`x=15.5, y=3.6`) overlaps the top of `TP_0_Spawn` (`x=15.5, y=0`).
2. **`P0-02` (Hot-Magenta `#FF55FF` / `#FF80FF` Chromakey Mask Pixels & Exit Door Occlusion):** Raw extracted Dead Cells sprites `exitDoorSewerShort_00.png` (`Room P` at `(224, -16)`) and `exitDoorPrisonCourtyard_00.png` (`Room T` at `(128, 40)`) contain `1,661` hot-magenta chromakey mask pixels (`5.8%` of texture area) each, and `switchCopper_00.png`, `tpStoneUp_00.png`, `tpStoneDown_00.png` contain minor magenta mask pixels. In `Room P`, `Art_P_ExitGlow` (`Background/-6`) is sorted behind `Art_P_SewerExitDoor` (`Ground/-5`), and `Art_P_Torch_3` (`x=226`) and `Art_T_TorchLow_4` (`x=128`) are bolted directly onto the exit door sprites.
3. **`P0-03` (Disembodied & Toy-Scaled `36u × 20u` Collector Room Canvases):** `armory_dome_00.png` and `armory_corps_00.png` (native `864×480 px = 36.0u × 20.0u @ 24 PPU`) were split `30u` apart in `Room M`, grounded at `y = 0`, and shrunk to `scale = 0.28f` (making the Collector's vat smaller than an enemy and suspending hanging trays in mid-air where `OneWay_M_Perch1` `[88..96, y=3.5]` stabs into the dome). In `Room R`, `armory_dome_00` (`scale = 0.50f` at `x=-74, y=24`) is sliced in half horizontally by `OneWay_R_Mid2` (`[-82..-70, y=28.5]`).
4. **`P0-04` (14 Platform / Torch / Chain / Scroll / Mushroom Overlaps Across Core & Wings):** One-way wooden platforms, wall lanterns, climbing chains, power scrolls, and glowing mushrooms collide with or slice through landmarks in Rooms `A, C, G, K, E, L, M, O, P, Q, R, S, T`.

---

## 2. Goals, Non-Goals & Constraints

### Goals
- Seal 100% of outer camera views around `Room A/B` (`x ∈ [-16, 0], y ∈ [-8, 15]`, `x ∈ [0, 18], y ∈ [6, 15]`, `x ∈ [18, 22], y ∈ [6, 7]`) with visual-only `dirt_00` bedrock on `TM_SolidVisual`.
- Sanitize all hot-magenta chromakey pixels (`R >= 200 && B >= 190 && G <= 175 && (R - G) >= 45 && (B - G) >= 35`) in `exitDoorSewerShort_00.png`, `exitDoorPrisonCourtyard_00.png`, `switchCopper_00.png`, `tpStoneUp_00.png`, `tpStoneDown_00.png` deterministically inside `PrisonPropSpriteGenerator.EnsureAll()`, and fix `Art_P_ExitGlow` sorting order (`Ground/-4`).
- Replace the disembodied/toy-scaled `armory_dome_00` / `armory_corps_00` instances on `Art_M_ArmoryDome1`, `Art_M_ArmoryCorps`, and `Art_R_ArmoryDome` with properly proportioned 24-PPU Prison architectural sprites (`alcovePillar_00`, `archeSmall_00`, `prisonBigAlcove_00`) while keeping their exact GameObject names and `floorY` grounding so `A08` and `A12` pass unmodified.
- Resolve all 14 AABB overlaps between one-way platforms, wall lanterns, climbing chains, power scrolls, glowing mushrooms, and architectural landmarks/banners across Rooms `A, C, G, K, E, L, M, O, P, Q, R, S, T`.
- Add EditMode regression test `A14` and re-capture both the 9 standard R6 GPU frames and key player-camera verification frames.

### Non-Goals (Deferred to Phase 2 & Phase 3)
- Changing physical room collider dimensions (`Ceiling_T`, `Ceiling_S`), adding new interior collider bulkheads, or changing `OneWay_*` platform collider coordinates (which are pinned by `A01`–`A06` and `A05b`–`A05e`).
- Global lighting intensity reduction (`0.60f -> 0.42f`) or reducing total lantern count below `80` (which is currently pinned by `A09` `globalLight.intensity >= 0.48f` and `A11` `LanternFixture_R5 >= 80`).

---

## 3. Approaches Considered

1. **Approach A (Recommended — Visual & Spatial Remediation within Existing Collider & Test Contract):**
   - Keep all physical colliders (`A01`–`A06`, `A05b`–`A05e`), all 41 landmark GameObject names (`A08`, `A12`), all 31 plank row names (`A10`), and `>= 80` lantern fixtures (`A11`) intact.
   - Fix `P0-01` via visual-only `TM_SolidVisual` bedrock skirts in `AB.prefab` and prop de-cluttering in `Room A`.
   - Fix `P0-02` via deterministic chromakey pixel sanitization in `PrisonPropSpriteGenerator.EnsureAll()` + `Art_P_ExitGlow` sorting order promotion.
   - Fix `P0-03` & `P0-04` by re-assigning `Art_M_ArmoryDome1`, `Art_M_ArmoryCorps`, and `Art_R_ArmoryDome` to proper 1:1 24-PPU Prison architectural sprites (`alcovePillar_00`, `archeSmall_00`, `prisonBigAlcove_00`) and relocating all clashing landmarks, banners, torches, and junk clusters into verified non-overlapping coordinates.
   - *Trade-off:* Zero risk of breaking existing geometry/playmode tests (`74/74` stay green), while fixing 100% of `P0` visual defects immediately.
2. **Approach B (Collider & Room Resizing in Phase 1):**
   - Alter `OneWay_*` collider coordinates and room ceiling heights immediately.
   - *Why rejected for Phase 1:* Modifying physical collider coordinates in Phase 1 would require rewriting `A01`–`A06` and `A05b`–`A05e`, mixing structural topology changes (Phase 2) with urgent visual bug fixes (Phase 1).

---

## 4. Exact Coordinate Specification for Phase 1

### 4.1 `PrisonPropSpriteGenerator.SanitizeChromakeyPrisonSprites()` (`P0-02`)
- Target sprites in `Assets/_Project/Art/Environment/Prison/Sprites/`:
  - Doorway sprites (`exitDoorSewerShort_00.png`, `exitDoorPrisonCourtyard_00.png`): replace chromakey magenta pixels (`a > 16 && r >= 200 && b >= 190 && g <= 175 && (r - g) >= 45 && (b - g) >= 35`) with deep portal recess shadow `Color32(14, 20, 28, a)`.
  - Interactive rune/switch sprites (`switchCopper_00.png`, `tpStoneUp_00.png`, `tpStoneDown_00.png`): replace chromakey magenta pixels with cyan-teal energy rune glow `Color32(36, 204, 218, a)`.
- After replacing pixels (if any chromakey pixel exists), re-encode PNG, write via `WriteBytesIfChanged`, and regenerate corresponding normal maps if applicable.

### 4.2 `AB.prefab` Bedrock Skirts & `Room A` Prop Layout (`P0-01`, `P0-04`)
- **Visual Bedrock Skirts (`BuildRoomAB`):**
  - `PaintVisualDirtSkirt(solidTm, pal, -16, 42, -8, -1);`
  - `PaintVisualDirtSkirt(solidTm, pal, -16, 0, -1, 15);` (called after `PaintPrisonOuterWall(-0.5f, 0f, -1f, 6f)` so `(-1, -1..5)` keeps its outer wall tile and `(-1, 6..14)` + `(-16..-2, -1..14)` are filled with opaque `dirt_00`).
  - `PaintVisualDirtSkirt(solidTm, pal, 0, 18, 6, 15);`
  - `PaintVisualDirtSkirt(solidTm, pal, 18, 22, 6, 7);`
- **`Room A` Props (`x ∈ [0, 18], y ∈ [0, 5.5]`):**
  - `Art_A_Junk`: `x = 0.6f, y = 0f`
  - `Art_A_Bed`: `x = 1.8f, y = 0f, scale = 0.90f` (`x ∈ [0.86..2.74]`)
  - `Art_A_Fountain`: `x = 4.3f, y = 0f, scale = 0.85f` (`x ∈ [3.45..5.15]`, clear of Player at `(3.0, 0)`)
  - `Art_A_HomonculusPipes`: `(4.3f, 2.6f), scale = 0.85f`
  - `Art_A_Cage`: `x = 6.2f, ceilingBottomY = 5.5f, scale = 1.0f` (`bottom = 3.5f`)
  - `Art_A_Window`: `(7.6f, 3.6f)` (`x ∈ [6.93..8.27]`, clear of `Bay #0` `[9..14]` and `TP_0_Spawn` at `15.5f`)
  - `Art_A_Torch`: `x = 14.3f, floorY = 0f` (`flameY = 2.2f`, between `Bay #0` `xMax = 14` and `TP_0_Spawn` `x = 15.5`)

### 4.3 Core Spine Prop/Torch/Banner De-cluttering (`CD.prefab`, `G.prefab`, `HJK.prefab`, `ELF.prefab`) (`P0-04`)
- **`Room C` (`CD.prefab`):**
  - `Art_C_BannerBig`: `(43.8f, 12.2f), scale = 0.65f` (`y ∈ [9.28..15.13]`, clear of `OneWay_C_Top` `y = 8.0` and `Art_C_BeamTL` `y = 15.03`).
  - `Art_C_Junk`: `x = 51.5f, y = 0f` (clear of `GlowingMushroom_0` at `x = 43.5f` and chain `I1` at `x = 46f`).
- **`Room G` (`G.prefab`):**
  - `Art_G_Window`: `(74.2f, 11.6f)` (`x ∈ [73.53..74.87]`).
  - `Art_G_TorchUpperEast`: `x = 71.8f, floorY = 8f` (`flameY = 10.2f`, clear of `Art_G_Window`).
  - `Art_G_Junk`: `x = 66.0f, y = 0f` (clear of `GlowingMushroom_1` at `x = 58.5f`).
- **`Room K` (`HJK.prefab`):**
  - `Art_K_BannerBig`: `(-27.6f, 19.8f), scale = 0.60f` (`y ∈ [17.10..22.50]`, strictly below `OneWay_K_Top` planks at `y = 23.125f`).
  - `Art_K_TorchLower`: `x = -20.5f, floorY = 16f` (`3.5u` clear of `Art_I6_Chain` at `x = -24.0f`).
  - `Art_K_Junk`: `x = -18.5f, y = 16f` (clear of `GlowingMushroom_2` at `x = -28.5f`).
- **`Room E` & `Room L` (`ELF.prefab`):**
  - `Art_E_Pillar1`: `x = -11.5f, y = 24f, scale = 0.85f` (`x ∈ [-14.70..-8.30]`).
  - `Art_E_TorchWest`: `x = -6.8f, floorY = 24f` (clear of `Art_E_Pillar1` and `OneWay_E_Combat` `[-4..2]`).
  - `Art_E_Pillar2`: `x = 14.5f, y = 24f, scale = 0.85f` (`x ∈ [11.30..17.70]`).
  - `Art_E_TorchEast`: `x = 18.6f, floorY = 24f` (clear of `Art_E_Pillar2`).
  - `Art_E_WindowEast`: `(20.2f, 28.6f)` (clear of `Art_E_TorchEast`).
  - `Art_E_Junk`: `x = 10.0f, y = 24f` (clear of `GlowingMushroom_3` at `x = -14.5f`).
  - `Art_L_BannerBig`: `(35.8f, 33.5f), scale = 0.68f` (`x ∈ [34.78..36.82], y ∈ [30.44..36.56]`, clear of `OneWay_L_1..4`).
  - `Art_L_Window`: `(30.5f, 35.2f)`.
  - `Art_L_LanternCeiling`: `x = 26.5f, ceilingBottomY = 38.5f`.

### 4.4 Expansion Wing Prop/Torch/Scroll De-cluttering (`W1`, `W2`, `W3`, `U1`) (`P0-02`, `P0-03`, `P0-04`)
- **`Room M` (`W1_EastCourtyard.prefab`):**
  - `Art_M_Dummy`: `pal.Dummy0` at `x = 81.5f, y = 0f, scale = 1.0f` (`x ∈ [80.79..82.21]`).
  - `Art_M_ArmoryDome1`: `pal.AlcovePillar` (`7.54u × 5.58u`) at `x = 109.2f, y = 0f, scale = 0.85f` (`x ∈ [106.0..112.4], y ∈ [0..4.74]`, clear of `OneWay_M_Perch1` `[88..96]`, chain `I7` `104`, `GlowingMushroom_4` moved to `113.8f`, and `OneWay_M_WellBridge` `[115..121]`).
  - `Art_M_ArmoryCorps`: `pal.ArcheSmall` (`5.0u × 5.0u`) at `x = 127.5f, y = 0f, scale = 1.0f` (`x ∈ [125.0..130.0], y ∈ [0..5.0]`, clear of `OneWay_M_WellBridge` `[115..121]` and `OneWay_M_Perch2` `[134..142]`).
  - `Art_M_WeaponDeco`: `pal.WeaponDeco` (`16.0u × 5.0u`) at `x = 153.8f, y = 0f, scale = 0.48f` (`x ∈ [149.96..157.64], y ∈ [0..2.40]`).
  - `I11_Scroll_EastCourtyard` (in `BuildScene`): moved from `(156f, 0f)` to **`(158.8f, 0f, 0f)`** (between `Art_M_WeaponDeco` `xMax = 157.64` and `TP_1_EastVault` `x = 161.0f`).
  - `mFloorXs`: `{ 84.5f, 100.5f, 123.0f, 146.5f, 162.8f }` (all 5 torches on open brick wall, zero platform or prop overlap).
- **`Room O` & `Room P` (`W2_SewerCistern.prefab`):**
  - `Art_O_CellDoor1` (`pal.SmallDoor`): moved to `x = 90.0f, y = -16f, scale = 1.0f` (`y ∈ [-16.0..-13.21]`, sitting `1.33u` below `OneWay_O_West` `[84..96, y = -11.5]`).
  - `Art_O_MonsterDoor` (`pal.MonsterDoorFront`): moved to `x = 130.5f, y = -16f, scale = 0.78f` (`x ∈ [125.04..135.96], y ∈ [-16.0..-6.64]`, in the open tall span between `OneWay_O_ShaftStep1/2` `xMax = 124.0` and `OneWay_O_East` `xMin = 138.0`).
  - `Art_O_CellDoor2` (`pal.BigDoor0`): at `x = 154.0f, y = -16f, scale = 0.90f` (`x ∈ [152.2..155.8], y ∈ [-16.0..-12.4]`).
  - `oFloorXs`: `{ 81.5f, 108.5f, 121.5f, 136.8f, 158.5f }`.
  - `Art_P_ExitGlow`: `sortingLayerID = Ground, sortingOrder = -4` (renders inside the dark archway of `Art_P_SewerExitDoor` on `Ground/-5`).
  - `pFloorXs`: `{ 169.5f, 182.0f, 202.0f, 218.0f }` (clear of `Art_P_CorruptedDoor` at `174.0`, `OneWay_P_Mid` `[184..198]`, `SD_2_SewerExit` `206..212`, `TP_3_SewerExit` `216`, and `Art_P_SewerExitDoor` `[219.75..228.25]`).
- **`Room Q` & `Room R` (`W3_WestBastion.prefab`):**
  - `Art_Q_SnakeLandmark`: `x = -91.8f, y = 16f, scale = 0.28f` (`x ∈ [-95.09..-88.51], y ∈ [16.0..21.44]`, strictly left of `OneWay_Q_Mid` `[-88..-76]`).
  - `Art_Q_Billot`: `x = -61.0f, y = 16f, scale = 0.85f` (`x ∈ [-62.28..-59.72]`).
  - `Art_Q_SkeletonSpawn`: `x = -49.0f, y = 16f, scale = 0.48f` (`x ∈ [-51.88..-46.12], y ∈ [16.0..22.24]`, in open span east of `OneWay_Q_Mid` `-76` and shaft `[-69..-63]`).
  - `Art_Q_JudasSeat`: `x = -38.0f, y = 16f, scale = 0.85f` (`x ∈ [-38.90..-37.10]`).
  - `qFloorXs`: `{ -98.5f, -73.0f, -56.0f, -34.0f }`.
  - `Art_R_ArmoryDome`: `pal.PrisonBigAlcove` (`5.0u × 5.0u`) at `x = -60.0f, y = 24f, scale = 0.90f` (`x ∈ [-62.25..-57.75], y ∈ [24.0..28.50]`, strictly below `CarvedArchBay #7` `[-60..-54, y = 30..35]` and between `OneWay_R_WellCap` `-63` and `OneWay_R_Mid1` `-54`).
  - `Art_R_BigSword`: mounted on upper vault wall at `(x = -95.5f, y = 31.5f), scale = 0.55f` (`x ∈ [-100.40..-90.60], y ∈ [29.60..33.40]`, strictly above `I12_Scroll_WestArmory` at `(-95.0f, 24.0f)`).
  - `rFloorXs`: `{ -90.0f, -76.0f, -55.5f, -35.0f }`.
- **`Room S` & `Room T` (`U1_UpperPromenade.prefab`):**
  - `Art_S_AlchemyBibli1`: `x = 63.2f, y = 24f, scale = 0.58f` (`x ∈ [58.70..67.70], y ∈ [24.0..30.55]`, strictly east of `OneWay_S_Gallery` `[44..58]`).
  - `Art_S_AlchemyBibli2`: `x = 86.2f, y = 24f, scale = 0.58f` (`x ∈ [81.70..90.70], y ∈ [24.0..30.55]`, strictly east of `I10_Chain_ST` `x = 80.0`).
  - `Art_S_AspectsDesk`: `x = 72.0f, y = 24f, scale = 0.75f` (`x ∈ [70.30..73.70]`) + `Art_S_TableauNoir` at `(72.0f, 28.5f), scale = 0.75f`.
  - `sXs` (Room S torches): floor torches at `{ 41.5f, 69.0f, 76.5f }`, ceiling lanterns at `{ 46.0f, 65.0f, 84.0f }`.
  - `Art_T_BigAlcove`: `x = 44.5f, y = 40f, scale = 1.0f` (`x ∈ [42.0..47.0], y ∈ [40.0..45.0]`, strictly west of `OneWay_T_Mid1` `[52..64]`).
  - `Art_T_Pillar1`: `x = 88.0f, y = 40f, scale = 0.90f` (`x ∈ [84.6..91.4], y ∈ [40.0..45.02]`, strictly west of `OneWay_T_Mid2` `[92..106, y = 44.5]`).
  - `Art_T_Pillar2`: `x = 109.0f, y = 40f, scale = 0.85f` (`x ∈ [107.89..110.11], y ∈ [40.0..44.28]`, between `OneWay_T_Mid2` `106` and `OneWay_T_High` `112`).
  - `Art_T_CourtyardArch`: `x = 126.0f, y = 40f, scale = 0.80f` (`x ∈ [122.0..130.0]`, clear of `TP_5_HighBastion` at `x = 132.0f`).
  - `Room T` lighting:
    - Floor torches `tFloorXs = { 40.5f, 68.0f, 94.0f, 118.0f, 136.5f }`
    - Ceiling lanterns `tCeilXs = { 48.0f, 68.0f, 93.0f, 112.0f, 130.0f }` (3rd lantern at `93.0f` clears `Art_T_Banner` at `x = 88.0f`!)
    - Mid-wall torches `tMidXs = { 48.0f, 62.0f, 84.0f, 108.0f, 134.0f }` (at `y = 49f => flameY = 51.2f`, clear of `Bay #8` `[66..74, 46..52]` and all platforms).
