# Quy trình Đơn vị phát hành (Tại Vụ không có phòng)

## 1. Biểu đồ luồng nghiệp vụ (Sequence Diagram)

Biểu đồ tuần tự dưới đây thể hiện sự tương tác nhanh gọn do mô hình Vụ không có phòng (bỏ qua cấp phòng, chuyên viên trình trực tiếp lên Lãnh đạo Vụ).

```mermaid
sequenceDiagram
    autonumber
    actor CV as Chuyên viên
    actor PVT as Phó vụ trưởng
    actor VTU as Vụ trưởng
    actor VT as Văn thư đơn vị

    %% Bước 1: Chuyên viên trình
    CV->>PVT: B1. Trình Lãnh đạo đơn vị xử lý (Trường hợp trình PVT)
    CV->>VTU: B1. Trình Lãnh đạo đơn vị xử lý (Trường hợp trình VTU)

    %% Bước 2: Phó vụ trưởng
    PVT-->>CV: B2. Trả lại hiệu chỉnh (nếu không đạt)
    PVT->>VTU: B2. Trình Vụ trưởng (nếu cần VTU phê duyệt)
    PVT->>VT: B2. Ký duyệt & chuyển phát hành (nếu PVT là người ký)

    %% Bước 3: Vụ trưởng
    VTU-->>CV: B3. Trả lại hiệu chỉnh (nếu không đạt)
    VTU->>PVT: B3. Chuyển Phó vụ trưởng phê duyệt (nếu PVT là người ký)
    VTU->>VT: B3. Ký duyệt & chuyển phát hành (nếu VTU là người ký)

    %% Bước 4: Văn thư đơn vị
    VT-->>CV: B4. Trả lại cán bộ soạn thảo (nếu hình thức, thể thức không đạt)
    VT->>VT: B4. Cấp số, ký số tổ chức (nếu có con dấu)
    VT->>VT: B4. Phát hành bản điện tử / in bản giấy
```

## 2. Mô tả chi tiết nghiệp vụ (Chi tiết theo Role)

B1. Chuyên viên:

- Soạn thảo dự thảo, đính kèm các văn bản liên quan.
- Chịu trách nhiệm về hình thức, thể thức văn bản và nhập đầy đủ thông tin văn bản điện tử.
- Tạo lập hồ sơ điện tử chuyển trực tiếp cho Lãnh đạo đơn vị xử lý (Vụ trưởng hoặc Phó vụ trưởng).

B2. Phó vụ trưởng:

- Tiếp nhận dự thảo điện tử và rà soát nội dung.
- Trả lại chuyên viên để hiệu chỉnh nếu không đạt.
- Nếu Phó vụ trưởng là người ký duyệt: Ký duyệt văn bản đi và chuyển Văn thư đơn vị phát hành.
- Nếu cần Vụ trưởng phê duyệt: Chuyển dự thảo văn bản đi lên Vụ trưởng.

B3. Vụ trưởng:

- Tiếp nhận dự thảo văn bản đi và rà soát nội dung.
- Trả lại chuyên viên để hiệu chỉnh nếu không đạt.
- Nếu Vụ trưởng là người ký duyệt: Ký duyệt văn bản đi và chuyển Văn thư đơn vị phát hành.
- Nếu Phó vụ trưởng là người ký duyệt: Chuyển dự thảo văn bản đi cho Phó vụ trưởng phê duyệt.

B4. Văn thư đơn vị:

- Tiếp nhận văn bản đã được Lãnh đạo đơn vị ký số, kiểm tra lại hình thức, thể thức văn bản.
- Trả lại cán bộ soạn thảo để xử lý nếu không đạt yêu cầu.
- Nếu đạt, thực hiện cấp số, ký số tổ chức (đối với đơn vị có con dấu).
- Phát hành bản điện tử đến các cơ quan, tổ chức đủ điều kiện và phát hành bản giấy đối với các cơ quan chưa đủ điều kiện nhận văn bản điện tử.
- Thực hiện công tác lưu trữ (chuyển bản chính giấy hoặc lưu văn bản điện tử) theo quy định của đơn vị có/không có con dấu.