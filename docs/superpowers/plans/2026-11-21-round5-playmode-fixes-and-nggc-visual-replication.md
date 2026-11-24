# Round 5 Play Mode Bug Closure (`Main_Level.unity`) & Full Visual Pipeline Replication (`NhaGiamGiengChuong.unity`) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close all `6` live Play Mode defects discovered during human playthrough of `Main_Level.unity` (one-way platform side head-bump blocking walk/roll, collected `PowerScrollPickup` leaving `ScrollAura_R4` glowing on empty jars, `LanternFlameCore_R4`/`TorchEmbers` rendering through the Player's body on `VFX` instead of `Background`, `Foreground_Silhouettes` floating mid-screen when jumping high, hard "paper-cut" vertical/top brick edges on `CarvedArchBays`, and dark midtones/floor pockets) and replicate the complete, Play-Mode-hardened `R4.2 + R5` `24 PPU` visual pipeline to `NhaGiamGiengChuong.unity` and its 5 room prefabs (`AB`, `CD`, `ELF`, `G`, `HJK`) with `62/62` passing tests and real RTX 2050 GPU verification.

**Architecture:** Preserve 100% of gameplay state machines, `.asmdef` boundaries, and v7 collider bounds/positions (`A01`–`A10`, `U01`–`U16`, `P01`–`P07`). Extend `PrisonPropSpriteGenerator` with a deterministic `24 PPU` vertical chiseled stone jamb (`prisonArchJamb_00.png` + `_NormalMap`) to frame all cut vertical edges of carved arch bays; add a lightweight `VertexSwayPin2D` `MaterialPropertyBlock` component so multi-piece `RoomBanner_*` props at any room elevation (`y = 0..38.5`) pin their top mount with zero seam jump (`[P2-2]`); extend `ParallaxLayer2D` with optional vertical camera-offset locking so `Foreground_Silhouettes` tracks the top viewport edge during high jumps without floating mid-screen; fix `PlatformEffector2D.surfaceArc = 160f` across both scenes; re-parent `ScrollAura_R4` under `PowerScrollPickup`'s `_icon` and move lantern flames/embers to `Background/-8..-7` behind `Actors`; brighten `DeadCells_PostProcessProfile` + corridor fill lights; and upgrade `NhaGiamGiengChuongBuilder` to the full 4-tier parallax, normal-mapped `24 PPU` prop, shader, and lighting stack.

**Tech Stack:** Unity `6000.6.9f1`, C# 9, URP 2D `17.6.0` (`Light2D`, `ShadowCaster2D`, `VolumeProfile`), Dead Cells `Prison` `24 PPU` pipeline, Unity Test Framework `1.8.0`, `rtk` CLI wrapper, Python/PIL GPU pixel verifier.

**Spec:**
- Live Play Mode Human Review & 6 Bug Diagnoses (this session, 21/11/2026)
- Independent Round 4.2 Strict Deep Review (`4.60/5`, `GO`, `[P2-2]`/`[P2-5]` handoff notes): `C:/Users/habayakka/.gemini/antigravity/brain/e34efb94-1932-4529-8812-93c6056b18d4/deep_review_round42_strict.md`
- Round 4.2 Implementation Plan: `D:/thietkegame/docs/superpowers/plans/2026-11-20-round42-mainlevel-visual-p1-closure.md`
- Project rules & assembly boundaries: `D:/thietkegame/AGENTS.md` (§§0–2, §6, §9, §10)
- Rubric & URP PostFX references: `C:/Users/habayakka/.gemini/config/plugins/unity/skills/using-unity/references/craft-game-floors/references/visual-scorecard.md` (2D Anchor Set), `C:/Users/habayakka/.gemini/config/plugins/unity/skills/using-unity/references/urp-postprocessing/urp-postprocessing.md`

## Global Constraints

- **Shell & Encoding:** Run every shell command through `rtk` (or `rtk proxy <command>`). Read/write all UTF-8 text files without BOM.
- **No Manual `.meta` Edits (`AGENTS.md` §2):** Never hand-create, hand-edit, or delete `.meta` files; let Unity generate/update `.meta` via `AssetDatabase.Refresh` and `TextureImporter.SaveAndReimport()`.
- **Unity `6000.6.9f1` Zero-Warning Policy:** Use `UnityEngine.Object.FindObjectsByType<T>()` / `FindObjectsByType<T>(FindObjectsInactive.Include)` and `FindAnyObjectByType<T>()`. Zero `CS0618`, zero `CS0414`, zero `warning CS`, zero `error CS`, zero `ShaderUtil.ShaderHasError`, `URP_PreFlight 5/5 True`, `defaultMaterialRenderers=0`.
- **Single-Unity-Process Lock (`7.5 GB` RAM Host) & `ProjectSettings` Hygiene:** Verify `Get-Process Unity -ErrorAction SilentlyContinue` is empty before every Unity call. Pass `$env:DOTNET_GCServer='0'` and `-job-worker-count 1` on every Unity invocation; pass `--burst-disable-compilation` for PlayMode and GPU capture runs; use `-batchmode -nographics` for build/tests and omit `-nographics` only for GPU capture. After the final Unity invocation, run `rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset` while preserving `ProjectSettings/TagManager.asset` and `ProjectSettings/EditorBuildSettings.asset`.
- **Preserved Invariants:**
  - Do **NOT** modify `.fortress-workspaces/antigravity` or any gameplay state machine under `Assets/_Project/Features/Player/` or `Assets/_Project/Features/Enemies/`.
  - Keep `Global Light 2D` intensity within **`[0.48f, 0.62f]`** in both `Main_Level.unity` (`MainLevelIntegrityTests.cs:263-264`) and `NhaGiamGiengChuong.unity` (`MapWiringTests.cs:317-320`).
  - Keep all `58/58` existing tests green (`25` Core EditMode + `26` NG EditMode + `7` NG PlayMode) plus `4` new Round 5 tests (`62/62` total).
  - In `NhaGiamGiengChuong.unity`, preserve every `BoxCollider2D`/`PolygonCollider2D` center, size, offset, and layer (`A01`–`A06`), all 9 grounded landmark names and `bounds.min.y` (`A08`, including `Art_F_ExitGlow` `bounds.min.y == 32f ± 0.12f`), `I1`–`I6` triggers, and `11/11` room point-light coverage (`>= 50%` per room, `>= 75%` total in `A10`).

## Review Focus

1. **One-Way Platform horizontal collision (`PlatformEffector2D.surfaceArc = 160f`):** Unity's default `surfaceArc = 180f` includes `±90°` left/right box side normals, turning `0.10u–0.25u` vertical overlaps between adjacent platforms and the Player's `1.60u` standing/rolling collider into solid walls. Every `PlatformEffector2D` in `Main_Level.unity` (`9` platforms) and `NhaGiamGiengChuong.unity` (`10` platforms) must use `surfaceArc = 160f` and `rotationalOffset = 0f`. *(Pinned in Task 2 & Task 3)*
2. **`ScrollAura_R4` lifecycle coupled to `PowerScrollPickup.Consume()` / `Reveal(true)`:** Parenting `ScrollAura_R4_{i}` under `Prison_Dressing_R3` leaves the HDR glow active after the scroll is picked up. `ScrollAura_R4_{i}` must be parented directly under the pickup's `_icon` Transform (`Floating icon`) so `Reveal(true)` disables both the scroll icon and its aura atomically. *(Pinned in Task 2)*
3. **Wall lantern sorting behind `Actors` (`Background/-9..-7`):** Placing `LanternFlameCore` or `TorchEmbers` on `VFX` (`> Actors`) makes flames render on top of the Player's body when walking past a wall lantern. All wall lantern sub-renderers (`LanternFixture`, `LanternFlameCore`, `TorchEmbers`) must sort on `Background` at orders `-9`, `-8`, `-7` respectively (strictly `< Ground` and `< Actors`). *(Pinned in Task 2 & Task 3)*
4. **`Foreground_Silhouettes` vertical camera tracking & EditMode bounds compatibility:** In EditMode (`cam.y = 3.8f` on `Main_Level`), `Foreground_Silhouettes` renderers must keep `bounds.min.y` in `[8.5f, 10.2f]`, while in Play Mode (`ParallaxLayer2D` with `_lockYToCameraStartOffset = true`) they must shift vertically `1:1` with `cameraPosition.y - _referenceCameraY` so jumping to upper platforms never reveals cut-off top edges floating mid-screen. *(Pinned in Task 1, Task 2 & Task 3)*
5. **Multi-elevation 3-piece `RoomBanner_*` world-Y pinning (`[P2-2]`):** In `NhaGiamGiengChuong.unity`, 3-piece banners hang at `topY = 16.5f` (Room C), `21.8f` (Room J), `30.2f` (Room E), and `37.2f` (Room F). Every multi-piece `RoomBanner` must attach `VertexSwayPin2D` setting `_PinTopWorldY = topY` via `MaterialPropertyBlock` so `_Top/_Mid/_Bot` sway with `0px` seam tear at every room altitude. *(Pinned in Task 1 & Task 3)*

---

## File Structure Map

