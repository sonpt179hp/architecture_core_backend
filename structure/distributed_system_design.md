# TÀI LIỆU QUYẾT ĐỊNH KIẾN TRÚC (ADR): LỰA CHỌN NỀN TẢNG TRIỂN KHAI DAPR

**Dự án:** Hệ thống quản lý văn bản (.NET 8, EF Core 9)
**Chủ đề:** Đánh giá ưu/nhược điểm khi sử dụng Dapr trên Docker Swarm vs. Kubernetes (K8s).

---

## 1. TỔNG QUAN VỀ DAPR TRONG HỆ THỐNG
**Dapr (Distributed Application Runtime)** áp dụng mẫu kiến trúc **Sidecar**. Thay vì ứng dụng (.NET) phải tự gọi trực tiếp đến RabbitMQ, Redis hay các service khác, một container Dapr (Sidecar) sẽ được chạy song song sát bên cạnh container ứng dụng. Ứng dụng chỉ việc gọi HTTP/gRPC đến Sidecar, và Sidecar sẽ lo toàn bộ việc giao tiếp hạ tầng bên dưới.

Việc chọn nền tảng điều phối (Orchestrator) nào quyết định trực tiếp đến sự thành bại của mô hình Sidecar này.

---

## 2. TRIỂN KHAI DAPR TRÊN DOCKER SWARM

Docker Swarm là công cụ điều phối có sẵn của Docker, nhắm tới sự đơn giản và dễ cài đặt.

### Ưu điểm:
* **Dễ dàng thiết lập ban đầu:** Developer dễ tiếp cận. Chỉ cần sử dụng file `docker-compose.yml` quen thuộc.
* **Tận dụng được sức mạnh API của Dapr:** Code .NET vẫn được hưởng lợi từ việc gọi API chuẩn của Dapr để Pub/Sub, State Management hay Service Invocation.
* **Phù hợp cho môi trường Dev/Test:** Cực kỳ lý tưởng để chạy thử nghiệm Local hoặc môi trường Staging nhỏ.

### Nhược điểm (Điểm nghẽn cần chú ý):
* **Tiêm Sidecar thủ công (Manual Injection):** Đây là vấn đề lớn nhất. Swarm không có cơ chế tự động đính kèm Sidecar. Lập trình viên phải tự khai báo thêm 1 container `daprd` cho *MỖI* container ứng dụng trong file cấu hình. Hệ thống có 15 services -> cấu hình thành 30 containers. File cấu hình sẽ phình to và cực kỳ dễ sai sót.
* **Quản lý mạng (Networking) rườm rà:** Phải tự cấu hình network nội bộ cho từng cặp App - Sidecar để chúng nhìn thấy nhau một cách thủ công.
* **Thiếu Auto-scaling:** Swarm không tự động nhân bản (scale) container khi tải tăng cao, làm giảm đi giá trị tự động hóa của kiến trúc Microservices.

---

## 3. TRIỂN KHAI DAPR TRÊN KUBERNETES (K8S)

Kubernetes hiện là tiêu chuẩn công nghiệp (Industry Standard) cho việc vận hành Microservices trên môi trường Production. Dapr được sinh ra với tư duy "Cloud-Native", coi K8s là môi trường sống ưu tiên nhất.

### Ưu điểm (Vượt trội):
* **Tự động tiêm Sidecar (Auto-Injection):** Thông qua K8s Mutating Webhooks, bạn chỉ cần thêm một dòng chú thích (`annotations: dapr.io/enabled: "true"`) vào cấu hình Deployment của ứng dụng. K8s sẽ tự động chèn container Dapr vào Pod mà không làm rác file cấu hình của bạn.
* **Cơ chế Auto-scaling mạnh mẽ (HPA & KEDA):** K8s theo dõi metrics (như CPU, lượng message trong queue) và tự động tăng/giảm số lượng bản sao của Microservice (và Sidecar của nó) theo thời gian thực.
* **Bảo mật và Cấu hình tập trung:** Dapr trên K8s tích hợp sẵn với K8s Secrets để quản lý mật khẩu an toàn, và sử dụng Custom Resource Definitions (CRDs) để quản lý cấu hình hạ tầng rất mạch lạc.
* **Giám sát (Observability) hoàn hảo:** Tự động bắt mọi traffic đi qua Pod và đẩy về hệ thống giám sát (như Prometheus, ELK) mà không cần can thiệp thủ công.

### Nhược điểm:
* **Đường cong học tập (Learning Curve) rất gắt:** K8s cực kỳ phức tạp. Đòi hỏi team phải học về Pods, Services, Ingress, Helm Charts, PVCs.
* **Tiêu tốn tài nguyên nền:** Một cụm K8s cơ bản (Control Plane) đã yêu cầu máy chủ có cấu hình khá mạnh (thường mất 2-4GB RAM chỉ để duy trì hệ thống lõi), chưa tính đến ứng dụng.
* **Cần chuyên gia vận hành:** Bắt buộc phải có kỹ sư DevOps/SysAdmin cứng tay để xử lý sự cố mạng, chứng chỉ bảo mật hoặc lỗi nghẽn nút (Node failure).

---

## 4. TỔNG KẾT VÀ KHUYẾN NGHỊ TỪ ARCHITECT

Dựa trên đặc thù dự án quản lý văn bản và để tối ưu chi phí rủi ro:

1. **KHÔNG KHUYẾN NGHỊ dùng Dapr nếu chốt giữ hạ tầng là Docker Swarm.** Việc duy trì thủ công hàng chục Sidecar trên Swarm sẽ biến thành "ác mộng vận hành". Nếu dùng Swarm, hãy để các service .NET 8 giao tiếp trực tiếp qua MassTransit (RabbitMQ) và gRPC/YARP.
2. **NÊN ÁP DỤNG Dapr nếu dự án có lộ trình rõ ràng lên Kubernetes.** Nếu tầm nhìn của dự án là scale phục vụ hàng chục ngàn người dùng đa chi nhánh, hoặc đóng gói bán SaaS/On-premise cho nhiều cơ quan khác nhau, hãy nâng cấp hạ tầng lên K8s và đưa Dapr vào ngay từ giai đoạn cấu trúc lõi. Sự kết hợp K8s + Dapr sẽ giúp code .NET của bạn hoàn toàn tách biệt khỏi hạ tầng, dễ dàng cắm-rút (plug-and-play) mọi công nghệ trong tương lai.