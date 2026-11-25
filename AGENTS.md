# Chỉ dẫn bắt buộc cho AI làm việc với project

Đọc file này trước khi làm việc. Quy tắc quan trọng nhất: **CẤM ĐOÁN, CẤM BỊA. Mọi kết luận về project phải có bằng chứng.**

## 1. Kiểm tra bằng chứng trước khi kết luận

- Đọc code, prefab, scene và cấu hình liên quan trước khi đánh giá tính năng hoặc đề xuất task.
- Không dựa vào tên file, cấu trúc thư mục, comment hoặc tài liệu kế hoạch để khẳng định một tính năng thiếu, chưa làm hoặc đã chạy đúng.
- Phân biệt rõ: người dùng xác nhận; có implementation trong code; có cấu hình trong prefab; có đối tượng/reference trong scene; đã quan sát chạy thực tế.
- Chỉ nói đã đọc những file thực sự đã đọc. Không nói đã kiểm tra toàn project khi mới đọc một phần.
- Mọi vấn đề phải nêu file và đoạn code/cấu hình làm bằng chứng. Nếu chưa đủ bằng chứng, ghi rõ chưa xác định và tiếp tục đọc phần liên quan; không bịa kết quả chạy thực tế.
- Không coi không có một state/class mang tên mong đợi là bằng chứng duy nhất rằng cơ chế không tồn tại. Tìm implementation trong toàn bộ phạm vi liên quan.
- Khi người dùng yêu cầu đối chiếu project khác hoặc cách làm thực tế, xem nguồn gốc có thể kiểm chứng, dẫn nguồn và phân biệt sự thật từ nguồn với đề xuất cho project này.

## 2. Chỉ thêm hoặc sửa khi người dùng cho phép

- Mặc định chỉ đọc. Chỉ thêm/sửa/xóa code khi người dùng cho phép rõ ràng trong phạm vi cụ thể.
- Yêu cầu đọc code, kiểm tra, giải thích, so sánh hoặc lập kế hoạch không phải là quyền sửa code.
- Quyền tạo/sửa tài liệu chỉ áp dụng cho tài liệu được yêu cầu; không mở rộng thành quyền sửa gameplay, prefab, scene, asset, package hay ProjectSettings.
- Việc sửa prefab, scene, ScriptableObject asset, Input Actions, package và cấu hình project cũng cần quyền phù hợp từ người dùng.
- Không chạy thao tác Unity có thể làm thay đổi project, build, Play Mode hoặc test nếu chưa được cho phép trong nhiệm vụ hiện tại.
- Nếu đã được cho phép, thực hiện đúng phạm vi đó; không xin lại cùng một quyền và không tự mở rộng phạm vi.
- Không ghi đè thay đổi có sẵn của người dùng. Kiểm tra trạng thái thay đổi trước khi ghi file được phép sửa.
- Không tự tạo/sửa/xóa file .meta.

## 3. Lập task dựa trên hiện trạng đã kiểm tra

- Kiểm tra implementation và wiring thực tế trước khi thêm task tạo, tích hợp hoặc hoàn thiện tính năng.
- Người dùng đã xác nhận player movement hiện có được coi là hoàn thiện; không tự tạo task làm lại/kiểm tra lại phần này.
- Người dùng đã xác nhận quái đã được đưa vào prefab. Không mặc định cần tạo hoặc đưa quái vào prefab lần nữa.
- Các bổ sung movement được người dùng yêu cầu phải được phân biệt với movement đã có. Kiểm tra implementation trước khi xác định phần việc cụ thể.
- docs/SprintPlan.md là đề xuất kế hoạch, không phải bằng chứng tính năng chưa tồn tại. Đối chiếu lại task với code/prefab/scene trước khi dùng kế hoạch.
- Task phải có đầu ra cụ thể và Acceptance Criteria dựa trên yêu cầu đã xác nhận. Không bịa lỗi, số liệu, thời gian hoàn thành hoặc trạng thái công việc.
- Estimate chưa có căn cứ thì để chưa chốt; không trình bày ước lượng như số liệu đã xác nhận.
- Không tự phân nhóm thành các vai trò cố định. Để người phụ trách trống cho thành viên tự pick task, trừ khi người dùng yêu cầu khác.
- Mã tham chiếu trong tài liệu không phải issue key Jira. Issue key thực tế chỉ được ghi khi đã lấy từ Jira.

## 4. Bắt đầu và kết thúc công việc

- Đọc docs/ProjectConfig.yaml để có ngữ cảnh; xác minh thông tin cần thiết bằng file project thực tế.
- Tìm các AGENTS.md ở thư mục con trước khi làm việc trong phạm vi đó, nếu có.
- Với yêu cầu đọc toàn bộ Assets, kiểm kê tất cả file và phân loại code, văn bản cấu hình, scene/prefab, hình ảnh/âm thanh và dữ liệu nhị phân. Nêu rõ phần đã đọc nội dung và phần chỉ kiểm kê.
- Báo cáo phạm vi đã đọc, bằng chứng chính, phần chưa xác định và mọi thay đổi đã thực hiện.
- Khi phát hiện nhận định trước đó sai, sửa nhận định theo bằng chứng. Không tự sửa implementation nếu người dùng chỉ yêu cầu kiểm tra.

## 5. Hiện trạng đã đối chiếu ngày 13/10/2026

Đây là mốc đọc file, không phải kết quả chạy Unity. Khi bắt đầu nhiệm vụ mới, xác minh lại các file liên quan nếu chúng đã thay đổi.

- Đã kiểm kê 4.717 file trong Assets, đọc 60 file C# và 2 shader; đọc/đối chiếu cấu hình serialized, prefab, scene, animation, Input Actions và metadata.
- Có 5 enemy prefab: Enemy_Zombie, Enemy_Rat, Enemy_Shield, Enemy_Archer, Enemy_Grenadier. Mỗi prefab đã có Health, Hurtbox, EnemyAnimator, EnemyController, Rigidbody2D, collider, animation controller và EnemyData tương ứng.
- Assets/_Project/Scenes/Sandbox_Player.unity đã có instance của cả 5 enemy prefab. Scene override đã gán _player, _projectiles và _vfx cho chúng. Không tạo task tích hợp lại các reference này như thể chưa có.
- Scene đã có Player.prefab, TrainingDummy, CameraFollow2D, VfxSpawner, HitStop, ProjectileSpawner, ground/platform và Chain_Test.
- Player trong Sandbox_Player đã được scene override gán VFX spawner, camera và hit-stop. Combat và feedback không chỉ mới tồn tại dưới dạng file code.
- Enemy_Archer.asset đã tham chiếu Projectile_Arrow.prefab. Enemy_Grenadier.asset đã tham chiếu Projectile_Grenade.prefab và Vfx_Explosion.asset.
- Các reference fileID bằng 0 trong prefab có thể được gán bằng scene override hoặc Initialize tại runtime. Phải kiểm tra cả hai trước khi kết luận thiếu wiring.
- Các hình PNG và file âm thanh đã được kiểm kê và đọc metadata import; chưa xem từng hình hoặc nghe từng âm thanh. Chưa chạy Play Mode, build hoặc test trong lần đọc này.