| Responsibility | Path | Action | Task |
|---|---|---|---|
| Deterministic `24 PPU` vertical stone arch jamb (`prisonArchJamb_00.png` + `_NormalMap`) | `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs`, `Assets/_Project/Art/Environment/Prison/Sprites/prisonArchJamb_00.png`, `Assets/_Project/Art/Environment/Prison/Normals/prisonArchJamb_00_n.png` | Modify / Generated | Task 1 |
| Per-banner `_PinTopWorldY` `MaterialPropertyBlock` binder for multi-elevation 3-piece banners | `Assets/_Project/Features/VFX/Scripts/VertexSwayPin2D.cs` | Create | Task 1 |
| Vertical camera-offset lock on `ParallaxLayer2D` for ceiling foreground silhouettes | `Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs` | Modify | Task 1 |
| Unit tests for `prisonArchJamb_00`, `VertexSwayPin2D`, and `ParallaxLayer2D` vertical lock | `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`, `Assets/_Project/Core/Tests/EditMode/ParallaxAndAtmosphereTests.cs` | Modify | Task 1 |
| `Main_Level.unity` Play Mode closure (`6` Play Mode fixes + `[P2-5]` Exit arch lighting) | `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`, `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset`, `Assets/_Project/Scenes/Main_Level.unity` | Modify | Task 2 |
| `Main_Level.unity` Round 5 EditMode regression test | `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs` | Modify | Task 2 |
| `NhaGiamGiengChuong` Round 5 visual pipeline replication (`5` room prefabs + scene + GPU capture helper) | `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef`, `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`, `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{AB,CD,ELF,G,HJK}.prefab`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` | Modify | Task 3 |
| `NhaGiamGiengChuong` Round 5 EditMode regression test (`A11`) | `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` | Modify | Task 3 |
| Full `62/62` test suite verification + RTX 2050 GPU captures across both scenes | `Logs/r5-*.{xml,log}`, GPU captures `gpu_r5_*.png` (outside repo) | Execute | Task 4 |

---

### Task 1: `24 PPU` Arch Jamb Generator (`prisonArchJamb_00`), Per-Banner `VertexSwayPin2D`, and `ParallaxLayer2D` Vertical Ceiling Lock

**Files:**
- Modify: `Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs:10-40,153-258`
- Create: `Assets/_Project/Features/VFX/Scripts/VertexSwayPin2D.cs`
- Modify: `Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs:10-95`
- Test: `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs:300-343`
- Test: `Assets/_Project/Core/Tests/EditMode/ParallaxAndAtmosphereTests.cs:40-120`

**Interfaces:**
- Consumes: `DeadCells.Tools.Editor.PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(string spritePath)`.
- Produces:
  - `DeadCells.Tools.Editor.PrisonPropSpriteGenerator.ArchJambAssetPath` (`"Assets/_Project/Art/Environment/Prison/Sprites/prisonArchJamb_00.png"`, `8×24 px @ 24 PPU`, `FilterMode.Point`, `Uncompressed`, `wrapModeU = Clamp`, `wrapModeV = Repeat`, bound `_NormalMap` at `"Assets/_Project/Art/Environment/Prison/Normals/prisonArchJamb_00_n.png"`).
  - `DeadCells.VFX.VertexSwayPin2D`:
    - `public void Configure(float pinTopWorldY, float swaySpanY = 3.0f, bool useWorldYPin = true)`
    - `public void ApplyPropertyBlock()` — writes `_UseWorldYPin`, `_PinTopWorldY`, and `_SwaySpanY` into a `MaterialPropertyBlock` on every `SpriteRenderer` on `gameObject` and its children. Called automatically in `Awake()`, `OnEnable()`, and `OnValidate()`.
  - `DeadCells.CameraSystem.ParallaxLayer2D`:
    - `public void ConfigureVerticalCameraLock(bool lockToCameraTop, float referenceCameraY)` — when `lockToCameraTop == true`, `UpdateParallax(Vector3 cameraPosition)` computes vertical displacement as `cameraPosition.y - _referenceCameraY` (so at `cameraPosition.y == _referenceCameraY` displacement is `0f`, and as the camera rises by `+Δy`, the layer rises by `+Δy` `1:1` with the top of the viewport).

- [ ] **Step 1: Write failing tests in `VisualShaderAndNormalTests.cs` and `ParallaxAndAtmosphereTests.cs`**

1. In `Assets/_Project/Core/Tests/EditMode/VisualShaderAndNormalTests.cs`, add `(PrisonPropSpriteGenerator.ArchJambAssetPath, 8, 24, "Assets/_Project/Art/Environment/Prison/Normals/prisonArchJamb_00_n.png")` to `expectedProps` in `PrisonGeneratedProps_Are24PpuPointUncompressedWithBoundNormalMaps()`.
2. In `Assets/_Project/Core/Tests/EditMode/ParallaxAndAtmosphereTests.cs`, add two tests:

```csharp
[Test]
public void ParallaxLayer2D_VerticalCameraLock_PreservesEditModeOriginAtRefYAndTracksCameraAscentOneToOne()
{
    var go = new GameObject("FgTest");
    var camGo = new GameObject("CamTest");
    try
    {
        var layer = go.AddComponent<ParallaxLayer2D>();
        layer.Configure(camGo.transform, new Vector2(1.18f, 1.0f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
        layer.ConfigureVerticalCameraLock(true, referenceCameraY: 3.8f);

        layer.UpdateParallax(new Vector3(18.5f, 3.8f, -10f));
        Assert.AreEqual(0f, go.transform.position.y, 1f / 24f, "At referenceCameraY (3.8), vertical offset must be 0.");

        layer.UpdateParallax(new Vector3(18.5f, 8.8f, -10f));
        Assert.AreEqual(5.0f, go.transform.position.y, 1f / 24f, "When camera climbs +5u, locked ceiling foreground must climb +5u.");
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(camGo);
    }
}

[Test]
public void VertexSwayPin2D_AppliesPerBannerTopWorldYViaPropertyBlock()
{
    var root = new GameObject("RoomBanner_Test");
    var child = new GameObject("Top");
    child.transform.SetParent(root.transform, false);
    var sr = child.AddComponent<SpriteRenderer>();
    try
    {
        var pin = root.AddComponent<VertexSwayPin2D>();
        pin.Configure(pinTopWorldY: 21.8f, swaySpanY: 3.0f, useWorldYPin: true);

        var block = new MaterialPropertyBlock();
        sr.GetPropertyBlock(block);
        Assert.AreEqual(1.0f, block.GetFloat("_UseWorldYPin"), 1e-4f);
        Assert.AreEqual(21.8f, block.GetFloat("_PinTopWorldY"), 1e-4f);
        Assert.AreEqual(3.0f, block.GetFloat("_SwaySpanY"), 1e-4f);
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(root);
    }
}
```

- [ ] **Step 2: Run the unit tests to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.ParallaxAndAtmosphereTests" -testResults D:/thietkegame/Logs/r5-t1-red.xml -logFile D:/thietkegame/Logs/r5-t1-red.log
```
Expected: Compile error / Red failure because `PrisonPropSpriteGenerator.ArchJambAssetPath`, `VertexSwayPin2D`, and `ConfigureVerticalCameraLock` do not exist yet.

- [ ] **Step 3: Implement `prisonArchJamb_00` in `PrisonPropSpriteGenerator.cs`, `VertexSwayPin2D.cs`, and `ConfigureVerticalCameraLock` in `ParallaxLayer2D.cs`**

1. **`Assets/_Project/Tools/Editor/PrisonPropSpriteGenerator.cs`:**
   - Add `ArchJambAssetPath` constant right after `ArchSillAssetPath` (`L11`) and update `EnsureAll()` (`L18-39`):

```csharp
public const string WallLanternAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonWallLantern_00.png";
public const string ArchSillAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchSill_00.png";
public const string ArchJambAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchJamb_00.png";
public const float WallLanternFlameOffsetLocalY = 0.125f;
```

```csharp
[MenuItem("Tools/DeadCells/Visual/Generate 24-PPU Prison Props")]
public static void EnsureAll()
{
    Directory.CreateDirectory(SpriteDirectory);

    byte[] lanternBytes = BuildWallLanternPng();
    byte[] archSillBytes = BuildArchSillPng();
    byte[] archJambBytes = BuildArchJambPng();

    bool lanternWritten = WriteBytesIfChanged(WallLanternAssetPath, lanternBytes);
    bool sillWritten = WriteBytesIfChanged(ArchSillAssetPath, archSillBytes);
    bool jambWritten = WriteBytesIfChanged(ArchJambAssetPath, archJambBytes);

    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

    bool lanternImported = ConfigureSpriteImporter(WallLanternAssetPath, TextureWrapMode.Clamp, TextureWrapMode.Clamp);
    bool sillImported = ConfigureSpriteImporter(ArchSillAssetPath, TextureWrapMode.Repeat, TextureWrapMode.Clamp);
    bool jambImported = ConfigureSpriteImporter(ArchJambAssetPath, TextureWrapMode.Clamp, TextureWrapMode.Repeat);

    PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(WallLanternAssetPath);
    PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(ArchSillAssetPath);
    PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(ArchJambAssetPath);

    Debug.Log(
        $"[PrisonPropSpriteGenerator] Verified 3 generated 24-PPU Prison props " +
        $"(lanternWritten={lanternWritten}, lanternImported={lanternImported}, " +
        $"sillWritten={sillWritten}, sillImported={sillImported}, " +
        $"jambWritten={jambWritten}, jambImported={jambImported}).");
}
```

   - Add `BuildArchJambPng()` right after `BuildArchSillPng()` (`L215`) and update `ConfigureSpriteImporter` (`L216-258`) to accept `TextureWrapMode wrapModeV = TextureWrapMode.Clamp`:

```csharp
private static byte[] BuildArchJambPng()
{
    const int width = 8;
    const int height = 24;
    Color32[] pixels = new Color32[width * height];

    Color32 innerOutline = Hex("#18201E");
    Color32 bevelLit = Hex("#667567");
    Color32 bevelHighlight = Hex("#738374");
    Color32 bevelWear = Hex("#4A584D");
    Color32 stoneBodyA = Hex("#3C4940");
    Color32 stoneBodyB = Hex("#47564B");
    Color32 stoneBodyHi = Hex("#526256");
    Color32 mortarGroove = Hex("#232C28");
    Color32 underLip = Hex("#242E2A");
    Color32 outerEdge = Hex("#151C1A");

    for (int y = 0; y < height; y++)
    {
        bool isMortar = (y == 7 || y == 15 || y == 23);
        int blockIdx = y / 8;

        // x = 7: inner crisp dark outline facing the carved bay opening
        SetPixel(pixels, width, height, 7, y, innerOutline);

        // x = 6: lit stone bevel with 1px wear notches at horizontal mortar seams
        if (isMortar)
            SetPixel(pixels, width, height, 6, y, bevelWear);
        else if (y == 1 || y == 2 || y == 9 || y == 10 || y == 17 || y == 18)
            SetPixel(pixels, width, height, 6, y, bevelHighlight);
        else
            SetPixel(pixels, width, height, 6, y, bevelLit);

        // x = 2..5: chiseled stone body with 1px dark horizontal mortar grooves at y = 7, 15, 23
        for (int x = 2; x <= 5; x++)
        {
            if (isMortar)
            {
                SetPixel(pixels, width, height, x, y, mortarGroove);
                continue;
            }

            Color32 bodyColor;
            if (x == 5 && (y % 8) <= 3)
                bodyColor = stoneBodyHi;
            else if (x == 2)
                bodyColor = stoneBodyA;
            else
                bodyColor = (((x + y + blockIdx) & 1) == 0) ? stoneBodyB : stoneBodyA;

            SetPixel(pixels, width, height, x, y, bodyColor);
        }

        // x = 1: outer under-lip shadow
        SetPixel(pixels, width, height, 1, y, isMortar ? outerEdge : underLip);

        // x = 0: outer dark edge flush with cut brick column
        SetPixel(pixels, width, height, 0, y, outerEdge);
    }

    return EncodePixelsToPng(width, height, pixels);
}

private static bool ConfigureSpriteImporter(string assetPath, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV = TextureWrapMode.Clamp)
{
    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
    if (importer == null)
        throw new InvalidOperationException($"Missing TextureImporter for '{assetPath}'.");

    TextureImporterSettings settings = new TextureImporterSettings();
    importer.ReadTextureSettings(settings);

    bool dirty =
        importer.textureType != TextureImporterType.Sprite ||
        importer.spriteImportMode != SpriteImportMode.Single ||
        Mathf.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) > 1e-4f ||
        importer.filterMode != FilterMode.Point ||
        importer.textureCompression != TextureImporterCompression.Uncompressed ||
        importer.mipmapEnabled ||
        importer.npotScale != TextureImporterNPOTScale.None ||
        importer.wrapModeU != wrapModeU ||
        importer.wrapModeV != wrapModeV ||
        settings.spriteAlignment != (int)SpriteAlignment.Center ||
        Vector2.SqrMagnitude(settings.spritePivot - new Vector2(0.5f, 0.5f)) > 1e-6f;

    if (!dirty)
        return false;

    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Single;
    importer.spritePixelsPerUnit = PixelsPerUnit;
    importer.filterMode = FilterMode.Point;
    importer.textureCompression = TextureImporterCompression.Uncompressed;
    importer.mipmapEnabled = false;
    importer.npotScale = TextureImporterNPOTScale.None;
    importer.wrapModeU = wrapModeU;
    importer.wrapModeV = wrapModeV;

    importer.ReadTextureSettings(settings);
    settings.spriteAlignment = (int)SpriteAlignment.Center;
    settings.spritePivot = new Vector2(0.5f, 0.5f);
    importer.SetTextureSettings(settings);

    importer.SaveAndReimport();
    return true;
}
```

2. **`Assets/_Project/Features/VFX/Scripts/VertexSwayPin2D.cs` (Create complete file):**

```csharp
using UnityEngine;

namespace DeadCells.VFX
{
    /// <summary>
    /// Binds per-instance _UseWorldYPin, _PinTopWorldY, and _SwaySpanY via MaterialPropertyBlock
    /// so multi-piece RoomBanner_* props at any dungeon elevation sway with zero seam tear.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class VertexSwayPin2D : MonoBehaviour
    {
        private static readonly int UseWorldYPinId = Shader.PropertyToID("_UseWorldYPin");
        private static readonly int PinTopWorldYId = Shader.PropertyToID("_PinTopWorldY");
        private static readonly int SwaySpanYId = Shader.PropertyToID("_SwaySpanY");

        [SerializeField] private bool _useWorldYPin = true;
        [SerializeField] private float _pinTopWorldY = 7.6f;
        [SerializeField] private float _swaySpanY = 4.0f;

        private MaterialPropertyBlock _propertyBlock;

        public bool UseWorldYPin => _useWorldYPin;
        public float PinTopWorldY => _pinTopWorldY;
        public float SwaySpanY => _swaySpanY;

        private void Awake() => ApplyPropertyBlock();

        private void OnEnable() => ApplyPropertyBlock();

        private void OnValidate() => ApplyPropertyBlock();

        public void Configure(float pinTopWorldY, float swaySpanY = 3.0f, bool useWorldYPin = true)
        {
            _pinTopWorldY = pinTopWorldY;
            _swaySpanY = Mathf.Max(0.01f, swaySpanY);
            _useWorldYPin = useWorldYPin;
            ApplyPropertyBlock();
        }

        public void ApplyPropertyBlock()
        {
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null)
                    continue;

                sr.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(UseWorldYPinId, _useWorldYPin ? 1.0f : 0.0f);
                _propertyBlock.SetFloat(PinTopWorldYId, _pinTopWorldY);
                _propertyBlock.SetFloat(SwaySpanYId, Mathf.Max(0.01f, _swaySpanY));
                sr.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
```

3. **`Assets/_Project/Features/CameraSystem/Scripts/ParallaxLayer2D.cs`:**
   - Add serialized fields `_lockYToCameraStartOffset` and `_referenceCameraY` after `_pixelsPerUnit` (`L18`), add `ConfigureVerticalCameraLock`, and update `UpdateParallax` (`L66-98`):

```csharp
[SerializeField] private bool _pixelSnap = true;
[SerializeField] private float _pixelsPerUnit = 24f;
[SerializeField] private bool _lockYToCameraStartOffset;
[SerializeField] private float _referenceCameraY;
[SerializeField, HideInInspector] private Vector3 _originPosition;
[SerializeField, HideInInspector] private bool _hasOrigin;
```

```csharp
public void ConfigureVerticalCameraLock(bool lockToCameraTop, float referenceCameraY)
{
    _lockYToCameraStartOffset = lockToCameraTop;
    _referenceCameraY = referenceCameraY;
}

public void UpdateParallax(Vector3 cameraPosition)
{
    if (!_hasOrigin)
    {
        _originPosition = transform.position;
        _hasOrigin = true;
    }

    float offsetX = (cameraPosition.x - _originPosition.x) * (1f - _parallaxFactor.x);
    float offsetY = _lockYToCameraStartOffset
        ? (cameraPosition.y - _referenceCameraY)
        : (cameraPosition.y - _originPosition.y) * (1f - _parallaxFactor.y);
```

- [ ] **Step 4: Run generator and verify Task 1 tests turn Green**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.PrisonPropSpriteGenerator.EnsureAll -quit -logFile D:/thietkegame/Logs/r5-t1-gen.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.VisualShaderAndNormalTests|DeadCells.Core.Tests.ParallaxAndAtmosphereTests" -testResults D:/thietkegame/Logs/r5-t1-green.xml -logFile D:/thietkegame/Logs/r5-t1-green.log
```
Expected: `total="11" passed="11" failed="0"` (`5` `VisualShaderAndNormalTests` + `6` `ParallaxAndAtmosphereTests`), `0` warnings.

---

### Task 2: Close All 6 Live Play Mode Bugs on `Main_Level.unity`

**Files:**
- Modify: `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs:90-150,280-315,600-625,760-805,905-930,1050-1225`
- Modify: `Assets/_Project/Settings/DeadCells_PostProcessProfile.asset` (regenerated by `MainLevelSceneRemediator.RemediateMainLevel`)
- Modify: `Assets/_Project/Scenes/Main_Level.unity` (regenerated by `MainLevelSceneRemediator.RemediateMainLevel`)
- Test: `Assets/_Project/Core/Tests/EditMode/MainLevelIntegrityTests.cs:780-784`

**Interfaces:**
- Consumes:
  - `PrisonPropSpriteGenerator.ArchJambAssetPath`, `PrisonPropSpriteGenerator.ArchSillAssetPath` (from Task 1)
  - `ParallaxLayer2D.ConfigureVerticalCameraLock(bool, float)` (from Task 1)
  - `VertexSwayPin2D` (from Task 1)
- Produces:
  - `Main_Level.unity` with:
    1. **`[PM-1]`:** All `9/9` `PlatformEffector2D` components (`One Way Platform 16/21/28` + 6 `Archive gallery` platforms) configured with `useOneWay = true`, `surfaceArc = 160f`, `rotationalOffset = 0f`.
    2. **`[PM-2]`:** All `ScrollAura_R4_{i}` GameObjects parented directly under their `PowerScrollPickup`'s `_icon` Transform (`Floating icon`) at `localPosition = Vector3.zero` (and assigned to `_glow` when `_glow == null`), so `PowerScrollPickup.Reveal(true)` disables the glow when collected.
    3. **`[PM-3]`:** All `8/8` `LanternFlameCore_R4` renderers on `Background/-8` and all `8/8` `TorchEmbers` `ParticleSystemRenderer`s on `Background/-7` (strictly behind `Ground` and `Actors`, in front of `LanternFixture_R4` `Background/-9`).
    4. **`[PM-4]`:** `Foreground_Silhouettes` configured with `ConfigureVerticalCameraLock(true, 3.8f)` and `wrapWidthUnits = 32f`, with all 16 ceiling silhouette sprites (8 beams + 8 chains, `8u` spacing dividing the `32u` wrap) keeping `bounds.min.y` in `[9.2f, 10.0f] ⊂ [8.5f, 10.2f]` at `cam.y = 3.8f`.
    5. **`[PM-5]`:** All `5` `CarvedArchBays` framed on their vertical left (`x = xMin`), vertical right (`x = xMax`), and top horizontal (`y = yMax`) cut edges with `ArchJamb_L_{i}_{y}`, `ArchJamb_R_{i}_{y}` (`prisonArchJamb_00.png` @ `24 PPU`, `scale=(1,1,1)`, `Background/-17`) and `ArchLintel_{i}_{sx}` (`prisonArchSill_00.png`, `flipY = true`, `Background/-17`).
    6. **`[PM-6]` & `[P2-5]`:** `Global Light 2D` intensity `= 0.62f` (color `(0.84f, 0.91f, 0.96f, 1f)`), `DeadCells_PostProcessProfile` `ColorAdjustments.postExposure = 0.62f`, `contrast = 6f`, `Vignette.intensity = 0.14f`, primary wall lanterns widened to `outerRadius = 9.0f, innerRadius = 1.8f, intensity = 2.0f`, `Exit Door/Accent light` boosted to `intensity = 1.65f, outerRadius = 7.5f`, plus `3` upper-route fill `Point Light2D`s (`GalleryFillLight_0..2` under `Prison_Dressing_R3` at `(56.5, 5.8)`, `(70.5, 6.8)`, `(87.5, 6.2)` with `intensity = 1.15f, outerRadius = 7.5f`, isolated to gameplay sorting layers, `0` lantern fixture children).

- [ ] **Step 1: Write the failing test in `MainLevelIntegrityTests.cs`**

Append `[Test] public void MainLevel_Round5_PlayModeFixes_OneWaySurfaceArcScrollAuraDisableLanternSortingArchJambsAndBrightness()` to `MainLevelIntegrityTests`:

```csharp
[Test]
public void MainLevel_Round5_PlayModeFixes_OneWaySurfaceArcScrollAuraDisableLanternSortingArchJambsAndBrightness()
{
    // [PM-1] All 9 PlatformEffector2D components use surfaceArc == 160f so side contacts never block walk/roll
    var effectors = UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsInactive.Include);
    Assert.AreEqual(9, effectors.Length, "Expected 9 PlatformEffector2D components in Main_Level.");
    foreach (var eff in effectors)
    {
        Assert.IsTrue(eff.useOneWay, $"PlatformEffector2D on '{eff.name}' must enable useOneWay.");
        Assert.AreEqual(160f, eff.surfaceArc, 1e-3f,
            $"PlatformEffector2D on '{eff.name}' must use surfaceArc=160f (not 180f) so side edges do not block Player walk/roll.");
    }

    // [PM-2] Every ScrollAura_R4_{i} is parented under PowerScrollPickup._icon so Reveal(true) disables it
    var scrolls = UnityEngine.Object.FindObjectsByType<PowerScrollPickup>(FindObjectsInactive.Include);
    Assert.GreaterOrEqual(scrolls.Length, 3);
    foreach (var scroll in scrolls)
    {
        var soScroll = new SerializedObject(scroll);
        var iconTr = soScroll.FindProperty("_icon").objectReferenceValue as Transform;
        Assert.IsNotNull(iconTr, $"PowerScrollPickup '{scroll.name}' missing _icon.");
        var aura = iconTr.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(sr => sr.gameObject.name.StartsWith("ScrollAura_R4_", StringComparison.Ordinal));
        Assert.IsNotNull(aura, $"PowerScrollPickup '{scroll.name}' must parent ScrollAura_R4_* under _icon so collecting hides the glow.");
    }

    // [PM-3] LanternFlameCore_R4 (Background/-8) and TorchEmbers (Background/-7) sort strictly on Background behind Actors
    var allRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
    var flameCores = allRenderers.Where(sr => sr.gameObject.name == "LanternFlameCore_R4").ToArray();
    Assert.AreEqual(8, flameCores.Length);
    foreach (var core in flameCores)
    {
        Assert.AreEqual("Background", core.sortingLayerName, "LanternFlameCore_R4 must sort on Background (behind Actors), not VFX.");
        Assert.AreEqual(-8, core.sortingOrder, "LanternFlameCore_R4 must sort at Background/-8 in front of LanternFixture_R4 (-9).");
        var embersPsr = core.transform.parent.Find("TorchEmbers")?.GetComponent<ParticleSystemRenderer>();
        Assert.IsNotNull(embersPsr);
        Assert.AreEqual("Background", embersPsr.sortingLayerName, "TorchEmbers must sort on Background (behind Actors), not VFX.");
        Assert.AreEqual(-7, embersPsr.sortingOrder);
    }

    // [PM-4] Foreground_Silhouettes locks vertical offset to camera top (referenceCameraY = 3.8) with wrapWidthUnits = 32
    var fgLayer = GameObject.Find("Prison_Parallax_R4/Foreground_Silhouettes")?.GetComponent<ParallaxLayer2D>();
    Assert.IsNotNull(fgLayer);
    var soFg = new SerializedObject(fgLayer);
    Assert.IsTrue(soFg.FindProperty("_lockYToCameraStartOffset").boolValue,
        "Foreground_Silhouettes must lock Y to camera start offset so jumping never reveals floating top edges.");
    Assert.AreEqual(32f, soFg.FindProperty("_wrapWidthUnits").floatValue, 1e-3f);

    // [PM-5] All 5 CarvedArchBays have vertical 24-PPU ArchJamb_L/R_* and top ArchLintel_* on Background/-17
    var archJambs = allRenderers.Where(sr => sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal)).ToArray();
    Assert.GreaterOrEqual(archJambs.Length, 70, $"Expected >= 70 ArchJamb_* renderers framing vertical bay cuts, found {archJambs.Length}.");
    foreach (var jamb in archJambs)
    {
        Assert.AreEqual("prisonArchJamb_00", jamb.sprite.name);
        Assert.AreEqual(24f, jamb.sprite.pixelsPerUnit, 1e-4f);
        Assert.AreEqual("Background", jamb.sortingLayerName);
        Assert.AreEqual(-17, jamb.sortingOrder);
    }

    // [PM-6] Brightened postExposure >= 0.55f, Vignette <= 0.16f, Global Light == 0.62f, and >= 3 GalleryFillLight_*
    var globalLight = UnityEngine.Object.FindObjectsByType<Light2D>().Single(l => l.lightType == Light2D.LightType.Global);
    Assert.AreEqual(0.62f, globalLight.intensity, 1e-3f);
    var vol = UnityEngine.Object.FindObjectsByType<Volume>().First(v => v.enabled && v.isGlobal);
    Assert.IsTrue(vol.sharedProfile.TryGet<ColorAdjustments>(out var ca) && ca.postExposure.value >= 0.55f);
    Assert.IsTrue(vol.sharedProfile.TryGet<Vignette>(out var vig) && vig.intensity.value <= 0.16f);
    int fillLights = UnityEngine.Object.FindObjectsByType<Light2D>()
        .Count(l => l.gameObject.name.StartsWith("GalleryFillLight_", StringComparison.Ordinal));
    Assert.AreEqual(3, fillLights);
}
```

- [ ] **Step 2: Run the new test to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests.MainLevel_Round5_PlayModeFixes_OneWaySurfaceArcScrollAuraDisableLanternSortingArchJambsAndBrightness" -testResults D:/thietkegame/Logs/r5-t2-red.xml -logFile D:/thietkegame/Logs/r5-t2-red.log
```
Expected: FAIL on `surfaceArc == 180f` (`!= 160f`).

