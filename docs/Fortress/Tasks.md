# Gothic Fortress — Task list và hợp đồng phối hợp Codex / Antigravity

Ngày chuẩn bị: 2026-11-14. Contract: **FORTRESS-1**. Đây là chỉ dẫn cho hai **orchestrator**, mỗi bên tự giao task cho subagent; không phải giao toàn bộ backlog cho một worker.

## 1. Mục tiêu, phạm vi và quyền thực thi

Hoàn thành framework môi trường theo `# MASTER PROMPT — COMPLETE UNITY 2D.md`, mục 0–28: import, pixel rendering, terrain/RuleTile, palette, animation, prefab, physics, interaction, platform, destruction, trap, lighting, VFX, audio hook, room builder, dashboard, demo và tài liệu/validation.

Trong lượt chuẩn bị chỉ tạo tài liệu, bootstrap và hai bản sao cô lập; không sửa gameplay, asset hoặc cấu hình Unity ở project gốc. Hai goal prompt là chỉ dẫn triển khai **khi người dùng gửi cho platform tương ứng**. Chưa gửi prompt không có nghĩa triển khai đã bắt đầu.

Khi chạy goal prompt: được sửa code/asset/config, dùng Editor, compile, EditMode/PlayMode test và quan sát demo **trong checkout của mình, đúng ownership bên dưới**. Không publish, push remote, cài phần mềm toàn máy, nâng Unity, chấp nhận điều khoản, sửa global config hoặc thay gameplay ngoài môi trường. Không tự tác giả/chỉnh `.meta`; Unity sinh metadata mới, metadata có sẵn được giữ nguyên khi sao chép/chuyển commit.

Đọc `AGENTS.md`, RTK.md được tham chiếu, `docs/ProjectConfig.yaml`, master prompt và tài liệu này trước. Tìm AGENTS.md con trước khi sửa. Chỉ dùng bằng chứng thực tế. Không làm lại player movement hoặc tạo lại enemy prefab đã được người dùng xác nhận. Bố cục map cuối do người dùng duyệt; chưa có ảnh thì không tuyên bố tái dựng đúng ảnh.

Mỗi task bắt đầu bằng kiểm tra implementation + wiring liên quan, chọn **reuse / extend / new / blocked-input** có chứng cứ. Danh sách này không khẳng định tính năng đang thiếu. Estimate chưa chốt. ID dưới đây là mã nội bộ, không phải Jira key.

## 2. Bằng chứng nền và lựa chọn platform

### Những gì đã kiểm tra

- `ProjectSettings/ProjectVersion.txt`: Unity **6000.5.0f1**. Unity CLI liệt kê Editor tại `D:/Unity/6000.5.0f1/Editor/Unity.exe`.
- `Packages/manifest.json`: Tilemap Extras 8.0.3, Input System 1.19.0, URP 17.5.0, Test Framework 1.7.0, Pipeline 1.0.0. Khi thực thi phải đối chiếu thêm lockfile và API thực tế; không tự đổi version.
- `GothicDungeonImporter.cs:35–189`: có import theo manifest, tạo Tile/Palette, Verify; các đoạn source đã được đọc qua CodeGraph, chưa chạy importer.
- `Health.cs:43–58`, `IDamageable.cs:3–10`, `DamageInfo.cs:8–32`: đã có damage contract. Không tạo hệ HP song song chỉ vì prompt mẫu không biết project có sẵn.
- `CameraFollow2D.cs:57–81`: camera follow/smoothing đã có; pixel-perfect phải phối hợp với nó.
- `TagManager.asset`: Ground=6, Player=7, Enemy=8, OneWayPlatform=9, Climbable=10; Sorting Layer hiện đọc được chỉ có Default. Không đổi ID layer hiện hữu.
- Đã kiểm tra danh sách bốn palette GothicDungeon và đọc phần đầu import manifest; chưa xem từng sprite hoặc xác nhận family 47-tile đầy đủ.
- Source working tree có thay đổi sẵn có ở `.gitignore`, `AGENTS.md`, URP global settings, package, build settings và nhiều file untracked. Không reset/stash/commit chúng trong repo gốc.
- Lệnh thực tế: Unity CLI `1.0.0-beta.13`; `agy --version` = `1.5.9`. Codex có trên PATH; phiên hiện tại có công cụ subagent. Chưa kiểm chứng đăng nhập, tool và subagent của phiên Antigravity.
- `unity status --project-path D:/thietkegame --json` trả `STATUS_NO_INSTANCES`. Không suy ra thiếu package: manifest đã có Pipeline; chỉ kết luận chưa có kết nối Editor được CLI tìm thấy trong lần kiểm tra.

### Vì sao chia như sau

