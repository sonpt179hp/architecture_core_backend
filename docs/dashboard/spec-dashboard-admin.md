# Đặc tả thiết kế Dashboard Admin — Hệ thống QLVB Bộ Tài chính

- **Ngày cập nhật:** 07/05/2026
- **Mục tiêu:** Làm tài liệu tham chiếu cho phân tích nghiệp vụ, thiết kế UI/UX, thiết kế API, phân quyền và thiết kế cơ sở dữ liệu khi triển khai Dashboard Admin cho hệ thống QLVB Bộ Tài chính.

---

# 1. Tổng quan và mục tiêu Dashboard Admin

Dashboard Admin là lớp giao diện web tác nghiệp nội bộ dành cho cán bộ vận hành hệ thống quản lý văn bản của Bộ Tài chính. Tài liệu này tổng quát hóa các luồng nghiệp vụ.
Dashboard Admin cần đáp ứng các mục tiêu sau:

1. Chuẩn hóa luồng xử lý cho các nhóm nghiệp vụ trọng tâm:
   - Văn bản đến
   - Văn bản đi
   - Tờ trình
   - Công việc / phiếu giao việc
   - Quản trị hệ thống
2. Tách rõ quyền xem, quyền thao tác, quyền duyệt, quyền ký và quyền quản trị.
3. Cho phép theo dõi trạng thái xử lý theo vai trò, đơn vị và thời hạn.
4. Lưu vết toàn bộ thao tác phục vụ audit trail, tra soát và giám sát trách nhiệm xử lý.

---

# 2. Phạm vi vai trò

## 2.1. Danh sách vai trò chính

| Vai trò | Mô tả | Phạm vi thao tác chính |
|---|---|---|
| Văn thư Bộ | Tiếp nhận, số hóa, phân luồng, phát hành ở cấp Bộ | Quản lý VB đến/VB đi ở phạm vi Bộ |
| Văn thư đơn vị | Tiếp nhận/chuyển xử lý trong phạm vi đơn vị | Quản lý VB đến/VB đi tại tenant/đơn vị |
| Lãnh đạo | Xem, cho ý kiến, giao việc, duyệt tờ trình, ký số | Điều hành, phê duyệt, giao nhiệm vụ |
| Chuyên viên | Xử lý văn bản, soạn dự thảo, cập nhật tiến độ công việc | Tác nghiệp chuyên môn |
| Quản trị hệ thống | Quản lý tenant, user, role, permission, API mapping | Quản trị cấu hình và phân quyền toàn hệ thống |

## 2.2. Nguyên tắc phân quyền

- Phân quyền theo cả 3 chiều: **vai trò × hành động × phạm vi tenant**.
- Một người dùng có thể mang nhiều vai trò trong cùng tenant hoặc khác tenant.
- Quyền thao tác không chỉ phụ thuộc role mà còn phụ thuộc trạng thái nghiệp vụ của bản ghi.
- Các hành động nhạy cảm như phát hành, ký số, duyệt, từ chối, phân quyền phải được kiểm soát và lưu vết.

## 2.3. Gợi ý mapping vai trò với module

| Module | Văn thư | Lãnh đạo | Chuyên viên | Quản trị hệ thống |
|---|---|---|---|---|
| A1 — Văn bản Đến | Chính | Phối hợp | Chính | Xem cấu hình |
| A2 — Văn bản Đi | Chính | Duyệt/Ký | Chính | Xem cấu hình |
| A3 — Tờ trình | Phối hợp | Chính | Chính | Xem cấu hình |
| A4 — Công việc | Phối hợp | Chính | Chính | Xem cấu hình |
| A5 — Quản trị hệ thống | Không | Giới hạn | Không | Chính |

---

# 3. Kiến trúc chức năng tổng thể Dashboard Admin

## 3.1. Nhóm module

Dashboard Admin gồm 5 module chính:

- **A1 — Văn bản Đến**
- **A2 — Văn bản Đi**
- **A3 — Tờ trình**
- **A4 — Công việc / Phiếu giao việc**
- **A5 — Quản trị hệ thống**

## 3.2. Bố cục giao diện web đề xuất

### 3.2.1. Layout tổng thể

- Sidebar trái: điều hướng module.
- Header trên: tenant hiện tại, thông báo, tìm kiếm nhanh, hồ sơ người dùng.
- Khu vực content: danh sách, form, dashboard widget, tabs nghiệp vụ.
- Panel phải hoặc drawer: chi tiết nhanh, lịch sử, preview file, ghi chú.

### 3.2.2. Các pattern UI dùng chung

| Pattern | Mục đích |
|---|---|
| List page | Danh sách văn bản/công việc/tờ trình |
| Detail page | Xem metadata + file + lịch sử + action |
| Split view | Danh sách bên trái, preview/chi tiết bên phải |
| Wizard modal | Các bước chuyển xử lý, phát hành, đồng trình |
| Approval dialog | Duyệt, yêu cầu chỉnh, từ chối |
| Timeline | Lịch sử xử lý / audit trail |
| Checklist panel | Điều kiện phát hành / điều kiện ký |

---

# 4. Module A1 — Văn bản Đến

## 4.1. Mục tiêu nghiệp vụ

Module Văn bản Đến hỗ trợ tiếp nhận, theo dõi, phân luồng và kiểm soát xử lý văn bản đến trên giao diện web. Đây là module trọng tâm cho Văn thư, Lãnh đạo và Chuyên viên.

## 4.2. Thành phần chức năng

- Danh sách văn bản đến
- Chi tiết văn bản đến
- **Tạo VB đến mới (nhập tay)** — Văn thư nhập thủ công VB giấy chưa có file số
- Chuyển xử lý văn bản đến
- Theo dõi lịch sử xử lý
- Theo dõi trạng thái và quá hạn

## 4.3. Danh sách văn bản đến

### 4.3.1. Mô tả màn hình

Danh sách web cần hỗ trợ nhiều hơn mobile:

- Bảng dữ liệu có phân trang.
- Bộ lọc nhanh theo tab trạng thái.
- Bộ lọc nâng cao theo thời gian, độ khẩn, đơn vị gửi, người xử lý, hạn xử lý.
- Tìm kiếm theo số ký hiệu, trích yếu, nơi gửi, mã hồ sơ.
- Cho phép mở chi tiết ở trang riêng hoặc panel bên phải.

### 4.3.2. Tabs trạng thái

Các tab filter chính:

- Tất cả
- Chưa xử lý
- Đang xử lý
- Đã xử lý
- Quá hạn

### 4.3.3. Cột dữ liệu đề xuất

| Cột | Ý nghĩa |
|---|---|
| Số đến | Số tiếp nhận văn bản đến |
| Số ký hiệu | Số/ký hiệu văn bản gốc |
| Trích yếu | Tóm tắt nội dung |
| Đơn vị gửi | Nơi phát hành văn bản |
| Ngày văn bản | Ngày ban hành |
| Ngày đến | Ngày tiếp nhận |
| Độ khẩn | Thường/Khẩn/Hỏa tốc |
| Mức độ mật | Thường/Mật/Tối mật/Tuyệt mật |
| Người/đơn vị đang xử lý | Chủ thể chịu trách nhiệm hiện tại |
| Hạn xử lý | Deadline |
| Trạng thái | Chưa xử lý/Đang xử lý/Đã xử lý/Quá hạn |
| Cập nhật cuối | Thời điểm cập nhật gần nhất |

## 4.4. Chức năng chi tiết module A1

| Chức năng | Mô tả | Vai trò | Ghi chú |
|---|---|---|---|
| Xem danh sách VB đến | Hiển thị danh sách văn bản đến theo quyền và tenant | Văn thư, Lãnh đạo, Chuyên viên | Có phân trang, sort, filter |
| Lọc theo tab trạng thái | Lọc nhanh theo Tất cả/Chưa xử lý/Đang xử lý/Đã xử lý/Quá hạn | Văn thư, Lãnh đạo, Chuyên viên | Filter mặc định theo vai trò |
| Tìm kiếm nâng cao | Tìm theo số ký hiệu, trích yếu, đơn vị gửi, người xử lý, hạn xử lý | Văn thư, Lãnh đạo, Chuyên viên | Hỗ trợ lưu bộ lọc |
| Xem chi tiết VB đến | Mở màn chi tiết văn bản | Văn thư, Lãnh đạo, Chuyên viên | Có preview file PDF |
| Xem file PDF | Xem nội dung file trên web PDF viewer | Văn thư, Lãnh đạo, Chuyên viên | Có zoom, download nếu được cấp quyền |
| Xem lịch sử xử lý | Hiển thị timeline xử lý toàn bộ | Văn thư, Lãnh đạo, Chuyên viên | Lấy từ audit trail chung |
| Chuyển xử lý | Chuyển văn bản cho cá nhân/đơn vị xử lý tiếp | Văn thư, Lãnh đạo | Wizard 2 bước |
| Ghi ý kiến chỉ đạo | Nhập ý kiến khi chuyển xử lý | Văn thư, Lãnh đạo | Có thể bắt buộc ở một số luồng |
| Từ chối nhận | Văn thư từ chối tiếp nhận VB, bắt buộc nhập lý do | Văn thư | Chỉ khi VB ở trạng thái New/Chưa chuyển |
| Scan bổ sung | Bổ sung file scan cho VB đã tiếp nhận nhưng thiếu file | Văn thư | Upload thêm vào `IncomingDocAttachments` loại Scan |
| Cập nhật kết quả xử lý | Ghi nhận đã xử lý/đang xử lý/kết quả xử lý | Chuyên viên, Lãnh đạo | Theo phân quyền |
| Theo dõi quá hạn | Đánh dấu cảnh báo quá hạn và SLA | Văn thư, Lãnh đạo, Chuyên viên | Màu sắc cảnh báo trên web |
| Thêm vào lịch (Giấy mời) | Nếu VB có loại Giấy mời, hiển thị nút tạo lịch họp từ thông tin VB | Lãnh đạo, Chuyên viên | Prefill tiêu đề, thời gian, địa điểm từ nội dung VB |
| Tiếp nhận VB từ Trục liên thông | VB đến qua Trục liên thông hiển thị badge riêng, luồng tiếp nhận khác VB thường | Văn thư Bộ | Xem mục 4.9 |
| Tạo VB đến mới (nhập tay) | Văn thư nhập thủ công thông tin VB giấy chưa có file số, upload scan sau | Văn thư Bộ, Văn thư đơn vị | SourceType = Manual; xem mục 4.10 |

## 4.5. Màn chi tiết văn bản đến

### 4.5.1. Bố cục đề xuất

Màn chi tiết web nên chia 3 vùng:

1. **Vùng thông tin chính**
   - Số đến, số ký hiệu, ngày đến, độ khẩn, nơi gửi, trích yếu, hạn xử lý.
2. **Vùng preview tài liệu**
   - PDF viewer hoặc danh sách file đính kèm.
3. **Vùng lịch sử và action**
   - Timeline xử lý, danh sách người liên quan, action bar theo quyền.

### 4.5.2. Thông tin cần hiển thị

- Metadata văn bản đến.
- Danh sách file đính kèm.
- Người chuyển gần nhất.
- Người đang chịu trách nhiệm xử lý.
- Hạn xử lý và mức độ quá hạn.
- Lịch sử phân công/chuyển xử lý.
- Ý kiến chỉ đạo và phản hồi xử lý.

## 4.6. Chuyển xử lý văn bản đến

### 4.6.1. Luồng 2 bước

**Bước 1: Chọn người nhận**
- Chọn đơn vị nhận xử lý.
- Chọn cán bộ/người nhận chính.
- Có thể chọn người phối hợp nếu thiết kế mở rộng.
- Chọn hạn xử lý.

**Bước 2: Xác nhận chuyển**
- Xem lại thông tin người nhận.
- Nhập ý kiến/chỉ đạo.
- Xác nhận thao tác.

### 4.6.2. Quy tắc nghiệp vụ

- Không cho chuyển xử lý khi văn bản đã đóng hoặc đã hủy.
- Hạn xử lý không được nhỏ hơn ngày hiện tại nếu không có quyền override.
- Người chuyển phải có quyền trên tenant hiện tại.
- Các lần chuyển phải được ghi log vào bảng lịch sử chung.