- [ ] **Step 3: Update `MainLevelSceneRemediator.cs` and run `RemediateMainLevel()`**

Apply the 6 surgical updates in `Assets/_Project/Tools/Editor/MainLevelSceneRemediator.cs`:

1. **`[PM-6]` Post-Processing (`EnsureSharedPostProcessProfile`, `L103-134`):**
   - Replace `vignette.intensity.value = 0.24f;` (`L108`) and `colorAdj.postExposure.value = 0.26f; ... colorAdj.contrast.value = 10f;` (`L128-130`) with:

```csharp
vignette.intensity.overrideState = true;
vignette.intensity.value = 0.14f;
```

```csharp
colorAdj.postExposure.overrideState = true;
colorAdj.postExposure.value = 0.62f;
colorAdj.contrast.overrideState = true;
colorAdj.contrast.value = 6f;
```

2. **`[PM-1]` One-Way Platform `surfaceArc = 160f` (`RemediateMainLevel`, right after the `One Way Platform 16/21/28` loop at `L618`):**

```csharp
foreach (var eff in UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsInactive.Include))
{
    eff.useOneWay = true;
    eff.surfaceArc = 160f;
    eff.rotationalOffset = 0f;
    EditorUtility.SetDirty(eff);
}
```

3. **`[PM-5]` Frame all 5 `CarvedArchBays` vertical & top edges + attach `VertexSwayPin2D` to `RoomBanner_*` (`BuildPrisonDressing`, `L763-805` & `CreateVerticalRoomBanner`, `L1572-1590`):**
   - Load `var archJambSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.ArchJambAssetPath);` right after `var archSillSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.ArchSillAssetPath);` (`L763`), and update the `CarvedArchBays` loop (`L766-788`):

```csharp
var archSillSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.ArchSillAssetPath);
var archJambSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.ArchJambAssetPath);
var webTint = new Color(0.85f, 0.88f, 0.90f, 0.65f);

for (int i = 0; i < CarvedArchBays.Length; i++)
{
    var (xMin, xMax, yMin, yMax) = CarvedArchBays[i];
    CreateDressingSprite(root, $"ArchBeam_TL_{i}", beamTL, new Vector3(xMin + 0.5f, yMax - 0.5f, 0f), bgLayerId, -15, litMat, Color.white);
    CreateDressingSprite(root, $"ArchBeam_TR_{i}", beamTR, new Vector3(xMax - 0.5f, yMax - 0.5f, 0f), bgLayerId, -15, litMat, Color.white);
    CreateDressingSprite(root, $"ArchWeb_TL_{i}", webSmallTL, new Vector3(xMin + 0.9f, yMax - 0.9f, 0f), bgLayerId, -16, litMat, webTint);
    CreateDressingSprite(root, $"ArchWeb_TR_{i}", webSmallTR, new Vector3(xMax - 0.9f, yMax - 0.9f, 0f), bgLayerId, -16, litMat, webTint);

    for (int sx = xMin; sx < xMax; sx++)
    {
        var sillGo = CreateDressingSprite(
            root,
            $"ArchSill_{i}_{sx}",
            archSillSprite,
            new Vector3(sx + 0.5f, yMin + 0.04f, 0f),
            bgLayerId,
            -17,
            litMat,
            Color.white);
        sillGo.transform.localScale = Vector3.one;
        EditorUtility.SetDirty(sillGo.transform);

        var lintelGo = CreateDressingSprite(
            root,
            $"ArchLintel_{i}_{sx}",
            archSillSprite,
            new Vector3(sx + 0.5f, yMax - 0.04f, 0f),
            bgLayerId,
            -17,
            litMat,
            Color.white);
        lintelGo.transform.localScale = Vector3.one;
        if (lintelGo.TryGetComponent<SpriteRenderer>(out var lintelSr))
        {
            lintelSr.flipY = true;
            EditorUtility.SetDirty(lintelSr);
        }
        EditorUtility.SetDirty(lintelGo.transform);
    }

    for (int sy = yMin; sy < yMax; sy++)
    {
        var jambLGo = CreateDressingSprite(
            root,
            $"ArchJamb_L_{i}_{sy}",
            archJambSprite,
            new Vector3(xMin + (4f / 24f), sy + 0.5f, 0f),
            bgLayerId,
            -17,
            litMat,
            Color.white);
        jambLGo.transform.localScale = Vector3.one;
        EditorUtility.SetDirty(jambLGo.transform);

        var jambRGo = CreateDressingSprite(
            root,
            $"ArchJamb_R_{i}_{sy}",
            archJambSprite,
            new Vector3(xMax - (4f / 24f), sy + 0.5f, 0f),
            bgLayerId,
            -17,
            litMat,
            Color.white);
        jambRGo.transform.localScale = Vector3.one;
        if (jambRGo.TryGetComponent<SpriteRenderer>(out var jambRSr))
        {
            jambRSr.flipX = true;
            EditorUtility.SetDirty(jambRSr);
        }
        EditorUtility.SetDirty(jambRGo.transform);
    }
}
```

   - In `CreateVerticalRoomBanner` (`L1572-1590`), attach `VertexSwayPin2D` to each piece so `_PinTopWorldY` is pinned via `MaterialPropertyBlock`:

```csharp
private static void CreateVerticalRoomBanner(
    Transform parent,
    string baseName,
    float x,
    float topY,
    Sprite topSprite,
    Sprite tiledSprite,
    Sprite botSprite,
    int sortingLayerId,
    int sortingOrder,
    Material material)
{
    float y = topY;
    var topGo = CreateDressingSprite(parent, baseName + "_Top", topSprite, new Vector3(x, y - topSprite.bounds.max.y, 0f), sortingLayerId, sortingOrder, material, Color.white);
    topGo.AddComponent<VertexSwayPin2D>().Configure(topY, 4.0f, true);
    y -= topSprite.bounds.size.y;
    var midGo = CreateDressingSprite(parent, baseName + "_Mid", tiledSprite, new Vector3(x, y - tiledSprite.bounds.max.y, 0f), sortingLayerId, sortingOrder, material, Color.white);
    midGo.AddComponent<VertexSwayPin2D>().Configure(topY, 4.0f, true);
    y -= tiledSprite.bounds.size.y;
    var botGo = CreateDressingSprite(parent, baseName + "_Bot", botSprite, new Vector3(x, y - botSprite.bounds.max.y, 0f), sortingLayerId, sortingOrder, material, Color.white);
    botGo.AddComponent<VertexSwayPin2D>().Configure(topY, 4.0f, true);
}
```

4. **`[PM-6]` Upper Gallery Fill Lights & `[PM-2]` Parent `ScrollAura_R4_{i}` under `PowerScrollPickup._icon` (`BuildPrisonDressing`, `L894-926`):**
   - Right after the `mushroomXs` loop (`L893`) and before `ExitDoor_BackGlow` (`L895`), create `GalleryFillLight_0..2`:

```csharp
Vector3[] galleryFillPositions =
{
    new Vector3(56.5f, 5.8f, 0f),
    new Vector3(70.5f, 6.8f, 0f),
    new Vector3(87.5f, 6.2f, 0f),
};
for (int i = 0; i < galleryFillPositions.Length; i++)
{
    var fillGo = new GameObject($"GalleryFillLight_{i}");
    fillGo.transform.SetParent(root, false);
    fillGo.transform.position = galleryFillPositions[i];
    var fillLight = fillGo.AddComponent<Light2D>();
    fillLight.lightType = Light2D.LightType.Point;
    fillLight.color = new Color(1.0f, 0.84f, 0.58f, 1f);
    fillLight.intensity = 1.15f;
    fillLight.pointLightInnerRadius = 1.5f;
    fillLight.pointLightOuterRadius = 7.5f;
    fillLight.falloffIntensity = 0.48f;
    fillLight.enabled = true;
    EditorUtility.SetDirty(fillGo);
    EditorUtility.SetDirty(fillLight);
}
```

   - Replace the `PowerScrollPickup` loop (`L909-926`) with idempotent parenting under `_icon`:

