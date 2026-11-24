# Round 4.2 Visual P1 Closure (`Main_Level.unity` Vertical Slice) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close all `3` visual P1 regressions identified by the independent Round 4.1 strict review (`[P1-R1]` `2.42×` mixel & dangling-chain `LanternFixture_R4`, `[P1-R2]` opaque sticker-orb `MushroomHalo_0..3` bleeding below floor & competing with `Power Scroll`, `[P1-R3]` `3.57:1` squashed `ArchSill_*` slicing across `Stone pillar` capitals) plus `2` targeted P2 visual defects (`[P2-1]` `ExitDoor_BackGlow` yellow floor-seam leak, `[P2-3]` `BannerBig_92` cornice overlap), pushing `Main_Level.unity` from `4.13/5` (`NO-GO`) to `>= 4.50/5` (`GO`, all 6 axes `>= 4.1/5`, `0` P0/P1) with `58/58` green tests and real RTX 2050 GPU verification before Play Mode review and replication to `NhaGiamGiengChuong.unity`.

**Architecture:** Preserve 100% of Round 4 / R4.1 rendering architecture, runtime scripts, `.asmdef` boundaries, and gameplay physics/colliders. Introduce one deterministic Editor prop sprite generator (`PrisonPropSpriteGenerator.cs`) reusing `PrisonNormalMapGenerator`'s Sobel pipeline to author exact `24 PPU` (`scale = 1.0`, `1:1` pixel density) wrought-iron wall-mounted sconce lanterns (`prisonWallLantern_00.png`) and carved stone arch sills (`prisonArchSill_00.png`) with bound `_NormalMap` textures; add a backwards-compatible `_DstBlend` mode to `EmissiveHdr2D.shader` so `Mat_Emissive_MushroomHalo.mat` renders a tiny cap-aligned additive glow backed by soft isolated local cyan bounce `Point Light2D`s; place `ArchSill_*` on `Background/-17` behind `Stone pillar` (`Background/-16`); and lock every fix with EditMode regression tests + real RTX 2050 GPU pixel verification.

**Tech Stack:** Unity `6000.6.9f1`, C# 9, URP 2D `17.6.0` (`Core2D.hlsl`, `Light2D`), Dead Cells `Prison` `24 PPU` pixel-art pipeline, Unity Test Framework `1.8.0`, `rtk` CLI wrapper, Python/PIL read-only GPU pixel verifier.

**Spec:**
- Independent Round 4.1 Strict Deep Review (source of `3` P1 + `P2-1/P2-3`): `C:/Users/habayakka/.gemini/antigravity/brain/c632aa93-c9f8-491c-a521-a6fb809cdd86/deep_review_round41_strict.md`
- Round 4 Baseline Strict Review: `C:/Users/habayakka/.gemini/antigravity/brain/f75a39e1-136f-436a-b713-216bcbf66873/deep_review_round4_strict.md`
- Project rules & assembly boundaries: `D:/thietkegame/AGENTS.md` (§§0–2, §6, §9, §10)
- Rubric & URP PostFX references: `C:/Users/habayakka/.gemini/config/plugins/unity/skills/using-unity/references/craft-game-floors/references/visual-scorecard.md` (2D Anchor Set), `C:/Users/habayakka/.gemini/config/plugins/unity/skills/using-unity/references/urp-postprocessing/urp-postprocessing.md`

## Global Constraints

- **Shell & Encoding:** Run every shell command through `rtk` (or `rtk proxy <command>`). Read/write all UTF-8 text files without BOM.
- **Strict Scope Isolation (`AGENTS.md` §2):** Do **NOT** modify `NhaGiamGiengChuong.unity`, `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/*.prefab`, `.fortress-workspaces/antigravity`, any `.asmdef`, any runtime script under `Assets/_Project/Core/Scripts/` or `Assets/_Project/Features/`, or any gameplay `BoxCollider2D`/physics component. Do **NOT** run `git commit`.
- **No Manual `.meta` Edits (`AGENTS.md` §2):** Never hand-create, hand-edit, or delete `.meta` files; let Unity generate/update `.meta` via `AssetDatabase.Refresh` and `TextureImporter.SaveAndReimport()`.
- **Unity `6000.6.9f1` Zero-Warning Policy:** Use `UnityEngine.Object.FindObjectsByType<T>()` / `FindObjectsByType<T>(FindObjectsInactive.Include)` and `FindAnyObjectByType<T>()`. Zero `CS0618`, zero `CS0414`, zero `warning CS`, zero `error CS`, zero `ShaderUtil.ShaderHasError`, `URP_PreFlight 5/5 True`, `defaultMaterialRenderers=0`.
- **Single-Unity-Process Lock (`7.5 GB` RAM Host) & `ProjectSettings` Hygiene:** Verify `Get-Process Unity -ErrorAction SilentlyContinue` is empty before every Unity call. Pass `$env:DOTNET_GCServer='0'` and `-job-worker-count 1` on every Unity invocation; pass `--burst-disable-compilation` for PlayMode and GPU capture runs; use `-batchmode -nographics` for remediate/tests and omit `-nographics` only for GPU capture. After the final Unity invocation, run `rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset` while preserving `ProjectSettings/TagManager.asset` and `ProjectSettings/EditorBuildSettings.asset`.
- **Preserved Invariants:** Keep `PrisonNormalMapGenerator.CoreArchitectureSpriteNames.Length == 36` intact; keep all `56/56` existing tests (`23` Core EditMode + `26` NG EditMode + `7` NG PlayMode) 100% green plus `2` new R4.2 tests (`58/58` total).

