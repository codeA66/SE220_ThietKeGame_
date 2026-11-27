# SE220 — Thiết Kế Game: Dead Cells-Inspired 2D Action Roguelite

Dự án game hành động đi cảnh 2D phong cách **Dead Cells** phát triển trên **Unity 6 (`6000.6.9f1`, Universal Render Pipeline 2D)** trong khuôn khổ học phần **SE220 — Thiết Kế Game**.

---

## 1. Điểm Nổi Bật Của Dự Án

- **Hệ thống Di chuyển & Chiến đấu tốc độ cao (`DeadCells.Player` & `DeadCells.Combat`):**
  - State Machine thuần C# `18` trạng thái: chạy, nhảy biến thiên độ cao, nhảy đôi trên không (`Air Jump`), lộn né bất tử (`Dodge Roll` + `Perfect Dodge`), bám/trượt tường & bật nhảy tường (`Wall Cling / Wall Jump`), leo dây xích (`Climb`), tự trèo gờ (`Ledge Mantle`), cúi ngồi thu hẹp hộp va chạm né tên (`Crouch`) và giẫm chấn địa từ trên không (`Air Stomp`).
  - Bộ đôi vũ khí khởi đầu **Kiếm chuỗi 3 đòn chí mạng** (`Brutality`) và **Cung 5 mũi tên găm thu hồi được** (`Tactics`, hỗ trợ tự chúc đường ngắm vào mục tiêu thấp), đi kèm nâng cấp chỉ số qua **Cuộn Sức Mạnh (`Power Scroll`: Brutality / Tactics / Survival)**.
- **Hệ sinh thái 5 Chủng Quái & AI đặc trưng (`DeadCells.Enemies`):**
  - `Zombie` (cào cận chiến & nhảy vồ tầm trung), `Rancid Rat` (áp sát tốc độ cao, thân thấp), `Shieldbearer` (khiên chắn đòn trực diện gây choáng ngược, húc ủi `Dash`), `Undead Archer` (bắn tên ngang có thể cúi né, nhảy lùi & dịch chuyển ra sau lưng khi bị dồn góc), `Grenadier` (câu chùm 3 lựu đạn nảy nổ hẹn giờ xuyên tường).
- **Màn chơi Kiến trúc Phòng Đa Tầng (`NhaGiamGiengChuong.unity`):**
  - Tổ hợp `10` Room Prefab liên thông (`AB`, `CD`, `G`, `HJK`, `ELF`, `W1_EastCourtyard`, `W2_SewerCistern`, `W3_WestBastion`, `U1_UpperPromenade` và phòng bí mật ẩn `VaultA`), `34` quái phân bố theo `12` `EncounterZone`, cổng khóa công tắc `SwitchDoor`, cổng dịch chuyển `TeleportGate`, rương hồi máu `HealingChest`, bia cổ ngữ `LoreTablet` và 3 hướng thoát màn.
- **Đồ họa URP 2D, Shader riêng & Âm thanh Tổng hợp (`DeadCells.VFX` & `DeadCells.HUD`):**
  - `10` shader HLSL/URP tùy biến (`CharacterLitHybrid2D`, `PlayerPaletteSwap`, `TorchFlame2D`, `WaterPuddle2D`, `GodRay2D`, `ParallaxFog2D`, `ScrollFocusBlur`...), hệ thống hạt máu/mảnh vỡ vật lý pooling và thư viện tổng hợp âm thanh PCM `22050 Hz` (`ProceduralSfxLibrary`).
  - HUD Dark Fantasy + **Minimap / Full Map Overlay** tự quét địa hình, sương mù khám phá và điểm đánh dấu động.

---

## 2. Yêu Cầu Môi Trường & Cài Đặt Nhanh

| Thành phần | Phiên bản / Yêu cầu |
| :--- | :--- |
| **Unity Editor** | **`6000.6.9f1`** (kèm module **Windows Build Support** và **WebGL Build Support**) |
| **Render Pipeline / Input** | URP 2D `17.6.0` · Unity Input System `1.20.1` |
| **Git & Git LFS** | Git `>= 2.40` + `git-lfs >= 3.0` |
| **Python (cho script kiểm tra nhanh)** | Python `>= 3.10` (chỉ dùng thư viện chuẩn) |

