---
title: "SE220 — Sprint 1: Playable Combat and Traversal Prototype"
sprint_number: 1
start_date: "2026-10-13"
end_date: "2026-10-19"
goal: "Một prototype có giao tranh, movement mở rộng, HUD, thắng/thua và retry."
status: "Planning"
---

# Sprint 1 — Playable Combat and Traversal Prototype

**Thời gian:** 13–19/10/2026, 7 ngày. **Nhóm:** 4 thành viên, tự pick task trên Jira.

## Sprint Goal

Bàn giao prototype Windows có một màn nhỏ, một player dùng kiếm, Zombie và Archer, thanh máu, thắng/thua, retry và movement mới: wall run, wall jump, speed boost sau chuỗi hạ quái, lăn thoát đỉnh dây/xích.

## Phạm vi và nền tảng

Player movement hiện có được coi là hoàn thiện theo xác nhận của người dùng. Dùng lại chạy, nhảy, nhảy đôi, lăn, drop-through, stomp, trèo mép và leo dây. Các task movement trong sprint này chỉ bổ sung khả năng còn thiếu được người dùng yêu cầu.

Dùng code/prefab player, combat, enemy, camera và VFX đang có; map dùng placeholder. Wall run/wall jump mở sẵn trong prototype. Câu hỏi về hệ thống rune là tìm hiểu, không bổ sung task thu thập rune/lưu unlock vào sprint.

## Cơ sở đặt tên task

Đã xem trực tiếp danh sách PR công khai của các project game:

- [SuperTux](https://github.com/SuperTux/supertux/pulls): có các tên như “Add speedrun ghost mode” và “Add per-player joystick configuration system”.
- [OpenRA](https://github.com/OpenRA/OpenRA/pulls): có tên “Add multi-select and batch delete to replay browser”.

Từ các ví dụ này, quy ước đề xuất cho nhóm là Summary bằng tiếng Anh, viết hành động + tính năng/đối tượng + ngữ cảnh cần thiết. Add cho tính năng bổ sung; Implement cho logic mới; Integrate cho code/prefab đã có; Configure cho cấu hình; Build/Create/Record cho đầu ra tương ứng. Đây là cách áp dụng cho sprint của nhóm, không phải khẳng định mọi studio dùng một chuẩn tên duy nhất. Mô tả và Acceptance Criteria bên dưới bằng tiếng Việt.

Tên task dùng thuật ngữ và đối tượng đã đọc trong project: Player.prefab, Enemy_Zombie.prefab, Enemy_Archer.prefab, Projectile_Arrow.prefab, PlayerContext, PlayerStateMachine, PlayerClimbState, Health, PlayerFeedback và VfxSpawner.

## Backlog để tạo issue Jira

Issue type: Task. Status ban đầu: To Do. Assignee: Unassigned. Label: sprint-01. Estimate: chưa chốt; người pick ước lượng sau khi đọc task và code liên quan. Priority và dependency là đề xuất lập kế hoạch. Mã W1-xx chỉ là mã tham chiếu trong tài liệu, chưa phải issue key Jira.

| Mã | Summary | Priority | Cần xong trước |
|---|---|---|---|
| W1-01 | Build the prototype combat level | High | — |
| W1-02 | Integrate player sword attacks with enemy damage | High | W1-01 |
| W1-03 | Integrate Zombie AI into the prototype level | High | W1-01 |
| W1-04 | Integrate Archer AI and arrow projectiles | High | W1-01 |
| W1-05 | Configure enemy encounters and camera framing | Medium | W1-01, W1-03, W1-04 |
| W1-06 | Add the player health HUD | High | — |
| W1-07 | Implement level victory and defeat conditions | High | W1-01, W1-03, W1-04 |
| W1-08 | Implement level restart and state reset | High | W1-07 |
| W1-09 | Add victory and defeat screens with a Retry button | High | W1-07 |
| W1-10 | Integrate combat hit feedback | Medium | W1-02, W1-03 |
| W1-11 | Create the Windows prototype build | High | W1-02, W1-03, W1-04, W1-05, W1-06, W1-07, W1-08, W1-09, W1-13, W1-14, W1-15, W1-16 |
| W1-12 | Record the prototype walkthrough and setup instructions | Medium | W1-11 |
| W1-13 | Implement wall running | High | — |
| W1-14 | Implement wall jumping | High | W1-13 |
| W1-15 | Add a movement speed boost for consecutive kills | Medium | — |
| W1-16 | Allow rolling off the top of climbable chains | Medium | — |

## Description và Acceptance Criteria

### W1-01 — Build the prototype combat level

**Description:** Dùng tile/placeholder tạo một scene gồm điểm spawn, khu giao tranh, một bức tường và một dây/xích để trình diễn movement mới.

**Acceptance Criteria:** Scene lưu trong Assets/_Project/Scenes; có ground, spawn, ranh giới màn và khu trình diễn wall run/wall jump/chain exit; đường chính sử dụng movement đang có.

**Phụ thuộc:** Không. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-02 — Integrate player sword attacks with enemy damage

**Description:** Đưa Player.prefab vào scene; nối WeaponData, Health, hurtbox và target mask của combat hiện có.

**Acceptance Criteria:** Kiếm gây damage cho enemy và player nhận damage từ enemy qua hệ thống hiện có; không có missing reference. Tận dụng movement đã hoàn thiện.

**Phụ thuộc:** W1-01. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-03 — Integrate Zombie AI into the prototype level

**Description:** Đưa Enemy_Zombie.prefab vào scene và nối player target, ground/attack mask, data và animation hiện có.

**Acceptance Criteria:** Zombie phát hiện player, tiếp cận, tấn công, nhận damage và chết; không tấn công sau khi chết.

**Phụ thuộc:** W1-01. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-04 — Integrate Archer AI and arrow projectiles

**Description:** Nối Enemy_Archer.prefab với player và ProjectileSpawner; cấu hình arrow collision, damage và vòng đời.

**Acceptance Criteria:** Archer bắn đúng hướng; arrow gây damage đúng và được dọn khi va chạm/hết vòng đời; Archer nhận damage và chết.

**Phụ thuộc:** W1-01. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-05 — Configure enemy encounters and camera framing

**Description:** Bố trí số lượng và vị trí quái; cấu hình camera hiện có cho giao tranh và khu trình diễn movement mới.

**Acceptance Criteria:** Spawn an toàn; đường đi và giao tranh nhìn thấy rõ; camera theo được khu vực tường/dây; không cần tạo camera system mới.

**Phụ thuộc:** W1-01, W1-03, W1-04. **Priority:** Medium. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-06 — Add the player health HUD

**Description:** Tạo HUD UGUI cho current/max health; nối với Health.Changed và khởi tạo đúng khi vào màn.

**Acceptance Criteria:** Thanh máu phản ánh máu ban đầu, damage và reset; UI đọc được ở độ phân giải bản demo.

**Phụ thuộc:** Không. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-07 — Implement level victory and defeat conditions

**Description:** Quản lý lượt chơi: thắng khi hết quái của lượt, thua khi player chết; nối các sự kiện hiện có.

**Acceptance Criteria:** Một lượt chỉ kết thúc một lần; dừng input chiến đấu sau kết thúc; cơ chế auto-respawn tạm trong PlayerController được phối hợp với luồng thua/retry.

**Phụ thuộc:** W1-01, W1-03, W1-04. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-08 — Implement level restart and state reset

**Description:** Dùng reload scene hoặc reset đối tượng để tạo lượt mới, khôi phục player, enemy, projectile, máu và trạng thái lượt.

**Acceptance Criteria:** Retry từ cả thắng/thua khôi phục toàn màn; ba lần retry liên tiếp không sót trạng thái cũ. Khi task 15 được ghép, reset cả kill counter, buff timer và hệ số tốc độ.

**Phụ thuộc:** W1-07. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-09 — Add victory and defeat screens with a Retry button

**Description:** Tạo panel kết quả và nút gọi luồng restart; ghép nút với chức năng của W1-08 khi sẵn sàng.

**Acceptance Criteria:** Panel phản ánh đúng kết quả; Retry bắt đầu lượt mới và đóng panel kết thúc.

**Phụ thuộc:** W1-07. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-10 — Integrate combat hit feedback

**Description:** Nối PlayerFeedback, HitStop và VFX đã có cho đánh trúng và nhận damage trong scene.

**Acceptance Criteria:** Có phản hồi khi đánh trúng/nhận damage; hiệu ứng không lặp vô hạn và được dọn khi restart.

**Phụ thuộc:** W1-02, W1-03. **Priority:** Medium. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-11 — Create the Windows prototype build

**Description:** Ghép gameplay, UI và movement mới vào scene chung; xử lý reference/layer/build và xuất Windows build. Ghép W1-10 khi sẵn sàng.

**Acceptance Criteria:** Build chạy ngoài Editor; có giao tranh, HUD, thắng/thua, retry và trình diễn các movement mới; không có lỗi chặn vòng chơi; ghi lỗi còn biết.

**Phụ thuộc:** W1-02, W1-03, W1-04, W1-05, W1-06, W1-07, W1-08, W1-09, W1-13, W1-14, W1-15, W1-16. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-12 — Record the prototype walkthrough and setup instructions

**Description:** Quay video 1–2 phút thể hiện giao tranh, movement mới, kết quả lượt chơi và retry; ghi cách chạy và điều khiển.

**Acceptance Criteria:** Có video, hướng dẫn chạy/điều khiển, danh sách tính năng và lỗi còn biết.

**Phụ thuộc:** W1-11. **Priority:** Medium. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-13 — Implement wall running

**Description:** Bổ sung phát hiện tiếp xúc tường và state chạy/leo dọc tường vào player FSM. Tận dụng body, collider, input và animation phù hợp hiện có. Khả năng này mở sẵn trong prototype.

**Acceptance Criteria:** Hoạt động với tường ở cả hai hướng; thông số tốc độ và giới hạn thời gian/độ cao có thể chỉnh; mất tiếp xúc hoặc hết giới hạn trả về state phù hợp; không nhầm trèo mép hoặc leo dây với wall run.

**Phụ thuộc:** Không. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-14 — Implement wall jumping

**Description:** Cho nhảy bật khỏi tường từ wall run; bổ sung lực đẩy ngang/dọc, hướng bật và chuyển về state trên không. Khả năng mở sẵn trong prototype.

**Acceptance Criteria:** Bật khỏi tường trái/phải với hướng đúng; có thể tiếp tục điều khiển và tiếp đất; quy tắc refill/spend air jump được ghi rõ và không cho vô hạn nhảy do lỗi chuyển state.

**Phụ thuộc:** W1-13. **Priority:** High. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-15 — Add a movement speed boost for consecutive kills

**Description:** Đếm các enemy bị player hạ trong cửa sổ thời gian; cấp speed buff tạm. Dùng sự kiện enemy/combat hiện có để đếm. Cho cấu hình ngưỡng kill, cửa sổ đếm, thời lượng và các hệ số tốc độ chạy/lăn/leo.

**Acceptance Criteria:** Đủ ngưỡng thì buff kích hoạt; hết thời gian trở lại tốc độ gốc; mỗi kill chỉ được đếm một lần; buff không nhân chồng ngoài quy tắc đã ghi; chết/retry xóa counter và buff. Ghép với enemy thật sau W1-03/W1-04.

**Phụ thuộc:** Không. **Priority:** Medium. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

### W1-16 — Allow rolling off the top of climbable chains

**Description:** Bổ sung nhánh Roll khi player ở vùng đỉnh dây/xích, nối PlayerClimbState với state lăn hiện có. Dùng được cho climbable hiện tại.

**Acceptance Criteria:** Roll tại đỉnh dây thoát sang đúng hướng; rời ClimbTarget, khôi phục gravity và áp dụng cooldown/giới hạn air roll hiện có; không còn trạng thái leo sau khi rời dây.

**Phụ thuộc:** Không. **Priority:** Medium. **Assignee:** Unassigned. **Estimate:** Chưa chốt.

## Pick task và tích hợp

- Đầu sprint có thể pick W1-01, W1-06, W1-13, W1-15 và W1-16 theo khả năng và tệp liên quan. Task map bàn giao scene thô sớm để các task integration bắt đầu.
- W1-14 bắt đầu sau W1-13. Các task combat/enemy nối vào scene khi W1-01 xong. Task speed boost có thể làm phần counter/timer trước, ghép với enemy thật khi enemy integration sẵn sàng.
- Các task sửa PlayerContext/PlayerStateMachine/PlayerController/PlayerClimbState cần báo tệp sẽ sửa và phối hợp để tránh ghi đè thay đổi; ai xong có thể pick task tiếp theo, không phân vai trò cố định.
- Người pick W1-01 phối hợp thay đổi scene tích hợp; các task khác ưu tiên prefab hoặc scene làm việc riêng.
- Mốc giữa sprint: có giao tranh và thắng/thua/retry. Cuối sprint ghép movement mới, W1-11 xuất build, W1-12 bàn giao demo.
- Mỗi người giữ một task In Progress; người khác xem đầu ra theo Acceptance Criteria trước khi chuyển Done. Bug do thay đổi mới gây ra tạo issue riêng với bước tái hiện và ưu tiên.

## Definition of Done

- [ ] Build Windows mở được ngoài Unity Editor.
- [ ] Có một màn với player dùng kiếm và hai loại enemy gây/nhận damage.
- [ ] HUD phản ánh máu player.
- [ ] Có thắng/thua và retry toàn màn.
- [ ] Có wall run và wall jump tại khu vực tường.
- [ ] Chuỗi hạ quái kích hoạt speed boost, buff hết hạn/reset đúng.
- [ ] Roll tại đỉnh dây thoát sang state lăn đúng.
- [ ] Có video, hướng dẫn chạy và lỗi còn biết.
- [ ] Các thay đổi đã ghép vào project chung.

## Backlog sau sprint

Thu thập rune và lưu mở khóa vĩnh viễn, hold-to-jump/hold-to-roll, boss, thêm vũ khí, progression/reward và procedural generation để cân nhắc sau prototype.