## Review Focus

1. **Pixel-density uniformity (`1:1` `24 PPU` across generated props & mushrooms):** `LanternFixture_R4` (`prisonWallLantern_00.png`), `ArchSill_*` (`prisonArchSill_00.png`), and `GlowingMushroom_0..3` (`glowingMushroom_3_00.png`) must all run at `sprite.pixelsPerUnit == 24f` and `localScale == (1, 1, 1)` (`effectivePpu == 24f`) so zero mixel artifacts (`> 1.05×`) appear beside `backWall`/`alcovePillar` (`24 PPU`). *(Pinned in Task 1 & Task 3)*
2. **Backwards compatibility of `EmissiveHdr2D.shader` `_DstBlend` property:** Adding `_DstBlend` must default to `10` (`BlendMode.OneMinusSrcAlpha`) so `Mat_Emissive_Flame`, `Mat_Emissive_Mushroom`, `Mat_Emissive_Scroll`, and `Mat_Emissive_Exit` keep identical rendering, while only `Mat_Emissive_MushroomHalo.mat` uses `_DstBlend = 1` (`BlendMode.One`, additive) with sub-threshold color (`maxChannel < 1.05`) so it never triggers an opaque Bloom orb. *(Pinned in Task 2)*
3. **Sorting-layer isolation of new `MushroomLight_0..3` Point `Light2D`s & no accidental lantern fixture injection:** Every `MushroomLight_*` must exclude `ParallaxFar`, `ParallaxMid`, and `Foreground` in `m_ApplyToSortingLayers`, stay excluded from `IsPrimaryLanternLight` (no `LanternFixture_R4` / `TorchFlicker2D` / `TorchEmbers` children), and remain strictly idempotent across repeated `RemediateMainLevel()` runs. *(Pinned in Task 3)*
4. **Occluded `ArchSill_*` behind `Stone pillar` & non-bleeding `MushroomHalo` / `ExitDoor_BackGlow` vertical bounds:** `ArchSill_*` must sort at `Background/-17` (strictly `< Stone pillar` `-16` and `> Background` `-20` / `TM_WallDecals` `-18`); `MushroomHalo_0..3` `bounds.min.y` must remain strictly `> floorTop` (zero below-floor cyan bleed); `ExitDoor_BackGlow` `bounds.min.y` must remain strictly `>= 0.25f` (zero yellow bleed through `floorStamp_*` top crevices). *(Pinned in Task 3 & Task 4)*
5. **`BannerBig_92` clean separation from both `CarvedArchBays[4]` and `Exit hall pillar`:** `BannerBig_92` must stay strictly right of `x = 93.0`, strictly above `Exit hall pillar` (`bounds.min.y > 5.58f`), and retain its top mount on a solid `East wing background` tile (`y <= 15`). *(Pinned in Task 3)*

---

## File Structure Map

| Responsibility | Path | Action | Task |
|---|---|---|---|
| Reusable single-sprite Sobel `_NormalMap` generator + secondary binder | `Assets/_Project/Tools/Editor/PrisonNormalMapGenerator.cs` | Modify | Task 1 |
| Deterministic `24 PPU` wall sconce lantern + carved arch sill sprite generator | `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs` | Create | Task 1 |
| Generated `24 PPU` sprites + Sobel normal maps (`Unity`-managed `.meta`) | `Assets/_Project/Art/Environment/Prison/Sprites/{prisonWallLantern_00,prisonArchSill_00}.png`, `Assets/_Project/Art/Environment/Prison/Normals/{prisonWallLantern_00_n,prisonArchSill_00_n}.png` | Generated by Task 1 | Task 1 |
| Configurable destination blend mode (`_DstBlend`) on emissive shader | `Assets/_Project/Art/Shaders/EmissiveHdr2D.shader` | Modify | Task 2 |
| Additive sub-threshold mushroom aura material (`Mat_Emissive_MushroomHalo.mat`) | `Assets/_Project/Tools/Editor/EnvironmentMaterialBuilder.cs`, `Assets/_Project/Art/Materials/Environment/Mat_Emissive_MushroomHalo.mat` | Modify / Generated | Task 2 |
| Shader, material & generated-prop unit tests | `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs` | Modify | Task 1 & Task 2 |
| `Main_Level.unity` R4.2 remediation (`P1-R1/R2/R3`, `P2-1/P2-3`) | `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`, `Assets/_Project/Scenes/Main_Level.unity` | Modify | Task 3 |
| R4.2 scene integration & regression lock test | `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs` | Modify | Task 3 |
| Full test suite (`58/58`) + RTX 2050 GPU verification & pixel metrics | `Logs/r42-*.{xml,log}`, GPU captures `gpu_r42_01..04.png` (outside repo) | Execute | Task 4 |

