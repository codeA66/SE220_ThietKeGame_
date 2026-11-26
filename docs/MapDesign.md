# Chuẩn bị thiết kế map

Ngày đối chiếu: 14/11/2026. Đây là hướng dẫn chuẩn bị, chưa phải map hoàn thành hoặc kết quả Play Mode.

## Vai trò và brief

- **Người phụ trách: người dùng — Level Designer / Map Designer.** Bạn chọn ảnh, duyệt bố cục và quyết định khi asset không đủ để tái hiện chi tiết.
- AI hỗ trợ phân tích ảnh, lựa tile, dựng scene trong phạm vi được giao và báo cáo kiểm tra.
- Yêu cầu mới nhất: tái dựng map theo ảnh người dùng sắp gửi, dùng tileset đã có. Ảnh tham chiếu: **chưa nhận**. Kích thước map, khung camera và sai khác được chấp nhận: chốt sau khi đọc ảnh.
- Không tự chuyển thành thiết kế một màn mới, thêm encounter, nhiệm vụ, procedural generation hoặc cơ chế gameplay ngoài ảnh/yêu cầu.

## Bộ công cụ hiện có và bằng chứng

| Hạng mục | Nguồn đã đối chiếu | Ý nghĩa khi dựng map |
| --- | --- | --- |
| Unity 6000.6.9f1 | `ProjectSettings/ProjectVersion.txt` | Mở đúng phiên bản project |
| GothicDungeon | `Assets/_Project/Art/Environment/GothicDungeon/` | Bộ asset cần đối chiếu với ảnh; không mặc định mọi chi tiết ảnh đều có tile tương ứng |
| 4 palette: Terrain, Architecture, Props, Nature | Các prefab trong `Assets/_Project/Art/Environment/GothicDungeon/Palettes/` | Grid của cả bốn palette đang là `(1, 1, 0)` |
| 612 Tile asset | `Assets/_Project/Art/Environment/GothicDungeon/Tiles/` | Terrain 159, Architecture 179, Props 149, Nature 125; cả 612 file có `m_ColliderType: 0` |
| Scale sprite | `SourceAssets/Tilesets/GothicDungeon/import-manifest.json` | Bốn sheet 32 PPU; sheet TerrainAndRuins_4x là 128 PPU. Không đổi tất cả về cùng PPU |
| Sandbox | `Assets/_Project/Scenes/Sandbox_Player.unity` | Nguồn tham khảo cấu hình scene; giữ scene gốc khi dựng map riêng |
| Physics layer | `ProjectSettings/TagManager.asset:14` | Ground=6, Player=7, Enemy=8, OneWayPlatform=9, Climbable=10 |
| Camera đang có | `Assets/_Project/Features/CameraSystem/Scripts/CameraFollow2D.cs:26` | Camera có bounds; dùng lại hệ thống này khi cần scene chơi được |

Đây là kiểm tra cấu hình trên đĩa; chưa xem từng sprite, chưa kiểm tra Editor đang kết nối và chưa quan sát bộ tile render trong scene mới.

## Quy trình sau khi nhận ảnh

1. **Đọc ảnh:** xác định ảnh là toàn map hay một khung hình, biên nhìn thấy, tỷ lệ, điểm mốc và các vùng bị che. Nếu không suy ra được scale đáng tin cậy, chốt scale với người dùng trước khi dựng.
2. **Đối chiếu asset:** xem trực tiếp sheet/sprite liên quan; lập danh sách tile khớp và chi tiết thiếu. Giữ nguyên bố cục ảnh; ghi rõ mọi thay thế cần thiết.
3. **Chốt hệ tọa độ:** quy đổi ảnh sang grid; ghi kích thước map và camera dùng để so sánh. Grid palette 1 unit không có nghĩa mọi sprite chỉ chiếm một ô.
4. **Dựng scene riêng:** tên đề xuất `Assets/_Project/Scenes/MapReference01.unity` — chưa tạo. Giữ sprite native scale; chia background, địa hình, kiến trúc và trang trí để dễ chỉnh theo ảnh. Chưa cần thêm Sorting Layer vào ProjectSettings; có thể dùng Order in Layer trên layer Default đang có.
5. **Khớp hình trước:** đặt silhouette địa hình, mép platform, tường, cột/cửa và các mốc lớn; sau đó mới thêm props, cây/cỏ và foreground. Không thêm chi tiết sáng tạo ngoài brief.
6. **Collision khi map cần chơi được:** tách collider khỏi tile trang trí. Tile GothicDungeon hiện có Collider Type=None; chỉ thêm TilemapCollider2D không tự tạo hình va chạm cho các tile này. Phương án ban đầu là collider đơn giản trên scene riêng, không sửa hàng loạt Tile asset dùng chung.
7. **Tích hợp khi được giao:** dùng lại Player, enemy prefab và hệ thống scene đang có; kiểm tra cả prefab, scene override và runtime initialization trước khi kết luận reference thiếu. Bố trí quái chỉ khi ảnh/yêu cầu có quy định.
8. **So sánh:** chụp cùng vùng nhìn/tỷ lệ với ảnh, đối chiếu side-by-side hoặc overlay; ghi từng khác biệt. Báo riêng chất lượng hình ảnh và khả năng chơi được.

