# ĐÁNH GIÁ SO SÁNH: HỆ THỐNG CŨ vs. KIẾN TRÚC MỚI
**Dự án:** Hệ thống quản lý văn bản
**Stack cũ:** .NET Framework 4.8 · IIS · SQL Server · Windows Container · Monolith
**Stack mới:** .NET 8 · EF Core 9 · PostgreSQL · Linux Container · Microservices · Docker Swarm

---
## 1. TỔNG QUAN

### 1.1. Từ "Windows-only" sang Cross-platform
Không còn bị ràng buộc bởi Windows Server license. Mỗi service có thể chạy trên Linux node rẻ hơn 3–5 lần, và deploy lên bất kỳ cloud provider nào (AWS, GCP, Azure, on-premise).

### 1.2. Từ "1 DB chung cho tất cả" sang Logical DB (Multiple database) + Partitioning
Database không còn là điểm thắt cổ chai duy nhất. Mỗi service có vùng dữ liệu riêng. Bảng lớn được partition sẵn — query performance ổn định dù dữ liệu tăng hàng chục triệu bản ghi.

### 1.3. Từ "Stored Procedure chứa nghiệp vụ" sang Domain Layer thuần C#
Nghiệp vụ nằm trong code C# rõ ràng, có thể viết unit test trong vài giây mà không cần kết nối DB. Refactor an toàn, onboarding nhanh hơn.

### 1.4. Từ "Gọi nội bộ đồng bộ" sang Event-Driven + Outbox
Hệ thống có thể chịu lỗi thành phần con mà không kéo sập toàn bộ. Phản hồi API nhanh hơn vì tác vụ nặng chạy ngầm. Không mất event dù broker tạm thời gián đoạn.

### 1.5. Từ "Scale thủ công, chậm" sang Auto-scale thông minh
Docker Swarm giúp tăng hoặc giảm số lượng container replica dễ hơn nhiều so với mô hình IIS/Windows cũ. Hạ tầng có thể mở rộng linh hoạt khi tải tăng, đồng thời vẫn giữ được chi phí vận hành hợp lý.

### 1.6. Lợi thế khi tích hợp AI vào kiến trúc mới so với hệ thống cũ

- **AI chạy độc lập, không kéo sập hệ thống chính:** Microservices + Event-Driven cho phép đặt AI (OCR, phân loại văn bản, semantic search, tóm tắt) vào service riêng. Khi AI service lỗi hoặc chậm, luồng nghiệp vụ chính không bị ảnh hưởng — điều không thể đảm bảo khi nhúng AI vào Monolith cũ.
- **SDK AI hiện đại chỉ hoạt động tốt trên .NET 8+:** Semantic Kernel, Microsoft.Extensions.AI, Azure OpenAI SDK đều được thiết kế cho .NET 6/8 trở lên, hỗ trợ async/streaming đầy đủ. Trên .NET Framework 4.8 phải dùng REST call thủ công, thiếu hỗ trợ chính thức từ các nhà cung cấp.
- **Thêm tính năng AI mới mà không động vào code cũ:** Mỗi capability AI mới (OCR → phân loại → hỏi đáp nội dung) chỉ cần subscribe vào event có sẵn trong hệ thống. Trong Monolith cũ, mỗi bổ sung AI đều bắt buộc sửa chạm vào codebase chung, tiềm ẩn rủi ro.

---

## 2. NỀN TẢNG RUNTIME: .NET Framework 4.8 → .NET 8

| Tiêu chí | Hệ thống cũ (.NET 4.8) | Hệ thống mới (.NET 8) |
|---|---|---|
| **Nền tảng hỗ trợ** | Windows-only, bắt buộc IIS | Cross-platform: Linux, macOS, Windows |
| **Container** | Windows Container — image nặng 5–8 GB, khởi động chậm | Linux Container — image chỉ 100–300 MB, khởi động < 2 giây |
| **Hiệu năng runtime** | Không có AOT, JIT chậm hơn | Native AOT, PGO, Span<T> — throughput tăng 2–4x theo benchmark của Microsoft |
| **Long-term Support** | Không còn được Microsoft cập nhật tính năng | LTS đến tháng 11/2026, được đầu tư tính năng cloud-native tích cực |
| **Tuyển dụng nhân sự** | Pool ứng viên thu hẹp dần, ít người học mới | Cộng đồng .NET hiện đại lớn, tài liệu phong phú |
| **Tích hợp AI/Modern Stack** | Khó — thiếu SDK chính thức cho nhiều dịch vụ hiện đại | Hỗ trợ đầy đủ: Semantic Kernel, Azure OpenAI |