---

### Task 1: Deterministic `24 PPU` Prison Prop Generator (`prisonWallLantern_00` & `prisonArchSill_00`) + Shared Normal API

**Files:**
- Modify: `Assets/_Project/Tools/Editor/PrisonNormalMapGenerator.cs:57-105`
- Create: `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs`
- Test: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs:232-285`

**Interfaces:**
- Consumes: `PrisonNormalMapGenerator` private Sobel & secondary-texture helpers (`GenerateSobelNormalPng`, `WriteBytesIfChanged`, `ConfigureNormalImporter`, `BindSecondaryNormalTexture`).
- Produces:
  - `DeadCells.Tools.Editor.PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(string spritePath)` — generates `<NormalDirectory>/<stem>_n.png`, reimports as `NormalMap` / `Point` / `Uncompressed` / `mipmapEnabled = false`, and binds `_NormalMap` on `spritePath`'s `TextureImporter`.
  - `DeadCells.Tools.Editor.PrisonPropSpriteGenerator.WallLanternAssetPath` (`"Assets/_Project/Art/Environment/Prison/Sprites/prisonWallLantern_00.png"`)
  - `DeadCells.Tools.Editor.PrisonPropSpriteGenerator.ArchSillAssetPath` (`"Assets/_Project/Art/Environment/Prison/Sprites/prisonArchSill_00.png"`)
  - `DeadCells.Tools.Editor.PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY` (`0.125f`)
  - `DeadCells.Tools.Editor.PrisonPropSpriteGenerator.EnsureAll()` (also exposed as `[MenuItem("Tools/DeadCells/Visual/Generate 24-PPU Prison Props")]`) — idempotently writes both PNGs, configures `TextureImporter` (`Sprite`, `Single`, `24 PPU`, `FilterMode.Point`, `Uncompressed`, `mipmapEnabled = false`, `npotScale = None`, `pivot = (0.5, 0.5)`), and invokes `PrisonNormalMapGenerator.GenerateAndBindNormalForSprite` for each.

- [ ] **Step 1: Write the failing test in `VisualShaderAndNormalTests.cs`**

Add `[Test] public void PrisonGeneratedProps_Are24PpuPointUncompressedWithBoundNormalMaps()` right after `PrisonCoreArchitectureSprites_HaveNormalMapsGeneratedAndBoundAsSecondaryTextures`:

```csharp
[Test]
public void PrisonGeneratedProps_Are24PpuPointUncompressedWithBoundNormalMaps()
{
    var expectedProps = new (string path, int width, int height, string normalPath)[]
    {
        (PrisonPropSpriteGenerator.WallLanternAssetPath, 20, 30,
            "Assets/_Project/Art/Environment/Prison/Normals/prisonWallLantern_00_n.png"),
        (PrisonPropSpriteGenerator.ArchSillAssetPath, 24, 8,
            "Assets/_Project/Art/Environment/Prison/Normals/prisonArchSill_00_n.png"),
    };

    Assert.AreEqual(0.125f, PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY, 1e-4f);

    foreach (var spec in expectedProps)
    {
        Assert.IsTrue(File.Exists(spec.path), $"Missing generated prop sprite '{spec.path}'.");
        Assert.IsTrue(File.Exists(spec.normalPath), $"Missing generated prop normal '{spec.normalPath}'.");

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.path);
        Assert.IsNotNull(sprite, $"Failed to load Sprite '{spec.path}'.");
        Assert.AreEqual(24f, sprite.pixelsPerUnit, 1e-4f, $"'{spec.path}' must use 24 PPU.");
        Assert.AreEqual(spec.width, Mathf.RoundToInt(sprite.rect.width), $"'{spec.path}' width mismatch.");
        Assert.AreEqual(spec.height, Mathf.RoundToInt(sprite.rect.height), $"'{spec.path}' height mismatch.");

        TextureImporter importer = AssetImporter.GetAtPath(spec.path) as TextureImporter;
        Assert.IsNotNull(importer);
        Assert.AreEqual(FilterMode.Point, importer.filterMode);
        Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        Assert.IsFalse(importer.mipmapEnabled);

        Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.normalPath);
        Assert.IsNotNull(normalTex);
        TextureImporter normalImporter = AssetImporter.GetAtPath(spec.normalPath) as TextureImporter;
        Assert.IsNotNull(normalImporter);
        Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType);
        Assert.AreEqual(FilterMode.Point, normalImporter.filterMode);

        SecondarySpriteTexture[] secondary = importer.secondarySpriteTextures;
        Assert.IsTrue(
            secondary != null && System.Array.Exists(secondary, s => s.name == "_NormalMap" && s.texture == normalTex),
            $"'{spec.path}' must bind '{spec.normalPath}' as secondary texture '_NormalMap'.");
    }
}
```

- [ ] **Step 2: Run `VisualShaderAndNormalTests` to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests.PrisonGeneratedProps_Are24PpuPointUncompressedWithBoundNormalMaps" -testResults D:/thietkegame/Logs/r42-t1-red.xml -logFile D:/thietkegame/Logs/r42-t1-red.log
```
Expected: Compile error / Red failure because `PrisonPropSpriteGenerator` does not exist yet.

