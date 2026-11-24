# Round 4 Vertical Slice (`Main_Level.unity`) — Hybrid Parallax, Dead Cells Shaders, Normal Maps, Dynamic Lighting & R3 P1 Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a commercial-grade 2D visual vertical slice on `Assets/_Project/Scenes/Main_Level.unity` combining a 4-tier Hybrid Parallax depth stack (Dead Cells grayscale + gradient-map far backdrop, existing `Prison` mid parallax, carved `TM_Background` viewing bays, dark foreground silhouettes), Lit-Hybrid character/enemy shaders, auto-generated pixel-art `_NormalMap` textures, live HDR-emissive / volumetric / flickering / shadow-casting 2D lighting, Split-Toning URP post-processing, and full closure of all 4 Round 3 P1 dressing defects — while preserving 100% of existing tests (`46/46` Root) and achieving `>= 4.3 / 5` on the 6-axis 2D Visual Scorecard before replicating the formula to `NhaGiamGiengChuong.unity`.

**Architecture:** Grounded directly in the verified Motion Twin (*Dead Cells*: Sébastien Bénard "Deepnight", Gwenaël Massé grayscale-to-gradient-map parallax, Thomas Vasseur normal-mapped 2D lighting) and The Game Kitchen (*Blasphemous*: multi-tier Unity Sorting Layers + foreground framing + atmospheric color grading) pipelines, adapted natively to Unity `6000.6.9f1` + URP 2D `17.6.0`. Work is decomposed into 6 cleanly bounded, TDD-gated tasks: (1) Sorting Layers & runtime parallax/flicker scripts, (2) Lit-Hybrid character/enemy shaders, (3) 5 environment shaders & materials, (4) automated Sobel `_NormalMap` secondary-texture generator for 35 core `Prison` sprites, (5) grayscale pixel-crisp `ParallaxFar` image-gen backdrop pipeline, and (6) `Main_Level.unity` assembly + R3 P1 cleanup + visual scorecard gate.

**Tech Stack:** Unity `6000.6.9f1`, C# 9, URP 2D `17.6.0` (`Core2D.hlsl`, `Lit2DCommon.hlsl`, `ShapeLightShared.hlsl`, `CombinedShapeLightShared.hlsl`, `Normals2DCommon.hlsl`), `Unity.2D.Sprite.Editor`, `Prison` sprite kit (`24 PPU`), Unity Test Framework `1.8.0`, `rtk` CLI wrapper.

**Spec:**
- Round 3 Strict Deep Review (baseline & P1 list): `C:/Users/habayakka/.gemini/antigravity/brain/f75a39e1-136f-436a-b713-216bcbf66873/deep_review_round3_strict.md`
- Dead Cells Map 1 Benchmark & Map Design Playbook: `C:/Users/habayakka/.gemini/antigravity/brain/27f7fe72-6219-4394-8ed6-62379e395f34/dead_cells_prison_benchmark_review.md` & `dead_cells_map_design_playbook.md`
- Project rules & assembly boundaries: `D:/thietkegame/AGENTS.md` (§§0–2, §9, §10)
- URP Post-Processing & Visual Scorecard references: `urp-postprocessing/SKILL.md`, `urp-postprocessing/references/effect-reference.md`, `craft-game-floors/references/visual-scorecard.md`

## Global Constraints

- **Shell & Encoding:** Run every command through `rtk` (or `rtk proxy <command>`). Read/write all UTF-8 text files without BOM.
- **No Manual `.meta` Edits (`AGENTS.md` §2):** Never hand-create, hand-edit, or delete `.meta` files; let Unity auto-generate/update `.meta` via `AssetDatabase` and `AssetImporter`.
- **Unity `6000.6.9f1` Zero-Warning Policy:** Use `Object.FindObjectsByType<T>()` / `Object.FindObjectsByType<T>(FindObjectsInactive.Include)` and `Object.FindAnyObjectByType<T>()` (never `FindObjectsSortMode` / `FindFirstObjectByType`). Zero `CS0618`, zero `CS0414`, zero shader compile errors (`ShaderUtil.ShaderHasError == false`).
- **Single-Unity-Process Lock (`7.5 GB` RAM Host) & `ProjectSettings` Hygiene:** Ensure `Get-Process Unity -ErrorAction SilentlyContinue` is empty before launching `Unity.exe`. Pass `$env:DOTNET_GCServer='0'` and `-job-worker-count 1` on every Unity invocation; use `-batchmode -nographics` for build/tests, omit `-nographics` only for GPU frame capture. Restore unintended `ProjectSettings/{TimeManager,ProjectSettings}.asset` churn after the final Unity run while keeping `ProjectSettings/TagManager.asset` and `ProjectSettings/EditorBuildSettings.asset`.
- **Assembly Boundaries (`AGENTS.md` §0):** Keep `CameraSystem` at 0 internal references; add only Unity package ref `"Unity.RenderPipelines.Universal.2D.Runtime"` to `VFX.asmdef` (0 internal refs); add `"CameraSystem"` to `TilesetTools.Editor.asmdef` and `Core.Tests.EditMode.asmdef`. Do not introduce new frameworks or alter gameplay physics/colliders.
- **Preserved Invariants:** All `46/46` existing Root tests (`26` NG EditMode + `7` NG PlayMode + `13` Core EditMode) MUST remain 100% green, including `Main_Level` `(40, 14)` `Background` tile, `Archive gallery` 40 boards, `Upgrade panel` `1000×476`, `Exit Door` `LevelExitTrigger`, `PlayerHUD` `_trailFill`, and `Scroll vat` non-`Player_PaletteSwap` check.

## Review Focus

1. **Character readability in unlit pockets & URP 2D Normal pass compatibility:** Lit-Hybrid character shaders (`PaletteSwapLit2D` / `MobFleshSwapLit2D`) must keep `_SelfLitWeight` in `[0.35, 0.55]` and include a `NormalsRendering` pass so Player/Enemies never turn pitch-black in unlit corners or break normal-lit sorting. *(Pinned in Task 2)*
2. **Foreground Point Lights washing out `ParallaxFar`:** Gameplay `Point Light2D`s must exclude the `ParallaxFar` sorting layer ID in `m_ApplyToSortingLayers`, otherwise foreground torches illuminate distant 50m dungeon towers and collapse depth. *(Pinned in Task 6)*
3. **Parallax edge gaps & execution-order jitter during camera shake/dash across `x = [-1, 113]`:** `ParallaxLayer2D` must execute after `CameraFollow2D` (`[DefaultExecutionOrder(50)]`) and wrap seamless panels across the full `114u` corridor bounds with zero visible edge gaps. *(Pinned in Task 1)*
4. **Dead Bloom (`threshold = 1.05` never exceeded) or silent `overrideState = false`:** Every HDR emissive material must carry max linear emission channel in `[1.5, 3.0]` so `Bloom` actually glows, and `ColorAdjustments` + `SplitToning` must set `overrideState = true` on every parameter. *(Pinned in Task 3 & Task 6)*
5. **Floating `TM_WallDecals` inside carved `TM_Background` viewing bays:** Every carved archway opening in `Background` / `East wing background` must also clear `TM_WallDecals` within the opening and be backed by `ParallaxFar` covering full camera vertical bounds `y = [-3, 16]`. *(Pinned in Task 6)*

