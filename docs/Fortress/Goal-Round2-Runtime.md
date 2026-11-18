# GOAL VÒNG 2 — Antigravity Swarm — Runtime & Final Integration

Bạn là **Antigravity Swarm orchestrator của nhánh Runtime**. Cả hai nhánh hiện đều chạy bằng Antigravity; tên `codex` trong checkout/branch/report là tên lịch sử, không yêu cầu gọi Codex để làm việc. Dùng native Swarm với file leases và reviewer chỉ đọc.

## Goal và tài liệu bắt buộc

Hoàn tất **P2-00, R2-00…R2-06** trong plan vòng 2: sửa Runtime findings, kiểm chứng lại **C00–C11**, nhận authoring, hoàn tất **C12–C14** và demo thật trên **Unity 6000.6.9f1**. Không báo toàn hệ thống hoàn thành chỉ vì runtime suite pass.

Đọc đầy đủ trước triển khai:

1. `D:/thietkegame/docs/superpowers/plans/2026-11-16-fortress-round2.md` — plan chuẩn, quyền migration, Swarm/lease, AC/test, handoff và gates.
2. AGENTS checkout và `D:/thietkegame/AGENTS.md`, RTK được tham chiếu, các skill Unity phù hợp; AGENTS con trước ghi.
3. Checkout `docs/ProjectConfig.yaml`, `docs/Fortress/Tasks.md`, `# MASTER PROMPT — COMPLETE UNITY 2D.md`, Runtime ledger/audit/source/tests và authoring handoff thực tế.

Vòng 2 thay version/executor/migration/phối hợp đã ghi rõ; giữ toàn bộ FORTRESS-1 và AC còn lại. Không sửa goal/Tasks/baseline để tự hợp thức hóa pass.

## Checkout và quyền

- **cwd/project:** `D:/thietkegame/.fortress-workspaces/codex`.
- **Branch:** `fortress/codex`. HEAD lúc lập plan còn baseline `411a440ed12407971163d74e2f0a12bc70ad41b2`; runtime work và reports vẫn untracked. Kiểm tra lại khi bắt đầu, giữ mọi tiến độ đã có.
- Root và checkout Authoring chỉ đọc. Không merge/copy về root, push/publish, reset/clean/stash mù, bootstrap lại hoặc tự clone từ root HEAD.
- Khi user gửi goal này, được sửa code/asset/scene/tests trong Runtime ownership, import/compile/EditMode/PlayMode/observe demo trong đúng checkout, local commit/tag. Không xin lại permission cho routine work đã cấp.
- Writable: `Assets/_Project/Features/FortressRuntime/**`, `Assets/_Project/Tools/Editor/FortressIntegration/**`, `Assets/_Project/Scenes/FortressGameplay/**`, `docs/Fortress/Reports/codex/**` và bốn guide Runtime trong Tasks.
- Ngoại lệ: nhận **commit migration config-only** Authoring đã ký MIGRATION_READY theo plan; sau full handoff READY nhận đúng AG delivery commits. Không tự sửa AG-owned files, Packages/ProjectSettings/URP settings. Correction gửi owner.
- Giữ Player/Enemies/Combat/CameraSystem và prefab gốc, Input Actions cũ, Sandbox_Player/source art; scene demo riêng được có Player instance overrides hợp lý và được kiểm chứng. Không làm lại movement, damage hoặc HP.
- Không tự viết `.meta`, cài phần mềm toàn máy, đổi global MCP/config, chấp nhận terms/license hoặc đóng Editor không thuộc swarm. Có quyền ghi duy nhất thư mục coordination dùng chung `.fortress-workspaces/.round2-editor-lease/` theo plan.

## Preflight và native Swarm

1. Ghi `Reports/codex/round2/preflight.md`, `status.md`: checkout/branch/HEAD/baseline/status, tools/Swarm sessions/Editor thực tế và limits. Đọc reports cũ nhưng chưa ký lại VERIFIED từ 32+24 test 6000.5.
2. Checkpoint owned work sau review; không stage `%ALLUSERSPROFILE%/`, credentials/caches hoặc `git add -A`. Không ghi đè untracked Runtime/metadata/report đã có.
3. Dispatch worker **door behavior**; worker **fixture+validator**; worker **coverage/guides đọc trước**; reviewer chỉ đọc. R2-01/R2-02 song song vì file lease khác nhau; không cho hai worker sửa cùng test file hoặc binder. Orchestrator sở hữu handoff/Editor/integration và giao thêm lease cụ thể khi dependency sẵn sàng.
4. Native Swarm không có thật thì BLOCKED_ENV `SWARM_UNAVAILABLE`; không giả workers/reviewer hoặc tự đổi sang solo.
5. Mặc định **một global Editor writer/process cho cả hai swarm**. Acquire directory lock atomic, giữ actual owner/PID/checkout/UTC; freeze Assets/Packages/ProjectSettings writers của checkout trong cửa sổ import/test. Release chỉ sau khi instance của mình kết thúc. Tuân thủ recovery/queue trong plan; không xóa lease writer sống.
6. Mọi live command phải target `D:/thietkegame/.fortress-workspaces/codex`; không dùng MCP pin root/Authoring. Check Editor version từ binary/runtime, không chỉ file manifest. Không chạy test vòng 2 bằng 6000.5.0f1.