- [ ] **Step 3: Expose `PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(string spritePath)`**

In `Assets/_Project/Tools/Editor/PrisonNormalMapGenerator.cs`:
- Keep `CoreArchitectureSpriteNames` untouched (`36` entries).
- Add `public static void GenerateAndBindNormalForSprite(string spritePath)`: validates `File.Exists(spritePath)`, computes `normalPath = $"{NormalDirectory}/{Path.GetFileNameWithoutExtension(spritePath)}_n.png"`, writes `GenerateSobelNormalPng(spritePath)` via `WriteBytesIfChanged`, calls `AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport)`, runs `ConfigureNormalImporter(normalPath)`, loads `Texture2D`, and calls `BindSecondaryNormalTexture(spritePath, normalTex)`.

- [ ] **Step 4: Implement `PrisonPropSpriteGenerator.cs` and generate both `24 PPU` props**

Create `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs` implementing `EnsureAll()` with deterministic pixel-grid rasterizers (bottom-up Y `0..height-1`, transparent alpha `0` outside silhouette):
1. **`prisonWallLantern_00.png` (`20×30 px`, `24 PPU`, pivot `(0.5, 0.5)` at `y = 15`):**
   - **Wall sconce backplate (`x = 7..12, y = 24..29`):** Dark wrought-iron plate (`#1B2128` 1px outline, `#36404B` iron fill, `#566574` top bevel highlight + 2 bolt highlights at `(8,27)` and `(11,27)`) bolted flush onto the wall behind it — **zero dangling chain**.
   - **Bracket neck & mounting collar (`x = 8..11, y = 21..24`):** Iron collar connecting plate to lantern roof (`#1B2128` / `#36404B` / `#4E5B69`).
   - **Pitched iron lantern hood (`y = 18..21`):** Tapered cap widening from `x = 7..12` (`y = 20`) to `x = 4..15` (`y = 18`) with `#5E6D7C` top rim highlight and `#1B2128` outline.
   - **Glass lantern cage (`x = 5..14, y = 6..17`, centered at `y = 12` ⇒ `WallLanternFlameOffsetLocalY = (15 - 12) / 24f = 0.125f`):** 1px iron side posts (`x = 5, 14`) + thin vertical center mullion (`x = 9..10` tinted dark bronze `#3B281B` only near top/bottom so flame reads cleanly); inner warm amber glass gradient (`#9E3E12` edges → `#E07E22` mid → `#FFD86B` core around `x = 8..11, y = 10..14`).
   - **Lower iron cradle & tapered finial (`y = 1..5`):** Base rim `x = 4..15` (`y = 5`), narrowing down to `x = 9..10` (`y = 1`) in `#1B2128` / `#36404B` / `#4E5B69`.
2. **`prisonArchSill_00.png` (`24×8 px`, `24 PPU`, pivot `(0.5, 0.5)`):**
   - Seamless horizontally tileable carved stone sill in Dead Cells Prison slate-moss palette (`backWall`/`alcovePillar` compatible):
     - `y = 7` (top rim): `1px` crisp dark outline `#18201E`
     - `y = 6` (lit stone bevel): `#667567` with subtle 1px wear notches (`#4A584D`) at `x = 7, 15, 23`
     - `y = 2..5` (chiseled stone body): `#3C4940` / `#47564B` stone blocks with 1px dark vertical mortar grooves (`#232C28`) at `x = 7, 15, 23`
     - `y = 1` (under-lip shadow): `#242E2A`
     - `y = 0` (bottom edge): `#151C1A`
3. Configure both importers (`Sprite`, `Single`, `spritePixelsPerUnit = 24f`, `FilterMode.Point`, `Uncompressed`, `mipmapEnabled = false`, `npotScale = None`, `wrapModeU = Repeat` on `prisonArchSill_00`, `Clamp` on `prisonWallLantern_00`), then call `PrisonNormalMapGenerator.GenerateAndBindNormalForSprite` on both.

- [ ] **Step 5: Run generator & verify Task 1 test turns Green**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.PrisonPropSpriteGenerator.EnsureAll -quit -logFile D:/thietkegame/Logs/r42-t1-gen.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/r42-t1-green.xml -logFile D:/thietkegame/Logs/r42-t1-green.log
```
Expected: `total="5" passed="5" failed="0"`, `0` `CS0618`/`CS0414` in `Logs/r42-t1-green.log`.

---

### Task 2: Additive `_DstBlend` on `EmissiveHdr2D.shader` & `Mat_Emissive_MushroomHalo.mat`

**Files:**
- Modify: `Assets/_Project/Art/Shaders/EmissiveHdr2D.shader:5-30`
- Modify: `Assets/_Project/Tools/Editor/EnvironmentMaterialBuilder.cs:17-55,133-182,268-283`
- Test: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs:78-221`

