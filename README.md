# Architecture Core Backend — GovDocs

> **Dự án:** Hệ thống quản lý văn bản (GovDocs)
> **Mục đích repository:** Tài liệu kiến trúc, quy tắc AI, skills scaffold code và solution mẫu cho core backend .NET 8

---

## Cấu trúc thư mục

```
architecture_core_backend/
├── docs/                          # Tài liệu tổng quan trình bày
├── overviews/                     # Tài liệu kiến trúc chi tiết
├── ai-rules/                      # Quy tắc kỹ thuật cho AI
├── .claude/skills/                # Skills scaffold code tự động
├── GovDocs/                       # Solution mẫu .NET 8 Clean Architecture
└── README.md                      # File này
```

---

## 1. Tài liệu tổng quan (`docs/`)

Hai tài liệu này dùng để giới thiệu tổng quan dự án.

| Tài liệu | Mục đích |
|---|---|
| `docs/architecture-overview.md` | Tổng quan kiến trúc core backend .NET 8: Clean Architecture, CQRS, Event-Driven, Multi-tenancy |
| `docs/ai-code-generation.md` | Quy trình sử dụng AI để gen code backend tự động theo 3 bước: Tiếp nhận đầu vào → Điều phối AI → Triển khai tự động |

**Khi nào dùng:**
- Trình bày định hướng kiến trúc
- Onboarding cho thành viên mới vào dự án
- Giải thích cách dự án áp dụng AI có kiểm soát

---

## 2. Tài liệu kiến trúc chi tiết (`overviews/`)

Sáu tài liệu này là **nền tảng quyết định kiến trúc** và **technical guardrails** cho toàn bộ dự án.

| Tài liệu | Nội dung |
|---|---|
| `overviews/design_pattern_architecture.md` | Clean Architecture, Pragmatic DDD, CQRS, Event-Driven — cách tổ chức code backend |
| `overviews/database_architecture.md` | Multi-tenancy, ltree cho cây tổ chức, partitioning cho bảng lớn |
| `overviews/backend_core_technical_guidelines.md` | Security, validation pipeline, resilience, observability, testing, EF Core vận hành |
| `overviews/distributed_system_design.md` | Phân tích Dapr, so sánh Docker Swarm vs Kubernetes, khuyến nghị triển khai |
| `overviews/technology_comparison.md` | So sánh hệ thống cũ với kiến trúc mới về runtime, database, code architecture, giao tiếp service |
| `overviews/old_system.md` | Tóm tắt hạn chế của hệ thống cũ, làm cơ sở cho đề xuất kiến trúc mới |

**Thứ tự nên đọc:**

1. `design_pattern_architecture.md` — Hiểu nguyên tắc thiết kế tổng thể
2. `database_architecture.md` — Hiểu cách tổ chức dữ liệu
3. `backend_core_technical_guidelines.md` — Hiểu các chuẩn kỹ thuật bắt buộc
4. `distributed_system_design.md` — Hiểu chiến lược triển khai
5. `technology_comparison.md` — Hiểu bối cảnh so sánh
6. `old_system.md` — Hiểu lý do thay đổi

---

## 3. Quy tắc AI (`ai-rules/`)

13 file quy tắc kỹ thuật bắt buộc mà AI phải tuân thủ khi sinh code. Mỗi file có cấu trúc **DO / DON'T / Ví dụ C#**.

| File | Nội dung |
|---|---|
| `01-clean-architecture.md` | Dependency rule, scope DDD, phân tách layer Domain / Application / Infrastructure / API |
| `02-cqrs-pattern.md` | Tách write (EF Core) và read (Dapper + SQL), cấu trúc Command/Query/Handler/Validator |
| `03-security-tenancy.md` | Resolve TenantId từ token, ICurrentUser, ITenantContext, Global Query Filter, Policy auth |
| `04-api-contract.md` | Versioning `/api/v1/`, ProblemDetails, phân trang chuẩn, HTTP status codes, Idempotency-Key |
| `05-resilience.md` | Retry chỉ cho transient error, Idempotent Consumer, Dead Letter Queue, Circuit Breaker |
| `06-observability.md` | Serilog structured logging, OpenTelemetry, health checks `/live` và `/ready`, audit log tách biệt |
| `07-testing.md` | Unit test Domain, integration test PostgreSQL thật (Testcontainers), Architecture Test, Contract Test |
| `08-efcore.md` | AsNoTracking cho read, migration theo bounded context, optimistic concurrency, transaction boundary |
| `09-error-handling.md` | Exception hierarchy, Global Exception Handler, ProblemDetails, mapping exception → HTTP status |
| `10-dependency-injection.md` | IServiceCollection extensions, service lifetimes, module registration, container validation |
| `11-configuration.md` | Options Pattern, appsettings structure, secrets strategy, startup validation, feature flags |
| `12-caching.md` | Redis distributed cache, cache-aside pattern, tenant-aware cache keys, invalidation, TTL |
| `13-background-jobs.md` | Outbox/Inbox pattern, BackgroundService polling, Hangfire/Quartz scheduled jobs |