### Bước 1: Clone & Cấu hình Git Hooks + Unity SmartMerge
```powershell
git clone https://github.com/codeA66/SE220_ThietKeGame_.git
cd SE220_ThietKeGame_

# Windows PowerShell — bật .githooks, longpaths, LFS và UnityYAMLMerge:
pwsh -ExecutionPolicy Bypass -File .\tools\setup-git.ps1
```
*(Trên macOS / Linux chạy: `./tools/setup-git.sh`)*

### Bước 2: Mở Dự Án Trong Unity
1. Mở **Unity Hub** → **Add project from disk** → trỏ tới thư mục gốc repo.
2. Mở scene chính tại `Assets/_Project/Scenes/NhaGiamGiengChuong.unity` (Build Index `0`) và nhấn **Play**.
   - Scene thử nghiệm cơ chế riêng lẻ: `Assets/_Project/Scenes/Sandbox_Player.unity`.
   - Scene tuyến màn chơi phụ: `Assets/_Project/Scenes/Main_Level.unity` (Build Index `1`).

---

## 3. Bảng Điều Khiển (Bàn Phím & Tay Cầm)

| Hành động | Bàn phím & Chuột | Tay cầm (Gamepad) |
| :--- | :--- | :--- |
| Di chuyển / Leo xích | `A` `D` / `W` `S` (hoặc Phím mũi tên) | Cần trái (`Left Stick`) / `D-Pad` |
| Nhảy / Nhảy đôi / Bật tường | `Space` | Nút Nam (`A` / `Cross`) |
| Cúi ngồi / Tụt qua sàn một chiều | Giữ `S` hoặc `C` (`S + Space` để tụt sàn) | Cần trái xuống / Nhấn cần phải (`R3`) |
| Giẫm chấn địa trên không (`Stomp`) | `S + Space` khi đang ở trên không | Giữ xuống + Nút Nam khi trên không |
| Tấn công (Kiếm / Cung đang cầm) | Chuột trái (`LMB`) | Nút Tây (`X` / `Square`) |
| Đổi sang Kiếm / Cung | Phím `1` / Phím `2` | `D-Pad Trái` / `D-Pad Phải` |
| Lộn né (`Dodge Roll`) | `Left Shift` | Nút Đông (`B` / `Circle`) |
| Tương tác (`[E]`: Cửa, Cổng, Rương, Scroll) | `E` | Nút Bắc (`Y` / `Triangle`) |
| Mở Bản đồ toàn màn hình (`Full Map`) | `M` hoặc `Tab` | `Select` / `View` |
| Tạm dừng (`Pause Menu`) / Chơi lại nhanh | `Esc` / `R` (khi kết thúc lượt) | `Start` / `Menu` |

---

## 4. Kiến Trúc Thư Mục & Assembly (`.asmdef` DAG)

Toàn bộ mã nguồn đặt dưới `Assets/_Project/`, phân tách thành `15` Assembly Definition phi chu trình (`0` phụ thuộc vòng):

```text
Assets/_Project/
├── Core/                  # DeadCells.Core (StateMachine gốc, GameDebug, Wallet, KillCounter, Audio PCM)
├── Combat/                # DeadCells.Combat (Health, Hurtbox, HitQuery, HitStop, WeaponData, RecoverableArrow)
├── VFX/                   # DeadCells.VFX (VfxSpawner, GeneratedVfx, BloodDrops, Debris, Sparks, TrailRibbon)
├── Features/
│   ├── Camera/            # DeadCells.CameraSystem (CameraFollow2D, ParallaxLayer2D)
│   ├── Currency/          # DeadCells.Currency (GoldWallet)
│   ├── Player/            # DeadCells.Player (18-state FSM, PlayerCombat, PlayerBow, PowerScroll, PlayerVfx)
│   ├── Enemies/           # DeadCells.Enemies (EnemyController, 5 Attack States, ProjectileSpawner, LootDropper)
│   ├── HUD/               # DeadCells.HUD (DarkFantasyHud, MinimapHud, ScrollUpgradeMenu, Feedback)
│   ├── RunFlow/           # DeadCells.RunFlow (RunSession, DefeatScreen)
│   └── NhaGiamGiengChuong/# DeadCells.NhaGiamGiengChuong (LevelRunController, EncounterZone, VaultA, Builder)
├── Editor/Build/          # DeadCells.Build.Editor (BuildPipelineCli cho Windows x64 & WebGL)
└── Tools/Editor/          # DeadCells.TilesetTools.Editor (Công cụ xử lý Tileset, Normal Map, Scene Setup)
```