| Platform | Căn cứ khả năng | Phần được giao | Giới hạn cần bù |
|---|---|---|---|
| Codex | Tài liệu chính thức mô tả subagents, tách ngữ cảnh và làm song song; phiên này có shell/code intelligence/subagent tools | Runtime, state machine, tích hợp damage/input, kiểm thử, kiểm tra ownership và tích hợp cuối | Tool có sẵn không chứng minh Editor đã kết nối; bắt buộc chạy test thực và xem demo. Không tự coi khả năng xử lý hình kém hơn Antigravity. |
| Antigravity | Tài liệu chính thức mô tả project/worktree, subagent workflow, artifact và visual screenshot/review | Asset audit, import/animation/RuleTile tools, prefab hình ảnh, lighting/VFX, room authoring, preview | Browser screenshot không tự chứng minh Unity Scene đúng. Phiên thực thi phải xác nhận tool điều khiển Unity và subagents; không giả định browser điều khiển được Unity. |

Đây là **phân công thực dụng theo loại đầu ra**, không phải benchmark hoặc kết luận tuyệt đối về độ thông minh/chất lượng của hai platform. Model, quyền, tool và quota thực tế có thể khác. Không pin model không có trên allowlist; không dùng model khác nếu user/rule không cho phép.

Nguồn đã truy cập 2026-11-14:

- Codex subagents: https://developers.openai.com/codex/multi-agent (chuyển tới https://learn.chatgpt.com/docs/agent-configuration/subagents).
- Antigravity: https://antigravity.google/docs/getting-started — mô tả các bề mặt app/CLI/IDE, worktree, artifacts và các workflow subagent. Tài liệu hiện gọi CLI là **agy**, không phải `antigravity`.
- Quy tắc local: `AGENTS.md`, `docs/UnityConnection.md`, `.agents/skills/unity-cli/SKILL.md`.

## 3. Môi trường cô lập đã chuẩn bị và cách mở

Bootstrap: `tools/fortress/prepare_workspaces.py`. Script dùng Python standard library, gọi Git qua RTK, không cài dependency. Hai local repo có cùng input snapshot và cùng baseline commit; không phải worktree của repo gốc.

| Mục | Đường dẫn |
|---|---|
| Project gốc, chỉ đọc khi hai platform làm việc | `D:/thietkegame` |
| Codex checkout | `D:/thietkegame/.fortress-workspaces/codex` |
| Antigravity checkout | `D:/thietkegame/.fortress-workspaces/antigravity` |
| Snapshot manifest + baseline commit + ready flag | `D:/thietkegame/.fortress-workspaces/preparation.json` |

Đọc `ready: true` và baseline SHA từ manifest trước khi mở task. Script từ chối ghi đè nếu `.fortress-workspaces` đã có, không tự xóa bản chuẩn bị cũ. Nếu bị gián đoạn, giữ thư mục để kiểm tra, không coi ready. Trước khi agents sửa gì có thể kiểm tra:

```powershell
rtk proxy python -B -X utf8 tools/fortress/prepare_workspaces.py --verify
```

**Không dùng `--verify` để đánh giá tiến độ sau khi agents đã sửa:** command này cố ý chỉ chấp nhận snapshot nguyên vẹn trước lúc bắt đầu.

Đã sao chép các thư mục project đầu vào `Assets`, `Packages`, `ProjectSettings`, `SourceAssets`, `docs`, `.agents`, bootstrap và file quy tắc gốc. Bao gồm asset cục bộ bị git-ignore trong Assets, để không âm thầm mất reference. Không sao chép `.git` gốc, `.codex/config.toml` đang pin project gốc, Library, Logs, UserSettings, caches đồ thị hoặc credentials. `.meta` có sẵn chỉ được sao chép nguyên byte. Các asset ignore vẫn chỉ nằm cục bộ, không tự push lên dịch vụ nào.

1. Mở **Codex project** đúng checkout Codex và gửi `Goal-Codex.md`.
2. Mở **Antigravity project** đúng checkout Antigravity và gửi `Goal-Antigravity.md`.
3. Chọn chế độ làm việc trực tiếp trong checkout đã chuẩn bị. Không để app tự tạo thêm worktree từ HEAD khác, vì sẽ bỏ sót input local/git-ignore.
4. Mỗi bên đọc/đối chiếu path của mọi MCP/Editor tool. Không dùng server Unity global nếu nó đang pin root hoặc checkout bên kia. CLI fallback phải luôn có `--project-path` riêng.
5. Preflight chỉ đọc:

```powershell
# Codex, thay bằng path Antigravity tương ứng cho bên kia
rtk proxy unity status --project-path D:/thietkegame/.fortress-workspaces/codex --json
```

6. Khi chưa có instance, goal prompt đã cho phép mở đúng Editor trong checkout và đợi import; đọc skill CLI trước. Nếu vướng license/điều khoản/auth, người dùng xử lý bước riêng tư đó; agent tiếp tục phần không phụ thuộc, không tự chấp nhận.
7. Mỗi project chỉ một writer điều khiển Editor tại một thời điểm: **orchestrator** hoặc worker được lease rõ ràng. Workers khác chỉ viết code riêng/đọc log; không cùng chạy importer, recompile, Play Mode hay đổi scene.
8. Hai Editor chỉ mở đồng thời nếu license/tài nguyên cho phép; nếu không, xếp lịch Editor mà vẫn viết code song song. Không đóng Editor của người dùng để lấy slot.

Nếu `.codegraph` không có trong bản sao thì dùng rg/read; không tạo index mới nếu chưa có quyền. Global CodeGraph pin root chỉ được dùng đọc baseline, không coi đó là source hiện tại của checkout sau sửa. Không ghi graph output vào Assets.