**Kết luận:** .NET 8 trên Linux Container giải quyết trực tiếp điểm yếu #1, #2, #3, #7 của hệ thống cũ. Chi phí hạ tầng giảm đáng kể vì không còn bắt buộc dùng Windows Server license.

---

## 3. DATABASE: SQL Server (Monolith) → PostgreSQL (Logical DB per Service)

| Tiêu chí | Hệ thống cũ (SQL Server chung) | Hệ thống mới (PostgreSQL + Logical DB) |
|---|---|---|
| **Mô hình dữ liệu** | 1 SQL Server chứa toàn bộ schema của mọi nghiệp vụ | Mỗi service có Logical DB riêng, ranh giới dữ liệu rõ ràng |
| **Multi-tenancy** | Không có cơ chế native — dễ rò rỉ dữ liệu giữa đơn vị | Shared-schema + Global Query Filter + `TenantId` enforce ở framework |
| **Cây tổ chức phân cấp** | Adjacency List + Recursive CTE — chậm khi có hàng triệu bản ghi | PostgreSQL `ltree` + GiST Index — O(log N), sub-tree query < vài ms |
| **Dữ liệu khổng lồ** | Single huge table → B-tree index phình to → Cache miss → chậm dần | Table Partitioning (List theo TenantId, Range theo CreatedAt) → Query routing trực tiếp vào partition |
| **Chi phí license** | SQL Server Enterprise license rất đắt | PostgreSQL: Open-source, miễn phí hoàn toàn |
| **Stored Procedure nặng** | Logic nghiệp vụ nhúng trong SP — khó test, khó refactor | Logic nằm trong Application Layer (Clean Arch), DB chỉ lưu trữ và truy vấn |
| **Scale DB** | DB là nút cổ chai duy nhất — scale app không giải quyết được | Có thể thêm Read Replica của PostgreSQL cho read-heavy workload |

**Kết luận:** Giải quyết trực tiếp điểm yếu #6 (DB là nút cổ chai) và #8 (khó tách tenant) của hệ thống cũ. PostgreSQL miễn phí license cũng giảm chi phí vận hành đáng kể.

---

## 4. KIẾN TRÚC CODE: Monolith tightly-coupled → Clean Architecture + Pragmatic CQRS + DDD

| Tiêu chí | Hệ thống cũ (Monolith) | Hệ thống mới (Clean Arch + CQRS + DDD) |
|---|---|---|
| **Phụ thuộc chéo** | Services gọi nhau trực tiếp, chung DB, chung config — khó tách | Domain Layer hoàn toàn độc lập, không phụ thuộc Infrastructure |
| **Thay đổi nghiệp vụ** | Sửa 1 chỗ dễ vỡ chỗ khác vì phụ thuộc ẩn | Aggregate Boundary rõ ràng — thay đổi trong 1 Bounded Context không ảnh hưởng BC khác |
| **Hiệu năng Read** | Dùng chung cho cả đọc lẫn ghi dưới database sử dụng store proceduce — Change Tracking tốn CPU | CQRS: Read dùng Dapper + SQL thuần → nhanh hơn gấp nhiều lần ở query Join phức tạp |
| **Khả năng test** | Khó viết unit test vì business logic nhúng trong controller hoặc SP | Domain Layer test hoàn toàn không cần DB. MediatR Handler test độc lập |
| **Thêm tính năng mới** | Tốc độ phát triển giảm dần vì nợ kỹ thuật tích lũy | Mỗi Command/Query Handler là 1 đơn vị độc lập — thêm tính năng không ảnh hưởng flow khác |
| **Học hỏi và tiếp cận dự án** | Dev mới phải hiểu toàn bộ codebase Monolith khi mới join dự án không biết bắt đầu từ đâu | Dev mới chỉ cần hiểu 1 Bounded Context (ví dụ: module Văn bản) là bắt đầu được viết code ngay — kiến trúc đã phân tách rõ ràng |

**Kết luận:** Giải quyết điểm yếu #4 (khó tách service thật sự) và #10 (nợ kỹ thuật tăng, tốc độ phát triển giảm). CQRS đặc biệt phù hợp với đặc thù hệ thống văn bản — Đọc gấp 10 lần Ghi.

---

## 5. GIAO TIẾP GIỮA SERVICES: Gọi nội bộ chặt → Event-Driven + Outbox Pattern

