# Round 6: Dead Cells *Prisoners' Quarters* Full-Scale Expansion (`NhaGiamGiengChuong` v8) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` from its v7 vertical slice (`107u × 40u`, `11` rooms in `5` prefabs, `5` enemies, `4` zones) into a full-scale **Dead Cells — *Prisoners' Quarters*** biome (**`337.0u × 80.0u`** physical collider envelope, **`19` rooms (`A`–`T`)** across **`9` modular Room Prefabs**, **`~818u` walkable surface** across `568u` solid floors + `31` one-way platforms + `Stairs_H`, **`30` enemies** across **`11` `EncounterZone`s** covering all **5 enemy archetypes**, **`2` Scrolls of Power**, **live Gold Economy**, **`6` Teleportation Stones (`3` bi-directional pairs)**, **`3` Copper Switch Doors**, **`1` Alchemy Lore Study**, and **`2` Biome Exits**: *Promenade of the Condemned* and *Toxic Sewers*) while keeping all existing room prefab tests (`A01`–`A05`), unit tests (`U01`–`U16`), core PlayMode tests (`P01`–`P06`), and Core EditMode tests (`29/29`) 100% green.

**Architecture:** Non-destructive modular Spine-and-Wings expansion. The 5 existing v7 Room Prefabs (`AB`, `CD`, `G`, `HJK`, `ELF` covering rooms `A..L`) stay at their exact v7 world coordinates (`[-30.5, 76.5] × [-1, 39]`), with `5` clean `4.0u` doorway openings carved into the outer boundary walls of `Room G` (`x=76, y=0..4` & `y=8..12`), `Room K` (`x=-30, y=16..20` & `y=24..28`), and `Room L` (`x=38, y=24..28`) while keeping `(76.25f, 5.0f)` (`Wall_GM_Mid_76` `[76, 76.5] × [4, 7]`) and `(-0.25f, 2.5f)` (`Wall_A_West` `[-0.5, 0] × [-1, 5.5]`) solid so `A01`–`A05` and `A08` pass unmodified. Four new modular Wing Prefabs (`W1_EastCourtyard.prefab`, `W2_SewerCistern.prefab`, `W3_WestBastion.prefab`, `U1_UpperPromenade.prefab` covering 8 new rooms `M..T`) dock onto those doorways. Primary scene singletons (`I3_HealingChest`, `I4_EncounterGate`, `I5_ExitTrigger`) remain the sole instances of their MonoBehaviour types so `FindAnyObjectByType` in `A07/P03..P07` has zero ambiguity, while new wing interactions use dedicated unit-tested components (`SwitchDoor`, `TeleportGate`, `LoreTablet`, `SecondaryBiomeExitTrigger`).

**Tech Stack:** Unity `6000.6.9f1` (URP 2D `17.6.0`, Input System `1.19.0`, NUnit Test Framework `1.8.0`), C# 9, `24 PPU` Dead Cells Prison tiles/sprites/normals, custom URP 2D shaders (`PaletteSwapLit2D`, `MobFleshSwapLit2D`, `ParallaxGradientMap2D`, `EmissiveHdr2D`, `AdditiveGodRay2D`, `VertexSway2D`, `GroundMist2D`), `rtk` CLI wrapper.

**Spec:** `D:/thietkegame/docs/superpowers/specs/2026-11-21-prisoners-quarters-full-scale-expansion-design.md`

---

## Global Constraints (`AGENTS.md` §§0–2, §6, §9, §10)

1. **In-Place Execution on `map-feat-designing`:**
   - Execute in-place on branch `map-feat-designing` in `D:/thietkegame`. Do **NOT** create new git worktrees or commits unless explicitly asked by the user.
2. **Forbidden Modification Scope:**
   - **Zero edits** to `.fortress-workspaces/antigravity`, `Assets/_Project/Features/Player/**`, `Assets/_Project/Features/Enemies/**`, `Assets/_Project/Scenes/Main_Level.unity`, `Assets/_Project/Scenes/Sandbox_Player.unity`, `LevelRunController.cs`, `ResultPanel.cs`, or `.meta` files by hand.
   - `Assets/_Project/Features/Currency/Scripts/GoldPickup.cs` and `Assets/_Project/Features/Currency/Prefabs/GoldPickup.prefab` are explicitly in-scope for the `Default` → `VFX` sorting-layer promotion so dropped coins render above `TM_Background`.
3. **Assembly Definition (`.asmdef`) Boundaries:**
   - Add `"Currency"` to `NhaGiamGiengChuong.Editor.asmdef`, `NhaGiamGiengChuong.Tests.EditMode.asmdef`, and `NhaGiamGiengChuong.Tests.PlayMode.asmdef`.
   - Add `"HUD"` to `NhaGiamGiengChuong.Tests.PlayMode.asmdef`.
   - Keep `Core`, `VFX`, `CameraSystem`, `Combat`, `Enemies`, `Player`, `Currency`, `HUD`, `RunFlow`, and `NhaGiamGiengChuong.asmdef` untouched.
4. **Single-Unity-Process Lock & Hygiene:**
   - Run all shell commands via `rtk` / `rtk proxy`.
   - Confirm `Get-Process Unity -ErrorAction SilentlyContinue` is empty before any `Unity.exe` invocation; pass `$env:DOTNET_GCServer='0'` and `-job-worker-count 1`.
   - After any Unity batch run, restore `ProjectSettings/TimeManager.asset` and `ProjectSettings/ProjectSettings.asset` via `rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset`.
   - Zero `CS0618`, `CS0414`, `warning CS`, `error CS`, `Shader error`, `Assertion failed`.

---

## Review Focus

1. **Task 1 (`SwitchDoor`, `TeleportGate`, `LoreTablet`, `SecondaryBiomeExitTrigger`, `GoldPickup` sorting):**
   - `TeleportGate` must enforce proximity discovery (`IsDiscovered`) so `CanTeleport` is true **only when both `this.IsDiscovered && _pairedGate.IsDiscovered`** (`TP_0_Spawn` and `TP_2_CisternJunction` cannot warp into locked `SwitchDoor` vaults `TP_1_EastVault` and `TP_3_SewerExit` until the destination gate is discovered) and must enforce a `0.25f` shared unscaled timestamp guard so a single button press never bounces back.
   - `SecondaryBiomeExitTrigger` must require both `(_switchDoor == null || _switchDoor.IsOpen)` AND both assigned Room P guardians (`_firstGuardian`, `_secondGuardian`) defeated before calling `_run.RequestWin(frame)`.
2. **Task 2 (`W1_EastCourtyard`, `W2_SewerCistern`, `W3_WestBastion`, `U1_UpperPromenade` & Core Spine Doorways):**
   - Carving doorways in `G.prefab` (`x=76, y=0..4` & `y=8..12`), `HJK.prefab` (`x=-30, y=16..20` & `y=24..28`), and `ELF.prefab` (`x=38, y=24..28`) must preserve `(76.25f, 5.0f)` (`Wall_GM_Mid_76` `[76, 76.5] × [4, 7]`) so `A01`–`A05` and `A08` pass unmodified.
   - Sub-floor `PaintVisualDirtSkirt` bounds must strictly avoid painting inside `Room O` (`x ∈ [76, 164], y ∈ [-16, -1]`) or `Room C` (`x ∈ [42, 56], y ∈ [0, 18.5]`).
   - All `21` new one-way platforms (`31` total: `10` core + `7` in `W1` + `5` in `W2` + `4` in `W3` + `5` in `U1`) must set `PlatformEffector2D.surfaceArc = 160f`, `rotationalOffset = 0f`.
3. **Task 3 (Full-Scale Scene Assembly & `A06`–`A13`):**
   - Additive scene cloning from `Main_Level.unity` (`Scroll of Power - Entrance`, `Power Scroll HUD`, `Scroll cinematic`, `Gold HUD`) must close `Main_Level.unity` without saving or dirtying it (`0` diff on `Main_Level.unity`).
   - All `9` carved arch bays (`#0..#8`) must have `0` AABB overlap with any grounded landmark or vertical `RoomBanner` (`A12`).
   - All `19` rooms must achieve `>= 50%` point-light coverage and `>= 75%` total map coverage (`A10`), with all volumetric point lights having `volumeIntensity <= 0.10f` and `intensity <= 1.50f` (`A12`).

---

## File Structure Map

### Modified Files
- `Assets/_Project/Features/Currency/Scripts/GoldPickup.cs` — Promote `Default` (`0`) sorting-layer `SpriteRenderer`s to `VFX` (`order = 5`) in `Awake()` + public `EnsureVfxSortingLayer()`.
- `Assets/_Project/Features/Currency/Prefabs/GoldPickup.prefab` — Updated by builder so `Coin` child `SpriteRenderer` uses `VFX` sorting layer (`order = 5`).
- `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef` — Add `"Currency"` reference.
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/NhaGiamGiengChuong.Tests.EditMode.asmdef` — Add `"Currency"` reference.
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/NhaGiamGiengChuong.Tests.PlayMode.asmdef` — Add `"Currency"` and `"HUD"` references.
- `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` — Carve `5` wing-docking doorways in `BuildRoomG/HJK/ELF`, add `BuildRoomW1_EastCourtyard`, `BuildRoomW2_SewerCistern`, `BuildRoomW3_WestBastion`, `BuildRoomU1_UpperPromenade`, expand `BuildScene` (`9` room prefabs, `30` enemies, `11` zones, `Projectile_Grenade` prewarm, `3` `SwitchDoor`s, `6` `TeleportGate`s, `1` `LoreTablet`, `1` `SecondaryBiomeExitTrigger`, `2` `PowerScrollPickup`s + `Power Scroll HUD` + `Scroll cinematic`, `Run Gold` + `Gold HUD`, expanded `340u × 86u` 4-tier parallax, `8` mushrooms, `6` mist strips, `6` shadow casters), and add `CaptureRound6GpuVerificationFrames()`.
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapGeometryTests.cs` — Update `A06` to verify full-scale `[-102.5, 234.5] × [-17, 63]` envelope, `7` floor unions (`T0..T6`), `17` openings + `3` `SwitchDoor` barriers, and `7` climbable chains (`I1, I2, I6..I10`).
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` — Update `A07, A08, A10, A11, A12` to full-scale `9`-prefab / `19`-room / `30`-enemy / `11`-zone counts and add `A13_FullScalePrisonersQuarters_ScrollsGoldTeleportersSwitchDoorsLoreAndDualExits`.
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/RunPlayTests.cs` — Update `P07` replay reset count assertions from `5` enemies / `4` zones to `30` enemies / `11` zones.
- `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{AB,CD,G,HJK,ELF}.prefab` & `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` — Rebuilt deterministically by `NhaGiamGiengChuongBuilder.BuildAll()`.

### Created Files
- `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SwitchDoor.cs`
- `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/TeleportGate.cs`
- `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/LoreTablet.cs`
- `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SecondaryBiomeExitTrigger.cs`
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/SwitchDoorTests.cs` (`U17`)
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/TeleportGateTests.cs` (`U18`)
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/LoreTabletTests.cs` (`U19`)
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/SecondaryBiomeExitTests.cs` (`U20`)
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/WingGeometryTests.cs` (`A05b`–`A05e`)
- `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/ExpansionPlayTests.cs` (`P08`)
- `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{W1_EastCourtyard,W2_SewerCistern,W3_WestBastion,U1_UpperPromenade}.prefab` (generated by `NhaGiamGiengChuongBuilder.BuildAll()`)

---

## Task 1: Assembly References, `GoldPickup` Sorting Guard & New Runtime Interactive Components (`SwitchDoor`, `TeleportGate`, `LoreTablet`, `SecondaryBiomeExitTrigger`) + Unit Tests `U17`–`U20`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/NhaGiamGiengChuong.Tests.EditMode.asmdef`
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/NhaGiamGiengChuong.Tests.PlayMode.asmdef`
- Modify: `Assets/_Project/Features/Currency/Scripts/GoldPickup.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SwitchDoor.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/TeleportGate.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/LoreTablet.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SecondaryBiomeExitTrigger.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/SwitchDoorTests.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/TeleportGateTests.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/LoreTabletTests.cs`
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/SecondaryBiomeExitTests.cs`

**Interfaces:**
- Consumes: `LevelRunController`, `EncounterZone.GetColliderFeet(BoxCollider2D)`, `Health`, `PlayerController`, `EnemyController`, `InputActionAsset`.
- Produces:
  - `DeadCells.NhaGiamGiengChuong.SwitchDoor`
  - `DeadCells.NhaGiamGiengChuong.TeleportGate`
  - `DeadCells.NhaGiamGiengChuong.LoreTablet`
  - `DeadCells.NhaGiamGiengChuong.SecondaryBiomeExitTrigger`
  - `DeadCells.Currency.GoldPickup.EnsureVfxSortingLayer()`

- [ ] **Step 1: Update the 3 `.asmdef` files and write the 4 failing unit test files (`U17`–`U20`)**