## 4. Ownership đóng băng — không cùng sửa một vùng

Các đường dẫn dưới đây tính tương đối trong **checkout riêng**. Đường dẫn mới là vùng dự kiến được tạo, không phải tuyên bố chúng đã tồn tại. Mọi path không được cấp quyền ở bảng là **read-only**; muốn mở rộng phải có quyết định owner bằng handoff, không tự sửa.

| Vùng ghi độc quyền | Owner | Nội dung |
|---|---|---|
| `Assets/_Project/Features/FortressRuntime/**` | Codex | Runtime interaction, platform, hazard, destruction, audio hook và runtime SO cần thiết; asmdef không làm Core phụ thuộc ngược |
| `Assets/_Project/Tools/Editor/FortressIntegration/**` | Codex | Bộ sinh/bind gameplay, collision/runtime validator, tích hợp cuối |
| `Assets/_Project/Features/FortressRuntime/Tests/**` | Codex | EditMode/PlayMode tests, asmdef test riêng, fixture rõ nhãn development |
| `Assets/_Project/Features/FortressRuntime/Prefabs/**` | Codex | Prefab gameplay/composed variant; tham chiếu visual prefab của AG sau handoff |
| `Assets/_Project/Scenes/FortressGameplay/**` | Codex | Fixture scene và `Demo/GothicFortress_Demo.unity` cuối |
| `Assets/_Project/Features/FortressRuntime/Input/**` | Codex | Input bổ sung cho interaction khi cần; không sửa movement actions hiện có |
| `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs` | Antigravity | Mở rộng importer cũ có kiểm chứng; không đổi asmdef cha nếu không cần |
| `Assets/_Project/Tools/Editor/FortressProduction/**` | Antigravity | Import/animation/RuleTile/palette/prefab tools, room builder, dashboard, asset validators; editor-only assembly riêng nếu cần |
| `Assets/_Project/Features/FortressPresentation/**` | Antigravity | Terrain config, lighting preset và helper presentation thực sự cần chạy runtime; không chứa logic gameplay |
| `Assets/_Project/Art/Environment/GothicDungeon/**` | Antigravity | Import settings/tile/palette hiện có trong đúng phạm vi; giữ fileID/GUID và tùy chỉnh thủ công |
| `Assets/_Project/Art/Environment/FortressGenerated/**` | Antigravity | Animation, material, VFX, RuleTile/terrain configuration và generated art data |
| `Assets/_Project/Features/FortressPresentation/Prefabs/Visual/**`, `Assets/_Project/Features/FortressPresentation/Prefabs/Rooms/**` | Antigravity | Prefab hình ảnh/ambient và room template; room collision cũng thuộc AG |
| `Assets/_Project/Scenes/FortressAuthoring/**` | Antigravity | Preview scene, gồm `GothicFortress_ArtPreview.unity`; không tạo final gameplay demo |
| `Assets/_Project/Features/FortressPresentation/Tests/**` | Antigravity | Test importer, topology, generators, ref và room template; asmdef editor/test riêng |
| `ProjectSettings/TagManager.asset`, `ProjectSettings/Physics2DSettings.asset`, `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`, `Assets/UniversalRenderPipelineGlobalSettings.asset` | Antigravity | Layer/sorting/physics/render setup tối thiểu, bảo toàn cấu hình cũ; ghi diff/rationale vào handoff |
| `Packages/manifest.json`, `Packages/packages-lock.json` | Antigravity | Chỉ thay nếu xác minh thật sự cần package tương thích; runtime Codex chỉ dùng dependency baseline tới lúc tích hợp |
| `docs/Fortress/Reports/codex/**` | Codex | Ledger, evidence, scope requests, runtime handoff và final report |
| `docs/Fortress/Reports/antigravity/**` | Antigravity | Ledger, evidence, asset catalog, authoring handoff |
| `docs/Fortress/Guides/{README_Unity_Setup,COLLISION_AND_PHYSICS_GUIDE,INTERACTIVE_OBJECT_GUIDE,PRODUCTION_WORKFLOW}.md` | Codex | Bốn guide setup/gameplay/tích hợp |
| `docs/Fortress/Guides/{ASSET_IMPORT_GUIDE,AUTOTILE_GUIDE,TILEMAP_ARCHITECTURE,ANIMATION_SETUP_GUIDE,PREFAB_LIBRARY_GUIDE,ROOM_BUILDER_GUIDE}.md` | Antigravity | Sáu guide authoring |

Luôn giữ nguyên `Sandbox_Player.unity`, existing player/enemy/combat/camera code, prefab và source art gốc. Nếu integration buộc cần sửa chúng: lập yêu cầu phạm vi với bằng chứng cụ thể, không tự mở quyền. Nhận damage có thể dùng component/adapter trong FortressRuntime qua API sẵn có. Hành vi chở người trên platform là bổ sung môi trường, không phải quyền làm lại movement.