## R2-01 — Fix door mất lệnh trong transition

- Trace `DoorController.Open/Close/SetActive/CompleteOpening/CompleteClosing` và callers/events trước sửa.
- Giữ public API. `Open/Close/SetActive(bool)` ghi nhận desired state mới nhất; hoàn tất transition hiện tại rồi thực hiện desired state, không đảo animation giữa chừng hoặc bỏ lệnh.
- Regression: Opening→request close; Closing→request open; true/false/true nhanh; repeated same request; lock/re-entry; collider thay đúng completion, event không lặp.
- PlayMode qua hold lever/pressure plate với timeout **0.35s khác 0**, nhả trước completion: cuối cùng Closed và blocker enabled. Không dùng timeout 0 để né case mất lệnh.
- Giữ tests chest/lock/loot và movement hiện có. Worker không sửa fixture/validator files của worker khác.

## R2-02 — Fixture thật và validator bắt sai wiring

- Đọc `FortressDevelopmentFixture` và `FortressRuntimeValidator`, tests hiện có. Bảo toàn FORTRESS-1 JSON và public entrypoints.
- Thêm negative tests trước fix: door có collider nhưng blocker array rỗng/sai; trap thiếu damage reference; actor không có usable input; hazard không trigger phù hợp; missing script/reference. Không được `passed` chỉ vì component có mặt.
- Gán door blocker/lock, CyclicTrap damage link, interaction action hiện có (clone ownership đúng), lever/plate→door/platform/trap events; fixture độc lập với code/asset mới AG trước integration.
- Actor/collider/input phải dùng được, visual development được ghi nhãn rõ; không mô tả SpriteRenderer chưa có sprite như demo hình ảnh. Audio thiếu clip là optional silent fallback, required gameplay wiring thiếu thì failed/blocked theo bằng chứng.
- PlayMode trên **fixture thật** chứng minh input→door/collider, hold release, trap damage chỉ phase có hại, Health/i-frame không bypass. Save dev fixture scene bằng Unity trong Runtime scope rồi reload/validate serialized refs.
- Test validator phải thực sự reject wiring hỏng và accept fixture đã sửa; không hạ tiêu chí để giữ C11 VERIFIED.

## R2-00/R2-03 — Migration receipt và gate runtime

- Trong khi worker sửa code, đọc chờ có điều kiện `Authoring/Reports/antigravity/round2/migration-handoff.json`. Đừng chạy Unity mới trước payload/lease; có thể tiếp tục source/tests/docs độc lập.
- Khi MIGRATION_READY: kiểm baseline/6000.6.9f1/source SHA/config-only allowlist/hash/evidence; fetch chính xác commit/ref cần, cherry-pick **chỉ sourceMigrationCommit**, không kéo toàn Authoring branch.
- Ghi `round2/migration-receipt.md` với source/applied SHA, payload hash và compile evidence; hai SHA có thể khác do parent khác. Nếu conflict hoặc package config sai, gửi Authoring correction thay vì sửa AG-owned settings.
- Chạy full Runtime EditMode + PlayMode trên 6000.6.9f1: các case lịch sử còn applicable và regression mới. Xác minh real Player rider ngang/dọc/drop/jump-off, hazard interval/multi-hurtbox/i-frame/perfect dodge, destruction/loot một lần, trap/reset/audio, fixture mới.
- Xuất coverage từng C00–C11 và canonical runtime-validation mới, phân biệt gate branch với integrated gate. Commit owned work và Unity-generated metadata, giữ evidence cũ làm lịch sử.

## R2-04 — Nhận full Authoring READY và bind (C12)