**Interfaces:**
- Consumes: `DeadCells/EmissiveHdr2D` shader (`Assets/_Project/Art/Shaders/EmissiveHdr2D.shader`).
- Produces:
  - Shader property `[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10` controlling `Blend SrcAlpha [_DstBlend]` in `DeadCells/EmissiveHdr2D`.
  - Material asset `Assets/_Project/Art/Materials/Environment/Mat_Emissive_MushroomHalo.mat` (`MaterialPaths[11]`, `_DstBlend = 1f`, `_EmissionColor = (0.18, 0.85, 0.78, 1.0)`, `_PulseSpeed = 1.4f`, `_PulseAmount = 0.15f`).
  - Existing 4 HDR materials (`Mat_Emissive_{Flame,Mushroom,Scroll,Exit}.mat`) explicitly preserve `_DstBlend = 10f` and existing HDR `_EmissionColor` values.

- [ ] **Step 1: Extend `EnvironmentShadersAndMaterials_CompileCleanlyWithValidHdrAndParameters` in `VisualShaderAndNormalTests.cs` (Red)**

1. Add `"_DstBlend"` to `expectedShaders` required props for `"DeadCells/EmissiveHdr2D"`.
2. Add `("Mat_Emissive_MushroomHalo.mat", "DeadCells/EmissiveHdr2D")` to `expectedMaterials`.
3. Inside the `emissiveMaterials` loop (`Flame`, `Mushroom`, `Scroll`, `Exit`), assert `Assert.AreEqual(10f, mat.GetFloat("_DstBlend"), 1e-4f, ...)` (verifies backwards-compatible `OneMinusSrcAlpha`).
4. Add explicit assertions for `Mat_Emissive_MushroomHalo.mat`:
   - `Assert.AreEqual(1f, haloMat.GetFloat("_DstBlend"), 1e-4f, "Mat_Emissive_MushroomHalo must use additive _DstBlend == 1 (BlendMode.One).")`
   - `float haloMax = Mathf.Max(haloEm.r, Mathf.Max(haloEm.g, haloEm.b));`
   - `Assert.That(haloMax, Is.InRange(0.5f, 1.0f), "Mat_Emissive_MushroomHalo must stay strictly below Bloom threshold (1.05) to avoid opaque sticker orbs.");`
   - `Assert.AreEqual(1.4f, haloMat.GetFloat("_PulseSpeed"), 1e-4f);`
   - `Assert.AreEqual(0.15f, haloMat.GetFloat("_PulseAmount"), 1e-4f);`