Metadata của folder gốc mới thuộc owner folder đó, dù file `.meta` nằm cạnh folder thay vì bên trong. Tests/prefab/input đặt dưới root tính năng riêng để không có hai bên cùng tạo `Assets/_Project/Tests.meta` hoặc `Assets/_Project/Prefabs.meta`. Nếu folder cha chưa có và cả hai bên cần dùng, không cùng sinh hai GUID cho nó: đặt output vào root đã có hoặc giao một owner tạo/bàn giao trước. Không tạo root Assets mới dùng chung ngoài bảng.

`AGENTS.md`, master prompt, ba tài liệu chuẩn bị này, bootstrap và baseline manifest là **immutable trong quá trình thực thi**. Không đồng thời cập nhật task list chung: mỗi platform cập nhật ledger của chính mình. Codex không sửa file AG sở hữu ngay cả khi đã import AG commit; correction gửi lại owner để có commit sửa đúng nhánh.

## 5. Contract FORTRESS-1 để hai nhánh không chờ nhau

### 5.1 Không phụ thuộc assembly chéo trước tích hợp

- Runtime Codex không reference assembly/class/asset mới của AG. Fixtures dùng component, collider và visual development tối thiểu; không quảng bá placeholder là production art.
- Authoring/presentation AG không reference `FortressRuntime` hoặc class mới của Codex. Visual prefab không gắn script runtime Codex; interactive artwork có thể có Animator, hook vị trí và state mapping trong catalog.
- AG room template có Grid/Tilemap/sorting/collision/lighting; không có dependency vào gameplay scripts chưa bàn giao.
- Sau khi AG sẵn sàng, Codex tạo composed prefab trong `FortressRuntime/Prefabs`, attach runtime và nối UnityEvents/Animator bằng API thực của handoff. Không tạo single-implementation interface chỉ để hai platform gọi nhau.
- Dùng `Health`, `IDamageable`, `DamageInfo`, Input System, VFX/camera hiện có khi phù hợp sau khi trace callers/wiring. Không sửa shared API để ép phù hợp prompt mẫu.

### 5.2 Contract dữ liệu bàn giao AG

AG xuất `docs/Fortress/Reports/antigravity/asset-catalog.json`, UTF-8:

```json
{
  "contract": "FORTRESS-1",
  "assets": [
    {
      "id": "unique-stable-logical-id",
      "category": "door",
      "assetPath": "Assets/_Project/Features/FortressPresentation/Prefabs/Visual/<actual-prefab>.prefab",
      "guid": "actual-guid-read-from-Unity",
      "status": "ready",
      "ppu": 32,
      "animator": {
        "parameters": [],
        "states": [],
        "completion": "actual completion signal or non-animated"
      },
      "hooks": [],
      "missingInputs": [],
      "evidence": []
    }
  ]
}
```

Ví dụ là schema, **không phải một asset đã có**. `status` thuộc `ready | draft | missing`. Không tạo path/GUID giả cho asset thiếu: dùng `assetPath: null`, `guid: null`. `category` dùng nhóm từ master (terrain/door/gate/chest/lever/platform/hazard/lighting/ambient/decoration...); `id` duy nhất và ổn định qua regeneration. `ppu` lấy theo asset, không ép mọi entry=32. `hooks` là object `{name, transformPath, purpose}` với path tương đối tồn tại. Animator parameters ghi `{name, type}`, states ghi `{name, layer, clipAssetPath, loop}`; completion mô tả cụ thể event/state/transition, không tự bịa event trong art. Entry không có Animator dùng `animator: null`.

`authoring-handoff.md` phải có baseline commit, delivered HEAD, danh sách commit theo thứ tự, changed paths, menu/API thực dùng, layer name+ID trước/sau, PPU, collision modes, preview scene, test commands/results, thiếu art và known limitations. Một JSON catalog không thay thế commit chứa asset + `.meta` do Unity tạo.

### 5.3 Contract báo cáo runtime Codex

Codex xuất `docs/Fortress/Reports/codex/runtime-validation.json`:

```json
{
  "contract": "FORTRESS-1",
  "status": "not-run",
  "checks": [],
  "errors": [],
  "missingInputs": []
}
```

`status` thuộc `not-run | passed | failed | blocked`. Khi đã chạy, mỗi check có `{id, status, evidencePath, message}`; check status thuộc `passed | failed | blocked | not-run`. Error có `{id, assetPath, message}`. Không gán passed cho kiểm tra chưa chạy. Dashboard AG đọc báo cáo bằng JSON/file; file chưa có phải hiện **Not run**, không phải 0 errors/Passed. Không cần runtime assembly reference để hiển thị report.

### 5.4 Physics/render ownership

AG sở hữu thêm layer/sorting theo master nhưng bảo toàn ID/matrix cũ. Codex fixture dùng layer baseline và LayerMask inspector; không hardcode ID layer mới chưa tồn tại. Sau handoff, C12 gán layer theo **tên đã xác minh** và chạy test collision matrix. Mọi khác biệt thiếu lớp gửi AG sửa, không lén sửa global settings từ Codex.

AG không đặt layer trang trí thành solid. Tile hiện có Collider=None không tự có va chạm chỉ vì thêm TilemapCollider; phải tạo collision tile/config/room geometry phù hợp mà không phá tile decoration dùng chung. Pixel camera chỉ đặt trên preview/template cần thiết; giữ camera hiện có và xử lý qua config riêng ở demo.

