# Giao việc sau merge ngày 19/11/2026

## Thứ tự đã chốt

| Phiếu | Việc | Điều kiện nhận |
| --- | --- | --- |
| G1 | Xác nhận nhánh game sau merge | Bắt đầu ngay |
| A1 | Chạy lại R10 và chốt bàn giao Fortress Authoring | Bắt đầu ngay khi có lượt dùng Unity |
| A2 | Nhận Authoring vào Fortress Runtime và nghiệm thu demo | A1 có `READY`, tag đóng băng và XML 20/20 |
| NG1 | Chốt checkout và chuẩn bị nền Nhà Giam Giếng Chuông | G1 đạt |
| NG2 | Dựng mẫu phòng C–D | NG1 bàn giao snapshot và assembly |
| NG3 | Nghiệm thu độc lập mẫu phòng C–D | NG2 bàn giao bản mẫu đã lưu |
| G2 | Chuẩn bị nhánh sạch để nhóm review | Có thể rà diff trong lúc chờ G1. Bàn giao sau G1 và sau khi chốt version Unity chung |
| G3 | Sửa importer GothicDungeon khi cần đổi manifest | Chỉ phát nếu có thay đổi manifest cần áp dụng lên palette đã tồn tại |

G1 và A1 có thể chuẩn bị đồng thời. Xếp lượt chạy Unity theo lease hiện hành. Mẫu C–D dùng bốn palette GothicDungeon gốc, nên có thể tiến hành khi G1 đạt mà không cần chờ A2. Sau khi NG3 đạt, mới phát các phiếu mở rộng phòng `M2–M5` trong kế hoạch v7. [Kế hoạch v7](../Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md#0-điều-phối-phiên-dựng) · [Kế hoạch Fortress vòng 2](../superpowers/plans/2026-11-16-fortress-round2.md#4-swarm-và-editor-lease)

## Mốc bàn giao

| Cây làm việc | Mốc đã kiểm tra | Trạng thái |
| --- | --- | --- |
| Game `D:/thietkegame` | `local/unity-sync-2026-11-19` tại `88069c8`, gồm snapshot `0a8f583` và `origin/main` tại `1521a95` | Hai phía được giữ nguyên qua merge. Game dùng Unity `6000.6.9f1`, URP `17.6.0`, Input System `1.20.1`. Chưa xác nhận chạy `Main_Level` sau merge |
| Authoring `.fortress-workspaces/antigravity` | `fortress/antigravity-clean` tại `b837ccd`. Bản sửa R10 nằm trong `ca745bc` | Preview và validator có bằng chứng. XML trước sửa đạt 19/20. Handoff `PENDING_R10_RERUN`, tag `fortress-authoring-r2-ready-v1` chưa có. Có thay đổi cấu hình chưa commit cần giữ |
| Runtime `.fortress-workspaces/codex` | `fortress/codex` tại `de9fc94`. Migration M2 đã nhận ở `63d1822` | Handoff `READY_FOR_AUTHORING_HANDOFF`. C12–C14 chờ Authoring. Có thay đổi chưa commit cần giữ |

Đối chiếu lại branch, HEAD và `git status` lúc nhận việc. Nếu revision đã tiến lên, kiểm tra diff và dùng bằng chứng mới gắn với revision đó. [Kết quả Authoring](../../.fortress-workspaces/antigravity/docs/Fortress/Reports/antigravity/round2/editmode-results.xml#L2) · [Trạng thái Runtime](../../.fortress-workspaces/codex/docs/Fortress/Reports/codex/round2/status.md#L30)

## Quy ước cho tất cả prompt

- Khi giao việc, gửi **prompt của phiếu** cùng mục này. Phạm vi thực thi có hiệu lực khi người dùng giao phiếu. Ghi rõ checkout thực tế, HEAD, trạng thái trước khi làm và những file thuộc phạm vi.
- Đọc `AGENTS.md`, `docs/ProjectConfig.yaml` và `AGENTS.md` con. Chạy shell qua `rtk`. Dùng CodeGraph trước khi lần code nếu checkout có index. Đối chiếu scene, prefab, `.meta`, manifest và lockfile trực tiếp khi kiểm tra reference hoặc cấu hình.
- Với Unity, dùng `unity-cli` và plugin tương ứng. Target đúng project path, dùng Editor đã cài `6000.6.9f1`. Dùng lease theo mục 4 của plan Fortress vòng 2 và chỉ có một Unity Editor writer khi chạy trên máy này. Trước khi chạy, xác minh owner/PID, dừng writer của checkout trong lượt import/test và giữ lease đến khi Editor thuộc phiên đã đóng. Giữ nguyên Editor hoặc lease của phiên khác còn hoạt động.
- Bảo toàn thay đổi có sẵn, chỉ stage file thuộc phiếu. Để Unity tạo metadata. Git commit hoặc tag chỉ thực hiện ở phiếu có ghi rõ. Mọi kết quả bàn giao nằm local.
- Trả `PASS`, `FAIL` hoặc `BLOCKED` theo bằng chứng, kèm checkout, HEAD, trạng thái tree, Editor thực tế, lệnh/thao tác, ca đã chạy, file bằng chứng và phần còn chờ. Ghi phiên bản file/manifest khi test tree có thay đổi. Nếu luồng yêu cầu reviewer/thử nghiệm độc lập, ghi người hoặc phiên thực hiện thật.

## G1 · Agent QA game sau merge

> Kiểm tra `D:/thietkegame` tại `local/unity-sync-2026-11-19`, mốc `88069c8`. Bạn được chạy import, compile và Play Mode để đo hiện trạng. Báo diff nếu Editor tự ghi file. Để việc sửa lỗi cho một phiếu riêng có file và bước tái hiện cụ thể.
>
> 1. Xác minh Editor `6000.6.9f1`, manifest/lock, URP và Input System. Mở `Assets/_Project/Scenes/Main_Level.unity`, xác nhận đây là scene được bật trong Build Settings. Chờ import/compile xong, lưu lỗi Console nếu có.
> 2. Chơi một lượt có đối kháng. Kiểm tra ánh sáng 2D, sprite/material, HUD máu/vũ khí/đạn, hiệu ứng đánh trúng, quái rơi vàng và HUD cập nhật khi nhặt. Mở một cuộn giấy, chọn nâng cấp, kiểm tra điều khiển được trả lại và chỉ số cập nhật.
> 3. Để Player chết, quan sát màn thua, bấm **Retry** và xác nhận scene nạp lại với HP, vũ khí/đạn, vàng và scroll ở trạng thái đầu lượt. Đối chiếu Console trong suốt lượt.
>
> **Đạt** khi vào được `Main_Level`, không có lỗi compile hoặc exception trong các luồng vừa chạy và Retry khởi tạo lượt mới đúng. Trả checklist từng bước, ảnh Game View, log/Console, cùng `git status` cuối. Nếu chỉ dừng ở import, ghi rõ những bước Play Mode còn chờ.

## A1 · Agent điều phối xác minh Fortress Authoring

> Làm việc tại `D:/thietkegame/.fortress-workspaces/antigravity`, nhánh `fortress/antigravity-clean`. Đọc `docs/Fortress/Goal-Round2-Authoring.md` ở cây gốc, plan vòng 2 và handoff trong checkout Authoring. Tiếp tục điều phối bằng Antigravity native Swarm theo plan. Giữ các thay đổi ProjectSettings có sẵn và ghi lại trạng thái khi test.
>
> 1. Đối chiếu XML cũ 19/20 và bản sửa `ca745bc`. R10 từng dừng ở kiểm tra khoảng cách `Fortress_Decoration_Palette`. Test hiện tính tâm ô bằng `grid.cellSize` và `tileAnchor`. Xác nhận source này có mặt ở revision chạy.
> 2. Dùng Unity `6000.6.9f1`. Chạy ca `R10_PaletteDisplaySpacing_And_MultiRoomAssembly_SaveReloadAndPhysics` nếu CLI hỗ trợ lọc. Sau đó chạy toàn bộ suite 20 ca EditMode Authoring. Dùng lệnh và cấu hình worker count trong mục Manual Verification của handoff. Lưu XML/log mới, giữ bằng chứng cũ và làm sạch token trước khi đưa log vào Git. Nếu lỗi OOM/Editor hoặc một assertion fail, ghi nguyên nhân theo log thực tế. Chỉ sửa file Authoring liên quan trực tiếp khi đã tái hiện lỗi và có bằng chứng cho cách sửa.
> 3. Reviewer đối chiếu kết quả spacing, ráp nhiều phòng, save/reopen và physics với chính revision được chạy. Chỉ khi đủ 20/20, đúng source và các điều kiện handoff đạt, cập nhật báo cáo, ký `READY`, commit phần thuộc phiếu và tạo tag local `fortress-authoring-r2-ready-v1` trên commit bàn giao. Nếu tag đã tồn tại, giữ tag cũ và dùng quy tắc correction của handoff.
>
> **Bàn giao** SHA source đã test, SHA commit/tag đã resolve, XML và log, sign-off, danh sách commit được phép nhận, cùng các giới hạn palette/preview đã biết. Khi thiếu bằng chứng, giữ trạng thái chờ với ca lỗi và bước chạy lại cụ thể.

## A2 · Agent tích hợp Fortress Runtime

> Chỉ nhận phiếu khi A1 có handoff `READY`, tag đóng băng đã resolve và XML 20/20 khớp source. Đọc `docs/Fortress/Goal-Round2-Runtime.md` ở cây gốc và `runtime-handoff.md` trong `D:/thietkegame/.fortress-workspaces/codex`. Dùng Runtime ở mốc `de9fc94` hoặc revision được bàn giao mới hơn. Checkout Runtime đang có thay đổi local. Tạo nhánh/worktree tích hợp riêng từ revision đã xác nhận và ghi đường dẫn mới vào báo cáo. Điều phối tiếp theo plan Runtime vòng 2.
>
> 1. Fetch tag từ checkout Authoring local, resolve SHA và đối chiếu toàn bộ danh sách commit, payload, `.meta`, GUID/fileID, asmdef và material. Runtime đã nhận migration M2 `2ce47b47` tại `63d1822`. Xác minh patch trước khi bỏ qua commit đã nhận. Áp dụng theo thứ tự handoff.
> 2. So `TagManager.asset` và `Physics2DSettings.asset` theo từng layer, ID sorting và cặp collision. Authoring có nhiều sorting layer, trong khi cây game sau merge chỉ có `Default`. Giữ reference và va chạm của cây nhận. Ghi cụ thể từng thay đổi cần cho Fortress.
> 3. Hoàn tất C12–C14 theo plan `R2-04` đến `R2-06`: binder và prefab đã gán, `GothicFortress_Demo.unity`, kiểm thử tích hợp và báo cáo cuối. Dùng asset và component hiện có. Kiểm Game View, va chạm, đèn Global Light 2D và luồng tương tác trong demo theo AC của plan.
>
> **Bàn giao** nhánh tích hợp local, SHA Authoring đã nhập, diff settings, kết quả EditMode/PlayMode và ảnh/log demo. Mẫu Giếng Chuông dùng các phiếu NG. Nếu sau này cần dùng Fortress trong game, dùng nhánh tích hợp làm nguồn đã kiểm chứng để lập danh sách asset/config cần chuyển.

## NG1 · Agent chốt snapshot và assembly cho Giếng Chuông

> Sau G1 đạt, lấy revision game đã kiểm tra làm nguồn. Tạo nhánh/worktree local riêng cho mẫu v7, ví dụ `local/nggc-cd-pilot` tại `D:/thietkegame/.fortress-workspaces/nggc-pilot`. Giữ nhánh snapshot `local/unity-sync-2026-11-19`. Đọc mục B0/B1 của `docs/Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md` và phần Snapshot trong `docs/Tests-Nha-Giam-Gieng-Chuong.md`.
>
> 1. B0. Kiểm tra checkout có thiết kế/sơ đồ v7, bốn palette GothicDungeon gốc, Player, sandbox, settings, package lock và metadata. Xuất manifest SHA-256 theo runbook vào thư mục bằng chứng ngoài checkout Unity đích. Ghi HEAD và xác minh manifest.
> 2. B1. Dùng Unity chuẩn bị `Assets/_Project/Features/NhaGiamGiengChuong/` cùng các thư mục và đúng ba asmdef theo plan. Dùng mẫu test assembly có sẵn, đối chiếu chiều reference và giới hạn EditMode trong Editor. Xác nhận compile, để Unity tạo `.meta` một lần.
>
> **Bàn giao** commit B1, path checkout, manifest, GUID của ba asmdef và kết quả compile. Commit cục bộ chỉ gồm phần chuẩn bị B1. Giao cùng commit cho agent dựng mẫu. Nếu đã có các file B1 ở revision mới, xác minh và dùng lại.

## NG2 · Agent dựng mẫu C–D

> Nhận checkout và commit B1 từ NG1. Đọc thiết kế v7, mục 2 của plan và đặc tả A01/P01. Bạn sở hữu `Rooms/CD.prefab`, `Assets/_Project/Scenes/NhaGiamGiengChuong.unity`, `Tests/EditMode/CDGeometryTests.cs`, `Tests/PlayMode/PilotPlayTests.cs` và metadata do Unity sinh cho các file đó. Giữ nguyên bố cục do Level Designer quyết định. Dùng palette GothicDungeon gốc, Player và wiring từ sandbox.
>
> 1. Dựng C `x=42–56` ở `y=0`, D `x=22–42` ở `y=8`, bệ nghỉ `y=4,5`, sàn đỉnh `y=8` và xích I1 tâm `(46;4)` theo kích thước/layer/effector ghi trong plan. Giữ đúng cửa nối và một collider trên mỗi mặt đỡ. Dùng collider riêng cho địa hình, giữ trang trí đi xuyên. Đặt chân Player mẫu `(46;0)`, gán camera, HitStop và VfxSpawner.
> 2. Lưu và mở lại scene. Chạy A01/P01 ở gate `Q-M1`, dùng input và physics của Player thật để leo xuyên hai bệ, qua D, quay lại và xuống đáy C. Ghi cao độ chân, mặt đỡ, trạng thái chặn cửa và mọi sai khác ảnh hưởng đường đi. Giữ `Main_Level` là entry game hiện tại.
>
> **Bàn giao** commit mẫu, manifest mới cùng revision, số đo, ảnh Scene View có collider và Game View 16:9. Tách quyết định art/bố cục cần Level Designer duyệt khỏi kết quả hình học. Sau khi giao, giữ nguyên bản mẫu trong lượt kiểm tra độc lập.

## NG3 · Agent QA độc lập cho Q-M1

> Nhận chính commit mẫu đã lưu và manifest của NG2. Đọc mục Q-M1 trong `docs/Tests-Nha-Giam-Gieng-Chuong.md` và mục 2 trong plan. Xác minh manifest, chạy A01 EditMode và P01 PlayMode với `NGGC_QA_GATE=Q-M1`. Lưu `XML`, log và ảnh riêng của lần chạy. Dùng scene đã mở lại từ đĩa.
>
> Đối chiếu chiều cao hai bệ, vị trí xích, mép ráp, cửa và collider. Quan sát Player xuất hiện tại `(46;0)`, leo qua `y=4,5`, đứng ở `y≈8`, sang D, quay lại và xuống `y≈0`. Giữ lượt đi do input thật, theo P01. Chụp Scene View có collider và Game View 16:9. So manifest sau khi chạy.
>
> **Đạt** khi A01 và P01 mỗi mã xuất hiện đúng một lần và Passed, bản mẫu lưu mở lại đúng, bằng chứng có cả hai chiều di chuyển và Level Designer có thể duyệt sai khác hình. Nếu fail, trả ca lỗi, kỳ vọng, số đo thật và vị trí bằng chứng cho agent NG2. Sau Q-M1 đạt, phát các phiếu mở rộng phòng theo plan v7.

## G2 · Agent chuẩn bị nhánh sạch để chia sẻ

> Dùng `origin/main` ở mốc `1521a95` làm điểm so với `88069c8`. Rà diff trong lúc chờ G1. Sau khi G1 đạt và nhóm chốt Editor chung, tạo nhánh/worktree local riêng cho thay đổi được chọn. Giữ nhánh snapshot và lịch sử `main` hiện tại.
>
> 1. Lập danh sách file theo mục đích sử dụng: migration Unity/UPM/URP, art GothicDungeon kèm `.meta` và source cần cho importer, tài liệu, tooling theo từng máy. Kiểm tra cấu hình `.codex/config.toml`, Unity Cloud project/organization và config cá nhân trước khi chọn diff. Giữ linkage Cloud của nhóm trừ khi đã có chủ đích đổi được xác nhận.
> 2. Tách commit nâng Unity và cấu hình runtime khỏi commit art/authoring. Giữ đúng manifest/lock, metadata, GUID và đường dẫn source mà tool thật sự dùng. Sửa các giá trị lỗi thời trong `docs/ProjectConfig.yaml` và `docs/MapDesign.md` theo config của **nhánh bàn giao**, gồm version package và entry `Main_Level`.
> 3. So diff nhánh bàn giao với `origin/main`. Xác nhận nội dung gameplay/scene của upstream còn nguyên, asset/reference đi cùng commit và không kèm đường dẫn cá nhân. Nếu nhánh đã chọn khác revision/bộ settings đã qua G1, chạy lại smoke phù hợp trên revision bàn giao trước khi báo sẵn sàng.
>
> **Bàn giao** nhánh local, danh sách commit, các quyết định config cần xác nhận, diff và bằng chứng. Nếu version chung còn mở, giao danh sách file và proposal tách commit để nhóm quyết trước khi tạo bản chia sẻ.

## G3 · Agent sửa importer khi có manifest mới

> Chỉ bắt đầu khi có một thay đổi manifest thực sự cần chạy lại. Làm trên nhánh local riêng từ revision đã chốt. Đọc `GothicDungeonImporter.Import()` và `Verify()` ở `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`, manifest thật và prefab palette liên quan. Tái hiện bằng cách thêm một entry paintable vào group đã có trên fixture hoặc checkout thử nghiệm.
>
> Sửa nguyên nhân palette cũ bị bỏ qua sau khi tile mới đã được ghi. Chọn cách cập nhật palette giữ nguyên GUID, ô đã sắp, PPU 32/128 và reference hiện có. Kiểm tra manifest và các đối tượng có thể cập nhật trước khi ghi để lỗi xác định được không để dở một nửa. Giữ `.meta` do Unity quản lý. Kiểm một ca thêm tile vào group có sẵn rồi chạy lại lần hai, xác nhận palette đầy đủ, các ô cũ còn nguyên, tile mới không bị nhân đôi và `Verify()` đạt.
>
> **Bàn giao** commit importer, asset thay đổi cần thiết của ca thật và bằng chứng. Với công việc chỉ dùng bộ tile đã import, tiếp tục dùng palette sẵn có.
