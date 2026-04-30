# 4. BACKEND CORE TECHNICAL GUIDELINES
**Dự án:** Hệ thống quản lý văn bản (.NET 8, EF Core 9)

Tài liệu này bổ sung các technical concerns còn thiếu để đủ cơ sở xây dựng một **core backend production-ready**. Nếu 3 file trước trả lời câu hỏi *"chọn kiến trúc gì"*, thì file này trả lời câu hỏi *"khi code core backend thực tế thì phải chuẩn hóa thêm những gì"*.

---

## 4.1. Cross-Cutting Pipeline: Validation, Exception Handling, Audit Logging
Xây dựng một request pipeline thống nhất cho toàn hệ thống thông qua **Middleware + MediatR Behaviors**.

### Phạm vi áp dụng
- **Validation:** FluentValidation cho Command/Query đầu vào.
- **Exception Handling:** Global Exception Middleware trả lỗi theo format chuẩn.
- **Audit Logging:** Ghi nhận ai làm gì, trên dữ liệu nào, lúc nào.
- **Correlation/Trace ID:** Mỗi request phải có mã theo dõi xuyên suốt toàn luồng.

### Tại sao cần chuẩn hóa phần này?
Nếu mỗi API tự validate, tự try/catch và tự log theo cách riêng thì code sẽ nhanh chóng bị lặp, khó kiểm soát và cực khó debug khi hệ thống tách thành nhiều service.

### Ưu điểm
- **Tính nhất quán cao:** Tất cả API tuân theo cùng một chuẩn xử lý lỗi và logging.
- **Giảm lặp code:** Business handler chỉ tập trung vào nghiệp vụ.
- **Dễ truy vết lỗi:** Có CorrelationId/TraceId xuyên suốt từ API đến background worker.
- **Phục vụ kiểm tra nghiệp vụ và pháp lý:** Audit log rất quan trọng với hệ thống quản lý văn bản.

### Nhược điểm
- **Tăng độ trừu tượng:** Dev mới sẽ khó hiểu luồng request hơn so với code controller trực tiếp.
- **Dễ lạm dụng logging:** Nếu log quá nhiều sẽ gây tốn storage và nhiễu khi tra cứu.

### Khuyến nghị triển khai
- Dùng `FluentValidation` trong MediatR Pipeline Behavior.
- Dùng `IExceptionHandler` hoặc Global Middleware để chuẩn hóa lỗi.
- Chuẩn hóa response lỗi theo `ProblemDetails`.
- Tách riêng **Audit Log** và **Technical Log**, không trộn lẫn.

---

## 4.2. Security Architecture: Authentication, Authorization, Tenant Isolation
Đây là phần bắt buộc phải có nếu muốn xây dựng core backend dùng được trong production nhiều tenant.

### Phạm vi áp dụng
- **Authentication:** JWT Bearer hoặc OpenId Connect.
- **Authorization:** Permission-based / Policy-based thay vì hardcode role trong controller.
- **Tenant Isolation:** Tenant context phải được resolve từ token/headers/domain và inject xuyên suốt request.
- **Data Access Boundary:** Tầng truy cập dữ liệu phải chặn truy cập chéo tenant ở mức framework.

### Tại sao cần chuẩn hóa phần này?
Ba tài liệu hiện tại mới nói đến `TenantId` ở mức database, nhưng chưa mô tả cách bảo vệ tenant ở tầng application và API. Nếu không chuẩn hóa từ sớm, nguy cơ rò rỉ dữ liệu liên tenant là lỗi nghiêm trọng nhất của hệ thống SaaS/đa đơn vị.

### Ưu điểm
- **Bảo vệ dữ liệu tenant:** Giảm xác suất query nhầm hoặc truy cập sai phạm vi.
- **Quản trị phân quyền tốt hơn:** Phù hợp hệ thống văn bản có nhiều cấp quyền duyệt/xem/chuyển xử lý.
- **Dễ mở rộng tích hợp SSO:** Có thể kết nối Keycloak, IdentityServer, Entra ID hoặc hệ IAM nội bộ.

### Nhược điểm
- **Thiết kế ban đầu phức tạp:** Permission model cần được làm rõ từ đầu.
- **Tăng chi phí kiểm thử:** Phải test theo nhiều tổ hợp role, permission, tenant scope.

### Khuyến nghị triển khai
- Không đọc `TenantId` trực tiếp từ payload nếu đã có claim đáng tin cậy trong token.
- Dùng Policy-based Authorization cho nghiệp vụ nhạy cảm.
- Tạo `ICurrentUser` / `ITenantContext` abstraction để dùng xuyên Application + Infrastructure.
- Bắt buộc mọi entity multi-tenant phải có `TenantId` và được enforce bằng EF Core Query Filter.
- Các thao tác admin liên tenant phải có cơ chế bypass rõ ràng, audit đầy đủ và cực kỳ hạn chế.