| Tiêu chí | Hệ thống cũ (Synchronous Internal Call) | Hệ thống mới (Event-Driven + MassTransit + Outbox) |
|---|---|---|
| **Độ kết dính** | Service A gọi thẳng Service B → nếu B chết, A chết theo (Cascading Failure) | Pub/Sub hoàn toàn Decoupled — B chết, A vẫn hoạt động bình thường |
| **Độ sẵn sàng** | Toàn bộ monolith down nếu 1 module lỗi | Mỗi service độc lập — lỗi cô lập trong boundary của service đó |
| **Phản hồi API** | User phải chờ toàn bộ chuỗi xử lý hoàn thành | Tác vụ nặng (gửi email, cập nhật ltree) chạy ngầm background — API trả về ngay |
| **Đảm bảo gửi event** | Không có cơ chế — event gửi thất bại thì mất | Outbox Pattern: event lưu DB cùng transaction → không mất event dù broker tạm thời down |
| **Tích hợp service mới** | Phải sửa code monolith, build lại toàn bộ | Subscribe event mới trong service mới — không động vào code service cũ |

**Kết luận:** Giải quyết điểm yếu #4 (phụ thuộc nội bộ chặt) và #9 (khó tích hợp thành phần hiện đại). Event-driven là nền tảng để tích hợp AI service, serverless job, và external system trong tương lai.

---

## 6. HẠ TẦNG TRIỂN KHAI: IIS / Windows Server → Docker Swarm + Linux Container

Hệ thống cũ triển khai trên IIS gắn chặt với Windows Server. Kiến trúc mới chuyển sang Docker Swarm chạy trên Linux Container — dễ tiếp cận hơn so với Kubernetes nhưng vẫn đủ năng lực điều phối Microservices ở quy mô vừa, giúp toàn bộ hạ tầng nhẹ hơn, linh hoạt hơn và tiết kiệm chi phí hơn đáng kể so với stack Windows cũ.

| Tiêu chí | Hệ thống cũ (IIS + Windows Server) | Hệ thống mới (Docker Swarm + Linux Container) |
|---|---|---|
| **Auto-scaling** | Thủ công — phải cấu hình và restart thủ công khi tải tăng | Docker Swarm `--replicas` scale ngang dễ dàng; kết hợp với MassTransit consumer để xử lý tải queue tăng đột biến |
| **Khởi động instance mới** | Windows container khởi động mất 30–90 giây — không đáp ứng kịp burst traffic | Linux container + .NET 8 khởi động dưới 2 giây — scale thêm replica nhanh, ít trễ hơn rất nhiều |
| **Cô lập lỗi giữa tenant** | Không có — 1 tenant xử lý nặng ảnh hưởng trực tiếp toàn bộ hệ thống | Rate Limiting tại API Gateway + Resource Quota ở DB level ngăn chặn 1 tenant ảnh hưởng tenant khác |
| **Quản lý cấu hình và secret** | Config lưu file trên server — dễ lộ thông tin, khó quản lý khi nhiều môi trường | Docker Swarm Secrets + environment config tập trung — secret không nằm trong image hay source code |
| **Giám sát và truy vết lỗi** | Không có công cụ trace xuyên module — lỗi rất khó tìm nguyên nhân | OpenTelemetry tích hợp sẵn trong code .NET — trace toàn bộ hành trình request qua nhiều service |
| **Triển khai và rollback** | Deploy thủ công qua IIS, rollback phải can thiệp tay | Docker Swarm Rolling Update — deploy từng batch container, rollback bằng `docker service rollback` |
| **Chi phí hạ tầng** | Windows Server CAL + IIS + SQL Server Enterprise license = chi phí cố định rất lớn | Linux + PostgreSQL + Docker Swarm = hoàn toàn Open-source, chi phí tỷ lệ theo nhu cầu thực tế |

**Kết luận:** Giải quyết điểm yếu #5 (phụ thuộc IIS/Windows), #7 (autoscale không hiệu quả), #8 (rủi ro vận hành multi-tenant), và #9 (khó tích hợp stack hiện đại). Docker Swarm phù hợp giai đoạn hiện tại — khi hệ thống cần scale lên quy mô lớn hơn, có thể migrate lên Kubernetes mà không cần thay đổi code ứng dụng.

---

## 7. TỔNG HỢP: HỆ THỐNG MỚI GIẢI QUYẾT NHỮNG HẠN CHẾ GÌ CỦA HỆ THỐNG CŨ?

Phần dưới đây tổng hợp lại theo cách dễ đọc hơn: mỗi hạn chế của hệ thống cũ tương ứng với nhóm công nghệ nào trong kiến trúc mới dùng để xử lý.

