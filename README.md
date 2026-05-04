# Architecture Core Backend — .NET Claude Foundation

> **Mục đích repository:** Foundation dùng chung cho các dự án .NET để phát triển với Claude Code.
> Repository này tập trung vào: **technical rules**, **reusable skills**, **reusable commands**, **specialized agents**, **sample solution**, và **hướng dẫn áp dụng cho project mới**.

---

## 1. Repository này dùng để làm gì?

Repository này đóng vai trò là **bộ nền dùng chung cho các dự án .NET** trong tương lai.

Nó giúp chuẩn hóa 4 lớp:

1. **Technical rules** — quy tắc kỹ thuật và kiến trúc mà AI và developer phải tuân thủ
2. **Skills** — các tác vụ scaffold code lặp lại theo Clean Architecture
3. **Commands** — các workflow ngắn gọn gọi bằng slash command
4. **Agents** — các reviewer / specialist theo từng concern như architecture, security, EF Core, testing

Ngoài ra repo còn có:
- **GovDocs/** làm solution mẫu tham chiếu
- **docs/** và **overviews/** làm tài liệu onboarding và architectural context

---

## 2. Cấu trúc thư mục và nhiệm vụ của từng phần

```text
architecture_core_backend/
├── CLAUDE.md
├── .mcp.json
├── .claude/
│   ├── settings.json
│   ├── skills/
│   ├── commands/
│   └── agents/
├── ai-rules/
├── docs/
│   ├── integration/
│   └── usage/
├── overviews/
├── templates/
├── GovDocs/
└── README.md
```

### `CLAUDE.md`
File **quan trọng nhất** cho Claude Code trong repo này.

Claude Code sẽ tự động đọc file này khi mở repo, nhờ đó hiểu:
- repo này là foundation dùng chung cho .NET
- `ai-rules/` là nguồn rule chuẩn duy nhất
- khi nào nên dùng skills, commands, agents
- cách áp dụng foundation này cho project mới

### `.mcp.json`
File cấu hình MCP dùng chung cho team.

Hiện tại file này khai báo **Roslyn MCP** để Claude Code có thể làm việc tốt hơn với .NET solution:
- semantic code navigation
- type lookup
- symbol resolution
- giảm việc phải đọc toàn bộ file thủ công

### `.claude/settings.json`
Cấu hình project-level cho Claude Code.

Hiện tại file này dùng để:
- xác định permissions mặc định
- là nơi đúng để cấu hình hooks về sau nếu cần

### `.claude/skills/`
Chứa các **skills tái sử dụng** để scaffold code theo pattern chuẩn.

Mỗi skill là một thư mục có `SKILL.md` riêng.

Các skills hiện có:
- `generate-command` — scaffold CQRS write use case
- `generate-query` — scaffold CQRS read use case
- `generate-domain-entity` — scaffold Aggregate Root / DDD entity
- `add-event-handler` — scaffold MassTransit consumer / event handler
- `setup-error-handling` — scaffold exception hierarchy + global exception handler
- `setup-dependency-injection` — scaffold DI structure
- `setup-configuration` — scaffold Options Pattern và appsettings structure
- `setup-caching` — scaffold Redis caching / decorator pattern
- `setup-background-job` — scaffold Outbox/Inbox + background jobs

**Vai trò:**
- phục vụ sinh code đúng convention
- tái sử dụng giữa nhiều repo .NET
- luôn tham chiếu trực tiếp `ai-rules/`

### `.claude/commands/`
Chứa các **slash commands** dùng lại được.

Commands hiện có:
- `/new-feature` — scaffold feature CQRS hoàn chỉnh
- `/add-domain-entity` — thêm domain entity / aggregate
- `/add-api-endpoint` — thêm API endpoint
- `/review-clean-architecture` — review architecture compliance
- `/write-unit-tests` — generate unit tests
- `/write-integration-tests` — generate integration tests

**Vai trò:**
- biến các workflow lặp lại thành lệnh ngắn
- giúp dev không phải viết prompt dài mỗi lần
- chỉ mô tả workflow và rule cần đọc, không thay thế `ai-rules/`

### `.claude/agents/`
Chứa các **specialized agents** cho review và design.

Agents hiện có:
- `clean-architecture-reviewer` — kiểm tra dependency rule và layer separation
- `dotnet-backend-architect` — hỗ trợ thiết kế bounded context / backend architecture
- `ef-core-reviewer` — review EF Core patterns và persistence concerns
- `test-engineer` — review / thiết kế test strategy
- `security-reviewer` — audit tenancy, authorization, secrets, security boundaries

**Vai trò:**
- phân vai reviewer theo concern
- dùng khi cần review sâu một chủ đề cụ thể
- không thay thế rules; chỉ dùng rules để review

### `ai-rules/`
Đây là **nguồn technical rules chuẩn duy nhất của repository**.

Tất cả skills, commands, agents và `CLAUDE.md` đều phải tham chiếu về đây.

13 file rule hiện có:
- `01-clean-architecture.md`
- `02-cqrs-pattern.md`
- `03-security-tenancy.md`
- `04-api-contract.md`
- `05-resilience.md`
- `06-observability.md`
- `07-testing.md`
- `08-efcore.md`
- `09-error-handling.md`
- `10-dependency-injection.md`
- `11-configuration.md`
- `12-caching.md`
- `13-background-jobs.md`

**Vai trò:**
- single source of truth cho technical rules
- chứa DO / DON'T / Examples đầy đủ
- dùng cho cả Claude lẫn developer/reviewer

### `docs/`
Tài liệu bổ trợ cho việc sử dụng foundation.

- `docs/integration/` — ghi lại các quyết định tích hợp foundation
- `docs/usage/` — hướng dẫn dùng foundation này cho project mới
- `docs/architecture-overview.md` — tổng quan kiến trúc
- `docs/ai-code-generation.md` — mô tả cách dự án dùng AI để sinh code

### `overviews/`
Tài liệu kiến trúc chi tiết và reasoning nền.

Đây là nơi chứa các tài liệu dạng “vì sao thiết kế như vậy”, dùng cho:
- tech lead
- architect
- onboarding
- review các quyết định kiến trúc lớn

### `templates/`
Hiện tại thư mục này để dành cho **project scaffolding templates** ở cấp solution/repository nếu về sau cần trích template từ `GovDocs/`.

**Lưu ý:** Claude Code không tự động đọc thư mục này.
Nó là thư mục dành cho con người / template repo strategy, không phải runtime folder của Claude.

### `GovDocs/`
Solution mẫu .NET 8 Clean Architecture.

**Vai trò:**
- reference implementation
- ví dụ thực tế để đối chiếu khi viết rules/skills
- không phải rule source chính

---

## 3. Cách Claude Code hiểu repo này

Claude Code **tự động đọc**:
- `CLAUDE.md`
- `.claude/settings.json`
- `.mcp.json`
- `.claude/skills/`
- `.claude/commands/`
- `.claude/agents/`

Claude Code **không tự động đọc** chỉ vì tên thư mục:
- `ai-rules/`
- `docs/`
- `overviews/`
- `templates/`

Vì vậy:
- `CLAUDE.md` phải chỉ rõ rằng `ai-rules/` là nguồn chuẩn
- skills/commands/agents phải tham chiếu trực tiếp `ai-rules/`

---

## 4. Cách dùng foundation này cho một project .NET mới

Xem đầy đủ tại:
- `docs/usage/how-to-use-in-new-dotnet-project.md`
- `docs/usage/when-to-use-skill-command-agent.md`

Tóm tắt:

1. Copy các file/thư mục sau vào project mới:
   - `CLAUDE.md`
   - `.claude/settings.json`
   - `.claude/skills/`
   - `.claude/commands/`
   - `.claude/agents/`
   - `.mcp.json`
2. Nên copy thêm `ai-rules/` để giữ full technical rules
3. Cài Roslyn MCP:
   ```bash
   dotnet tool install --global Microsoft.MCP.Server.Roslyn
   ```
4. Dùng các commands / skills / agents trong project mới

---

## 5. Quy tắc tổ chức cần duy trì

### Giữ nguyên nguyên tắc sau
- `ai-rules/` là **nguồn rule chuẩn duy nhất**
- skills / commands / agents **không copy full rule** từ `ai-rules/`
- skills / commands / agents chỉ nên:
  - nói khi nào dùng
  - workflow ngắn gọn
  - file rule cần đọc
  - output mong muốn
- nếu pattern chỉ hữu ích cho GovDocs thì giữ trong `GovDocs/`
- nếu pattern dùng chung được cho nhiều dự án .NET thì promote vào foundation

### Không nên làm
- tạo thêm thư mục `rules/` khác song song với `ai-rules/`
- nhét full technical rules vào commands hoặc agents
- coi `GovDocs/` là rule source thay cho `ai-rules/`

---

## 6. Khi nào sửa phần nào?

### Sửa `ai-rules/` khi:
- thay đổi technical standards
- thêm một concern kiến trúc mới
- cần cập nhật DO / DON'T / examples

### Sửa `.claude/skills/` khi:
- có pattern scaffold mới cần tái sử dụng
- workflow generate code thay đổi
- muốn AI sinh code đúng hơn cho một use case cụ thể

### Sửa `.claude/commands/` khi:
- có workflow lặp lại mới cần gọi bằng slash command
- muốn rút ngắn prompt cho dev

### Sửa `.claude/agents/` khi:
- cần thêm reviewer/specialist mới
- muốn tách review theo concern sâu hơn

### Sửa `CLAUDE.md` khi:
- thay đổi cách Claude nên hiểu repo
- thay đổi bootstrap strategy cho project mới
- thay đổi priority giữa rules / skills / commands / agents

---

## 7. Hướng dẫn đọc repo cho từng vai trò

### Developer mới
1. Đọc `README.md`
2. Đọc `CLAUDE.md`
3. Đọc `docs/architecture-overview.md`
4. Đọc `ai-rules/` liên quan đến task đang làm
5. Thử dùng commands / skills
6. Xem `GovDocs/` để tham chiếu code thực tế

### Tech Lead / Architect
1. Đọc `README.md`
2. Đọc `CLAUDE.md`
3. Đọc toàn bộ `overviews/`
4. Review `ai-rules/`
5. Review chất lượng của skills / commands / agents
6. Quyết định pattern nào đủ generic để promote vào foundation

### Khi review AI-generated code
1. Xác định task đó dùng skill / command / agent nào
2. Đối chiếu với `ai-rules/`
3. Kiểm tra code thực tế trong `GovDocs/` hoặc project đích
4. Chỉ promote lại vào foundation khi pattern đủ generic

---

## 8. Trạng thái hiện tại của foundation

Hiện tại foundation này đã có:
- 13 technical rules trong `ai-rules/`
- 9 reusable skills trong `.claude/skills/`
- 6 reusable commands trong `.claude/commands/`
- 5 specialized agents trong `.claude/agents/`
- `CLAUDE.md` chuẩn hóa để Claude hiểu repo đúng cách
- `.mcp.json` để hỗ trợ Roslyn MCP
- `GovDocs/` làm solution mẫu tham chiếu

Đây là bộ khung phù hợp để tiếp tục phát triển dùng chung cho các dự án .NET về sau.