## 4.7. Bảng action theo role × trạng thái cho module A1

### 4.7.1. Quy ước trạng thái văn bản đến

- `New` — mới tiếp nhận
- `Assigned` — đã chuyển xử lý
- `InProgress` — đang xử lý
- `Resolved` — đã xử lý xong
- `Overdue` — quá hạn
- `Closed` — kết thúc hồ sơ xử lý

### 4.7.2. Ma trận action

| Vai trò | New | Assigned | InProgress | Resolved | Overdue | Closed |
|---|---|---|---|---|---|---|
| Văn thư | Xem, sửa metadata, chuyển xử lý | Xem, chuyển lại | Xem, theo dõi | Xem, lưu hồ sơ | Xem, nhắc xử lý, chuyển lại | Xem |
| Lãnh đạo | Xem, cho ý kiến, chuyển xử lý | Xem, cho ý kiến | Xem, chỉ đạo, chuyển tiếp | Xem, xác nhận | Xem, chỉ đạo gấp | Xem |
| Chuyên viên | Xem nếu được giao | Xem, nhận xử lý | Xem, cập nhật kết quả | Xem | Xem, cập nhật xử lý | Xem |
| Quản trị hệ thống | Xem cấu hình, tra soát | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình |

## 4.8. Action code gợi ý cho module A1

| Action Code | Ý nghĩa |
|---|---|
| `incomingdoc.view` | Xem danh sách/chi tiết văn bản đến |
| `incomingdoc.filter` | Sử dụng bộ lọc nâng cao |
| `incomingdoc.assign` | Chuyển xử lý văn bản đến |
| `incomingdoc.reassign` | Chuyển lại văn bản đến |
| `incomingdoc.reject` | Từ chối nhận văn bản đến |
| `incomingdoc.scan` | Scan bổ sung file cho văn bản đến |
| `incomingdoc.comment` | Ghi ý kiến xử lý/chỉ đạo |
| `incomingdoc.resolve` | Cập nhật hoàn thành xử lý |
| `incomingdoc.download` | Tải file đính kèm |
| `incomingdoc.audit.view` | Xem lịch sử xử lý |
| `incomingdoc.calendar.add` | Thêm VB Giấy mời vào lịch |

## 4.9. Tích hợp Trục liên thông (VB đến qua Trục)

### 4.9.1. Đặc điểm nhận diện

VB đến từ Trục liên thông quốc gia cần được phân biệt rõ với VB thường:

- Hiển thị badge `Trục LT` riêng biệt trên danh sách và màn chi tiết.
- Trường `SourceType` trong `IncomingDocs` phân biệt: `Manual` (nhập tay) / `TrucLienThong` (từ Trục) / `Scan` (scan giấy).

### 4.9.2. Luồng tiếp nhận VB từ Trục

1. Hệ thống nhận dữ liệu từ Trục liên thông qua API tích hợp.
2. Tự động tạo bản ghi `IncomingDocs` với `SourceType = TrucLienThong`.
3. Văn thư Bộ nhận thông báo và xác nhận tiếp nhận.
4. Sau xác nhận, VB chuyển sang luồng xử lý thông thường.

### 4.9.3. Yêu cầu nghiệp vụ

| Yêu cầu | Mô tả |
|---|---|
| Xác nhận tiếp nhận | Văn thư phải xác nhận đã nhận trước khi chuyển xử lý |
| Gửi phản hồi Trục | Sau xử lý, hệ thống gửi trạng thái xử lý ngược lại Trục nếu cần |
| Không trùng lặp | Hệ thống phải kiểm tra trùng mã VB từ Trục trước khi tạo bản ghi mới |
| Lưu mã Trục | Lưu `ExternalRefCode` (mã định danh phía Trục) để đối soát |

## 4.10. Tạo VB đến mới (nhập tay)

### 4.10.1. Mục tiêu

Cho phép Văn thư nhập thủ công thông tin VB giấy vào hệ thống khi chưa có file số hoặc chưa kết nối Trục liên thông. Đây là luồng phổ biến nhất tại các đơn vị chưa số hóa hoàn toàn.

### 4.10.2. Các trường bắt buộc khi nhập tay

| Trường | Bắt buộc | Ghi chú |
|---|---|---|
| Số ký hiệu văn bản | Có | Số/ký hiệu trên văn bản gốc |
| Ngày ban hành | Có | Ngày trên văn bản gốc |
| Ngày đến | Có | Ngày tiếp nhận thực tế |
| Đơn vị gửi | Có | Tên cơ quan phát hành |
| Trích yếu | Có | Tóm tắt nội dung |
| Độ khẩn | Có | Thường / Khẩn / Hỏa tốc |
| Độ mật | Có | Thường / Mật / Tối mật / Tuyệt mật |
| File đính kèm | Không | Có thể upload sau qua Scan bổ sung |

### 4.10.3. Quy tắc nghiệp vụ

- `SourceType = Manual` để phân biệt với VB từ Trục hoặc Scan.
- Nếu chưa có file, VB vẫn được tạo và chuyển xử lý bình thường.
- Văn thư có thể bổ sung file scan sau bằng chức năng "Scan bổ sung".
- Số ký hiệu phải validate không trùng trong cùng TenantId và năm.

---

# 5. Module A2 — Văn bản Đi

## 5.1. Mục tiêu nghiệp vụ

Module Văn bản Đi hỗ trợ soạn thảo, hoàn thiện, kiểm tra điều kiện và phát hành văn bản đi trên nền web. Web dashboard cần ưu tiên trải nghiệm form lớn, quản lý version, file đính kèm và checklist điều kiện phát hành.

## 5.2. Phạm vi chức năng

- Soạn thảo văn bản đi
- Quản lý dự thảo
- Tự động lưu nháp
- Gợi ý AI hỗ trợ nội dung
- Quản lý file đính kèm
- Kiểm tra điều kiện phát hành
- Thực hiện phát hành chính thức

## 5.3. Chức năng chi tiết module A2

| Chức năng | Mô tả | Vai trò | Ghi chú |
|---|---|---|---|
| Tạo văn bản đi | Tạo mới hồ sơ dự thảo văn bản đi | Chuyên viên, Văn thư | Khởi tạo từ form web |
| Soạn thảo metadata | Nhập số ký hiệu dự kiến, trích yếu, loại văn bản, nơi nhận, độ khẩn | Chuyên viên, Văn thư | Có validate dữ liệu |
| Soạn thảo nội dung | Soạn nội dung dự thảo hoặc liên kết file dự thảo | Chuyên viên | Có thể tích hợp editor hoặc upload file |
| Autosave 30 giây | Tự động lưu bản nháp định kỳ | Chuyên viên | Hiển thị trạng thái autosave |
| Gợi ý AI | AI hỗ trợ tóm tắt/chỉnh cấu trúc/gợi ý nội dung | Chuyên viên | Chỉ là gợi ý, không tự phát hành |
| Đính kèm file | Tải lên file dự thảo, phụ lục, tờ trình, bản cuối | Chuyên viên, Văn thư | Phân loại loại file |
| Xem checklist phát hành | Hiển thị các điều kiện bắt buộc trước khi phát hành | Chuyên viên, Văn thư, Lãnh đạo | Panel kiểm tra điều kiện |
| Trình ký/duyệt | Chuyển hồ sơ sang bước ký số/duyệt | Chuyên viên, Văn thư | Gắn với luồng tờ trình nếu có |
| Phát hành văn bản | Xác nhận phát hành khi đủ điều kiện | Văn thư, Lãnh đạo được ủy quyền | Có log và timestamp |
| Theo dõi trạng thái văn bản đi | Draft/Review/PendingSign/Issued | Văn thư, Lãnh đạo, Chuyên viên | Badge trạng thái |

## 5.4. Màn soạn thảo văn bản đi trên web

### 5.4.1. Khu vực giao diện

- Form metadata bên trái hoặc phía trên.
- Khu vực nội dung/soạn thảo trung tâm.
- Panel file đính kèm.
- Panel checklist phát hành.
- Action bar cố định: Lưu nháp, Trình duyệt, Trình ký, Xem trước, Phát hành.

### 5.4.2. Các nhóm trường dữ liệu chính

| Nhóm trường | Nội dung |
|---|---|
| Thông tin chung | Loại văn bản, trích yếu, độ khẩn, độ mật, đơn vị soạn |
| Phát hành | Số ký hiệu, ngày dự kiến, nơi nhận |
| Nội dung | Nội dung chính hoặc file dự thảo |
| Ký số và duyệt | Người duyệt, người ký, trạng thái ký |
| Tệp đính kèm | Dự thảo, tờ trình duyệt, file bản cuối, phụ lục |

## 5.5. Checklist điều kiện phát hành

Một văn bản đi chỉ được phát hành khi đáp ứng đầy đủ 4 điều kiện bắt buộc:

1. Có **tờ trình duyệt**.
2. Có **lãnh đạo ký số**.
3. Đã **cấp số**.
4. Có **file bản cuối**.

### 5.5.1. Bảng checklist điều kiện phát hành

| Điều kiện | Mô tả | Bắt buộc | Cách kiểm tra |
|---|---|---|---|
| Tờ trình duyệt | Hồ sơ phải gắn ít nhất 1 tờ trình hợp lệ đã hoàn tất luồng duyệt | Có | Kiểm tra liên kết Submissions và trạng thái |
| Lãnh đạo ký số | Người có thẩm quyền đã ký số thành công | Có | Kiểm tra trạng thái ký số |
| Cấp số | Văn bản đã được cấp số phát hành chính thức | Có | Kiểm tra trường số phát hành và timestamp |
| File bản cuối | Có file bản cuối để lưu hồ sơ/phát hành | Có | Kiểm tra attachment loại FinalDocument |

### 5.5.2. Hành vi hệ thống

- Nếu thiếu bất kỳ điều kiện nào, nút Phát hành ở trạng thái disabled hoặc cảnh báo không thể thực hiện.
- Hệ thống phải chỉ rõ điều kiện nào chưa đạt.
- Việc bypass điều kiện chỉ dành cho quyền quản trị đặc biệt nếu có chính sách riêng.

## 5.6. Quy tắc cấp số văn bản đi

Cấp số là nghiệp vụ nhạy cảm về pháp lý — số văn bản phải duy nhất, liên tục, không được bỏ số.

### 5.6.1. Cấu trúc số văn bản đề xuất

```
[Số thứ tự] / [Năm] - [Ký hiệu loại VB] - [Mã đơn vị]
Ví dụ: 123/2026-CV-BTC
```

### 5.6.2. Quy tắc nghiệp vụ

| Quy tắc | Mô tả |
|---|---|
| Ai được cấp số | Chỉ Văn thư Bộ (cho VB cấp Bộ) hoặc Văn thư đơn vị (cho VB nội bộ đơn vị) |
| Tính duy nhất | Số phải unique trong phạm vi TenantId + năm + loại VB |
| Tính liên tục | Số thứ tự tăng dần, không được bỏ số giữa chừng |
| Thời điểm cấp | Chỉ cấp khi VB đã đủ điều kiện phát hành (tờ trình duyệt + ký số) |
| Thu hồi số | Nếu VB bị hủy sau khi đã cấp số, số đó được đánh dấu "đã hủy" — không tái sử dụng |
| Lưu vết | Mọi thao tác cấp số/hủy số phải ghi vào `DocProcessingHistory` |

### 5.6.3. Trường liên quan trong DBML

- `OutgoingDocs.NumberingCode` — mã quy tắc đánh số (cấu hình theo loại VB)
- `OutgoingDocs.IssuedNumber` — số văn bản đã cấp chính thức
- `OutgoingDocs.IssuedDate` — ngày cấp số
- `OutgoingDocs.HasIssuedNumber` — cờ kiểm tra điều kiện phát hành