| Hạn chế của hệ thống cũ | Ảnh hưởng thực tế | Kiến trúc / công nghệ mới khắc phục |
|---|---|---|
| **Runtime cũ, phụ thuộc Windows** | Khó triển khai cloud-native, khó tận dụng Linux, chi phí hạ tầng cao | **.NET 8 cross-platform** chạy tốt trên Linux Container |
| **Windows Container nặng và khởi động chậm** | Scale chậm khi tải tăng, tốn CPU/RAM/disk | **Linux Container + .NET 8** giúp image nhẹ hơn rất nhiều và start nhanh |
| **Scale ngang không hiệu quả** | Khó đáp ứng tải đột biến, tăng tải phải can thiệp thủ công | **Docker Swarm + Linux Container** giúp scale replica đơn giản, nhanh hơn và nhẹ hơn nhiều so với Windows stack cũ |
| **Monolith phụ thuộc chéo** | Sửa 1 chỗ dễ ảnh hưởng nhiều chỗ khác, khó tách service | **Clean Architecture + Pragmatic CQRS + DDD** tách rõ boundary nghiệp vụ |
| **Phụ thuộc IIS / thư viện Windows cũ** | Khó container hóa, khó triển khai hiện đại, khó portability | **Kestrel + .NET 8** không còn phụ thuộc IIS truyền thống |
| **Database là nút cổ chai** | Scale app không kéo theo scale dữ liệu, query nặng làm chậm toàn hệ thống | **PostgreSQL + Logical DB + Partitioning + CQRS Read bằng Dapper** |
| **Khó autoscale mượt theo nhu cầu thật** | Tăng tải đột ngột dễ gây nghẽn hoặc phải dư thừa tài nguyên | **Docker Swarm + container start nhanh** giúp scale replica dễ dàng hơn |
| **Khó tách tenant và cô lập lỗi giữa các đơn vị** | Dễ rò rỉ dữ liệu, tenant lớn có thể ảnh hưởng tenant khác | **Shared-schema multi-tenancy + `TenantId` + Global Query Filter + Rate Limiting** |
| **Khó tích hợp các thành phần hiện đại** | Tích hợp AI, event bus, gateway, tracing tốn nhiều công ghép nối | **Event-Driven + MassTransit/RabbitMQ + gRPC/YARP + OpenTelemetry** |
| **Nợ kỹ thuật tăng theo thời gian** | Tốc độ phát triển chậm dần, khó maintain, khó tuyển người | **Clean Architecture + Testing Strategy + service boundary rõ ràng** |

**Kết luận:** Nếu hệ thống cũ phù hợp để duy trì vận hành ngắn hạn, thì kiến trúc mới phù hợp hơn cho mục tiêu mở rộng dài hạn, phục vụ nhiều tenant, nhiều người dùng và nhiều nghiệp vụ hơn mà vẫn giữ được khả năng bảo trì, kiểm thử và tối ưu chi phí hạ tầng.
---

## 8. RỦI RO CẦN QUẢN LÝ KHI CHUYỂN ĐỔI

| Rủi ro | Mô tả | Cách giảm thiểu |
|---|---|---|
| **Data Migration** | Chuyển SQL Server → PostgreSQL, đặc biệt phần cây tổ chức sang ltree dễ sai | Chạy song song 2 hệ thống trong giai đoạn chuyển tiếp. Pilot với 1-2 đơn vị nhỏ trước, rollback được |
| **Eventual Consistency** | Dữ liệu giữa services có độ trễ đồng bộ ngắn — dễ gây hiển thị sai trên UI | Thiết kế UI chịu được trạng thái trung gian. Document rõ SLA đồng bộ cho từng luồng nghiệp vụ |
| **Multi-Tenant Data Isolation** | Quên filter `TenantId` trong 1 query là có thể lộ dữ liệu đơn vị khác | Bắt buộc Global Query Filter trong EF Core. Architecture Test tự động kiểm tra toàn bộ entity |
| **Debug phân tán** | Lỗi trải qua nhiều service, queue, background worker — rất khó trace | Tích hợp OpenTelemetry + CorrelationId + centralized logging từ ngày đầu, không để sau |
| **Operational Complexity** | Docker Swarm + Broker + PostgreSQL Cluster phức tạp hơn IIS truyền thống | Cần DevOps chuyên trách. Xây dựng runbook cho sự cố. Monitoring và alerting bắt buộc từ sớm |