### 5.5 Generated và customized assets

Từng generator cần xác định asset nó quản lý và stable path/GUID, chạy lại không duplicate, không ghi đè asset chỉnh tay. Không nhận một prefab đã chỉnh tay là generated chỉ từ tên `PF_`. Báo cáo dry-run trước khi sửa dữ liệu hiện có, test chạy hai lần và manual customization. Input thiếu thì báo đúng asset/config thiếu; không dựng art/âm thanh/normal map giả để che lấp.

## 6. Task list có phân công

**Trạng thái ban đầu:** tất cả task thực thi bên dưới là `TODO`, chưa chạy. Bootstrap và tài liệu chuẩn bị là công việc riêng của lượt hiện tại. Orchestrator có thể gộp task nhỏ cho một worker, nhưng mỗi ID vẫn có evidence riêng. Mỗi worker chỉ có một vùng ghi được lease; review chỉ đọc.

### 6.1 Codex — runtime và final integration

| ID | Worker dự kiến | Phụ thuộc | Đầu ra | Acceptance Criteria và proof tối thiểu |
|---|---|---|---|---|
| C00 | Orchestrator | Baseline ready | `Reports/codex/preflight.md`, `status.md` | Đúng checkout/SHA; capability, Editor path/status, dirty baseline và permissions có bằng chứng; chỉ launch Editor riêng khi cần. |
| C01 | Runtime-audit | C00 | `runtime-audit.md`, reusable API/wiring map | Trace damage/hurtbox/input/player grounding/camera/VFX/asmdef liên quan; phân biệt code, prefab, scene và runtime; không tự kết luận thiếu vì không có tên class. |
| C02 | Interaction | C01 | Interaction selection/input và event contract trong FortressRuntime | Có input dùng được, lọc range/target hợp lệ, xử lý target bị disable/destroy; không sửa movement/input hiện hữu; tests cho selection và mất target. |
| C03 | Mechanisms | C02 | Door/gate/lock/chest components + fixtures | Đủ trạng thái mục 11, optional requirement/key, callbacks; animation completion không kẹt hoặc mở collider sớm; spam interact không phát loot lặp; test success/locked/re-entry. |
| C04 | Activation | C02 | Lever/switch/pressure plate + fixtures | Hold/toggle, entered/left; multi-collider/multiple objects không nhả sai; UnityEvents nối door/platform/trap; test disable/reset không giữ occupancy ma. |
| C05 | Platforms | C01 | MovingPlatform/elevator ngang-dọc + fixtures | Đầu/cuối/tốc độ/wait/reverse/auto/switch; physics-safe; Player hiện có đi/đứng/nhảy xuống được, không đổi movement; test reversal, end wait, rider attach/detach. |
| C06 | Platforms | C05 | Retractable/collapsing/chain-supported variants | Collider theo trạng thái, reset/retrigger đúng; không nhân bản mover cho mỗi tên; test collapse và restore khi có occupant. |
| C07 | Damage | C01 | Hazard damage component + fixtures | Amount/interval/knockback/type/activation; dùng damage contract hiện có; test i-frame, multi-hurtbox, exit/re-entry, disable/dead target. |
| C08 | Traps | C07 | Spike/flame/blade/rock/floor/crusher behavior + fixtures | Trạng thái/timing/cooldown/reset mục 14; blade pivot ổn định; rock impact một lần; damage chỉ trong phase hoạt động; test timing và reset. |
| C09 | Destruction | C01 | Destructible behavior + fixtures | Max/current HP, intact/damaged/cracked/breaking/destroyed, collider off, effects/debris/loot/audio hooks; test chết một lần, damage sau chết, thiếu animation fallback được ghi rõ. |
| C10 | Audio | C01 | Audio attachments/hooks cho cơ cấu và môi trường | AudioSource optional, thiếu clip im lặng; one-shot/loop không nhân khi activate lặp; không tạo nhạc/âm thanh giả; self-check qua fixture. |
| C11 | Validation | C03–C10 | Runtime/collision validator, tests và `runtime-validation.json`; `runtime-handoff.md` | Fixture compile/run độc lập không cần AG; test state/collision/body/layer/component/ref; phân biệt logic assertion với test physics thực; lệnh, exit code, result XML/log có thật. |
| C12 | Integration | C11, A12, A13 handoff | Import authoring commits, composed gameplay prefab, bind tool | Chỉ apply delta AG, giữ `.meta`/GUID; không sửa AG-owned files; generation idempotent; lever→gate/elevator/trap và chest/destruction callbacks wiring đúng; xác minh nhánh merge sạch. |
| C13 | Integration/QA | C12 | `Scenes/FortressGameplay/Demo/GothicFortress_Demo.unity` | Đủ checklist mục 21: ground/wall/ceiling/platform/vertical/one-way/gate/lever/chest/torch/magic/hazard/mover/background/foreground/lighting; dùng Player có sẵn; camera pixel clarity và ride platform kiểm tra thực; layout chưa được user duyệt ghi rõ. |
| C14 | QA/reviewer | C13 | `final-validation.md`, report tổng, bốn guide được cấp | Đối chiếu mục 26–27; compile + relevant Edit/PlayMode + missing refs/scripts + scene hierarchy; nêu errors/missing inputs/not-run, không bịa build pass; ghi đủ 10 guides đã nhận từ cả hai bên. |

