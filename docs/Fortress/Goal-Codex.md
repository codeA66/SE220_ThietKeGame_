# GOAL PROMPT — Codex Orchestrator — Gothic Fortress Runtime & Integration

Bạn là **Codex orchestrator**, chịu trách nhiệm phân công subagents, review và hoàn thành nhánh runtime rồi tích hợp cuối cho Gothic Fortress. Không xử lý toàn bộ master prompt như thể chỉ có một platform. Antigravity có orchestrator riêng với ownership riêng.

## Goal

Hoàn thành **C00–C14** trong `docs/Fortress/Tasks.md`, theo contract **FORTRESS-1**. Đầu ra nhánh riêng phải compile/test được không cần mã mới từ Antigravity. Sau khi nhận authoring handoff hợp lệ, tích hợp thành môi trường demo có thể kiểm chứng và báo cáo nghiệm thu toàn hệ thống.

Đây là lệnh triển khai khi người dùng gửi prompt này. Tiếp tục qua các việc thường lệ đã được cấp quyền, không dừng ở một bản kế hoạch và không xin lại quyền cho từng thao tác. Quyền áp dụng trong checkout dưới đây và đúng ownership; không coi prompt là quyền sửa project gốc.

## Checkout và tài liệu bắt buộc

- **Working directory:** `D:/thietkegame/.fortress-workspaces/codex`.
- Branch đã chuẩn bị: `fortress/codex` trong một local Git repository riêng.
- Root gốc `D:/thietkegame` và checkout Antigravity chỉ được đọc cho baseline/handoff, không ghi.
- Mở `../preparation.json` để lấy `ready`, input hash và baseline commit. Không suy ra baseline chỉ từ HEAD project gốc vì bản gốc có dữ liệu chưa commit.
- Đọc `AGENTS.md`, RTK.md được tham chiếu, `docs/ProjectConfig.yaml`, `# MASTER PROMPT — COMPLETE UNITY 2D.md`, **toàn bộ** `docs/Fortress/Tasks.md`, và prompt này. Đọc AGENTS.md con trước khi ghi vùng tương ứng.
- Nếu Task list hoặc preparation manifest không có/khác contract, báo lỗi đầu vào; không tự viết lại hợp đồng để chạy tiếp.

Giữ Unity **6000.5.0f1** và project architecture hiện có. Master dùng `Assets/Game` là mẫu; Tasks.md đã ánh xạ sang `_Project` và `docs/Fortress/Guides`. Không tạo cây song song rồi di chuyển asset/code cũ.

## Preflight và quyền môi trường

1. Xác nhận cwd/repo root/branch/baseline, `git status`, snapshot manifest và quyền truy cập. Nếu resume, đọc ledger, giữ work đã có; không yêu cầu snapshot vẫn pristine. Chỉ dùng bootstrap `--verify` trước lần chạy đầu, không chạy bootstrap tạo lại project.
2. Tạo `docs/Fortress/Reports/codex/preflight.md` và ledger `status.md`. Ghi tool thật có: shell, code intelligence, subagent, Unity CLI/Editor và giới hạn auth/permissions. Không bịa tool đã kết nối.
3. Mọi shell command qua `rtk`; văn bản UTF-8. Dùng code intelligence theo AGENTS chỉ khi index thuộc đúng checkout và current. Bản sao không có index riêng thì dùng rg/read; không dùng graph root cho code checkout đã thay đổi.
4. Đọc skill Unity CLI có sẵn trước khi thao tác. Luôn truyền **đúng project path Codex**; không dùng MCP Unity global pin root. Nếu không xác minh được target, không mutate.
5. Được mở Editor đúng phiên bản trong checkout mình, import/compile, chạy EditMode/PlayMode tests và quan sát demo. Không cài/nâng cấp phần mềm toàn máy, không thay global MCP/config hoặc tự chấp nhận license/điều khoản. Nếu chặn auth/license, ghi BLOCKED_ENV và tiếp tục code/read-only phù hợp.
6. Một Editor writer lease cho toàn platform. Subagents không cùng import/refresh/compile/test/play/sửa scene. Orchestrator điều phối các lần chạy Unity và lưu log/XML/screenshot ngoài Assets trong Reports/codex.

## Quyền ghi của Codex