- [ ] **Step 2: Run `VisualShaderAndNormalTests` to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests.EnvironmentShadersAndMaterials_CompileCleanlyWithValidHdrAndParameters" -testResults D:/thietkegame/Logs/r42-t2-red.xml -logFile D:/thietkegame/Logs/r42-t2-red.log
```
Expected: FAIL — missing `_DstBlend` property and missing `Mat_Emissive_MushroomHalo.mat`.

- [ ] **Step 3: Update `EmissiveHdr2D.shader` and `EnvironmentMaterialBuilder.cs`**

1. In `Assets/_Project/Art/Shaders/EmissiveHdr2D.shader`:
   - Add `[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10` to `Properties` (does not enter `UnityPerMaterial` CBUFFER since it is a render-state tag only).
   - Replace `Blend SrcAlpha OneMinusSrcAlpha` (`L25`) with `Blend SrcAlpha [_DstBlend]`.
2. In `Assets/_Project/Tools/Editor/EnvironmentMaterialBuilder.cs`:
   - Add `$"{MaterialDirectory}/Mat_Emissive_MushroomHalo.mat"` as `MaterialPaths[11]`.
   - Add `private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");`.
   - Extend `EnsureEmissiveMaterial(..., float dstBlend = 10f)` to call `changed |= SetFloatIfChanged(mat, DstBlendId, dstBlend);`.
   - Call `EnsureEmissiveMaterial(MaterialPaths[11], emissiveShader, new Color(0.18f, 0.85f, 0.78f, 1.0f), pulseSpeed: 1.4f, pulseAmount: 0.15f, dstBlend: 1.0f)`.

- [ ] **Step 4: Run `EnvironmentMaterialBuilder.EnsureAll` and verify Task 2 test turns Green**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.EnvironmentMaterialBuilder.EnsureAll -quit -logFile D:/thietkegame/Logs/r42-t2-mat.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests" -testResults D:/thietkegame/Logs/r42-t2-green.xml -logFile D:/thietkegame/Logs/r42-t2-green.log
```
Expected: `total="5" passed="5" failed="0"`, `shaderErrors=0`, `0` warnings.

---

### Task 3: Remediate `Main_Level.unity` (`[P1-R1]`, `[P1-R2]`, `[P1-R3]`, `[P2-1]`, `[P2-3]`) & Lock Regressions

**Files:**
- Modify: `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs:30-40,238-250,750-912,1104-1211`
- Modify: `Assets/_Project/Scenes/Main_Level.unity` (regenerated by `MainLevelSceneRemediator.RemediateMainLevel`)
- Test: `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs:650-660`

**Interfaces:**
- Consumes:
  - `PrisonPropSpriteGenerator.EnsureAll()`, `.WallLanternAssetPath`, `.ArchSillAssetPath`, `.WallLanternFlameOffsetLocalY` (from Task 1)
  - `Assets/_Project/Art/Materials/Environment/Mat_Emissive_MushroomHalo.mat` (from Task 2)
- Produces:
  - Updated `MainLevelSceneRemediator.RemediateMainLevel()` generating `Main_Level.unity` with:
    - `8/8` `LanternFixture_R4` using `prisonWallLantern_00` @ `scale=(1,1,1)`, `localPosition=(0, 0.125, 0)`, `Background/-9`.
    - `4/4` `GlowingMushroom_0..3` @ `scale=(1,1,1)` (`24 PPU`), `4/4` cap-aligned additive `MushroomHalo_0..3` (`Mat_Emissive_MushroomHalo`, `scale=(0.16, 0.14)`, `alpha=0.24`, `bounds.min.y > floorTop`), and `4/4` sorting-layer-isolated local `MushroomLight_0..3` (`Point Light2D`, `intensity=0.72`, `outerRadius=2.1`, excluding `ParallaxFar/Mid/Foreground`, `0` child fixture/flicker).
    - `32/32` `ArchSill_*` using `prisonArchSill_00` @ `scale=(1,1,1)` on `Background/-17` (strictly behind `Stone pillar` `-16`).
    - `ExitDoor_BackGlow` at `(108, 2.55, 0)`, `scale=(1.40, 1.40, 1)` (`bounds.min.y >= 0.25f`).
    - `BannerBig_92` at `(95.2, 9.10, 0)`, `scale=(0.78, 0.78, 1)` (`bounds.min.y > 5.58f`, strictly clearing `Exit hall pillar` & `CarvedArchBays[4]`).

- [ ] **Step 1: Write the failing regression-lock test in `MainLevelIntegrityTests.cs`**

Append `[Test] public void MainLevel_Round42_VisualP1Closure_ZeroMixelLanternsAdditiveMushroomsOccludedSillsAndCleanExitGlow()` to `MainLevelIntegrityTests`:
- **`[P1-R1]`:** Find all `LanternFixture_R4` (`>= 8`), assert `sr.sprite.name == "prisonWallLantern_00"`, `sr.sprite.pixelsPerUnit == 24f`, `sr.transform.localScale == Vector3.one` (within `1e-4f`), `Mathf.Abs(sr.transform.localPosition.y - 0.125f) <= 1e-3f`, and zero scene `SpriteRenderer`s reference `LightAndBanner_005`.
- **`[P1-R2]`:** For `i = 0..3`:
  - `GlowingMushroom_{i}`: `localScale == Vector3.one` (within `1e-4f`).
  - `MushroomHalo_{i}`: `sharedMaterial.name == "Mat_Emissive_MushroomHalo"`, `sharedMaterial.GetFloat("_DstBlend") == 1f`, `localScale.x <= 0.20f`, `localScale.y <= 0.18f`, `sr.bounds.min.y > floorTop` (where `floorTop` is `-1f` for `i = 0,2,3` and `0f` for `i = 1`).
  - `MushroomLight_{i}`: exists, `Light2D` `Point`, `enabled`, `intensity` in `[0.4f, 1.0f]`, `pointLightOuterRadius` in `[1.5f, 2.8f]`, `m_ApplyToSortingLayers` excludes `ParallaxFar`, `ParallaxMid`, `Foreground`, and `transform.Find("LanternFixture_R4") == null`.
- **`[P1-R3]`:** Find all `ArchSill_*` (`== 32`), assert `sr.sprite.name == "prisonArchSill_00"`, `sr.sprite.pixelsPerUnit == 24f`, `sr.transform.localScale == Vector3.one` (within `1e-4f`), `sr.sortingLayerName == "Background"`, `sr.sortingOrder == -17`, and strictly `< pillarSr.sortingOrder` for every `Stone pillar` (`alcovePillar_00`).
- **`[P2-1]`:** `ExitDoor_BackGlow` `SpriteRenderer.bounds.min.y >= 0.25f`.
- **`[P2-3]`:** `BannerBig_92` `SpriteRenderer.bounds.min.y > exitHallPillarSr.bounds.max.y` (zero overlap with `Exit hall pillar`), while remaining strictly outside `CarvedArchBays[4]`.

- [ ] **Step 2: Run the new test to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests.MainLevel_Round42_VisualP1Closure_ZeroMixelLanternsAdditiveMushroomsOccludedSillsAndCleanExitGlow" -testResults D:/thietkegame/Logs/r42-t3-red.xml -logFile D:/thietkegame/Logs/r42-t3-red.log
```
Expected: FAIL on old `LightAndBanner_005`, `MushroomHalo`, `ArchSill`, `ExitDoor_BackGlow`, and `BannerBig_92`.

- [ ] **Step 3: Update `MainLevelSceneRemediator.cs` and run `RemediateMainLevel()`**

Apply exact surgical edits in `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`:
1. Remove unused `LanternFixtureTilePath` (`L36`) and `LoadTileSpriteRequired` (`L1362-1368`, avoiding any `CS0414` / dead-method warning).
2. In `RemediateMainLevel()` (`L242-244`), call `PrisonPropSpriteGenerator.EnsureAll();` right before `EnvironmentMaterialBuilder.EnsureAll();`.
3. In `BuildPrisonDressing()` (`L750-912`):
   - Load `mushroomHaloMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Emissive_MushroomHalo.mat");` and `archSillSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.ArchSillAssetPath);`.
   - **ArchSill (`L774-787`):** Place `ArchSill_{i}_{sx}` at `new Vector3(sx + 0.5f, yMin + 0.04f, 0f)`, `sortingLayer = bgLayerId`, `sortingOrder = -17`, `localScale = Vector3.one`, `color = Color.white` (remove `sillTint` and `0.28` squash).
   - **BannerBig_92 (`L798-800`):** Change position from `(95.2f, 7.4f, 0f)` → `new Vector3(95.2f, 9.10f, 0f)`, keep `localScale = new Vector3(0.78f, 0.78f, 1f)`.
   - **GlowingMushrooms (`L838-877`):**
     - Place `GlowingMushroom_{i}` at `new Vector3(mx, floorTop - mushroomBrightSprite.bounds.min.y, 0f)` with `localScale = Vector3.one` (`24 PPU` exact `1:1`), `flipX = (i % 2) == 1`.
     - Compute `float budX = mx + (flipX ? 0.484f : -0.484f); float budY = floorTop + 0.410f;`.
     - Place `MushroomHalo_{i}` at `new Vector3(budX, budY, 0f)`, `Ground/-8`, `mushroomHaloMat`, `new Color(1f, 1f, 1f, 0.24f)`, `localScale = new Vector3(0.16f, 0.14f, 1f)`.
     - Create child GameObject `MushroomLight_{i}` under `root` at `new Vector3(budX, budY, 0f)` with `Light2D` (`LightType.Point`, `color = new Color(0.17f, 0.90f, 0.84f, 1f)`, `intensity = 0.72f`, `pointLightInnerRadius = 0.15f`, `pointLightOuterRadius = 2.1f`, `falloffIntensity = 0.55f`, `enabled = true`). *(Sorting layer isolation & `NormalMapQuality.Fast` are automatically applied by `BuildParallaxAndAtmosphereR4` Step 13d.)*
   - **ExitDoor_BackGlow (`L880-890`):** Change position to `new Vector3(108f, 2.55f, 0f)` and `localScale = new Vector3(1.40f, 1.40f, 1f)`.
4. In `BuildParallaxAndAtmosphereR4()` (`L1105`, `L1184-1197`):
   - Load `var lanternFixtureSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.WallLanternAssetPath);`.
   - Set `fixtureGo.transform.localPosition = new Vector3(0f, PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY, 0f);` (`0.125f`) and `fixtureGo.transform.localScale = Vector3.one;`.

- [ ] **Step 4: Run `RemediateMainLevel` and verify Task 3 tests turn Green**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.RemediateMainLevel -quit -logFile D:/thietkegame/Logs/r42-mainlevel-remediate.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests" -testResults D:/thietkegame/Logs/r42-t3-green.xml -logFile D:/thietkegame/Logs/r42-t3-green.log
```
Expected: `total="8" passed="8" failed="0"`, `0` `CS0618`/`CS0414` in both logs.

---

### Task 4: Full Regression Suite (`58/58`), Real GPU Verification (`gpu_r42_01..04.png`), Pixel Proof & Repo Hygiene

**Files:**
- Verify (read-only): `Logs/r42-{mainlevel-remediate,core-green,ng-edit-regression,ng-play-regression,gpu-capture}.log`, `Logs/r42-{core-green,ng-edit-regression,ng-play-regression}.xml`
- Restore churn only: `ProjectSettings/TimeManager.asset`, `ProjectSettings/ProjectSettings.asset`

**Interfaces:**
- Consumes: `MainLevelSceneRemediator.CaptureRound4GpuVerificationFrames()` (reads env vars `R4_GPU_CAPTURE_DIR` and `R4_GPU_CAPTURE_PREFIX`).
- Produces: Fresh `58/58` test XMLs, `5/5` clean logs, `4` real RTX 2050 GPU captures `gpu_r42_01..04.png`, and quantitative pixel proof for all 3 closed P1s + 2 P2s.

- [ ] **Step 1: Run all 3 test suites sequentially (single Unity process lock)**

Run (wait for each process to exit before starting the next):
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames "Core.Tests.EditMode" -testResults D:/thietkegame/Logs/r42-core-green.xml -logFile D:/thietkegame/Logs/r42-core-green.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames "NhaGiamGiengChuong.Tests.EditMode" -testResults D:/thietkegame/Logs/r42-ng-edit-regression.xml -logFile D:/thietkegame/Logs/r42-ng-edit-regression.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 --burst-disable-compilation -projectPath D:/thietkegame -runTests -testPlatform PlayMode -assemblyNames "NhaGiamGiengChuong.Tests.PlayMode" -testResults D:/thietkegame/Logs/r42-ng-play-regression.xml -logFile D:/thietkegame/Logs/r42-ng-play-regression.log
```
Expected: `25/25` (`Core.Tests.EditMode`) + `26/26` (`NG EditMode`) + `7/7` (`NG PlayMode`) = **`58/58` Passed, `0` Failed**.

- [ ] **Step 2: Capture 4 real RTX 2050 GPU verification frames (`gpu_r42_01..04.png`)**

Set `R4_GPU_CAPTURE_DIR` to the session artifact directory (outside `D:/thietkegame`) and `R4_GPU_CAPTURE_PREFIX=gpu_r42`, then run WITHOUT `-nographics`:
```powershell
$env:DOTNET_GCServer='0'; $env:R4_GPU_CAPTURE_PREFIX='gpu_r42'; $env:R4_GPU_CAPTURE_DIR='<ARTIFACT_DIR>'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -job-worker-count 1 --burst-disable-compilation -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.CaptureRound4GpuVerificationFrames -quit -logFile D:/thietkegame/Logs/r42-gpu-capture.log
```
Expected in `Logs/r42-gpu-capture.log`:
- `gfxDevice=NVIDIA GeForce RTX 2050 ... shaderErrors=0`
- `URP_PreFlight urpActive=True hdr=True camPost=True volMask=True profileAll5=True`
- `PaletteScan defaultMaterialRenderers=0`
- `Initialized lights=19 shadowCasters=3` (`15` existing + `4` `MushroomLight_0..3`)
- All 4 `gpu_r42_01..04.png` written (`1280×720`).

- [ ] **Step 3: Visually inspect `4/4` GPU frames + zoomed crops and verify quantitative pixel gates**

Open all 4 `gpu_r42_01..04.png` (plus `2×–3×` zoomed crops of `Mushroom_0/2`, `Bay #0 ArchSill + pillar`, `Spawn lantern + Player`, `Exit Door floor seam`, `BannerBig_92`) and verify via read-only Python script:
- **`[P1-R1]`:** Every visible lantern (`gpu_r42_01..04`) renders crisp `24 PPU` wall-mounted iron sconce matching stone brick pixel scale, zero floating chain, flame dead-center.
- **`[P1-R2]`:** `Mushroom_0` (`gpu_r42_01`) & `Mushroom_2` (`gpu_r42_03`) show crisp `24 PPU` glowing cyan bud + gentle local cyan light bounce on surrounding bricks; **zero** opaque circular sticker ball; **zero** cyan pixels (`G > 80 && B > 80`) below floor top `y = floorTop` under the mushroom; does NOT compete visually with `Power Scroll`.
- **`[P1-R3]`:** `ArchSill_0_*` (`gpu_r42_01`) renders crisp `1:1` square stone trim cleanly tucked BEHIND the right `Stone pillar` capital (`0px` horizontal bar slicing across the pillar).
- **`[P2-1]`:** Under Exit Door (`gpu_r42_04`, `y = 585..615, x = 800..1080`): **`0` yellow bright pixels** (`R > 140 && G > 110`) leaking into floor brick seams (down from `66` in R4.1).
- **`[P2-3]`:** `BannerBig_92` (`gpu_r42_04`) clears `Exit hall pillar` completely while staying on solid `East wing background`.
- **No R4.1 regressions:** `RoomBanner_61` `0px` seam jump, Bay #4 `meanLum >= 12` & `<10 < 35%`, Player ROI `p50 >= 55`, Exit Hall right border `x = 1250..1280` `maxRGB = [0, 0, 0]`, `maxCh >= 240 <= 0.5%`.

- [ ] **Step 4: Restore `ProjectSettings` churn & verify final hygiene (NO `git commit`)**

Run:
```powershell
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
rtk git diff --stat -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset ProjectSettings/TagManager.asset ProjectSettings/EditorBuildSettings.asset
rtk git status --short
```
Verify:
- `0` diff on `ProjectSettings/{TimeManager,ProjectSettings}.asset`; `TagManager.asset` & `EditorBuildSettings.asset` preserved.
- `0` changes to `NhaGiamGiengChuong.unity`, `Rooms/*.prefab`, `.fortress-workspaces/antigravity`, runtime `.cs` / `.asmdef`, or gameplay `BoxCollider2D`s.
- `0` `CS0618`, `0` `CS0414`, `0` `warning CS`, `0` `error CS`, `0` `Assertion failed`, `0` `More than one global light` across all `5` `Logs/r42-*.log` files.
- `0` remaining `Unity.exe` processes.

---

## Handoff Note for Future `NhaGiamGiengChuong.unity` Replication (Out of R4.2 Scope)

Once `Main_Level.unity` passes independent R4.2 review (`GO >= 4.5/5`) and human Play Mode inspection, remember `[P2-2]` before replicating `RoomBanner_*` to `NhaGiamGiengChuong.unity`: `Mat_RoomBanner_Sway.mat` currently hardcodes `_PinTopWorldY = 7.6`. Any `RoomBanner` placed at a different `topY` in `NhaGiamGiengChuong` will need per-height sway materials or a lightweight `MaterialPropertyBlock` pin setter.