Song song sau C01: **C02, C05, C07, C09, C10**. Sau C02: C03 và C04 có thể tách worker với file leases không trùng. C08 tiếp sau C07; C06 tiếp sau C05. C11 tích lũy test từng nhánh, không đợi tới cuối mới kiểm tra.

### 6.2 Antigravity — asset, authoring và presentation

| ID | Worker dự kiến | Phụ thuộc | Đầu ra | Acceptance Criteria và proof tối thiểu |
|---|---|---|---|---|
| A00 | Orchestrator | Baseline ready | `Reports/antigravity/preflight.md`, `status.md` | Đúng checkout/SHA, tool/subagent/auth có thật; Editor path và status; duy trì một Editor writer; không dùng MCP pin root. |
| A01 | Asset-audit | A00 | `asset-audit.md`, source/frame/family inventory | Kiểm kê nguồn, manifest, sprites, audio, normal maps; xem sheet liên quan; ghi inspected vs inventory-only; xác định topology/frame thiếu bằng chứng; không đọc đủ chỉ từ tên file. |
| A02 | Project-setup | A00 | Render/sorting/layer/physics conventions, config tối thiểu | Đối chiếu manifest+lock, asmdef/API; giữ Unity và ID layer cũ; thêm layer/sorting master, bảo toàn collision matrix cũ ngoài delta môi trường có lý do; ghi diff trước/sau. |
| A03 | Import | A01 | Mở rộng importer + Sprite Validator | Menu `Tools/Gothic Fortress/Asset Importer`, Import All; scan/category/manifest/slice, PPU configurable, Point/no mip/no compression/clamp/alpha; duplicate/dimension/metadata report; test native size và rerun không đổi GUID. |
| A04 | Terrain | A01, A03 | Terrain family/config và RuleTile Generator | Kiểm tra tất cả family A–K mục 5; tạo RT_Stone_* / RT_Wood / RT_Metal khi nguồn cho phép; 47 topology hợp lệ, transitions/decor alternatives/collision/sorting config; thiếu art thành draft với exact mapping. |
| A05 | Palettes | A04 | Palette Generator và palette mục 6 | Đủ các nhóm Stone/Moss/Damaged/Floor/Ceiling/Platform/Wood/Metal/Decoration/Hazard có dữ liệu; nhóm thiếu hiển thị thiếu, không bịa tile; paint usable, không trộn decoration collision. |
| A06 | Animation | A01, A03 | Animation Builder, clips/controllers, animation metadata | Reliable frame sequence/strip metadata; không đoán frame count; FPS/loop/order/length/pivot/scale editable; categories mục 9 có available/missing list; test missing frame và static variation. |
| A07 | Visual-prefabs | A02, A03, A06 | Visual prefab generator và library | Categories mục 10/19, SpriteRenderer/Animator/pivot/sorting/scale đúng; static decoration không Rigidbody/collision thừa; visual gate/chest/lever/trap không reference runtime Codex; run twice giữ customize. |
| A08 | Rooms-physics | A02, A05 | Grid/12 Tilemaps, solid+one-way config, room template | Cell/anchor/render order/TilemapCollider/Composite/static body đúng API; clean solid boundary, one-way từ dưới, no decoration collision; collider Tile=None được xử lý đúng; fixture không phụ thuộc Codex. |
| A09 | Presentation | A02, A03 | Pixel-perfect configuration utility và lighting presets | PPU giữ theo manifest, không ép tất cả 32; reference resolution/camera size có căn cứ; preserve follow camera integration, 7 lighting presets mục 16; normal map thiếu ghi rõ; kiểm tra hình thật. |
| A10 | Ambient | A02, A03, A06 | Ambient prefabs mục 17 | Dust/embers/fog/drips/steam/smoke/magic/splash/sparks theo asset có sẵn; emission/lifetime/speed/direction/randomness/size/opacity/space chỉnh được; Point texture, sorting và hiệu ứng bật/tắt được kiểm tra. |
| A11 | Room-builder | A08, A09 | Room Builder + 9 preset mục 20 | Tạo phòng kích thước chỉ định, cấu trúc/sorting/collision/light hợp lệ, lưu prefab/scene; không random fill; chạy lại không overwrite room người dùng tùy chỉnh. |
| A12 | Dashboard/QA | A03–A11 | Production Dashboard + authoring tests/validators | Đủ 10 tool mục 23 (runtime collision phần Codex qua report), count thực và nút theo master; thiếu Codex report hiện Not run; Open Demo chưa có hiện Not integrated; test no source/incomplete family/rerun/customized prefab. |
| A13 | Delivery/reviewer | A12 | Preview scene, asset catalog, authoring handoff, 6 guide | Preview có foreground/background, source actual, visual evidence; references/GUID/Animator verified; authoring compile/tests độc lập; catalog FORTRESS-1 đúng; missing art exact; freeze delivery HEAD cho Codex import. |