- `Draft` — đang soạn thảo
- `PendingReview` — chờ duyệt nội bộ
- `PendingSign` — chờ ký số
- `ReadyToIssue` — đủ điều kiện phát hành
- `Issued` — đã phát hành
- `SentToTruc` — đã gửi qua Trục liên thông thành công
- `TrucSendFailed` — gửi Trục thất bại, cần retry
- `Rejected` — bị trả lại/chưa đạt
- `Cancelled` — hủy văn bản

### 5.6.1. Luồng gửi qua Trục liên thông

Sau khi phát hành, nếu VB đi cần gửi qua Trục liên thông:

1. Hệ thống tự động gửi hoặc Văn thư chọn kênh gửi `Trục liên thông`.
2. Hiển thị trạng thái `Đang gửi Trục...` trong khi chờ phản hồi.
3. Nếu thành công: chuyển sang `SentToTruc`, lưu mã xác nhận từ Trục.
4. Nếu thất bại: chuyển sang `TrucSendFailed`, hiển thị lỗi cụ thể và nút **Gửi lại**.
5. Lưu toàn bộ log gửi Trục vào `DocProcessingHistory` với `ActionCode = outgoingdoc.truc.send`.

## 5.7. Bảng action theo role × trạng thái cho module A2

| Vai trò | Draft | PendingReview | PendingSign | ReadyToIssue | Issued | Rejected | Cancelled |
|---|---|---|---|---|---|---|---|
| Văn thư | Xem, cập nhật, đính kèm | Xem, phối hợp | Xem, theo dõi ký | Xem, cấp số, phát hành | Xem, tra cứu | Xem, trả chỉnh | Xem |
| Lãnh đạo | Xem | Xem, cho ý kiến | Xem, ký số/từ chối ký | Xem, xác nhận phát hành nếu được phân quyền | Xem | Xem, yêu cầu chỉnh | Xem |
| Chuyên viên | Tạo, sửa, autosave, AI gợi ý, đính kèm | Xem, cập nhật theo góp ý | Xem | Xem | Xem | Sửa và trình lại | Xem |
| Quản trị hệ thống | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình |

## 5.8. Action code gợi ý cho module A2

| Action Code | Ý nghĩa |
|---|---|
| `outgoingdoc.view` | Xem danh sách/chi tiết văn bản đi |
| `outgoingdoc.create` | Tạo dự thảo văn bản đi |
| `outgoingdoc.update` | Cập nhật dự thảo |
| `outgoingdoc.autosave` | Tự động lưu nháp |
| `outgoingdoc.ai.suggest` | Sử dụng AI gợi ý |
| `outgoingdoc.attach` | Quản lý file đính kèm |
| `outgoingdoc.submit` | Trình duyệt/trình ký |
| `outgoingdoc.numbering` | Cấp số văn bản |
| `outgoingdoc.issue` | Phát hành văn bản |
| `outgoingdoc.sign` | Ký số văn bản |
| `outgoingdoc.truc.send` | Gửi văn bản qua Trục liên thông |
| `outgoingdoc.truc.retry` | Gửi lại văn bản khi lỗi gửi Trục |

---

# 6. Module A3 — Tờ trình

## 6.1. Mục tiêu nghiệp vụ

Module Tờ trình hỗ trợ quản lý toàn bộ quy trình từ khởi tạo đề xuất đến đồng trình, trình lãnh đạo, duyệt, ký số và liên kết phát hành văn bản đi.

## 6.2. Luồng 6 bước

Luồng chuẩn của tờ trình gồm 6 bước:

1. **Tạo**
2. **Đồng trình**
3. **Trình lãnh đạo**
4. **Duyệt**
5. **Ký số**
6. **Phát hành**

Trong thực tế bước 4 và bước 5 có thể cùng gắn với vai trò lãnh đạo, nhưng cần tách logic để quản lý rõ hành vi phê duyệt và hành vi ký số.

## 6.3. Diễn giải chi tiết từng bước

| Bước | Mô tả | Vai trò chính | Kết quả đầu ra |
|---|---|---|---|
| Tạo | Khởi tạo hồ sơ tờ trình, nhập nội dung, căn cứ, đề xuất | Chuyên viên | Bản nháp tờ trình |
| Đồng trình | Gửi xin ý kiến các đơn vị/cá nhân liên quan | Chuyên viên, đơn vị phối hợp | Ý kiến đồng trình |
| Trình lãnh đạo | Gửi hồ sơ cho lãnh đạo xem xét | Chuyên viên, Văn thư | Hồ sơ chờ duyệt |
| Duyệt | Lãnh đạo đưa ra quyết định | Lãnh đạo | Phê duyệt / yêu cầu chỉnh / từ chối |
| Ký số | Ký số hồ sơ/tài liệu bằng SIM PKI | Lãnh đạo | Chữ ký số hợp lệ |
| Phát hành | Hoàn tất hồ sơ, liên kết với văn bản đi nếu có | Văn thư, Chuyên viên theo luồng | Hồ sơ hoàn tất |

## 6.4. Chức năng chi tiết module A3

| Chức năng | Mô tả | Vai trò | Ghi chú |
|---|---|---|---|
| Tạo tờ trình | Tạo hồ sơ tờ trình mới | Chuyên viên | Form web nhiều trường |
| Soạn nội dung tờ trình | Nhập căn cứ, nội dung đề xuất, kiến nghị | Chuyên viên | Có thể dùng rich text hoặc file |
| Đính kèm tài liệu | Gắn file liên quan, phụ lục, hồ sơ nền | Chuyên viên | Có phân loại file |
| Chọn đơn vị đồng trình | Chọn các đơn vị/cá nhân cần cho ý kiến | Chuyên viên | Có deadline phản hồi |
| Tổng hợp ý kiến đồng trình | Thu thập phản hồi từ các bên liên quan | Chuyên viên, Lãnh đạo | Hiển thị dạng bảng/timeline |
| Trình lãnh đạo | Gửi hồ sơ sang bước duyệt | Chuyên viên, Văn thư | Khóa một số trường nếu cần |
| Lãnh đạo duyệt | Phê duyệt hồ sơ | Lãnh đạo | Ghi vào review log |
| Lãnh đạo yêu cầu chỉnh | Trả lại để sửa | Lãnh đạo | Bắt buộc nhập lý do |
| Lãnh đạo từ chối | Từ chối hồ sơ | Lãnh đạo | Bắt buộc nhập lý do |
| Ký số SIM PKI | Ký số trên web bằng SIM PKI | Lãnh đạo | Cần tích hợp dịch vụ ký |
| Kết thúc/phát hành | Hoàn tất luồng tờ trình | Văn thư, Chuyên viên | Có thể liên kết VB đi |

## 6.5. Form tạo tờ trình

### 6.5.1. Nhóm trường đề xuất

| Nhóm trường | Nội dung |
|---|---|
| Thông tin chung | Mã hồ sơ, tiêu đề, loại tờ trình, mức độ ưu tiên |
| Căn cứ | Danh sách văn bản/căn cứ pháp lý liên quan |
| Nội dung đề xuất | Nội dung trình, kiến nghị, phương án |
| Đơn vị phối hợp | Các đơn vị đồng trình, người liên quan |
| Tài liệu đính kèm | Phụ lục, văn bản nền, dự thảo |
| Trình duyệt | Người lãnh đạo phê duyệt/ký số |

## 6.6. Màn duyệt của lãnh đạo

Màn duyệt web cho lãnh đạo cần hiển thị:

- Tóm tắt hồ sơ tờ trình.
- File preview.
- Danh sách ý kiến đồng trình.
- Lịch sử chỉnh sửa quan trọng.
- Nút hành động rõ ràng, ưu tiên cao.

### 6.6.1. Ba action chính của lãnh đạo

| Action | Ý nghĩa | Yêu cầu |
|---|---|---|
| Phê duyệt | Đồng ý cho chuyển bước tiếp theo | Có thể yêu cầu ký ngay hoặc duyệt trước |
| Yêu cầu chỉnh | Trả lại hồ sơ để chỉnh sửa | Bắt buộc nhập lý do/góp ý |
| Từ chối | Không chấp nhận hồ sơ | Bắt buộc nhập lý do tối thiểu 10 ký tự |
| Ủy quyền duyệt | Chuyển quyền duyệt cho người khác | Chọn người được ủy quyền, ghi nhận vào lịch sử; người được ủy quyền thực hiện duyệt thay |

### 6.6.2. Quy tắc ủy quyền duyệt

- Lãnh đạo chọn người được ủy quyền từ danh sách cán bộ có thẩm quyền phù hợp.
- Ghi nhận đầy đủ vào `SubmissionReviews`: `ReviewAction = Delegate`, `DelegatedToId` là người được ủy quyền.
- Người được ủy quyền nhận thông báo và thực hiện duyệt với đầy đủ 3 action (Phê duyệt / Yêu cầu chỉnh / Từ chối).
- Lịch sử hiển thị rõ: "Lãnh đạo A ủy quyền cho B duyệt lúc HH:MM ngày DD/MM/YYYY".

## 6.7. Ký số SIM PKI

### 6.7.1. Mục tiêu

Cho phép lãnh đạo thực hiện ký số trên hồ sơ/tài liệu liên quan trong môi trường web dashboard thông qua tích hợp SIM PKI.

### 6.7.2. Yêu cầu chức năng

- Chọn tài liệu cần ký.
- Hiển thị người ký và trạng thái chứng thư số.
- Gửi yêu cầu ký đến dịch vụ SIM PKI.
- Nhận kết quả ký thành công/thất bại.
- Lưu dấu thời gian ký, serial chứng thư, trạng thái giao dịch.

### 6.7.3. Yêu cầu kiểm soát

- Chỉ người có thẩm quyền mới được kích hoạt ký.
- Không cho sửa nội dung bản ký sau khi ký thành công nếu không tạo version mới.
- Lưu đầy đủ log lỗi ký số để phục vụ tra soát.

## 6.8. Trạng thái đề xuất cho tờ trình

- `Draft`
- `CoReviewing`
- `PendingLeaderReview`
- `Approved`
- `RevisionRequested`
- `Rejected`
- `Signed`
- `Completed`

## 6.9. Bảng action theo role × trạng thái cho module A3

| Vai trò | Draft | CoReviewing | PendingLeaderReview | Approved | RevisionRequested | Rejected | Signed | Completed |
|---|---|---|---|---|---|---|---|---|
| Văn thư | Xem, phối hợp hồ sơ | Xem | Xem, hỗ trợ luân chuyển | Xem | Xem | Xem | Xem | Xem/lưu hồ sơ |
| Lãnh đạo | Xem | Xem | Phê duyệt, yêu cầu chỉnh, từ chối | Ký số | Xem | Xem | Xem | Xem |
| Chuyên viên | Tạo, sửa, đính kèm, chọn đồng trình | Xem, tổng hợp ý kiến | Xem | Xem | Sửa và trình lại | Xem | Xem | Xem |
| Quản trị hệ thống | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình |

## 6.10. Action code gợi ý cho module A3

| Action Code | Ý nghĩa |
|---|---|
| `submission.view` | Xem tờ trình |
| `submission.create` | Tạo tờ trình |
| `submission.update` | Cập nhật tờ trình |
| `submission.coreview` | Gửi/nhận ý kiến đồng trình |
| `submission.submitleader` | Trình lãnh đạo |
| `submission.approve` | Phê duyệt |
| `submission.requestrevision` | Yêu cầu chỉnh |
| `submission.reject` | Từ chối |
| `submission.sign` | Ký số SIM PKI |
| `submission.complete` | Hoàn tất hồ sơ |

---

# 7. Module A4 — Công việc / Phiếu giao việc

## 7.1. Mục tiêu nghiệp vụ

Module Công việc hỗ trợ giao việc, theo dõi tiến độ, phân rã sub-task và ghi nhận log cập nhật phục vụ điều hành.

## 7.2. Góc nhìn theo vai trò

### 7.2.1. Lãnh đạo

- Tab **Tôi giao**
- Tab **Của đơn vị**

### 7.2.2. Chuyên viên

- Tab **Được giao tôi**
- Tab **Tôi giao người khác**

## 7.3. Thành phần chức năng

- Danh sách phiếu giao việc
- Chi tiết công việc
- Quản lý công việc con
- Cập nhật phần trăm tiến độ
- Nhật ký cập nhật tiến độ

