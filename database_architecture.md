# 1. DATABASE ARCHITECTURE GUIDELINES
**Dự án:** Hệ thống quản lý văn bản

Tài liệu này quy định các chiến lược thiết kế cơ sở dữ liệu, giải thích lý do lựa chọn và các rủi ro (trade-offs) cần nắm rõ khi thực thi.

---

## 1.1. Chiến lược: Logical Database & Shared-Schema Multi-Tenancy
Sử dụng một Physical DB Cluster (PostgreSQL) chứa nhiều Logical DB cho từng Service. Áp dụng Shared DB - Shared Schema cho Multi-Tenancy (phân tách dữ liệu bằng `TenantId`).

### Tại sao chọn kiến trúc này thay vì "Database-per-Tenant" hay "Physical DB-per-Service"?
- **Thay vì Database-per-Tenant (Mỗi khách hàng 1 DB):** Kiến trúc này quá tốn kém resource hạ tầng khi số lượng Tenant (tổ chức) lên tới hàng trăm, hàng ngàn. Quá trình chạy Migration/Update Schema cho hàng ngàn DB sẽ trở thành ác mộng vận hành.
- **Thay vì Physical DB-per-Service (Mỗi service 1 cụm server DB riêng):** Sẽ gây lãng phí tài nguyên cực lớn ở giai đoạn đầu và giữa dự án khi tải chưa quá cao. Logical DB vẫn đảm bảo ranh giới dữ liệu (Data Boundary) của Microservices nhưng chia sẻ chung CPU/RAM của 1 cluster mạnh mẽ.

### Ưu điểm
- **Tối ưu chi phí:** Tận dụng tối đa phần cứng (Resource Pooling).
- **Dễ bảo trì:** Update schema chỉ cần chạy 1 lần cho tất cả Tenant.
- **Phát triển nhanh:** Dev không phải lo lắng về việc quản lý connection string phức tạp cho hàng ngàn DB khác nhau.

### Nhược điểm (Và cách khắc phục)
- **Rủi ro rò rỉ dữ liệu (Noisy Neighbor / Data Leak):** Một query quên filter `TenantId` có thể lộ dữ liệu khách hàng khác. 
  -> *Khắc phục:* Bắt buộc cấu hình Global Query Filter trong EF Core.
- **Nghẽn cổ chai vật lý:** Nếu 1 Tenant đột nhiên spam query, cả cluster có thể bị chậm.
  -> *Khắc phục:* Cài đặt Rate Limiting tại API Gateway và Resource Quota ở mức Database.

---

## 1.2. Chiến lược: LTree (Materialized Path) cho Cây Tổ chức
Sử dụng extension `ltree` của PostgreSQL để lưu đường dẫn phân cấp (VD: `TenantA.Org1.Unit2.Dept3`) và phi chuẩn hóa (denormalize) nó sang các bảng như `Documents`.

### Tại sao chọn kiến trúc này thay vì "Adjacency List" (Cột ParentId)?
- **Thay vì Adjacency List:** Cách truyền thống dùng `ParentId` bắt buộc phải sử dụng đệ quy (Recursive CTE) để lấy ra toàn bộ văn bản của nhánh tổ chức. Đệ quy SQL cực kỳ tốn CPU và không thể Scale khi bảng Document có hàng chục triệu bản ghi. LTree đánh index bằng GiST, tìm kiếm Sub-tree (`<@`) chỉ bằng O(1) hoặc O(log N).

### Ưu điểm
- **Hiệu năng Read vô địch:** Truy vấn văn bản xuyên tổ chức, xuyên nhánh (Subtree Query) tốc độ cực kỳ nhanh (chỉ mất vài ms).
- Dễ dàng truy xuất cấp độ (Depth) của một node tổ chức mà không cần code logic phức tạp.

### Nhược điểm
- **Chi phí Write cao:** Khi "Move" một Đơn vị từ Tổ chức A sang Tổ chức B, phải update lại chuỗi `ltree` của tất cả các node con bên dưới nó.
- **Eventual Consistency:** Phải đồng bộ `path` từ `Master_DB` sang `Document_DB` thông qua Message Queue, gây ra độ trễ ngắn.

---

## 1.3. Chiến lược: Table Partitioning (List & Range)
Cắt các bảng dữ liệu khổng lồ (Documents, Tasks, Logs) thành các phân vùng vật lý (Partitions) nhỏ hơn theo `TenantId` (List) hoặc `CreatedAt` (Range).

### Tại sao chọn kiến trúc này thay vì "Single Huge Table" (Một bảng khổng lồ)?
- **Thay vì Single Huge Table:** Khi bảng đạt ngưỡng hàng chục triệu record, B-Tree Index sẽ phình to, RAM không đủ chứa Index (Cache miss), dẫn đến tốc độ Insert và Select giảm sút nghiêm trọng (Database Degration).

### Ưu điểm
- **Query Routing:** Query của Tenant nào đi thẳng vào Partition của Tenant đó, Disk I/O được tối ưu.
- **Data Archiving dễ dàng:** Xóa dữ liệu Log cũ chỉ cần `DROP PARTITION` (vài mili-giây) thay vì `DELETE FROM` (tốn hàng giờ và gây Lock table).

### Nhược điểm
- Tăng độ phức tạp cho Database Admin (DBA). Phải có kịch bản/job tự động tạo Partition mới khi có Tenant mới hoặc sang tháng mới.