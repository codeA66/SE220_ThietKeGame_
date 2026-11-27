# Git Conventions & Workflow

> Quy chuẩn bắt buộc cho toàn bộ thành viên và AI agents khi tạo commit, branch, hoặc Pull Request trên `SE220_ThietKeGame_`.

---

## 1. Khởi tạo môi trường Git lần đầu (One-Time Team Setup)

Ngay sau khi `git clone` hoặc kéo nhánh `main` mới nhất về máy, chạy script khởi tạo để tự động cấu hình `.githooks`, `core.longpaths`, `Git LFS` và bộ gộp cảnh `UnityYAMLMerge` (SmartMerge):

```powershell
# Windows (PowerShell)
pwsh -ExecutionPolicy Bypass -File .\tools\setup-git.ps1
```

```bash
# macOS / Linux / Git Bash
./tools/setup-git.sh
```

Script này kích hoạt tự động:
- **`.githooks/pre-commit`**: Chặn quên/thừa file `.meta` trong `Assets/`, chặn dấu conflict `<<<<<<<`, chặn gọi `Debug.Log*` trần hoặc `UnityEngine.Input` cũ trong runtime C#, chặn stage nhầm thư mục tạm của AI agents (`.superpowers/`, `.ags/`, `.fortress-workspaces/`...) và file nhị phân `> 25 MB` ngoài LFS.
- **`.githooks/commit-msg`**: Kiểm tra định dạng Conventional Commits trước khi tạo commit.
- **`UnityYAMLMerge`**: Tự động gộp thông minh các file YAML của Unity (`.unity`, `.prefab`, `.asset`, `.mat`, `.controller`, `.anim`).

---

## 2. Định dạng Commit (Conventional Commits)

Sử dụng chuẩn [Conventional Commits](https://www.conventionalcommits.org/) kèm `scope`:

```text
type(scope): imperative summary
```

### Các `type` hợp lệ

| Type | Khi nào sử dụng |
| :--- | :--- |
| `feat` | Tính năng mới (gameplay, UI, màn chơi, VFX, công cụ) |
| `fix` | Sửa lỗi (bugfix) |
| `refactor` | Tái cấu trúc mã nguồn (không đổi hành vi bên ngoài) |
| `test` | Thêm hoặc cập nhật EditMode / PlayMode tests |
| `perf` | Tối ưu hiệu năng, bộ nhớ, pooling |
| `docs` | Cập nhật tài liệu (`docs/`, `README.md`) |
| `build` | Cấu hình build Unity, `.asmdef`, UPM packages |
| `ci` | GitHub Actions workflows, scripts kiểm tra CI/CD |
| `chore` | Cấu hình Git, dọn dẹp tài nguyên, công việc bảo trì |
| `style` | Định dạng mã nguồn, khoảng trắng (không đổi logic) |

### Quy tắc viết Subject
- **Chữ thường (`lowercase`)** cho `type` và `scope`; động từ nguyên mẫu mệnh lệnh tiếng Anh (`add`, `fix`, `prevent`, `extract` — không dùng `added` hay `adds`).
- **Không đặt dấu chấm `.`** ở cuối dòng tiêu đề.
- **Giới hạn độ dài:** Tối đa `72` ký tự (khuyến khích gọn dưới `55` ký tự).
- **Kỷ luật Atomic Commit:** Mỗi commit chỉ giải quyết **1 mối quan tâm logic duy nhất** và luôn để lại repo ở trạng thái biên dịch sạch, đầy đủ cặp `Asset ↔ .meta`.

### Ví dụ chuẩn
```text
feat(player): preserve crouched collider across roll and bow draw
fix(nggc): close full map on gamepad start without opening pause
test(hud): cover minimap gate invalidation and secret room stealth
ci(guardrails): add static meta pairing and asmdef cycle check
```

---

## 3. Đặt tên Nhánh & Chiến lược Trunk-Based PR

### Quy tắc đặt tên nhánh
```text
<type>/<scope>-<short-kebab-description>
```
Ví dụ: `feat/nggc-vault-alcove`, `fix/hud-scroll-modal-focus`, `ci/github-actions-pipeline`.

### Chiến lược nhánh (`main` + Feature PRs)

| Nhánh | Vai trò | Quy tắc hợp nhất |
| :--- | :--- | :--- |
| `main` | Nhánh chính luôn biên dịch sạch và chạy được | Không push trực tiếp code chưa kiểm chứng; bảo vệ chống `force-push` |
| `feat/*`, `fix/*`, `refactor/*`, `ci/*`, `docs/*` | Nhánh phát triển tính năng / sửa lỗi ngắn hạn tách từ `main` | Mở Pull Request vào `main`, vượt qua cổng kiểm tra CI `Repo & Unity Guardrails` + tests trước khi merge |
| Tag `v*.*.*` (`v0.1.0`, `v0.2.0`, ...) | Đánh dấu mốc phát hành / nộp bài Sprint | Tự động kích hoạt workflow `Build & Release` xuất bản Windows `.zip` và WebGL |

---

## 4. Quy tắc Vàng với Unity, `.meta` & Git LFS

1. **Tuyệt đối không tự tạo/sửa/xóa tay file `.meta`:** Luôn để Unity Editor (hoặc `AssetDatabase`) sinh/xóa `.meta` để bảo toàn GUID tham chiếu trên toàn dự án.
2. **Không để thư mục rỗng trong `Assets/`:** Git không lưu thư mục rỗng; một file `Folder.meta` không có file bên trong sẽ bị Unity tự xóa trên máy thành viên khác khi clone mới.
3. **Git LFS cho tài nguyên nhị phân:** Mọi file `.png`, `.wav`, `.mp3`, `.ogg`, `.psd`, `.ttf`, `.otf`, `.mp4`, `.fbx`, `.zip` đều được định tuyến tự động qua Git LFS trong `.gitattributes`. Đảm bảo máy đã cài `git lfs version` trước khi push tài nguyên mới.
4. **Kiểm chứng nhanh trước khi mở PR:**
   ```bash
   python tools/ci/verify_guardrails.py
   ```
