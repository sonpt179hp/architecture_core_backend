# README

## Tài liệu kiến trúc

Mô tả ngắn nhiệm vụ của từng file trong thư mục gốc:

- `database_architecture.md` — Định hướng kiến trúc cơ sở dữ liệu: multi-tenancy, ltree cho cây tổ chức và partitioning cho bảng lớn.
- `design_pattern_architecture.md` — Định hướng tổ chức code backend: Clean Architecture, Pragmatic DDD, CQRS và giao tiếp event-driven.
- `distributed_system_design.md` — Phân tích quyết định triển khai Dapr, so sánh Docker Swarm với Kubernetes, đưa ra khuyến nghị áp dụng.
- `backend_core_technical_guidelines.md` — Technical guardrails cho backend production-ready: security, cross-cutting pipeline, resilience, observability, testing và EF Core vận hành.
- `technology_comparison.md` — So sánh hệ thống cũ với kiến trúc mới về runtime, database, code architecture, giao tiếp service và hạ tầng triển khai.
- `old_system.md` — Tóm tắt các hạn chế của hệ thống cũ, làm cơ sở cho đề xuất kiến trúc mới.

---

## AI Rules (`ai-rules/`)

Bộ luật nền tĩnh cho AI, cấu trúc **DO / DON'T / Ví dụ C#**. Traceable về đúng section trong các file kiến trúc gốc.

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

---

## Claude Code Skills (`.claude/skills/`)

Workflow thực thi cho AI scaffold code theo đúng convention. Mỗi skill có numbered steps, edge cases và references sang `ai-rules/`.

| Skill | Lệnh gọi | Chức năng |
|---|---|---|
| `generate-command` | `/generate-command` | Scaffold Command + CommandHandler + Validator + Controller action (write use case) |
| `generate-query` | `/generate-query` | Scaffold Query + Dapper Handler + DTO + GET action (read use case) |
| `generate-domain-entity` | `/generate-domain-entity` | Scaffold Aggregate Root + Value Objects + Domain Events + EF Config + Repository |
| `add-event-handler` | `/add-event-handler` | Scaffold MassTransit Consumer + Idempotency + ConsumerDefinition + DLQ |
