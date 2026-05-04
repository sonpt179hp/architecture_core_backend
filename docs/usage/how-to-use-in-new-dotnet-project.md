# Cách dùng foundation này trong một dự án .NET mới

## Mục tiêu

Cho phép một dự án .NET mới dùng lại toàn bộ rules, skills, commands, agents và templates từ repository này.

## Cách áp dụng khuyến nghị

Hiện tại nên dùng **copy curated assets** thay vì git submodule.

### Copy các file/thư mục sau vào project mới

```text
CLAUDE.md
.claude/settings.json
.claude/skills/
.claude/commands/
.claude/agents/
.mcp.json
```

Tuỳ nhu cầu nên copy thêm:

```text
ai-rules/
```

để giữ full technical rules cho team review.

## Trình tự setup

### 1. Khởi tạo solution .NET

Có thể dùng solution tự tạo hoặc scaffold từ template Clean Architecture riêng của team.

### 2. Copy bộ foundation cần thiết

Copy `CLAUDE.md`, `.claude/settings.json`, `.claude/skills/`, `.claude/commands/`, `.claude/agents/` và `.mcp.json` vào root của project mới.

### 3. Cài Roslyn MCP

```bash
dotnet tool install --global Microsoft.MCP.Server.Roslyn
```

Sau đó Claude Code sẽ đọc cấu hình từ:

```text
.mcp.json
```

### 4. Mở Claude Code trong project mới

Claude sẽ có sẵn:
- shared rules
- reusable skills
- commands cho workflow .NET
- agents review chuyên môn

### 5. Workflow khuyến nghị

- Dùng commands để scaffold nhanh
- Dùng skills khi cần thao tác hẹp hơn
- Dùng agents để review trước khi merge

Ví dụ:

```bash
/new-feature
/add-domain-entity
/review-clean-architecture
/write-unit-tests
```

## Quy ước bảo trì

Nếu project mới có custom rules riêng:
- thêm vào `ai-rules/` ở project đó
- không chỉnh trực tiếp foundation trừ khi rule đủ generic để dùng chung

Nếu project mới sinh ra pattern mới đáng tái sử dụng:
- cập nhật lại repo `architecture_core_backend`
- đưa pattern đó vào shared skills/rules/templates

## Khi nào nên sửa foundation này

Sửa lại repo foundation khi:
- pattern dùng được cho nhiều dự án .NET
- rule là generic, không gắn chặt một domain cụ thể
- command/agent giúp giảm lặp lại trong nhiều repo
