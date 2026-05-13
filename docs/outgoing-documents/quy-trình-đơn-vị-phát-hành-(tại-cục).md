# Luồng quy trình Đơn vị phát hành (Tại Cục)

## 1. Biểu đồ luồng nghiệp vụ (Sequence Diagram)

Biểu đồ tuần tự dưới đây thể hiện chi tiết trách nhiệm, luồng trình duyệt và trả lại (nếu không đạt) giữa các vai trò tham gia trong quy trình phát hành văn bản đi tại Cục.

```mermaid
sequenceDiagram
    autonumber
    actor CV as Chuyên viên
    actor PTP_TT as Phó trưởng phòng<br/>(thuộc trung tâm)
    actor TP_TT as Trưởng phòng<br/>(thuộc trung tâm)
    actor PGĐ_TT as Phó trưởng phòng (không thuộc TT)<br/>/ Phó GĐ trung tâm
    actor GĐ_TT as Trưởng phòng (không thuộc TT)<br/>/ Giám đốc trung tâm
    actor LĐ_HC as Lãnh đạo phòng hành chính
    actor PCT as Phó cục trưởng
    actor CT as Cục trưởng
    actor VT as Văn thư đơn vị

    %% Bước 1: Chuyên viên trình
    CV->>PTP_TT: B1. Trình Lãnh đạo cấp phòng xử lý
    
    %% Bước 2: Phó trưởng phòng (thuộc trung tâm)
    PTP_TT-->>CV: B2. Trả lại hiệu chỉnh (nếu không đạt)
    PTP_TT->>TP_TT: B2. Chuyển hồ sơ trình Trưởng phòng
    PTP_TT->>PGĐ_TT: B2. Hoặc chuyển Lãnh đạo trung tâm (Phó GĐ)
    PTP_TT->>GĐ_TT: B2. Hoặc chuyển Lãnh đạo trung tâm (GĐ)

    %% Bước 3: Trưởng phòng (thuộc trung tâm)
    TP_TT-->>CV: B3. Trả lại hiệu chỉnh (nếu không đạt)
    TP_TT->>PGĐ_TT: B3. Chuyển Lãnh đạo trung tâm (Phó GĐ)
    TP_TT->>GĐ_TT: B3. Hoặc chuyển Lãnh đạo trung tâm (GĐ)

    %% Bước 4: Phó trưởng phòng (không thuộc TT) / Phó GĐ trung tâm
    PGĐ_TT-->>CV: B4. Trả lại hiệu chỉnh (nếu không đạt)
    PGĐ_TT->>GĐ_TT: B4. Chuyển trình Trưởng phòng / Giám đốc TT
    PGĐ_TT->>PCT: B4. Ký nháy, trình Lãnh đạo Cục
    PGĐ_TT->>CT: B4. Ký nháy, trình Lãnh đạo Cục
    PGĐ_TT->>LĐ_HC: B4. Hoặc chuyển trình qua LĐ phòng hành chính

    %% Bước 5: Trưởng phòng (không thuộc TT) / Giám đốc trung tâm
    GĐ_TT-->>CV: B5. Trả lại hiệu chỉnh (nếu không đạt)
    GĐ_TT->>PCT: B5. Ký nháy, trình Lãnh đạo Cục
    GĐ_TT->>CT: B5. Ký nháy, trình Lãnh đạo Cục
    GĐ_TT->>LĐ_HC: B5. Hoặc chuyển trình qua LĐ phòng hành chính

    %% Bước 6: Lãnh đạo phòng hành chính
    LĐ_HC-->>CV: B6. Trả lại hiệu chỉnh (nếu không đạt)
    LĐ_HC->>PCT: B6. Chuyển trình Lãnh đạo Cục
    LĐ_HC->>CT: B6. Chuyển trình Lãnh đạo Cục

    %% Bước 7: Phó cục trưởng
    PCT-->>CV: B7. Trả lại hiệu chỉnh (nếu không đạt)
    PCT->>CT: B7. Trình Cục trưởng (nếu cần CT duyệt)
    PCT->>VT: B7. Ký duyệt & chuyển phát hành

    %% Bước 8: Cục trưởng
    CT-->>CV: B8. Trả lại hiệu chỉnh (nếu không đạt)
    CT->>PCT: B8. Chuyển Phó cục trưởng phê duyệt (nếu PCT ký)
    CT->>VT: B8. Ký duyệt & chuyển phát hành

    %% Bước 9: Văn thư đơn vị
    VT-->>CV: B9. Trả lại xử lý (nếu hình thức, thể thức không đạt)
    VT->>VT: B9. Cấp số, ký số tổ chức (nếu có con dấu)
    VT->>VT: B9. Phát hành bản điện tử / in bản giấy
```