---

## File Structure Map

| Responsibility | Path | Action | Task |
|---|---|---|---|
| Sorting Layer setup (`ParallaxFar..Foreground`) | `Assets/_Project/Tools/Editor/SortingLayerSetup.cs` | Create | Task 1 |
| Runtime parallax controller (pixel-snap + wrap) | `Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs` | Create | Task 1 |
| Runtime multi-sine torch intensity/radius flicker | `Assets/_Project/Features/VFX/Scripts/TorchFlicker2D.cs` | Create | Task 1 |
| Assembly refs (`VFX`, `TilesetTools.Editor`, `Core.Tests.EditMode`) | `VFX.asmdef`, `TilesetTools.Editor.asmdef`, `Core.Tests.EditMode.asmdef` | Modify | Task 1 |
| Parallax, Sorting Layer & Flicker unit tests | `Assets/_Project/Core/Tests/EditMode/ParallaxAndAtmosphereTests.cs` | Create | Task 1 |
| Player Lit-Hybrid palette-swap shader | `Assets/_Project/Art/Shaders/PaletteSwapLit2D.shader` | Create | Task 2 |
| Enemy Lit-Hybrid flesh-swap shader | `Assets/_Project/Art/Shaders/MobFleshSwapLit2D.shader` | Create | Task 2 |
| Player & 5 Enemy materials migrated to Lit-Hybrid | `Player_PaletteSwap.mat`, `Mob_{zombie,zombieShield,deadArcher,grenadier,Rat}.mat` | Modify | Task 2 |
| Shader, material, normal & backdrop test suite | `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs` | Create | Task 2–5 |
| Dead Cells grayscale-to-gradient-map parallax shader | `Assets/_Project/Art/Shaders/ParallaxGradientMap2D.shader` | Create | Task 3 |
| HDR emissive glow shader (for Bloom > 1.05) | `Assets/_Project/Art/Shaders/EmissiveHdr2D.shader` | Create | Task 3 |
| Additive scrolling god-ray shader | `Assets/_Project/Art/Shaders/AdditiveGodRay2D.shader` | Create | Task 3 |
| Lit vertex-sway shader for banners & hanging vines | `Assets/_Project/Art/Shaders/VertexSway2D.shader` | Create | Task 3 |
| Procedural scrolling ground mist shader | `Assets/_Project/Art/Shaders/GroundMist2D.shader` | Create | Task 3 |
| Idempotent environment material builder + 10 `.mat` assets | `Assets/_Project/Tools/Editor/EnvironmentMaterialBuilder.cs` + `Assets/_Project/Art/Materials/Environment/*.mat` | Create | Task 3 |
| Automated Sobel `_NormalMap` generator + secondary texture binder | `Assets/_Project/Tools/Editor/PrisonNormalMapGenerator.cs` + `Assets/_Project/Art/Environment/Prison/Normals/*_n.png` | Create | Task 4 |
| Image-gen grayscale pixel-crisp `ParallaxFar` texture & processor | `Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png` + `Assets/_Project/Tools/Editor/ParallaxTextureProcessor.cs` | Create | Task 5 |
| Vertical slice scene assembly, SplitToning PostFX & R3 P1 fixes | `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`, `DeadCells_PostProcessProfile.asset`, `Main_Level.unity`, `TagManager.asset` | Modify | Task 6 |
| Vertical slice integration test gate | `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs` | Modify | Task 6 |

---

### Task 1: Sorting Layer Hierarchy & Runtime Atmosphere Components (`ParallaxLayer2D` + `TorchFlicker2D`)

**Files:**
- Modify: `Assets/_Project/Features/VFX/Scripts/VFX.asmdef`
- Modify: `Assets/_Project/Tools/Editor/TilesetTools.Editor.asmdef`
- Modify: `Assets/_Project/Core/Tests/EditMode/Core.Tests.EditMode.asmdef`
- Create: `Assets/_Project/Tools/Editor/SortingLayerSetup.cs`
- Create: `Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs`
- Create: `Assets/_Project/Features/VFX/Scripts/TorchFlicker2D.cs`
- Test: `Assets/_Project/Core/Tests/EditMode/ParallaxAndAtmosphereTests.cs`

**Interfaces:**
- Consumes: `DeadCells.CameraSystem.CameraFollow2D`, `UnityEngine.Rendering.Universal.Light2D`.
- Produces:
  - `DeadCells.Tools.Editor.SortingLayerSetup.EnsureDeadCellsSortingLayers()` — registers exact order `["Default", "ParallaxFar", "ParallaxMid", "Background", "Ground", "Actors", "VFX", "Foreground"]` in `ProjectSettings/TagManager.asset`.
  - `DeadCells.Tools.Editor.SortingLayerSetup.GetSortingLayerId(string layerName) -> int`
  - `DeadCells.CameraSystem.ParallaxLayer2D` (`[DefaultExecutionOrder(50)]`): `Configure(Transform cameraTransform, Vector2 parallaxFactor, float wrapWidthUnits, bool pixelSnap = true, float pixelsPerUnit = 24f)`, `UpdateParallax(Vector3 cameraPosition)` (pure deterministic transform update callable in EditMode tests and `LateUpdate`).
  - `DeadCells.VFX.TorchFlicker2D`: `Configure(Light2D targetLight, float baseIntensity, float baseOuterRadius, float amplitude = 0.08f, float speed = 3.2f, float phaseOffset = 0f)`, `EvaluateIntensity(float time) -> float`, `EvaluateOuterRadius(float time) -> float`, `ApplyAtTime(float time)`.

- [ ] **Step 1: Update `.asmdef` references and write failing tests in `ParallaxAndAtmosphereTests.cs`**