## 7.4. Chức năng chi tiết module A4

| Chức năng | Mô tả | Vai trò | Ghi chú |
|---|---|---|---|
| Xem danh sách công việc | Hiển thị danh sách theo tab vai trò | Lãnh đạo, Chuyên viên, Văn thư | Có filter trạng thái/hạn xử lý |
| Tạo phiếu giao việc | Tạo mới nhiệm vụ và giao người thực hiện | Lãnh đạo, Chuyên viên được phân quyền | Có chọn đơn vị/cá nhân thực hiện |
| Xem chi tiết công việc | Xem nội dung, người giao, người nhận, deadline, tiến độ | Lãnh đạo, Chuyên viên, Văn thư | Có timeline |
| Tạo sub-task | Phân rã công việc thành các đầu việc nhỏ | Lãnh đạo, Chuyên viên phụ trách | Có thể gán người xử lý riêng |
| Cập nhật tiến độ | Cập nhật % hoàn thành, nội dung cập nhật | Người được giao, người phụ trách | Có log từng lần cập nhật |
| Ghi nhận khó khăn/vướng mắc | Thêm note trong log tiến độ | Người được giao | Hỗ trợ lãnh đạo theo dõi |
| Đính kèm kết quả | Chuyên viên đính kèm file kết quả khi báo hoàn thành | Chuyên viên | Lưu vào `TaskAttachments` |
| Đánh giá kết quả | Lãnh đạo đánh giá chất lượng kết quả sau khi CV hoàn thành | Lãnh đạo tạo CV | Nhập nhận xét + mức đánh giá (Đạt/Không đạt/Xuất sắc) |
| Theo dõi quá hạn | Cảnh báo công việc và sub-task quá hạn | Lãnh đạo, Chuyên viên | Hiển thị badge và màu cảnh báo |
| Đóng công việc | Xác nhận hoàn thành công việc | Người giao/Người có quyền | Có thể yêu cầu nghiệm thu |

## 7.5. Danh sách công việc trên web

### 7.5.1. Cột hiển thị đề xuất

| Cột | Ý nghĩa |
|---|---|
| Mã công việc | Mã định danh phiếu giao việc |
| Tiêu đề | Tên công việc |
| Người giao | Người tạo/giao việc |
| Người nhận chính | Người chịu trách nhiệm chính |
| Đơn vị phụ trách | Đơn vị thực hiện |
| Hạn xử lý | Deadline |
| Tiến độ | % hoàn thành |
| Trạng thái | New/InProgress/Completed/Overdue/Closed |
| Cập nhật cuối | Lần cập nhật tiến độ mới nhất |

## 7.6. Màn chi tiết công việc

Màn chi tiết web cần gồm:

- Tóm tắt công việc.
- Danh sách sub-task.
- Timeline cập nhật tiến độ.
- File đính kèm nếu có.
- Action bar: giao tiếp, cập nhật tiến độ, tạo sub-task, đóng việc.

## 7.7. Sub-task

### 7.7.1. Yêu cầu nghiệp vụ

- Một công việc cha có thể có nhiều công việc con.
- Mỗi sub-task có người phụ trách, hạn xử lý, trạng thái và % tiến độ riêng.
- Tiến độ công việc cha có thể được nhập tay hoặc tính toán theo sub-task tùy cấu hình.

## 7.8. Cập nhật tiến độ

### 7.8.1. Dữ liệu cập nhật

Mỗi lần cập nhật tiến độ cần lưu:

- % tiến độ trước và sau cập nhật.
- Nội dung cập nhật.
- Vướng mắc/rủi ro.
- Người cập nhật.
- Thời điểm cập nhật.

## 7.9. Trạng thái đề xuất cho công việc

- `New`
- `InProgress`
- `PendingReview`
- `Completed`
- `Overdue`
- `Closed`
- `Cancelled`

## 7.10. Bảng action theo role × trạng thái cho module A4

| Vai trò | New | InProgress | PendingReview | Completed | Overdue | Closed | Cancelled |
|---|---|---|---|---|---|---|---|
| Văn thư | Xem nếu liên quan | Xem | Xem | Xem | Xem | Xem | Xem |
| Lãnh đạo | Tạo/giao việc, xem | Xem, chỉ đạo, tạo sub-task | Xem, duyệt hoàn thành | Xem, đóng việc | Xem, nhắc xử lý | Xem | Xem |
| Chuyên viên | Xem nếu được giao | Cập nhật tiến độ, tạo sub-task phụ nếu được quyền | Báo cáo kết quả | Xem | Cập nhật, giải trình | Xem | Xem |
| Quản trị hệ thống | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình | Xem cấu hình |

## 7.11. Action code gợi ý cho module A4

| Action Code | Ý nghĩa |
|---|---|
| `task.view` | Xem công việc |
| `task.create` | Tạo phiếu giao việc |
| `task.assign` | Giao việc |
| `task.subtask.create` | Tạo công việc con |
| `task.progress.update` | Cập nhật tiến độ |
| `task.review` | Duyệt/đóng công việc |
| `task.overdue.manage` | Xử lý công việc quá hạn |

---

# 8. Module A5 — Quản trị hệ thống

## 8.1. Mục tiêu nghiệp vụ

Module Quản trị hệ thống cung cấp giao diện web cho quản trị mô hình multi-tenant, tài khoản người dùng, vai trò, quyền và ánh xạ quyền API.

## 8.2. Phạm vi quản trị

- Quản lý tenant
- Quản lý user/staff
- Quản lý role
- Quản lý permission/action
- Quản lý API mapping
- Theo dõi tình trạng cấu hình quyền

## 8.3. Chức năng chi tiết module A5

| Chức năng | Mô tả | Vai trò | Ghi chú |
|---|---|---|---|
| Quản lý tenant | Tạo/sửa/kích hoạt/vô hiệu hóa tenant, quan hệ cha-con | Quản trị hệ thống | Theo mô hình hierarchical multi-tenant |
| Tạo tài khoản user | Tạo tài khoản mới, gán tenant, gán role ban đầu | Quản trị hệ thống | Wizard 3 bước: Thông tin → Gán tenant → Gán role |
| Khóa/mở khóa tài khoản | Tạm khóa hoặc mở khóa tài khoản người dùng | Quản trị hệ thống | Bắt buộc nhập lý do khi khóa |
| Reset mật khẩu | Đặt lại mật khẩu cho người dùng | Quản trị hệ thống | Gửi link reset qua email hoặc cấp mật khẩu tạm |
| Gán role cho user | Gán/bỏ role theo tenant/phạm vi | Quản trị hệ thống | Hỗ trợ nhiều role trên nhiều tenant |
| Quản lý role | Tạo/sửa role nghiệp vụ, xem danh sách action đã gán | Quản trị hệ thống | Có thể theo tenant hoặc global |
| Quản lý permission/action | Gán action cho role, xem ma trận quyền | Quản trị hệ thống | Dùng cho UI và API |
| API mapping | Map API endpoint với action yêu cầu | Quản trị hệ thống | Phục vụ kiểm soát backend |
| Xem tình trạng cấu hình | Phát hiện user chưa có role, role chưa có action, API chưa map | Quản trị hệ thống, Lãnh đạo được cấp quyền | Dashboard health |
| Xem audit log quản trị | Xem lịch sử các thao tác quản trị: ai gán quyền gì, lúc nào | Quản trị hệ thống | Lấy từ `DocProcessingHistory` với EntityType = AdminAction |

## 8.4. Luồng onboard user mới

```
Bước 1 — Tạo tài khoản
  Nhập: Họ tên, email, số điện thoại, đơn vị công tác
  Validate: email chưa tồn tại trong hệ thống

Bước 2 — Gán tenant
  Chọn tenant chính (đơn vị trực thuộc)
  Có thể gán thêm tenant phụ nếu cần

Bước 3 — Gán role
  Chọn role theo từng tenant đã gán
  Xem trước danh sách quyền sẽ có

Xác nhận → Gửi email kích hoạt tài khoản
```

## 8.5. Nhóm màn hình quản trị

| Màn hình | Mục đích |
|---|---|
| Tenant Management | Quản lý cấu trúc Bộ/đơn vị |
| User Management | Quản lý người dùng nội bộ |
| Role Management | Quản lý vai trò nghiệp vụ |
| Permission Mapping | Quản lý action theo role |
| API Mapping | Ánh xạ endpoint với action |
| Configuration Health | Kiểm tra sai cấu hình phổ biến |

## 8.5. Bảng action theo role × trạng thái cho module A5

Trong module này, trạng thái thường là trạng thái cấu hình hơn là trạng thái hồ sơ nghiệp vụ.

| Vai trò | Xem cấu hình | Tạo mới | Cập nhật | Khóa/Mở khóa | Gán quyền | Map API |
|---|---|---|---|---|---|---|
| Văn thư | Không | Không | Không | Không | Không | Không |
| Lãnh đạo | Giới hạn theo báo cáo | Không | Không | Không | Không | Không |
| Chuyên viên | Không | Không | Không | Không | Không | Không |
| Quản trị hệ thống | Có | Có | Có | Có | Có | Có |

## 8.6. Action code gợi ý cho module A5

| Action Code | Ý nghĩa |
|---|---|
| `admin.tenant.view` | Xem tenant |
| `admin.tenant.manage` | Quản lý tenant |
| `admin.staff.view` | Xem user/staff |
| `admin.staff.manage` | Quản lý user/staff |
| `admin.role.view` | Xem role |
| `admin.role.manage` | Quản lý role |
| `admin.permission.manage` | Gán permission/action |
| `admin.api.mapping.manage` | Quản lý API mapping |
| `admin.config.health.view` | Xem tình trạng cấu hình |

---

# 9. Module dùng chung bắt buộc trước Sprint 1

## 9.1. Notification Center

Hệ thống QLVB không thể vận hành hiệu quả nếu không có thông báo. Đây là module dùng chung phục vụ tất cả nghiệp vụ.

### 9.1.1. Các loại thông báo bắt buộc

| Sự kiện | Người nhận | Kênh |
|---|---|---|
| VB đến được chuyển xử lý | Người nhận xử lý | In-app + email (nếu cấu hình) |
| Tờ trình chờ duyệt | Lãnh đạo | In-app + email |
| Tờ trình bị yêu cầu chỉnh / từ chối | Chuyên viên, Văn thư liên quan | In-app + email |
| Công việc sắp đến hạn / quá hạn | Người thực hiện + người giao | In-app |
| VB đi chờ ký số | Lãnh đạo ký | In-app |
| Cảnh báo cấu hình phân quyền | Quản trị hệ thống | In-app |

### 9.1.2. Yêu cầu tối thiểu Sprint 1

- Bell icon thông báo trong header.
- Danh sách thông báo chưa đọc / đã đọc.
- Đánh dấu đã xem.
- Click thông báo → điều hướng tới màn hình chi tiết liên quan.

## 9.2. Danh mục dùng chung

Các màn hình form và dashboard đều cần dữ liệu danh mục nền. Nếu không có danh mục dùng chung, dev không thể triển khai dropdown và rule tính SLA.

### 9.2.1. Danh mục tối thiểu

| Danh mục | Phục vụ |
|---|---|
| Loại văn bản | VB đến / VB đi |
| Loại tờ trình | Tờ trình |
| Độ khẩn | VB đến / VB đi |
| Độ mật | VB đến / VB đi |
| Đơn vị gửi/nhận | VB đến / VB đi |
| Quy tắc đánh số | Cấp số VB đi |
| Cấu hình SLA theo loại VB | Tính quá hạn |

### 9.2.2. Yêu cầu triển khai

- Danh mục được quản lý tập trung ở module Quản trị hoặc cấu hình chung.
- Các module A1/A2/A3/A4 chỉ đọc danh mục qua API dùng chung.
- Dashboard chỉ hiển thị text đã resolve từ mã danh mục, không hard-code text.

---

# 10. Yêu cầu SLA và hạn xử lý chung

## 10.1. Mục tiêu

