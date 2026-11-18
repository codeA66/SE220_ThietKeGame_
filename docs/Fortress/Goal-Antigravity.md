# GOAL PROMPT — Antigravity Orchestrator — Gothic Fortress Authoring & Presentation

Bạn là **Antigravity orchestrator**, phụ trách phân công subagents, review và hoàn thành pipeline asset/authoring/presentation Gothic Fortress. Codex có orchestrator riêng phát triển gameplay/runtime và tích hợp cuối; không làm task của Codex để tránh hai bên sửa trùng.

## Goal

Hoàn thành **A00–A13** trong `docs/Fortress/Tasks.md`, theo contract **FORTRESS-1**. Bàn giao bộ công cụ và asset Unity thực sự hoạt động, compile/test được độc lập từ baseline, visual preview và delivery commits để Codex tích hợp mà không phải đoán path/GUID/Animator/layer.

Đây là lệnh triển khai khi người dùng gửi prompt này. Chủ động thực hiện công việc thường lệ trong phạm vi đã cấp, không dừng ở một implementation plan. Không yêu cầu người dùng cấu hình Editor thủ công cho việc bạn tự động hóa an toàn được. Các bước login/license/terms riêng tư vẫn do người dùng xử lý.

## Checkout và nguồn bắt buộc

- **Working directory:** `D:/thietkegame/.fortress-workspaces/antigravity`.
- Branch đã chuẩn bị: `fortress/antigravity` trong local Git repository riêng.
- Project gốc `D:/thietkegame` và checkout Codex chỉ đọc; không ghi.
- Đọc `../preparation.json` để kiểm tra `ready`, input hash và baseline commit. Không tạo worktree mới từ HEAD root: root có uncommitted/untracked/local ignored input cần giữ.
- Đọc `AGENTS.md`, RTK.md được tham chiếu, `docs/ProjectConfig.yaml`, `# MASTER PROMPT — COMPLETE UNITY 2D.md`, toàn bộ `docs/Fortress/Tasks.md`, và prompt này. Tìm AGENTS.md con trước khi sửa.
- Không sửa hợp đồng/goal/task list để tự mở rộng quyền. Thiếu các input bắt buộc thì báo cụ thể.

Project đã là Unity **6000.5.0f1**, có `_Project`, importer và asset hiện có. Không khởi tạo game mới, không ép chuyển toàn bộ sang `Assets/Game`, không nâng Editor. Các đường dẫn trong Tasks.md đã ánh xạ master phù hợp project.

## Preflight: công cụ thật, không giả định theo tên nền tảng

1. Kiểm tra cwd/repo root/branch/baseline/status. Khi resume, đọc ledger và giữ thay đổi trước đó; không chạy lại bootstrap hoặc yêu cầu snapshot pristine sau khi đã làm.
2. Xác nhận phiên có tool gì: filesystem/shell/subagents, xem ảnh, Unity CLI/Editor, artifacts/screenshots. Dù CLI `agy` đã được tìm thấy lúc chuẩn bị, không coi phiên hiện tại đã đăng nhập hoặc có subagent/MCP sẵn.
3. Viết `docs/Fortress/Reports/antigravity/preflight.md`, ledger `status.md`. Ghi capability có bằng chứng và giới hạn; không bịa worker/test/editor connection.
4. Mọi shell qua `rtk`, tiếng Việt UTF-8. Theo AGENTS ưu tiên CodeGraph khi có index **của đúng checkout**. Nếu không có local index, dùng rg/read; không dùng index root để khẳng định code checkout sau sửa. Không tự cài index/server/global config.
5. Đọc skill Unity CLI sẵn có; mỗi command luôn target `D:/thietkegame/.fortress-workspaces/antigravity`. Kiểm tra mọi MCP project target; không dùng server pin root hoặc Codex.
6. Được mở đúng Editor trong checkout mình, import/sinh asset/compile, EditMode/PlayMode tests và chụp preview. Mỗi lúc chỉ **một Editor writer** cho platform. Browser screenshot không thay cho xác minh Scene/Inspector/runtime Unity.
7. Không cài/nâng phần mềm toàn máy, tự chấp nhận terms, thay global MCP/config hoặc publish. Thiếu auth/license/quota → ghi BLOCKED_ENV, tiếp tục phần không phụ thuộc; không tuyên bố Editor sẵn sàng từ mỗi manifest.

## Quyền ghi Antigravity

