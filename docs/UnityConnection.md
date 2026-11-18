# Kết nối Codex với Unity

Project dùng Unity CLI + `com.unity.pipeline` để Codex thao tác với Unity Editor.
Cấu hình MCP riêng cho project nằm trong `.codex/config.toml`, pin tới
`D:\thietkegame`; skill CLI nằm trong `.agents/skills/unity-cli/`.

## Sử dụng

1. Mở project bằng Unity 6000.6.9f1. Nếu hiện **Unity Editor Software Terms**,
   người dùng tự đọc và xử lý điều khoản, rồi đợi Unity import/compile xong.
2. Mở project này trong Codex. Sau khi thêm cấu hình MCP, mở phiên Codex mới
   hoặc khởi động lại ứng dụng để nạp server Unity.
3. Yêu cầu một thao tác chỉ đọc trước, ví dụ: “Đọc hierarchy của scene đang mở”.

Codex cũng có thể dùng CLI trực tiếp trong phiên hiện tại:

```powershell
unity status --project-path D:\thietkegame --json
unity command --project-path D:\thietkegame --detail compact --limit 10 --json
```

`status` phải tìm thấy Editor của project này và báo sẵn sàng; việc Codex đọc được
cấu hình MCP hoặc server trả lời initialize chưa chứng minh Editor đã kết nối.

Chỉ thao tác trong phạm vi người dùng giao. Kết nối không tự cấp quyền thay đổi
gameplay, xóa asset, chạy Play Mode, test hoặc build ngoài nhiệm vụ đã cho phép.

Nguồn: [Unity CLI thay thế MCP cũ](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli),
[Unity Pipeline](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package),
[Codex MCP](https://developers.openai.com/codex/mcp).