1. Add `"Unity.RenderPipelines.Universal.2D.Runtime"` to `VFX.asmdef`; add `"CameraSystem"` to `TilesetTools.Editor.asmdef` and `Core.Tests.EditMode.asmdef`.
2. Create `Assets/_Project/Core/Tests/EditMode/ParallaxAndAtmosphereTests.cs` with 4 tests:
   - `SortingLayerSetup_RegistersAllDeadCellsLayersInOrderIdempotently`: calls `EnsureDeadCellsSortingLayers()` twice, asserts `SortingLayer.layers.Select(l => l.name)` contains `Default` at index 0 followed by `ParallaxFar`, `ParallaxMid`, `Background`, `Ground`, `Actors`, `VFX`, `Foreground` in strict relative order with no duplicates.
   - `ParallaxLayer2D_AppliesFractionalFactorAndPixelSnaps`: sets origin `(0, 0)`, factor `(0.25f, 0.10f)`, `wrapWidthUnits = 0f`, calls `UpdateParallax(new Vector3(10f, 5f, -10f))`, asserts `position.x` equals `7.5f` within `1f / 24f` (displacement `camX * (1 - factorX)` so visual motion relative to camera is `0.25×`) and snaps to `1/24` grid.
   - `ParallaxLayer2D_WrapsHorizontallyAcrossFullCorridorWithoutEdgeGap` *(Review Focus #3)*: configures `wrapWidthUnits = 32f`, factor `(0.15f, 0f)`, steps camera from `x = -10f` to `x = 120f` in `2f` increments, asserts `Mathf.Abs(layer.transform.position.x - camX) <= wrapWidthUnits * 0.5f + 0.1f` at every step (guaranteeing a 3-panel `96u` span always covers a `~20u` orthographic view with zero gaps).
   - `TorchFlicker2D_OscillatesSmoothlyWithinBoundsAndHandlesNullLight`: evaluates `EvaluateIntensity(t)` & `EvaluateOuterRadius(t)` across `t = 0..10f` (step `0.1f`), asserts values stay strictly within `[base * (1 - amp), base * (1 + amp)]` and vary (`max - min > base * amp * 0.5f`), and verifies calling `ApplyAtTime(1f)` with null `Light2D` does not throw.

- [ ] **Step 2: Run `ParallaxAndAtmosphereTests` to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.ParallaxAndAtmosphereTests" -testResults D:/thietkegame/Logs/r4-t1-red.xml -logFile D:/thietkegame/Logs/r4-t1-red.log
```
Expected: Compile error / Red failure because `SortingLayerSetup`, `ParallaxLayer2D`, and `TorchFlicker2D` do not exist yet.

- [ ] **Step 3: Implement `SortingLayerSetup.cs`, `ParallaxLayer2D.cs`, and `TorchFlicker2D.cs`**

1. **`SortingLayerSetup.cs`:** Load `AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]` into `SerializedObject`, read `m_SortingLayers`, append any missing names from `["ParallaxFar", "ParallaxMid", "Background", "Ground", "Actors", "VFX", "Foreground"]` with deterministic unique non-zero IDs (preserving `Default` at index 0 and existing IDs), call `ApplyModifiedPropertiesWithoutUndo()` + `AssetDatabase.SaveAssets()`.
2. **`ParallaxLayer2D.cs`:** Mark `[DefaultExecutionOrder(50)]` so `LateUpdate()` runs strictly after `CameraFollow2D.LateUpdate()` (default order `0`). Store `_originPosition`, `_cameraTransform`, `_parallaxFactor` (where `0` = locked to camera, `1` = world static, `0.15` = far depth), `_wrapWidthUnits`, `_pixelSnap`, `_pixelsPerUnit`. In `UpdateParallax(Vector3 cameraPosition)`:
   - Compute `float offsetX = (cameraPosition.x - _originPosition.x) * (1f - _parallaxFactor.x);` and `offsetY = (cameraPosition.y - _originPosition.y) * (1f - _parallaxFactor.y);`
   - When `_wrapWidthUnits > 0.01f`, shift `_wrapAnchorX` by multiples of `_wrapWidthUnits` so `Mathf.Abs((_originPosition.x + _wrapAnchorX + offsetX) - cameraPosition.x) <= _wrapWidthUnits * 0.5f`.
   - If `_pixelSnap && _pixelsPerUnit > 0f`, round world X/Y to `1f / _pixelsPerUnit`.
   - In `Start()` / `LateUpdate()`, auto-resolve `Camera.main?.transform` if `_cameraTransform == null` and call `UpdateParallax`.
3. **`TorchFlicker2D.cs`:** Combine two incommensurate sine waves (`0.65f * Sin(w) + 0.35f * Sin(w * 2.37f + 1.1f)`) clamped to `[-1f, 1f]` so intensity & radius breathe organically without harsh strobe. Apply in `Update()` using `Time.time`.

- [ ] **Step 4: Run `ParallaxAndAtmosphereTests` to verify Green pass**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.ParallaxAndAtmosphereTests" -testResults D:/thietkegame/Logs/r4-t1-green.xml -logFile D:/thietkegame/Logs/r4-t1-green.log
```
Expected: `total="4" passed="4" failed="0"`, `0` `CS0618` / `CS0414` in `Logs/r4-t1-green.log`.

- [ ] **Step 5: Checkpoint (ready for Task 2)**

Verify `SortingLayerSetup`, `ParallaxLayer2D`, and `TorchFlicker2D` compile cleanly with zero warnings before moving to Task 2.

---

### Task 2: Character & Enemy Lit-Hybrid Shaders (`PaletteSwapLit2D` & `MobFleshSwapLit2D`)

**Files:**
- Create: `Assets/_Project/Art/Shaders/PaletteSwapLit2D.shader`
- Create: `Assets/_Project/Art/Shaders/MobFleshSwapLit2D.shader`
- Create: `Assets/_Project/Tools/Editor/CharacterLitShaderMigrator.cs`
- Modify (via `CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials()`):
  - `Assets/_Project/Art/Characters/Player/Player_PaletteSwap.mat`
  - `Assets/_Project/Features/Enemies/Materials/Mob_zombie.mat`
  - `Assets/_Project/Features/Enemies/Materials/Mob_zombieShield.mat`
  - `Assets/_Project/Features/Enemies/Materials/Mob_deadArcher.mat`
  - `Assets/_Project/Features/Enemies/Materials/Mob_grenadier.mat`
  - `Assets/_Project/Features/Enemies/Materials/Mob_Rat.mat`
- Test: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`

**Interfaces:**
- Consumes: Existing `PaletteSwap2D.shader` (index lookup `u = (round(index.r * 255) + 0.5) / 256`, `v = (_PaletteRow + 0.5) / _PaletteRows`), `MobFleshSwap2D.shader` (magenta `(1, 0, 1)` → `_FleshColor`), URP 17.6.0 `Core2D.hlsl` / `ShapeLightShared.hlsl` / `CombinedShapeLightShared.hlsl` / `Normals2DCommon.hlsl`.
- Produces:
  - Shader `"DeadCells/PaletteSwapLit2D"` with properties `_MainTex`, `_PaletteTex`, `_PaletteRow`, `_PaletteRows`, `_SelfLitWeight` (default `0.45`), `_Flash` (default `0`), `_MaskTex`, `_NormalMap`.
  - Shader `"DeadCells/MobFleshSwapLit2D"` with properties `_MainTex`, `_FleshColor`, `_SelfLitWeight` (default `0.45`), `_Flash`, `_FlashColor`, `_MaskTex`, `_NormalMap`.
  - `DeadCells.Tools.Editor.CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials()` — idempotently upgrades all 6 character/enemy `.mat` assets while preserving `_PaletteTex`, `_PaletteRow`, `_PaletteRows`, and `_FleshColor`.

- [ ] **Step 1: Write failing tests in `VisualShaderAndNormalTests.cs`**

Create `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs` with test `CharacterLitHybridShaders_CompileCleanlyAndBindPreservedProperties` *(Review Focus #1)*:
- Loads `Shader.Find("DeadCells/PaletteSwapLit2D")` and `Shader.Find("DeadCells/MobFleshSwapLit2D")`, asserts non-null and `ShaderUtil.ShaderHasError(shader) == false`.
- Asserts shader source text for both contains `"LightMode" = "Universal2D"` AND `"LightMode" = "NormalsRendering"`.
- Loads `Player_PaletteSwap.mat`, asserts `shader.name == "DeadCells/PaletteSwapLit2D"`, `GetTexture("_PaletteTex") != null`, `GetFloat("_SelfLitWeight")` in `[0.35f, 0.55f]`, `GetFloat("_PaletteRows") >= 1f`.
- Loads all 5 `Mob_*.mat` materials, asserts each has `shader.name == "DeadCells/MobFleshSwapLit2D"` and `GetFloat("_SelfLitWeight")` in `[0.35f, 0.55f]`.

- [ ] **Step 2: Run test to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/r4-t2-red.xml -logFile D:/thietkegame/Logs/r4-t2-red.log
```
Expected: FAIL (`Shader.Find("DeadCells/PaletteSwapLit2D")` returns null).