Song song sau audit: **A02 và A03**; sau A03: **A04, A06, A09, A10** theo dependency riêng trong bảng. A05 theo A04; A07 theo A06; A08 theo A05. Không hai worker cùng ghi importer/source manifest/generated asset.

### 6.3 Coverage đối chiếu master prompt

| Mục master | Owner task |
|---|---|
| 0–2, 24: discovery/architecture/coding | C00–C01, A00–A02, quy tắc ownership |
| 3: importer | A03 |
| 4: pixel-perfect | A09, C13 xác minh tích hợp |
| 5–6: RuleTile/palette | A04–A05 |
| 7–8: tilemap/physics/layers | A02, A08, C05–C08, C11–C14 |
| 9–10: animation/animated prefab | A06–A07 |
| 11: interaction | C02–C04, C12 |
| 12: platforms | C05–C06 |
| 13–14: destruction/traps | C07–C09 |
| 15–17: sorting/light/ambient/post-processing phù hợp | A02, A09–A10 |
| 18: audio | C10; A01 liệt kê nguồn |
| 19: prefab generation | A07 visual, C12 composed gameplay |
| 20: room builder | A08, A11 |
| 21–22: demo/input/tests | C02, C11–C14; A13 preview |
| 23: tools/dashboard | A03–A12; C11 runtime/collision report, C12 bind tool |
| 25: 10 documentation guides | A13 sáu, C14 bốn |
| 26–28: validation/DoD/execution | C14 tích hợp; A13 nhánh authoring |

## 7. Orchestration, sign-off và không giẫm chân

Mô hình: **hai orchestrator + fan-out/fan-in trong mỗi platform**, review độc lập chỉ đọc, tích hợp cuối một chiều AG → Codex. Không có runtime messaging giữa hai app; dùng artifact/commit làm handoff.

### Dispatch ticket bắt buộc cho mỗi worker

```text
Task ID / mục tiêu:
Checkout tuyệt đối và baseline SHA:
Inputs đã đọc / evidence:
Files được ghi (lease cụ thể, không chỉ ghi "toàn project"):
Files cấm ghi:
Contract FORTRESS-1 và dependency đã sẵn sàng:
Đầu ra + Acceptance Criteria từ task:
Kiểm tra phải chạy; Editor writer hiện là ai:
Báo lại: changed paths, API/config, test/evidence, thiếu inputs/risks:
Không spawn thêm writer; gửi yêu cầu cho orchestrator nếu cần tách việc.
```

Orchestrator không chỉ gửi master prompt cho mọi worker. Cấp context gọn gồm AGENTS + task + contract cần thiết. Có native subagents thì dùng; nếu phiên không có khả năng đó, ghi rõ và tự thực thi cùng task theo thứ tự, không bịa agent đã chạy hoặc gọi lệnh không được tài liệu/CLI xác nhận. Không cài SDK chỉ để giả lập orchestration.

### Ledger và chữ ký bằng chứng

Mỗi platform tạo ledger riêng `Reports/<platform>/status.md`; mỗi task có:

```text
ID | TODO/IN_PROGRESS/REVIEW/VERIFIED/BLOCKED_INPUT/BLOCKED_ENV
Platform / worker session ID thực tế / người thực hiện
Lease files / dependency / baseline
Changed paths / commit SHA hoặc diff snapshot nếu chưa commit
Verification commands + exit codes + đường dẫn log/XML/screenshots
Reviewer session ID + findings đã xử lý
Sign-off: VERIFIED bởi <orchestrator/session>, <UTC>, <evidence path>
Known limits / exact missing input / next action
```

Không điền tên/chữ ký giả. Nếu không có reviewer độc lập, ghi `self-review` và giới hạn, không tự gọi là independent review. VERIFIED chỉ khi AC đã có chứng cứ; task bị source-art block vẫn bàn giao phần chạy được nhưng không đổi thành đủ artwork. Orchestrator tổng hợp trạng thái, không để worker ghi đè ledger của worker khác.

### Lịch và bàn giao cuối

1. C00/A00 chạy độc lập từ cùng baseline. Hai bên audit phần sở hữu, không phải đợi nhau thống nhất B0 lần nữa: FORTRESS-1 đã chốt ranh giới, thiếu dữ liệu được report.
2. Mỗi bên phát triển/compile/test độc lập. Codex hoàn tất C11 ngay cả khi AG chưa xong.
3. AG hoàn tất A13, commit **chỉ các path AG sở hữu** và metadata Unity tạo liên quan; ghi delivered HEAD và danh sách commit. Không đưa bootstrap baseline vào delta bàn giao.
4. Codex kiểm tra sạch local changes, đọc handoff từ checkout AG, đối chiếu baseline commit và changed paths trước khi import. Chỉ khi handoff `READY` mới fetch/cherry-pick các commit delivery theo thứ tự; không fetch mù cả nhánh đang viết. Giữ asset ignore cần thiết có sẵn trong baseline, không push.
5. Ví dụ lệnh trên checkout Codex, **thay SHA bằng giá trị đã kiểm chứng**, không chạy nguyên placeholder:

```powershell
rtk proxy git -C D:/thietkegame/.fortress-workspaces/codex fetch D:/thietkegame/.fortress-workspaces/antigravity fortress/antigravity
rtk proxy git -C D:/thietkegame/.fortress-workspaces/codex diff --name-only <BASELINE_SHA> <DELIVERED_SHA>
rtk proxy git -C D:/thietkegame/.fortress-workspaces/codex cherry-pick <DELIVERY_COMMIT_1> <DELIVERY_COMMIT_2>
```

Chỉ local fetch/commit, không remote push. Không dùng reset --hard, clean -fd, ghi đè conflict theo ours/theirs. Nếu phạm vi AG commit sai, yêu cầu AG tách commit; không lấy luôn thay đổi ngoài quyền.

6. C12–C14 chỉ bắt đầu integration sau handoff. Codex báo lỗi AG bằng `Reports/codex/authoring-change-request.md`: bằng chứng/path/expected/actual; AG sửa vùng mình rồi bàn giao commit tiếp. Lúc này phần phụ thuộc thật sự cần chờ, các task khác vẫn tiếp tục.
7. Nếu AG chưa READY khi Codex đạt C11: xuất `runtime-handoff.md`, ghi `READY_FOR_AUTHORING_HANDOFF`, kết thúc lượt với đầu ra đầy đủ của nhánh runtime. Không busy-wait vô hạn hoặc tự tạo asset nhánh AG. Gửi lại Goal-Codex.md để resume sau A13; orchestrator đọc ledger và không làm lại task VERIFIED.
8. Kết quả toàn bộ nằm trong checkout Codex sau tích hợp, **không tự merge/copy về repo gốc**. Người dùng review diff/report rồi giao bước áp dụng về root riêng; không commit thay đổi đang có của người dùng.

## 8. Cổng nghiệm thu và failure scenarios

### Gate P — chuẩn bị

- Bootstrap tests pass; hai snapshot có cùng hashes/baseline commit; repo gốc không bị ghi asset/code/config Unity.
- Source dirty changes được giữ nguyên; ignored input cần thiết được copy, không copy pin MCP sai checkout.
- Chưa có Editor session/auth không chặn xuất prompt; phải ghi rõ chưa runtime-ready.

### Gate R / A — từng nhánh

- Mỗi bên compile được từ baseline của mình và test liên quan, không cần class mới bên kia.
- Asset generation có stable identity, kiểm tra chạy lại và preserve customized assets.
- Branch diff nằm đúng ownership; guides, catalog/report và signed ledger có đủ evidence.
- Package unavailable/source missing/editor license missing có status riêng, không bị gộp thành Passed.

### Gate I — toàn hệ thống

- Compile và test trên snapshot đã tích hợp; references/sprites/scripts/controllers/RuleTiles/collider/sorting đúng.
- Kiểm tra thực: one-way, moving rider, lever→gate/platform/trap, chest loot một lần, hazard interval, destructible chết một lần, camera clarity.
- Dashboard số liệu thực; demo đáp ứng mục 21; tài liệu đủ 10 file; đối chiếu tất cả checkbox mục 27.
- Nêu asset/mapping/frame thiếu chính xác, lỗi còn lại, phần chưa chạy. Không tuyên bố production-ready hoặc build pass nếu bằng chứng chưa đủ.

### Tình huống phải xử lý được

| Tình huống | Kết quả bắt buộc |
|---|---|
| Source đổi khi bootstrap copy | Không ready; không ghi đè/xóa source hoặc snapshot cũ |
| Một agent mở root hoặc MCP pin root | Ngừng mọi mutation, sửa target ở session/checkout của mình; không tiếp tục trên root |
| AG không có frame/tile topology | Draft/missing + danh sách exact input; các nhánh khác vẫn làm được |
| Codex report chưa có trong authoring checkout | Dashboard hiện Not run, vẫn compile/hoạt động |
| Hai subagent muốn Editor cùng lúc | Orchestrator serialize lease; không cùng ghi AssetDatabase |
| AG handoff chứa path Codex sở hữu | Từ chối import commit đó, yêu cầu tách đúng scope |
| Unity import tự đổi file ngoài ownership | Ghi diff; không ship/cherry-pick phần đó; nhờ owner xử lý, không blanket revert thay đổi người khác |
| Agent không có subagent tool | Ghi giới hạn và tự làm task; không giả danh worker/reviewer |
| Một platform xong trước | Bàn giao nhánh, không lấn scope bên kia; resume ở điểm integration |

## 9. Hai goal prompt

- [Goal-Codex.md](Goal-Codex.md): gửi cho Codex orchestrator trong checkout Codex.
- [Goal-Antigravity.md](Goal-Antigravity.md): gửi cho Antigravity orchestrator trong checkout Antigravity.

Hai prompt độc lập để **bắt đầu và hoàn thành nhánh riêng**. Việc chứng minh toàn hệ thống chạy cùng nhau luôn cần bước tích hợp sau handoff; không hứa hai sản phẩm hoàn toàn độc lập mà không có điểm ghép cuối.
