# GOAL VÒNG 2 — Antigravity Swarm — Authoring & Unity 6000.6.9f1

Bạn là **Antigravity Swarm orchestrator của nhánh Authoring**. Người dùng chọn native Swarm để các subagents làm task độc lập đồng thời, có reviewer chỉ đọc; không chuyển nguyên prompt cho một worker rồi gọi đó là Swarm.

## Goal và thứ tự ưu tiên

Hoàn tất **P2-00, A2-01, M2, A2-02, A2-03, A2-04** trong plan vòng 2, sửa finding hiện có và xác minh lại toàn bộ **A00–A13** theo FORTRESS-1/master. Chuẩn duy nhất: **Unity 6000.6.9f1**. Sau migration, Runtime có thể phát triển đồng thời; sau authoring READY, Runtime tích hợp cuối.

Đọc đầy đủ trước triển khai:

1. `D:/thietkegame/docs/superpowers/plans/2026-11-16-fortress-round2.md` — plan chuẩn, bao gồm scope, migration payload, lease, AC/test và evidence.
2. AGENTS của checkout và `D:/thietkegame/AGENTS.md`, RTK được tham chiếu, skill Unity CLI/package/2D/sprite/UI tương ứng.
3. Checkout `docs/ProjectConfig.yaml`, `docs/Fortress/Tasks.md`, `# MASTER PROMPT — COMPLETE UNITY 2D.md`, ledger/handoff/reports/tests/source hiện tại.

Goal/plan vòng 2 thay chỉ dẫn version/executor/migration/phối hợp vòng 1 ở các điểm ghi rõ; giữ các AC và ranh giới còn lại. Không sửa tài liệu gốc để tự cho mình pass.

## Checkout và authorization

- **cwd/project:** `D:/thietkegame/.fortress-workspaces/antigravity`.
- **Branch:** `fortress/antigravity-clean`. HEAD lúc lập plan: `c99a7f1a0c3d15436a43abc636e758409b29b082`; recheck khi bắt đầu/resume, không reset nếu đã tiến thêm.
- Baseline lịch sử: `411a440ed12407971163d74e2f0a12bc70ad41b2`. Giữ 5 clean delivery commits và revision F1/F2 đang sửa; không dùng legacy branch `fortress/antigravity`, không rewrite/amend lịch sử đã giao.
- Root `D:/thietkegame` và checkout Runtime/Codex **chỉ đọc**. Không ghi root, push/publish, tự chuyển work về root, cài phần mềm toàn máy, đổi global settings/MCP hoặc tự xử lý license/terms.
- Khi người dùng gửi goal này, được sửa code/asset/config, dùng Editor đã cài, import/compile/EditMode/PlayMode/preview, local commit/tag **đúng scope plan**. Không xin lại quyền cho routine work đã được cấp. Việc riêng tư hoặc scope mới phải báo chính xác; phần độc lập vẫn tiếp tục.
- Được ghi toàn bộ vùng AG trong Tasks mục 4, reports/sáu guide; thêm allowlist migration mục 3 plan: ProjectVersion, ProjectSettings.asset, ShaderGraphSettings, UniversalRP.asset, ProjectConfig. Các file này chỉ thay để chuẩn hóa version, không nhân tiện thay gameplay/cấu hình không liên quan.
- Chỉ thư mục điều phối `.fortress-workspaces/.round2-editor-lease/` được dùng chung có quyền ghi. Không sửa preparation.json/AGENTS/master/Tasks/goal cũ. Không tự viết `.meta`.

## Preflight và điều phối Swarm

1. Xác nhận path/branch/HEAD/status, input hashes root trong plan, baseline, source cần giữ; đọc AGENTS con. Checkpoint owned changes, không blanket add hoặc ghi đè dirty work.
2. Ghi `docs/Fortress/Reports/antigravity/round2/preflight.md` và `status.md`, actual Swarm sessions/tools/Editor capabilities. Nếu native Swarm không có: BLOCKED_ENV `SWARM_UNAVAILABLE`, không giả worker/reviewer hoặc âm thầm làm solo.
3. Dùng ticket/file lease theo plan. Tách worker sửa importer/assembly; worker regression asset/physics; worker catalog/guides/coverage; reviewer chỉ đọc. Các writer đụng cùng importer/test file chạy nối tiếp hoặc chuyển lease, không sửa chồng.
4. Orchestrator giữ **global Editor lease một writer/process cho cả hai swarm**. Acquire atomic directory; xác minh owner/PID/project/version; đóng băng mọi Assets/Packages/ProjectSettings writer trong checkout trước import/test để tránh auto-refresh. Thực thi và release theo mục 4 plan.
5. Tất cả CLI live commands phải target đúng project này. Không dùng Unity MCP global đang pin root/Runtime. Đọc skill và discover schema/commands thật, không đoán API.

## Công việc bắt buộc

### A2-01 — Compile blockers trước phiên Unity mới

- Đọc lại body tại `GothicDungeonImporter.cs:1168`: phục hồi khai báo `TryReadPngDimensions(string filePath, out int width, out int height)`, đối chiếu baseline và mọi caller.
- Sửa ranh giới API của `TryResolveSequenceTextureDimensions` (hiện internal trong TilesetTools.Editor nhưng caller ở FortressProduction.Editor). Mở đúng method public nếu vẫn cần API đó; không thêm dependency vòng hoặc dời parser bừa sang caller.
- Giữ F1/F2, missing-texture rejection và NPOT/frame validation; code sửa không có nghĩa test đã Passed.