## 2. Mô tả chi tiết nghiệp vụ (Chi tiết theo Role)

B1. Chuyên viên:

- Soạn thảo dự thảo, đính kèm văn bản liên quan, chịu trách nhiệm về hình thức/thể thức.
- Tạo lập hồ sơ điện tử trình Lãnh đạo cấp phòng xử lý (Trưởng phòng hoặc Phó Trưởng phòng).

B2. Phó trưởng phòng (thuộc trung tâm):

- Tiếp nhận rà soát.
- Trả lại Chuyên viên nếu không đạt.
- Nếu đạt, chuyển hồ sơ lên Trưởng phòng hoặc Lãnh đạo trung tâm (Phó giám đốc hoặc Giám đốc trung tâm).

B3. Trưởng phòng (thuộc trung tâm):

- Tiếp nhận rà soát.
- Trả lại Chuyên viên nếu không đạt.
- Nếu đạt, chuyển hồ sơ lên Lãnh đạo trung tâm (Phó giám đốc hoặc Giám đốc trung tâm).

B4. Phó trưởng phòng (không thuộc trung tâm)/Phó giám đốc trung tâm:

- Tiếp nhận hồ sơ.
- Trả lại Chuyên viên nếu không đạt.
- Nếu cần trình cao hơn, chuyển Trưởng phòng/Giám đốc trung tâm.
- Nếu đạt và cần chuyển Lãnh đạo Cục, thực hiện ký nháy và trình Lãnh đạo Cục (Cục trưởng hoặc Phó cục trưởng) hoặc chuyển Lãnh đạo phòng hành chính để trình.

B5. Trưởng phòng (không thuộc trung tâm)/Giám đốc trung tâm:

- Rà soát nội dung.
- Trả lại Chuyên viên nếu không đạt.
- Nếu đạt, thực hiện ký nháy và trình tới Lãnh đạo Cục (Cục trưởng hoặc Phó cục trưởng) hoặc chuyển Lãnh đạo phòng hành chính để trình.

B6. Lãnh đạo phòng hành chính:

- Tiếp nhận dự thảo điện tử, rà soát.
- Trả lại Chuyên viên nếu không đạt.
- Nếu đạt, chuyển trình hồ sơ đến Lãnh đạo Cục (Cục trưởng hoặc Phó cục trưởng).

B7. Phó cục trưởng:

- Rà soát nội dung.
- Trả lại Chuyên viên nếu không đạt.
- Nếu Phó cục trưởng ký duyệt, thực hiện ký và chuyển Văn thư đơn vị.
- Nếu cần Cục trưởng duyệt, trình lên Cục trưởng.

B8. Cục trưởng:

- Rà soát nội dung.
- Trả lại Chuyên viên nếu không đạt.
- Nếu Cục trưởng ký duyệt, thực hiện ký và chuyển Văn thư đơn vị.
- Nếu Phó cục trưởng là người ký, chuyển dự thảo cho Phó cục trưởng phê duyệt.

B9. Văn thư đơn vị:

- Tiếp nhận văn bản có chữ ký số của Lãnh đạo.
- Kiểm tra hình thức, thể thức.
- Trả lại cán bộ soạn thảo nếu không đạt.
- Nếu đạt, thực hiện cấp số, ký số tổ chức (đối với đơn vị có con dấu) và phát hành bản điện tử/bản giấy.
- Xử lý lưu trữ theo nghiệp vụ đơn vị có/không có con dấu.