**Khi nào dùng:**
- AI đọc các file này trước khi sinh code
- Developer review code AI sinh ra có tuân thủ rules không
- Onboarding để hiểu chuẩn kỹ thuật của dự án

---

## 4. Skills scaffold code (`.claude/skills/`)

9 skills giúp AI tự động sinh code theo đúng convention của dự án. Mỗi skill có workflow chi tiết và tham chiếu đến `ai-rules/`.

| Skill | Lệnh gọi | Chức năng |
|---|---|---|
| `generate-command` | `/generate-command` | Sinh Command + CommandHandler + Validator + Controller action (write use case) |
| `generate-query` | `/generate-query` | Sinh Query + Dapper Handler + DTO + GET action (read use case) |
| `generate-domain-entity` | `/generate-domain-entity` | Sinh Aggregate Root + Value Objects + Domain Events + EF Config + Repository |
| `add-event-handler` | `/add-event-handler` | Sinh MassTransit Consumer + Idempotency + ConsumerDefinition + DLQ |
| `setup-error-handling` | `/setup-error-handling` | Sinh exception hierarchy + GlobalExceptionHandler + ProblemDetails mapping |
| `setup-dependency-injection` | `/setup-dependency-injection` | Sinh DI extension methods + Program.cs structure + container validation |
| `setup-configuration` | `/setup-configuration` | Sinh Options classes + appsettings structure + validation + secrets strategy |
| `setup-caching` | `/setup-caching` | Sinh Redis connection + cached repository decorator + cache key helpers |
| `setup-background-job` | `/setup-background-job` | Sinh Outbox table + processor BackgroundService + Hangfire setup |

**Cách dùng:**
- Trong Claude Code CLI hoặc IDE extension, gõ `/generate-command` để gọi skill
- AI sẽ tự động sinh toàn bộ file cần thiết theo đúng cấu trúc Clean Architecture
- Sau khi sinh xong, AI tự chạy build, test và sửa lỗi cho đến khi pass

---

## 5. Solution mẫu (`GovDocs/`)

Solution .NET 8 mẫu theo đúng kiến trúc Clean Architecture + CQRS + Event-Driven đã được định nghĩa trong tài liệu.

```
GovDocs/
├── src/
│   ├── GovDocs.Domain              # Aggregate Roots, Value Objects, Domain Events
│   ├── GovDocs.Application         # Commands, Queries, Handlers, Validators
│   ├── GovDocs.Infrastructure      # EF Core, Dapper, Redis, MassTransit, Background Jobs
│   └── GovDocs.Api                 # Controllers, Middleware, Swagger, Health Checks
├── tests/
│   ├── GovDocs.UnitTests           # Unit tests cho Domain và Application
│   └── GovDocs.IntegrationTests    # Integration tests với Testcontainers
├── .github/workflows/              # CI/CD pipelines
├── .claude/skills/                 # Skills riêng cho solution này (nếu có)
├── docker-compose.yml              # PostgreSQL, Redis, RabbitMQ local
├── Dockerfile                      # Container image cho API
└── GovDocs.sln                     # Solution file
```

**Mục đích:**
- Làm template để khởi tạo bounded context mới
- Làm tài liệu tham khảo khi viết code thủ công
- Làm cơ sở để AI học convention của dự án

---

## Hướng dẫn sử dụng

### Cho người mới tham gia dự án

1. Đọc `docs/architecture-overview.md` để hiểu bức tranh tổng thể
2. Đọc `docs/ai-code-generation.md` để hiểu cách dự án dùng AI
3. Đọc `overviews/design_pattern_architecture.md` để hiểu nguyên tắc thiết kế
4. Đọc `overviews/backend_core_technical_guidelines.md` để hiểu chuẩn kỹ thuật
5. Xem solution mẫu `GovDocs/` để hiểu cấu trúc code thực tế
6. Thử dùng skills trong `.claude/skills/` để sinh code mẫu

### Cho kiến trúc sư / tech lead

1. Đọc toàn bộ `overviews/` để nắm quyết định kiến trúc
2. Review và cập nhật `ai-rules/` khi có thay đổi chuẩn kỹ thuật
3. Cập nhật skills trong `.claude/skills/` khi có pattern mới
4. Duy trì solution mẫu `GovDocs/` để phản ánh best practices mới nhất

### Cho developer

1. Đọc `docs/ai-code-generation.md` để hiểu workflow dùng AI
2. Đọc `ai-rules/` liên quan đến tác vụ đang làm
3. Dùng skills để sinh code, sau đó review và chỉnh sửa theo nghiệp vụ cụ thể
4. Chạy test và đảm bảo tuân thủ architecture tests

---

## Liên hệ và đóng góp

- Nếu phát hiện lỗi trong tài liệu hoặc skills, tạo issue trong repository
- Nếu muốn đề xuất thay đổi kiến trúc, tạo pull request với ADR (Architecture Decision Record)
- Nếu muốn thêm skill mới, tham khảo cấu trúc skills hiện có trong `.claude/skills/`