Hệ thống cần theo dõi hạn xử lý xuyên suốt cho VB đến, tờ trình và công việc để phục vụ điều hành, cảnh báo quá hạn và nhắc việc tự động.

## 10.2. Quy tắc đề xuất

| Đối tượng | Cách xác định hạn xử lý | Ghi chú |
|---|---|---|
| Văn bản đến | Theo ngày được giao xử lý + số ngày SLA theo loại VB/độ khẩn | Hỏa tốc có SLA ngắn hơn VB thường |
| Tờ trình | Theo ngày trình lãnh đạo + SLA duyệt nội bộ | Có thể cấu hình khác nhau theo loại tờ trình |
| Công việc | Theo deadline do người giao nhập | Có thể chỉnh sửa bởi lãnh đạo |

## 10.3. Quy tắc chuyển trạng thái quá hạn

- Job nền chạy định kỳ kiểm tra các bản ghi chưa hoàn tất nhưng đã quá `DueDate`.
- Nếu quá hạn, hệ thống tự động chuyển sang trạng thái `Overdue` hoặc gắn cờ quá hạn.
- Tất cả thay đổi phải ghi vào `DocProcessingHistory`.

## 10.4. Cảnh báo và nhắc việc

- Gửi thông báo trước hạn N giờ/N ngày theo cấu hình.
- Gửi cảnh báo ngay khi quá hạn.
- Dashboard phải có bộ lọc `Sắp đến hạn` và `Quá hạn`.

---

# 11. Yêu cầu audit trail và lịch sử xử lý chung

## 11.1. Mục tiêu

Tất cả các module nghiệp vụ cần có khả năng lưu vết lịch sử xử lý tập trung để:

- Tra soát ai đã làm gì, khi nào.
- Theo dõi luồng xử lý thực tế.
- Hỗ trợ báo cáo kiểm tra nội bộ.
- Hỗ trợ giải trình khi có tranh chấp nghiệp vụ.

## 11.2. Các loại sự kiện cần lưu

| Nhóm sự kiện | Ví dụ |
|---|---|
| Tiếp nhận | Tạo mới văn bản đến/văn bản đi/tờ trình/công việc |
| Chuyển bước | Chuyển xử lý, trình duyệt, chuyển đơn vị |
| Phản hồi | Góp ý, yêu cầu chỉnh, từ chối |
| Phê duyệt | Duyệt hồ sơ, xác nhận hoàn thành |
| Ký số | Ký thành công/thất bại |
| Phát hành | Cấp số, phát hành văn bản |
| Tiến độ | Cập nhật % tiến độ công việc |
| Quản trị | Gán role, đổi quyền, khóa user |

---

# 12. Yêu cầu phi chức năng chính

## 12.1. Hiệu năng

- Danh sách phải hỗ trợ phân trang server-side.
- Các bộ lọc phổ biến phải có chỉ mục dữ liệu phù hợp.
- Preview PDF cần tối ưu cho tài liệu nhiều trang.

## 12.2. Bảo mật

- Mọi API thao tác phải kiểm tra action permission.
- Dữ liệu phải được giới hạn theo tenant và phạm vi vai trò.
- File mật/chữ ký số phải kiểm soát quyền tải xuống.

## 12.3. Theo dõi và giám sát

- Các action quan trọng phải có log.
- Các trạng thái lỗi ký số/phát hành phải có thông báo rõ ràng.
- Hỗ trợ truy vết theo mã hồ sơ, người thực hiện, khoảng thời gian.

---

# 13. Database DBML cho toàn bộ Dashboard Admin

DBML dưới đây tuân theo các nguyên tắc của project:

- PK dùng `uniqueidentifier`
- Có trường audit: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- Có soft delete: `IsDeleted`, `DeletedAt`, `DeletedBy`
- Có `TenantId`
- FK người dùng tham chiếu `Staffs`
- Tên bảng và cột dùng PascalCase

