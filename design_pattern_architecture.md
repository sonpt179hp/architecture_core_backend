# 2. DESIGN PATTERN ARCHITECTURE GUIDELINES
**Dự án:** Hệ thống quản lý văn bản

Tài liệu này định hướng cách viết code, tổ chức source code (.NET) và luồng giao tiếp giữa các thành phần.

---

## 2.1. Kiến trúc: Clean Architecture + Pragmatic DDD (Cho Core Services)
Tách biệt Business Logic (Domain) ra khỏi Infrastructure (Database, API, Framework). Sử dụng Pragmatic DDD (tinh gọn) cho các Service cốt lõi (Document, Office Management).

### Ưu điểm
- **Khả năng bảo trì:** Code cực kỳ "sạch", dễ dàng thay đổi ORM (từ EF Core sang Dapper) hoặc đổi Database mà không ảnh hưởng tới Business Logic.
- **Testability:** Viết Unit Test cho Domain Layer cực kỳ nhanh chóng và độc lập.

### Nhược điểm
- **Over-engineering với Service nhỏ:** Sẽ rất lãng phí và cồng kềnh nếu áp dụng Clean/DDD cho các Service CRUD đơn giản (như Setting, Master Data).
- **Steep Learning Curve:** Dev cần thời gian để quen với tư duy Aggregate Root, Value Objects thay vì CRUD Entities.

---

## 2.2. Pattern: CQRS (Command Query Responsibility Segregation)
Tách biệt hoàn toàn luồng Ghi (Command) và luồng Đọc (Query) thông qua thư viện `MediatR`.
- **Write (Command):** Dùng Entity Framework Core.
- **Read (Query):** Dùng Dapper kết hợp SQL thuần/View.

### Tại sao chọn kiến trúc này thay vì "Single Repository Pattern" dùng chung cho cả Đọc và Ghi?
- **Thay vì Single Repository:** Nếu dùng EF Core cho mọi query, tác vụ Read sẽ bị chậm do chi phí `Change Tracking` và việc sinh câu lệnh SQL đôi khi không tối ưu. Hệ thống Quản lý Văn bản có đặc thù: Đọc gấp 10 lần Ghi. Nếu trộn chung, luồng Read nặng sẽ block tài nguyên của luồng Write.

### Ưu điểm
- **Tối ưu Performance cực hạn:** Dapper query trực tiếp từ DB map thẳng ra DTO, nhanh gấp nhiều lần EF Core ở các câu query Join phức tạp.
- **Single Responsibility (SRP):** Hàm Đọc không quan tâm đến Validate Logic, Hàm Ghi không bận tâm đến việc Join bảng lấy tên hiển thị. Code Handler trở nên ngắn gọn và dễ focus.

### Nhược điểm
- Tăng số lượng file/class (Mỗi API endpoint thường sinh ra 1 Command, 1 CommandHandler, 1 Validator).
- Developer phải thành thạo cả LINQ (EF Core) và SQL thuần (Dapper).

---

## 2.3. Pattern: Event-Driven Communication (Bất đồng bộ)
Sử dụng Message Broker (Kafka/RabbitMQ) kết hợp với **Outbox Pattern** để giao tiếp và đồng bộ dữ liệu giữa các Microservices.

### Tại sao chọn kiến trúc này thay vì "Synchronous API Calls" (REST/gRPC calls)?
- **Thay vì REST/gRPC (Gọi đồng bộ):** Nếu `Document Service` gọi REST sang `Organization Service` để check cây tổ chức mỗi khi người dùng xem văn bản, sự cố mạng hoặc lỗi ở `Organization Service` sẽ làm sập luôn `Document Service`. (Cascading Failure).
- Cơ chế Event-Driven (Pub/Sub) giúp các Service hoàn toàn Decoupled (tách rời).

### Ưu điểm
- **High Availability (Độ sẵn sàng cao):** Service chết tạm thời cũng không sao, khi sống lại nó sẽ consume tiếp message đang chờ trong Queue.
- **Độ phản hồi API nhanh:** Luồng API của User không bị block bởi các tác vụ nặng (VD: Gửi Email, Cập nhật LTree) vì các tác vụ này được đẩy xuống Background Worker chạy ngầm.

### Nhược điểm
- **Eventual Consistency:** Dữ liệu không nhất quán ngay lập tức, có độ trễ (Delay). Developer cần nắm vững kỹ thuật xử lý để tránh lỗi hiển thị UI cho người dùng.
- Khó debug: Theo dõi (Tracing) luồng lỗi qua nhiều service khó hơn gọi tuần tự. Cần phải tích hợp các công cụ Distributed Tracing (như Jaeger, OpenTelemetry).