```csharp
var scrolls = UnityEngine.Object.FindObjectsByType<PowerScrollPickup>(FindObjectsInactive.Include)
    .OrderBy(s => s.transform.position.x)
    .ToArray();
for (int i = 0; i < scrolls.Length; i++)
{
    foreach (var oldSr in scrolls[i].GetComponentsInChildren<SpriteRenderer>(true))
    {
        if (oldSr != null && oldSr.gameObject.name.StartsWith("ScrollAura_R4_", StringComparison.Ordinal))
            UnityEngine.Object.DestroyImmediate(oldSr.gameObject);
    }

    var soScroll = new SerializedObject(scrolls[i]);
    var iconTr = soScroll.FindProperty("_icon")?.objectReferenceValue as Transform ?? scrolls[i].transform;
    var auraGo = CreateDressingSprite(
        iconTr,
        $"ScrollAura_R4_{i}",
        fxGlowSprite,
        iconTr.position,
        vfxLayerId,
        -2,
        scrollGlowMat,
        new Color(1f, 1f, 1f, 0.36f));
    auraGo.transform.localPosition = Vector3.zero;
    auraGo.transform.localScale = new Vector3(0.46f, 0.46f, 1f);
    EditorUtility.SetDirty(auraGo.transform);

    var glowProp = soScroll.FindProperty("_glow");
    if (glowProp != null && glowProp.objectReferenceValue == null)
    {
        glowProp.objectReferenceValue = auraGo;
        soScroll.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(scrolls[i]);
    }
}
```

5. **`[PM-4]` `Foreground_Silhouettes` vertical camera lock & `32u`-periodic anchors (`L1052-1078`):**

```csharp
var fgGo = new GameObject("Foreground_Silhouettes");
fgGo.transform.SetParent(parallaxRoot.transform, false);
fgGo.transform.localPosition = Vector3.zero;
var fgParallax = fgGo.AddComponent<ParallaxLayer2D>();
fgParallax.Configure(camTransform, new Vector2(1.18f, 1.0f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
fgParallax.ConfigureVerticalCameraLock(true, 3.8f);

var fgBeamTL = LoadSpriteRequired(PrisonSpritesDir + "blackBeam_topLeft_0_00.png");
var fgBeamTR = LoadSpriteRequired(PrisonSpritesDir + "blackBeam_topRight_0_00.png");
var fgChain = LoadSpriteRequired(PrisonSpritesDir + "cageChain_00.png");
var fgTint = new Color(6f / 255f, 10f / 255f, 16f / 255f, 0.96f);

float[] fgAnchorXs = { -20f, -12f, -4f, 4f, 12f, 20f, 28f, 36f };
for (int i = 0; i < fgAnchorXs.Length; i++)
{
    float x = fgAnchorXs[i];
    var beamSprite = (i % 2 == 0) ? fgBeamTL : fgBeamTR;
    var bGo = CreateDressingSprite(fgGo.transform, $"FgBeam_{i}", beamSprite, Vector3.zero, fgLayerId, 20, unlitMat, fgTint);
    bGo.transform.localPosition = new Vector3(x, 11.45f, 0f);
    EditorUtility.SetDirty(bGo.transform);

    var cGo = CreateDressingSprite(fgGo.transform, $"FgChain_{i}", fgChain, Vector3.zero, fgLayerId, 19, unlitMat, fgTint);
    cGo.transform.localPosition = new Vector3(x + 2.2f, 10.30f, 0f);
    cGo.transform.localScale = new Vector3(1.1f, 2.0f, 1f);
    EditorUtility.SetDirty(cGo.transform);
}
EditorUtility.SetDirty(fgParallax);
```

6. **`[PM-3]` & `[PM-6]` Lighting & `Background/-8..-7` sorting on `LanternFlameCore_R4` and `TorchEmbers` (`L1125-1225` & `CreateTorchEmbers`, `L1444-1500`):**
   - In the `foreach (var light in allLights)` loop (`L1125-1225`), update `Global Light 2D`, `Exit Door/Accent light`, warm lantern intensities/radii, `LanternFlameCore_R4`, and `CreateTorchEmbers`:

```csharp
if (light.lightType == Light2D.LightType.Global)
{
    light.intensity = 0.62f;
    light.color = new Color(0.84f, 0.91f, 0.96f, 1f);
    SetLightSortingLayers(light, allLayerIds);
    EditorUtility.SetDirty(light);
    continue;
}

if (light.lightType != Light2D.LightType.Point)
    continue;

SetLightSortingLayers(light, gameplayLayerIds);
SetLightNormalMapQuality(light, Light2D.NormalMapQuality.Fast, 3.2f);

if (light.gameObject.name == "Accent light" &&
    light.transform.parent != null &&
    light.transform.parent.name == "Exit Door")
{
    light.intensity = 1.65f;
    light.pointLightInnerRadius = 1.5f;
    light.pointLightOuterRadius = 7.5f;
    light.falloffIntensity = 0.40f;
}

if (IsPrimaryLanternLight(light))
{
    if (light.color.r >= light.color.b)
    {
        if (light.gameObject.name == "Exit hall lantern")
        {
            light.intensity = 2.10f;
            light.pointLightInnerRadius = 1.8f;
            light.pointLightOuterRadius = 9.2f;
            light.falloffIntensity = 0.35f;
        }
        else if (light.gameObject.name == "Spawn lantern")
        {
            light.intensity = 1.85f;
            light.pointLightInnerRadius = 1.5f;
            light.pointLightOuterRadius = 8.2f;
            light.falloffIntensity = 0.42f;
        }
        else
        {
            light.intensity = 2.00f;
            light.pointLightInnerRadius = 1.8f;
            light.pointLightOuterRadius = 9.0f;
            light.falloffIntensity = 0.42f;
        }
    }
    else
    {
        light.intensity = 1.45f;
        light.pointLightOuterRadius = 6.0f;
    }

    ConfigureLightVolumetric(light, true, 0.14f);

    if (!light.TryGetComponent<TorchFlicker2D>(out var flicker))
        flicker = light.gameObject.AddComponent<TorchFlicker2D>();

    flicker.Configure(light, light.intensity, light.pointLightOuterRadius, amplitude: 0.08f, speed: 3.2f, phaseOffset: primaryLanternIndex * 1.15f);
    flicker.enabled = true;
    EditorUtility.SetDirty(flicker);

    DestroyChildIfExists(light.transform, "LanternFixture_R4");
    DestroyChildIfExists(light.transform, "LanternFlameCore_R4");
    DestroyChildIfExists(light.transform, "TorchEmbers");

    if (light.color.r >= light.color.b)
    {
        var fixtureGo = CreateDressingSprite(
            light.transform,
            "LanternFixture_R4",
            lanternFixtureSprite,
            light.transform.position,
            bgLayerId,
            -9,
            litMat,
            Color.white);
        fixtureGo.transform.localPosition = new Vector3(0f, PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY, 0f);
        fixtureGo.transform.localScale = Vector3.one;
        EditorUtility.SetDirty(fixtureGo.transform);

        var coreGo = CreateDressingSprite(light.transform, "LanternFlameCore_R4", fxGlowSprite, light.transform.position, bgLayerId, -8, flameMat, new Color(1f, 1f, 1f, 0.82f));
        coreGo.transform.localPosition = Vector3.zero;
        coreGo.transform.localScale = new Vector3(0.085f, 0.12f, 1f);
        EditorUtility.SetDirty(coreGo.transform);

        CreateTorchEmbers(light.transform, particlesUnlitMat, bgLayerId, -7);
    }

    primaryLanternIndex++;
}
```

   - Update `CreateTorchEmbers` (`L1444-1496`) signature to `private static void CreateTorchEmbers(Transform parent, Material particlesUnlitMat, int sortingLayerId, int sortingOrder = -7)` and set `psr.sortingLayerID = sortingLayerId; psr.sortingOrder = sortingOrder;`.

- [ ] **Step 4: Run `RemediateMainLevel` and verify all `9/9` `MainLevelIntegrityTests` pass**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.RemediateMainLevel -quit -logFile D:/thietkegame/Logs/r5-mainlevel-remediate.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.Core.Tests.MainLevelIntegrityTests" -testResults D:/thietkegame/Logs/r5-t2-green.xml -logFile D:/thietkegame/Logs/r5-t2-green.log
```
Expected: `total="9" passed="9" failed="0"`, `0` warnings in both logs.

---

### Task 3: Replicate Full R4.2 + R5 Visual Pipeline to `NhaGiamGiengChuong.unity` & All 5 Room Prefabs

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{AB,CD,ELF,G,HJK}.prefab` (regenerated by `NhaGiamGiengChuongBuilder.BuildAll`)
- Modify: `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` (regenerated by `NhaGiamGiengChuongBuilder.BuildAll`)
- Test: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs:530-545`

**Interfaces:**
- Consumes:
  - `DeadCells.Tools.Editor.SortingLayerSetup.EnsureDeadCellsSortingLayers()`, `.GetSortingLayerId(string)`
  - `DeadCells.Tools.Editor.PrisonPropSpriteGenerator.EnsureAll()`, `.WallLanternAssetPath`, `.ArchSillAssetPath`, `.ArchJambAssetPath`, `.WallLanternFlameOffsetLocalY`
  - `DeadCells.Tools.Editor.EnvironmentMaterialBuilder.EnsureAll()`, `.MaterialDirectory`
  - `DeadCells.Tools.Editor.CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials()`
  - `DeadCells.Tools.Editor.MainLevelSceneRemediator.EnsureSharedPostProcessProfile()`
  - `DeadCells.CameraSystem.ParallaxLayer2D` (with `ConfigureVerticalCameraLock`)
  - `DeadCells.VFX.TorchFlicker2D`, `DeadCells.VFX.VertexSwayPin2D`
- Produces:
  - `NhaGiamGiengChuongBuilder.BuildAll()` generating `Rooms/{CD,AB,G,HJK,ELF}.prefab` and `NhaGiamGiengChuong.unity` with the complete R4.2 + R5 visual stack.
  - `NhaGiamGiengChuongBuilder.CaptureRound5GpuVerificationFrames()` — captures 5 real GPU 1280×720 sRGB frames across the 5 room clusters (`gpu_r5_ng_01_room_ab.png`, `gpu_r5_ng_02_room_cd.png`, `gpu_r5_ng_03_room_g.png`, `gpu_r5_ng_04_room_hjk.png`, `gpu_r5_ng_05_room_elf.png`) into `R5_GPU_CAPTURE_DIR`.

- [ ] **Step 1: Add `"TilesetTools.Editor"` reference to `NhaGiamGiengChuong.Editor.asmdef` and write failing test `A11` in `MapWiringTests.cs`**

1. Replace `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef` with:

```json
{
  "name": "NhaGiamGiengChuong.Editor",
  "rootNamespace": "DeadCells.NhaGiamGiengChuong.Editor",
  "references": [
    "NhaGiamGiengChuong",
    "Core",
    "Combat",
    "Enemies",
    "Player",
    "HUD",
    "CameraSystem",
    "VFX",
    "TilesetTools.Editor",
    "Unity.InputSystem",
    "UnityEngine.UI",
    "Unity.RenderPipelines.Core.Runtime",
    "Unity.RenderPipelines.Universal.Runtime",
    "Unity.RenderPipelines.Universal.2D.Runtime"
  ],
  "includePlatforms": [ "Editor" ],
  "autoReferenced": false
}
```

2. Append `[Test] public void A11_Round5_ParallaxShadersNormalMapsZeroMixelPropsAndOneWayArc()` to `MapWiringTests` in `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (right after `A10_DeadCellsPrisonVisualStandard`, before `AssertZone`):

