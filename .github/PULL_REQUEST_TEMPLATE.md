## Tóm tắt thay đổi (Summary)
<!-- Mô tả ngắn gọn vấn đề giải quyết và giải pháp chính -->
- 

## Liên kết Task / Issue
<!-- Ghi mã issue/task nếu có, hoặc để N/A -->
- Refs: 

## Phạm vi ảnh hưởng (Affected Systems)
- [ ] `Core` / `Camera` / `VFX`
- [ ] `Combat` / `Player` / `Enemies`
- [ ] `HUD` / `Minimap` / `Audio`
- [ ] `NhaGiamGiengChuong` / `Main_Level` (Scenes / Room Prefabs)
- [ ] `Editor` / `Build` / `CI-CD` / `Docs`

## Checklist bắt buộc trước khi Merge
- [ ] Mọi file mới/xóa/di chuyển trong `Assets/` đều đi kèm đúng file `.meta` do Unity sinh ra (`python tools/ci/verify_guardrails.py` PASS).
- [ ] Không dùng `Debug.Log*` trần trong code runtime (chỉ qua `DeadCells.Core.GameDebug`) và không dùng `UnityEngine.Input` cũ.
- [ ] Đã lưu sạch Scene / Prefab / ScriptableObject trong Unity Editor (`0` Missing Script, không làm bẩn asset ngoài phạm vi).
- [ ] Đã chạy kiểm chứng EditMode / PlayMode tests liên quan.

## Bằng chứng kiểm thử (Verification)
<!-- Dán kết quả chạy test, log hoặc ảnh chụp màn hình -->
```text

```
