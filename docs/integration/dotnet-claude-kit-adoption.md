# Tích hợp `dotnet-claude-kit` vào `architecture_core_backend`

## Mục tiêu

Biến repository này thành **foundation dùng chung cho các dự án .NET** với 2 phần rõ ràng:

1. `ai-rules/` — nguồn canonical, giải thích đầy đủ các quy tắc kỹ thuật
2. `.claude/` — nơi chứa skills, commands, agents và settings cho Claude Code

## Quyết định chính

Không mirror nguyên trạng `dotnet-claude-kit`.
Thay vào đó, dùng nó làm **nguồn tham khảo** và chuẩn hóa repo hiện tại thành một bộ kit nội bộ.

## Mapping

| Thành phần hiện tại | Thành phần mới | Vai trò |
|---|---|---|
| `ai-rules/` | giữ nguyên | Nguồn rule canonical duy nhất cho Claude và developer |
| `.claude/skills/` | giữ nguyên + chuẩn hóa | Skills nghiệp vụ/local pattern |
| _chưa có_ | `.claude/commands/` | Slash-command workflows dùng lại được |
| _chưa có_ | `.claude/agents/` | Chuyên gia đánh giá theo từng concern |
| _chưa có_ | `.claude/settings.json` | Project-level Claude Code settings |
| _chưa có_ | `.mcp.json` | Team-shared MCP config cho Roslyn |

## Cấu trúc sau tích hợp

```text
.claude/
  settings.json
  skills/
  commands/
  agents/
.mcp.json
```

## Nguyên tắc duy trì

- `ai-rules/` là nguồn rule duy nhất
- Skills phải tham chiếu trực tiếp `ai-rules/`
- Không hardcode logic riêng của `GovDocs` nếu muốn tái sử dụng cho repo khác
- Các command/agent mới phải bám theo Clean Architecture và CQRS

## Bước tiếp theo khuyến nghị

1. Chuẩn hóa 9 skills hiện có để tham chiếu `ai-rules/`
2. Bổ sung thêm skills còn thiếu từ `dotnet-claude-kit` theo nhu cầu thực tế
3. Tạo bootstrap guide để copy `.claude/` vào project .NET mới
4. Sau khi ổn định mới cân nhắc bật hooks mặc định