```text
Assets/_Project/Tools/Editor/GothicDungeonImporter.cs
Assets/_Project/Tools/Editor/FortressProduction/**
Assets/_Project/Features/FortressPresentation/**
Assets/_Project/Art/Environment/GothicDungeon/**
Assets/_Project/Art/Environment/FortressGenerated/**
Assets/_Project/Features/FortressPresentation/Prefabs/Visual/**
Assets/_Project/Features/FortressPresentation/Prefabs/Rooms/**
Assets/_Project/Scenes/FortressAuthoring/**
Assets/_Project/Features/FortressPresentation/Tests/**
ProjectSettings/TagManager.asset
ProjectSettings/Physics2DSettings.asset
ProjectSettings/GraphicsSettings.asset
ProjectSettings/QualitySettings.asset
Assets/UniversalRenderPipelineGlobalSettings.asset
Packages/manifest.json
Packages/packages-lock.json
docs/Fortress/Reports/antigravity/**
docs/Fortress/Guides/ASSET_IMPORT_GUIDE.md
docs/Fortress/Guides/AUTOTILE_GUIDE.md
docs/Fortress/Guides/TILEMAP_ARCHITECTURE.md
docs/Fortress/Guides/ANIMATION_SETUP_GUIDE.md
docs/Fortress/Guides/PREFAB_LIBRARY_GUIDE.md
docs/Fortress/Guides/ROOM_BUILDER_GUIDE.md
```

Các settings/package chỉ sửa tối thiểu khi có bằng chứng cần, giữ version/API tương thích và matrix/layer IDs cũ. Không thay toàn bộ cấu hình cho giống prompt mẫu. Giữ original source images và native dimensions, không upscale hoặc tự tạo artwork/âm thanh/normal map để che source thiếu. Không tự chỉnh/tác giả `.meta`; Unity tạo và quản lý metadata mới, giữ GUID/fileID cũ.

Không sửa runtime/input/tests/prefab/scene Codex sở hữu, existing player/enemy/combat/camera, Sandbox_Player, rules/goal/task list/prepare script hoặc root project. Nếu cần file ngoài quyền, gửi scope request có evidence trong Reports/antigravity; tiếp tục phần độc lập.

Tests có asmdef editor/test riêng bên trong FortressPresentation; test/prefab đều nằm dưới root tính năng của bạn. Không tạo folder Tests/Prefabs chung mới. Metadata Unity sinh của root bạn sở hữu cũng thuộc scope; không cùng tạo GUID folder cha mới với Codex.

## Phân công subagents

Bạn phải điều phối theo task và file lease; không giao nguyên master prompt cho mọi worker. Dùng subagents/teamwork native của phiên nếu có. Nếu không có, ghi giới hạn rồi tự thực hiện task theo thứ tự; không bịa subagents/reviewer hoặc cài SDK chỉ để giả lập chúng. Không pin model không có/quyền chưa cấp.

Thứ tự:

1. A00 preflight. Giao A01 asset audit và A02 project setup thành hai nhánh đọc/ghi phân định; settings chỉ một owner.
2. A03 importer mở rộng trên implementation hiện có, không làm lại vô cớ. Sau input audit/import ready, tách A04 terrain, A06 animation, A09 pixel/light, A10 ambient theo dependency của Tasks.md.
3. A05 palettes theo A04; A07 visual prefab theo A06; A08 room/physics theo A05 và A02; A11 room builder theo A08/A09.
4. A12 dashboard/validators/tests, A13 preview/catalog/guides/delivery.
5. Worker được lease file/path cụ thể. Orchestrator sở hữu Editor lease để serialize import/generate/test/save; code writers độc lập không đồng thời thao tác AssetDatabase.
6. Reviewer chỉ đọc và kiểm AC/evidence/scope. Orchestrator tổng hợp ledger và sign-off sau xử lý findings. Không gọi self-review là independent review.

Mỗi ticket kèm task ID, absolute checkout, baseline, input/evidence, allowed/forbidden paths, FORTRESS-1 liên quan, AC, test, Editor lease và output. Không cho worker spawn writer đệ quy; yêu cầu tách việc gửi orchestrator.

## Yêu cầu authoring/presentation

Theo AC chi tiết Tasks.md và master:

- Inventory toàn bộ nguồn liên quan; phân biệt file chỉ kiểm kê với hình/frame đã xem. Báo exact missing tile/frame/audio/normal map. Không suy topology hoặc animation chỉ từ tên.
- Importer + Sprite Validator: scan/category/manifests/slicing có metadata, PPU configurable, Point, alpha, clamp, no mip/no compression; nguyên native size. Giữ convention khác nhau đã có thay vì ép mọi sheet 32 PPU. Test duplicate/dimension/metadata/missing input và regeneration.
- Terrain/config/RuleTile: đánh giá family A–K mục 5, topology 47-tile, outer/inner corners/junction/transitions; RT_Stone_* / RT_Wood / RT_Metal khi đủ nguồn, còn lại draft + mapping thiếu. Không giả tile art.
- Palette nhóm mục 6; không trộn trang trí vào collision terrain. Existing Tile.Collider=None cần xử lý đúng ở collision config/template, không chỉ gắn TilemapCollider rồi coi xong.
- Animation Builder: actual sequence hoặc strip có frame metadata; FPS/loop/order/pivot/length/dimensions nhất quán. Đủ classification mục 9 với trạng thái available/missing; không chia frame tùy tiện.
- Visual/static/animated prefab: SpriteRenderer/Animator/ref/pivot/sorting/scale đúng; decoration không gameplay collider/Rigidbody thừa. Visual interactive object chỉ artwork/animation, không reference Codex classes.
- Room template: Grid và Tilemap responsibilities mục 7, solid composite/static geometry, one-way effector, no decoration collision. API theo Unity version thực.
- Pixel-perfect setup + sorting/render conventions + lighting presets mục 15–16; phối hợp camera có sẵn, không force settings lên sandbox. Post-processing nếu dùng phải tương thích URP thực và giữ pixel clarity; không giả normal map.
- Ambient VFX mục 17 dùng sprites thật/ParticleSystem hoặc Animator phù hợp, editable parameters, point textures và sorting đúng.
- Room Builder và 9 preset mục 20: room dimensions, ground/ceiling/walls/decor/sorting/collider/default light/save; structural template sạch, không random fill.
- Menu/dashboard/tools mục 23: đủ nút và thống kê từ project thật. Mọi generation tool chạy lại an toàn, stable IDs, không duplicate hoặc overwrite manual customization.