```csharp
[Test]
public void A11_Round5_ParallaxShadersNormalMapsZeroMixelPropsAndOneWayArc()
{
    EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

    // 1. [PM-1] All 10 PlatformEffector2D components across the 5 rooms use surfaceArc == 160f
    var effectors = Object.FindObjectsByType<PlatformEffector2D>(FindObjectsInactive.Include);
    Assert.AreEqual(10, effectors.Length, "[A11/OneWayCount] Expected 10 PlatformEffector2D components in NhaGiamGiengChuong.");
    foreach (var eff in effectors)
    {
        Assert.IsTrue(eff.useOneWay, $"[A11/OneWay/{eff.name}] useOneWay must be true.");
        Assert.AreEqual(160f, eff.surfaceArc, 1e-3f,
            $"[A11/OneWayArc/{eff.name}] surfaceArc must be 160f so side contacts never block walk/roll.");
    }

    // 2. [P1-R1] & [PM-3] Zero legacy LightAndBanner_001/005; >= 22 24-PPU LanternFixture_R5 (Background/-9), LanternFlameCore_R5 (Background/-8), TorchEmbers (Background/-7)
    var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
    foreach (var sr in allRenderers)
    {
        if (sr.sprite == null)
            continue;
        Assert.AreNotEqual("LightAndBanner_001", sr.sprite.name, $"[A11/NoMixelTorch/{sr.name}] Must not use legacy 32-PPU LightAndBanner_001.");
        Assert.AreNotEqual("LightAndBanner_005", sr.sprite.name, $"[A11/NoMixelLantern/{sr.name}] Must not use legacy 32-PPU LightAndBanner_005.");
    }

    var fixtures = allRenderers.Where(sr => sr.gameObject.name == "LanternFixture_R5").ToArray();
    Assert.GreaterOrEqual(fixtures.Length, 22, $"[A11/LanternFixtures] Expected >= 22 LanternFixture_R5 renderers, found {fixtures.Length}.");
    foreach (var fix in fixtures)
    {
        Assert.IsNotNull(fix.sprite);
        Assert.AreEqual("prisonWallLantern_00", fix.sprite.name);
        Assert.AreEqual(24f, fix.sprite.pixelsPerUnit, 1e-4f);
        Assert.AreEqual(Vector3.one, fix.transform.localScale);
        Assert.AreEqual("Background", fix.sortingLayerName);
        Assert.AreEqual(-9, fix.sortingOrder);

        var parent = fix.transform.parent;
        Assert.IsNotNull(parent);
        var core = parent.Find("LanternFlameCore_R5")?.GetComponent<SpriteRenderer>();
        Assert.IsNotNull(core, $"[A11/FlameCore/{parent.name}] Missing LanternFlameCore_R5.");
        Assert.AreEqual("Background", core.sortingLayerName, "[A11/FlameCoreLayer] Must sort on Background behind Actors.");
        Assert.AreEqual(-8, core.sortingOrder);
        Assert.IsNotNull(core.sharedMaterial);
        Assert.AreEqual("DeadCells/EmissiveHdr2D", core.sharedMaterial.shader.name);

        var embers = parent.Find("TorchEmbers")?.GetComponent<ParticleSystemRenderer>();
        Assert.IsNotNull(embers, $"[A11/TorchEmbers/{parent.name}] Missing TorchEmbers.");
        Assert.AreEqual("Background", embers.sortingLayerName, "[A11/TorchEmbersLayer] Must sort on Background behind Actors.");
        Assert.AreEqual(-7, embers.sortingOrder);
    }

    // 3. [P1-R2] 4/4 GlowingMushroom_0..3 (24 PPU @ scale 1,1,1), 4/4 MushroomHalo_0..3 (_DstBlend=1, scale.x<=0.20, above floor), 4/4 MushroomLight_0..3
    float[] expectedMushroomFloors = { 0f, 0f, 16f, 24f };
    for (int i = 0; i < 4; i++)
    {
        var mGo = GameObject.Find($"GlowingMushroom_{i}");
        Assert.IsNotNull(mGo, $"[A11/Mushroom/{i}] Missing GlowingMushroom_{i}.");
        var mSr = mGo.GetComponent<SpriteRenderer>();
        Assert.IsNotNull(mSr?.sprite);
        Assert.AreEqual("glowingMushroom_3_00", mSr.sprite.name);
        Assert.AreEqual(Vector3.one, mGo.transform.localScale);
        Assert.AreEqual("DeadCells/EmissiveHdr2D", mSr.sharedMaterial.shader.name);

        var hGo = GameObject.Find($"MushroomHalo_{i}");
        Assert.IsNotNull(hGo, $"[A11/MushroomHalo/{i}] Missing MushroomHalo_{i}.");
        var hSr = hGo.GetComponent<SpriteRenderer>();
        Assert.IsNotNull(hSr?.sharedMaterial);
        Assert.AreEqual(1f, hSr.sharedMaterial.GetFloat("_DstBlend"), 1e-4f);
        Assert.LessOrEqual(hGo.transform.localScale.x, 0.20f + 1e-4f);
        Assert.Greater(hSr.bounds.min.y, expectedMushroomFloors[i],
            $"[A11/MushroomHaloClearFloor/{i}] Halo bottom ({hSr.bounds.min.y:F2}) must stay above floorTop ({expectedMushroomFloors[i]:F2}).");

        var lGo = GameObject.Find($"MushroomLight_{i}");
        Assert.IsNotNull(lGo?.GetComponent<Light2D>(), $"[A11/MushroomLight/{i}] Missing MushroomLight_{i}.");
    }

    // 4. [P1-R3] & [PM-5] 5 carved arch bays with >= 25 ArchSill_* and >= 40 ArchJamb_* at 24 PPU, scale (1,1,1), Background/-17
    var archSills = allRenderers.Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal)).ToArray();
    Assert.GreaterOrEqual(archSills.Length, 25, $"[A11/ArchSills] Expected >= 25 ArchSill_* renderers, found {archSills.Length}.");
    foreach (var sill in archSills)
    {
        Assert.AreEqual("prisonArchSill_00", sill.sprite.name);
        Assert.AreEqual(24f, sill.sprite.pixelsPerUnit, 1e-4f);
        Assert.AreEqual(Vector3.one, sill.transform.localScale);
        Assert.AreEqual("Background", sill.sortingLayerName);
        Assert.AreEqual(-17, sill.sortingOrder);
    }

    var archJambs = allRenderers.Where(sr => sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal)).ToArray();
    Assert.GreaterOrEqual(archJambs.Length, 40, $"[A11/ArchJambs] Expected >= 40 ArchJamb_* renderers, found {archJambs.Length}.");
    foreach (var jamb in archJambs)
    {
        Assert.AreEqual("prisonArchJamb_00", jamb.sprite.name);
        Assert.AreEqual(24f, jamb.sprite.pixelsPerUnit, 1e-4f);
        Assert.AreEqual(Vector3.one, jamb.transform.localScale);
        Assert.AreEqual("Background", jamb.sortingLayerName);
        Assert.AreEqual(-17, jamb.sortingOrder);
    }

    // 5. [P2-2] Multi-elevation 3-piece vertical room banners carry VertexSwayPin2D with exact _PinTopWorldY via MaterialPropertyBlock
    (string name, float topY)[] expectedBanners =
    {
        ("Art_C_Banner", 16.5f),
        ("Art_J_Banner", 21.8f),
        ("Art_E_Banner", 30.2f),
        ("Art_F_Banner", 37.2f),
    };
    var block = new MaterialPropertyBlock();
    foreach (var (bannerName, topY) in expectedBanners)
    {
        var bannerGo = GameObject.Find(bannerName);
        Assert.IsNotNull(bannerGo, $"[A11/Banner/{bannerName}] Missing multi-piece banner container.");
        var pin = bannerGo.GetComponent<VertexSwayPin2D>();
        Assert.IsNotNull(pin, $"[A11/BannerPin/{bannerName}] Missing VertexSwayPin2D.");
        pin.ApplyPropertyBlock();

        var pieces = bannerGo.GetComponentsInChildren<SpriteRenderer>(true);
        Assert.AreEqual(3, pieces.Length, $"[A11/BannerPieces/{bannerName}] Expected 3 pieces (Top, Mid, Bot).");
        foreach (var piece in pieces)
        {
            Assert.AreEqual(Vector3.one, piece.transform.localScale, $"[A11/BannerScale/{bannerName}/{piece.name}] Must use 1:1 24-PPU scale.");
            Assert.IsNotNull(piece.sharedMaterial);
            Assert.AreEqual("DeadCells/VertexSway2D", piece.sharedMaterial.shader.name);
            piece.GetPropertyBlock(block);
            Assert.AreEqual(1.0f, block.GetFloat("_UseWorldYPin"), 1e-4f);
            Assert.AreEqual(topY, block.GetFloat("_PinTopWorldY"), 1e-3f,
                $"[A11/BannerPinY/{bannerName}/{piece.name}] _PinTopWorldY must match room topY={topY:F1}.");
        }
    }

    // 6. 4-Tier Hybrid Parallax, Lighting Isolation, GroundMist_R5 & ShadowCasters_R5
    var parallaxRoot = GameObject.Find("Prison_Parallax_R5");
    Assert.IsNotNull(parallaxRoot, "[A11/ParallaxRoot] Missing Prison_Parallax_R5.");
    var farBackdrop = parallaxRoot.transform.Find("ParallaxFar_Backdrop");
    var midAlcoves = parallaxRoot.transform.Find("ParallaxMid_Alcoves");
    var fgSilhouettes = parallaxRoot.transform.Find("Foreground_Silhouettes");
    Assert.IsNotNull(farBackdrop?.GetComponent<ParallaxLayer2D>(), "[A11/ParallaxFar]");
    Assert.IsNotNull(midAlcoves?.GetComponent<ParallaxLayer2D>(), "[A11/ParallaxMid]");
    Assert.IsNotNull(fgSilhouettes?.GetComponent<ParallaxLayer2D>(), "[A11/Foreground]");

    var farRenderers = farBackdrop.GetComponentsInChildren<SpriteRenderer>(true);
    Assert.GreaterOrEqual(farRenderers.Length, 9, "[A11/ParallaxFarPanels] Expected 3x3 grid of T_Prison_ParallaxFar_Grayscale panels.");
    foreach (var sr in farRenderers)
    {
        Assert.AreEqual("T_Prison_ParallaxFar_Grayscale", sr.sprite.name);
        Assert.AreEqual(Vector3.one, sr.transform.localScale);
        Assert.AreEqual("DeadCells/ParallaxGradientMap2D", sr.sharedMaterial.shader.name);
    }

    var midRenderers = midAlcoves.GetComponentsInChildren<SpriteRenderer>(true);
    Assert.IsNotEmpty(midRenderers);
    Assert.IsFalse(midRenderers.Any(sr => sr.sprite != null && sr.sprite.name.StartsWith("alccoveParalaxeB_00", StringComparison.Ordinal)),
        "[A11/NoBlackAlcoveB] ParallaxMid_Alcoves must contain 0 alccoveParalaxeB_00 sprites.");

    var soFg = new SerializedObject(fgSilhouettes.GetComponent<ParallaxLayer2D>());
    Assert.IsTrue(soFg.FindProperty("_lockYToCameraStartOffset").boolValue,
        "[A11/FgVerticalLock] Foreground_Silhouettes must enable _lockYToCameraStartOffset.");

    int farLayerId = SortingLayer.NameToID("ParallaxFar");
    int midLayerId = SortingLayer.NameToID("ParallaxMid");
    int fgLayerId = SortingLayer.NameToID("Foreground");
    var pointLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
        .Where(l => l.lightType == Light2D.LightType.Point)
        .ToArray();
    int volumetricFlickerCount = 0;
    foreach (var light in pointLights)
    {
        var soLight = new SerializedObject(light);
        var layersProp = soLight.FindProperty("m_ApplyToSortingLayers");
        Assert.IsNotNull(layersProp);
        var applied = Enumerable.Range(0, layersProp.arraySize).Select(idx => layersProp.GetArrayElementAtIndex(idx).intValue).ToArray();
        CollectionAssert.DoesNotContain(applied, farLayerId, $"[A11/LightIsolation/{light.name}] Must exclude ParallaxFar.");
        CollectionAssert.DoesNotContain(applied, midLayerId, $"[A11/LightIsolation/{light.name}] Must exclude ParallaxMid.");
        CollectionAssert.DoesNotContain(applied, fgLayerId, $"[A11/LightIsolation/{light.name}] Must exclude Foreground.");

        if (light.enabled && light.volumetricEnabled && light.volumeIntensity > 0f && light.GetComponent<TorchFlicker2D>() != null)
            volumetricFlickerCount++;
    }
    Assert.GreaterOrEqual(volumetricFlickerCount, 20,
        $"[A11/VolumetricFlicker] Expected >= 20 Point Light2Ds with volumetricEnabled + TorchFlicker2D, found {volumetricFlickerCount}.");

    var mistRoot = GameObject.Find("GroundMist_R5");
    Assert.IsNotNull(mistRoot, "[A11/GroundMistRoot]");
    var mistRenderers = mistRoot.GetComponentsInChildren<SpriteRenderer>(true);
    Assert.GreaterOrEqual(mistRenderers.Length, 3);
    foreach (var mr in mistRenderers)
        Assert.AreEqual("DeadCells/GroundMist2D", mr.sharedMaterial.shader.name);

    var castersRoot = GameObject.Find("ShadowCasters_R5");
    Assert.IsNotNull(castersRoot, "[A11/ShadowCastersRoot]");
    Assert.GreaterOrEqual(castersRoot.GetComponentsInChildren<ShadowCaster2D>(true).Length, 3);
}
```

- [ ] **Step 2: Run `A11` to verify Red failure**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -testFilter "DeadCells.NhaGiamGiengChuong.Tests.Assets.MapWiringTests.A11_Round5_ParallaxShadersNormalMapsZeroMixelPropsAndOneWayArc" -testResults D:/thietkegame/Logs/r5-t3-red.xml -logFile D:/thietkegame/Logs/r5-t3-red.log
```
Expected: FAIL on `surfaceArc == 180f` and missing `Prison_Parallax_R5` / `LanternFixture_R5`.

- [ ] **Step 3: Upgrade `NhaGiamGiengChuongBuilder.cs` and rebuild all 5 Room Prefabs + `NhaGiamGiengChuong.unity`**

In `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`, apply these exact updates:

1. **Add `using DeadCells.Tools.Editor;` at top of file (`L10`) and extend `PrisonPalette` (`L58-108`) & `LoadPrisonPalette()` (`L152-210`):**
   - Add `using DeadCells.Tools.Editor;` after `using DeadCells.VFX;`.
   - Add sprite and material fields to `PrisonPalette`:

```csharp
public Sprite WallLantern;
public Sprite ArchSill;
public Sprite ArchJamb;
public Sprite GlowingMushroomBright;
public Sprite FxGlowWhite;
public Sprite ParallaxFarSprite;
public Sprite AlcoveParallaxA;
public Sprite VineParallaxA;
public Sprite VineParallaxB;
public Sprite BlackBeamTL;
public Sprite BlackBeamTR;
public Sprite CageChain;
public Sprite WhitePixel;
public Sprite WebSmallTLSprite;
public Sprite WebSmallTRSprite;

public Material MatParallaxFar;
public Material MatParallaxMid;
public Material MatGodRay;
public Material MatBannerSway;
public Material MatRoomBannerSway;
public Material MatVineSway;
public Material MatEmissiveFlame;
public Material MatEmissiveMushroom;
public Material MatEmissiveMushroomHalo;
public Material MatEmissiveExit;
public Material MatGroundMist;
public Material MatParticlesUnlit;
```

   - In `LoadPrisonPalette()` (`L152-210`), populate those fields:

```csharp
WallLantern = LoadSpriteAtPath(PrisonPropSpriteGenerator.WallLanternAssetPath),
ArchSill = LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchSillAssetPath),
ArchJamb = LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchJambAssetPath),
GlowingMushroomBright = LoadPrisonSprite("glowingMushroom_3_00.png"),
FxGlowWhite = LoadSpriteAtPath("Assets/_Project/Art/VFX/ReferenceFeedback/fxGlowWhite.png"),
ParallaxFarSprite = LoadSpriteAtPath("Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png"),
AlcoveParallaxA = LoadPrisonSprite("alccoveParalaxeA_00.png"),
VineParallaxA = LoadPrisonSprite("paralaxLianeA_00.png"),
VineParallaxB = LoadPrisonSprite("paralaxLianeB_00.png"),
BlackBeamTL = LoadPrisonSprite("blackBeam_topLeft_0_00.png"),
BlackBeamTR = LoadPrisonSprite("blackBeam_topRight_0_00.png"),
CageChain = LoadPrisonSprite("cageChain_00.png"),
WhitePixel = LoadPrisonSprite("whitePixel_00.png"),
WebSmallTLSprite = LoadPrisonSprite("webSmallTL_00.png"),
WebSmallTRSprite = LoadPrisonSprite("webSmallTR_00.png"),

MatParallaxFar = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_ParallaxFar_Prison.mat"),
MatParallaxMid = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_ParallaxMid_Prison.mat"),
MatGodRay = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_GodRay_Additive.mat"),
MatBannerSway = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Banner_Sway.mat"),
MatRoomBannerSway = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_RoomBanner_Sway.mat"),
MatVineSway = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Vine_Sway.mat"),
MatEmissiveFlame = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Flame.mat"),
MatEmissiveMushroom = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Mushroom.mat"),
MatEmissiveMushroomHalo = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_MushroomHalo.mat"),
MatEmissiveExit = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Exit.mat"),
MatGroundMist = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_GroundMist_Prison.mat"),
MatParticlesUnlit = LoadMaterialAtPath(ParticlesUnlitMatPath),
```

   - Add helper loaders right after `LoadPrisonSprite` (`L1343`):

```csharp
private static Sprite LoadSpriteAtPath(string path)
{
    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
    if (sprite == null)
        throw new InvalidOperationException("Missing Sprite at path: " + path);
    return sprite;
}

private static Material LoadMaterialAtPath(string path)
{
    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (mat == null)
        throw new InvalidOperationException("Missing Material at path: " + path);
    return mat;
}
```

2. **Top of `BuildAll()` (`L111-128`):**
   - Call the R4.2/R5 visual generators before `LoadPrisonPalette()`:

```csharp
EnsureFolder(RoomsFolder);
EnsureFolder(UiFolder);