- [ ] **Step 3: Implement `PaletteSwapLit2D.shader`, `MobFleshSwapLit2D.shader`, and `CharacterLitShaderMigrator.cs`**

1. **Both Lit-Hybrid shaders (exact URP 17.6.0 contract):**
   - Pass 1 (`Tags { "LightMode" = "Universal2D" }`): include `Core2D.hlsl`, `#include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"`, `CombinedShapeLightShared.hlsl`, `#pragma multi_compile_instancing`.
   - Vertex computes `output.lightingUV = half2(ComputeScreenPos(output.positionCS / output.positionCS.w).xy);` after `UnityFlipSprite(input.positionOS, unity_SpriteProps.xy)`.
   - Fragment performs exact palette/flesh swap (`point` clamp sampling, `clip(alpha - 0.001h)`), constructs `SurfaceData2D` via 5-arg `InitializeSurfaceData(albedo.rgb, albedo.a, half4(1,1,1,1), half3(0,0,1), surfaceData);` and `InputData2D` via `InitializeInputData(input.uv, input.lightingUV, inputData);`, evaluates `half4 lit = CombinedShapeLightShared(surfaceData, inputData);`, blends `half3 finalRgb = lerp(lit.rgb, albedo.rgb, _SelfLitWeight);`, then applies `_Flash` override (`lerp(finalRgb, _FlashColor.rgb, _Flash)` / white) AFTER lighting so hit-flash remains blindingly crisp.
   - Pass 2 (`Tags { "LightMode" = "NormalsRendering" }`): uses `Normals2DCommon.hlsl` (`CommonNormalsVertex` + `CommonNormalsFragment`) with alpha-clip from `_MainTex` so normal-lit 2D passes treat characters cleanly.
   - Keep `CBUFFER_START(UnityPerMaterial)` identical layout across passes for SRP Batcher compatibility.
2. **`CharacterLitShaderMigrator.cs`:** Static `[MenuItem("Tools/DeadCells/Visual/Migrate Character Materials to Lit-Hybrid")]` + `ApplyLitHybridToCharacterMaterials()` that switches shaders on the 6 `.mat` files, sets `_SelfLitWeight = 0.45f`, marks dirty, and calls `AssetDatabase.SaveAssets()`.