```dbml
Project dashboard_admin_spec {
  database_type: "SQLServer"
  Note: '''
    Dashboard Admin schema for QLVB Bo Tai Chinh.
    Covers Incoming Docs, Outgoing Docs, Submissions, Tasks and common processing history.
    Last updated: 2026-05-07
  '''
}

Table IncomingDocs [headercolor: #1E88E5] {
  IncomingDocId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  SourceType varchar(30) [not null, default: 'Manual', note: 'Manual/TrucLienThong/Scan']
  ExternalRefCode nvarchar(100) [null, note: 'Mã định danh từ Trục liên thông nếu có']
  DocNumber nvarchar(100) [null, note: 'Số đến nội bộ']
  ExternalDocNumber nvarchar(100) [null, note: 'Số/ký hiệu văn bản gốc']
  Title nvarchar(500) [not null]
  Summary nvarchar(max) [null]
  SenderOrgName nvarchar(255) [null]
  ReceivedDate datetime [not null]
  IssuedDate datetime [null]
  DueDate datetime [null]
  PriorityLevel tinyint [not null, default: 1, note: '1=Thuong,2=Khan,3=HoaToc']
  ConfidentialityLevel tinyint [not null, default: 1, note: '1=Thuong,2=Mat,3=ToiMat,4=TuyetMat']
  Status tinyint [not null, default: 1, note: '1=New,2=Assigned,3=InProgress,4=Resolved,5=Overdue,6=Closed']
  CurrentAssigneeId uniqueidentifier [null]
  CurrentAssigneeTenantId uniqueidentifier [null]
  MainFileUrl nvarchar(1000) [null]
  Notes nvarchar(max) [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, Status, ReceivedDate) [name: 'IX_IncomingDocs_Tenant_Status_ReceivedDate']
    (TenantId, DueDate) [name: 'IX_IncomingDocs_Tenant_DueDate']
    (CurrentAssigneeId) [name: 'IX_IncomingDocs_CurrentAssigneeId']
    (ExternalDocNumber) [name: 'IX_IncomingDocs_ExternalDocNumber']
    (SourceType) [name: 'IX_IncomingDocs_SourceType']
    (ExternalRefCode) [name: 'IX_IncomingDocs_ExternalRefCode']
  }
}

Table IncomingDocAttachments [headercolor: #42A5F5] {
  IncomingDocAttachmentId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  IncomingDocId uniqueidentifier [not null]
  FileName nvarchar(255) [not null]
  FileUrl nvarchar(1000) [not null]
  FileType nvarchar(100) [null]
  FileSize bigint [null]
  AttachmentType tinyint [not null, default: 1, note: '1=Main,2=Scan,3=Appendix,4=Other']
  SortOrder int [not null, default: 0]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (IncomingDocId, AttachmentType, SortOrder) [name: 'IX_IncomingDocAttachments_Doc_AttachmentType_SortOrder']
  }
}

Table IncomingDocAssignments [headercolor: #90CAF9] {
  IncomingDocAssignmentId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  IncomingDocId uniqueidentifier [not null]
  FromStaffId uniqueidentifier [not null]
  ToStaffId uniqueidentifier [not null]
  ToTenantId uniqueidentifier [null]
  AssignedDate datetime [not null]
  DueDate datetime [null]
  Comment nvarchar(max) [null]
  AssignmentStatus tinyint [not null, default: 1, note: '1=Assigned,2=Accepted,3=InProgress,4=Completed,5=Rejected,6=Cancelled']
  IsCurrent bit [not null, default: 1]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (IncomingDocId, AssignedDate) [name: 'IX_IncomingDocAssignments_Doc_AssignedDate']
    (ToStaffId, AssignmentStatus) [name: 'IX_IncomingDocAssignments_ToStaff_Status']
    (IsCurrent) [name: 'IX_IncomingDocAssignments_IsCurrent']
  }
}

Table OutgoingDocs [headercolor: #43A047] {
  OutgoingDocId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  DocCode nvarchar(100) [null]
  Title nvarchar(500) [not null]
  Summary nvarchar(max) [null]
  DocType nvarchar(100) [null]
  DraftContent nvarchar(max) [null]
  PriorityLevel tinyint [not null, default: 1]
  ConfidentialityLevel tinyint [not null, default: 1]
  NumberingCode nvarchar(100) [null]
  IssuedNumber nvarchar(100) [null]
  IssuedDate datetime [null]
  DrafterId uniqueidentifier [null]
  ReviewerId uniqueidentifier [null]
  SignerId uniqueidentifier [null]
  HasSubmission bit [not null, default: 0]
  HasDigitalSignature bit [not null, default: 0]
  HasIssuedNumber bit [not null, default: 0]
  HasFinalFile bit [not null, default: 0]
  Status tinyint [not null, default: 1, note: '1=Draft,2=PendingReview,3=PendingSign,4=ReadyToIssue,5=Issued,6=SentToTruc,7=TrucSendFailed,8=Rejected,9=Cancelled']
  LastAutoSavedAt datetime [null]
  FinalFileUrl nvarchar(1000) [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, Status, UpdatedAt) [name: 'IX_OutgoingDocs_Tenant_Status_UpdatedAt']
    (SignerId, Status) [name: 'IX_OutgoingDocs_SignerId_Status']
    (IssuedNumber) [name: 'IX_OutgoingDocs_IssuedNumber']
  }
}

Table OutgoingDocAttachments [headercolor: #81C784] {
  OutgoingDocAttachmentId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  OutgoingDocId uniqueidentifier [not null]
  FileName nvarchar(255) [not null]
  FileUrl nvarchar(1000) [not null]
  FileType nvarchar(100) [null]
  FileSize bigint [null]
  AttachmentType tinyint [not null, default: 1, note: '1=Draft,2=Submission,3=Appendix,4=FinalDocument,5=Other']
  SortOrder int [not null, default: 0]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (OutgoingDocId, AttachmentType, SortOrder) [name: 'IX_OutgoingDocAttachments_Doc_AttachmentType_SortOrder']
  }
}

Table Submissions [headercolor: #FB8C00] {
  SubmissionId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  OutgoingDocId uniqueidentifier [null]
  SubmissionCode nvarchar(100) [null]
  Title nvarchar(500) [not null]
  Summary nvarchar(max) [null]
  Content nvarchar(max) [null]
  SubmissionType nvarchar(100) [null]
  PriorityLevel tinyint [not null, default: 1]
  Status tinyint [not null, default: 1, note: '1=Draft,2=CoReviewing,3=PendingLeaderReview,4=Approved,5=RevisionRequested,6=Rejected,7=Signed,8=Completed']
  SubmitterId uniqueidentifier [not null]
  LeaderId uniqueidentifier [null]
  SignedAt datetime [null]
  CompletedAt datetime [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, Status, UpdatedAt) [name: 'IX_Submissions_Tenant_Status_UpdatedAt']
    (OutgoingDocId) [name: 'IX_Submissions_OutgoingDocId']
    (LeaderId, Status) [name: 'IX_Submissions_LeaderId_Status']
  }
}

Table SubmissionReviews [headercolor: #FFB74D] {
  SubmissionReviewId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  SubmissionId uniqueidentifier [not null]
  ReviewerId uniqueidentifier [not null]
  ReviewAction tinyint [not null, note: '1=Approve,2=RequestRevision,3=Reject,4=Delegate']
  ReviewComment nvarchar(max) [null]
  DelegatedToId uniqueidentifier [null, note: 'Người được ủy quyền duyệt thay, chỉ có giá trị khi ReviewAction=4']
  ReviewedAt datetime [not null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (SubmissionId, ReviewedAt) [name: 'IX_SubmissionReviews_Submission_ReviewedAt']
    (ReviewerId) [name: 'IX_SubmissionReviews_ReviewerId']
    (DelegatedToId) [name: 'IX_SubmissionReviews_DelegatedToId']
  }
}

Table SubmissionCoReviews [headercolor: #FFD180] {
  SubmissionCoReviewId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  SubmissionId uniqueidentifier [not null]
  ReviewerTenantId uniqueidentifier [null]
  ReviewerId uniqueidentifier [null]
  ReviewStatus tinyint [not null, default: 1, note: '1=Pending,2=Responded,3=Approved,4=RevisionRequested,5=Rejected']
  ReviewComment nvarchar(max) [null]
  ReviewedAt datetime [null]
  DueDate datetime [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (SubmissionId, ReviewStatus) [name: 'IX_SubmissionCoReviews_Submission_ReviewStatus']
    (ReviewerId) [name: 'IX_SubmissionCoReviews_ReviewerId']
    (ReviewerTenantId) [name: 'IX_SubmissionCoReviews_ReviewerTenantId']
  }
}

Table Tasks [headercolor: #8E24AA] {
  TaskId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  TaskCode nvarchar(100) [null]
  Title nvarchar(500) [not null]
  Description nvarchar(max) [null]
  ParentTaskId uniqueidentifier [null]
  AssignedById uniqueidentifier [not null]
  AssignedToId uniqueidentifier [not null]
  AssignedToTenantId uniqueidentifier [null]
  StartDate datetime [null]
  DueDate datetime [null]
  ProgressPercent decimal(5,2) [not null, default: 0]
  Status tinyint [not null, default: 1, note: '1=New,2=InProgress,3=PendingReview,4=Completed,5=Overdue,6=Closed,7=Cancelled']
  PriorityLevel tinyint [not null, default: 1]
  ResultSummary nvarchar(max) [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, Status, DueDate) [name: 'IX_Tasks_Tenant_Status_DueDate']
    (AssignedToId, Status) [name: 'IX_Tasks_AssignedToId_Status']
    (AssignedById) [name: 'IX_Tasks_AssignedById']
    (ParentTaskId) [name: 'IX_Tasks_ParentTaskId']
  }
}

Table SubTasks [headercolor: #BA68C8] {
  SubTaskId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  TaskId uniqueidentifier [not null]
  Title nvarchar(500) [not null]
  Description nvarchar(max) [null]
  AssignedToId uniqueidentifier [null]
  StartDate datetime [null]
  DueDate datetime [null]
  ProgressPercent decimal(5,2) [not null, default: 0]
  Status tinyint [not null, default: 1, note: '1=New,2=InProgress,3=Completed,4=Overdue,5=Closed,6=Cancelled']
  SortOrder int [not null, default: 0]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TaskId, SortOrder) [name: 'IX_SubTasks_TaskId_SortOrder']
    (AssignedToId, Status) [name: 'IX_SubTasks_AssignedToId_Status']
  }
}

Table TaskProgressLogs [headercolor: #CE93D8] {
  TaskProgressLogId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  TaskId uniqueidentifier [not null]
  SubTaskId uniqueidentifier [null]
  PreviousProgressPercent decimal(5,2) [null]
  CurrentProgressPercent decimal(5,2) [not null]
  UpdateComment nvarchar(max) [null]
  Blockers nvarchar(max) [null]
  LoggedAt datetime [not null]
  LoggedById uniqueidentifier [not null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TaskId, LoggedAt) [name: 'IX_TaskProgressLogs_TaskId_LoggedAt']
    (SubTaskId, LoggedAt) [name: 'IX_TaskProgressLogs_SubTaskId_LoggedAt']
    (LoggedById) [name: 'IX_TaskProgressLogs_LoggedById']
  }
}

Table DocProcessingHistory [headercolor: #546E7A] {
  DocProcessingHistoryId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  EntityType nvarchar(100) [not null, note: 'IncomingDoc, OutgoingDoc, Submission, Task, SubTask, AdminAction']
  EntityId uniqueidentifier [not null]
  ActionCode nvarchar(150) [not null]
  ActionName nvarchar(255) [not null]
  FromStatus tinyint [null]
  ToStatus tinyint [null]
  PerformedById uniqueidentifier [not null]
  PerformedForId uniqueidentifier [null]
  Comment nvarchar(max) [null]
  MetadataJson nvarchar(max) [null]
  ProcessedAt datetime [not null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (EntityType, EntityId, ProcessedAt) [name: 'IX_DocProcessingHistory_EntityType_EntityId_ProcessedAt']
    (PerformedById, ProcessedAt) [name: 'IX_DocProcessingHistory_PerformedById_ProcessedAt']
    (ActionCode) [name: 'IX_DocProcessingHistory_ActionCode']
  }
}

Ref: IncomingDocs.TenantId > Tenants.TenantId [delete: restrict]
Ref: IncomingDocs.CurrentAssigneeId > Staffs.StaffId [delete: set null]
Ref: IncomingDocs.CurrentAssigneeTenantId > Tenants.TenantId [delete: set null]
Ref: IncomingDocs.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: IncomingDocs.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: IncomingDocs.DeletedBy > Staffs.StaffId [delete: set null]

Ref: IncomingDocAttachments.TenantId > Tenants.TenantId [delete: restrict]
Ref: IncomingDocAttachments.IncomingDocId > IncomingDocs.IncomingDocId [delete: restrict]
Ref: IncomingDocAttachments.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: IncomingDocAttachments.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: IncomingDocAttachments.DeletedBy > Staffs.StaffId [delete: set null]

Ref: IncomingDocAssignments.TenantId > Tenants.TenantId [delete: restrict]
Ref: IncomingDocAssignments.IncomingDocId > IncomingDocs.IncomingDocId [delete: restrict]
Ref: IncomingDocAssignments.FromStaffId > Staffs.StaffId [delete: restrict]
Ref: IncomingDocAssignments.ToStaffId > Staffs.StaffId [delete: restrict]
Ref: IncomingDocAssignments.ToTenantId > Tenants.TenantId [delete: set null]
Ref: IncomingDocAssignments.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: IncomingDocAssignments.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: IncomingDocAssignments.DeletedBy > Staffs.StaffId [delete: set null]

Ref: OutgoingDocs.TenantId > Tenants.TenantId [delete: restrict]
Ref: OutgoingDocs.DrafterId > Staffs.StaffId [delete: set null]
Ref: OutgoingDocs.ReviewerId > Staffs.StaffId [delete: set null]
Ref: OutgoingDocs.SignerId > Staffs.StaffId [delete: set null]
Ref: OutgoingDocs.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: OutgoingDocs.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: OutgoingDocs.DeletedBy > Staffs.StaffId [delete: set null]

Ref: OutgoingDocAttachments.TenantId > Tenants.TenantId [delete: restrict]
Ref: OutgoingDocAttachments.OutgoingDocId > OutgoingDocs.OutgoingDocId [delete: restrict]
Ref: OutgoingDocAttachments.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: OutgoingDocAttachments.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: OutgoingDocAttachments.DeletedBy > Staffs.StaffId [delete: set null]

Ref: Submissions.TenantId > Tenants.TenantId [delete: restrict]
Ref: Submissions.OutgoingDocId > OutgoingDocs.OutgoingDocId [delete: set null]
Ref: Submissions.SubmitterId > Staffs.StaffId [delete: restrict]
Ref: Submissions.LeaderId > Staffs.StaffId [delete: set null]
Ref: Submissions.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: Submissions.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: Submissions.DeletedBy > Staffs.StaffId [delete: set null]

Ref: SubmissionReviews.TenantId > Tenants.TenantId [delete: restrict]
Ref: SubmissionReviews.SubmissionId > Submissions.SubmissionId [delete: restrict]
Ref: SubmissionReviews.ReviewerId > Staffs.StaffId [delete: restrict]
Ref: SubmissionReviews.DelegatedToId > Staffs.StaffId [delete: set null]
Ref: SubmissionReviews.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: SubmissionReviews.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: SubmissionReviews.DeletedBy > Staffs.StaffId [delete: set null]

Ref: SubmissionCoReviews.TenantId > Tenants.TenantId [delete: restrict]
Ref: SubmissionCoReviews.SubmissionId > Submissions.SubmissionId [delete: restrict]
Ref: SubmissionCoReviews.ReviewerTenantId > Tenants.TenantId [delete: set null]
Ref: SubmissionCoReviews.ReviewerId > Staffs.StaffId [delete: set null]
Ref: SubmissionCoReviews.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: SubmissionCoReviews.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: SubmissionCoReviews.DeletedBy > Staffs.StaffId [delete: set null]

Ref: Tasks.TenantId > Tenants.TenantId [delete: restrict]
Ref: Tasks.ParentTaskId > Tasks.TaskId [delete: set null]
Ref: Tasks.AssignedById > Staffs.StaffId [delete: restrict]
Ref: Tasks.AssignedToId > Staffs.StaffId [delete: restrict]
Ref: Tasks.AssignedToTenantId > Tenants.TenantId [delete: set null]
Ref: Tasks.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: Tasks.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: Tasks.DeletedBy > Staffs.StaffId [delete: set null]

Ref: SubTasks.TenantId > Tenants.TenantId [delete: restrict]
Ref: SubTasks.TaskId > Tasks.TaskId [delete: restrict]
Ref: SubTasks.AssignedToId > Staffs.StaffId [delete: set null]
Ref: SubTasks.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: SubTasks.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: SubTasks.DeletedBy > Staffs.StaffId [delete: set null]

Ref: TaskProgressLogs.TenantId > Tenants.TenantId [delete: restrict]
Ref: TaskProgressLogs.TaskId > Tasks.TaskId [delete: restrict]
Ref: TaskProgressLogs.SubTaskId > SubTasks.SubTaskId [delete: set null]
Ref: TaskProgressLogs.LoggedById > Staffs.StaffId [delete: restrict]
Ref: TaskProgressLogs.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: TaskProgressLogs.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: TaskProgressLogs.DeletedBy > Staffs.StaffId [delete: set null]

Table TaskAttachments [headercolor: #E1BEE7] {
  TaskAttachmentId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  TaskId uniqueidentifier [not null]
  SubTaskId uniqueidentifier [null]
  FileName nvarchar(255) [not null]
  FileUrl nvarchar(1000) [not null]
  FileType nvarchar(100) [null]
  FileSize bigint [null]
  AttachmentType tinyint [not null, default: 1, note: '1=Reference,2=Result,3=Other']
  UploadedById uniqueidentifier [not null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TaskId, AttachmentType) [name: 'IX_TaskAttachments_TaskId_AttachmentType']
    (SubTaskId) [name: 'IX_TaskAttachments_SubTaskId']
  }
}

Ref: TaskAttachments.TenantId > Tenants.TenantId [delete: restrict]
Ref: TaskAttachments.TaskId > Tasks.TaskId [delete: restrict]
Ref: TaskAttachments.SubTaskId > SubTasks.SubTaskId [delete: set null]
Ref: TaskAttachments.UploadedById > Staffs.StaffId [delete: restrict]
Ref: TaskAttachments.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: TaskAttachments.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: TaskAttachments.DeletedBy > Staffs.StaffId [delete: set null]

Ref: DocProcessingHistory.TenantId > Tenants.TenantId [delete: restrict]
Ref: DocProcessingHistory.PerformedById > Staffs.StaffId [delete: restrict]
Ref: DocProcessingHistory.PerformedForId > Staffs.StaffId [delete: set null]
Ref: DocProcessingHistory.CreatedBy > Staffs.StaffId [delete: restrict]
Ref: DocProcessingHistory.UpdatedBy > Staffs.StaffId [delete: restrict]
Ref: DocProcessingHistory.DeletedBy > Staffs.StaffId [delete: set null]
```

---

## 13.1. Vai trò và chức năng từng bảng

### IncomingDocs
Bảng trung tâm của module Văn bản Đến. Lưu toàn bộ thông tin định danh và trạng thái của một văn bản đến: số đến nội bộ, số ký hiệu gốc, trích yếu, đơn vị gửi, ngày tiếp nhận, ngày ban hành, độ khẩn, độ mật, nguồn gốc (nhập tay / Trục liên thông / scan), người đang chịu trách nhiệm xử lý và trạng thái xử lý hiện tại. Mỗi bản ghi đại diện cho một văn bản đến duy nhất trong phạm vi tenant.

### IncomingDocAttachments
Lưu danh sách file đính kèm của văn bản đến. Tách riêng khỏi `IncomingDocs` để hỗ trợ nhiều file trên một văn bản (file chính, file scan bổ sung, phụ lục). Phân loại file theo `AttachmentType` để phân biệt file gốc, file scan và phụ lục. Phục vụ chức năng xem file, tải file và scan bổ sung.