## Điểm kỹ thuật cần giữ khi dựng

- Nền/tường trang trí không mặc định là vật cản. Collider Ground chỉ bao phủ bề mặt thực sự đi/đứng/va chạm được.
- One-way platform trong sandbox nằm trên layer 9, có `PlatformEffector2D.m_UseOneWay: 1` và collider `m_UsedByEffector: 1` (`Sandbox_Player.unity:1807`, `:1842`, `:1879`). Dùng cấu hình tương đương khi map có platform xuyên từ dưới; không áp dụng cho tất cả nền.
- Camera sandbox đang dùng orthographic size=7 và bounds `x=-15.5, y=-1, width=59, height=20` (`Sandbox_Player.unity:1411`, `:1461`). Đây là cấu hình của sandbox, không phải kích thước mặc định cho ảnh mới.
- `PlayerMovementConfig.asset:15` có run speed=7, air speed=6.5, một air jump (`:25`) và crouch shrink đang bật (`:52`). Không suy ra khoảng nhảy an toàn từ mỗi vận tốc; không sửa movement đã được người dùng xác nhận hoàn thiện.
- Chưa xác nhận wall run/wall jump hoạt động trong nhiệm vụ này. Không dựa vào task trong SprintPlan để coi các cơ chế đó đã có hoặc chưa có.
- Build Settings hiện ghi `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` ở index `0` (`ProjectSettings/EditorBuildSettings.asset:9`, 10 room prefab, 34 địch) và `Assets/_Project/Scenes/Main_Level.unity` ở index `1` (`ProjectSettings/EditorBuildSettings.asset:12`). Chỉ cập nhật danh sách scene khi có nhiệm vụ build/tích hợp tương ứng.
- Không tự tạo/sửa/xóa `.meta`; để Unity quản lý chúng. Không chạy lại importer chỉ để bắt đầu vẽ map: `GothicDungeonImporter.Import` có ghi asset và prefab.

## Checklist bàn giao

- [ ] Ảnh tham chiếu và phạm vi cần tái dựng đã được xác định.
- [ ] Có scene riêng, kích thước/scale và camera so sánh được ghi lại.
- [ ] Địa hình, platform và các mốc lớn khớp ảnh; chi tiết bị che chưa bị tự bịa thêm.
- [ ] Các phần thay thế/không thể khớp bằng tileset hiện có được liệt kê và người dùng duyệt.
- [ ] Có ảnh so sánh cùng khung hình và danh sách khác biệt còn lại; không tự công bố tỷ lệ giống khi chưa đo.
- [ ] Nếu bàn giao map chơi được: collider, layer, spawn, reference và camera bounds đã được kiểm tra; playtest chỉ được ghi đạt sau khi thực sự chạy trong phạm vi cho phép.
- [ ] Không ghi đè sandbox, gameplay hoặc thay đổi sẵn có ngoài phạm vi được giao.

## Việc bắt đầu ngay

Người dùng gửi ảnh tham chiếu. AI phân tích ảnh và mở các sheet liên quan trước khi xác định tile, layout và scene cần dựng. Tài liệu GDD/PRD hiện còn phần template; không dùng phần trống làm yêu cầu thiết kế map đã chốt.