- [ ] **Step 4: Run migrator batch method and verify tests pass**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials -quit -logFile D:/thietkegame/Logs/r4-t2-migrate.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/r4-t2-green.xml -logFile D:/thietkegame/Logs/r4-t2-green.log
```
Expected: `total="1" passed="1" failed="0"`, `0` shader errors in both logs.

---

### Task 3: Environment Shaders (Grayscale Gradient Map, HDR Emissive, Additive God-Ray, Vertex Sway, Ground Mist) & Material Assets

**Files:**
- Create: `Assets/_Project/Art/Shaders/ParallaxGradientMap2D.shader`
- Create: `Assets/_Project/Art/Shaders/EmissiveHdr2D.shader`
- Create: `Assets/_Project/Art/Shaders/AdditiveGodRay2D.shader`
- Create: `Assets/_Project/Art/Shaders/VertexSway2D.shader`
- Create: `Assets/_Project/Art/Shaders/GroundMist2D.shader`
- Create: `Assets/_Project/Tools/Editor/EnvironmentMaterialBuilder.cs`
- Create (via Unity `AssetDatabase.CreateAsset`): `Assets/_Project/Art/Materials/Environment/*.mat` (`10` materials)
- Test: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`

**Interfaces:**
- Consumes: URP 17.6.0 `Core2D.hlsl`, `Lit2DCommon.hlsl`, `ShapeLightShared.hlsl`.
- Produces:
  - 5 compiled shaders: `"DeadCells/ParallaxGradientMap2D"`, `"DeadCells/EmissiveHdr2D"`, `"DeadCells/AdditiveGodRay2D"`, `"DeadCells/VertexSway2D"`, `"DeadCells/GroundMist2D"`.
  - `DeadCells.Tools.Editor.EnvironmentMaterialBuilder.EnsureAll()` returning exact paths under `Assets/_Project/Art/Materials/Environment/`:
    - `Mat_ParallaxFar_Prison.mat` (`ParallaxGradientMap2D`, 4-stop Prison cool abyss gradient `#070D11` → `#0F1C1E` → `#1B302C` → `#2E4A42`, `_LitBlend = 0.0f`)
    - `Mat_ParallaxMid_Prison.mat` (`ParallaxGradientMap2D`, `#0B1316` → `#182A26` → `#2B463B` → `#446558`, `_LitBlend = 0.25f`)
    - `Mat_GodRay_Additive.mat` (`AdditiveGodRay2D`, warm shaft tint `(1.0, 0.92, 0.68, 0.26)`, `_ScrollSpeed = 0.14`, `_ShimmerStrength = 0.20`)
    - `Mat_Banner_Sway.mat` (`VertexSway2D`, `_SwayAmplitude = 0.09`, `_SwayFrequency = 1.6`, `_SwayPhaseScale = 0.7`)
    - `Mat_Vine_Sway.mat` (`VertexSway2D`, `_SwayAmplitude = 0.12`, `_SwayFrequency = 1.2`, `_SwayPhaseScale = 0.5`)
    - `Mat_Emissive_Flame.mat` (`EmissiveHdr2D`, `[HDR]` `_EmissionColor = (2.2, 1.25, 0.40, 1)`, `_PulseSpeed = 3.5`, `_PulseAmount = 0.12`)
    - `Mat_Emissive_Mushroom.mat` (`EmissiveHdr2D`, `[HDR]` `_EmissionColor = (0.35, 1.85, 1.55, 1)`, `_PulseSpeed = 1.4`, `_PulseAmount = 0.15`)
    - `Mat_Emissive_Scroll.mat` (`EmissiveHdr2D`, `[HDR]` `_EmissionColor = (2.0, 1.45, 0.45, 1)`, `_PulseSpeed = 2.2`, `_PulseAmount = 0.18`)
    - `Mat_Emissive_Exit.mat` (`EmissiveHdr2D`, `[HDR]` `_EmissionColor = (2.1, 1.75, 0.85, 1)`, `_PulseSpeed = 1.2`, `_PulseAmount = 0.10`)
    - `Mat_GroundMist_Prison.mat` (`GroundMist2D`, `_MistColor = (0.38, 0.54, 0.52, 0.15)`, `_ScrollSpeed = 0.04`, `_NoiseScale = 3.5`)

- [ ] **Step 1: Append failing tests to `VisualShaderAndNormalTests.cs`**

Add `EnvironmentShadersAndMaterials_CompileCleanlyWithValidHdrAndParameters` *(Review Focus #4)*:
- Asserts all 5 environment shaders exist via `Shader.Find(...)` and `ShaderUtil.ShaderHasError(s) == false`.
- Asserts all 10 materials exist under `Assets/_Project/Art/Materials/Environment/` with their expected shader names.
- For all 4 `Mat_Emissive_*.mat`, reads `_EmissionColor`, asserts `Mathf.Max(c.r, c.g, c.b)` is in `[1.5f, 3.0f]` (strictly above `Bloom.threshold = 1.05f`, capped below blowout).

- [ ] **Step 2: Run test to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "EnvironmentShadersAndMaterials" -testResults D:/thietkegame/Logs/r4-t3-red.xml -logFile D:/thietkegame/Logs/r4-t3-red.log
```
Expected: FAIL (5 shaders & 10 materials missing).

- [ ] **Step 3: Implement the 5 environment shaders and `EnvironmentMaterialBuilder.cs`**

1. **`ParallaxGradientMap2D.shader`:** Point-samples `_MainTex` with `uv + float2(_ScrollSpeed * _Time.y, 0)`, computes grayscale `lum = dot(tex.rgb, half3(0.299, 0.587, 0.114))`, evaluates piecewise-linear 4-stop gradient across `_ColorDeep` (`0.0`), `_ColorMidDark` (`0.33`), `_ColorMidLight` (`0.66`), `_ColorHighlight` (`1.0`), optionally blends `_LitBlend` with `CombinedShapeLightShared` (`Universal2D` pass).
2. **`EmissiveHdr2D.shader`:** Unlit `Universal2D` pass (`Blend SrcAlpha OneMinusSrcAlpha`), samples `_MainTex`, multiplies `tex.rgb * _EmissionColor.rgb * (1 + sin(_Time.y * _PulseSpeed) * _PulseAmount) * input.color.rgb` without clamping to 1.0 so HDR values pass cleanly into URP `Bloom`.
3. **`AdditiveGodRay2D.shader`:** `Blend SrcAlpha One`, `ZWrite Off`, modulates `_MainTex` with soft animated diagonal dust shimmer `0.85 + _ShimmerStrength * sin(uv.x * 18.0 + uv.y * 6.0 - _Time.y * _ScrollSpeed * 6.283)` and vertical smoothstep fade (`smoothstep(0.0, 0.18, uv.y) * smoothstep(1.0, 0.88, uv.y)`).
4. **`VertexSway2D.shader`:** Full URP 2D Lit shader (`Universal2D` + `NormalsRendering` via `Lit2DCommon.hlsl` & `Normals2DCommon.hlsl`), adds horizontal sway `sin(_Time.y * _SwayFrequency + worldPos.x * _SwayPhaseScale) * _SwayAmplitude * (1.0 - uv.y)` in vertex stage so top edge (`uv.y = 1`) stays pinned to wall/ceiling while bottom sways smoothly.
5. **`GroundMist2D.shader`:** `Blend SrcAlpha OneMinusSrcAlpha`, `ZWrite Off`, computes two counter-scrolling layered 2D value-noise octaves along `uv.x + _Time.y * _ScrollSpeed`, multiplies by vertical fade `smoothstep(0.0, 0.25, uv.y) * smoothstep(1.0, 0.15, uv.y)` and horizontal edge fade `smoothstep(0.0, 0.08, uv.x) * smoothstep(1.0, 0.92, uv.x)` so ground mist dissolves seamlessly at corridor bounds.
6. **`EnvironmentMaterialBuilder.cs`:** Static `[MenuItem("Tools/DeadCells/Visual/Build Environment Materials")]` + `EnsureAll()` creating/updating all 10 `.mat` assets idempotently under `Assets/_Project/Art/Materials/Environment/`.

- [ ] **Step 4: Build materials & run tests to verify Green pass**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.EnvironmentMaterialBuilder.EnsureAll -quit -logFile D:/thietkegame/Logs/r4-t3-build.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/r4-t3-green.xml -logFile D:/thietkegame/Logs/r4-t3-green.log
```
Expected: `total="2" passed="2" failed="0"`, `0` shader errors.

---

### Task 4: Automated Pixel-Art `_NormalMap` Generator & Secondary Texture Binder (35 Core `Prison` Sprites)

**Files:**
- Create: `Assets/_Project/Tools/Editor/PrisonNormalMapGenerator.cs`
- Create (via generator): `Assets/_Project/Art/Environment/Prison/Normals/*_n.png` (`35` normal maps)
- Modify (via `TextureImporter.secondarySpriteTextures` re-import — zero manual `.meta` edits!): `35` core `Prison/Sprites/*.png` import settings
- Test: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`

**Interfaces:**
- Consumes: `Assets/_Project/Art/Environment/Prison/Sprites/*.png`, `UnityEditor.TextureImporter.secondarySpriteTextures`.
- Produces:
  - `DeadCells.Tools.Editor.PrisonNormalMapGenerator.CoreArchitectureSpriteNames` (`public static readonly string[]`, exact `35` filenames: `backWall_0_00..9_00.png`, `floorStamp_0_00..9_00.png`, `alcovePillar_00.png`, `archeSmall_00.png`, `prisonBigAlcove_00.png`, `woodenPlatform_left_00.png`, `woodenPlatform_0_00..4_00.png`, `woodenPlatform_right_00.png`, `exitDoor_00.png`, `prionSmallWindow_00.png`, `alccoveParalaxeA_00.png`, `alccoveParalaxeB_00.png`, `bannerBig_00.png`, `stoneStamp_0_00.png`).
  - `DeadCells.Tools.Editor.PrisonNormalMapGenerator.GenerateCoreArchitectureNormals()` — generates crisp Sobel tangent-space normal maps and binds `_NormalMap` secondary textures on all 35 source sprites idempotently.

- [ ] **Step 1: Append failing test to `VisualShaderAndNormalTests.cs`**

Add `PrisonCoreArchitectureSprites_HaveNormalMapsGeneratedAndBoundAsSecondaryTextures`:
- Iterates `PrisonNormalMapGenerator.CoreArchitectureSpriteNames` (`Assert.AreEqual(35, ...)`).
- For each filename, asserts `Assets/_Project/Art/Environment/Prison/Normals/<stem>_n.png` exists, its `TextureImporter` has `textureType == TextureImporterType.NormalMap`, `filterMode == FilterMode.Point`, `textureCompression == TextureImporterCompression.Uncompressed`.
- Checks the source sprite's `TextureImporter.secondarySpriteTextures` contains an entry with `name == "_NormalMap"` whose `texture` equals the generated normal map `Texture2D`.

- [ ] **Step 2: Run test to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "PrisonCoreArchitectureSprites" -testResults D:/thietkegame/Logs/r4-t4-red.xml -logFile D:/thietkegame/Logs/r4-t4-red.log
```
Expected: FAIL (`0/35` normal maps exist).

- [ ] **Step 3: Implement `PrisonNormalMapGenerator.cs`**

1. Read each source PNG directly from disk via `File.ReadAllBytes` + `Texture2D.LoadImage` (no need to mutate source `isReadable`).
2. Compute height map `h(x, y) = (0.299 * r + 0.587 * g + 0.114 * b) * a`.
3. Compute 3×3 Sobel gradients `dx`, `dy` with clamped boundary indexing; scale by `strength = 1.6f`, construct tangent-space normal `n = Vector3(-dx * strength, -dy * strength, 1f).normalized` (transparent pixels `a < 0.05` get flat `(0, 0, 1)`), encode `Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f)`.
4. Write PNG to `Assets/_Project/Art/Environment/Prison/Normals/<stem>_n.png`, import with `textureType = TextureImporterType.NormalMap`, `filterMode = FilterMode.Point`, `mipmapEnabled = false`, `textureCompression = TextureImporterCompression.Uncompressed`, `npotScale = TextureImporterNPOTScale.None`.
5. On each source sprite's `TextureImporter`, upsert `new SecondarySpriteTexture { name = "_NormalMap", texture = normalTex }` into `importer.secondarySpriteTextures` (preserving non-`_NormalMap` entries) and call `SaveAndReimport()`.

- [ ] **Step 4: Generate normal maps and verify tests pass**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.PrisonNormalMapGenerator.GenerateCoreArchitectureNormals -quit -logFile D:/thietkegame/Logs/r4-t4-gen.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/r4-t4-green.xml -logFile D:/thietkegame/Logs/r4-t4-green.log
```
Expected: `total="3" passed="3" failed="0"`, `35/35` `_NormalMap` textures bound, `0` warnings.

---

### Task 5: Grayscale Pixel-Crisp `ParallaxFar` Backdrop Asset Pipeline (Image-Gen → Dead Cells Gradient-Map Input)

**Files:**
- Create: `Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png` (`768×256` seamless horizontal pixel-art dungeon depth backdrop, `24 PPU` = `32u × 10.67u` per tile panel)
- Create: `Assets/_Project/Tools/Editor/ParallaxTextureProcessor.cs`
- Test: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`

**Interfaces:**
- Consumes: Raw generated dungeon backdrop image (generated via `image-generator` subagent: wide gothic subterranean prison hall with distant layered arches, stone viaducts, chains, mist — strictly seamless horizontal framing, no characters/UI/text) and `Mat_ParallaxFar_Prison.mat` from Task 3.
- Produces:
  - `Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png` — grayscale, horizontally seamless, pixel-crisp (`FilterMode.Point`, `wrapModeU = Repeat`, `24 PPU`), compressed luminance `[0.12, 0.78]` so `ParallaxGradientMap2D` maps it seamlessly into `Prison`'s cool slate-green palette with low internal contrast.
  - `DeadCells.Tools.Editor.ParallaxTextureProcessor.NormalizeParallaxFarTexture(string rawInputPath)` and zero-arg Unity batch entry point `NormalizeParallaxFarFromDefault()` — processes any raw AI-generated PNG into the exact `768×256` seamless grayscale pixel-art spec and configures Unity `TextureImporter` (`spriteImportMode = Single`, `spritePixelsToUnits = 24`, `filterMode = Point`, `wrapModeU = Repeat`, `wrapModeV = Clamp`, `textureCompression = Uncompressed`).

- [ ] **Step 1: Append failing test to `VisualShaderAndNormalTests.cs`**

Add `ParallaxFarBackdrop_IsSeamlessGrayscalePixelArtWithLowInternalContrast`:
- Asserts `Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png` exists and loads as `Sprite` (`24 PPU`, `768×256`).
- Asserts `TextureImporter.filterMode == FilterMode.Point` and `wrapModeU == TextureWrapMode.Repeat`.
- Reads raw PNG pixels directly from disk, asserts:
  1. Pure grayscale (`max |R - G| <= 2/255` and `max |G - B| <= 2/255`).
  2. Low internal contrast matching Gwenaël Massé's background legibility rule (`stddev(lum) <= 0.24f`, `max(lum) <= 0.82f`, `min(lum) >= 0.06f` — neither crushed black nor competing bright white).
  3. Horizontal seam continuity: mean absolute luminance difference between leftmost column `x = 0` and rightmost column `x = W - 1` is `<= 0.06f`.

- [ ] **Step 2: Run test to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "ParallaxFarBackdrop" -testResults D:/thietkegame/Logs/r4-t5-red.xml -logFile D:/thietkegame/Logs/r4-t5-red.log
```
Expected: FAIL (`T_Prison_ParallaxFar_Grayscale.png` does not exist yet).

- [ ] **Step 3: Generate raw backdrop with `image-generator` and implement `ParallaxTextureProcessor.cs`**

1. Invoke `image-generator` subagent with a strict art-direction brief: *side-scrolling 2D subterranean medieval prison abyss, distant colossal gothic stone arches, deep arched corridors and stone viaduct bridges fading into mist, muted grayscale atmospheric perspective, low contrast, strictly side-view orthographic, zero characters, zero torches/bright flares, zero UI/text, wide 3:1 aspect ratio*. Save raw output to session scratch directory (never pollute `Assets/` with raw unquantized source).
2. Implement `ParallaxTextureProcessor.cs`:
   - Downsamples raw image cleanly to `384×128` then point-scales `2×` to `768×256` (producing chunky `12 PPU` atmospheric far pixels aligned on the `24 PPU` grid — crisp pixel art without noisy 1px static!).
   - Converts to grayscale luminance, compresses range to `[0.12f, 0.78f]` with 16-level ordered quantization (`Dead Cells` pixel-art shading steps), applies 48px cosine horizontal wrap-blend across left/right boundary so `x = 0` and `x = W - 1` tile seamlessly (`diff <= 0.02f`), writes `Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png`, and configures `TextureImporter` (`24 PPU`, `Point`, `Repeat` U / `Clamp` V, `Uncompressed`).

- [ ] **Step 4: Run processor and verify tests pass**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.ParallaxTextureProcessor.NormalizeParallaxFarFromDefault -quit -logFile D:/thietkegame/Logs/r4-t5-proc.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/r4-t5-green.xml -logFile D:/thietkegame/Logs/r4-t5-green.log
```
Expected: `total="4" passed="4" failed="0"`, `0` warnings. Visually inspect `T_Prison_ParallaxFar_Grayscale.png` via `read_file` to confirm architectural silhouettes read clearly before Task 6.

---

### Task 6: `Main_Level.unity` Vertical Slice Assembly (4-Tier Parallax, Carved Archways, R3 P1 Cleanup, Volumetric/Shadow/Flicker/HDR Lighting & SplitToning PostFX) + Visual Scorecard Gate

**Files:**
- Modify: `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`
- Modify (via `RemediateMainLevel`):
  - `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset`
  - `Assets/_Project/Scenes/Main_Level.unity`
  - `ProjectSettings/TagManager.asset` (via `SortingLayerSetup.EnsureDeadCellsSortingLayers()`)
- Test: `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs`

**Interfaces:**
- Consumes:
  - `SortingLayerSetup.EnsureDeadCellsSortingLayers()` & `GetSortingLayerId(string)` (Task 1)
  - `ParallaxLayer2D` & `TorchFlicker2D` (Task 1)
  - `CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials()` (Task 2)
  - `EnvironmentMaterialBuilder.EnsureAll()` & `Assets/_Project/Art/Materials/Environment/*.mat` (Task 3)
  - `PrisonNormalMapGenerator.GenerateCoreArchitectureNormals()` (Task 4)
  - `T_Prison_ParallaxFar_Grayscale.png` (Task 5)
- Produces:
  - Fully remediated `Main_Level.unity` vertical slice + upgraded `DeadCells_PostProcessProfile.asset` passing both all `13/13` legacy Core tests + new Round 4 vertical slice integrity test + `46+` total Root tests + `>= 4.3 / 5` Visual Scorecard inspection.

- [ ] **Step 1: Add failing integration test `MainLevel_Round4_ParallaxLightingShadersAndCleanDressing_ValidatesVerticalSlice` in `MainLevelIntegrityTests.cs`**

Assert all vertical slice contracts and Review Focus items:
1. **R3 P1 defects eliminated:**
   - Zero GameObjects named `Beam_TR_West`, `Beam_TL_East`, `Web_TR_West`, `Web_TL_East`, `Beam_TL_West`, `Beam_TR_East`, or starting with `CornerL_One Way Platform` / `CornerR_One Way Platform` (`P1-M1`, `P1-M2`).
   - Zero `SpriteRenderer`s under `Prison_Dressing_R3` using `floorjunk_1_00` (`2.96u` sarcophagus); `BannerBig_92` bottom (`bounds.min.y`) `>= 2.2f` (strictly clear of `Return to main route` platform top `y = 2.0f`) (`P1-M4`).
   - All 6 `GodRay_0..5` use shader `"DeadCells/AdditiveGodRay2D"` and sit centered below their paired window (`godRay.transform.position.y < windowY - 0.5f`) (`P1-M3`).
2. **4-Tier Parallax & Carved Archways *(Review Focus #5)*:**
   - `Prison_Parallax_R4` contains `ParallaxFar_Backdrop` (`ParallaxLayer2D`, `sortingLayerName == "ParallaxFar"`, `T_Prison_ParallaxFar_Grayscale` + `ParallaxGradientMap2D`, vertical bounds cover `[-3f, 16f]`), `ParallaxMid_Alcoves` (`ParallaxLayer2D`, `sortingLayerName == "ParallaxMid"`, using `alccoveParalaxeA_00` / `alccoveParalaxeB_00` / `paralaxLiane*`), and `Foreground_Silhouettes` (`ParallaxLayer2D`, `sortingLayerName == "Foreground"`, all renderers strictly above combat ceiling `bounds.min.y >= 8.5f`).
   - At least 150 interior cells across the 5 carved archway bays (`[16..22, 4..12]`, `[31..35, 4..12]`, `[52..60, 3..13]`, `[69..76, 5..13]`, `[86..93, 4..13]`) have `Background` / `East wing background` cleared (`GetTile == null`) AND `TM_WallDecals.GetTile(cell) == null` inside every carved bay cell!
   - `(40, 14)` on `Background` remains non-null (preserving existing R3 test).
3. **Sorting Layer Light Isolation, Volumetric, Flicker, ShadowCasters & HDR Emissive *(Review Focus #2 & #4)*:**
   - Every enabled Point `Light2D` in `Main_Level` excludes `SortingLayer.NameToID("ParallaxFar")` in `m_ApplyToSortingLayers`.
   - At least 4 primary Point `Light2D`s have `volumetricEnabled == true`, `volumeIntensity > 0f`, and an active `TorchFlicker2D` component.
   - At least 3 `ShadowCaster2D` components exist on step/ledge geometry (`castsShadows == true`, `selfShadows == false`).
   - At least 5 `SpriteRenderer`s use `"DeadCells/EmissiveHdr2D"` (mushrooms, scroll aura, exit back-glow, lantern flame cores) and at least 1 `GroundMist_R4` renderer uses `"DeadCells/GroundMist2D"`.
   - `BannerBig_*` / `RoomBanner_*` use `"DeadCells/VertexSway2D"`.
4. **Upgraded `DeadCells_PostProcessProfile.asset` *(Review Focus #4)*:**
   - `ColorAdjustments` (`contrast.overrideState && saturation.overrideState`) and `SplitToning` (`shadows.overrideState && highlights.overrideState`) are active with `overrideState == true`, alongside existing `Bloom`, `Vignette`, and `Tonemapping (ACES)`.

- [ ] **Step 2: Run new test to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "MainLevel_Round4_ParallaxLightingShadersAndCleanDressing" -testResults D:/thietkegame/Logs/r4-t6-red.xml -logFile D:/thietkegame/Logs/r4-t6-red.log
```
Expected: FAIL on R3 P1 objects still present, missing `Prison_Parallax_R4`, missing `SplitToning`/`ColorAdjustments`, missing `ShadowCaster2D`/`TorchFlicker2D`.

- [ ] **Step 3: Upgrade `MainLevelSceneRemediator.cs` (PostFX + R3 P1 Cleanup + 4-Tier Parallax + Carved Arches + Lighting/VFX)**

Inside `MainLevelSceneRemediator.cs`:
1. **`EnsureSharedPostProcessProfile()`:** Keep `Bloom` (`threshold = 1.05f`, `intensity = 0.42f`, `scatter = 0.60f`), `Vignette` (`0.24f / 0.45f / #080C12`), `Tonemapping (ACES)`, and upsert `ColorAdjustments` (`contrast = 10f`, `saturation = 10f`, `postExposure = 0.05f`, all `overrideState = true`) + `SplitToning` (`shadows = #1D2E42`, `highlights = #F5C07A`, `balance = -10f`, all `overrideState = true`) with `EnsureSubAsset(...)`.
2. **Pass 9–10 (`RemediateMainLevel`):** After filling `Background` (`x=-1..52, y=-4..15`) and `East wing background` (`x=52..111, y=-1..15`), carve the 5 Gothic viewing bays `CarvedArchBays = { (16, 22, 4, 12), (31, 35, 4, 12), (52, 60, 3, 13), (69, 76, 5, 13), (86, 93, 4, 13) }` (rounded arched top corners: keep the 2 top-corner cells `(xMin, yMax-1)` and `(xMax-1, yMax-1)` solid so each opening reads as a vaulted Gothic arch, not a sharp rectangle!). Clear `TM_WallDecals` strictly across all carved cells so zero decals float in mid-air.
3. **Pass 12 (`BuildPrisonDressing` — R3 P1 Cleanup + Live Shaders):**
   - Remove floating `Beam_*`, `Web_*`, and `CornerL/R_One Way Platform*` completely.
   - Replace `FloorJunk` sprites with low rubble only (`floorjunk_6_00`, `floorjunk_7_00`, `floorjunk_8_00`, `floorjunk_9_00`, all `<= 0.62u`).
   - Frame the 5 carved arch bays with `littleBlackBeam_topLeft/Right_0_00` pinned flush inside the actual arch top corners (`y = yMax - 1`, `sort = -15`) and subtle `webSmallTL/TR_00` inside the arch corners.
   - Move `BannerBig_92` → `(89.5f, 7.6f)` (scale `0.78f` so bottom stays `>= 4.1f`, far clear of `Return to main route`), and assign `Mat_Banner_Sway.mat` to all `BannerBig_*` & `RoomBanner_*`.
   - Re-align all 6 `GodRay_0..5` to `coord + new Vector2(0.55f, -1.25f)` using `Mat_GodRay_Additive.mat`, scale `(0.42f, 0.48f, 1f)`, rotation `-18°`, `SortingLayer: VFX`, `sort = 10`.
   - Add 4 damp-corner `glowingMushroom_0..3_00` clusters (`x = 16.5, 51.0, 69.0, 93.0`, grounded flush on floor, scale `0.55f`, `Mat_Emissive_Mushroom.mat`, `SortingLayer: Ground`, `sort = -7`), `ExitDoor_BackGlow` (`monsterDoorBackGlow_00.png` behind `Exit Door` at `(108, 2.2)`, `Mat_Emissive_Exit.mat`, `sort = -11`), and `ScrollAura_R4` subtle emissive halos (`Mat_Emissive_Scroll.mat`) behind both scrolls.
4. **Pass 13 (`BuildParallaxAndAtmosphereR4`):**
   - Call `SortingLayerSetup.EnsureDeadCellsSortingLayers()` and `EnvironmentMaterialBuilder.EnsureAll()`.
   - Assign Sorting Layers across scene (`Background`, `East wing background`, `TM_WallDecals`, pillars, windows, banners → `"Background"`; `Stone visuals`, `East wing stone`, `TM_SolidRim`, platforms, floor junk, exit door → `"Ground"`; `Player`, `Enemy_*` → `"Actors"`; `GodRays`, `AmbientDustMotes`, `GroundMist_R4`, `TorchEmbers` → `"VFX"`).
   - Build idempotent `Prison_Parallax_R4` container:
     - `ParallaxFar_Backdrop`: `ParallaxLayer2D` (`factor = (0.15f, 0.06f)`, `wrapWidthUnits = 32f`), 3 tiled side-by-side `SpriteRenderer` panels (`-32, 0, +32`, each `32u × 20u` covering `y = [-3.5, 16.5]` at `Mat_ParallaxFar_Prison.mat`, `SortingLayer: ParallaxFar`).
     - `ParallaxMid_Alcoves`: `ParallaxLayer2D` (`factor = (0.38f, 0.14f)`, `wrapWidthUnits = 32f`), repeating `alccoveParalaxeA_00` & `alccoveParalaxeB_00` arches (`Mat_ParallaxMid_Prison.mat`) + swaying `paralaxLianeA_00`/`B_00` (`Mat_Vine_Sway.mat`) across `x = [-16, 128]`, `SortingLayer: ParallaxMid`.
     - `Foreground_Silhouettes`: `ParallaxLayer2D` (`factor = (1.18f, 1.0f)`, `wrapWidthUnits = 0f`), sparse dark `#080C12` ceiling arches/chains placed strictly at `y >= 9.5f` (`bounds.min.y >= 8.5f`) so combat view is 100% unobstructed, `SortingLayer: Foreground`.
   - Build `GroundMist_R4` quad/sprite strips along low cell blocks (`x = [15, 28], y = -1..0.6` and `x = [68, 94], y = -1..0.6`) using `Mat_GroundMist_Prison.mat`, `SortingLayer: VFX`, `sort = -1`.
   - Configure lighting:
     - `Ambient light` (`Global Light 2D`): `intensity = 0.52f`, `color = (0.71f, 0.82f, 0.89f, 1f)`, targeting all sorting layers.
     - All gameplay Point `Light2D`s: set `m_ApplyToSortingLayers` via `SerializedObject` to `[Default, Background, Ground, Actors, VFX]` (explicitly excluding `ParallaxFar` and `ParallaxMid`!).
     - On primary lanterns (`Accent light` × 4, `Gallery lantern`, `Archive lantern`, `Exit hall lantern`): enable `volumetricEnabled = true`, `volumeIntensity = 0.10f`, attach `TorchFlicker2D` (`amp = 0.08f`, `speed = 3.2f`, staggered phase), add crisp `Mat_Emissive_Flame.mat` core sprite + tiny `TorchEmbers` child `ParticleSystem` (`maxParticles = 6`, warm HDR startColor, `ParticlesUnlit.mat`, `SortingLayer: VFX`).
   - Build `ShadowCasters_R4` container with 3 `ShadowCaster2D` (`m_ShadowCastingSource = 1` ShapeEditor, `castsShadows = true`, `selfShadows = false`, `m_ApplyToSortingLayers = [Background, Ground]`) framing the solid stone step-terraces (`x = 37..45` step wall, `x = 68` drop-off, `x = 94` exit step) so point lights cast crisp architectural shadow falloff along floor elevation changes.

- [ ] **Step 4: Run `RemediateMainLevel`, execute full Root test suite (`100%` green), restore `ProjectSettings` churn, and run visual verification**

Run:
```powershell
# Ensure NO other Unity.exe process is running first!
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.RemediateMainLevel -quit -logFile D:/thietkegame/Logs/r4-mainlevel-remediate.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests" -testResults D:/thietkegame/Logs/r4-core-green.xml -logFile D:/thietkegame/Logs/r4-core-green.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests" -testResults D:/thietkegame/Logs/r4-ng-edit-regression.xml -logFile D:/thietkegame/Logs/r4-ng-edit-regression.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform PlayMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.Play" -testResults D:/thietkegame/Logs/r4-ng-play-regression.xml -logFile D:/thietkegame/Logs/r4-ng-play-regression.log
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
```
Expected:
- `r4-core-green.xml`: **`22/22` Passed** (`13` existing + `4` Task 1 + `4` Task 2–5 + `1` Task 6), `0` failed, `0` `CS0618`, `0` `CS0414`, `0` shader errors.
- `r4-ng-edit-regression.xml`: **`26/26` Passed**, `r4-ng-play-regression.xml`: **`7/7` Passed** (zero regressions to `NhaGiamGiengChuong`!).

- [ ] **Step 5: Visual Scorecard Capture & Human Eye-Test Gate (`craft-game-floors`)**

1. Re-run 1:1 visual render inspection on `Main_Level.unity` (verifying carved arch bays revealing `ParallaxMid` & `ParallaxFar`, zero floating beams/brackets, grounded rubble, aligned God-rays) AND capture real GPU frames in Unity Editor (`unity-pipeline` / `Main Camera` Game View after 1 discarded warm-up frame per `visual-scorecard.md` Steps 1–3: standard corridor gameplay frame, Player near lantern close-up proving Lit-Hybrid + NormalMap + Bloom, and carved-arch parallax frame).
2. Score all 6 axes (`palette_discipline`, `style_uniformity`, `lighting_depth`, `silhouette`, `ui_intentionality`, `feedback_visibility`) — every axis must hit `>= 4.0 / 5` (average `>= 4.3 / 5`) before replicating the proven pattern to `NhaGiamGiengChuong.unity` in a follow-up plan.
