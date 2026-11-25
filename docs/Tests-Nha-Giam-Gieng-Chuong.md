# Đặc tả kiểm thử Nhà Giam Giếng Chuông v7

**Bộ test dự kiến cho phiên dựng map.** Worker tra tên file và mã ca ở bảng dưới. Tester mở runbook khi nhận phiếu. **Q-M1** cần 2 ca trên mẫu C–D. **Q1** cần 30 ca trên cùng bản scene cuối và nghiệm thu thủ công. Chưa chạy các ca Unity trong tài liệu này. [Runbook cho tester](#runbook) · [Nghiệm thu thủ công](Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md#nghiem-thu) · [Thiết kế v7](NhaGiamGiengChuong.md)

## 1. Quy ước để tester nhận đúng bộ test

Gốc tính đường dẫn là `Assets/_Project/Features/NhaGiamGiengChuong/`. Mỗi mã là **một** method tên `U01_...`, `A01_...` hoặc `P01_...` theo mã tương ứng. Dùng `[Test]` ở EditMode, `[UnityTest]` ở PlayMode. Chạy các biến thể trong chính method đó, tạo lại trạng thái cho mỗi biến thể độc lập. Ghi nhãn biến thể, giá trị mong đợi và giá trị đo vào kết quả NUnit. Chỉ ghi biến thể đạt sau khi mọi assertion của biến thể đã chạy. Đặt lại listener, scene tạm, thiết bị input, input đang giữ và time scale khi kết thúc từng ca, kể cả ca lỗi.

| Task viết test | Thư mục | Namespace (tiền tố chung là `DeadCells.NhaGiamGiengChuong.Tests`) | File và mã |
| --- | --- | --- | --- |
| W1 | Tests/EditMode | `.Unit` | EncounterZoneTests.cs · U01–U03 |
| W2a | Tests/EditMode | `.Unit` | HealingChestTests.cs · U04–U07 |
| W2b | Tests/EditMode | `.Unit` | EncounterGateTests.cs · U08–U10 |
| W3a | Tests/EditMode | `.Unit` | LevelRunTests.cs · U11–U15 |
| W3b | Tests/EditMode | `.Unit` | ExitTriggerTests.cs · U16 |
| M1a / M2 / M3 / M4 / M5 | Tests/EditMode | `.Assets` | CDGeometryTests.cs · A01 / ABGeometryTests.cs · A02 / GGeometryTests.cs · A03 / HJKGeometryTests.cs · A04 / ELFGeometryTests.cs · A05 |
| M6 / I1 (task ráp wiring) | Tests/EditMode | `.Assets` | MapGeometryTests.cs · A06 / MapWiringTests.cs · A07 |
| M1b | Tests/PlayMode | `.Play` | PilotPlayTests.cs · P01 |
| T1 | Tests/PlayMode | `.Play` | EncounterPlayTests.cs · P02, P04 |
| T2 | Tests/PlayMode | `.Play` | ChestPlayTests.cs · P03 |
| T3 | Tests/PlayMode | `.Play` | RunPlayTests.cs · P05–P07 |

**Fixture cho U.** Dùng component hoặc hàm sản xuất của màn do task tương ứng sở hữu. Dựng actor, `Health`, collider, cặp địch, callback và phase vừa đủ để đi qua đúng điểm vào của component. Dùng observer để đếm wake, lần hồi, lần mở, mục kết quả và yêu cầu nạp scene. Có thể điều khiển số frame ở đầu vào của bộ phân xử cấp màn. Fixture chỉ cấp dữ liệu, phát sự kiện và đo đầu ra. Ví dụ U13 gửi hai sự kiện vào bộ phân xử thật và đọc kết quả đã phát. Tự tính người thắng bằng một bản sao của thuật toán rồi assert kết quả đó sẽ không kiểm được code của màn.

**Fixture cho A/P.** A01–A05 nạp đúng prefab đã lưu trong `Rooms/` vào scene EditMode tạm, đặt transform ráp theo plan. A06/A07 mở lại scene đích từ đường dẫn asset. P01–P07 nạp chính scene `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` và dùng instance thật trong scene. Áp dụng giao điểm, bounds và cao độ trong world space. Với hình học serialized, có thể so số thực trong 0,001 u để xử lý sai số biểu diễn. Với Player đang đứng, ghi chân đo được ở đáy collider chính, mặt đỡ trúng và contact offset cấu hình; so với cao độ thiết kế có tính contact offset. Các vùng điều kiện U01 có biên đóng chính xác. Khi nạp scene PlayMode từ asset path trong Editor, đặt phần gọi UnityEditor trong nhánh `#if UNITY_EDITOR`; giữ test PlayMode có thể biên dịch và được Test Runner phát hiện. Ghi scene đang mở, thiết bị/action map và time scale trước fixture. Trong teardown/finally, nhả nút mô phỏng, tháo thiết bị và listener do fixture tạo, trả lại action map/time scale và scene cũ (kể cả Sandbox nếu đó là scene ban đầu), bỏ thay đổi tạm của fixture. Mỗi lần chờ vật lý, intro hay AI đều có giới hạn theo thời gian thực và thông báo bước bị kẹt.

## 2. Unit EditMode · 16 ca

| Mã | Chuẩn bị | Tác động | Kết quả cần assert |
| --- | --- | --- | --- |
| U01 | Một vùng chưa thức cho mỗi điểm thử, đúng Player sống, phase đang chơi. B1 = [20,23] × [0,6], G1 = [58,76] × [0,7.75], J1 = [2,5] × [16,22.5], E1 = [−15,−12] × [24,31]. | Cho chân vào tâm và bốn góc (gồm maxX,maxY), rồi thử điểm ngoài từng cạnh 0,01 u ở giữa cạnh. Riêng G1 thử (68;7,75) và (68;8). | Mỗi điểm trong hoặc trên biên đánh thức đúng nhóm của vùng một lần, gồm đủ hai địch ở E. Điểm ngoài cho 0 lần. Hai điểm của G1 lần lượt cho 1 và 0. |
| U02 | B1 đang chờ. Chuẩn bị Player được gán với collider chính có offset, actor khác, kiếm/đạn/ghost và các phase intro, đang chơi, đã chốt. | Thử actor khác; đầu hoặc vật phụ chồng vùng trong khi chân ở ngoài; Player chết trong vùng; Player sống trong intro hoặc sau kết quả. Đối chứng bằng đúng Player sống có chân trên sàn trong vùng, phase đang chơi. | Các ca âm giữ nguyên số lần thức bằng 0. Đối chứng thức đúng Zombie B. Cùng một actor có nhiều collider vẫn xét tâm X và đáy collider chính. |
| U03 | E1 có hai instance riêng; observer đếm lần thức cho từng instance. | Vào, ở lại nhiều lần, rời về K, hạ một địch đã thức, quay vào. Khởi tạo lượt mới rồi vào E1. | Lượt đầu mỗi địch chỉ thức một lần, địch đã hạ vẫn chết, địch còn lại giữ trạng thái. Lượt mới bắt đầu với cả hai chờ và mỗi địch có đúng một lần thức mới. |
| U04 | I3 còn thưởng. Đúng Player sống trong vùng, phase chơi. Khởi tạo Health thật với Max rồi gây một đòn hợp lệ để Current < Max. Đăng ký đếm lần hồi sau setup. | Gửi một nhấn, ghi HP và UI; gửi lại callback/nhấn lặp; gây mất máu lần hai rồi thử lại. | Lần đầu Current = Max, một lần hồi, hết thưởng, lời nhắc và điểm sáng tắt. Sau lần mất máu thứ hai HP giữ ở mức thiếu và số lần hồi vẫn bằng một. |
| U05 | Player sống, đang chơi, trong I3, HP = Max, rương mới. | Nhấn Interact khi đầy, sau đó gây mất máu và nhấn lại. | Khi đầy hiện đúng “Đầy máu”, phần thưởng và điểm sáng còn, không phát hồi. Khi thiếu, lời nhắc dùng rương xuất hiện và lần nhấn tiếp theo hồi đầy đúng một lần. |
| U06 | Mỗi biến thể dùng rương mới. Tạo yêu cầu hợp lệ của Player thiếu máu, giữ callback cấp thưởng đến sau khi đổi điều kiện. | Trước khi cấp, lần lượt đổi thành actor khác, ra ngoài trigger, HP = 0, lượt đã khóa, hoặc HP vừa đầy từ nguồn khác. Thêm ca nhấn từ ngoài ngay ban đầu. Sau ca rời vùng cho Player trở lại rồi nhấn mới. | Mỗi ca âm giữ nguyên HP tại thời điểm cấp và giữ lượt thưởng; người chết có HP = 0. Actor khác cũng giữ nguyên HP. Ở ca quay lại, yêu cầu mới hợp lệ hồi đầy đúng một lần. |
| U07 | Player thiếu máu trong I3. Đăng ký observer vào Health.Changed sau setup. Khi quan sát lần hồi về Max đầu tiên, đặt cờ chống đệ quy của fixture, gây một đòn có hiệu lực rồi lập tức yêu cầu rương lần nữa ngay trong callback. | Bấm một lần để bắt đầu chuỗi trên. Đếm riêng Changed đưa HP tới Max và xác nhận lần tái nhập thực sự đã xảy ra khi Player thiếu HP. | Chỉ một lần đưa HP về Max; đòn trong callback làm HP còn thiếu. Rương kết thúc ở trạng thái đã dùng, dấu tắt; callback lồng không cấp thưởng, không kẹt. |
| U08 | I4 đóng, gắn đúng hai reference Zombie E và Archer E, theo dõi thay đổi blocker và song sắt. Hai lượt fixture độc lập. | Lượt một báo chết Zombie E rồi Archer E, lượt hai làm ngược. Đo trước, sau từng mạng và sau lần xử lý thứ hai. | 0/2 và 1/2 vẫn chặn/hiện hình. 2/2 mở đúng một lần, cùng bước bỏ blocker và ẩn hình. |
| U09 | I4 đóng, có cả Zombie B cùng loại với Zombie E, Rat G, Shield J và hai địch E. | Phát tín hiệu chết ngoài E, báo Zombie E hai lần, lặp các tín hiệu ngoài E, rồi báo Archer E hai lần. | Ngoài E không đổi số mạng. Cùng một instance chỉ tính một lần; sau Zombie E số đếm là 1 và cửa đóng, sau Archer E cửa mở đúng một lần. |
| U10 | Ở E đã có một mạng. Chuẩn bị thêm hai lượt để thử khóa khi cổng còn đóng hoặc đã mở. | Rời E về K rồi quay lại; hạ địch E còn lại và đi tầng dưới rồi quay về. Ở các lượt kiểm khóa, phát tín hiệu chết hoặc tương tác sau chốt. | Tiến độ 1/2 được giữ khi rút lui; 2/2 mở và giữ mở qua quay lại. Sau chốt cả hình, blocker và số lần mở giữ nguyên ở trạng thái lúc khóa. |
| U11 | Bộ điều phối vừa tạo, Player chưa cung cấp state hiện hành. Observer đếm thời điểm trao quyền và mốc bắt đầu đồng hồ. | Cho điều phối lần lượt thấy state chưa sẵn sàng (null), Spawn trong nhiều lần cập nhật, rồi state chơi sau khi intro trao quyền. Lặp cập nhật cùng state chơi. | Trong null/Spawn, phase còn intro và đồng hồ chưa chạy. Sau khi trao quyền thật, chuyển đang chơi đúng một lần và ghi một mốc thời gian. |
| U12 | Lượt đang chơi, chưa có kết quả. Hai fixture độc lập. | Ở frame N chỉ gửi ứng viên thắng hợp lệ từ I5 hoặc chỉ gửi sự kiện chết của Player. Quyết toán hết N và lặp lại cập nhật. | Observer nhận 0 sự kiện Finished trước quyết toán, đúng một Finished(Thắng) hoặc Finished(Thua) sau quyết toán của code lượt. |
| U13 | Hai lượt đang chơi. Đặt chung số frame N cho cả hai nguồn điều kiện khi kiểm bộ phân xử. | Lượt một gửi thắng rồi chết; lượt hai gửi chết rồi thắng. Quyết toán sau cả hai tín hiệu, theo dõi toàn bộ sự kiện Finished do code lượt phát. | Mỗi thứ tự chỉ phát một Finished(Thua), không phát Finished(Thắng). |
| U14 | Hai lượt mới. Chọn frame N và N+1. | A gửi thắng ở N rồi chết ở N+1; B gửi chết ở N rồi thắng ở N+1. Đưa tín hiệu N+1 đến ngay trước lần cập nhật quyết toán N nếu implementation trì hoãn một frame. | A chỉ Thắng, B chỉ Thua; kết quả được quyết định theo sự kiện của N. Tín hiệu tới muộn và cập nhật lặp không ghi đè N hoặc phát Finished lần hai. |
| U15 | Hai fixture độc lập dùng bộ điều phối sản xuất. Quan sát phase, quyết định cho phép gameplay và quyền yêu cầu tạo lượt mới. | Thử yêu cầu Chơi lại sớm. Chốt từng kết quả, cập nhật lại, kiểm quyền chơi và gọi hai lần quyết định cấp lượt mới mà TryReplay sử dụng. | Trước chốt bị từ chối. Sau chốt phase kết thúc khóa gameplay; lần đầu nhận quyền replay, lần hai bị từ chối khi đang chuyển. Unit dừng ở quyết định, không nạp scene. P05–P07 kiểm khóa component và UI thật. |
| U16 | I5 gắn đúng Player, gate và run. Mỗi tổ hợp bắt đầu với bộ đếm ứng viên thắng bằng 0. | Thử actor khác, collider vật phụ của đạn/ghost, Player chết, I4 đóng, phase intro, phase kết quả; cuối cùng cho đúng Player sống bước vào khi I4 mở và đang chơi. | Các ca âm cho 0 ứng viên. Ca hợp lệ tạo ứng viên thắng cho đúng frame, panel chỉ xuất hiện qua bộ phân xử của U12–U14. |

`Health.RestoreFull()` gán Current = Max rồi phát Changed ngay trong cùng lời gọi. Đây là lý do U07 cần tái nhập ngay trong callback. Intro Player chuyển từ Spawn sang Idle khi kết thúc hồi sinh. [Health.cs](../Assets/_Project/Features/Combat/Scripts/Health.cs#L61) · [PlayerSpawnState.cs](../Assets/_Project/Features/Player/Scripts/StateMachine/States/PlayerSpawnState.cs#L32)

## 3. Cấu hình EditMode · 7 ca

Dùng hình thực của collider đang tham gia va chạm. Với bậc H kiểm đường biên/polyline từng bậc; với cửa kiểm khoảng mở xuyên chiều dày vách và phần tường sát hai đầu cửa, tránh đếm riêng số box. Với mỗi bệ một chiều, kiểm độ dày 0,25 u, layer OneWayPlatform, collider bật Used By Effector, PlatformEffector2D cùng GameObject và Use One Way bật. Với mỗi xích, kiểm trigger rộng 0,5 u ở layer Climbable. Với mặt đặc, kiểm layer Ground. Mỗi phép đo in tên prefab/object, điểm đo và giá trị thực khi lệch.

| Mã | Nạp | Thao tác đo | Kết quả cần assert |
| --- | --- | --- | --- |
| A01 | Rooms/CD.prefab đã lưu | Đo sàn, trần C và D, hai mặt ván, I1, mối nối và cửa do CD sở hữu; quét các collider ở chuông, đèn, giá đỡ, cửa sổ. | C [42,56]@0, trần 18,5; D [22,42]@8, trần 13,5. Đỉnh C [42,56]@8 và nghỉ [44,49]@4,5 một chiều. I1 tâm (46;4), cao 9. Ở x=42 và 56 đều hở y=0–4 và 8–12; ở x=22 hở y=8–12. Một nguồn va chạm mỗi mặt, nét trang trí không chặn Player. |
| A02 | Rooms/AB.prefab | Đo sàn/trần, bục tập, A/B và phần tiếp giáp B/C của prefab. | Sàn [0,42]@0, trần A/B 5,5 và 6. Bục [9,14]@2 một chiều, mặt dưới 1,75 u, đủ cao cho thân đứng 1,6 u. A/B x=18 và đầu B x=42 thông y=0–4. |
| A03 | Rooms/G.prefab | Đo sàn dưới và ba đoạn sàn trên theo cả hai trục, hai đầu nối sang C, trần, I2. | Sàn [56,76]@0, trần 14,5; ở y=8 đặc [56,65], một chiều [65,72], đặc [72,76]; mép liền. I2 x=68 y=−0,5…8,5. Phía G của x=56 thông y=0–4 và 8–12. |
| A04 | Rooms/HJK.prefab | Lấy đường biên H, đỉnh K, sàn J/K, I6, trần và các cửa hai cao độ. | H đáy y=7, từ phải đi trái có [18,22]@8, [15,18]@10, [12,15]@12, [9,12]@14, [6,9]@16. Mỗi bậc cao 2, mặt đáp ≥3 u. Sàn K/J [−30,6]@16, đỉnh K [−30,−16]@24 một chiều; trần H/J=22,5, K=30,5. I6 x=−24 y=15,5…24,5. H/J x=6 và J/K x=−16 thông y=16–20; K/E x=−16 thông y=24–28. |
| A05 | Rooms/ELF.prefab | Đo mặt đặc E/L, bục E, bốn bệ L, các dải chồng và cặp sàn F/trần E. Kiểm mặt cắt ở hai lỗ x=22. | Sàn [−16,38]@24, bục E [−4,2]@26 một chiều. L lần lượt [32,38]@26, [28,34]@28, [24,30]@30, [22,26]@32 một chiều. Hành lang nhảy ở 32–34, 28–30, 24–26 thông qua bệ kế. F có một sàn [0,22]@32, đáy y=31 làm trần E; trần E riêng [−16,0]@31; trần L/F=38,5/38. Cửa E/L y=24–28 cho phép đặt riêng I4, L/F y=32–36 thông. |
| A06 | Scene đích đã ráp từ 5 prefab | Đo bao vỏ và hợp các mặt đỡ tại từng tầng. Đo 11 lỗ qua vách tĩnh. Ở bản đã wiring, loại đúng blocker được gán cho I4 khỏi phép đo vách, rồi đo riêng blocker ở x=22 y=24–28. Quét mối H–D, H–J, E–F tìm collider lặp và vách x=22 xuyên nhầm tầng. | Bao vỏ từ (−30,5;−1) tới (76,5;39). Sàn chính [0,76]@0, [18,76]@8, [−30,9]@16, [−30,38]@24, [0,26]@32. Đủ 11 khu và 11 lỗ, đúng cả X và khoảng Y từng hàng. Chỉ I4 được chắn E/L sau wiring. Không có khe hoặc collider chồng ở các mối nêu trên. |
| A07 | Scene đích sau I1 đã lưu, mở lại | Dò missing script và reference thật; kiểm prefab/data và scene override của Player, camera, hệ thống, 5 địch; đọc các quan hệ vùng→địch, gate→cặp E, I3/I5→Player/run, kết quả→Chơi lại; đo placement. | Một Player chân (3;0) quay phải, có intro/feedback với camera, VFX và HitStop. Camera target đúng, size 7, Rect(−32,−4,110,47). Một ProjectileSpawner nối VFX, dùng đúng Projectile_Arrow, targets=128, ground=576. Năm địch có ref Player/projectiles/VFX, đúng spawn B Zombie (29;0), G Rat (62;0), J Shield (−4;16), E Zombie (−9;24) + Archer (10;24). Bốn vùng có biên U01 và đúng thành viên. I3 tâm (72;0,75), size (2,5;1,5), có action Player/Interact và feedback; I4 nối đúng hai instance E, hình và blocker kín y=24–28 ở x=22; I5 tâm (5;33), size (2;2), nằm trong vòm; reference kết quả và nút Chơi lại resolve. |

Nguồn đối chiếu cho các khoảng mở, mặt đặc, bệ và xích là [bảng tọa độ v7](NhaGiamGiengChuong.md#tọa-độ-bản-thô). Nguồn cho prefab, override runtime và mask là [wiring cấp màn trong plan](Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md#4-wiring-cấp-màn).

## 4. Tích hợp PlayMode · 7 ca

Mỗi biến thể mở lượt sạch khi yêu cầu trạng thái đầu khác nhau. Cho phép định vị actor **chỉ ở bước chuẩn bị** để cô lập ca; ghi lại vị trí ban đầu. Khi kiểm tuyến, điều khiển Player bằng input và đợi bước vật lý thật. Khi kiểm combat, xác nhận đòn/đạn đã đi vào mục tiêu bằng quan sát va chạm hoặc đường đánh hợp lệ. Ghi HP trước/sau, nguồn, frame và danh tính instance. Ảnh 16:9 và kiểm đọc tín hiệu đi đường vẫn thực hiện theo §5 trong plan.

| Mã | Chuẩn bị | Hành động | Kết quả cần assert |
| --- | --- | --- | --- |
| P01 | Fixture đọc biến môi trường NGGC_QA_GATE do runbook truyền để chọn Q-M1 hoặc Q1; thiếu/sai gate thì báo thiếu fixture setup. Mở scene đã lưu của mốc đó. Ở Q-M1 kiểm Player của scene mẫu mở lại xuất hiện đúng tại (46;0) trước khi phát input; ghi chân sau intro, fixture không dời Player. Ở Q1, xác nhận spawn chính tại (3;0) rồi sau intro định vị một lần tại (46;0) cho riêng phép thử I1. | Giữ Lên, xuyên bệ nghỉ và sàn đỉnh; nhả nút, đi trái qua x=42 trên sàn D. Quay phải đến I1, Xuống + Nhảy, lặp ở bệ nghỉ nếu đáp vào đó. Ghi các mốc chân, mặt đang đỡ và input. | Theo đúng thứ tự: rời sàn 0, vượt 4,5, đáp y≈8, qua D bằng input, quay lại, đáp y≈0 ở C. Sàn D giữ Player; chuông, cửa sổ, giá đỡ không chặn đường. Sau bước chuẩn bị, mọi chuyển vị đều do Player/vật lý. |
| P02 | Sau intro, renderer của từng địch hiện tại spawn đã định, 4 vùng chờ. Dùng lượt riêng cho G từ trên trước và từ dưới trước. | Trước khi mở vùng, chờ qua nhịp AI. Cho đường kiếm và tên cắt vị trí thân địch đang chờ. Gọi thêm Health.TakeDamage với đòn dương khi HP đầy để kiểm filter dù collider đang ngừng mô phỏng. Đặt Player trong tầm bị đánh nhưng chân ngoài vùng. Thử actor phụ và Player chết. Sau đó lần lượt vào B1/G1/J1/E1 bằng chân Player sống, rời và quay lại. Cho đòn có Stun = 0 trúng Shield khi đang đối mặt, rồi trúng từ sau ở ngoài cửa sổ bất tử; ghi hướng, Stun và HP trước/sau từng đòn. Cho Archer thật bắn một phát trúng và một phát né. | Trước kích hoạt cả 5 giữ vị trí, không sát thương hai chiều. Ở G y=8 Rat còn chờ, đi xuống vùng hoặc vào dưới đều thức. Mỗi vùng thức đúng nhóm một lần; E có hai địch, đi lùi giữ tiến độ. Khi thức có đòn hợp lệ đổi HP, Shield chặn trước và nhận đòn từ sau; Projectile_Arrow của Archer trúng làm HP Player giảm, phát né không giảm HP. |
| P03 | Tại I3 với Rat còn sống, một lượt cho E bàn phím, một lượt cho buttonNorth tay cầm (thiết bị Input System). Dùng action asset và listener thật, bắt đầu với HP đầy. | Nhấn và nhả một nút mỗi lần. Thử ngoài vùng; vào rương nhấn khi đầy, gây mất máu, nhấn lần nữa trong vùng; giữ nút, nhả, thử nhấn lặp sau khi mất máu thêm. | Cả hai binding kích hoạt cùng giao dịch; ngoài vùng giữ nguyên trạng thái, đầy báo “Đầy máu” và giữ thưởng; thiếu hiện nhắc, một lần nhấn hồi Max, tắt dấu và sáng. Mất máu tiếp theo giữ HP thiếu. |
| P04 | Hai lượt mới, I4 đóng; Zombie B, Shield J và Rat G độc lập với cặp E. | Thử đi qua/Interact ở 0/2, phát mạng ngoài E, kích E1 rồi hạ một địch bằng đường damage thật. Rút về K, quay lại, thử lối ở 1/2; hạ địch còn lại. Lặp hai thứ tự, quay đi rồi trở lại sau 2/2. | Cửa K thông ở mọi bước. 0/2 và 1/2 chưa thể đi sang L; hình và blocker còn. Sau mạng thứ hai ghi cùng thời điểm hình biến mất và blocker nhả, Player qua được, cửa giữ mở. |
| P05 | Hai lượt tại I5 đã mở I4 trong scene thật, Player sống/thiếu đủ HP để nhận một đòn chí tử. Observer gắn trước khi phát tín hiệu, đếm cả các lần hiện panel. | Trong một lượt gọi xử lý tiếp xúc thật của I5 với collider Player rồi gây sát thương chí tử qua Health; lượt kia gây sát thương trước rồi đưa cùng collider vào đúng xử lý tiếp xúc. Ghi frame ngay ở cả hai điểm vào và callback Died. Giữa hai thao tác không yield. Đợi bộ điều phối quyết toán. | Mỗi lượt có hai thao tác cùng Time.frameCount, có Died thật; lượt chạm trước phải ghi nhận ứng viên thắng trước đòn chí tử. Lượt chết trước cho phép I5 lọc Player đã chết. Cả hai chỉ hiện Thua một lần, không có frame/panel Thắng. Nếu frame khác nhau, fixture chưa tạo được tình huống cần kiểm. |
| P06 | Sau intro tạo hai lượt, một lượt thắng, một lượt thua. Cho HitStop thật đang có hiệu lực khi kết quả chốt. Ghi HP, vị trí, trạng thái của 4 vùng, I3, I4, I5, panel. | Chờ hit-stop thực sự kết thúc và qua **hơn 1,5 s thời gian gameplay ở tốc độ bình thường** để vượt hạn tự hồi sinh; dùng đồng hồ thực để phát hiện bị treo. Nhấn di chuyển, đánh, Interact; đưa đòn/đạn có hiệu lực vào mục tiêu. | Panel và một kết quả còn nguyên. Không tự respawn, input chơi không làm Player di chuyển/đánh; HP, sát thương và cờ rương/cổng/đích giữ nguyên; UI còn nhận Chơi lại. Log có frame đầu/cuối, thời gian đã qua và thời điểm HitStop trả timeScale. |
| P07 | Thử sau thắng và sau thua bằng UI thật. Chuẩn bị trạng thái đã đổi (địch chết, rương dùng, cổng mở) và theo dõi ID các đạn cũ đang bay/cắm cùng đạn Archer từ pool. | Bấm đúp Chơi lại khi chuyển scene. Quan sát từ lúc nạp đến sau intro. Dùng lại cung, hoàn thành thêm một lượt; kiểm số callback/panel. | Mỗi lần chỉ nạp một lượt mới. Một Player chân (3;0) quay phải, đầy máu; một intro trước khi trao quyền và tính giờ. Năm địch đủ HP ở spawn A07, bốn vùng chờ, I3 sẵn, I4 đóng, I5 sẵn, panel cũ ẩn. Không có đạn/hit muộn từ lượt trước; đạn mới bắn bình thường, lượt sau chỉ một kết quả. |

<a id="runbook"></a>

## 5. Runbook cho tester · Q-M1 và Q1

Mỗi checkout chỉ mở một Editor tại một thời điểm. Các checkout độc lập có thể có Editor riêng khi máy đủ tài nguyên. Tester chạy từng suite nối tiếp trong checkout QA. Trước khi chạy batch, bàn giao scene đã lưu và đóng Editor đang giữ checkout đó.

<a id="snapshot"></a>

### 5.1. Chốt đầu vào bằng manifest SHA-256

Người bàn giao tạo manifest ở từng mốc: **B0** cho đầu vào, **M1b** cho mẫu C–D sẽ chạy Q-M1, **sau I1 và T1–T3** cho bản tích hợp chạy Q1. Ghi đường dẫn manifest và HEAD vào phiếu. Khi chuyển mốc, giữ manifest cũ và ghi manifest mới vì nội dung đã thay đổi. Tester và từng người nhận checkout ở B0 dùng **Verify** với đúng manifest do người bàn giao cấp. Mỗi gate giữ nguyên một manifest từ trước suite đầu đến sau suite cuối.

Mở PowerShell ở gốc checkout cụ thể. Nạp khối sau một lần trong cửa sổ của mỗi người. Hàm so cả revision và nội dung file theo đường dẫn tương đối, kể cả file untracked/ignored. Một thay đổi, file thiếu hoặc file thêm đều dừng kiểm. Manifest nằm trong TEMP ngoài project, kiểu tạo độc quyền tránh ghi đè.

<details>
<summary>Mở và sao chép hàm Confirm-MapSnapshot</summary>

```powershell
$project = (Resolve-Path .).Path
$ns = 'DeadCells.NhaGiamGiengChuong.Tests'
function Confirm-MapSnapshot([ValidateSet('Create','Verify')]$mode, [string]$manifest) {
    $revision = rtk proxy git -C $project rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'BLOCKED - Git HEAD' }
    @'
import hashlib, json, os, sys, tempfile
from pathlib import Path

mode, base, manifest, revision = sys.argv[1:]
base, manifest = Path(base).resolve(), Path(manifest).resolve()
temp = Path(tempfile.gettempdir()).resolve()
if temp not in manifest.parents or base in manifest.parents:
    sys.exit("BLOCKED - manifest must be in TEMP outside checkout")
files = {}
def record(path):
    key = path.relative_to(base).as_posix()
    digest = hashlib.sha256()
    with path.open("rb") as src:
        for block in iter(lambda: src.read(1 << 20), b""):
            digest.update(block)
    files[key] = digest.hexdigest()

def walk_error(error):
    raise error

for name in ("Assets", "Packages", "ProjectSettings", "SourceAssets/Tilesets/GothicDungeon"):
    folder = base / name
    if not folder.is_dir():
        sys.exit("BLOCKED - missing directory " + name)
    for root, dirs, names in os.walk(folder, onerror=walk_error):
        for directory in dirs:
            child = Path(root) / directory
            if child.is_symlink() or getattr(child, "is_junction", lambda: False)():
                sys.exit("BLOCKED - linked directory requires review: " + str(child))
        for filename in names:
            record(Path(root) / filename)
for name in ("NhaGiamGiengChuong.md", "NhaGiamGiengChuong.png",
             "Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md", "Tests-Nha-Giam-Gieng-Chuong.md"):
    record(base / "docs" / name)

if mode.lower() == "create":
    with manifest.open("x", encoding="utf-8") as out:
        json.dump({"revision": revision, "files": files}, out, ensure_ascii=False, sort_keys=True)
else:
    with manifest.open(encoding="utf-8") as src:
        saved = json.load(src)
    old = saved["files"]
    problems = []
    if saved["revision"] != revision:
        problems.append("HEAD: " + saved["revision"] + " -> " + revision)
    problems.extend("missing " + p for p in sorted(old.keys() - files.keys()))
    problems.extend("extra " + p for p in sorted(files.keys() - old.keys()))
    problems.extend("changed " + p for p in sorted(files.keys() & old.keys()) if files[p] != old[p])
    if problems:
        sys.exit("BLOCKED\n" + "\n".join(problems))
print(mode, revision, len(files), "files", manifest)
'@ | rtk proxy python -X utf8 - $mode $project $manifest $revision
    if ($LASTEXITCODE -ne 0) { throw 'BLOCKED - snapshot; xem file/ly do o tren' }
}
```

</details>

**Người bàn giao** chạy khối này ở mỗi mốc và đưa đường dẫn in ra lên phiếu. Chọn tên mốc B0, M1b hoặc Q1. Nếu phải chuyển manifest sang máy khác, sao chép bản được bàn giao vào TEMP và xác minh SHA-256 của chính file manifest theo giá trị trên phiếu.

```powershell
$stage = Read-Host 'Moc ban giao (B0 / M1b / Q1)'
$manifest = Join-Path ([IO.Path]::GetTempPath()) ("GiengChuong-$stage-" + [guid]::NewGuid() + '.json')
Confirm-MapSnapshot Create $manifest
rtk proxy python -X utf8 -c "import hashlib,sys;print(hashlib.sha256(open(sys.argv[1],'rb').read()).hexdigest())" $manifest
```

**Người nhận B0 hoặc tester ở gate** chạy khối sau với đường dẫn trên phiếu của đúng mốc. Đối chiếu hash manifest in ra với phiếu trước khi dùng. Nếu nhận B0 chỉ cần tới đây. Ở Q-M1/Q1, tiếp tục 5.2 trong cùng cửa sổ.

```powershell
$manifest = Read-Host 'Duong dan tuyet doi cua manifest tren phieu'
$expectedHash = (Read-Host 'SHA-256 cua manifest tren phieu').Trim()
$hash = rtk proxy python -X utf8 -c "import hashlib,sys;print(hashlib.sha256(open(sys.argv[1],'rb').read()).hexdigest())" $manifest
if ($LASTEXITCODE -ne 0 -or $hash -ne $expectedHash) { throw 'BLOCKED - manifest SHA-256' }
Confirm-MapSnapshot Verify $manifest
```

### 5.2. Chạy suite và đối chiếu báo cáo NUnit

Tester nạp khối dưới. Mỗi lần chạy ghi XML mới vào TEMP. Bộ đọc đối chiếu namespace, fixture, mã và số lần xuất hiện với bảng §1. Nó in mọi mã thiếu/thừa/trùng cùng các lỗi test, lỗi teardown và kết quả root/suite. Trạng thái trả về cho script là 0 = PASS XML, 1 = FAIL, 2 = BLOCKED. Chỉ ký gate sau khi kiểm nhãn các biến thể và bằng chứng thủ công.

<details>
<summary>Mở và sao chép hai hàm đọc XML và chạy gate</summary>

```powershell
$report = Join-Path ([IO.Path]::GetTempPath()) ('GiengChuong-results-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $report -ErrorAction Stop | Out-Null
function Read-MapNUnit([string]$xml, [int]$runner, [string]$filter) {
    @'
import re, sys
from collections import Counter
from xml.etree import ElementTree as ET

xml, runner, scope = sys.argv[1:]
ns = "DeadCells.NhaGiamGiengChuong.Tests."
suites = {
    "Unit.EncounterZoneTests": "U01 U02 U03",
    "Unit.HealingChestTests": "U04 U05 U06 U07",
    "Unit.EncounterGateTests": "U08 U09 U10",
    "Unit.LevelRunTests": "U11 U12 U13 U14 U15",
    "Unit.ExitTriggerTests": "U16",
    "Assets.CDGeometryTests": "A01", "Assets.ABGeometryTests": "A02",
    "Assets.GGeometryTests": "A03", "Assets.HJKGeometryTests": "A04",
    "Assets.ELFGeometryTests": "A05", "Assets.MapGeometryTests": "A06",
    "Assets.MapWiringTests": "A07", "Play.PilotPlayTests": "P01",
    "Play.EncounterPlayTests": "P02 P04", "Play.ChestPlayTests": "P03",
    "Play.RunPlayTests": "P05 P06 P07",
}
expected = {code: ns + fixture for fixture, codes in suites.items()
            if ns + fixture == scope or (ns + fixture).startswith(scope + ".")
            for code in codes.split()}
try:
    root = ET.parse(xml).getroot()
except (OSError, ET.ParseError) as error:
    print("BLOCKED", xml, error)
    sys.exit(2)
if root.tag != "test-run" or not expected:
    print("BLOCKED - invalid NUnit root/filter:", root.tag, scope)
    sys.exit(2)

cases = list(root.iter("test-case"))
found, all_ids = Counter(), Counter()
extra, incomplete, failures = [], [], []
for node in root.iter():
    if node.tag not in ("test-run", "test-suite", "test-case"):
        continue
    name = node.get("fullname") or node.get("name") or node.tag
    result = node.get("result", "")
    failure = node.find("failure")
    print(node.tag, result, name)
    if failure is not None or result.startswith("Failed") or result == "Error" or node.get("label") == "Error":
        msg = "" if failure is None else failure.findtext("message", "")
        trace = "" if failure is None else failure.findtext("stack-trace", "")
        failures.append(name + " " + result + "\n" + msg + "\n" + trace)
    elif result != "Passed":
        incomplete.append(name + " " + result)
    if node.tag == "test-case":
        match = re.fullmatch(r"(.+)\.([UAP]\d{2})_\w+", name)
        if match:
            fixture, code = match.groups()
            all_ids[code] += 1
            if expected.get(code) == fixture:
                found[code] += 1
                continue
        extra.append(name)
missing = sorted(set(expected) - set(found))
duplicates = sorted(code for code, count in all_ids.items() if count > 1)
print("missing:", missing, "extra/wrong fixture:", extra, "duplicate IDs:", duplicates)
print("NUnit root:", root.attrib, "cases:", len(cases), "CLI exit:", runner, "XML:", xml)
for detail in failures:
    print("failure:", detail)
for detail in incomplete:
    print("incomplete:", detail)
try:
    counts = {k: int(root.attrib[k]) for k in ("total", "passed", "failed", "errors", "skipped", "inconclusive")
              if k in root.attrib}
    invalid_counts = any(n < 0 for n in counts.values())
except ValueError:
    counts, invalid_counts = {}, True
if failures or counts.get("failed", 0) > 0 or counts.get("errors", 0) > 0:
    print("FAIL", scope)
    sys.exit(1)
if (runner != "0" or not cases or incomplete or missing or extra or duplicates
        or invalid_counts or counts.get("skipped", 0) or counts.get("inconclusive", 0)
        or any(counts[k] != len(cases) for k in ("total", "passed") if k in counts)):
    print("BLOCKED", scope)
    sys.exit(2)
print("PASS XML", scope)
'@ | rtk proxy python -X utf8 - $xml $runner $filter
}
function Test-MapGate([ValidateSet('Q-M1','Q1')]$gate) {
    if ($gate -eq 'Q-M1') {
        $filters = @("$ns.Assets.CDGeometryTests", "$ns.Play.PilotPlayTests")
    } else {
        $filters = @("$ns.Unit", "$ns.Assets", "$ns.Play")
    }
    $results = @()
    foreach ($filter in $filters) {
        Confirm-MapSnapshot Verify $manifest
        $mode = if ($filter.StartsWith("$ns.Play")) { 'PlayMode' } else { 'EditMode' }
        $xml = Join-Path $report ($filter + '-' + [guid]::NewGuid() + '.xml')
        $priorGate = $env:NGGC_QA_GATE
        try {
            $env:NGGC_QA_GATE = $gate
            rtk proxy unity test $project --mode $mode --filter $filter --report-format nunit --output $xml
            $runner = $LASTEXITCODE
        } finally {
            $env:NGGC_QA_GATE = $priorGate
        }
        Read-MapNUnit $xml $runner $filter
        $results += $LASTEXITCODE
    }
    Confirm-MapSnapshot Verify $manifest
    Write-Host "$gate - suite exits: $results - reports: $report"
    if ($results -contains 1) { throw "FAIL - $gate" }
    if (@($results | Where-Object { $_ -ne 0 }).Count) { throw "BLOCKED - $gate" }
    Write-Host 'PASS XML - kiem nhan bien the va bang chung thu cong truoc khi ky'
}
```

</details>

### 5.3. Chọn đúng gate trên phiếu

**Q-M1.** Sau bước Verify với manifest M1b, chạy lệnh dưới. Cần A01 và P01, mỗi mã đúng một lần. Đọc log/ảnh đi hai chiều trong plan, xác nhận save/reopen, vị trí chân ban đầu trong scene mẫu và lộ trình đáy–đỉnh–D–đáy trước khi giao M2–M5.

```powershell
Test-MapGate Q-M1
```

**Q1.** Ở checkout QA cuối, lặp 5.1 với manifest bản tích hợp **sau I1 và T1–T3**, nạp 5.2 rồi chạy lệnh dưới. Phải nhận đủ 16 U + 7 A + 7 P, bao gồm A01/P01 chạy lại. Dùng đúng manifest này cho nghiệm thu §5 trong plan và Verify lại sau nghiệm thu trước khi ký.

```powershell
Test-MapGate Q1
```

**Ghi phiếu.** Ghi gate/task, checkout, HEAD, manifest và hash, phiên bản Editor, mode/filter, mã, nhãn biến thể, XML/log/ảnh, mong đợi và thực đo. **PASS** cần toàn bộ biến thể đúng và bằng chứng thủ công của gate đầy đủ. **FAIL** khi assertion, teardown hoặc quan sát trái thiết kế; chép mã, nhãn, failure/message và stack trace về tác giả ở §1. Nếu còn thiếu ID, giữ cả danh sách mã thiếu trên phiếu lỗi. **BLOCKED** khi thiếu fixture, XML, môi trường hoặc bằng chứng, sai snapshot/filter, 0 ca, thiếu/thừa/trùng mã, thiếu biến thể, skipped/inconclusive hoặc tình huống cùng frame chưa tạo được. Tester ghi trường hợp lẫn FAIL và BLOCKED vào cùng phiếu; gate vẫn đóng.

Q1 cần bằng chứng lượt bỏ qua B/J/G, đi nhánh G theo hai chiều, hụt bậc H/L, tín hiệu Archer trước phát đầu, các góc camera 16:9 và thời gian tuyến chính/nhánh. Ghi quyết định nghiệm thu cuối tại Q1 của [kế hoạch triển khai và mục nghiệm thu scene](Plan-Trien-Khai-Nha-Giam-Gieng-Chuong.md).

