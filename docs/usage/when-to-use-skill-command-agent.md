# Khi nào dùng Skill, Command, Agent?

Tài liệu này giúp team chọn đúng công cụ trong Claude Code khi làm việc với các dự án .NET dùng foundation này.

---

## Tóm tắt nhanh

| Công cụ | Dùng khi nào? | Ví dụ |
|---|---|---|
| **Skill** | Cần scaffold một tác vụ kỹ thuật cụ thể | Sinh Command, Query, Domain Entity, Caching |
| **Command** | Cần chạy một workflow lặp lại nhiều bước | Tạo feature mới, review Clean Architecture |
| **Agent** | Cần một reviewer/chuyên gia tập trung vào một concern | Security review, EF Core review, test strategy |
| **ai-rules** | Cần kiểm tra rule chuẩn hoặc giải thích vì sao | CQRS, tenancy, API contract, testing |

---

## 1. Dùng Skill khi nào?

Dùng **skill** khi bạn đã biết rõ việc cần làm là một tác vụ scaffold cụ thể.

### Nên dùng skill khi:
- cần sinh một use case cụ thể
- cần setup một infrastructure concern cụ thể
- cần tạo một nhóm file theo pattern đã chuẩn hóa

### Ví dụ

```text
/generate-command
/generate-query
/generate-domain-entity
/setup-caching
/setup-error-handling
```

### Cách nghĩ

> Tôi biết mình cần sinh loại artifact nào.

Ví dụ:
- “Tạo command PublishDocument” → dùng `/generate-command`
- “Tạo query GetDocumentList” → dùng `/generate-query`
- “Tạo aggregate Document” → dùng `/generate-domain-entity`

---

## 2. Dùng Command khi nào?

Dùng **command** khi bạn muốn Claude chạy một workflow nhiều bước, không chỉ scaffold một file/nhóm file đơn lẻ.

### Nên dùng command khi:
- bắt đầu một feature mới
- cần review toàn bộ một module
- cần tạo test cho một feature
- muốn workflow ngắn gọn, dễ gọi lại

### Ví dụ

```text
/new-feature
/add-domain-entity
/add-api-endpoint
/review-clean-architecture
/write-unit-tests
/write-integration-tests
```

### Cách nghĩ

> Tôi muốn Claude thực hiện một quy trình từ đầu đến cuối.

Ví dụ:
- “Tạo feature quản lý văn bản mới” → dùng `/new-feature`
- “Review module Documents có vi phạm Clean Architecture không” → dùng `/review-clean-architecture`
- “Viết integration tests cho endpoint tạo văn bản” → dùng `/write-integration-tests`

---

## 3. Dùng Agent khi nào?

Dùng **agent** khi bạn cần một góc nhìn chuyên sâu hoặc review tập trung vào một concern.

### Nên dùng agent khi:
- cần review bảo mật
- cần review kiến trúc
- cần review EF Core/performance
- cần thiết kế test strategy
- cần phân tích bounded context / architecture design

### Ví dụ

```text
@clean-architecture-reviewer
@security-reviewer
@ef-core-reviewer
@test-engineer
@dotnet-backend-architect
```

### Cách nghĩ

> Tôi muốn một chuyên gia tập trung vào một khía cạnh cụ thể.

Ví dụ:
- “Review xem code có leak tenant không” → dùng `@security-reviewer`
- “Review EF Core query có vấn đề performance không” → dùng `@ef-core-reviewer`
- “Đánh giá module này có vi phạm dependency rule không” → dùng `@clean-architecture-reviewer`

---

## 4. Khi nào đọc `ai-rules/`?

`ai-rules/` là nguồn technical rule chuẩn duy nhất.

Đọc `ai-rules/` khi:
- cần hiểu vì sao Claude sinh code theo cách đó
- cần review code AI sinh ra
- cần cập nhật rule chung cho nhiều project
- cần onboard dev mới

### Ví dụ

| Cần hiểu gì | Đọc file nào |
|---|---|
| Clean Architecture | `ai-rules/01-clean-architecture.md` |
| CQRS | `ai-rules/02-cqrs-pattern.md` |
| Security / Tenancy | `ai-rules/03-security-tenancy.md` |
| API Contract | `ai-rules/04-api-contract.md` |
| Testing | `ai-rules/07-testing.md` |
| EF Core | `ai-rules/08-efcore.md` |
| Caching | `ai-rules/12-caching.md` |
| Background Jobs | `ai-rules/13-background-jobs.md` |

---

## 5. Quy tắc chọn nhanh

### Nếu câu hỏi là “tạo cái gì?”
Dùng **Skill**.

Ví dụ:
```text
/generate-command
/setup-caching
```

### Nếu câu hỏi là “làm workflow này từ đầu đến cuối?”
Dùng **Command**.

Ví dụ:
```text
/new-feature
/review-clean-architecture
```

### Nếu câu hỏi là “hãy review như một chuyên gia?”
Dùng **Agent**.

Ví dụ:
```text
@security-reviewer
@ef-core-reviewer
```

### Nếu câu hỏi là “chuẩn đúng là gì?”
Đọc **ai-rules**.

Ví dụ:
```text
ai-rules/02-cqrs-pattern.md
ai-rules/07-testing.md
```

---

## 6. Ví dụ workflow thực tế

### Tạo feature mới

```text
/new-feature
```

Sau đó nếu cần đi sâu:

```text
/generate-command
/generate-query
/write-unit-tests
@clean-architecture-reviewer
```

### Review trước khi merge

```text
/review-clean-architecture
@security-reviewer
@test-engineer
```

### Setup infrastructure concern

```text
/setup-caching
/setup-background-job
/setup-error-handling
```

---

## 7. Nguyên tắc bảo trì

- Không copy full rule vào skill/command/agent
- Skill/command/agent chỉ tham chiếu `ai-rules/`
- Nếu rule thay đổi, sửa trong `ai-rules/` trước
- Nếu workflow thay đổi, sửa trong `.claude/commands/`
- Nếu scaffold pattern thay đổi, sửa trong `.claude/skills/`
- Nếu reviewer concern thay đổi, sửa trong `.claude/agents/`