SortingLayerSetup.EnsureDeadCellsSortingLayers();
PrisonPropSpriteGenerator.EnsureAll();
EnvironmentMaterialBuilder.EnsureAll();
CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials();
MainLevelSceneRemediator.EnsureSharedPostProcessProfile();
```

3. **`AddOneWayBox` (`L1288-1303`):**
   - Set `surfaceArc = 160f` and `rotationalOffset = 0f`:

```csharp
var effector = go.AddComponent<PlatformEffector2D>();
effector.useOneWay = true;
effector.surfaceArc = 160f;
effector.rotationalOffset = 0f;
return col;
```

4. **Sorting Layers on Visual Tilemaps & Props (`CreateRoomVisualGrid`, `L1347-1374` & Prop Helpers `L1529-1849`):**
   - Update `CreateRoomVisualGrid` and `CreateVisualTilemap` (`L1347-1374`) to set `sortingLayerID`:

```csharp
private static void CreateRoomVisualGrid(
    GameObject artRoot,
    Material spriteLit,
    out Tilemap bgTm,
    out Tilemap decalTm,
    out Tilemap solidTm,
    out Tilemap rimTm)
{
    var gridGo = CreateChild(artRoot, "VisualGrid");
    var grid = gridGo.AddComponent<Grid>();
    grid.cellSize = new Vector3(1f, 1f, 0f);

    int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
    int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");

    bgTm = CreateVisualTilemap(gridGo, "TM_Background", spriteLit, bgLayerId, -20, Color.white);
    decalTm = CreateVisualTilemap(gridGo, "TM_WallDecals", spriteLit, bgLayerId, -18, new Color(0.88f, 0.92f, 0.90f, 1f));
    solidTm = CreateVisualTilemap(gridGo, "TM_SolidVisual", spriteLit, groundLayerId, -5, Color.white);
    rimTm = CreateVisualTilemap(gridGo, "TM_SolidRim", spriteLit, groundLayerId, -3, Color.white);
}

private static Tilemap CreateVisualTilemap(GameObject gridGo, string name, Material spriteLit, int sortingLayerId, int sortingOrder, Color tint)
{
    var go = CreateChild(gridGo, name);
    var tm = go.AddComponent<Tilemap>();
    tm.color = tint;
    var renderer = go.AddComponent<TilemapRenderer>();
    renderer.sharedMaterial = spriteLit;
    renderer.sortingLayerID = sortingLayerId;
    renderer.sortingOrder = sortingOrder;
    return tm;
}
```

   - In `AddSolidBeamSprite` (`L1534`), `AddPrisonPlankRow` (`L1603`), `AddPlatformCornerBrace` (`L1620`), `AddPrisonChainArt` (`L1633, L1644, L1653`), and `AddCornerBeamsAndWebs` (`L1678, L1686`), set `sr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");`.
   - In `AddPrisonSpriteProp` (`L1821-1831`), assign `Background` when `sortingOrder <= -9` and `Ground` when `sortingOrder >= -8`:

```csharp
private static void AddPrisonSpriteProp(GameObject parent, string name, Sprite sprite, Material mat, Vector2 pos, float scale, int sortingOrder, Color color)
{
    var go = CreateChild(parent, name);
    go.transform.position = new Vector3(pos.x, pos.y, 0f);
    go.transform.localScale = new Vector3(scale, scale, 1f);
    var sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = sprite;
    sr.sharedMaterial = mat;
    sr.sortingLayerID = sortingOrder <= -9
        ? SortingLayerSetup.GetSortingLayerId("Background")
        : SortingLayerSetup.GetSortingLayerId("Ground");
    sr.sortingOrder = sortingOrder;
    sr.color = color;
}
```

5. **Carved Arch Viewing Bays + `ArchSill_*`, `ArchLintel_*`, `ArchJamb_L/R_*` (`Background/-17`):**
   - Add helper `CarveAndFrameArchBay` right after `PaintPrisonBackWall`:

```csharp
private static void CarveAndFrameArchBay(
    GameObject artRoot,
    Tilemap bgTm,
    Tilemap decalTm,
    PrisonPalette pal,
    Material litMat,
    int bayIndex,
    int xMin,
    int xMax,
    int yMin,
    int yMax)
{
    for (int cx = xMin; cx < xMax; cx++)
    {
        for (int cy = yMin; cy < yMax; cy++)
        {
            bool isTopCorner = (cy == yMax - 1) && (cx == xMin || cx == xMax - 1);
            if (isTopCorner)
                continue;

            var cell = new Vector3Int(cx, cy, 0);
            bgTm.SetTile(cell, null);
            decalTm.SetTile(cell, null);
        }
    }

    int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
    var webTint = new Color(0.85f, 0.88f, 0.90f, 0.65f);

    CreateArchFrameSprite(artRoot, $"ArchBeam_TL_{bayIndex}", pal.LittleBeamTL, new Vector3(xMin + 0.5f, yMax - 0.5f, 0f), bgLayerId, -15, litMat, Color.white);
    CreateArchFrameSprite(artRoot, $"ArchBeam_TR_{bayIndex}", pal.LittleBeamTR, new Vector3(xMax - 0.5f, yMax - 0.5f, 0f), bgLayerId, -15, litMat, Color.white);
    CreateArchFrameSprite(artRoot, $"ArchWeb_TL_{bayIndex}", pal.WebSmallTLSprite, new Vector3(xMin + 0.9f, yMax - 0.9f, 0f), bgLayerId, -16, litMat, webTint);
    CreateArchFrameSprite(artRoot, $"ArchWeb_TR_{bayIndex}", pal.WebSmallTRSprite, new Vector3(xMax - 0.9f, yMax - 0.9f, 0f), bgLayerId, -16, litMat, webTint);

    for (int sx = xMin; sx < xMax; sx++)
    {
        CreateArchFrameSprite(artRoot, $"ArchSill_{bayIndex}_{sx}", pal.ArchSill, new Vector3(sx + 0.5f, yMin + 0.04f, 0f), bgLayerId, -17, litMat, Color.white);
        var lintelGo = CreateArchFrameSprite(artRoot, $"ArchLintel_{bayIndex}_{sx}", pal.ArchSill, new Vector3(sx + 0.5f, yMax - 0.04f, 0f), bgLayerId, -17, litMat, Color.white);
        lintelGo.GetComponent<SpriteRenderer>().flipY = true;
    }

    for (int sy = yMin; sy < yMax; sy++)
    {
        CreateArchFrameSprite(artRoot, $"ArchJamb_L_{bayIndex}_{sy}", pal.ArchJamb, new Vector3(xMin + (4f / 24f), sy + 0.5f, 0f), bgLayerId, -17, litMat, Color.white);
        var jambRGo = CreateArchFrameSprite(artRoot, $"ArchJamb_R_{bayIndex}_{sy}", pal.ArchJamb, new Vector3(xMax - (4f / 24f), sy + 0.5f, 0f), bgLayerId, -17, litMat, Color.white);
        jambRGo.GetComponent<SpriteRenderer>().flipX = true;
    }
}

private static GameObject CreateArchFrameSprite(
    GameObject parent,
    string name,
    Sprite sprite,
    Vector3 worldPos,
    int sortingLayerId,
    int sortingOrder,
    Material material,
    Color color)
{
    var go = CreateChild(parent, name);
    go.transform.position = worldPos;
    go.transform.localScale = Vector3.one;
    var sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = sprite;
    sr.sortingLayerID = sortingLayerId;
    sr.sortingOrder = sortingOrder;
    sr.sharedMaterial = material;
    sr.color = color;
    return go;
}
```

   - Call `CarveAndFrameArchBay` right after `PaintPrisonBackWall(...)` in each of the 5 room builders (producing `27` `ArchSill_*`, `27` `ArchLintel_*`, and `40` `ArchJamb_*` total, all clear of wall lanterns and grounded landmarks):
     - In `BuildRoomAB` (after `L330`): `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 0, 25, 30, 2, 5);`
     - In `BuildRoomCD` (after `L263`): `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 1, 46, 52, 11, 16);`
     - In `BuildRoomG` (after `L383`): `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 2, 63, 68, 9, 13);`
     - In `BuildRoomHJK` (after `L463`): `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 3, -9, -3, 18, 21);`
     - In `BuildRoomELF` (after `L539`): `CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 4, -9, -4, 25, 30);`

6. **`24 PPU` Wall Lanterns, Swaying Banners, Additive God-Rays, and `Art_F_ExitGlow` (`L294, L356, L494, L581, L587-602, L1720-1796`):**
   - On the 4 single-piece `BannerBig` calls (`L294` `Art_C_BannerBig`, `L356` `Art_B_Banner`, `L494` `Art_K_BannerBig`, `L581` `Art_L_BannerBig`), pass `pal.MatBannerSway` instead of `mat`.
   - On `Art_F_ExitGlow` (`L587-602` in `BuildRoomELF`), assign `pal.MatEmissiveExit`, `Background/-6`, and isolate its `Light2D`:

```csharp
var exitGlowGo = CreateChild(artRoot, "Art_F_ExitGlow");
const float exitGlowScale = 0.55f;
exitGlowGo.transform.position = new Vector3(5f, 32f - pal.PortalBackOut.bounds.min.y * exitGlowScale, 0f);
exitGlowGo.transform.localScale = new Vector3(exitGlowScale, exitGlowScale, 1f);
var exitGlowSr = exitGlowGo.AddComponent<SpriteRenderer>();
exitGlowSr.sprite = pal.PortalBackOut;
exitGlowSr.sharedMaterial = pal.MatEmissiveExit;
exitGlowSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Background");
exitGlowSr.sortingOrder = -6;
exitGlowSr.color = new Color(1f, 0.92f, 0.65f, 0.45f);
var exitGlowLight = exitGlowGo.AddComponent<Light2D>();
exitGlowLight.lightType = Light2D.LightType.Point;
exitGlowLight.color = new Color(1f, 0.92f, 0.65f, 1f);
exitGlowLight.intensity = 1.65f;
exitGlowLight.pointLightOuterRadius = 7.5f;
exitGlowLight.pointLightInnerRadius = 2.0f;
ConfigureGameplayPointLight(exitGlowLight, enableVolumetric: true, volumeIntensity: 0.14f, addFlicker: true, phaseSeed: 5f);
```

   - Replace `AddStandingTorchBeacon`, `AddCeilingLanternBeacon`, `AddWindowWithGodray`, and `AddVerticalRoomBanner` (`L1720-1796`) with:

```csharp
private static void AddStandingTorchBeacon(GameObject parent, string name, Material mat, float x, float floorY, Color lightColor, float outerRadius, float intensity)
{
    BuildWallLanternBeacon(parent, name, "FlameLight", mat, x, floorY + 2.2f, lightColor, Mathf.Max(outerRadius, 8.0f), Mathf.Max(intensity, 1.65f));
}

private static void AddCeilingLanternBeacon(GameObject parent, string name, Material mat, float x, float ceilingBottomY, Color lightColor, float outerRadius, float intensity)
{
    BuildWallLanternBeacon(parent, name, "LanternLight", mat, x, ceilingBottomY - 1.35f, lightColor, Mathf.Max(outerRadius, 8.0f), Mathf.Max(intensity, 1.65f));
}

private static void BuildWallLanternBeacon(
    GameObject parent,
    string name,
    string lightChildName,
    Material litMat,
    float x,
    float flameY,
    Color lightColor,
    float outerRadius,
    float intensity)
{
    int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
    var lanternSprite = LoadSpriteAtPath(PrisonPropSpriteGenerator.WallLanternAssetPath);
    var fxGlowSprite = LoadSpriteAtPath("Assets/_Project/Art/VFX/ReferenceFeedback/fxGlowWhite.png");
    var flameMat = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Flame.mat");
    var particlesUnlitMat = LoadMaterialAtPath(ParticlesUnlitMatPath);

    var go = CreateChild(parent, name);
    go.transform.position = new Vector3(x, flameY, 0f);
    go.transform.localScale = Vector3.one;

    var fixtureGo = CreateChild(go, "LanternFixture_R5");
    fixtureGo.transform.localPosition = new Vector3(0f, PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY, 0f);
    fixtureGo.transform.localScale = Vector3.one;
    var fixtureSr = fixtureGo.AddComponent<SpriteRenderer>();
    fixtureSr.sprite = lanternSprite;
    fixtureSr.sharedMaterial = litMat;
    fixtureSr.sortingLayerID = bgLayerId;
    fixtureSr.sortingOrder = -9;
    fixtureSr.color = Color.white;

    var coreGo = CreateChild(go, "LanternFlameCore_R5");
    coreGo.transform.localPosition = Vector3.zero;
    coreGo.transform.localScale = new Vector3(0.085f, 0.12f, 1f);
    var coreSr = coreGo.AddComponent<SpriteRenderer>();
    coreSr.sprite = fxGlowSprite;
    coreSr.sharedMaterial = flameMat;
    coreSr.sortingLayerID = bgLayerId;
    coreSr.sortingOrder = -8;
    coreSr.color = new Color(1f, 1f, 1f, 0.82f);

    CreateTorchEmbers(go.transform, particlesUnlitMat, bgLayerId, -7);

    var lightGo = CreateChild(go, lightChildName);
    lightGo.transform.localPosition = Vector3.zero;
    var light = lightGo.AddComponent<Light2D>();
    light.lightType = Light2D.LightType.Point;
    light.color = lightColor;
    light.intensity = intensity;
    light.pointLightOuterRadius = outerRadius;
    light.pointLightInnerRadius = 1.8f;
    light.falloffIntensity = 0.42f;
    ConfigureGameplayPointLight(light, enableVolumetric: true, volumeIntensity: 0.14f, addFlicker: true, phaseSeed: x * 0.37f + flameY * 0.19f);
}

private static void CreateTorchEmbers(Transform parent, Material particlesUnlitMat, int sortingLayerId, int sortingOrder)
{
    var go = new GameObject("TorchEmbers");
    go.transform.SetParent(parent, false);
    go.transform.localPosition = Vector3.zero;

    var ps = go.AddComponent<ParticleSystem>();
    var main = ps.main;
    main.loop = true;
    main.playOnAwake = true;
    main.prewarm = true;
    main.simulationSpace = ParticleSystemSimulationSpace.World;
    main.maxParticles = 6;
    main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
    main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.10f);
    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.8f, 1.0f, 0.35f, 0.85f));
    main.gravityModifier = -0.15f;

    var emission = ps.emission;
    emission.enabled = true;
    emission.rateOverTime = 3.5f;

    var shape = ps.shape;
    shape.enabled = true;
    shape.shapeType = ParticleSystemShapeType.Circle;
    shape.radius = 0.22f;

    var colorOverLifetime = ps.colorOverLifetime;
    colorOverLifetime.enabled = true;
    var grad = new Gradient();
    grad.SetKeys(
        new[]
        {
            new GradientColorKey(new Color(1.0f, 0.78f, 0.30f), 0f),
            new GradientColorKey(new Color(1.0f, 0.38f, 0.12f), 1f),
        },
        new[]
        {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(0.95f, 0.2f),
            new GradientAlphaKey(0f, 1f),
        });
    colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

    var psr = go.GetComponent<ParticleSystemRenderer>();
    if (psr != null)
    {
        psr.sharedMaterial = particlesUnlitMat;
        psr.sortingLayerID = sortingLayerId;
        psr.sortingOrder = sortingOrder;
    }
}

private static void ConfigureGameplayPointLight(Light2D light, bool enableVolumetric, float volumeIntensity, bool addFlicker, float phaseSeed)
{
    int[] gameplayLayerIds =
    {
        SortingLayerSetup.GetSortingLayerId("Default"),
        SortingLayerSetup.GetSortingLayerId("Background"),
        SortingLayerSetup.GetSortingLayerId("Ground"),
        SortingLayerSetup.GetSortingLayerId("Actors"),
        SortingLayerSetup.GetSortingLayerId("VFX"),
    };
    SetLightSortingLayers(light, gameplayLayerIds);
    SetLightNormalMapQuality(light, Light2D.NormalMapQuality.Fast, 3.2f);

    if (enableVolumetric)
    {
        light.volumetricEnabled = true;
        light.volumeIntensity = volumeIntensity;
        var so = new SerializedObject(light);
        var volEnProp = so.FindProperty("m_LightVolumeEnabled");
        var volIntProp = so.FindProperty("m_LightVolumeIntensity");
        if (volEnProp != null) volEnProp.boolValue = true;
        if (volIntProp != null) volIntProp.floatValue = volumeIntensity;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    if (addFlicker)
    {
        if (!light.TryGetComponent<TorchFlicker2D>(out var flicker))
            flicker = light.gameObject.AddComponent<TorchFlicker2D>();
        flicker.Configure(light, light.intensity, light.pointLightOuterRadius, amplitude: 0.08f, speed: 3.2f, phaseOffset: phaseSeed);
        flicker.enabled = true;
    }
}

private static void SetLightSortingLayers(Light2D light, int[] layerIds)
{
    light.targetSortingLayers = layerIds;
    var so = new SerializedObject(light);
    var prop = so.FindProperty("m_ApplyToSortingLayers");
    if (prop != null && prop.isArray)
    {
        prop.arraySize = layerIds.Length;
        for (int i = 0; i < layerIds.Length; i++)
            prop.GetArrayElementAtIndex(i).intValue = layerIds[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}

private static void SetLightNormalMapQuality(Light2D light, Light2D.NormalMapQuality quality, float distance)
{
    var so = new SerializedObject(light);
    var qProp = so.FindProperty("m_NormalMapQuality");
    var dProp = so.FindProperty("m_NormalMapDistance");
    if (qProp != null) qProp.intValue = (int)quality;
    if (dProp != null) dProp.floatValue = distance;
    so.ApplyModifiedPropertiesWithoutUndo();
}

private static void AddWindowWithGodray(GameObject parent, string name, Material matLit, Material matUnlit, PrisonPalette pal, Vector2 pos)
{
    var winGo = CreateChild(parent, name);
    winGo.transform.position = new Vector3(pos.x, pos.y, 0f);
    var winSr = winGo.AddComponent<SpriteRenderer>();
    winSr.sprite = pal.WindowSmall;
    winSr.sharedMaterial = matLit;
    winSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Background");
    winSr.sortingOrder = -15;
    winSr.color = new Color(0.86f, 0.92f, 1f, 1f);

    var rayGo = CreateChild(winGo, "GodRay");
    rayGo.transform.localPosition = new Vector3(0.45f, -0.95f, 0f);
    rayGo.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
    rayGo.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
    var raySr = rayGo.AddComponent<SpriteRenderer>();
    raySr.sprite = pal.GodRay;
    raySr.sharedMaterial = pal.MatGodRay != null ? pal.MatGodRay : matUnlit;
    raySr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");
    raySr.sortingOrder = 10;
    raySr.color = Color.white;
}

private static void AddVerticalRoomBanner(GameObject parent, string name, PrisonPalette pal, Material mat, float x, float topY, int sortingOrder)
{
    var container = CreateChild(parent, name);
    var swayMat = pal.MatRoomBannerSway != null ? pal.MatRoomBannerSway : mat;
    const float scale = 1.0f;
    float y = topY;
    AddPrisonSpriteProp(container, "Top", pal.BannerRoomTop, swayMat, new Vector2(x, y - pal.BannerRoomTop.bounds.max.y * scale), scale, sortingOrder, Color.white);
    y -= pal.BannerRoomTop.bounds.size.y * scale;
    AddPrisonSpriteProp(container, "Mid", pal.BannerRoomTiled, swayMat, new Vector2(x, y - pal.BannerRoomTiled.bounds.max.y * scale), scale, sortingOrder, Color.white);
    y -= pal.BannerRoomTiled.bounds.size.y * scale;
    AddPrisonSpriteProp(container, "Bot", pal.BannerRoomBot, swayMat, new Vector2(x, y - pal.BannerRoomBot.bounds.max.y * scale), scale, sortingOrder, Color.white);

    var pin = container.AddComponent<VertexSwayPin2D>();
    pin.Configure(topY, 4.0f, true);
}
```