- Chỉ nhận full authoring sau canonical `authoring-handoff.md` ký READY trên 6000.6.9f1; không nhận PENDING_UNITY_VERIFICATION hoặc 16 Passed cũ.
- Authoring sẽ freeze bằng local tag **`refs/tags/fortress-authoring-r2-ready-v1`** (correction dùng v2 mới). Resolve sang actual SHA, kiểm full diff/history/baseline/owner, testedSourceCommit/hashes/ordered commits, freeze danh sách rồi mới apply.
- Nhận các clean authoring delivery commits hiện có và correction vòng 2; không cherry-pick baseline hoặc migration payload đã áp dụng; ghi source→applied mapping. Không fetch/cherry-pick legacy `fortress/antigravity`, không ours/theirs mù khi conflict.
- Cần thay AG output: ghi `Reports/codex/round2/authoring-change-request.md` với path/hunk/evidence/expected/actual; Authoring tạo correction. Không sửa source AG từ Runtime.
- Tạo/reuse `FortressGameplayBinder` và composed prefabs/variants trong Runtime. Catalog GUID/ID/hook/Animator/state/completion phải dùng thật; không sửa visual source prefab hoặc đợi event không tồn tại ở static fallback.
- Test generation lần hai/customization/reload refs; lever/plate→gate/elevator/trap; chest/destruction callback một lần; actual layers/masks/collision/rider/grounding. Dùng existing Player với demo-instance overrides hợp lý, không chỉnh prefab gốc.

## R2-05/R2-06 — Demo, toàn bộ AC và nghiệm thu (C13–C14)

- Tạo `Assets/_Project/Scenes/FortressGameplay/Demo/GothicFortress_Demo.unity` bằng actual art và existing Player/camera. Đủ mọi mục master 21: walls/ground/ceiling/platforms/vertical/one-way/gate/lever/chest/torch/magic/hazard/mover/decor/background/foreground/light.
- Quan sát Unity demo thực trên 6000.6.9f1, chứng minh frameCount tiến, console, input/short hold, rider/one-way, damage/i-frame/loot/destruction và pixel clarity. Screenshot tĩnh không chứng minh simulation đang chạy.
- Sau integration, compile/relevant full EditMode/PlayMode, authoring validators/tests applicable; kiểm refs/scripts/sprites/controllers/clips/RuleTiles/collision/sorting và hierarchy thật. Nếu source/asset đổi sau test phải rerun proof bị ảnh hưởng.
- Đối chiếu **từng C00–C14 và A00–A13**, master 26–27 trong `round2/acceptance-matrix.md`; hoàn tất mọi phần buildable, không giới hạn vào findings cũ. Missing art giữ exact list/draft/missing, không bịa art/âm thanh/map approval.
- Viết bốn guide `README_Unity_Setup.md`, `COLLISION_AND_PHYSICS_GUIDE.md`, `INTERACTIVE_OBJECT_GUIDE.md`, `PRODUCTION_WORKFLOW.md`; xác minh sáu guide nhận từ AG, đủ **10 guides**.
- Cập nhật canonical status/runtime-validation/runtime-handoff và `Reports/codex/final-validation.md`, kèm evidence mới trong round2. Reviewer độc lập đọc AC/diff/wiring/evidence/scope trước sign-off.

## Proof, trạng thái và hoàn thành

Theo mẫu commands/schema trong plan. Mọi command qua RTK, UTF-8; test CLI dùng positional project + `--editor-version 6000.6.9f1`; live commands có `--project-path`. Không tự thêm flag/package/install từ trí nhớ. Lưu actual exit code/XML/Editor log đã lọc secret, UTC, source commit và hash Assets/Packages/ProjectSettings được test, screenshots/observations và actual worker/reviewer IDs.

`Passed` chỉ cho kiểm tra đã chạy cùng source/version. C11 branch pass không thay C12–C14. Tách FAILED/BLOCKED_ENV/BLOCKED_INPUT/NOT_RUN; chưa build thì nói chưa build, không bịa build pass. Không dùng source thiếu để che lỗi code/config có thể sửa.

Nếu full authoring chưa READY khi runtime gate xong: ghi **READY_FOR_AUTHORING_HANDOFF**, xuất report/runtime commits và next-action, kết thúc lượt nhánh có mốc resume, không busy-wait hoặc tự tạo AG assets. Người dùng gửi lại cùng goal vòng 2 sau handoff thì đọc ledger và tiếp tục R2-04, không làm lại task còn proof hợp lệ.

Khi hoàn tất integrated gate: báo **READY_FOR_USER_REVIEW**, actual tested/delivery SHAs, task VERIFIED, commands/results/evidence, blockers/missing input còn lại và limits. Người dùng là bên nghiệm thu; không tự gọi production-ready hoặc áp về root. Chặn Editor/license/OOM thì giữ work, báo đúng điều kiện, tiếp tục phần độc lập; không dùng Editor cũ để ký bản mới.

Bắt đầu từ preflight/checkpoint và dispatch native Swarm. Mục tiêu là sửa và hoàn tất hệ thống theo plan, không xuất thêm một kế hoạch rồi dừng.
