# Gothic Dungeon — bộ asset làm map

Nguồn: `C:\Users\habayakka\Downloads\assetsgame`. Đã xem cả 5 ảnh để phân loại.
Các PNG trong thư mục này là bản sao đổi tên, giữ nguyên byte của ảnh nguồn.
Tên gốc, SHA-256, kích thước, category và vùng cắt nằm trong `import-manifest.json`.

| Tên gốc | Tên mới | Nội dung quan sát được |
|---|---|---|
| `ChatGPT Image Nov 12, 2026, 08_06_10 PM-3.png` | `T_GothicDungeon_Architecture.png` | Vòm, cửa, cửa sổ Gothic, cột, lan can, bệ đỡ, cầu thang, tượng |
| `ChatGPT Image Nov 12, 2026, 08_06_17 PM-4.png` | `T_GothicDungeon_DungeonProps.png` | Lồng, xích, ống, máy móc, lò, đèn, cờ, rương, thùng, nội thất và tượng |
| `ChatGPT Image Nov 12, 2026, 08_06_25 PM-5.png` | `T_GothicDungeon_ElementalEffects.png` | Lửa, portal, phép, nước/băng, khói, đạn và các biến thể rương |
| `ChatGPT Image Nov 12, 2026, 08_06_31 PM-6.png` | `T_GothicDungeon_DebrisAndEffects.png` | Đá, mảnh vụn, gai, tinh thể và các dải VFX |
| `tileset_original_upscaled_4x.png` | `T_GothicDungeon_TerrainAndRuins_4x.png` | Tường đá/rêu, viền, nền/bệ, dốc, kiến trúc, cây, đá và tường đổ |

## Trong Unity

Asset import nằm tại `Assets/_Project/Art/Environment/GothicDungeon/`:

- `Textures/`: 5 sheet, Multiple Sprites; cắt bằng manifest, không dùng lưới đều cho cả sheet.
- `Tiles/`: các mảnh tĩnh, chia theo nhóm Terrain, Architecture, Props, Nature.
- `Palettes/`: 4 palette tương ứng, tên bắt đầu bằng `GothicDungeon_`.

Mở **Window → 2D → Tile Palette**, chọn một palette `GothicDungeon_*`.
Chọn đúng Active Tilemap của scene đang làm trước khi vẽ.
Grid trong palette chỉ sắp xếp các mảnh để chọn; không khẳng định đây là bộ tile
seamless 1 ô. Có mảnh lớn nhiều ô, dốc và module hình dạng không đều.

Đã chuẩn bị 612 Tile asset tĩnh và 63 sprite/VFX tham chiếu (675 sprite tổng cộng).
Tên `Strip_*` là cả dải hiệu ứng, chưa phải một frame animation đã căn chỉnh.
Các vùng VFX chỉ nằm trong Textures, không trộn vào palette làm map.

## Cấu hình import

- PPU 32 cho bốn sheet 1448 × 1086.
- PPU 128 cho sheet terrain 5792 × 4344: đã kiểm tra ảnh này là phóng nearest-neighbor
  đúng 4×, nên hệ số PPU 4× giữ cùng tỷ lệ hình với các sheet còn lại.
- Point filtering, Clamp, sRGB, không mipmap, không compression, không Read/Write.
- Max Size 8192, NPOT None để giữ nguyên độ phân giải sheet terrain.
- Sprite ID tạo ổn định theo tên sheet + tên sprite; không tự sửa file `.meta`.
- Các mảnh tĩnh có render outline theo silhouette để hạn chế lấy lẫn hình bên cạnh
  khi các bounding box trong atlas chồng nhau. Outline không phải collider.

## Giới hạn hiện tại và cách chỉnh

- Tất cả Tile asset đặt **Collider None**. Ý nghĩa nền, đất đặc, platform một chiều,
  prop hay vật cản sẽ xác định theo ảnh map mẫu và đường đi; không suy chỉ từ hình.
- Các dải VFX chưa được chốt frame, pivot chung, FPS hoặc tạo AnimationClip.
- Giữ nguyên PNG gốc; vùng cắt/outline là dữ liệu import và có thể tinh chỉnh.
  `Previews/` đánh số vùng cắt: xanh là mảnh tĩnh, vàng là VFX/tham chiếu.
  Chỉ số là thứ tự trong mảng `sprites` của từng sheet trong manifest.
- Đây chưa phải một map hoàn chỉnh. Ảnh map mẫu sẽ dùng để chốt bố cục, tỷ lệ,
  thứ tự lớp và cách ghép các mảnh.

Menu **Tools → DeadCells → Tilesets → Import Gothic Dungeon** import lại từ manifest.
Thao tác này cập nhật slicing của 5 texture; palette và Tile asset đã có được giữ lại.
Nếu tinh chỉnh bằng Sprite Editor, cần cập nhật manifest trước khi chạy lại menu.
Tool chỉ chạy trong Editor, tại `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`.