### IncomingDocAssignments
Lưu toàn bộ lịch sử chuyển xử lý của một văn bản đến. Mỗi lần văn thư hoặc lãnh đạo chuyển văn bản cho người/đơn vị khác tạo ra một bản ghi mới. Trường `IsCurrent` đánh dấu lần chuyển đang có hiệu lực. Lưu ý kiến/chỉ đạo kèm theo và trạng thái tiếp nhận của người được giao. Phục vụ timeline xử lý và kiểm soát trách nhiệm.

### OutgoingDocs
Bảng trung tâm của module Văn bản Đi. Lưu toàn bộ thông tin hồ sơ văn bản đi từ khi khởi tạo dự thảo đến khi phát hành: loại văn bản, trích yếu, nội dung soạn thảo, người soạn/duyệt/ký, số văn bản đã cấp, ngày phát hành và trạng thái theo luồng. Bốn cờ `HasSubmission`, `HasDigitalSignature`, `HasIssuedNumber`, `HasFinalFile` phục vụ kiểm tra điều kiện phát hành mà không cần join bảng phụ.

### OutgoingDocAttachments
Lưu file đính kèm của văn bản đi, phân loại theo vai trò của từng file trong quy trình: bản dự thảo, tờ trình duyệt, phụ lục, bản cuối chính thức. `AttachmentType = 4 (FinalDocument)` là file bản cuối — điều kiện bắt buộc để phát hành. Tách riêng để hỗ trợ quản lý nhiều file và nhiều version trên một hồ sơ.

### Submissions
Bảng trung tâm của module Tờ trình. Lưu thông tin hồ sơ tờ trình: tiêu đề, loại, tóm tắt, nội dung đề xuất, người trình, lãnh đạo được trình và trạng thái theo luồng 6 bước. Liên kết với `OutgoingDocs` khi tờ trình gắn với một văn bản đi cụ thể. Là đầu mối để tra cứu toàn bộ quá trình duyệt và đồng trình.

### SubmissionReviews
Lưu từng lần lãnh đạo thực hiện hành động duyệt trên tờ trình: Phê duyệt, Yêu cầu chỉnh, Từ chối hoặc Ủy quyền. Một tờ trình có thể có nhiều bản ghi nếu bị trả về chỉnh sửa nhiều lần. Khi ủy quyền, `DelegatedToId` ghi nhận người được duyệt thay. Phục vụ audit trail duyệt và hiển thị lịch sử quyết định của lãnh đạo.

### SubmissionCoReviews
Lưu ý kiến đồng trình từ các đơn vị/cá nhân được mời góp ý trước khi trình lãnh đạo. Tách riêng khỏi `SubmissionReviews` vì đây là ý kiến tham khảo, không phải quyết định phê duyệt. Mỗi đơn vị/cá nhân được mời tạo một bản ghi, có hạn phản hồi riêng và trạng thái theo dõi độc lập.

### Tasks
Bảng trung tâm của module Công việc. Lưu phiếu giao việc: tên, mô tả, người giao, người nhận chính, đơn vị thực hiện, ngày bắt đầu, hạn hoàn thành, % tiến độ và trạng thái. `ParentTaskId` cho phép tạo cấu trúc cây task nếu cần; tuy nhiên công việc con cấp 1 được quản lý qua bảng `SubTasks` riêng để tối ưu truy vấn và hiển thị.

### SubTasks
Lưu công việc con (sub-task) của phiếu giao việc. Mỗi sub-task có người phụ trách, hạn xử lý, % tiến độ và trạng thái riêng. Tiến độ của phiếu giao việc cha có thể được tính tự động từ trung bình % hoàn thành các sub-task. Khi tất cả sub-task hoàn thành, phiếu chính chuyển sang trạng thái chờ đánh giá.

### TaskProgressLogs
Lưu lịch sử từng lần cập nhật tiến độ của công việc hoặc sub-task. Ghi nhận % tiến độ trước và sau, nội dung cập nhật, vướng mắc/rủi ro, người cập nhật và thời điểm. Phục vụ timeline tiến độ trên màn chi tiết công việc và audit trail điều hành.

### TaskAttachments
Lưu file đính kèm của công việc và sub-task. Phân loại theo `AttachmentType`: tài liệu tham khảo (Reference) và file kết quả (Result) do chuyên viên nộp khi báo hoàn thành. `AttachmentType = 2 (Result)` là file kết quả chính thức phục vụ nghiệm thu.

### DocProcessingHistory
Bảng audit trail tập trung cho toàn hệ thống. Ghi lại mọi hành động trên văn bản đến, văn bản đi, tờ trình, công việc và thao tác quản trị. Mỗi bản ghi lưu: loại đối tượng, ID đối tượng, mã hành động, trạng thái trước/sau, người thực hiện, thời điểm và dữ liệu bổ sung dạng JSON. Là nguồn dữ liệu duy nhất cho tab "Lịch sử xử lý" ở mọi màn hình chi tiết và báo cáo kiểm tra nội bộ.

---

## 13.2. Mô tả chi tiết các trường thông tin

### IncomingDocs

| Trường | Kiểu | Mục đích |
|---|---|---|
| `IncomingDocId` | uniqueidentifier | PK định danh văn bản đến |
| `TenantId` | uniqueidentifier | Đơn vị/tenant sở hữu văn bản đến. Dùng để phân vùng dữ liệu theo đơn vị |
| `SourceType` | varchar(30) | Nguồn gốc văn bản: `Manual` (nhập tay), `TrucLienThong` (từ Trục liên thông), `Scan` (scan giấy). Ảnh hưởng đến luồng tiếp nhận |
| `ExternalRefCode` | nvarchar(100) | Mã định danh do Trục liên thông cấp. Dùng để đối soát và tránh tạo trùng khi nhận VB từ Trục |
| `DocNumber` | nvarchar(100) | Số đến nội bộ do văn thư cấp khi tiếp nhận (ví dụ: 123/VB-BTC) |
| `ExternalDocNumber` | nvarchar(100) | Số/ký hiệu ghi trên văn bản gốc của cơ quan phát hành |
| `Title` | nvarchar(500) | Tiêu đề/trích yếu chính của văn bản đến |
| `Summary` | nvarchar(max) | Tóm tắt nội dung chi tiết hơn tiêu đề, dùng cho tìm kiếm và hiển thị |
| `SenderOrgName` | nvarchar(255) | Tên cơ quan/đơn vị phát hành văn bản gốc |
| `ReceivedDate` | datetime | Ngày tiếp nhận thực tế tại văn thư |
| `IssuedDate` | datetime | Ngày ban hành ghi trên văn bản gốc |
| `DueDate` | datetime | Hạn xử lý. Tính từ ngày được giao + SLA theo loại VB/độ khẩn |
| `PriorityLevel` | tinyint | Độ khẩn: 1=Thường, 2=Khẩn, 3=Hỏa tốc. Ảnh hưởng đến SLA và hiển thị cảnh báo |
| `ConfidentialityLevel` | tinyint | Độ mật: 1=Thường, 2=Mật, 3=Tối mật, 4=Tuyệt mật. Kiểm soát quyền xem và tải file |
| `Status` | tinyint | Trạng thái xử lý hiện tại: 1=New, 2=Assigned, 3=InProgress, 4=Resolved, 5=Overdue, 6=Closed |
| `CurrentAssigneeId` | uniqueidentifier | FK → Staffs. Người đang chịu trách nhiệm xử lý hiện tại. Cập nhật mỗi lần chuyển xử lý |
| `CurrentAssigneeTenantId` | uniqueidentifier | FK → Tenants. Đơn vị của người đang xử lý. Dùng để lọc theo đơn vị |
| `MainFileUrl` | nvarchar(1000) | Đường dẫn file chính (file scan hoặc file số). Shortcut để preview nhanh mà không cần query `IncomingDocAttachments` |
| `Notes` | nvarchar(max) | Ghi chú nội bộ của văn thư, không hiển thị ra ngoài |

### IncomingDocAttachments

| Trường | Kiểu | Mục đích |
|---|---|---|
| `IncomingDocAttachmentId` | uniqueidentifier | PK định danh file đính kèm của văn bản đến |
| `TenantId` | uniqueidentifier | Tenant sở hữu file đính kèm |
| `IncomingDocId` | uniqueidentifier | FK → `IncomingDocs`. Xác định file thuộc văn bản đến nào |
| `FileName` | nvarchar(255) | Tên file hiển thị cho người dùng |
| `FileUrl` | nvarchar(1000) | Đường dẫn lưu trữ file trên hệ thống file/object storage |
| `FileType` | nvarchar(100) | Loại file/MIME hoặc phần mở rộng để hỗ trợ preview phù hợp |
| `FileSize` | bigint | Dung lượng file, phục vụ hiển thị và kiểm soát upload |
| `AttachmentType` | tinyint | Phân loại file: 1=Main, 2=Scan, 3=Appendix, 4=Other |
| `SortOrder` | int | Thứ tự hiển thị file trong danh sách đính kèm |

### IncomingDocAssignments

| Trường | Kiểu | Mục đích |
|---|---|---|
| `IncomingDocAssignmentId` | uniqueidentifier | PK định danh một lần chuyển xử lý |
| `TenantId` | uniqueidentifier | Tenant phát sinh lần chuyển xử lý |
| `IncomingDocId` | uniqueidentifier | FK → `IncomingDocs`. Văn bản được chuyển xử lý |
| `FromStaffId` | uniqueidentifier | FK → Staffs. Người chuyển/giao xử lý |
| `ToStaffId` | uniqueidentifier | FK → Staffs. Người được giao xử lý |
| `ToTenantId` | uniqueidentifier | FK → Tenants. Đơn vị đích được giao xử lý, nếu có |
| `AssignedDate` | datetime | Thời điểm thực hiện chuyển xử lý |
| `DueDate` | datetime | Hạn xử lý áp cho lần giao này |
| `Comment` | nvarchar(max) | Ý kiến/chỉ đạo đi kèm khi giao xử lý |
| `AssignmentStatus` | tinyint | Trạng thái của lần giao: Assigned, Accepted, InProgress, Completed, Rejected, Cancelled |
| `IsCurrent` | bit | Đánh dấu đây có phải lần giao đang còn hiệu lực hiện tại hay không |

### OutgoingDocs

| Trường | Kiểu | Mục đích |
|---|---|---|
| `OutgoingDocId` | uniqueidentifier | PK định danh văn bản đi |
| `TenantId` | uniqueidentifier | Tenant/đơn vị sở hữu văn bản đi |
| `DocCode` | nvarchar(100) | Mã hồ sơ nội bộ hoặc mã dự thảo để tra cứu trước khi phát hành |
| `Title` | nvarchar(500) | Tiêu đề/trích yếu chính của văn bản đi |
| `Summary` | nvarchar(max) | Tóm tắt nội dung văn bản |
| `DocType` | nvarchar(100) | Loại văn bản đi: công văn, quyết định, thông báo... |
| `DraftContent` | nvarchar(max) | Nội dung bản dự thảo nếu hệ thống hỗ trợ soạn trực tiếp trên web |
| `PriorityLevel` | tinyint | Độ khẩn của văn bản đi |
| `ConfidentialityLevel` | tinyint | Độ mật của văn bản đi |
| `NumberingCode` | nvarchar(100) | Mã quy tắc đánh số áp dụng khi cấp số phát hành |
| `IssuedNumber` | nvarchar(100) | Số văn bản chính thức sau khi cấp số |
| `IssuedDate` | datetime | Ngày phát hành/cấp số chính thức |
| `DrafterId` | uniqueidentifier | FK → Staffs. Người soạn thảo chính |
| `ReviewerId` | uniqueidentifier | FK → Staffs. Người duyệt nội bộ hoặc người kiểm tra nội dung |
| `SignerId` | uniqueidentifier | FK → Staffs. Người có thẩm quyền ký số |
| `HasSubmission` | bit | Cờ cho biết hồ sơ đã có tờ trình duyệt hợp lệ hay chưa |
| `HasDigitalSignature` | bit | Cờ cho biết văn bản đã được ký số thành công hay chưa |
| `HasIssuedNumber` | bit | Cờ cho biết văn bản đã được cấp số chính thức hay chưa |
| `HasFinalFile` | bit | Cờ cho biết đã có file bản cuối chính thức để phát hành hay chưa |
| `Status` | tinyint | Trạng thái hiện tại của văn bản đi theo luồng Draft → Issued/Truc... |
| `LastAutoSavedAt` | datetime | Thời điểm autosave gần nhất, phục vụ khôi phục bản nháp và hiển thị UI |
| `FinalFileUrl` | nvarchar(1000) | Đường dẫn nhanh tới file bản cuối để preview/tải xuống |