### M2 — Xuất migration chung sớm cho Runtime

- Source chuẩn: ProjectVersion/manifest/lock ở root với hashes plan. Đồng bộ snapshot đã resolve, dùng đúng 6000.6.9f1 đã cài, không tự nâng package khác hoặc cài Editor toàn máy.
- Dưới lease resolve/import/compile; giữ ID layer/matrix/authoring work; review mọi serialization change. Direct dependencies khớp root manifest; lock được resolve lại cần diff/hash/rationale.
- Nếu version mới lộ thêm compile/API errors trong Authoring, sửa phần compile của A2-02 ngay trước gate M2 bằng file lease đúng owner; không để hai task chờ nhau. Code fix vẫn commit riêng với migration config-only.
- Tạo commit **config-only** với đúng payload allowlist. Không gộp code fix, generated assets, TagManager/Physics2D hoặc report.
- Sau khi có SHA commit thật, viết `Reports/antigravity/round2/migration-handoff.json` đúng schema plan: status `MIGRATION_READY`, 6000.6.9f1, baseline/source SHA, file hashes, verification/exit/evidence, actual signer/UTC.
- Đây là ngoại lệ vòng 2 cho phép Runtime nhận **chỉ migration commit** trước full authoring READY. Không giao cả nhánh đang viết; freeze payload, correction có revision mới.

### A2-02/A2-03 — Authoring hoàn chỉnh trên version mới

- Chạy A02–A13 và R01–R08 hiện có: tối thiểu 18 test methods phải được discover trước mọi bổ sung, lấy case count thực từ XML. Không xóa/skip test khó để pass.
- R07: manifest file thật + texture/frame thiếu/unreadable phải báo error và không verified/imported. R08: artifact thật NPOT 96x24/54x26, pivot (0.25,0.75), fps12, loop false, order [2,0,1], giữ native dimensions và rerun GUID.
- R03: physics jump-through/land/continuous walk-off; đi ngang thực qua mép từ trạng thái chạm platform tới rơi/chạm floor, không teleport thay proof.
- Verify rerun/customization, topology 47, tile/collision modes, room hierarchy, 9 presets, sorting/light/pixel camera, ambient, Animator/controller/frame refs và missing source behavior. Fix các regression trong ownership thay vì tạo framework khác.
- Chạy preview Unity thực trên 6000.6.9f1, chụp evidence mới. Không dùng 16 Passed và screenshot 6000.5 cũ để ký version mới.
- Catalog FORTRESS-1 đúng stable IDs/path/GUID/Animator/completion/hooks/PPU/status/missingInputs; phần thiếu art giữ draft/missing. Inventory snapshot 84 entries/57 ready/24 draft/3 missing chỉ là mốc đối chiếu, không hardcode.
- Đối chiếu từng A00–A13 và master liên quan trong `round2/acceptance-matrix.md`; hoàn tất mọi phần buildable, sửa sáu guide theo workflow thật.

## Evidence, handoff READY và correction

- Report mới: `docs/Fortress/Reports/antigravity/round2/`, gồm preflight/status/migration-handoff/evidence-index/acceptance-matrix, XML/log/screenshots/validator results. Lưu command, UTC, actual exit code, tested source commit và hash code/asset/config; không in secret.
- Cập nhật canonical `Reports/antigravity/status.md`, `asset-catalog.json`, `authoring-handoff.md`; không để bên nhận đọc status cũ. Giữ log/XML vòng 1 làm lịch sử, không ghi đè thành evidence vòng 2.
- Không tuyên bố READY khi compile/suite/visual còn chưa chạy. Thiếu Editor/license/OOM/package ghi BLOCKED_ENV riêng; tiếp tục phần độc lập, không âm thầm dùng 6000.5.0f1.
- Reviewer độc lập kiểm AC/source/serialized refs/evidence/scope/full delivery diff. Tự review thì ghi self-review, không giả independent review.
- Trước bàn giao: local commits chỉ owned delta + metadata Unity sinh + migration ngoại lệ; không legacy/credentials/caches/changes Runtime. Handoff ghi ordered delivery commits, migration commit riêng, layers/matrix/API/fallback thật, exact missing inputs và six guides.
- Commit evidence cuối rồi tạo **tag local mới `fortress-authoring-r2-ready-v1`** trỏ final HEAD. Handoff ghi ready ref và testedSourceCommit; báo SHA cuối lấy thực bằng `git rev-parse` tag. Không điền SHA giả/tự tham chiếu hoặc move tag đã giao. Correction tạo commit/tag v2 và evidence mới.
- Khi nhận change request từ Runtime: chỉ sửa vùng Authoring, test lại phần bị ảnh hưởng và bàn giao correction; không sửa code/scene Runtime để giải xung đột.
- A13 READY là authoring đã được kiểm chứng theo input có sẵn; **không phải** toàn hệ thống đã nghiệm thu. Không đợi Runtime để hoàn tất authoring delivery.

## Hoàn thành lượt

Báo tiếng Việt: task đã VERIFIED và evidence mới; migration SHA/hash; delivery ref + actual final SHA + ordered commits; missing art/env; reviewer; cách Runtime nhận bàn giao. Chưa build thì ghi chưa build. Nếu bị chặn, ghi rõ task/condition/next action và mốc resume; không busy-wait.

Bắt đầu từ preflight/checkpoint và dispatch native Swarm. Thực hiện plan tới khi authoring READY có bằng chứng hoặc có blocker cụ thể; không kết thúc sau một kế hoạch mới.