Update `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuong.Editor.asmdef` by preserving all existing fields and adding `"Currency"` to `"references"`:
```json
{
    "name": "NhaGiamGiengChuong.Editor",
    "rootNamespace": "DeadCells.NhaGiamGiengChuong.Editor",
    "references": [
        "NhaGiamGiengChuong",
        "Core",
        "Combat",
        "Currency",
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
    "includePlatforms": [
        "Editor"
    ],
    "autoReferenced": false
}
```

Update `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/NhaGiamGiengChuong.Tests.EditMode.asmdef` by preserving all existing fields and adding `"Currency"` to `"references"`:
```json
{
    "name": "NhaGiamGiengChuong.Tests.EditMode",
    "rootNamespace": "DeadCells.NhaGiamGiengChuong.Tests",
    "references": [
        "NhaGiamGiengChuong",
        "Core",
        "Combat",
        "Currency",
        "Enemies",
        "Player",
        "HUD",
        "CameraSystem",
        "VFX",
        "Unity.InputSystem",
        "UnityEngine.UI",
        "Unity.RenderPipelines.Core.Runtime",
        "Unity.RenderPipelines.Universal.Runtime",
        "Unity.RenderPipelines.Universal.2D.Runtime",
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

Update `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/NhaGiamGiengChuong.Tests.PlayMode.asmdef` by preserving all existing fields and adding `"Currency"` and `"HUD"` to `"references"`:
```json
{
    "name": "NhaGiamGiengChuong.Tests.PlayMode",
    "rootNamespace": "DeadCells.NhaGiamGiengChuong.Tests",
    "references": [
        "NhaGiamGiengChuong",
        "Core",
        "Combat",
        "Currency",
        "Enemies",
        "Player",
        "HUD",
        "CameraSystem",
        "VFX",
        "Unity.InputSystem",
        "Unity.InputSystem.TestFramework",
        "UnityEngine.UI",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
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

Create `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/SwitchDoorTests.cs` (`U17`):
```csharp
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class SwitchDoorTests
    {
        private readonly List<GameObject> _created = new();

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            Time.timeScale = 1f;
        }

        [Test]
        public void U17_SwitchDoor_RejectsOutsideDeadOrEnded_AndOpensBarrierAtomicallyOnceWhenAliveInsideTrigger()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(144f, 0f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);

            var doorGo = Track(new GameObject("SD_1_EastVault"));
            doorGo.transform.position = new Vector3(144f, 0f, 0f);
            var trigger = doorGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 0.75f);
            trigger.size = new Vector2(2.5f, 1.5f);

            var barrierGo = Track(new GameObject("DoorBarrier"));
            barrierGo.transform.SetParent(doorGo.transform, false);
            var barrierCol = barrierGo.AddComponent<BoxCollider2D>();
            var barrierSr = barrierGo.AddComponent<SpriteRenderer>();

            var switchVisualGo = Track(new GameObject("SwitchVisual"));
            switchVisualGo.transform.SetParent(doorGo.transform, false);
            var switchSr = switchVisualGo.AddComponent<SpriteRenderer>();
            var offSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);
            var onSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);

            var promptGo = Track(new GameObject("PromptIndicator"));
            promptGo.transform.SetParent(doorGo.transform, false);

            var sd = doorGo.AddComponent<SwitchDoor>();
            sd.Configure("SD_1", run, null, trigger, barrierGo, switchSr, offSprite, onSprite, null, promptGo, health, playerCol);

            int openEvents = 0;
            bool barrierOffAtEvent = false;
            sd.Opened += _ =>
            {
                openEvents++;
                barrierOffAtEvent = !barrierCol.gameObject.activeInHierarchy && !barrierSr.gameObject.activeInHierarchy;
            };

            // 1. Intro phase -> prompt hidden, interact rejected.
            sd.RefreshVisualState();
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(sd.TryInteract(), "[U17/Intro] Must reject before Playing.");
            Assert.IsFalse(sd.IsOpen);
            Assert.IsTrue(barrierGo.activeSelf);
            Assert.AreSame(offSprite, switchSr.sprite);

            // Enter Playing phase.
            run.TickIntro(new DummyPlayState(), 0f);
            sd.RefreshVisualState();
            Assert.IsTrue(sd.PromptVisible, "[U17/InTrigger] Prompt must show when alive and inside trigger.");

            // 2. Outside trigger -> rejected.
            playerGo.transform.position = new Vector3(130f, 0f, 0f);
            Physics2D.SyncTransforms();
            sd.RefreshVisualState();
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(sd.TryInteract(), "[U17/Outside] Must reject outside lever trigger.");
            Assert.IsFalse(sd.IsOpen);

            // 3. Return inside trigger and interact -> opens atomically once, swaps sprite, hides prompt.
            playerGo.transform.position = new Vector3(144f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.IsTrue(sd.TryInteract(), "[U17/ValidOpen] Expected true.");
            Assert.IsTrue(sd.IsOpen);
            Assert.AreEqual(1, openEvents);
            Assert.IsTrue(barrierOffAtEvent, "[U17/Atomic] Barrier collider and renderer must disable atomically before event.");
            Assert.IsFalse(barrierGo.activeSelf);
            Assert.AreSame(onSprite, switchSr.sprite);
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(promptGo.activeSelf);

            // 4. Repeat interact -> rejected, remains open.
            Assert.IsFalse(sd.TryInteract(), "[U17/Repeat] Repeat press must return false.");
            Assert.AreEqual(1, openEvents);
            Assert.IsTrue(sd.IsOpen);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
```

Create `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/TeleportGateTests.cs` (`U18`):
```csharp
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Currency;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class TeleportGateTests
    {
        private readonly List<GameObject> _created = new();

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            Time.timeScale = 1f;
        }

        [Test]
        public void U18_TeleportGate_RequiresBothGatesDiscovered_WarpsFeetToPairedGate_EnforcesCooldownAndPromotesGoldPickupSorting()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(15.5f, 0f, 0f);
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var inactiveSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);
            var activeSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);

            var gateAGo = Track(new GameObject("TP_0_Spawn"));
            gateAGo.transform.position = new Vector3(15.5f, 0f, 0f);
            var trigA = gateAGo.AddComponent<BoxCollider2D>();
            trigA.isTrigger = true;
            trigA.offset = new Vector2(0f, 1.0f);
            trigA.size = new Vector2(2.5f, 2.0f);
            var srA = gateAGo.AddComponent<SpriteRenderer>();
            var promptA = Track(new GameObject("PromptA"));

            var gateBGo = Track(new GameObject("TP_1_EastVault"));
            gateBGo.transform.position = new Vector3(161f, 0f, 0f);
            var trigB = gateBGo.AddComponent<BoxCollider2D>();
            trigB.isTrigger = true;
            trigB.offset = new Vector2(0f, 1.0f);
            trigB.size = new Vector2(2.5f, 2.0f);
            var srB = gateBGo.AddComponent<SpriteRenderer>();
            var promptB = Track(new GameObject("PromptB"));

            var gateA = gateAGo.AddComponent<TeleportGate>();
            var gateB = gateBGo.AddComponent<TeleportGate>();
            gateA.Configure("TP_0", run, null, trigA, gateB, true, Vector2.zero, null, promptA, srA, inactiveSp, activeSp, health, playerCol);
            gateB.Configure("TP_1", run, null, trigB, gateA, false, Vector2.zero, null, promptB, srB, inactiveSp, activeSp, health, playerCol);

            // 1. Gate A is discovered, Gate B is undiscovered -> CanTeleport is false, warp rejected!
            gateA.RefreshVisualState();
            Assert.IsTrue(gateA.IsDiscovered);
            Assert.IsFalse(gateB.IsDiscovered);
            Assert.IsFalse(gateA.CanTeleport, "[U18/UndiscoveredTarget] Cannot teleport to undiscovered vault gate.");
            Assert.IsFalse(gateA.PromptVisible);
            Assert.IsFalse(gateA.TryTeleport(10f), "[U18/UndiscoveredReject]");
            Assert.AreEqual(15.5f, EncounterZone.GetColliderFeet(playerCol).x, 1e-3f);

            // 2. Player physically reaches Gate B at (161, 0) -> RefreshVisualState auto-discovers Gate B!
            playerGo.transform.position = new Vector3(161f, 0f, 0f);
            Physics2D.SyncTransforms();
            gateB.RefreshVisualState();
            Assert.IsTrue(gateB.IsDiscovered, "[U18/AutoDiscover] Entering Gate B trigger must discover it.");
            Assert.AreSame(activeSp, srB.sprite);
            Assert.IsTrue(gateB.CanTeleport);
            Assert.IsTrue(gateA.CanTeleport);

            // 3. Teleport from Gate B (161, 0) back to Gate A (15.5, 0) at t=20s -> succeeds once.
            Assert.IsTrue(gateB.TryTeleport(20f), "[U18/WarpBToA]");
            Vector2 feetAfterBToA = EncounterZone.GetColliderFeet(playerCol);
            Assert.AreEqual(15.5f, feetAfterBToA.x, 1e-3f);
            Assert.AreEqual(0f, feetAfterBToA.y, 1e-3f);
            Assert.AreEqual(1, gateB.TeleportCount);

            // 4. Immediate bounce-back on Gate A at t=20.05s (< 0.25s cooldown) -> rejected!
            Assert.IsFalse(gateA.TryTeleport(20.05f), "[U18/CooldownGuard] Must reject immediate bounce-back within 0.25s.");
            Assert.AreEqual(15.5f, EncounterZone.GetColliderFeet(playerCol).x, 1e-3f);

            // 5. After cooldown at t=20.35s -> Gate A warps back to Gate B (161, 0).
            Assert.IsTrue(gateA.TryTeleport(20.35f), "[U18/WarpAToB]");
            Vector2 feetAfterAToB = EncounterZone.GetColliderFeet(playerCol);
            Assert.AreEqual(161f, feetAfterAToB.x, 1e-3f);
            Assert.AreEqual(0f, feetAfterAToB.y, 1e-3f);

            // 6. Verify GoldPickup sorting-layer promotion from Default (0) to VFX above Background.
            var coinRoot = Track(new GameObject("GoldPickupTest"));
            coinRoot.AddComponent<Rigidbody2D>();
            var coinChild = Track(new GameObject("Coin"));
            coinChild.transform.SetParent(coinRoot.transform, false);
            var coinSr = coinChild.AddComponent<SpriteRenderer>();
            coinSr.sortingLayerID = 0;
            coinSr.sortingOrder = 0;
            var pickup = coinRoot.AddComponent<GoldPickup>();
            pickup.EnsureVfxSortingLayer();
            Assert.AreEqual("VFX", coinSr.sortingLayerName, "[U18/GoldPickupSorting] Default sorting layer must promote to VFX.");
            Assert.GreaterOrEqual(coinSr.sortingOrder, 5);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
```

Create `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/LoreTabletTests.cs` (`U19`):
```csharp
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class LoreTabletTests
    {
        private readonly List<GameObject> _created = new();

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            Time.timeScale = 1f;
        }

        [Test]
        public void U19_LoreTablet_TogglesLoreBannerInsideTrigger_AndAutoClosesOnWalkAwayOrRunEnd()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(72f, 24f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var tabletGo = Track(new GameObject("I16_LoreTablet"));
            tabletGo.transform.position = new Vector3(72f, 24f, 0f);
            var trigger = tabletGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 1.0f);
            trigger.size = new Vector2(3.0f, 2.0f);

            var promptGo = Track(new GameObject("PromptIndicator"));
            var bannerGo = Track(new GameObject("LoreBanner"));
            var bannerText = bannerGo.AddComponent<Text>();

            var tablet = tabletGo.AddComponent<LoreTablet>();
            tablet.Configure(
                "I16",
                "Biên Niên Sử Ngục Tối",
                "Mầm bệnh Malaise phát tán từ tầng cống ngầm...",
                run,
                null,
                trigger,
                null,
                promptGo,
                bannerGo,
                bannerText,
                health,
                playerCol);

            // 1. Inside trigger, not reading -> prompt visible, banner hidden.
            tablet.RefreshVisualState();
            Assert.IsTrue(tablet.PromptVisible);
            Assert.IsFalse(tablet.IsReading);
            Assert.IsFalse(bannerGo.activeSelf);

            // 2. First press -> opens lore banner, increments ReadCount to 1, hides prompt while reading.
            Assert.IsTrue(tablet.TryInteract(), "[U19/Open]");
            Assert.IsTrue(tablet.IsReading);
            Assert.AreEqual(1, tablet.ReadCount);
            Assert.IsTrue(bannerGo.activeSelf);
            Assert.IsFalse(tablet.PromptVisible);
            StringAssert.Contains("Malaise", bannerText.text);

            // 3. Second press inside trigger -> closes lore banner, restores prompt.
            Assert.IsTrue(tablet.TryInteract(), "[U19/CloseToggle]");
            Assert.IsFalse(tablet.IsReading);
            Assert.IsFalse(bannerGo.activeSelf);
            Assert.IsTrue(tablet.PromptVisible);

            // 4. Open again, then walk out of trigger -> auto-closes banner!
            Assert.IsTrue(tablet.TryInteract());
            Assert.IsTrue(tablet.IsReading);
            playerGo.transform.position = new Vector3(60f, 24f, 0f);
            Physics2D.SyncTransforms();
            tablet.RefreshVisualState();
            Assert.IsFalse(tablet.IsReading, "[U19/AutoCloseOnWalkAway]");
            Assert.IsFalse(bannerGo.activeSelf);
            Assert.IsFalse(tablet.PromptVisible);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
```

Create `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/SecondaryBiomeExitTests.cs` (`U20`):
```csharp
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class SecondaryBiomeExitTests
    {
        private const string ShieldPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab";
        private const string ArcherPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab";

        private readonly List<GameObject> _created = new();

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            Time.timeScale = 1f;
        }

        [Test]
        public void U20_SecondaryBiomeExitTrigger_RequiresSwitchDoorOpenAndBothGuardiansDefeated_BeforeSubmittingWinCandidate()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(206f, -16f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var sdGo = Track(new GameObject("SD_2_SewerExit"));
            sdGo.transform.position = new Vector3(206f, -16f, 0f);
            var sdTrig = sdGo.AddComponent<BoxCollider2D>();
            sdTrig.isTrigger = true;
            sdTrig.size = new Vector2(3f, 2f);
            var barrierGo = Track(new GameObject("Barrier"));
            var sd = sdGo.AddComponent<SwitchDoor>();
            sd.Configure("SD_2", run, null, sdTrig, barrierGo, null, null, null, null, null, health, playerCol);

            var shieldP = SpawnEnemy(ShieldPrefabPath);
            var archerP = SpawnEnemy(ArcherPrefabPath);

            var exitGo = Track(new GameObject("I15_SewerExitTrigger"));
            exitGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var sewerExit = exitGo.AddComponent<SecondaryBiomeExitTrigger>();
            sewerExit.Configure("I15", "Toxic Sewers", run, sd, shieldP, archerP, null, health, playerCol);

            int candidates = 0;
            sewerExit.WinCandidateSubmitted += _ => candidates++;

            // 1. SwitchDoor closed & guardians alive -> rejected.
            Assert.IsFalse(sewerExit.IsUnlocked);
            Assert.IsFalse(sewerExit.TrySubmitForCollider(playerCol, 10), "[U20/Locked]");
            Assert.AreEqual(0, candidates);

            // 2. Open SwitchDoor SD_2, but only 1/2 guardians defeated -> still rejected!
            Assert.IsTrue(sd.TryInteract(), "[U20/OpenSwitchDoor]");
            sewerExit.NotifyGuardianDied(shieldP);
            Assert.IsFalse(sewerExit.AreGuardiansDefeated);
            Assert.IsFalse(sewerExit.IsUnlocked);
            Assert.IsFalse(sewerExit.TrySubmitForCollider(playerCol, 11), "[U20/OneGuardianAlive]");

            // 3. Defeat second guardian -> IsUnlocked becomes true, submits Win candidate at frame 12!
            sewerExit.NotifyGuardianDied(archerP);
            Assert.IsTrue(sewerExit.AreGuardiansDefeated);
            Assert.IsTrue(sewerExit.IsUnlocked);
            Assert.IsTrue(sewerExit.TrySubmitForCollider(playerCol, 12), "[U20/UnlockedWin]");
            Assert.AreEqual(1, candidates);
            Assert.AreEqual("Toxic Sewers", sewerExit.BiomeName);

            // Settle on next frame -> LevelOutcome.Win.
            Assert.IsTrue(run.ResolvePendingOutcome(13));
            Assert.AreEqual(LevelOutcome.Win, run.Outcome);
        }

        private EnemyController SpawnEnemy(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, "Missing prefab: " + prefabPath);
            var inst = Track(Object.Instantiate(prefab));
            return inst.GetComponent<EnemyController>();
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
```

- [ ] **Step 2: Run EditMode tests to verify `U17`–`U20` fail before implementation**

Run via `rtk proxy`:
```powershell
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform EditMode -testFilter 'SwitchDoorTests|TeleportGateTests|LoreTabletTests|SecondaryBiomeExitTests' -testResults 'D:\thietkegame\Logs\r6-t1-red.xml' -logFile 'D:\thietkegame\Logs\r6-t1-red.log' -job-worker-count 1"
```
Expected: Fails to compile because `SwitchDoor`, `TeleportGate`, `LoreTablet`, `SecondaryBiomeExitTrigger`, and `GoldPickup.EnsureVfxSortingLayer()` do not exist yet.

- [ ] **Step 3: Implement `GoldPickup.EnsureVfxSortingLayer()`, `SwitchDoor.cs`, `TeleportGate.cs`, `LoreTablet.cs`, and `SecondaryBiomeExitTrigger.cs`**

1. **`Assets/_Project/Features/Currency/Scripts/GoldPickup.cs`:**
   - In `Awake()`, call `EnsureVfxSortingLayer();` after `_body = GetComponent<Rigidbody2D>();`.
   - Add public method:
     ```csharp
     public void EnsureVfxSortingLayer()
     {
         int vfxLayerId = SortingLayer.NameToID("VFX");
         if (vfxLayerId == 0)
             return;
         foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
         {
             if (sr != null && sr.sortingLayerID == 0)
             {
                 sr.sortingLayerID = vfxLayerId;
                 if (sr.sortingOrder < 5)
                     sr.sortingOrder = 5;
             }
         }
     }
     ```
2. **`Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SwitchDoor.cs`:**
   - Namespace `DeadCells.NhaGiamGiengChuong`.
   - Serialized fields: `_doorId`, `_run`, `_player`, `_playerHealth`, `_playerCollider`, `_switchTrigger`, `_doorBarrierRoot`, `_switchRenderer`, `_switchOffSprite`, `_switchOnSprite`, `_inputActions`, `_actionMapName = "Player"`, `_interactActionName = "Interact"`, `_promptIndicator`.
   - Properties: `DoorId`, `IsOpen`, `PromptVisible`, `DoorBarrierRoot`, `SwitchTrigger`, `PromptIndicator`, `event Action<SwitchDoor> Opened`.
   - `Configure(...)` sets all fields, resets `_isOpen = false`, activates `_doorBarrierRoot`, sets `_switchRenderer.sprite = _switchOffSprite`, subscribes input if enabled, and calls `RefreshVisualState()`.
   - `TryInteract()` validates `!_isOpen && _run != null && _run.IsPlaying && health != null && health.IsAlive && IsColliderInTrigger(col)`, sets `_isOpen = true`, calls `_doorBarrierRoot.SetActive(false)`, sets `_switchRenderer.sprite = _switchOnSprite`, calls `RefreshVisualState()`, fires `Opened?.Invoke(this)`, and returns `true`.
3. **`Assets/_Project/Features/NhaGiamGiengChuong/Scripts/TeleportGate.cs`:**
   - Namespace `DeadCells.NhaGiamGiengChuong`.
   - Constant `public const float TeleportCooldownSeconds = 0.25f;`.
   - Serialized fields: `_gateId`, `_run`, `_player`, `_playerHealth`, `_playerCollider`, `_triggerZone`, `_pairedGate`, `_startsDiscovered`, `_arrivalOffset`, `_inputActions`, `_actionMapName = "Player"`, `_interactActionName = "Interact"`, `_promptIndicator`, `_stoneRenderer`, `_inactiveSprite`, `_activeSprite`.
   - Private state: `_isDiscovered`, `_promptVisible`, `_teleportCount`, `_cooldownUntilTime = -10f`.
   - Properties: `GateId`, `PairedGate`, `IsDiscovered`, `CanTeleport => _isDiscovered && _pairedGate != null && _pairedGate._isDiscovered`, `ArrivalFeetPosition => (Vector2)transform.position + _arrivalOffset`, `TeleportCount`, `PromptVisible`, `TriggerZone`, `PromptIndicator`, `event Action<TeleportGate> Discovered`, `event Action<TeleportGate, TeleportGate> Teleported`.
   - `Discover()` sets `_isDiscovered = true`, updates `_stoneRenderer.sprite = _activeSprite` (if non-null), and fires `Discovered?.Invoke(this)`.
   - `RefreshVisualState()` (called in `Update()` and `Configure()`/`Awake()`): if `!_isDiscovered && _run != null && _run.IsPlaying && health != null && health.IsAlive && IsColliderInTrigger(col)`, calls `Discover()`. Sets `_promptVisible = _run != null && _run.IsPlaying && CanTeleport && health != null && health.IsAlive && IsColliderInTrigger(col) && Time.unscaledTime >= _cooldownUntilTime`.
   - `TryTeleport()` calls `TryTeleport(Time.unscaledTime)`. `TryTeleport(float currentTime)` validates `_run != null && _run.IsPlaying && health != null && health.IsAlive && IsColliderInTrigger(col)`, calls `Discover()` if `!_isDiscovered`, then checks `CanTeleport && currentTime >= _cooldownUntilTime`. Computes `Vector2 currentFeet = EncounterZone.GetColliderFeet(col); Vector2 delta = _pairedGate.ArrivalFeetPosition - currentFeet;`, translates `actorTransform.position += (Vector3)delta`, updates `Rigidbody2D.position` and zeroes `linearVelocity` if `Rigidbody2D` exists, calls `Physics2D.SyncTransforms()`, sets `_cooldownUntilTime = _pairedGate._cooldownUntilTime = currentTime + TeleportCooldownSeconds`, increments `_teleportCount`, refreshes both gates' visual states, and invokes `Teleported?.Invoke(this, _pairedGate)`.
4. **`Assets/_Project/Features/NhaGiamGiengChuong/Scripts/LoreTablet.cs`:**
   - Namespace `DeadCells.NhaGiamGiengChuong`.
   - Serialized fields: `_tabletId`, `_loreTitle`, `_loreBody`, `_run`, `_player`, `_playerHealth`, `_playerCollider`, `_triggerZone`, `_inputActions`, `_actionMapName = "Player"`, `_interactActionName = "Interact"`, `_promptIndicator`, `_loreBannerRoot`, `_loreBannerText`.
   - Properties: `TabletId`, `LoreTitle`, `LoreBody`, `IsReading`, `ReadCount`, `PromptVisible`, `TriggerZone`, `PromptIndicator`, `LoreBannerRoot`, `event Action<LoreTablet, bool> ReadingStateChanged`.
   - `RefreshVisualState()`: if `_isReading` and (`_run == null || !_run.IsPlaying || health == null || !health.IsAlive || !IsColliderInTrigger(col)`), sets `_isReading = false` and fires `ReadingStateChanged?.Invoke(this, false)`. Sets `_promptVisible = !_isReading && _run != null && _run.IsPlaying && health != null && health.IsAlive && IsColliderInTrigger(col)`. Updates `_promptIndicator.SetActive(_promptVisible)` and `_loreBannerRoot.SetActive(_isReading)`.
   - `TryInteract()`: validates `_run != null && _run.IsPlaying && health != null && health.IsAlive && IsColliderInTrigger(col)`, toggles `_isReading = !_isReading`, if `_isReading` increments `_readCount` and formats `_loreBannerText.text = $"{_loreTitle}\n{_loreBody}"`, calls `RefreshVisualState()`, fires `ReadingStateChanged?.Invoke(this, _isReading)`, and returns `true`.
5. **`Assets/_Project/Features/NhaGiamGiengChuong/Scripts/SecondaryBiomeExitTrigger.cs`:**
   - Namespace `DeadCells.NhaGiamGiengChuong`, `[RequireComponent(typeof(BoxCollider2D))]`.
   - Serialized fields: `_exitId`, `_biomeName = "Toxic Sewers"`, `_run`, `_switchDoor`, `_firstGuardian`, `_secondGuardian`, `_player`, `_playerHealth`, `_playerCollider`.
   - Tracks guardian deaths via `_defeatedGuardians` (`HashSet<EnemyController>`), subscribing to `_firstGuardian.Died` and `_secondGuardian.Died` in `OnEnable`/`Configure` (and checking `enemy.GetComponent<Health>()?.IsAlive == false`), plus public `NotifyGuardianDied(EnemyController enemy)`.
   - Properties: `ExitId`, `BiomeName`, `Run`, `SwitchDoor`, `FirstGuardian`, `SecondGuardian`, `AreGuardiansDefeated`, `IsUnlocked => (_switchDoor == null || _switchDoor.IsOpen) && AreGuardiansDefeated`, `event Action<int> WinCandidateSubmitted`.
   - `TrySubmitForCollider(Collider2D candidateCollider, int frame)` validates `candidateCollider != null && _run != null && _run.IsPlaying && IsUnlocked && candidateCollider == ResolvePlayerCollider() && ResolvePlayerHealth()?.IsAlive == true`, calls `_run.RequestWin(frame)`, fires `WinCandidateSubmitted?.Invoke(frame)`, and returns `true`.

- [ ] **Step 4: Run EditMode unit tests `U01`–`U20` to verify all pass**

Run via `rtk proxy`:
```powershell
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform EditMode -testFilter 'DeadCells.NhaGiamGiengChuong.Tests.Unit' -testResults 'D:\thietkegame\Logs\r6-t1-green.xml' -logFile 'D:\thietkegame\Logs\r6-t1-green.log' -job-worker-count 1"
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
```
Expected: `20/20` unit tests (`U01`–`U20`) pass with `0` warnings and `0` errors.

- [ ] **Step 5: Sync CodeGraph**

```powershell
rtk proxy cmd /c codegraph sync
```

---

## Task 2: Core Spine Doorways (`G`, `HJK`, `ELF`) & 4 New Modular Wing Prefab Builders (`W1_EastCourtyard`, `W2_SewerCistern`, `W3_WestBastion`, `U1_UpperPromenade`) + Isolated Wing Geometry Tests `A05b`–`A05e`

**Files:**
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/WingGeometryTests.cs` (`A05b`–`A05e`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`
- Generate: `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/{CD,AB,G,HJK,ELF,W1_EastCourtyard,W2_SewerCistern,W3_WestBastion,U1_UpperPromenade}.prefab`

**Interfaces:**
- Consumes: `PrisonPalette`, `NhaGiamGiengChuongBuilder` helpers (`AddSolidBox`, `AddOneWayBox`, `AddClimbableTrigger`, `CreateRoomVisualGrid`, `PaintPrisonBackWall`, `CarveAndFrameArchBay`, `PaintPrisonSolidFloor`, `PaintVisualDirtSkirt`, `PaintPrisonOuterWall`, `PaintPrisonCeiling`, `PaintPrisonCeilingRimOnly`, `PaintPrisonInteriorHeader`, `AddPrisonPlankRow`, `AddPlatformCornerBrace`, `AddPrisonChainArt`, `AddCornerBeamsAndWebs`, `AddGroundedPrisonSprite`, `AddVerticalRoomBanner`, `AddStandingTorchBeacon`, `AddCeilingLanternBeacon`).
- Produces:
  - `BuildRoomW1_EastCourtyard(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)`
  - `BuildRoomW2_SewerCistern(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)`
  - `BuildRoomW3_WestBastion(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)`
  - `BuildRoomU1_UpperPromenade(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)`
  - `9` Room Prefabs saved under `Assets/_Project/Features/NhaGiamGiengChuong/Rooms/`.

- [ ] **Step 1: Write the failing Wing Geometry Tests `A05b`–`A05e` in `WingGeometryTests.cs`**

Create `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/WingGeometryTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class WingGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A05b_W1EastCourtyardPrefab_MatchesRoomsMNCollidersChainsOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("W1_EastCourtyard");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            // Room M (x=76..164, y=0..7)
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_M_West", 76f, 115f, -1f, 0f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_M_WellBridge", 115f, 121f, 0f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_M_East", 121f, 164f, -1f, 0f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_M_Perch1", 88f, 96f, 3.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_M_Perch2", 134f, 142f, 3.5f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05b/FloorM_Union", 0f, 76f, 164f);

            // Room N (x=76..164, y=8..22.5)
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_N_West", 76f, 101f, 7f, 8f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_N_WellCap", 101f, 107f, 8f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_N_East", 107f, 164f, 7f, 8f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05b/FloorN_Union", 8f, 76f, 164f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_N_Mid1", 86f, 96f, 12.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_N_Mid2", 116f, 128f, 12.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_N_High", 136f, 150f, 16.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Ceiling_N", 76f, 164.5f, 22.5f, 23f);

            // Walls & Headers
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Wall_MN_East", 164f, 164.5f, -1f, 22.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Wall_N_WestUpper_76", 76f, 76.5f, 15f, 22.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Wall_M_VaultHeader_150", 149.75f, 150.25f, 4f, 7f);

            // Chain I7 & Openings
            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05b/I7", 104f, -0.5f, 8.5f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05b/Open_GM_Low_76", 76f, 0f, 4f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05b/Open_GN_High_76", 76f, 8f, 12f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05b/Open_M_Vault_150", 150f, 0f, 4f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05b");
        }

        [Test]
        public void A05c_W2SewerCisternPrefab_MatchesRoomsOPCollidersChainOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("W2_SewerCistern");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Floor_O", 76f, 164f, -17f, -16f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Floor_P", 164f, 234f, -17f, -16f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05c/FloorOP_Union", -16f, 76f, 234f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Ceiling_P", 164f, 234.5f, -1.5f, -1f);

            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_O_West", 84f, 96f, -11.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_O_ShaftStep1", 112f, 124f, -11f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_O_ShaftStep2", 112f, 124f, -5.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_O_East", 138f, 150f, -11.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_P_Mid", 184f, 198f, -11.5f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_O_West", 75.5f, 76f, -17f, -1f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_OP_Header_164", 163.75f, 164.25f, -12f, -1f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_P_SewerHeader_212", 211.75f, 212.25f, -12f, -1.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_P_East", 234f, 234.5f, -17f, -1.5f);

            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05c/I8", 118f, -16.5f, 0.5f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05c/Open_OP_164", 164f, -16f, -12f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05c/Open_P_SewerGate_212", 212f, -16f, -12f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05c");
        }

        [Test]
        public void A05d_W3WestBastionPrefab_MatchesRoomsQRCollidersChainOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("W3_WestBastion");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Floor_Q", -102f, -30f, 15f, 16f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_Q_Mid", -88f, -76f, 19.5f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Floor_R_West", -102f, -69f, 23f, 24f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_R_WellCap", -69f, -63f, 24f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Floor_R_East", -63f, -30f, 23f, 24f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05d/FloorR_Union", 24f, -102f, -30f);

            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_R_Mid1", -54f, -42f, 28.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_R_Mid2", -82f, -70f, 28.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Ceiling_R", -102.5f, -30f, 38.5f, 39f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Wall_QR_West", -102.5f, -102f, 15f, 38.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Wall_R_EastUpper_Neg30", -30.5f, -30f, 31f, 38.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Wall_R_ArmoryHeader_Neg88", -88.25f, -87.75f, 28f, 38.5f);

            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05d/I9", -66f, 15.5f, 24.5f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05d/Open_KQ_Low_Neg30", -30f, 16f, 20f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05d/Open_KR_High_Neg30", -30f, 24f, 28f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05d/Open_R_Armory_Neg88", -88f, 24f, 28f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05d");
        }

        [Test]
        public void A05e_U1UpperPromenadePrefab_MatchesRoomsSTCollidersChainOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("U1_UpperPromenade");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Floor_S", 38f, 92f, 23f, 24f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_S_Gallery", 44f, 58f, 28.5f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Floor_T_West", 38f, 77f, 39f, 40f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_T_ShaftCap", 77f, 83f, 40f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Floor_T_East", 83f, 140f, 39f, 40f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05e/FloorT_Union", 40f, 38f, 140f);

            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_T_Mid1", 52f, 64f, 44.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_T_Mid2", 92f, 106f, 44.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_T_High", 112f, 126f, 49f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Ceiling_T", 37.5f, 140.5f, 62.5f, 63f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Wall_S_East", 92f, 92.5f, 23f, 39f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Wall_T_West", 37.5f, 38f, 39f, 62.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Wall_T_East", 140f, 140.5f, 39f, 62.5f);

            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05e/I10", 80f, 23.5f, 40.5f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05e/Open_LS_38", 38f, 24f, 28f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05e");
        }
    }
}
```

- [ ] **Step 2: Run `WingGeometryTests` to verify `A05b`–`A05e` fail before building the wing prefabs**

Run via `rtk proxy`:
```powershell
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform EditMode -testFilter 'WingGeometryTests' -testResults 'D:\thietkegame\Logs\r6-t2-red.xml' -logFile 'D:\thietkegame\Logs\r6-t2-red.log' -job-worker-count 1"
```
Expected: Fails because `W1_EastCourtyard.prefab`, `W2_SewerCistern.prefab`, `W3_WestBastion.prefab`, and `U1_UpperPromenade.prefab` do not exist yet.

- [ ] **Step 3: Implement Core Spine Doorway Carves (`G`, `HJK`, `ELF`), Extended `PrisonPalette`, and the 4 New Wing Prefab Builders in `NhaGiamGiengChuongBuilder.cs`**

In `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`:
1. **Extend `PrisonPalette` and `LoadPrisonPalette()`** with the verified on-disk `24 PPU` sprites from `Assets/_Project/Art/Environment/Prison/Sprites/`:
   - `PrisonFountain = LoadPrisonSprite("prisonFountain_00.png")`
   - `HomonculusFlush = LoadPrisonSprite("homonculusFlush_00.png")`
   - `ArmoryDome = LoadPrisonSprite("armory_dome_00.png")`
   - `ArmoryCorps = LoadPrisonSprite("armory_corps_00.png")`
   - `WeaponDeco = LoadPrisonSprite("weaponDeco_00.png")`
   - `Dummy0 = LoadPrisonSprite("dummy_0_00.png")`
   - `MonsterDoorFront = LoadPrisonSprite("monsterDoorFront_00.png")`
   - `SmallDoor = LoadPrisonSprite("smallDoor_00.png")`
   - `BigDoor0 = LoadPrisonSprite("bigDoor_0_00.png")`
   - `ExitDoorSewerShort = LoadPrisonSprite("exitDoorSewerShort_00.png")`
   - `HingeCorruptedDoor = LoadPrisonSprite("hingeCorruptedDoor_00.png")`
   - `JudasSeat = LoadPrisonSprite("judasSeat_00.png")`
   - `Billot = LoadPrisonSprite("billot_00.png")`
   - `SkeletonSpawn = LoadPrisonSprite("skeletonSpawn_00.png")`
   - `SnakeLandmark = LoadPrisonSprite("snakeLandmark_00.png")`
   - `BigSword = LoadPrisonSprite("bigSword_00.png")`
   - `AlchemyBibli = LoadPrisonSprite("alchemyBibli_00.png")`
   - `TableauNoir = LoadPrisonSprite("tableauNoir_00.png")`
   - `AspectsNpcStuffs = LoadPrisonSprite("aspectsNPCstuffs_00.png")`
   - `ExitDoorPrisonCourtyard = LoadPrisonSprite("exitDoorPrisonCourtyard_00.png")`
   - `SpawnerPillar = LoadPrisonSprite("spawnerPillar_00.png")`
   - `SwitchCopperOn = LoadPrisonSprite("switchCopper_00.png")`
   - `SwitchCopperOff = LoadPrisonSprite("switchCopper_off_00.png")`
   - `TpStoneUp = LoadPrisonSprite("tpStoneUp_00.png")`
   - `TpStoneDown = LoadPrisonSprite("tpStoneDown_00.png")`
   - `TpRocks = Enumerable.Range(0, 4).Select(i => LoadPrisonSprite($"tpRock_{i}_00.png")).ToArray()`
   - `MatEmissiveScroll = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Scroll.mat")`
2. **Update Core Spine Prefabs (`AB`, `CD`, `G`, `HJK`, `ELF`) for Wing Docking & Non-Overlapping Dirt Skirts:**
   - In `BuildRoomAB`:
     - Update dirt skirt to `PaintVisualDirtSkirt(solidTm, pal, -4, 42, -8, -1);`.
     - Add grounded Homunculus spawn fountain `AddGroundedPrisonSprite(artRoot, "Art_A_Fountain", pal.PrisonFountain, mat, 3.2f, 0f, 0.85f, -13, Color.white);` and `AddPrisonSpriteProp(artRoot, "Art_A_HomonculusPipes", pal.HomonculusFlush, mat, new Vector2(3.2f, 2.6f), 0.85f, -14, Color.white);` (both strictly outside `Bay #0 [9..14, 2..5]`).
   - In `BuildRoomCD`:
     - Update dirt skirt to `PaintVisualDirtSkirt(solidTm, pal, 42, 56, -8, -1);`.
   - In `BuildRoomG`:
     - Replace `AddSolidBox(colRoot, "Wall_G_East", 76f, 76.5f, -1f, 14.5f);` with:
       ```csharp
       AddSolidBox(colRoot, "Wall_GM_Mid_76", 76f, 76.5f, 4f, 7f);
       AddSolidBox(colRoot, "Wall_GN_Upper_76", 76f, 76.5f, 12f, 14.5f);
       ```
     - Replace `PaintVisualDirtSkirt(solidTm, pal, 56, 80, -5, -1);` with `PaintVisualDirtSkirt(solidTm, pal, 56, 76, -21, -1);` (strictly trimmed at `xMax = 76` so `0` tiles enter `Room O`, and extended down to `y = -21`).
     - Replace `PaintPrisonOuterWall(solidTm, pal, 76f, 76.5f, -1f, 15f);` with:
       ```csharp
       PaintPrisonOuterWall(solidTm, pal, 76f, 76.5f, 4f, 7f);
       PaintPrisonOuterWall(solidTm, pal, 76f, 76.5f, 12f, 15f);
       ```
     - Pass `hasRightBottomWall: false` to `AddCornerBeamsAndWebs(..., "G", ...)` so no bottom-right cobweb floats in the `G<->M_low` doorway at `x = 76, y = 0..4`.
   - In `BuildRoomHJK`:
     - Replace `AddSolidBox(colRoot, "Wall_K_West", -30.5f, -30f, 15f, 30.5f);` with:
       ```csharp
       AddSolidBox(colRoot, "Wall_KQ_Mid_Neg30", -30.5f, -30f, 20f, 23f);
       AddSolidBox(colRoot, "Wall_KR_Upper_Neg30", -30.5f, -30f, 28f, 30.5f);
       ```
     - Update dirt skirt to `PaintVisualDirtSkirt(solidTm, pal, -30, 6, 7, 15);`.
     - Replace `PaintPrisonOuterWall(solidTm, pal, -30.5f, -30f, 15f, 31f);` with:
       ```csharp
       PaintPrisonOuterWall(solidTm, pal, -30.5f, -30f, 20f, 23f);
       PaintPrisonOuterWall(solidTm, pal, -30.5f, -30f, 28f, 31f);
       ```
     - Pass `hasLeftBottomWall: false` to `AddCornerBeamsAndWebs(..., "K", ...)` so no bottom-left cobweb floats in the `K<->Q_low` doorway at `x = -30, y = 16..20`.
   - In `BuildRoomELF`:
     - Replace `AddSolidBox(colRoot, "Wall_L_East", 38f, 38.5f, 23f, 38.5f);` with:
       ```csharp
       AddSolidBox(colRoot, "Wall_LS_Upper_38", 38f, 38.5f, 28f, 38.5f);
       ```
     - Replace `PaintPrisonOuterWall(solidTm, pal, 38f, 38.5f, 23f, 39f);` with:
       ```csharp
       PaintPrisonOuterWall(solidTm, pal, 38f, 38.5f, 28f, 39f);
       ```
     - Remove `AddPlatformCornerBrace(artRoot, "Art_L1_BraceR", pal.WoodenCornerRight, mat, 38f, 26f, false);` so no wooden wall bracket floats inside the open `L<->S` doorway (`x = 38, y = 24..28`).
     - Pass `hasRightBottomWall: false` to `AddCornerBeamsAndWebs(..., "L", ...)` so no bottom-right cobweb floats in the `L<->S` doorway at `x = 38, y = 24..28`.
3. **Implement `BuildRoomW1_EastCourtyard`, `BuildRoomW2_SewerCistern`, `BuildRoomW3_WestBastion`, and `BuildRoomU1_UpperPromenade`:**
   - Each builder creates `Collision` and `Art` children, adds the exact colliders and climbable chains from Spec `§5.2` (verified by `A05b`–`A05e`), creates `VisualGrid` (`1.0u`) with the 4 visual-only Tilemaps (`TM_Background`, `TM_WallDecals`, `TM_SolidVisual`, `TM_SolidRim`), paints backwalls, carves its arch viewing bay (`Bay #5` `[118..126, 14..19]` in Room N, `Bay #6` `[100..106, -13..-8]` in Room O, `Bay #7` `[-60..-54, 30..35]` in Room R, `Bay #8` `[66..74, 46..52]` in Room T), paints solid floors, ceilings, outer walls, interior headers, and exact non-overlapping `PaintVisualDirtSkirt` fills from Spec `§5.5 #6`, adds all `21` wing plank rows (`Art_M_WellBridgePlanks`, `Art_M_Perch1Planks`, `Art_M_Perch2Planks`, `Art_N_WellCapPlanks`, `Art_N_Mid1Planks`, `Art_N_Mid2Planks`, `Art_N_HighPlanks`, `Art_O_WestPlanks`, `Art_O_ShaftStep1Planks`, `Art_O_ShaftStep2Planks`, `Art_O_EastPlanks`, `Art_P_MidPlanks`, `Art_Q_MidPlanks`, `Art_R_WellCapPlanks`, `Art_R_Mid1Planks`, `Art_R_Mid2Planks`, `Art_S_GalleryPlanks`, `Art_T_ShaftCapPlanks`, `Art_T_Mid1Planks`, `Art_T_Mid2Planks`, `Art_T_HighPlanks` -> `31` total with the `10` core plank rows), chain art (`Art_I7_Chain`, `Art_I8_Chain`, `Art_I9_Chain`, `Art_I10_Chain`), corner beams/webs (with `hasLeftBottomWall` / `hasRightBottomWall` set to `true` ONLY where a solid bottom wall exists), vertical banners (`Art_N_Banner` `x=109.5, topY=21.8`, `Art_P_Banner` `x=180.0, topY=-2.2`, `Art_R_Banner` `x=-40.0, topY=37.8`, `Art_T_Banner` `x=88.0, topY=61.8`), and hero landmarks with **explicit room-fitting scales** from Spec `§5.5 #5` (`0` overlap with bays, ceilings, or `SwitchDoor` barriers):
     - **Room M (`W1_EastCourtyard`):** `Art_M_Dummy` (`dummy_0_00`, `x=83f, floorY=0f, scale=0.90f`), `Art_M_ArmoryDome1` (`armory_dome_00`, `x=98f, floorY=0f, scale=0.28f`), `Art_M_ArmoryCorps` (`armory_corps_00`, `x=128f, floorY=0f, scale=0.28f`), `Art_M_WeaponDeco` (`weaponDeco_00`, `x=156.5f, floorY=0f, scale=0.45f`).
     - **Room N (`W1_EastCourtyard`):** `Art_N_Alcove1` (`prisonBigAlcove_00`, `x=84f, floorY=8f, scale=1.0f`), `Art_N_Alcove2` (`prisonBigAlcove_00`, `x=148f, floorY=8f, scale=1.0f`), `Art_N_Cage1` (`suspendedCage_00`, `x=113f, ceilingY=22.5f, scale=1.0f`), `Art_N_Cage2` (`suspendedCage_00`, `x=136f, ceilingY=22.5f, scale=1.0f`).
     - **Room O (`W2_SewerCistern`):** `Art_O_MonsterDoor` (`monsterDoorFront_00`, `x=90f, floorY=-16f, scale=0.85f`), `Art_O_CellDoor1` (`smallDoor_00`, `x=128f, floorY=-16f, scale=0.90f`), `Art_O_CellDoor2` (`bigDoor_0_00`, `x=154f, floorY=-16f, scale=0.85f`).
     - **Room P (`W2_SewerCistern`):** `Art_P_CorruptedDoor` (`hingeCorruptedDoor_00`, `x=174f, floorY=-16f, scale=0.85f`), `Art_P_SewerExitDoor` (`exitDoorSewerShort_00`, `x=224f, floorY=-16f, scale=0.85f`), `Art_P_ExitGlow` (`PortalBackOut_0_00`, `x=224f, floorY=-16f, scale=0.55f` + warm `Light2D` `intensity=1.40f, outerRadius=7.2f, volumeIntensity=0.09f`).
     - **Room Q (`W3_WestBastion`):** `Art_Q_JudasSeat` (`judasSeat_00`, `x=-42f, floorY=16f, scale=0.85f`), `Art_Q_Billot` (`billot_00`, `x=-56f, floorY=16f, scale=0.85f`), `Art_Q_SkeletonSpawn` (`skeletonSpawn_00`, `x=-74f, floorY=16f, scale=0.45f`), `Art_Q_SnakeLandmark` (`snakeLandmark_00`, `x=-90f, floorY=16f, scale=0.30f`).
     - **Room R (`W3_WestBastion`):** `Art_R_ArmoryDome` (`armory_dome_00`, `x=-74f, floorY=24f, scale=0.50f`), `Art_R_BigSword` (`bigSword_00`, `x=-95f, floorY=24f, scale=0.55f`), `Art_R_Cage` (`suspendedCage_00`, `x=-46f, ceilingY=38.5f, scale=1.0f`).
     - **Room S (`U1_UpperPromenade`):** `Art_S_AlchemyBibli1` (`alchemyBibli_00`, `x=47f, floorY=24f, scale=0.60f`), `Art_S_AlchemyBibli2` (`alchemyBibli_00`, `x=60f, floorY=24f, scale=0.60f`), `Art_S_AspectsDesk` (`aspectsNPCstuffs_00`, `x=72f, floorY=24f, scale=0.75f`), `Art_S_TableauNoir` (`tableauNoir_00`, `x=72f, y=28.5f, scale=0.75f`).
     - **Room T (`U1_UpperPromenade`):** `Art_T_BigAlcove` (`prisonBigAlcove_00`, `x=52f, floorY=40f, scale=1.0f`), `Art_T_Pillar1` (`alcovePillar_00`, `x=96f, floorY=40f, scale=1.0f`), `Art_T_Pillar2` (`spawnerPillar_00`, `x=112f, floorY=40f, scale=0.85f`), `Art_T_CourtyardArch` (`exitDoorPrisonCourtyard_00`, `x=128f, floorY=40f, scale=0.80f`).
   - Place **`62` `24 PPU` wing wall/ceiling sconce lanterns** (`88` total across the `19` rooms with the `26` core lanterns; `intensity = 1.35f..1.40f <= 1.50f`, `volumeIntensity = 0.09f <= 0.10f`) at the exact verified positions & outer radii below so every single room achieves **`95.2%–100.0%` point-light coverage** (`>= 50%` required) and **`98.1%` total map coverage** (`>= 75%` required) with `0` lanterns floating inside any carved arch bay:
     - **Room M (`5` floor beacons @ `floorY=0f, r=9.5f`):** `x = { 84f, 102f, 120f, 138f, 156f }`.
     - **Room N (`5` floor beacons @ `floorY=8f, r=10.0f` + `4` ceiling beacons @ `ceilingY=22.5f, r=10.0f` = `9`):** floor `x = { 84f, 102f, 120f, 138f, 156f }`, ceiling `x = { 90f, 112f, 134f, 154f }`.
     - **Room O (`5` floor beacons @ `floorY=-16f, r=10.0f` + `4` ceiling beacons @ `ceilingY=-1f, r=10.0f` = `9`):** floor `x = { 84f, 102f, 120f, 138f, 156f }`, ceiling `x = { 90f, 112f, 134f, 154f }`.
     - **Room P (`4` floor beacons @ `floorY=-16f, r=10.0f` + `3` ceiling beacons @ `ceilingY=-1.5f, r=10.0f` = `7` + `Art_P_ExitGlow`):** floor `x = { 172f, 190f, 208f, 226f }`, ceiling `x = { 178f, 198f, 218f }`.
     - **Room Q (`4` floor beacons @ `floorY=16f, r=9.5f`):** `x = { -93f, -75f, -57f, -39f }`.
     - **Room R (`4` floor beacons @ `floorY=24f, r=10.0f` + `3` ceiling beacons @ `ceilingY=38.5f, r=10.0f` = `7`):** floor `x = { -93f, -75f, -57f, -39f }`, ceiling `x = { -86f, -66f, -46f }`.
     - **Room S (`3` floor beacons @ `floorY=24f, r=10.0f` + `3` ceiling beacons @ `ceilingY=39f, r=10.0f` = `6`):** floor `x = { 46f, 65f, 84f }`, ceiling `x = { 46f, 65f, 84f }`.
     - **Room T (`5` floor beacons @ `floorY=40f, r=10.5f` + `5` mid-wall beacons @ `floorY=49f, r=10.5f` + `5` ceiling beacons @ `ceilingY=62.5f, r=10.5f` = `15`):** floor & ceiling `x = { 48f, 68f, 88f, 108f, 128f }`, mid-wall `x = { 48f, 62f, 88f, 108f, 128f }` (`62f` mounts on solid brick left of `Bay #8 [66..74, 46..52]`).
   - Register the 4 new `SaveRoomPrefab` calls in `BuildAll()`.

- [ ] **Step 4: Run `BuildAll()` and execute `A01`–`A05` + `A05b`–`A05e` to verify all 9 isolated room prefab geometry tests pass**

Run via `rtk proxy`:
```powershell
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -quit -logFile 'D:\thietkegame\Logs\r6-t2-build.log' -job-worker-count 1"
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform EditMode -testFilter 'CDGeometryTests|ABGeometryTests|GGeometryTests|HJKGeometryTests|ELFGeometryTests|WingGeometryTests' -testResults 'D:\thietkegame\Logs\r6-t2-green.xml' -logFile 'D:\thietkegame\Logs\r6-t2-green.log' -job-worker-count 1"
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
```
Expected: `9/9` room prefab geometry tests (`A01`–`A05` + `A05b`–`A05e`) pass with `0` warnings and `0` errors.

- [ ] **Step 5: Sync CodeGraph**

```powershell
rtk proxy cmd /c codegraph sync
```

---

## Task 3: Full-Scale Scene Assembly in `NhaGiamGiengChuongBuilder.BuildScene` (`9` Prefabs, `30` Enemies, `11` Zones, `2` Scrolls, Gold Economy, `6` Teleporters, `3` Switch Doors, Lore Tablet, Dual Exits, Expanded Parallax/Atmosphere) & EditMode Asset/Wiring Suite `A06`–`A13`

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapGeometryTests.cs` (`A06`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A07`–`A12` + new `A13`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`
- Generate: `Assets/_Project/Scenes/NhaGiamGiengChuong.unity`

**Interfaces:**
- Consumes: `SwitchDoor`, `TeleportGate`, `LoreTablet`, `SecondaryBiomeExitTrigger`, `PowerScrollPickup`, `PowerScrollInteraction`, `ScrollUpgradeMenu`, `ScrollPickupPresentation`, `GoldWallet`, `EnemyGoldDrops`, `GoldPickup`, `GoldHud`, `W1_EastCourtyard.prefab`, `W2_SewerCistern.prefab`, `W3_WestBastion.prefab`, `U1_UpperPromenade.prefab`.
- Produces: Full-scale `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` passing `A06`–`A13`.

- [ ] **Step 1: Update `A06`–`A12` and add `A13_FullScalePrisonersQuarters_ScrollsGoldTeleportersSwitchDoorsLoreAndDualExits`**

1. **In `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapGeometryTests.cs` (`A06`):**
   - Collect `gateBlocker` (`I4_EncounterGate.GateBarrierRoot.GetComponent<BoxCollider2D>()`), plus all `SwitchDoor` barrier colliders (`Object.FindObjectsByType<SwitchDoor>().Select(sd => sd.DoorBarrierRoot.GetComponent<BoxCollider2D>()).ToArray()`).
   - Update outer physical envelope assertions to:
     - `minX == -102.5f`, `maxX == 234.5f`, `minY == -17.0f`, `maxY == 63.0f`.
   - Assert all `7` floor unions (`T0..T6`) via `GeometryAuditHelper.AssertFloorUnionCovers`:
     - `T0` (`topY = -16f`): `[76f, 234f]`
     - `T1` (`topY = 0f`): `[0f, 164f]`
     - `T2` (`topY = 8f`): `[18f, 164f]`
     - `T3` (`topY = 16f`): `[-102f, 9f]`
     - `T4` (`topY = 24f`): `[-102f, 92f]`
     - `T5` (`topY = 32f`): `[0f, 26f]`
     - `T6` (`topY = 40f`): `[38f, 140f]`
   - Assert all `17` inter-room openings (`11` core + `G<->M_low` `x=76, y=0..4`, `G<->N_high` `x=76, y=8..12`, `O<->P` `x=164, y=-16..-12`, `K<->Q_low` `x=-30, y=16..20`, `K<->R_high` `x=-30, y=24..28`, `L<->S` `x=38, y=24..28`) and the `3` `SwitchDoor` openings (`M_Vault` `x=150, y=0..4`, `P_SewerGate` `x=212, y=-16..-12`, `R_Armory` `x=-88, y=24..28` excluding their respective `SwitchDoor` barrier collider).
   - Assert all `7` climbable chains (`I1` `x=46, -0.5..8.5`, `I2` `x=68, -0.5..8.5`, `I6` `x=-24, 15.5..24.5`, `I7` `x=104, -0.5..8.5`, `I8` `x=118, -16.5..0.5`, `I9` `x=-66, 15.5..24.5`, `I10` `x=80, 23.5..40.5`).
2. **In `Assets/_Project/Features/NhaGiamGiengChuong/Tests/EditMode/MapWiringTests.cs` (`A07`, `A08`, `A10`, `A11`, `A12`, `A13`):**
   - **`A07`:**
     - `CameraFollow2D._bounds == new Rect(-104f, -20f, 340f, 86f)`.
     - `ProjectileSpawner._prewarm` contains **both** `"Projectile_Arrow"` and `"Projectile_Grenade"`.
     - `enemies.Length == 30` (asserting all `30` exact names & spawn coordinates from Spec `§5.3`, all dormant and wired to `_player`, `_projectiles`, `_vfx`).
     - `zones.Length == 11` (asserting all `11` zones `B1, G1, J1, E1, M1, N1, O1, P1, Q1, R1, T1` with exact bounds and member lists from Spec `§5.3`).
     - `I3_HealingChest`, `I4_EncounterGate`, `I5_ExitTrigger` remain unique (`FindObjectsByType<HealingChest>().Length == 1`, `FindObjectsByType<EncounterGate>().Length == 1`, `FindObjectsByType<ExitTrigger>().Length == 1`).
   - **`A08`:**
     - `solidTilemaps.Length == 9` (`9` room prefabs), all `10` core solid samples (including `(76.25f, 5.0f)` and `(-0.25f, 2.5f)`) plus `4` new wing solid samples (`(90f, -0.5f)`, `(120f, -16.5f)`, `(-60f, 15.5f)`, `(60f, 39.5f)`) covered, plus grounded landmark checks for `Art_A_Fountain` (`0f`), `Art_M_ArmoryDome1` (`0f`), `Art_O_MonsterDoor` (`-16f`), `Art_P_SewerExitDoor` (`-16f`), `Art_Q_JudasSeat` (`16f`), `Art_R_ArmoryDome` (`24f`), `Art_S_AlchemyBibli1` (`24f`), `Art_T_CourtyardArch` (`40f`).
   - **`A10`:**
     - `grids.Length == 9`, `tilemaps.Length >= 36`, `bgTilemaps.Length == 9`, `solidTilemaps.Length == 9`.
     - All `31` plank rows (`10` core + `21` wing plank rows) verified to use `woodenPlatform_*`.
     - All `19` rooms (`A..T`, using the exact `19` room `Rect`s from Spec `§5.2`) achieve `>= 50%` point-light coverage each (`95.2%–100.0%` measured) and `>= 75%` total map coverage (`98.1%` measured).
   - **`A11`:**
     - `effectors.Length == 31`, all with `useOneWay == true` and `surfaceArc == 160f`.
     - `fixtures.Length >= 80` (`88` `LanternFixture_R5` @ `24 PPU`, `Background/-9`, `LanternFlameCore_R5` `Background/-8`, `TorchEmbers` `Background/-7`).
     - `8/8` `GlowingMushroom_0..7` + `MushroomHalo_0..7` (`_DstBlend == 1`, `bounds.min.y > floorTop`) + `MushroomLight_0..7`.
     - `9` carved arch bays (`>= 45` `ArchSill_*`, `>= 70` `ArchJamb_*` @ `24 PPU`, `Background/-17`).
     - `8` multi-elevation 3-piece vertical `RoomBanner`s (`Art_C_Banner` `16.5f`, `Art_J_Banner` `21.8f`, `Art_E_Banner` `30.2f`, `Art_F_Banner` `37.2f`, `Art_N_Banner` `21.8f`, `Art_P_Banner` `-2.2f`, `Art_R_Banner` `37.8f`, `Art_T_Banner` `61.8f`) with `VertexSwayPin2D`.
     - `ParallaxFar_Backdrop` has `>= 20` (`5×4`) `T_Prison_ParallaxFar_Grayscale` panels, `GroundMist_R5` has `6` strips, `ShadowCasters_R5` has `6` casters.
   - **`A12`:**
     - Sub-floor dirt skirts verified at `(10, -2)`, `(10, -4)`, `(49, -4)`, `(66, -4)`, `(66, -18)`, `(120, -20)`, `(200, -20)`, `(-8, 13)`, `(-24, 13)`, `(-60, 10)`, `(40, 18)`, `(50, 20)`, `(65, 18)`, `(110, 35)`, AND verify **zero `TM_SolidVisual` tiles inside `Room O` upper interior `(80, -3, 0)` and `(104, -3, 0)` or `Room C` upper interior `(49, 17, 0)`**!
     - Bay-vs-Landmark AABB clearance check expanded across all `9` bays and all core + wing landmarks & banners (`Art_B_Arch1/Pillar/Arch2`, `Art_G_Alcove`, `Art_J_Arch`, `Art_E_Pillar1/2`, `Art_F_ExitArch`, `Art_A_Fountain`, `Art_M_Dummy/ArmoryDome1/ArmoryCorps/WeaponDeco`, `Art_N_Alcove1/2`, `Art_O_MonsterDoor/CellDoor1/2`, `Art_P_CorruptedDoor/SewerExitDoor`, `Art_Q_JudasSeat/Billot/SkeletonSpawn/SnakeLandmark`, `Art_R_ArmoryDome/BigSword`, `Art_S_AlchemyBibli1/2/AspectsDesk`, `Art_T_BigAlcove/Pillar1/Pillar2/CourtyardArch`, and all `8` `Art_*_Banner`s).
   - **Add `A13_FullScalePrisonersQuarters_ScrollsGoldTeleportersSwitchDoorsLoreAndDualExits`:**
     ```csharp
     [Test]
     public void A13_FullScalePrisonersQuarters_ScrollsGoldTeleportersSwitchDoorsLoreAndDualExits()
     {
         EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

         var player = Object.FindAnyObjectByType<PlayerController>();
         var hitStop = Object.FindAnyObjectByType<HitStop>();
         var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include);
         Assert.AreEqual(30, enemies.Length);
         Assert.AreEqual(5, enemies.Count(e => e.name.Contains("Grenadier")), "[A13/GrenadierCount] Expected 5 Enemy_Grenadier instances.");

         // 1. 2 Scrolls of Power + ScrollAura_R5_* under _icon + _glow pointing to Scroll glow + PowerScrollInteraction + ScrollUpgradeMenu + ScrollPickupPresentation
         var scrolls = Object.FindObjectsByType<PowerScrollPickup>(FindObjectsInactive.Include);
         Assert.AreEqual(2, scrolls.Length, "[A13/ScrollCount] Expected 2 PowerScrollPickup instances.");
         foreach (var scroll in scrolls)
         {
             var soScroll = new SerializedObject(scroll);
             var iconTr = soScroll.FindProperty("_icon").objectReferenceValue as Transform;
             var glowGo = soScroll.FindProperty("_glow").objectReferenceValue as GameObject;
             Assert.IsNotNull(iconTr, $"[A13/ScrollIcon/{scroll.name}]");
             Assert.IsNotNull(glowGo, $"[A13/ScrollGlow/{scroll.name}]");
             Assert.IsNotNull(glowGo.GetComponent<Light2D>(), $"[A13/ScrollGlowLight/{scroll.name}] _glow must remain wired to Scroll glow Light2D.");
             Assert.IsTrue(iconTr.GetComponentsInChildren<SpriteRenderer>(true).Any(sr => sr.name.StartsWith("ScrollAura_R5_", StringComparison.Ordinal)),
                 $"[A13/ScrollAura/{scroll.name}] ScrollAura_R5_* must be parented under _icon so Reveal(true) hides it.");
         }
         var scrollInteraction = Object.FindAnyObjectByType<PowerScrollInteraction>();
         Assert.IsNotNull(scrollInteraction, "[A13/PowerScrollInteraction]");
         Assert.AreEqual(scrollInteraction, new SerializedObject(player).FindProperty("_powerScrolls").objectReferenceValue, "[A13/Player.PowerScrolls]");
         var scrollMenu = Object.FindAnyObjectByType<DeadCells.HUD.ScrollUpgradeMenu>();
         var scrollPres = Object.FindAnyObjectByType<DeadCells.HUD.ScrollPickupPresentation>();
         Assert.IsNotNull(scrollMenu, "[A13/ScrollUpgradeMenu]");
         Assert.IsNotNull(scrollPres, "[A13/ScrollPickupPresentation]");
         var soMenu = new SerializedObject(scrollMenu);
         Assert.AreEqual(player, soMenu.FindProperty("_player").objectReferenceValue, "[A13/ScrollMenu.Player]");
         Assert.AreEqual(hitStop, soMenu.FindProperty("_gameplayTime").objectReferenceValue, "[A13/ScrollMenu.GameplayTime]");
         Assert.AreEqual(scrollPres, soMenu.FindProperty("_presentation").objectReferenceValue, "[A13/ScrollMenu.Presentation]");
         var soPres = new SerializedObject(scrollPres);
         Assert.IsNotNull(soPres.FindProperty("_playerSprite").objectReferenceValue, "[A13/ScrollPres.PlayerSprite]");
         Assert.IsNotNull(soPres.FindProperty("_gameplayHud").objectReferenceValue, "[A13/ScrollPres.GameplayHud]");
         Assert.IsNotNull(soPres.FindProperty("_audio").objectReferenceValue, "[A13/ScrollPres.Audio] Must point to non-null AudioSource on Scroll cinematic.");
         Assert.IsNotNull(soPres.FindProperty("_pose").objectReferenceValue, "[A13/ScrollPres.Pose]");

         // 2. GoldWallet + EnemyGoldDrops (all 30 enemies registered) + GoldHud + GoldPickup Vfx sorting
         var wallet = Object.FindAnyObjectByType<DeadCells.Currency.GoldWallet>();
         var goldDrops = Object.FindAnyObjectByType<DeadCells.Currency.EnemyGoldDrops>();
         var goldHud = Object.FindAnyObjectByType<DeadCells.HUD.GoldHud>();
         Assert.IsNotNull(wallet, "[A13/GoldWallet]");
         Assert.IsNotNull(goldDrops, "[A13/EnemyGoldDrops]");
         Assert.IsNotNull(goldHud, "[A13/GoldHud]");
         var soDrops = new SerializedObject(goldDrops);
         Assert.AreEqual(wallet, soDrops.FindProperty("_wallet").objectReferenceValue);
         Assert.AreEqual(30, soDrops.FindProperty("_enemies").arraySize, "[A13/GoldDropsEnemies] All 30 enemies must be registered.");
         var pickupPrefab = soDrops.FindProperty("_pickupPrefab").objectReferenceValue as DeadCells.Currency.GoldPickup;
         Assert.IsNotNull(pickupPrefab, "[A13/GoldPickupPrefab]");
         Assert.AreEqual("VFX", pickupPrefab.GetComponentInChildren<SpriteRenderer>(true).sortingLayerName, "[A13/GoldPickupSorting]");

         // 3. 3 SwitchDoors (SD_1_EastVault, SD_2_SewerExit, SD_3_WestArmory)
         var switchDoors = Object.FindObjectsByType<SwitchDoor>(FindObjectsInactive.Include);
         Assert.AreEqual(3, switchDoors.Length, "[A13/SwitchDoorCount]");
         foreach (var sd in switchDoors)
         {
             Assert.IsNotNull(sd.DoorBarrierRoot?.GetComponent<BoxCollider2D>(), $"[A13/SwitchDoorBarrier/{sd.name}]");
             Assert.IsNotNull(sd.SwitchTrigger, $"[A13/SwitchDoorTrigger/{sd.name}]");
             Assert.IsNotNull(sd.PromptIndicator, $"[A13/SwitchDoorPrompt/{sd.name}]");
         }

         // 4. 6 TeleportGates forming 3 bi-directional pairs with discovery gating
         var tpGates = Object.FindObjectsByType<TeleportGate>(FindObjectsInactive.Include);
         Assert.AreEqual(6, tpGates.Length, "[A13/TeleportGateCount]");
         foreach (var tp in tpGates)
         {
             Assert.IsNotNull(tp.PairedGate, $"[A13/TeleportPair/{tp.name}]");
             Assert.AreEqual(tp, tp.PairedGate.PairedGate, $"[A13/TeleportReciprocal/{tp.name}]");
         }
         var tp1 = tpGates.Single(t => t.name == "TP_1_EastVault");
         var tp3 = tpGates.Single(t => t.name == "TP_3_SewerExit");
         Assert.IsFalse(new SerializedObject(tp1).FindProperty("_startsDiscovered").boolValue, "[A13/TP1StartsUndiscovered]");
         Assert.IsFalse(new SerializedObject(tp3).FindProperty("_startsDiscovered").boolValue, "[A13/TP3StartsUndiscovered]");

         // 5. 1 LoreTablet (I16_LoreTablet in Room S) & 1 SecondaryBiomeExitTrigger (I15_SewerExitTrigger in Room P)
         var lore = Object.FindAnyObjectByType<LoreTablet>();
         Assert.IsNotNull(lore, "[A13/LoreTablet]");
         var sewerExit = Object.FindAnyObjectByType<SecondaryBiomeExitTrigger>();
         Assert.IsNotNull(sewerExit, "[A13/SewerExit]");
         Assert.AreEqual("Toxic Sewers", sewerExit.BiomeName);
         Assert.IsNotNull(sewerExit.SwitchDoor);
         Assert.IsNotNull(sewerExit.FirstGuardian);
         Assert.IsNotNull(sewerExit.SecondGuardian);
     }
     ```

- [ ] **Step 2: Run `MapGeometryTests` and `MapWiringTests` to verify `A06`–`A13` fail before scene assembly update**

Run via `rtk proxy`:
```powershell
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform EditMode -testFilter 'MapGeometryTests|MapWiringTests' -testResults 'D:\thietkegame\Logs\r6-t3-red.xml' -logFile 'D:\thietkegame\Logs\r6-t3-red.log' -job-worker-count 1"
```
Expected: Fails because `BuildScene` has not yet instantiated the 4 new wing prefabs, `25` new enemies, `7` new zones, `SwitchDoor`s, `TeleportGate`s, `LoreTablet`, `SecondaryBiomeExitTrigger`, Scrolls, and Gold economy.

- [ ] **Step 3: Implement Full-Scale `BuildScene` and `BuildRound5SceneAtmosphereAndLighting` in `NhaGiamGiengChuongBuilder.cs`**

1. **Ensure `GoldPickup.prefab` sorting layer (`EnsureGoldPickupSortingLayer()`):**
   - Load `Assets/_Project/Features/Currency/Prefabs/GoldPickup.prefab` via `PrefabUtility.LoadPrefabContents`, set `Coin` child `SpriteRenderer.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX")` and `sortingOrder = Mathf.Max(sr.sortingOrder, 5)`, and save back via `PrefabUtility.SaveAsPrefabAsset` if needed.
2. **In `BuildScene`:**
   - Instantiate all `9` room prefabs under `Rooms`: `CD`, `AB`, `G`, `HJK`, `ELF`, `W1_EastCourtyard`, `W2_SewerCistern`, `W3_WestBastion`, `U1_UpperPromenade`.
   - Expand `AmbientVFX` box scales to cover `340u × 86u` (`dustGo` at `(66f, 23f, 0f)` scale `(336f, 80f, 0.1f)`, `dripGo` at `(66f, 23f, 0f)` scale `(320f, 76f, 0.1f)`).
   - Add `Projectile_Grenade.prefab` (`Assets/_Project/Features/Enemies/Prefabs/Projectile_Grenade.prefab`) as the 2nd entry in `ProjectileSpawner._prewarm` (`arraySize = 2`).
   - Update `CameraFollow2D._bounds` to `new Rect(-104f, -20f, 340f, 86f)`.
   - Spawn all `30` dormant enemies (`5` core + `25` wing enemies at exact coordinates from Spec `§5.3`) and create all `11` `EncounterZone`s (`B1, G1, J1, E1, M1, N1, O1, P1, Q1, R1, T1`).
   - Build the `3` `SwitchDoor` instances (`SD_1_EastVault`, `SD_2_SewerExit`, `SD_3_WestArmory`), `6` `TeleportGate` instances (`TP_0_Spawn` ↔ `TP_1_EastVault`, `TP_2_CisternJunction` ↔ `TP_3_SewerExit`, `TP_4_WestCatacombs` ↔ `TP_5_HighBastion`, each dressed with `tpStoneUp/Down_00` + floating `tpRock_0..3_00`), `1` `LoreTablet` (`I16_LoreTablet` at `(72, 24)` in Room S), and `1` `SecondaryBiomeExitTrigger` (`I15_SewerExitTrigger` at `(224, -15)` in Room P wired to `SD_2_SewerExit`, `Enemy_Shield_P1`, `Enemy_Archer_P1`).
   - **Before calling `BuildRound5SceneAtmosphereAndLighting`**, open `Assets/_Project/Scenes/Main_Level.unity` via `EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main_Level.unity", OpenSceneMode.Additive)` and locate nested source objects inside `mainLevelScene` by recursive transform search across `mainLevelScene.GetRootGameObjects()` (`"Scroll of Power - Entrance"` under `Prison/Power Scrolls`, `"Power Scroll HUD"` at root, `"Scroll cinematic"` under `Systems/Combat and Pickup Feedback`, and `"Gold HUD"` under `PlayerHUD`):
     - Unparent cloned objects (`transform.SetParent(null, true)`) before calling `SceneManager.MoveGameObjectToScene(clone, scene)`.
     - Position `I11_Scroll_EastCourtyard` at `(156f, 0f, 0f)` (`>= 2.0u` clear of `TP_1_EastVault` at `(161f, 0f)`) and `I12_Scroll_WestArmory` at `(-95f, 24f, 0f)` under a new `Power Scrolls` container carrying `PowerScrollInteraction`.
     - On each cloned `PowerScrollPickup`, remove `ScrollAura_R4_*`, create `ScrollAura_R5_0` / `ScrollAura_R5_1` (`Mat_Emissive_Scroll`, `VFX/-2`, `scale=(0.46, 0.46, 1)`) parented directly under `_icon` (`Floating scroll`), and **keep `_glow` pointing to the existing `Scroll glow` (`Light2D`) child** so `Reveal(true)` disables both `_icon` (with `ScrollAura_R5_*`) and `Scroll glow` (`Light2D`) simultaneously!
     - Attach an `AudioSource` (`playOnAwake = false`, `spatialBlend = 0f`) directly onto the cloned `Scroll cinematic` GameObject.
     - Rebind `ScrollUpgradeMenu` (`_player = playerCtrl`, `_gameplayTime = hitStop`, `_presentation = clonedPowerScrollHud.GetComponent<ScrollPickupPresentation>()`) and `ScrollPickupPresentation` (`_playerSprite = playerGo.GetComponentInChildren<SpriteRenderer>()`, `_gameplayHud = playerHudGo.GetComponent<Canvas>()`, `_audio = clonedScrollCinematic.GetComponent<AudioSource>()`, and `_pose/_scroll/_halo/_rays/_stars/_light` pointing to the matching children of `clonedScrollCinematic`).
     - Wire `PowerScrollInteraction._scrolls = [scrollEast, scrollWest]` and `PlayerController._powerScrolls = scrollInteraction`.
     - Create `Run Gold` (`GoldWallet` + `EnemyGoldDrops` wired to `_wallet`, `_player = playerHealth`, `_pickupPrefab = GoldPickup.prefab`, `_enemies = all 30 enemies`, `_goldPerEnemy = 20`), parent the cloned `Gold HUD` under `playerHudGo.transform` (`worldPositionStays = false`), and wire `GoldHud._wallet = goldWallet`.
     - Close `Main_Level.unity` via `EditorSceneManager.CloseScene(mainLevelScene, true)` without saving `Main_Level.unity`.
   - In `BuildRound5SceneAtmosphereAndLighting` (called **after** `Main_Level` additive cloning so its final `Light2D` pass strips `ParallaxFar`, `ParallaxMid`, and `Foreground` from `Scroll glow` and `Flash light` too):
     - Expand `ParallaxFar_Backdrop` to a `5×4` grid (`cols = {-64f, -32f, 0f, 32f, 64f}`, `rows = {-17.3333f, 4.0f, 25.3333f, 46.6667f}` = `20` panels spaced at exact `21.3333u = 512px / 24 PPU` vertical intervals with `0` row overlap) and `ParallaxMid_Alcoves` to `4` vertical tiers (`tierYs = {-13.0f, 2.6f, 22.0f, 44.0f}`) across `chunkOffsets = {-32f, 0f, 32f}`.
     - Expand `GlowingMushroom` to `8` clusters (`#0..#7` per Spec `§5.5 #3`), `GroundMist_R5` to `6` strips (`Mist_RoomC/G/K/O/P/Q`), and `ShadowCasters_R5` to `6` casters (`Caster_StairsH`, `Caster_FloorG_Upper`, `Caster_FloorF_CeilingE`, `Caster_FloorN_West`, `Caster_FloorR_East`, `Caster_FloorT_West`).

- [ ] **Step 4: Run `BuildAll()` and execute the full EditMode test suite (`NhaGiamGiengChuong.Tests.EditMode` + `DeadCells.Core.Tests`)**

Run via `rtk proxy`:
```powershell
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.BuildAll -quit -logFile 'D:\thietkegame\Logs\r6-t3-build.log' -job-worker-count 1"
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform EditMode -testFilter 'DeadCells.NhaGiamGiengChuong.Tests' -testResults 'D:\thietkegame\Logs\r6-ng-edit-green.xml' -logFile 'D:\thietkegame\Logs\r6-ng-edit-green.log' -job-worker-count 1"
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform EditMode -testFilter 'DeadCells.Core.Tests' -testResults 'D:\thietkegame\Logs\r6-core-green.xml' -logFile 'D:\thietkegame\Logs\r6-core-green.log' -job-worker-count 1"
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
```
Expected:
- `Logs/r6-ng-edit-green.xml`: **`37/37` Passed** (`17` Asset/Geometry/Wiring tests `A01`–`A05`, `A05b`–`A05e`, `A06`–`A13` + `20` Unit tests `U01`–`U20`), `0` Failed.
- `Logs/r6-core-green.xml`: **`29/29` Passed**, `0` Failed.
- `rtk git diff -- Assets/_Project/Scenes/Main_Level.unity` is empty (`0` diff).

- [ ] **Step 5: Sync CodeGraph**

```powershell
rtk proxy cmd /c codegraph sync
```

---

## Task 4: PlayMode Regression & Expansion Suite (`P01`–`P08`) + 9-Frame Real RTX 2050 GPU Capture Verifier (`CaptureRound6GpuVerificationFrames`)

**Files:**
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/RunPlayTests.cs` (`P07`)
- Create: `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/ExpansionPlayTests.cs` (`P08`)
- Modify: `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs` (`CaptureRound6GpuVerificationFrames`)

**Interfaces:**
- Consumes: `NhaGiamGiengChuong.unity`, `SwitchDoor`, `TeleportGate`, `LoreTablet`, `SecondaryBiomeExitTrigger`, `PowerScrollPickup`, `GoldWallet`, `EnemyGoldDrops`, `PlayModeTestHelper`.
- Produces:
  - `ExpansionPlayTests.P08_SwitchDoorTeleportGateLoreTabletGoldDropAndSecondarySewerExitWinFlow`
  - `NhaGiamGiengChuongBuilder.CaptureRound6GpuVerificationFrames()` capturing `9` `1280×720` sRGB PNGs across all `9` room prefabs (`gpu_r6_ng_01_room_ab.png` .. `gpu_r6_ng_09_room_u1.png`).

- [ ] **Step 1: Update `P07` in `RunPlayTests.cs` and create `ExpansionPlayTests.cs` (`P08`)**

1. In `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/RunPlayTests.cs` (`P07`, lines 302–313), update:
   ```csharp
   var enemies2 = UnityEngine.Object.FindObjectsByType<EnemyController>();
   Assert.AreEqual(30, enemies2.Length, $"[P07/{label}/Reset] All 30 enemies restored.");
   ...
   var zones2 = UnityEngine.Object.FindObjectsByType<EncounterZone>();
   Assert.AreEqual(11, zones2.Length);
   Assert.IsTrue(zones2.All(z => !z.IsActivated), $"[P07/{label}/Reset] All 11 zones dormant.");
   ```
2. Create `Assets/_Project/Features/NhaGiamGiengChuong/Tests/PlayMode/ExpansionPlayTests.cs` (`P08`):
   ```csharp
   using System.Collections;
   using System.Linq;
   using DeadCells.Combat;
   using DeadCells.Currency;
   using DeadCells.Enemies;
   using DeadCells.Player;
   using NUnit.Framework;
   using UnityEngine;
   using UnityEngine.InputSystem;
   using UnityEngine.TestTools;

   namespace DeadCells.NhaGiamGiengChuong.Tests.Play
   {
       public sealed class ExpansionPlayTests : InputTestFixture
       {
           public override void TearDown()
           {
               Time.timeScale = 1f;
               base.TearDown();
           }

           [UnityTest]
           public IEnumerator P08_SwitchDoorTeleportGateLoreTabletGoldDropAndSecondarySewerExitWinFlow()
           {
               yield return PlayModeTestHelper.LoadLevelScene();

               var run = Object.FindAnyObjectByType<LevelRunController>();
               var player = Object.FindAnyObjectByType<PlayerController>();
               var wallet = Object.FindAnyObjectByType<GoldWallet>();
               var panel = Object.FindAnyObjectByType<ResultPanel>();
               var lore = Object.FindAnyObjectByType<LoreTablet>();
               var sewerExit = Object.FindAnyObjectByType<SecondaryBiomeExitTrigger>();
               var switchDoors = Object.FindObjectsByType<SwitchDoor>();
               var tpGates = Object.FindObjectsByType<TeleportGate>();

               yield return PlayModeTestHelper.WaitForPlaying(run);

               // 1. Teleport discovery gating at start: TP_0_Spawn cannot warp to undiscovered TP_1_EastVault
               var tp0 = tpGates.Single(t => t.name == "TP_0_Spawn");
               var tp1 = tpGates.Single(t => t.name == "TP_1_EastVault");
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(15.5f, 0f));
               yield return new WaitForFixedUpdate();
               yield return null;
               Assert.IsTrue(tp0.IsDiscovered);
               Assert.IsFalse(tp1.IsDiscovered);
               Assert.IsFalse(tp0.TryTeleport(), "[P08/UndiscoveredReject] TP_0 must not warp to undiscovered TP_1.");

               // 2. Wake Zone M1, defeat Grenadier_M1 -> verifies GoldPickup drops on VFX sorting layer & credits GoldWallet
               Assert.AreEqual(0, wallet.Balance, "[P08/InitialGold]");
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(82f, 0f));
               yield return new WaitForFixedUpdate();
               yield return null;
               var grenadierM1 = Object.FindObjectsByType<EnemyController>().Single(e => e.name == "Enemy_Grenadier_M1");
               Assert.IsTrue(grenadierM1.enabled, "[P08/ZoneM1] Enemy_Grenadier_M1 must wake in Zone M1.");
               Assert.IsTrue(grenadierM1.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
               yield return null;
               var droppedCoin = Object.FindAnyObjectByType<GoldPickup>();
               Assert.IsNotNull(droppedCoin, "[P08/GoldDrop] Defeating enemy must spawn GoldPickup.");
               Assert.AreEqual("VFX", droppedCoin.GetComponentInChildren<SpriteRenderer>().sortingLayerName, "[P08/CoinVfxLayer]");
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(92f, 3.5f));
               yield return PlayModeTestHelper.WaitUntil(() => wallet.Balance >= 20, 2.5f, "[P08/GoldCollected] GoldPickup must credit GoldWallet.");
               Assert.GreaterOrEqual(wallet.Balance, 20);

               // 3. Open SwitchDoor SD_1_EastVault at (144, 0), enter vault at (161, 0) to discover TP_1, warp back to TP_0 (15.5, 0)
               var sd1 = switchDoors.Single(d => d.name == "SD_1_EastVault");
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(144f, 0f));
               yield return new WaitForFixedUpdate();
               yield return null;
               Assert.IsTrue(sd1.TryInteract(), "[P08/OpenSD1]");
               Assert.IsFalse(sd1.DoorBarrierRoot.activeInHierarchy);

               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(161f, 0f));
               yield return new WaitForFixedUpdate();
               yield return null;
               Assert.IsTrue(tp1.IsDiscovered, "[P08/DiscoverTP1]");
               Assert.IsTrue(tp1.TryTeleport(), "[P08/TeleportTP1ToTP0]");
               Assert.AreEqual(15.5f, PlayModeTestHelper.GetPlayerFeet(player).x, 0.25f);

               // 4. Inspect LoreTablet I16 in Room S at (72, 24) and auto-close on walk away
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(72f, 24f));
               yield return new WaitForFixedUpdate();
               yield return null;
               Assert.IsTrue(lore.TryInteract(), "[P08/LoreOpen]");
               Assert.IsTrue(lore.IsReading);
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(50f, 24f));
               yield return new WaitForFixedUpdate();
               yield return null;
               Assert.IsFalse(lore.IsReading, "[P08/LoreAutoClose]");

               // 5. Toxic Sewers secondary exit in Room P: wake P1, kill Shield_P1 + Archer_P1, open SD_2_SewerExit, trigger win at (224, -15)
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(180f, -16f));
               yield return new WaitForFixedUpdate();
               yield return null;
               var enemies = Object.FindObjectsByType<EnemyController>();
               Assert.IsTrue(enemies.Single(e => e.name == "Enemy_Shield_P1").GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
               Assert.IsTrue(enemies.Single(e => e.name == "Enemy_Archer_P1").GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
               yield return null;

               var sd2 = switchDoors.Single(d => d.name == "SD_2_SewerExit");
               PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(206f, -16f));
               yield return new WaitForFixedUpdate();
               yield return null;
               Assert.IsTrue(sd2.TryInteract(), "[P08/OpenSD2]");
               Assert.IsTrue(sewerExit.IsUnlocked, "[P08/SewerExitUnlocked]");

               Assert.IsTrue(sewerExit.TrySubmitForCollider(player.GetComponent<BoxCollider2D>(), Time.frameCount), "[P08/SewerWinSubmit]");
               yield return PlayModeTestHelper.WaitUntil(() => run.IsEnded && panel.IsVisible, 2f, "[P08/SewerWinSettle]");
               Assert.AreEqual(LevelOutcome.Win, run.Outcome);
               Assert.AreEqual(ResultPanel.WinTitle, panel.TitleText.text);
           }
       }
   }
   ```

- [ ] **Step 2: Add `CaptureRound6GpuVerificationFrames()` to `NhaGiamGiengChuongBuilder.cs`**

In `Assets/_Project/Features/NhaGiamGiengChuong/Editor/NhaGiamGiengChuongBuilder.cs`, add `public static void CaptureRound6GpuVerificationFrames()` (reusing the D3D12 HDR `RenderTextureFormat.ARGBHalf` + `Light2D.UpdateMesh` + `ShadowCaster2D.Update` + `VertexSwayPin2D.ApplyPropertyBlock` pipeline from `CaptureRound5GpuVerificationFrames`) capturing all `9` room prefabs (`1280×720` sRGB PNGs) into `R6_GPU_CAPTURE_DIR` (default `Logs/r6-gpu-captures`):
1. `gpu_r6_ng_01_room_ab.png` at `cam=(18f, 3.0f, -10f), ortho=5.8f` (Room A/B: Fountain, `TP_0_Spawn`, Bay #0, sub-floor dirt skirt)
2. `gpu_r6_ng_02_room_cd.png` at `cam=(45f, 6.5f, -10f), ortho=6.5f` (Room C/D: Bay #1, `Art_C_Banner`, `GlowingMushroom_0`, `Mist_RoomC`)
3. `gpu_r6_ng_03_room_g.png` at `cam=(66f, 5.5f, -10f), ortho=6.2f` (Room G: Bay #2, `I3_HealingChest`, `G<->M_low` & `G<->N_high` doorways)
4. `gpu_r6_ng_04_room_hjk.png` at `cam=(-8f, 19.5f, -10f), ortho=6.0f` (Room H/J/K: Bay #3, `Art_J_Arch`, `Art_J_Banner`, `K<->Q/R` doorways)
5. `gpu_r6_ng_05_room_elf.png` at `cam=(8f, 29.5f, -10f), ortho=6.5f` (Room E/L/F: Bay #4, `Art_E_Banner`, `Art_F_ExitArch`, `L<->S` doorway)
6. `gpu_r6_ng_06_wing_w1_mn.png` at `cam=(122f, 11.5f, -10f), ortho=7.0f` (Rooms M/N: Bay #5, `Art_N_Banner`, `Enemy_Grenadier`, `SD_1_EastVault`, `I11_Scroll_EastCourtyard`)
7. `gpu_r6_ng_07_wing_w2_op.png` at `cam=(198f, -10.5f, -10f), ortho=6.8f` (Rooms O/P: `Mist_RoomP`, `SD_2_SewerExit`, `Art_P_SewerExitDoor` + `Art_P_ExitGlow`, `TP_3_SewerExit`, sub-floor skirt)
8. `gpu_r6_ng_08_wing_w3_qr.png` at `cam=(-66f, 27.5f, -10f), ortho=7.0f` (Rooms Q/R: Bay #7, `Art_R_Banner`, `SD_3_WestArmory`, `I12_Scroll_WestArmory`, `Mist_RoomQ`)
9. `gpu_r6_ng_09_wing_u1_st.png` at `cam=(72f, 38.0f, -10f), ortho=7.0f` (Rooms S/T: `Art_S_AlchemyBibli`, `I16_LoreTablet`, `I10_Chain_ST`, Bay #8, `Art_T_Banner`)

Assert in logs that every frame has `clip>=240 <= 0.500%`, `shaderErrors == 0`, `URP_PreFlight 5/5 True`, and `defaultMaterialRenderers == 0`.

- [ ] **Step 3: Run Full PlayMode Test Suite (`P01`–`P08`) and Real RTX 2050 GPU Capture Verifier**

Run via `rtk proxy`:
```powershell
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\thietkegame' -runTests -testPlatform PlayMode -testFilter 'DeadCells.NhaGiamGiengChuong.Tests.Play' -testResults 'D:\thietkegame\Logs\r6-ng-play-green.xml' -logFile 'D:\thietkegame\Logs\r6-ng-play-green.log' -job-worker-count 1"
rtk proxy pwsh -NoProfile -Command "$p = Get-Process Unity -ErrorAction SilentlyContinue; if ($p) { throw 'Unity already running' }; $env:DOTNET_GCServer='0'; & 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -projectPath 'D:\thietkegame' -executeMethod DeadCells.NhaGiamGiengChuong.Editor.NhaGiamGiengChuongBuilder.CaptureRound6GpuVerificationFrames -quit -logFile 'D:\thietkegame\Logs\r6-gpu-ng.log' -job-worker-count 1"
rtk git checkout -- ProjectSettings/TimeManager.asset ProjectSettings/ProjectSettings.asset
```
Expected:
- `Logs/r6-ng-play-green.xml`: **`8/8` Passed (`P01`–`P08`)**, `0` Failed.
- Total test count across project: **`29` Core EditMode + `37` NG EditMode + `8` NG PlayMode = `74/74` Passed (`100%`)**.
- `Logs/r6-gpu-ng.log`: `URP_PreFlight 5/5 True`, `defaultMaterialRenderers=0`, `9/9` PNGs captured with `clip>=240 <= 0.5%`.
- `0` `CS0618`, `0` `CS0414`, `0` `warning CS`, `0` `error CS`, `0` `Shader error`, `0` `Assertion failed` across all Round 6 logs.

- [ ] **Step 4: Sync CodeGraph & Final Hygiene Verification**

```powershell
rtk proxy cmd /c codegraph sync
rtk git diff --stat -- ProjectSettings/
```
Expected: `0` diff on `ProjectSettings/TimeManager.asset` and `ProjectSettings/ProjectSettings.asset`.