---

## 4.3. API Contract Standardization: Response Model, Versioning, Idempotency
Chuẩn hóa hợp đồng API để frontend, mobile app và tích hợp bên ngoài không bị lệ thuộc vào cách viết controller của từng team.

### Phạm vi áp dụng
- Chuẩn response success/error.
- API versioning.
- Pagination/filter/sort convention.
- Idempotency cho các lệnh ghi có nguy cơ gửi lặp.

### Tại sao cần chuẩn hóa phần này?
Nếu không có chuẩn API ngay từ đầu, mỗi service sẽ trả dữ liệu theo một kiểu khác nhau. Về sau khi tích hợp nhiều team hoặc public API ra ngoài, việc sửa lại contract sẽ rất tốn kém.

### Ưu điểm
- **Frontend tích hợp dễ hơn:** Không phải viết nhiều adapter cho từng service.
- **Giảm breaking changes:** Versioning giúp nâng cấp an toàn.
- **An toàn khi retry:** Idempotency giúp tránh tạo trùng văn bản/công việc khi client gửi lại request.

### Nhược điểm
- **Tăng số quy ước phải tuân thủ:** Team cần kỷ luật cao.
- **Một số API CRUD đơn giản sẽ có cảm giác “nặng tay”.**

### Khuyến nghị triển khai
- Dùng `ProblemDetails` cho lỗi.
- Chuẩn hóa phân trang: `page`, `pageSize`, `totalCount`.
- Áp dụng API versioning từ đầu (`/api/v1/...`).
- Với command quan trọng (tạo văn bản, phát hành, duyệt), cân nhắc `Idempotency-Key`.

---

## 4.4. Resilience Patterns: Retry, Timeout, Circuit Breaker, Inbox/Idempotent Consumer
Khi hệ thống đã dùng event-driven và có giao tiếp giữa nhiều service, resilience không còn là “nice to have” mà là phần cốt lõi.

### Phạm vi áp dụng
- Retry có kiểm soát cho database/message broker/http calls.
- Timeout rõ ràng cho external dependencies.
- Circuit Breaker cho lời gọi đồng bộ.
- Idempotent Consumer / Inbox Pattern cho message processing.

### Tại sao cần chuẩn hóa phần này?
Outbox chỉ đảm bảo **publish message an toàn từ phía producer**. Nó **không giải quyết** việc consumer xử lý trùng message, retry vô hạn, timeout hay lỗi dependency downstream.

### Ưu điểm
- **Tăng độ ổn định hệ thống:** Giảm cascading failure.
- **Xử lý message an toàn hơn:** Tránh tạo trùng dữ liệu khi broker redeliver.
- **Dễ vận hành production:** Có nguyên tắc rõ ràng khi service phụ thuộc bị chậm hoặc lỗi.

### Nhược điểm
- **Dễ cấu hình sai:** Retry quá nhiều có thể làm nghẽn hệ thống hơn.
- **Khó debug hơn:** Kết quả cuối cùng phụ thuộc nhiều trạng thái trung gian (retry, dead-letter, timeout).

### Khuyến nghị triển khai
- Retry chỉ áp dụng cho lỗi transient, không retry lỗi business.
- Timeout phải explicit, không để phụ thuộc mặc định framework.
- Với consumer, lưu `MessageId`/`EventId` đã xử lý để chống xử lý lặp.
- Thiết kế Dead-letter queue và cơ chế replay có kiểm soát.

---

## 4.5. Observability: Structured Logging, Metrics, Tracing, Health Checks
Muốn vận hành backend production ổn định thì phải quan sát được hệ thống theo thời gian thực.

### Phạm vi áp dụng
- **Structured Logging** (`Serilog` hoặc tương đương).
- **Metrics** (request duration, DB latency, queue depth, consumer lag).
- **Distributed Tracing** (`OpenTelemetry`).
- **Health Checks** cho app, DB, broker, cache.

### Tại sao cần chuẩn hóa phần này?
File distributed system đã nhắc đến observability ở mức hạ tầng, nhưng chưa có guideline cho tầng code .NET. Nếu code không phát sinh đúng telemetry, hệ thống giám sát cũng không phát huy tác dụng.

### Ưu điểm
- **Phát hiện sự cố sớm:** Thấy được service nào chậm, queue nào tắc.
- **Rút ngắn thời gian debug:** Trace xuyên service giúp truy vết đầy đủ.
- **Dễ tối ưu hiệu năng:** Có số liệu thực tế thay vì đoán mò.

### Nhược điểm
- **Tăng chi phí lưu trữ và hạ tầng giám sát.**
- **Nếu thiết kế log kém có thể lộ dữ liệu nhạy cảm.**

### Khuyến nghị triển khai
- Log theo dạng structured, không log string tự do là chính.
- Không log token, mật khẩu, nội dung bí mật của văn bản.
- Health check chia thành `liveness` và `readiness`.
- Tất cả background worker cũng phải phát metrics/traces như API service.