### OutgoingDocAttachments

| Trường | Kiểu | Mục đích |
|---|---|---|
| `OutgoingDocAttachmentId` | uniqueidentifier | PK định danh file đính kèm của văn bản đi |
| `TenantId` | uniqueidentifier | Tenant sở hữu file |
| `OutgoingDocId` | uniqueidentifier | FK → `OutgoingDocs`. File thuộc văn bản đi nào |
| `FileName` | nvarchar(255) | Tên file hiển thị |
| `FileUrl` | nvarchar(1000) | Đường dẫn lưu trữ file |
| `FileType` | nvarchar(100) | Kiểu file/MIME/phần mở rộng |
| `FileSize` | bigint | Dung lượng file |
| `AttachmentType` | tinyint | Loại file: 1=Draft, 2=Submission, 3=Appendix, 4=FinalDocument, 5=Other |
| `SortOrder` | int | Thứ tự hiển thị trong danh sách file |

### Submissions

| Trường | Kiểu | Mục đích |
|---|---|---|
| `SubmissionId` | uniqueidentifier | PK định danh hồ sơ tờ trình |
| `TenantId` | uniqueidentifier | Tenant sở hữu hồ sơ tờ trình |
| `OutgoingDocId` | uniqueidentifier | FK → `OutgoingDocs`. Liên kết tới văn bản đi nếu tờ trình phục vụ phát hành một VB cụ thể |
| `SubmissionCode` | nvarchar(100) | Mã hồ sơ tờ trình dùng cho tra cứu nội bộ |
| `Title` | nvarchar(500) | Tiêu đề tờ trình |
| `Summary` | nvarchar(max) | Tóm tắt nội dung hoặc mục đích đề xuất |
| `Content` | nvarchar(max) | Nội dung đầy đủ của tờ trình |
| `SubmissionType` | nvarchar(100) | Loại tờ trình để phân luồng hoặc áp SLA riêng |
| `PriorityLevel` | tinyint | Mức độ ưu tiên xử lý tờ trình |
| `Status` | tinyint | Trạng thái hiện tại của hồ sơ tờ trình |
| `SubmitterId` | uniqueidentifier | FK → Staffs. Người khởi tạo/trình hồ sơ |
| `LeaderId` | uniqueidentifier | FK → Staffs. Lãnh đạo được trình để duyệt/ký |
| `SignedAt` | datetime | Thời điểm ký số thành công |
| `CompletedAt` | datetime | Thời điểm hoàn tất hồ sơ tờ trình |

### SubmissionReviews

| Trường | Kiểu | Mục đích |
|---|---|---|
| `SubmissionReviewId` | uniqueidentifier | PK định danh một lần duyệt |
| `TenantId` | uniqueidentifier | Tenant phát sinh hành động duyệt |
| `SubmissionId` | uniqueidentifier | FK → `Submissions`. Tờ trình được duyệt |
| `ReviewerId` | uniqueidentifier | FK → Staffs. Lãnh đạo thực hiện hành động duyệt |
| `ReviewAction` | tinyint | Loại hành động: 1=Approve, 2=RequestRevision, 3=Reject, 4=Delegate |
| `ReviewComment` | nvarchar(max) | Nội dung ý kiến, lý do từ chối hoặc yêu cầu chỉnh sửa |
| `DelegatedToId` | uniqueidentifier | FK → Staffs. Người được ủy quyền duyệt thay. Chỉ có giá trị khi `ReviewAction = 4` |
| `ReviewedAt` | datetime | Thời điểm lãnh đạo thực hiện hành động duyệt |

### SubmissionCoReviews

| Trường | Kiểu | Mục đích |
|---|---|---|
| `SubmissionCoReviewId` | uniqueidentifier | PK định danh một lần đồng trình |
| `TenantId` | uniqueidentifier | Tenant sở hữu hồ sơ tờ trình |
| `SubmissionId` | uniqueidentifier | FK → `Submissions`. Tờ trình cần lấy ý kiến đồng trình |
| `ReviewerTenantId` | uniqueidentifier | FK → Tenants. Đơn vị được mời đồng trình |
| `ReviewerId` | uniqueidentifier | FK → Staffs. Cá nhân được mời đồng trình |
| `ReviewStatus` | tinyint | Trạng thái phản hồi: 1=Pending, 2=Responded, 3=Approved, 4=RevisionRequested, 5=Rejected |
| `ReviewComment` | nvarchar(max) | Nội dung ý kiến đồng trình |
| `ReviewedAt` | datetime | Thời điểm đơn vị/cá nhân phản hồi |
| `DueDate` | datetime | Hạn phản hồi ý kiến đồng trình |

### Tasks

| Trường | Kiểu | Mục đích |
|---|---|---|
| `TaskId` | uniqueidentifier | PK định danh phiếu giao việc |
| `TenantId` | uniqueidentifier | Tenant sở hữu phiếu giao việc |
| `TaskCode` | nvarchar(100) | Mã phiếu giao việc để tra cứu nhanh |
| `Title` | nvarchar(500) | Tên công việc |
| `Description` | nvarchar(max) | Mô tả chi tiết nội dung, yêu cầu và kết quả mong đợi |
| `ParentTaskId` | uniqueidentifier | FK → `Tasks` (self-ref). Công việc cha nếu cần cấu trúc cây task |
| `AssignedById` | uniqueidentifier | FK → Staffs. Người giao việc |
| `AssignedToId` | uniqueidentifier | FK → Staffs. Người nhận việc chính, chịu trách nhiệm chính |
| `AssignedToTenantId` | uniqueidentifier | FK → Tenants. Đơn vị của người nhận việc |
| `StartDate` | datetime | Ngày bắt đầu thực hiện |
| `DueDate` | datetime | Hạn hoàn thành công việc |
| `ProgressPercent` | decimal(5,2) | Phần trăm tiến độ hiện tại (0–100) |
| `Status` | tinyint | Trạng thái: New, InProgress, PendingReview, Completed, Overdue, Closed, Cancelled |
| `PriorityLevel` | tinyint | Mức độ ưu tiên của công việc |
| `ResultSummary` | nvarchar(max) | Tóm tắt kết quả do người thực hiện ghi khi báo hoàn thành |

### SubTasks

| Trường | Kiểu | Mục đích |
|---|---|---|
| `SubTaskId` | uniqueidentifier | PK định danh công việc con |
| `TenantId` | uniqueidentifier | Tenant sở hữu công việc con |
| `TaskId` | uniqueidentifier | FK → `Tasks`. Phiếu giao việc cha |
| `Title` | nvarchar(500) | Tên công việc con |
| `Description` | nvarchar(max) | Mô tả chi tiết công việc con |
| `AssignedToId` | uniqueidentifier | FK → Staffs. Người phụ trách sub-task |
| `StartDate` | datetime | Ngày bắt đầu sub-task |
| `DueDate` | datetime | Hạn hoàn thành sub-task |
| `ProgressPercent` | decimal(5,2) | Phần trăm tiến độ sub-task |
| `Status` | tinyint | Trạng thái sub-task |
| `SortOrder` | int | Thứ tự hiển thị trong danh sách sub-task của công việc cha |

### TaskProgressLogs

| Trường | Kiểu | Mục đích |
|---|---|---|
| `TaskProgressLogId` | uniqueidentifier | PK định danh một lần cập nhật tiến độ |
| `TenantId` | uniqueidentifier | Tenant sở hữu bản ghi |
| `TaskId` | uniqueidentifier | FK → `Tasks`. Công việc được cập nhật tiến độ |
| `SubTaskId` | uniqueidentifier | FK → `SubTasks`. Sub-task được cập nhật, nếu có |
| `PreviousProgressPercent` | decimal(5,2) | Phần trăm tiến độ trước khi cập nhật |
| `CurrentProgressPercent` | decimal(5,2) | Phần trăm tiến độ sau khi cập nhật |
| `UpdateComment` | nvarchar(max) | Nội dung cập nhật, mô tả công việc đã làm |
| `Blockers` | nvarchar(max) | Vướng mắc, rủi ro hoặc yếu tố cản trở tiến độ |
| `LoggedAt` | datetime | Thời điểm thực hiện cập nhật tiến độ |
| `LoggedById` | uniqueidentifier | FK → Staffs. Người thực hiện cập nhật tiến độ |

### TaskAttachments

| Trường | Kiểu | Mục đích |
|---|---|---|
| `TaskAttachmentId` | uniqueidentifier | PK định danh file đính kèm của công việc |
| `TenantId` | uniqueidentifier | Tenant sở hữu file |
| `TaskId` | uniqueidentifier | FK → `Tasks`. Công việc chứa file đính kèm |
| `SubTaskId` | uniqueidentifier | FK → `SubTasks`. Sub-task chứa file, nếu file gắn với sub-task cụ thể |
| `FileName` | nvarchar(255) | Tên file hiển thị |
| `FileUrl` | nvarchar(1000) | Đường dẫn lưu trữ file |
| `FileType` | nvarchar(100) | Kiểu file/MIME/phần mở rộng |
| `FileSize` | bigint | Dung lượng file |
| `AttachmentType` | tinyint | Loại file: 1=Reference (tài liệu tham khảo), 2=Result (kết quả nộp), 3=Other |
| `UploadedById` | uniqueidentifier | FK → Staffs. Người upload file |

### DocProcessingHistory

| Trường | Kiểu | Mục đích |
|---|---|---|
| `DocProcessingHistoryId` | uniqueidentifier | PK định danh bản ghi lịch sử |
| `TenantId` | uniqueidentifier | Tenant phát sinh hành động |
| `EntityType` | nvarchar(100) | Loại đối tượng: IncomingDoc, OutgoingDoc, Submission, Task, SubTask, AdminAction |
| `EntityId` | uniqueidentifier | ID của đối tượng liên quan (VB đến, VB đi, tờ trình, công việc...) |
| `ActionCode` | nvarchar(150) | Mã hành động theo danh sách action code đã định nghĩa (ví dụ: `incomingdoc.assign`) |
| `ActionName` | nvarchar(255) | Tên hành động hiển thị cho người dùng trong timeline |
| `FromStatus` | tinyint | Trạng thái của đối tượng trước khi thực hiện hành động |
| `ToStatus` | tinyint | Trạng thái của đối tượng sau khi thực hiện hành động |
| `PerformedById` | uniqueidentifier | FK → Staffs. Người thực hiện hành động |
| `PerformedForId` | uniqueidentifier | FK → Staffs. Người liên quan (người nhận, người được giao...) nếu có |
| `Comment` | nvarchar(max) | Ghi chú hoặc ý kiến đi kèm hành động |
| `MetadataJson` | nvarchar(max) | Dữ liệu bổ sung dạng JSON tùy theo loại hành động (ví dụ: thông tin ký số, mã Trục...) |
| `ProcessedAt` | datetime | Thời điểm thực tế hành động được thực hiện |

---

# 14. Kết luận

Tài liệu này đặc tả đầy đủ Dashboard Admin của hệ thống QLVB Bộ Tài chính theo định hướng web dashboard, bao gồm:

- Phạm vi vai trò và nguyên tắc phân quyền.
- 5 module nghiệp vụ với chức năng chi tiết, ma trận action theo role × trạng thái.
- Luồng Trục liên thông (VB đến + VB đi), ủy quyền duyệt, SLA/hạn xử lý.
- Checklist phát hành, luồng tờ trình 6 bước, ký số SIM PKI.
- DBML 13 bảng nghiệp vụ cốt lõi theo convention project.
- Mô tả chi tiết vai trò và từng trường dữ liệu của tất cả các bảng.