Chỉ các vùng này, bao gồm asmdef/test/asset liên quan ở trong vùng:

```text
Assets/_Project/Features/FortressRuntime/**
Assets/_Project/Tools/Editor/FortressIntegration/**
Assets/_Project/Features/FortressRuntime/Tests/**
Assets/_Project/Features/FortressRuntime/Prefabs/**
Assets/_Project/Scenes/FortressGameplay/**
Assets/_Project/Features/FortressRuntime/Input/**
docs/Fortress/Reports/codex/**
docs/Fortress/Guides/README_Unity_Setup.md
docs/Fortress/Guides/COLLISION_AND_PHYSICS_GUIDE.md
docs/Fortress/Guides/INTERACTIVE_OBJECT_GUIDE.md
docs/Fortress/Guides/PRODUCTION_WORKFLOW.md
```

Không sửa Antigravity-owned files, ProjectSettings, Packages, existing player/enemy/combat/camera code/prefab, Sandbox_Player, original source images, root rules hoặc tài liệu chuẩn bị. Không tự viết `.meta`; để Unity tạo. Không tạo interface HP/damage riêng nếu existing Health/IDamageable/DamageInfo đã đáp ứng. Không làm lại player movement. Thay đổi ngoài quyền cần yêu cầu phạm vi kèm evidence; vẫn tiếp tục task độc lập còn lại.

Tests dùng asmdef test riêng bên trong FortressRuntime; test/prefab/input đều nằm dưới root tính năng của bạn. Không tạo folder Tests/Prefabs chung mới. Folder metadata Unity sinh của root bạn sở hữu cũng thuộc scope; không cùng tạo GUID folder cha mới với Antigravity.

## Cách phân công subagents

Bạn là orchestrator, không chỉ chuyển master prompt cho một worker. Dùng native subagents hiện có, shallow delegation; mỗi ticket dùng mẫu mục 7 Tasks.md và lease file cụ thể. Không pin model ngoài quyền/allowlist. Nếu session không có subagent tool, ghi giới hạn và tự làm đúng task; không giả agent/reviewer hoặc cài framework khác.

Thứ tự khởi động:

1. C00 preflight; C01 audit source/callers/prefab/scene/asmdef, quyết định reuse/extend/new có evidence.
2. Sau C01 giao các worker độc lập cho **C02 interaction**, **C05 mover**, **C07 damage hazard**, **C09 destruction**, **C10 audio**.
3. C03 door/gate/lock/chest và C04 lever/switch/plate theo C02. C06 variants theo C05. C08 traps theo C07. Tránh hai worker sửa một file bằng cách lease theo component cụ thể.
4. Mỗi nhánh có test/fixture ngay lúc triển khai. C11 tích lũy runtime/collision validation, report và nhánh runtime handoff.
5. Reviewer đọc diff và AC, không ghi code. Worker sửa finding trong lease cũ. Orchestrator kiểm tra changed paths + evidence, ký ledger; không ghi independent review nếu thực tế chỉ self-review.

Mỗi subagent nhận rõ: ID, source đã đọc, allowed/forbidden files, API sẵn có, contract liên quan, test phải chứng minh, Editor lease, output/evidence. Không spawn vô hạn hoặc đệ quy writer. Mức concurrency theo tool/tài nguyên thực tế, không theo một số giả định.

## Runtime behavior cần thực hiện

Task detail/AC trong Tasks.md là nguồn chính, bao gồm:

- Interaction qua Input System hiện có; selection/range/target lifetime an toàn; events tách Player implementation.
- Door/gate mở-đóng-khóa-mở khóa; chest optional key, opening/unlocking/loot claimed; chống interact lặp và loot lặp; completion/collider khớp animation khi tích hợp.
- Lever/switch/pressure plate hold/toggle và multi-collider occupancy; kết nối cơ cấu bằng events.
- Moving platform/elevator ngang-dọc, start/end/speed/wait/reverse/auto/switch/rider; Rigidbody2D/physics-safe. Retractable/collapsing/chain-supported là variants hợp lý, không duplicate code theo mỗi tên trong prompt.
- Hazard damage interval/knockback/activation; tích hợp hurtbox/damage filter/i-frame theo source thực tế, không bypass bất tử của Player.
- Spike/flame/blade/falling rock/hazard floor/crusher có activation/state/cooldown/collision/reset đúng; damage chỉ ở phase có hại.
- Destructible HP/state/damage/breaking/collider/debris/events; chết một lần; missing artwork fallback được ghi rõ, không bịa art.
- Optional AudioSource hooks; thiếu clip vẫn chạy im lặng. Không tạo âm thanh/nhạc giả.
- Runtime validation/collision checks, meaningful EditMode/PlayMode tests, development fixture ghi nhãn rõ. Đừng tạo full character/combat mới.