---

## 5. Kiểm Thử, Guardrails & Build Tự Động (CI/CD)

### Kiểm tra tính toàn vẹn Repo (chạy dưới 2 giây, không cần mở Unity)
```bash
python tools/ci/verify_guardrails.py
```
Kiểm tra tự động: cặp 1:1 `Asset ↔ .meta`, trùng lặp GUID, `Missing Script` trong Scene/Prefab, chu trình `.asmdef`, luật cấm `Debug.Log` trần / `UnityEngine.Input` cũ và chặn file rác AI agents.

### Chạy bộ Test NUnit (`EditMode` & `PlayMode` — hơn 200 bài test)
Trong Unity Editor mở **Window → General → Test Runner**, hoặc chạy qua [Unity CLI](https://docs.unity.com):
```bash
unity test . --mode EditMode --output TestResults-EditMode.xml -- -nographics
unity test . --mode PlayMode --output TestResults-PlayMode.xml
```

### Build Headless từ Dòng Lệnh (`Windows x64` & `WebGL`)
```bash
# Windows x64 -> Build/Windows/DeadCells.exe
unity build . --target StandaloneWindows64 \
  --execute-method DeadCells.Build.Editor.BuildPipelineCli.BuildFromCommandLine \
  --output-path Build/Windows/DeadCells.exe \
  --args "-buildTarget Win64 -buildVersion 0.1.0"

# WebGL -> Build/WebGL
unity build . --target WebGL \
  --execute-method DeadCells.Build.Editor.BuildPipelineCli.BuildFromCommandLine \
  --output-path Build/WebGL \
  --args "-buildTarget WebGL -buildVersion 0.1.0"
```

### Hệ thống GitHub Actions (`.github/workflows/`)
1. **`ci-guardrails.yml`**: Tự động chạy trên mọi Push/PR vào `main` (`< 25s`, không cần license Unity) để bảo vệ `.meta`, GUID, YAML scene/prefab, `.asmdef` DAG và chuẩn Conventional Commits.
2. **`ci-unity-tests.yml`**: Tự động cài Unity `6000.6.9f1` có cache `Library/` và chạy trọn bộ `EditMode` + `PlayMode` tests khi kho cấu hình đủ GitHub Secrets (`UNITY_SERVICE_ACCOUNT_ID`, `UNITY_SERVICE_ACCOUNT_SECRET`, `UNITY_LICENSE_SERIAL` hoặc `UNITY_LICENSE_FILE_BASE64`).
3. **`cd-release.yml`**: Khi đẩy tag `v*.*.*` (ví dụ `git tag v0.1.0 && git push origin v0.1.0`) hoặc kích hoạt thủ công `workflow_dispatch`, tự động đóng gói bản **Windows x64 `.zip`** lên **GitHub Releases**, triển khai bản **WebGL** lên **GitHub Pages**, và tùy chọn đẩy lên **itch.io** qua `butler`.

---

## 6. Tài Liệu Nội Bộ Liên Quan (`docs/`)

- [AGENTS.md](AGENTS.md) & [CLAUDE.md](CLAUDE.md): Nguyên tắc làm việc dựa trên bằng chứng và cấu trúc kỹ thuật.
- [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md) & [docs/NAMING_CONVENTIONS.md](docs/NAMING_CONVENTIONS.md): Quy chuẩn C# và đặt tên tài nguyên Unity.
- [docs/GIT_CONVENTIONS.md](docs/GIT_CONVENTIONS.md): Quy trình Git, Conventional Commits, Hooks và SmartMerge.
- [docs/NhaGiamGiengChuong.md](docs/NhaGiamGiengChuong.md) & [docs/Tests-Nha-Giam-Gieng-Chuong.md](docs/Tests-Nha-Giam-Gieng-Chuong.md): Đặc tả thiết kế màn chơi và ma trận kiểm thử.