---

## 4.6. Testing Strategy: Unit, Integration, Contract, Architecture Tests
Ba file hiện tại định hướng kiến trúc khá tốt, nhưng chưa có chiến lược kiểm thử để đảm bảo kiến trúc đó không bị “vỡ” theo thời gian.

### Phạm vi áp dụng
- **Unit Test:** Domain logic, validators, policies.
- **Integration Test:** EF Core, PostgreSQL, message broker, outbox/inbox.
- **Contract Test:** API contract và event contract giữa services.
- **Architecture Test:** Kiểm tra rule phụ thuộc giữa các layer.

### Tại sao cần chuẩn hóa phần này?
Core backend chỉ thật sự “production-ready” khi các quyết định kiến trúc được kiểm chứng bằng test tự động. Nếu không, Clean Architecture/CQRS chỉ dừng ở tài liệu mà không được enforce.

### Ưu điểm
- **Giảm regression:** Thay đổi code ít làm vỡ hệ thống ngầm.
- **Tăng độ tin cậy khi refactor:** Đặc biệt quan trọng với DDD/CQRS.
- **Bảo vệ contract liên service:** Tránh lỗi tích hợp khi đổi schema event/API.

### Nhược điểm
- **Tốn công đầu tư ban đầu:** Integration test với DB thật và broker thật phức tạp hơn mock.
- **Pipeline CI chạy lâu hơn** nếu không tổ chức hợp lý.

### Khuyến nghị triển khai
- Domain test không phụ thuộc DB.
- Integration test nên chạy với PostgreSQL thật (containerized), không chỉ dùng in-memory provider.
- Có contract test cho event publish/consume quan trọng.
- Dùng architecture tests để chặn Infrastructure reference ngược vào Domain/Application.

---

## 4.7. EF Core 9 Operational Guidelines: Migrations, Concurrency, Transaction Boundaries
Vì dự án chọn EF Core 9 làm nền tảng persistence chính, cần có guideline riêng để tránh lỗi ở production.

### Phạm vi áp dụng
- Migration strategy.
- Optimistic concurrency.
- Transaction boundary.
- Performance tuning cho query/write path.

### Tại sao cần chuẩn hóa phần này?
Hiện tài liệu database mới tập trung vào mô hình dữ liệu, chưa nói rõ cách **thực thi bằng EF Core 9**. Đây là khoảng trống rất thực tế vì phần lớn lỗi production .NET backend nằm ở transaction, migration hoặc query performance.

### Ưu điểm
- **Giảm lỗi khi deploy schema.**
- **Giảm mất dữ liệu/cập nhật đè dữ liệu** khi nhiều user thao tác đồng thời.
- **Tối ưu hiệu năng thực tế** khi dùng EF Core ở write model.

### Nhược điểm
- **Cần discipline cao từ team:** Không thể để mỗi dev tự đặt rule migration khác nhau.
- **Một số tính năng nâng cao** như concurrency token sẽ làm code update phức tạp hơn.

### Khuyến nghị triển khai
- Mỗi bounded context/service tự quản migration riêng.
- Không auto-run migration khi app startup ở production nếu chưa có chiến lược kiểm soát.
- Dùng optimistic concurrency cho bản ghi nghiệp vụ quan trọng.
- Query read-only phải dùng `AsNoTracking()` nếu vẫn đi qua EF Core.
- Chỉ mở transaction ở đúng application use case cần thiết, tránh transaction kéo dài.

---

## 4.8. KẾT LUẬN VÀ KHUYẾN NGHỊ ƯU TIÊN
Ba tài liệu hiện tại **đã tốt ở tầng định hướng kiến trúc lớn**, đặc biệt là:
- Database strategy.
- Coding architecture theo Clean/CQRS/Event-driven.
- ADR về Dapr/K8s/Swarm.

Tuy nhiên, để đủ cơ sở xây dựng **core backend thực chiến**, cần bổ sung ngay các guideline sau theo thứ tự ưu tiên:

1. **Security Architecture** — vì đây là lớp bảo vệ tenant và dữ liệu nghiệp vụ.
2. **Cross-Cutting Pipeline** — để chuẩn hóa validation, lỗi, audit và trace.
3. **Resilience Patterns** — vì hệ thống đã định hướng event-driven/microservices.
4. **Testing Strategy** — để enforce kiến trúc bằng kiểm thử tự động.
5. **EF Core 9 Operational Guidelines** — để giảm lỗi migration, transaction, concurrency.
6. **API Contract Standardization** — để nhiều team tích hợp ổn định về lâu dài.
7. **Observability** — để vận hành production có thể quan sát và debug được.

Nếu chỉ được bổ sung **1 file duy nhất**, thì file này nên được xem là **bộ technical guardrails** cho team backend trước khi bắt đầu scaffold core solution và các service đầu tiên.