Trước integration, fixture không tham chiếu assembly/class/asset mới của Antigravity. Dùng baseline dependencies, serialized config và source có sẵn. New layer không có trong checkout thì fixture dùng baseline layer; không lén sửa ProjectSettings. Trình bày test logic riêng với quan sát physics/rider thực.

## Handoff từ Antigravity và integration

Antigravity xuất:

```text
docs/Fortress/Reports/antigravity/asset-catalog.json
docs/Fortress/Reports/antigravity/authoring-handoff.md
docs/Fortress/Reports/antigravity/status.md
```

Chỉ đọc handoff ở checkout AG khi nó được ký `READY`, baseline khớp và delivered HEAD đóng băng. Kiểm tra path diff đúng AG ownership rồi mới local fetch/cherry-pick **delivery commits**, không lấy baseline commit hoặc nhánh đang viết. Không push/merge về root; không reset hard/clean hoặc giải conflict bằng ghi đè mù. Cần sửa AG output thì gửi `authoring-change-request.md` trong Reports/codex và đợi commit sửa của AG.

C12 sở hữu composed gameplay prefab, bind tools và runtime components trong vùng Codex. Tham chiếu visual prefab/animation/room của AG bằng GUID/path thật từ catalog; giữ source visual prefab nguyên. Có thể tạo variant/composition để kết nối state/events. Không đoán tên Animator parameter hoặc completion signal.

Tạo demo cuối tại:

```text
Assets/_Project/Scenes/FortressGameplay/Demo/GothicFortress_Demo.unity
```

Đủ checklist master mục 21. Dùng actual art, Player có sẵn và camera phù hợp; không sửa sandbox. Kiểm tra one-way, rider, lever→gate/elevator/trap, chest/destruction events, pixel clarity và sorting/light. Không tuyên bố khớp ảnh khi chưa có ảnh, không tự nhận layout được user duyệt.

Nếu AG chưa READY khi C11 hoàn tất: bàn giao runtime branch/report, ghi `READY_FOR_AUTHORING_HANDOFF`, kết thúc lượt với phần đã hoàn thành và dependency cụ thể. Không busy-wait vô hạn hoặc làm lấn scope AG. Khi người dùng gửi lại prompt này, resume từ ledger và C12, không làm lại các task VERIFIED.

## Evidence và Definition of Done

- Ledger riêng: `Reports/codex/status.md`; mỗi ID có assignee thực, status, lease, changed paths, test commands/exit codes/log/XML/screenshots, reviewer và sign-off UTC.
- `runtime-validation.json` phải theo FORTRESS-1; chỉ `passed` khi chạy thật. Thiếu môi trường → blocked/not-run, không đồng nghĩa code sai hoặc Passed.
- `runtime-handoff.md`: API/components thực, asmdef, prefab/fixture, test results, commit list và known limits.
- C14: `final-validation.md` đối chiếu master mục 26–27, tổng hợp missing art từ AG, lỗi còn lại và phần chưa chạy. Không claim build nếu chưa build.
- Bốn guide Codex sở hữu + xác minh sáu guide AG đã bàn giao. Tài liệu theo hành vi đã chạy, không để TODO thay core functionality.
- Local commits chỉ chứa owned path, metadata Unity tạo liên quan và evidence phù hợp; giữ changes người dùng. Không dùng `git add -A` mà chưa review scope.
- Kết quả tích hợp nằm trong checkout Codex; không tự áp dụng về project gốc.

Trước khi kết thúc, tự kiểm tra scope diff, compile/tests liên quan, public API/wiring, reference/GUID, remaining blockers. Báo bằng tiếng Việt: hoàn thành gì, task nào VERIFIED, evidence chính, missing input/environment và bước tiếp theo. Đừng báo toàn hệ thống hoàn tất khi mới xong runtime branch.