7. **Scene-Level 4-Tier Parallax, `GlowingMushroom_0..3`, `GroundMist_R5`, `ShadowCasters_R5`, Actor/Light Sorting Layers & GPU Capture (`BuildScene`, `L777-1053`):**
   - In `BuildScene`:
     - Set `globalLight.intensity = 0.60f; globalLight.color = new Color(0.84f, 0.91f, 0.96f, 1f);`.
     - Set `chestVisualSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");` (`L936`), `gateSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");` (`L985`), and in `BuildAmbientVfx` set `dustRenderer.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");` and `dripRenderer.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");`.
     - Right before `EditorSceneManager.MarkSceneDirty(scene);` (`L1050`), call `BuildRound5SceneAtmosphereAndLighting(cam.transform, spriteLit, pal, playerGo, enemiesRoot);`:

```csharp
private static void BuildRound5SceneAtmosphereAndLighting(
    Transform camTransform,
    Material spriteLit,
    PrisonPalette pal,
    GameObject playerGo,
    GameObject enemiesRoot)
{
    int defaultLayerId = SortingLayerSetup.GetSortingLayerId("Default");
    int parallaxFarLayerId = SortingLayerSetup.GetSortingLayerId("ParallaxFar");
    int parallaxMidLayerId = SortingLayerSetup.GetSortingLayerId("ParallaxMid");
    int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
    int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");
    int actorsLayerId = SortingLayerSetup.GetSortingLayerId("Actors");
    int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
    int fgLayerId = SortingLayerSetup.GetSortingLayerId("Foreground");

    int[] allLayerIds = { defaultLayerId, parallaxFarLayerId, parallaxMidLayerId, bgLayerId, groundLayerId, actorsLayerId, vfxLayerId, fgLayerId };
    int[] gameplayLayerIds = { defaultLayerId, bgLayerId, groundLayerId, actorsLayerId, vfxLayerId };

    // Assign Actors sorting layer to Player and all 5 Enemies
    foreach (var sr in playerGo.GetComponentsInChildren<SpriteRenderer>(true))
        sr.sortingLayerID = actorsLayerId;
    foreach (var sr in enemiesRoot.GetComponentsInChildren<SpriteRenderer>(true))
        sr.sortingLayerID = actorsLayerId;

    // 1. 4-Tier Hybrid Parallax container Prison_Parallax_R5
    var parallaxRoot = new GameObject("Prison_Parallax_R5");
    parallaxRoot.transform.position = Vector3.zero;

    var farGo = CreateChild(parallaxRoot, "ParallaxFar_Backdrop");
    var farParallax = farGo.AddComponent<ParallaxLayer2D>();
    farParallax.Configure(camTransform, new Vector2(0.15f, 0.06f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
    float[] farXs = { -32f, 0f, 32f };
    float[] farYs = { 4.0f, 25.3333f, 46.6667f };
    for (int ix = 0; ix < farXs.Length; ix++)
    {
        for (int iy = 0; iy < farYs.Length; iy++)
        {
            var panel = CreateArchFrameSprite(
                farGo,
                $"Panel_{ix}_{iy}",
                pal.ParallaxFarSprite,
                new Vector3(farXs[ix], farYs[iy], 0f),
                parallaxFarLayerId,
                0,
                pal.MatParallaxFar,
                Color.white);
            panel.transform.localPosition = new Vector3(farXs[ix], farYs[iy], 0f);
        }
    }

    var midGo = CreateChild(parallaxRoot, "ParallaxMid_Alcoves");
    var midParallax = midGo.AddComponent<ParallaxLayer2D>();
    midParallax.Configure(camTransform, new Vector2(0.38f, 0.14f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
    float[] chunkOffsets = { -32f, 0f, 32f };
    float[] alcoveAXs = { -13.3333f, -8f, -2.6667f, 2.6667f, 8f, 13.3333f };
    float[] tierYs = { 2.6f, 22.0f };
    float[] vineXs = { -9.5f, 1.5f, 11.5f };
    var vineTint = new Color(0.58f, 0.72f, 0.64f, 0.90f);
    for (int c = 0; c < chunkOffsets.Length; c++)
    {
        var chunkGo = CreateChild(midGo, $"Chunk_{c}");
        chunkGo.transform.localPosition = new Vector3(chunkOffsets[c], 0f, 0f);
        for (int t = 0; t < tierYs.Length; t++)
        {
            float ty = tierYs[t];
            for (int a = 0; a < alcoveAXs.Length; a++)
            {
                var spGo = CreateArchFrameSprite(chunkGo, $"AlcoveA_{t}_{a}", pal.AlcoveParallaxA, Vector3.zero, parallaxMidLayerId, 0, pal.MatParallaxMid, Color.white);
                spGo.transform.localPosition = new Vector3(alcoveAXs[a], ty, 0f);
            }
            for (int v = 0; v < vineXs.Length; v++)
            {
                var vineSp = (v % 2 == 0) ? pal.VineParallaxA : pal.VineParallaxB;
                var vGo = CreateArchFrameSprite(chunkGo, $"Vine_{t}_{v}", vineSp, Vector3.zero, parallaxMidLayerId, 2, pal.MatVineSway, vineTint);
                vGo.transform.localPosition = new Vector3(vineXs[v], ty + 3.6f, 0f);
            }
        }
    }

    var fgGo = CreateChild(parallaxRoot, "Foreground_Silhouettes");
    var fgParallax = fgGo.AddComponent<ParallaxLayer2D>();
    fgParallax.Configure(camTransform, new Vector2(1.18f, 1.0f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
    fgParallax.ConfigureVerticalCameraLock(true, 1.5f);
    var unlitMat = LoadMaterialAtPath(SpriteUnlitMatPath);
    var fgTint = new Color(6f / 255f, 10f / 255f, 16f / 255f, 0.96f);
    float[] fgAnchorXs = { -20f, -12f, -4f, 4f, 12f, 20f, 28f, 36f };
    for (int i = 0; i < fgAnchorXs.Length; i++)
    {
        float x = fgAnchorXs[i];
        var beamSprite = (i % 2 == 0) ? pal.BlackBeamTL : pal.BlackBeamTR;
        var bGo = CreateArchFrameSprite(fgGo, $"FgBeam_{i}", beamSprite, Vector3.zero, fgLayerId, 20, unlitMat, fgTint);
        bGo.transform.localPosition = new Vector3(x, 9.85f, 0f);

        var cGo = CreateArchFrameSprite(fgGo, $"FgChain_{i}", pal.CageChain, Vector3.zero, fgLayerId, 19, unlitMat, fgTint);
        cGo.transform.localPosition = new Vector3(x + 2.2f, 8.70f, 0f);
        cGo.transform.localScale = new Vector3(1.1f, 2.0f, 1f);
    }

    // 2. 4 Damp-Corner GlowingMushroom_0..3 + Cap-Aligned Additive MushroomHalo_0..3 + Isolated MushroomLight_0..3
    var dressingRoot = new GameObject("Prison_Dressing_R5");
    (float x, float floorTop)[] mushroomSpecs =
    {
        (43.5f, 0f),
        (58.5f, 0f),
        (-28.5f, 16f),
        (-14.5f, 24f),
    };
    for (int i = 0; i < mushroomSpecs.Length; i++)
    {
        float mx = mushroomSpecs[i].x;
        float floorTop = mushroomSpecs[i].floorTop;
        bool flipX = (i % 2) == 1;
        Vector3 mPos = new Vector3(mx, floorTop - pal.GlowingMushroomBright.bounds.min.y, 0f);
        var mGo = CreateArchFrameSprite(dressingRoot, $"GlowingMushroom_{i}", pal.GlowingMushroomBright, mPos, groundLayerId, -7, pal.MatEmissiveMushroom, Color.white);
        mGo.GetComponent<SpriteRenderer>().flipX = flipX;

        float budX = mx + (flipX ? 0.484f : -0.484f);
        float budY = floorTop + 0.410f;
        var haloGo = CreateArchFrameSprite(dressingRoot, $"MushroomHalo_{i}", pal.FxGlowWhite, new Vector3(budX, budY, 0f), groundLayerId, -8, pal.MatEmissiveMushroomHalo, new Color(1f, 1f, 1f, 0.24f));
        haloGo.transform.localScale = new Vector3(0.16f, 0.14f, 1f);

        var lightGo = CreateChild(dressingRoot, $"MushroomLight_{i}");
        lightGo.transform.position = new Vector3(budX, budY, 0f);
        var mLight = lightGo.AddComponent<Light2D>();
        mLight.lightType = Light2D.LightType.Point;
        mLight.color = new Color(0.17f, 0.90f, 0.84f, 1f);
        mLight.intensity = 0.72f;
        mLight.pointLightInnerRadius = 0.15f;
        mLight.pointLightOuterRadius = 2.1f;
        mLight.falloffIntensity = 0.55f;
        ConfigureGameplayPointLight(mLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f);
    }

    // 3. GroundMist_R5 across lower wells Room C, Room G, and Room K
    var mistRoot = new GameObject("GroundMist_R5");
    (string name, Vector3 pos, Vector3 scale)[] mistSpecs =
    {
        ("Mist_RoomC", new Vector3(49f, 1.0f, 0f), new Vector3(14f * 24f, 2.0f * 24f, 1f)),
        ("Mist_RoomG", new Vector3(66f, 1.0f, 0f), new Vector3(20f * 24f, 2.0f * 24f, 1f)),
        ("Mist_RoomK", new Vector3(-23f, 17.0f, 0f), new Vector3(14f * 24f, 2.0f * 24f, 1f)),
    };
    foreach (var spec in mistSpecs)
    {
        var strip = CreateArchFrameSprite(mistRoot, spec.name, pal.WhitePixel, spec.pos, vfxLayerId, -1, pal.MatGroundMist, Color.white);
        strip.transform.localScale = spec.scale;
    }

    // 4. ShadowCasters_R5 on [Background, Ground]
    var castersRoot = new GameObject("ShadowCasters_R5");
    int[] shadowTargetLayers = { bgLayerId, groundLayerId };
    CreateBoxShadowCaster(castersRoot.transform, "Caster_StairsH", new Vector3(15f, 11f, 0f), 6f, 6f, shadowTargetLayers);
    CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorG_Upper", new Vector3(60.5f, 7.5f, 0f), 9f, 1f, shadowTargetLayers);
    CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorF_CeilingE", new Vector3(11f, 31.5f, 0f), 22f, 1f, shadowTargetLayers);

    // 5. Ensure all scene lights (Global + Point) have strict sorting-layer masks and Fast NormalMapQuality
    foreach (var light in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
    {
        if (light.lightType == Light2D.LightType.Global)
        {
            SetLightSortingLayers(light, allLayerIds);
        }
        else if (light.lightType == Light2D.LightType.Point)
        {
            SetLightSortingLayers(light, gameplayLayerIds);
            SetLightNormalMapQuality(light, Light2D.NormalMapQuality.Fast, 3.2f);
        }
    }
}

private static void CreateBoxShadowCaster(Transform parent, string name, Vector3 worldCenter, float width, float height, int[] targetSortingLayerIds)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.position = worldCenter;

    var sc = go.AddComponent<ShadowCaster2D>();
    sc.castsShadows = true;
    sc.selfShadows = false;

    float hw = width * 0.5f;
    float hh = height * 0.5f;
    var shape = new[]
    {
        new Vector3(-hw, -hh, 0f),
        new Vector3(-hw, hh, 0f),
        new Vector3(hw, hh, 0f),
        new Vector3(hw, -hh, 0f),
    };

    int shapeHash;
    unchecked
    {
        shapeHash = (int)2166136261;
        for (int i = 0; i < shape.Length; i++)
            shapeHash = shapeHash * 16777619 ^ shape[i].GetHashCode();
    }

    var so = new SerializedObject(sc);
    so.FindProperty("m_ShadowCastingSource").intValue = 1;
    so.FindProperty("m_CastingOption").intValue = (int)ShadowCaster2D.ShadowCastingOptions.CastShadow;
    so.FindProperty("m_CastsShadows").boolValue = true;
    so.FindProperty("m_SelfShadows").boolValue = false;
    so.FindProperty("m_ShapePathHash").intValue = shapeHash;

    var pathProp = so.FindProperty("m_ShapePath");
    pathProp.arraySize = shape.Length;
    for (int i = 0; i < shape.Length; i++)
        pathProp.GetArrayElementAtIndex(i).vector3Value = shape[i];

    var layersProp = so.FindProperty("m_ApplyToSortingLayers");
    layersProp.arraySize = targetSortingLayerIds.Length;
    for (int i = 0; i < targetSortingLayerIds.Length; i++)
        layersProp.GetArrayElementAtIndex(i).intValue = targetSortingLayerIds[i];

    so.ApplyModifiedPropertiesWithoutUndo();
}
```

   - Add `CaptureRound5GpuVerificationFrames()` to `NhaGiamGiengChuongBuilder`:

```csharp
public static void CaptureRound5GpuVerificationFrames()
{
    string outDir = Environment.GetEnvironmentVariable("R5_GPU_CAPTURE_DIR");
    if (string.IsNullOrWhiteSpace(outDir))
        outDir = Path.GetFullPath("Logs/r5-gpu-captures");
    Directory.CreateDirectory(outDir);

    string[] r5ShaderNames =
    {
        "DeadCells/PaletteSwapLit2D",
        "DeadCells/MobFleshSwapLit2D",
        "DeadCells/ParallaxGradientMap2D",
        "DeadCells/EmissiveHdr2D",
        "DeadCells/AdditiveGodRay2D",
        "DeadCells/VertexSway2D",
        "DeadCells/GroundMist2D",
    };

    int shaderErrors = 0;
    foreach (string shaderName in r5ShaderNames)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
        {
            shaderErrors++;
            Debug.LogError($"[R5_NG_GPU_VERIFY] Shader error or missing: {shaderName}");
        }
    }
    Debug.Log($"[R5_NG_GPU_VERIFY] gfxDevice={SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) shaderErrors={shaderErrors}");

    var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    if (!scene.IsValid())
        throw new InvalidOperationException("Failed to open NhaGiamGiengChuong.unity for GPU verification.");

    var urpAsset = UniversalRenderPipeline.asset;
    var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
    if (cam == null)
        throw new InvalidOperationException("Main Camera missing in NhaGiamGiengChuong.unity.");

    var uacd = cam.GetComponent<UniversalAdditionalCameraData>();
    var globalVol = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
        .FirstOrDefault(v => v.enabled && v.isGlobal);

    bool hdrOk = urpAsset != null && urpAsset.supportsHDR;
    bool camPostOk = uacd != null && uacd.renderPostProcessing;
    bool volMaskOk = uacd != null && globalVol != null && (uacd.volumeLayerMask & (1 << globalVol.gameObject.layer)) != 0;
    bool profileOk = globalVol != null && globalVol.sharedProfile != null &&
                     globalVol.sharedProfile.TryGet<Bloom>(out var b) && b.intensity.overrideState &&
                     globalVol.sharedProfile.TryGet<Vignette>(out var v) && v.intensity.overrideState &&
                     globalVol.sharedProfile.TryGet<Tonemapping>(out var tm) && tm.mode.overrideState &&
                     globalVol.sharedProfile.TryGet<ColorAdjustments>(out var ca) && ca.contrast.overrideState && ca.saturation.overrideState &&
                     globalVol.sharedProfile.TryGet<SplitToning>(out var st) && st.shadows.overrideState && st.highlights.overrideState;

    Debug.Log($"[R5_NG_GPU_VERIFY] URP_PreFlight urpActive={urpAsset != null} hdr={hdrOk} camPost={camPostOk} volMask={volMaskOk} profileAll5={profileOk}");

    int defaultMatCount = UnityEngine.Object.FindObjectsByType<Renderer>()
        .Count(r => r.sharedMaterial != null && r.sharedMaterial.name == "Default-Material");
    Debug.Log($"[R5_NG_GPU_VERIFY] PaletteScan defaultMaterialRenderers={defaultMatCount}");

    foreach (var pin in UnityEngine.Object.FindObjectsByType<VertexSwayPin2D>(FindObjectsInactive.Include))
        pin.ApplyPropertyBlock();

    const System.Reflection.BindingFlags NonPublicInstance =
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var updateMeshMethod = typeof(Light2D).GetMethod("UpdateMesh", NonPublicInstance);
    var updateBoundsMethod = typeof(Light2D).GetMethod("UpdateBoundingSphere", NonPublicInstance);
    foreach (var light in UnityEngine.Object.FindObjectsByType<Light2D>())
    {
        if (light != null && light.isActiveAndEnabled && light.lightType != Light2D.LightType.Global)
        {
            updateMeshMethod?.Invoke(light, new object[] { true });
            updateBoundsMethod?.Invoke(light, null);
        }
    }
    foreach (var sc in UnityEngine.Object.FindObjectsByType<ShadowCaster2D>())
    {
        if (sc != null && sc.isActiveAndEnabled)
            sc.Update();
    }

    var parallaxLayers = UnityEngine.Object.FindObjectsByType<ParallaxLayer2D>(FindObjectsInactive.Include);
    var origParallaxPositions = parallaxLayers.Select(l => l != null ? l.transform.position : Vector3.zero).ToArray();
    var origPos = cam.transform.position;
    float origOrtho = cam.orthographicSize;
    var origTarget = cam.targetTexture;

    const int width = 1280;
    const int height = 720;
    var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
    rt.antiAliasing = 1;
    rt.Create();
    var readTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false, linear: true);
    var outTex = new Texture2D(width, height, TextureFormat.RGB24, false, linear: false);

    try
    {
        cam.targetTexture = rt;

        cam.transform.position = new Vector3(20f, 3.0f, -10f);
        cam.orthographicSize = 5.8f;
        foreach (var layer in parallaxLayers)
            if (layer != null && layer.isActiveAndEnabled) layer.UpdateParallax(cam.transform.position);
        cam.Render();

        var shots = new (string file, Vector3 pos, float orthoSize)[]
        {
            ("gpu_r5_ng_01_room_ab.png", new Vector3(20f, 3.0f, -10f), 5.8f),
            ("gpu_r5_ng_02_room_cd.png", new Vector3(45f, 6.5f, -10f), 6.5f),
            ("gpu_r5_ng_03_room_g.png", new Vector3(66f, 5.5f, -10f), 6.2f),
            ("gpu_r5_ng_04_room_hjk.png", new Vector3(-8f, 19.5f, -10f), 6.0f),
            ("gpu_r5_ng_05_room_elf.png", new Vector3(8f, 29.5f, -10f), 6.5f),
        };

        foreach (var shot in shots)
        {
            cam.transform.position = shot.pos;
            cam.orthographicSize = shot.orthoSize;
            foreach (var layer in parallaxLayers)
                if (layer != null && layer.isActiveAndEnabled) layer.UpdateParallax(shot.pos);
            cam.Render();

            RenderTexture.active = rt;
            readTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readTex.Apply(false, false);
            RenderTexture.active = null;

            Color[] linearPixels = readTex.GetPixels();
            var srgbPixels = new Color32[linearPixels.Length];
            for (int i = 0; i < linearPixels.Length; i++)
            {
                Color lin = linearPixels[i];
                byte cr = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.r))) * 255f);
                byte cg = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.g))) * 255f);
                byte cb = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.b))) * 255f);
                srgbPixels[i] = new Color32(cr, cg, cb, 255);
            }
            outTex.SetPixels32(srgbPixels);
            outTex.Apply(false, false);

            File.WriteAllBytes(Path.Combine(outDir, shot.file), outTex.EncodeToPNG());
            Debug.Log($"[R5_NG_GPU_VERIFY] Captured {shot.file} ({width}x{height}) at cam={shot.pos} ortho={shot.orthoSize}");
        }
    }
    finally
    {
        RenderTexture.active = null;
        cam.targetTexture = origTarget;
        cam.transform.position = origPos;
        cam.orthographicSize = origOrtho;
        for (int i = 0; i < parallaxLayers.Length; i++)
            if (parallaxLayers[i] != null) parallaxLayers[i].transform.position = origParallaxPositions[i];
        UnityEngine.Object.DestroyImmediate(outTex);
        UnityEngine.Object.DestroyImmediate(readTex);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
    }
}
```

- [ ] **Step 4: Run `NhaGiamGiengChuongBuilder.BuildAll` and verify all `27/27` NG EditMode tests pass**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -quit -logFile D:/thietkegame/Logs/r5-ng-build.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames "NhaGiamGiengChuong.Tests.EditMode" -testResults D:/thietkegame/Logs/r5-t3-green.xml -logFile D:/thietkegame/Logs/r5-t3-green.log
```
Expected: `total="27" passed="27" failed="0"` (`A01`–`A11` + `U01`–`U16`), `0` warnings.

---

### Task 4: Full Regression Suite (`62/62`), Real RTX 2050 GPU Captures Across Both Scenes & Repo Hygiene

**Files:**
- Verify (read-only): `Logs/r5-{mainlevel-remediate,ng-build,core-green,ng-edit-regression,ng-play-regression,gpu-mainlevel,gpu-ng}.log`, `Logs/r5-{core-green,ng-edit-regression,ng-play-regression}.xml`
- Restore churn only: `ProjectSettings/TimeManager.asset`, `ProjectSettings/ProjectSettings.asset`

**Interfaces:**
- Consumes: `MainLevelSceneRemediator.CaptureRound4GpuVerificationFrames()`, `NhaGiamGiengChuongBuilder.CaptureRound5GpuVerificationFrames()`.
- Produces: Fresh `62/62` green test XMLs (`28` Core EditMode + `27` NG EditMode + `7` NG PlayMode), `9` real RTX 2050 GPU frames (`4` `Main_Level` + `5` `NhaGiamGiengChuong`), and verified pixel metrics.

- [ ] **Step 1: Run all 3 test suites sequentially (single Unity process lock)**

Run:
```powershell
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames "Core.Tests.EditMode" -testResults D:/thietkegame/Logs/r5-core-green.xml -logFile D:/thietkegame/Logs/r5-core-green.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 -projectPath D:/thietkegame -runTests -testPlatform EditMode -assemblyNames "NhaGiamGiengChuong.Tests.EditMode" -testResults D:/thietkegame/Logs/r5-ng-edit-regression.xml -logFile D:/thietkegame/Logs/r5-ng-edit-regression.log
$env:DOTNET_GCServer='0'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -nographics -job-worker-count 1 --burst-disable-compilation -projectPath D:/thietkegame -runTests -testPlatform PlayMode -assemblyNames "NhaGiamGiengChuong.Tests.PlayMode" -testResults D:/thietkegame/Logs/r5-ng-play-regression.xml -logFile D:/thietkegame/Logs/r5-ng-play-regression.log
```
Expected: `28/28` (`Core.Tests.EditMode`) + `27/27` (`NG EditMode`) + `7/7` (`NG PlayMode`) = **`62/62` Passed, `0` Failed**.

- [ ] **Step 2: Capture real RTX 2050 GPU verification frames for both `Main_Level.unity` (`4` frames) and `NhaGiamGiengChuong.unity` (`5` frames)**

Run WITHOUT `-nographics`:
```powershell
$env:DOTNET_GCServer='0'; $env:R4_GPU_CAPTURE_PREFIX='gpu_r5_ml'; $env:R4_GPU_CAPTURE_DIR='<ARTIFACT_DIR>'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -job-worker-count 1 --burst-disable-compilation -projectPath D:/thietkegame -executeMethod DeadCells.Tools.Editor.MainLevelSceneRemediator.CaptureRound4GpuVerificationFrames -quit -logFile D:/thietkegame/Logs/r5-gpu-mainlevel.log
$env:DOTNET_GCServer='0'; $env:R5_GPU_CAPTURE_DIR='<ARTIFACT_DIR>'; rtk proxy D:/Unity/6000.6.9f1/Editor/Unity.exe -batchmode -job-worker-count 1 --burst-disable-compilation -projectPath D:/thietkegame -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.CaptureRound5GpuVerificationFrames -quit -logFile D:/thietkegame/Logs/r5-gpu-ng.log
```
Expected in both GPU logs: `gfxDevice=NVIDIA GeForce RTX 2050 (Direct3D12) shaderErrors=0`, `URP_PreFlight 5/5 True`, `defaultMaterialRenderers=0`.

- [ ] **Step 3: Visually inspect all `9/9` GPU frames + `2×–3×` crops and restore `ProjectSettings` churn**

Run:
```powershell
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
rtk git diff --stat -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset ProjectSettings/TagManager.asset ProjectSettings/EditorBuildSettings.asset
```
Verify:
- `Main_Level` frames show brighter midtones (`meanLum` lifted `+25%–40%` over R4.2 without highlight clipping `maxCh>=240 <= 0.5%`), chiseled stone jambs framing vertical arch cuts (`0` paper-cut brick edges), and `0` regression on lanterns/mushrooms/banners.
- `NhaGiamGiengChuong` frames (`AB`, `CD`, `G`, `HJK`, `ELF`) show `1:1` `24 PPU` wall sconce lanterns (`0` dangling chains), seamless multi-height `RoomBanner`s (`0px` seam jump), framed arch bays revealing `ParallaxFar/Mid`, and subtle cyan mushroom bounce + ground mist in the lower wells.
- `0` `CS0618`, `0` `CS0414`, `0` `warning CS`, `0` `error CS`, `0` `Shader error` across all logs, and `0` running `Unity.exe` processes.