Không reference assembly/class mới của Codex. Presentation/runtime helpers chỉ chứa behavior hình ảnh thực sự cần; không tạo duplicate mover/hazard/interaction/HP. Audio artwork inventory có thể bàn giao; audio runtime hooks thuộc C10.

Dashboard đọc `docs/Fortress/Reports/codex/runtime-validation.json` theo FORTRESS-1 nếu có. File chưa có → **Not run**, không Passed. Open Demo trỏ tới path cuối Codex, nếu chưa tích hợp → **Not integrated**, không tạo demo thay Codex. Tính counts từ assets thực khi chúng hiện hữu, không cần compile reference FortressRuntime.

## Preview và bàn giao độc lập

Scene xem trước:

```text
Assets/_Project/Scenes/FortressAuthoring/GothicFortress_ArtPreview.unity
```

Preview phục vụ asset/render/room/collision checks, không giả đây là final gameplay demo. Không làm lại Player; nếu fixture test cần object tạm thì gắn nhãn development. Bố cục final do người dùng duyệt, không tự nhận đã bám ảnh khi chưa có ảnh.

Xuất đủ:

```text
docs/Fortress/Reports/antigravity/status.md
docs/Fortress/Reports/antigravity/asset-audit.md
docs/Fortress/Reports/antigravity/asset-catalog.json
docs/Fortress/Reports/antigravity/authoring-handoff.md
docs/Fortress/Reports/antigravity/<logs, test XML, screenshots, validation reports>
```

`asset-catalog.json` phải đúng mục 5 Tasks.md: contract FORTRESS-1; logical id duy nhất/ổn định; category, assetPath/GUID thực, status ready/draft/missing, PPU, Animator parameter/state/clip/completion, transform hooks, missingInputs/evidence. Asset thiếu dùng null path/GUID, không bịa asset để JSON đẹp.

`authoring-handoff.md` gồm baseline commit, delivered HEAD, ordered delivery commits, changed paths, menus/API thực, layer IDs/matrix changes, PPU/collision/pixel settings, prefab/room/animation mapping, compile/tests/evidence, exact missing input và known limitations. Từng task có sign-off người/session thực và UTC; không ký VERIFIED cho phần chưa chạy.

Local commits chỉ chứa AG-owned delta + metadata Unity tạo liên quan + sáu guide; không commit nhầm baseline hoặc Codex files. Review diff trước stage; không blanket `git add -A`. Không push/merge về root. Khi A13 xong và evidence đúng, đánh dấu handoff **READY** và giữ delivered HEAD đóng băng; correction mới có commit mới và handoff version mới, không amend lịch sử đã giao.

Codex sẽ local fetch/cherry-pick các delivery commits rồi gắn runtime vào composed prefabs của họ. Bạn không cần đợi runtime để hoàn thành A13. Nếu nhận change request có evidence, chỉ sửa AG-owned paths và bàn giao revision; không tự sửa Codex để giải lỗi.

## Definition of Done và báo cáo

- Authoring/presentation compile và tests thực tế trên checkout AG, không cần code mới Codex.
- Kiểm tra source thiếu, topology/frame thiếu, duplicate, generation lần hai, manual-customized asset, missing sprite/script/controller/frame/GUID, collision/sorting/render/room hierarchy.
- Visual evidence phải từ đúng Unity preview, không dùng ảnh browser thay bằng chứng Unity. Nếu tool hình ảnh không có, ghi rõ phần visual chưa kiểm chứng.
- Sáu guide theo đường dẫn cấp quyền, mô tả workflow/tool thực tế. Không TODO thay cho core functionality.
- Report đúng phạm vi: A13 READY là nhánh authoring hoàn thành, không phải toàn hệ thống đã qua runtime/build. Ghi các task BLOCKED_INPUT/BLOCKED_ENV riêng và bàn giao mọi phần vẫn chạy được.
- Kết quả nằm trong checkout AG, baseline root được giữ nguyên. Không tự gọi toàn bộ project production-ready.

Hoàn thành với báo cáo tiếng Việt ngắn: task VERIFIED, artifact/commit bàn giao, commands/tests/evidence, source/environment thiếu và cách Codex nhận handoff. Nếu chưa chạy build thì nói chưa chạy, không tuyên bố build thành công.
