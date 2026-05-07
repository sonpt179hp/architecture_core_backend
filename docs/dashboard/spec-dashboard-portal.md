# Đặc tả Dashboard Portal - Hệ thống QLVB Bộ Tài chính

- **Ngày cập nhật:** 07/05/2026
- **Mục đích tài liệu:** Làm tài liệu tham chiếu cho phân tích, thiết kế UI/UX, API, xử lý dữ liệu snapshot và triển khai Dashboard Portal của hệ thống QLVB Bộ Tài chính.

---

# 1. Tổng quan và mục tiêu Dashboard Portal

## 1.1. Bối cảnh

Dashboard Portal là lớp hiển thị tổng hợp ở mức cổng thông tin, phục vụ nhu cầu theo dõi cấp cao và công khai có chọn lọc. Khác với Dashboard Admin thiên về điều hành chi tiết, Dashboard Portal tập trung vào:

- Chỉ số tổng hợp toàn hệ thống
- Xu hướng xử lý theo thời gian
- So sánh hiệu quả giữa các đơn vị
- Cảnh báo điều hành mức vĩ mô
- Tra cứu công khai tối giản cho đối tượng ngoài hệ thống
- Insight phục vụ báo cáo giao ban

## 1.2. Mục tiêu

Dashboard Portal được thiết kế để đáp ứng các mục tiêu sau:

1. Cung cấp góc nhìn tổng quan, nhanh và dễ hiểu cho Lãnh đạo Bộ.
2. Hỗ trợ theo dõi tình hình tiếp nhận, xử lý, hoàn thành văn bản trên toàn hệ thống.
3. Làm rõ xu hướng vận hành theo tháng/quý để phục vụ giao ban và chỉ đạo.
4. Tạo cơ chế cảnh báo sớm đối với đơn vị có dấu hiệu quá hạn hoặc suy giảm hiệu quả.
5. Công khai có kiểm soát một số thông tin tra cứu trạng thái văn bản/hồ sơ mà không làm lộ dữ liệu nhạy cảm.
6. Chuẩn hóa dữ liệu tổng hợp theo mô hình snapshot để tối ưu hiệu năng truy vấn và hiển thị.

## 1.3. Định vị của Portal trong kiến trúc tổng thể

| Thành phần | Mục tiêu chính | Đối tượng sử dụng | Mức độ chi tiết |
|---|---|---|---|
| Dashboard Admin | Điều hành, xử lý công việc hằng ngày | Văn thư, Lãnh đạo đơn vị, Chuyên viên, Quản trị | Chi tiết, thao tác được |
| Dashboard Portal | Tổng hợp, báo cáo cấp cao, công khai tối giản | Lãnh đạo Bộ, người xem tổng quan, người tra cứu công khai | Tổng hợp, ít thao tác nghiệp vụ |

---

# 2. Phạm vi Dashboard Portal

## 2.1. Phạm vi nghiệp vụ

Dashboard Portal bao gồm 2 lớp chính:

1. **Portal nội bộ cấp cao** dành cho Lãnh đạo Bộ và cán bộ được phân quyền xem số liệu tổng hợp.
2. **Portal công khai tối giản** dành cho nhu cầu tra cứu trạng thái văn bản/hồ sơ theo mã tra cứu.

## 2.2. Portal nội bộ cấp cao (Lãnh đạo Bộ)

Phục vụ các nhu cầu:

- Xem KPI toàn hệ thống ở mức Bộ
- Theo dõi biến động xử lý theo tháng/quý
- So sánh hiệu quả giữa các khối đơn vị
- Nhận cảnh báo điều hành
- Tạo câu tóm tắt phục vụ báo cáo giao ban
- Xuất báo cáo nhanh PDF/Excel

**Không thuộc phạm vi chính của Portal nội bộ:**

- Xử lý văn bản chi tiết
- Giao việc trực tiếp trên từng hồ sơ
- Duyệt, ký, chuyển xử lý văn bản
- Xem toàn bộ nội dung chi tiết tài liệu mật hoặc nhạy cảm từ màn hình Portal

## 2.3. Portal công khai tối giản

Phục vụ các nhu cầu:

- Tra cứu trạng thái xử lý theo mã hồ sơ / mã văn bản
- Xem một số trạng thái không nhạy cảm
- Tăng minh bạch thông tin ở mức phù hợp

**Không thuộc phạm vi của Portal công khai:**

- Nội dung văn bản
- Người xử lý
- Luồng phê duyệt nội bộ
- Chỉ số điều hành chi tiết theo đơn vị
- Văn bản có mức độ mật hoặc dữ liệu thuộc phạm vi hạn chế công bố

---

# 3. Đối tượng sử dụng

| Nhóm người dùng | Nhu cầu chính | Phạm vi truy cập |
|---|---|---|
| Lãnh đạo Bộ | Theo dõi toàn cảnh vận hành, chỉ số chỉ đạo, cảnh báo | Toàn Bộ hoặc theo cụm đơn vị được phân quyền |
| Văn phòng/Ban tổng hợp | Chuẩn bị số liệu giao ban, tổng hợp báo cáo | Toàn Bộ hoặc cấp tổng hợp |
| Lãnh đạo đơn vị cấp dưới | Xem vị trí đơn vị trong bức tranh tổng thể | Theo đơn vị và phạm vi được phân quyền |
| Người dân / tổ chức bên ngoài | Tra cứu tiến độ cơ bản theo mã | Chỉ tra cứu công khai tối giản |

---

# 4. Nguyên tắc thiết kế Portal

## 4.1. Dữ liệu hiển thị công khai và nội bộ

### Dữ liệu chỉ hiển thị nội bộ

- KPI toàn hệ thống theo Bộ, Cục/Vụ, Trung tâm
- Tỷ lệ đúng hạn, quá hạn, tồn đọng theo đơn vị
- Xếp hạng đơn vị
- Cảnh báo điều hành
- Insight tổng hợp phục vụ giao ban
- So sánh hiệu quả xử lý giữa các khối đơn vị

### Dữ liệu có thể hiển thị công khai

- Mã hồ sơ / mã văn bản
- Loại hồ sơ / loại văn bản
- Ngày tiếp nhận hoặc ngày ghi nhận
- Trạng thái xử lý tổng quát
- Ngày dự kiến hoàn thành (nếu phù hợp công khai)

## 4.2. Quy tắc ẩn thông tin nhạy cảm

1. Không hiển thị nội dung chi tiết văn bản.
2. Không hiển thị trích yếu nếu có yếu tố nhạy cảm khi ở chế độ công khai.
3. Không hiển thị người xử lý cụ thể trên Portal công khai.
4. Không hiển thị lịch sử luân chuyển nội bộ trên Portal công khai.
5. Không hiển thị tài liệu thuộc danh mục mật, tối mật, tuyệt mật.
6. Không hiển thị tệp đính kèm trên Portal công khai.
7. Với Portal nội bộ, chỉ hiển thị dữ liệu tổng hợp; khi drill-down sang danh sách nghiệp vụ thì phải áp dụng lại phân quyền hệ thống gốc.

## 4.3. Tần suất cập nhật snapshot

| Loại dữ liệu | Tần suất đề xuất | Ghi chú |
|---|---|---|
| KPI tổng hợp ngày | Mỗi 30 phút hoặc theo lịch giờ | Dùng cho dashboard nội bộ |
| Snapshot tháng | Chạy cuối ngày hoặc đầu ngày hôm sau | Phục vụ xu hướng, xếp hạng, insight |
| Snapshot quý | Chạy tại thời điểm khóa kỳ hoặc đầu quý mới | Phục vụ báo cáo cấp cao |
| Cảnh báo điều hành | 15 phút đến 30 phút/lần | Có thể sinh từ rule engine |
| Tra cứu công khai | Gần thời gian thực hoặc đồng bộ theo batch ngắn | Chỉ đồng bộ trường an toàn |
| Insight tự động | 1 lần/ngày hoặc 1 lần/tháng tùy chế độ báo cáo | Có thể regenerate khi khóa dữ liệu |

## 4.4. Nguyên tắc hiển thị

- Ưu tiên hiển thị số ít nhưng có ý nghĩa điều hành cao.
- Thể hiện so sánh kỳ trước để tránh số liệu rời rạc.
- Dùng màu sắc cảnh báo nhất quán: bình thường, lưu ý, nghiêm trọng.
- Mọi tỷ lệ phải có quy tắc tính rõ ràng và nhất quán giữa Portal và báo cáo xuất file.
- Nếu dữ liệu chưa đồng bộ xong, phải hiển thị thời điểm cập nhật gần nhất.
- Biểu đồ và bảng xếp hạng nên hỗ trợ drill-down sang danh sách tổng hợp theo quyền.

---

# 5. Module P1 - KPI tổng hợp toàn hệ thống

## 5.1. Mục tiêu

Hiển thị nhanh các KPI điều hành trọng tâm ở mức toàn hệ thống hoặc theo phạm vi tenant được phân quyền.

## 5.2. Bộ KPI đề xuất

1. Tổng Văn bản Đến
2. Tổng Văn bản Đi
3. Tổng Tờ trình
4. Tổng Phiếu giao việc
5. Tỷ lệ đúng hạn
6. Số hồ sơ/văn bản quá hạn hiện tại

Mỗi KPI cần kèm:

- Giá trị hiện tại
- Chênh lệch so với kỳ trước
- Tỷ lệ tăng/giảm
- Trạng thái xu hướng: tăng, giảm, đi ngang

## 5.3. Định nghĩa KPI

| KPI | Ý nghĩa | Công thức / nguồn tổng hợp |
|---|---|---|
| Tổng VB đến | Tổng số văn bản đến phát sinh trong kỳ | Đếm nghiệp vụ văn bản đến theo kỳ |
| Tổng VB đi | Tổng số văn bản đi phát sinh trong kỳ | Đếm nghiệp vụ văn bản đi theo kỳ |
| Tổng tờ trình | Tổng số tờ trình phát sinh trong kỳ | Đếm nghiệp vụ tờ trình theo kỳ |
| Tổng phiếu giao việc | Tổng số công việc phát sinh trong kỳ | Đếm nghiệp vụ công việc theo kỳ |
| Tỷ lệ đúng hạn | Tỷ lệ hồ sơ/văn bản được xử lý đúng hoặc trước hạn | Xem mục 5.3.1 |
| Số quá hạn hiện tại | Tổng hồ sơ/văn bản/công việc đang quá hạn tại thời điểm snapshot | Đếm bản ghi overdue đang mở |

### 5.3.1. Chuẩn hóa công thức Tỷ lệ đúng hạn

Đây là KPI quan trọng nhất và dễ gây tranh cãi nhất nếu không chuẩn hóa. Cần thống nhất trước khi triển khai.

**Công thức:**

```
Tỷ lệ đúng hạn = Số bản ghi hoàn thành đúng hạn / Tổng số bản ghi đã hoàn thành × 100%
```

**Định nghĩa rõ từng thành phần:**

| Thành phần | Định nghĩa | Ghi chú |
|---|---|---|
| Hoàn thành đúng hạn | Bản ghi có trạng thái `Resolved/Completed/Closed` VÀ `UpdatedAt <= DueDate` | Nếu không có DueDate thì không tính vào mẫu số |
| Tổng đã hoàn thành | Bản ghi có trạng thái `Resolved/Completed/Closed` | Không tính bản ghi `Cancelled` hoặc `Rejected` |
| Phạm vi tính | Tính riêng cho từng loại: VB đến / VB đi / Tờ trình / Công việc | Tỷ lệ tổng hợp = trung bình có trọng số hoặc tổng gộp |

**Quy tắc bắt buộc:**
- Số liệu trên Portal phải dùng **cùng công thức** với báo cáo xuất file — không được tính khác nhau.
- Nếu bản ghi không có `DueDate`, loại khỏi cả tử số và mẫu số.
- Làm tròn đến 1 chữ số thập phân (ví dụ: 91.6%).

## 5.4. Bảng chức năng chi tiết

| Chức năng | Mô tả | Phạm vi hiển thị | Ghi chú |
|---|---|---|---|
| Hiển thị KPI chính | Hiển thị 6 thẻ KPI tổng quan của toàn hệ thống | Nội bộ cấp cao | Có màu trạng thái và icon phân biệt |
| So sánh kỳ trước | Tính chênh lệch số lượng hoặc tỷ lệ với kỳ trước | Nội bộ cấp cao | Cần chuẩn hóa cách tính phần trăm tăng/giảm |
| Chọn loại kỳ | Cho phép xem theo ngày, tháng, quý | Nội bộ cấp cao | Đồng bộ với trường PeriodType trong snapshot |
| Hiển thị thời điểm cập nhật | Cho biết snapshot gần nhất đã được đồng bộ lúc nào | Nội bộ cấp cao | Hỗ trợ kiểm soát độ mới dữ liệu |
| Xuất báo cáo KPI | Xuất PDF/Excel theo kỳ hiện tại | Nội bộ cấp cao | Phục vụ báo cáo giao ban |

---

# 6. Module P2 - Xu hướng và biểu đồ

## 6.1. Mục tiêu

Giúp người xem nhận ra xu hướng vận hành và biến động khối lượng xử lý trong 3 đến 6 tháng gần nhất, thay vì chỉ nhìn số liệu tức thời.

## 6.2. Thành phần chính

### Biểu đồ line chart xu hướng 3-6 tháng

Các chuỗi dữ liệu đề xuất:

- Tiếp nhận
- Đã xử lý
- Quá hạn

### Biểu đồ cột so sánh khối đơn vị

So sánh giữa các khối hoặc nhóm đơn vị:

- Cục / Vụ
- Trung tâm
- Có thể gộp theo cụm đơn vị nếu cần

Các chỉ số so sánh có thể bao gồm:

- Số lượng xử lý
- Tỷ lệ đúng hạn
- Số lượng quá hạn

## 6.3. Yêu cầu drill-down

- Click vào một điểm trên line chart → mở danh sách snapshot hoặc drill-down sang xếp hạng đơn vị của kỳ đó.
- Click vào một cột đơn vị → chuyển sang màn hình xếp hạng hoặc danh sách đơn vị chi tiết.
- Drill-down chỉ hiển thị dữ liệu trong phạm vi quyền của người xem.

## 6.4. Bảng chức năng chi tiết

| Chức năng | Mô tả | Phạm vi hiển thị | Ghi chú |
|---|---|---|---|
| Line chart xu hướng | Hiển thị xu hướng tiếp nhận, xử lý, quá hạn trong 3-6 tháng | Nội bộ cấp cao | Nên dùng dữ liệu snapshot tháng |
| Chọn mốc thời gian | Cho phép chuyển giữa 3 tháng và 6 tháng | Nội bộ cấp cao | Có thể mở rộng 12 tháng sau |
| Biểu đồ cột theo khối đơn vị | So sánh hiệu quả giữa các đơn vị hoặc nhóm đơn vị | Nội bộ cấp cao | Có thể sắp xếp giảm dần theo chỉ số |
| Tooltip chi tiết | Hiển thị giá trị cụ thể khi hover/tap | Nội bộ cấp cao | Cần format số và phần trăm nhất quán |
| Drill-down biểu đồ | Điều hướng sang dữ liệu chi tiết tổng hợp theo kỳ hoặc đơn vị | Nội bộ cấp cao | Không đi thẳng vào dữ liệu nghiệp vụ nếu chưa qua phân quyền |

---

# 7. Module P3 - Xếp hạng đơn vị

## 7.1. Mục tiêu

Tạo góc nhìn cạnh tranh tích cực và hỗ trợ lãnh đạo nhanh chóng nhận diện đơn vị làm tốt hoặc cần ưu tiên chấn chỉnh.

## 7.2. Nội dung hiển thị

- Top 5 đơn vị theo tỷ lệ đúng hạn
- Bottom 3 đơn vị cần lưu ý

Ngoài thứ hạng, nên hiển thị thêm:

- Tỷ lệ đúng hạn
- Số lượng hồ sơ/văn bản quá hạn
- Tổng hồ sơ đang chờ xử lý
- Xu hướng cải thiện hoặc suy giảm so với kỳ trước
- Loại nghiệp vụ đang xếp hạng: Tổng hợp / VB đến / VB đi / Tờ trình / Công việc

## 7.3. Nguyên tắc xếp hạng

- Xếp hạng theo kỳ tháng là chính.
- Chỉ xếp hạng các đơn vị có đủ dữ liệu hoạt động trong kỳ.
- Nếu cùng tỷ lệ đúng hạn, có thể dùng OverdueCount hoặc ProcessedCount làm tiêu chí phụ.
- Đơn vị có sản lượng quá thấp có thể gắn cờ “tham khảo”, không dùng để đánh giá chính thức.

## 7.4. Bảng chức năng chi tiết

| Chức năng | Mô tả | Phạm vi hiển thị | Ghi chú |
|---|---|---|---|
| Top 5 đơn vị | Hiển thị 5 đơn vị có tỷ lệ đúng hạn cao nhất | Nội bộ cấp cao | Có thể kèm huy hiệu xếp hạng |
| Bottom 3 đơn vị | Hiển thị 3 đơn vị có tỷ lệ thấp hoặc quá hạn cao | Nội bộ cấp cao | Dùng để lưu ý điều hành |
| Hiển thị chỉ số phụ | Kèm số lượng quá hạn, tồn đọng, tổng xử lý | Nội bộ cấp cao | Giúp tránh đánh giá một chiều |
| So sánh với kỳ trước | Thể hiện thay đổi vị trí hoặc thay đổi tỷ lệ | Nội bộ cấp cao | Dùng dữ liệu snapshot kỳ trước |
| Lọc theo loại nghiệp vụ | Xem xếp hạng theo VB đến/VB đi/Tờ trình/Công việc | Nội bộ cấp cao | Cần dimension `Category` trong snapshot |

---

# 8. Module P4 - Cảnh báo điều hành

## 8.1. Mục tiêu

Tự động phát hiện và hiển thị các vấn đề cần quan tâm ở mức quản trị, thay vì chờ người dùng tự đọc số liệu.

## 8.2. Các loại cảnh báo chính

1. Đơn vị có tỷ lệ quá hạn > 10%
2. Backlog tăng bất thường
3. Đơn vị chưa cập nhật dữ liệu hoặc không phát sinh snapshot đúng lịch
4. Tờ trình tồn đọng lâu ở trạng thái chờ duyệt
5. Đơn vị không phát sinh nghiệp vụ trong kỳ

## 8.3. Quy tắc cảnh báo đề xuất

| Loại cảnh báo | Điều kiện gợi ý | Mức độ |
|---|---|---|
| OverdueRateHigh | Tỷ lệ quá hạn của đơn vị > 10% trong kỳ hiện tại | warning hoặc critical |
| BacklogSpike | PendingCount tăng mạnh so với trung bình 2-3 kỳ gần nhất | warning |
| MissingUpdate | Đơn vị chưa có snapshot mới trong khoảng thời gian quy định | info hoặc warning |
| SubmissionStuck | Tờ trình ở trạng thái chờ duyệt quá N ngày | warning hoặc critical |
| NoActivity | Đơn vị không phát sinh VB/tờ trình/công việc trong kỳ | info hoặc warning |

## 8.4. Bảng chức năng chi tiết

| Chức năng | Mô tả | Phạm vi hiển thị | Ghi chú |
|---|---|---|---|
| Danh sách cảnh báo | Hiển thị các cảnh báo điều hành đang mở | Nội bộ cấp cao | Sắp xếp theo severity và thời gian |
| Cảnh báo quá hạn > 10% | Đánh dấu đơn vị có tỷ lệ quá hạn vượt ngưỡng | Nội bộ cấp cao | Có thể cấu hình ngưỡng sau |
| Cảnh báo backlog bất thường | Phát hiện lượng tồn tăng nhanh | Nội bộ cấp cao | Cần rule so sánh theo lịch sử |
| Cảnh báo chưa cập nhật | Nhận diện đơn vị chưa có snapshot mới | Nội bộ cấp cao | Hữu ích cho vận hành dữ liệu |
| Cảnh báo tờ trình tồn đọng | Phát hiện hồ sơ chờ lãnh đạo duyệt quá lâu | Nội bộ cấp cao | Liên quan điều hành cấp cao |
| Cảnh báo không phát sinh nghiệp vụ | Phát hiện đơn vị không có hoạt động trong kỳ | Nội bộ cấp cao | Có thể là bất thường dữ liệu |
| Đánh dấu đã xử lý | Cho phép ghi nhận cảnh báo đã được xem/xử lý | Nội bộ cấp cao | Chỉ là trạng thái quản trị, không thay nghiệp vụ |

---

# 9. Module P5 - Tra cứu công khai

## 9.1. Mục tiêu

Cho phép người dân, tổ chức hoặc bên liên quan tra cứu trạng thái cơ bản của hồ sơ/văn bản theo mã, tăng tính minh bạch nhưng vẫn đảm bảo an toàn thông tin.

## 9.2. Hành vi chính

Người dùng nhập:

- Mã hồ sơ
- Hoặc mã văn bản

Kết quả trả về chỉ gồm các trường an toàn:

- Mã tra cứu
- Loại văn bản/hồ sơ
- Ngày tiếp nhận
- Trạng thái hiện tại ở mức tổng quát
- Ngày dự kiến hoàn thành

## 9.3. Trạng thái công khai đề xuất

- Đang xử lý
- Đã hoàn thành

## 9.4. Quy tắc bảo mật

- Không công khai nội dung văn bản.
- Không công khai tên cán bộ xử lý.
- Không công khai ý kiến chỉ đạo, chuyển luồng, file đính kèm.
- Không công khai văn bản thuộc danh mục hạn chế.
- Có thể ẩn hoặc mã hóa một phần mã hồ sơ nếu quy định yêu cầu.
- Cần có rate limit hoặc captcha để chống brute-force tra cứu hàng loạt.
- Cần log lại các lượt tra cứu công khai để audit và phát hiện lạm dụng.

## 9.5. Cơ sở pháp lý — bắt buộc xác nhận trước khi triển khai

> **Đây là điểm rủi ro pháp lý cao nhất của toàn bộ tính năng Portal công khai.**

Trước khi triển khai tính năng tra cứu công khai, cần xác nhận và lưu hồ sơ các nội dung sau:

| Câu hỏi cần xác nhận | Lý do |
|---|---|
| Bộ Tài chính có văn bản pháp lý nào cho phép công khai trạng thái hồ sơ không? | Tránh vi phạm quy định bảo mật thông tin nhà nước |
| Loại hồ sơ nào được phép tra cứu công khai? | Không phải tất cả VB đều phù hợp công khai |
| Nếu người dân tra cứu và thấy hồ sơ "quá hạn" → có phát sinh khiếu nại pháp lý không? | Rủi ro trách nhiệm pháp lý của đơn vị |
| Dữ liệu công khai có cần qua phê duyệt trước khi đồng bộ không? | Kiểm soát chất lượng dữ liệu trước khi công bố |

**Khuyến nghị:** Chỉ triển khai Portal công khai sau khi có văn bản chấp thuận từ đơn vị pháp chế hoặc lãnh đạo có thẩm quyền. Trong giai đoạn MVP, có thể triển khai Portal công khai ở chế độ **nội bộ** (chỉ truy cập trong mạng nội bộ) trước khi mở ra Internet.

## 9.6. Xử lý kết quả không tìm thấy

- Nếu mã không tồn tại, hiển thị thông báo trung tính: `Không tìm thấy dữ liệu phù hợp với mã đã nhập.`
- Không tiết lộ liệu mã đó từng tồn tại hay không.
- Không phân biệt rõ “mã sai”, “mã bị ẩn”, “mã thuộc diện không công khai” ở giao diện công khai.

## 9.7. Bảng chức năng chi tiết

| Chức năng | Mô tả | Phạm vi hiển thị | Ghi chú |
|---|---|---|---|
| Tìm theo mã hồ sơ/văn bản | Người dùng nhập mã để tra cứu | Công khai tối giản | Tìm chính xác theo mã đã phát hành |
| Hiển thị trạng thái không nhạy cảm | Trả về trạng thái tổng quát của hồ sơ | Công khai tối giản | Chỉ dùng bộ trạng thái đã chuẩn hóa |
| Hiển thị ngày dự kiến hoàn thành | Cho phép người tra cứu biết mốc dự kiến | Công khai tối giản | Có thể để trống nếu chưa xác định |
| Chặn dữ liệu nhạy cảm | Ẩn hoàn toàn nội dung và thông tin nội bộ | Công khai tối giản | Áp dụng ở cả API và UI |
| Xử lý không tìm thấy | Hiển thị thông báo trung tính khi không có dữ liệu | Công khai tối giản | Tránh lộ thông tin qua thông báo lỗi |
| Bảo vệ endpoint tra cứu | Áp dụng captcha/rate limit/log truy cập | Công khai tối giản | Bắt buộc nếu mở public Internet |

---

# 10. Module P6 - Insight tự động

## 10.1. Mục tiêu

Sinh ra các câu tóm tắt tự động phục vụ báo cáo giao ban, giúp Lãnh đạo Bộ hoặc bộ phận tổng hợp nhanh chóng nắm được điểm nổi bật trong kỳ mà không cần tự diễn giải từ nhiều biểu đồ.

## 10.2. Nội dung insight đề xuất

Insight có thể phản ánh các ý sau:

- Tháng này tổng VB đến tăng X% so với tháng trước.
- Tỷ lệ đúng hạn toàn Bộ đạt Y%, tăng/giảm Z điểm %.
- Đơn vị A dẫn đầu về tỷ lệ đúng hạn.
- Đơn vị B cần lưu ý do tỷ lệ quá hạn vượt ngưỡng.
- Backlog toàn hệ thống đang tăng/giảm so với 3 tháng gần nhất.

## 10.3. Nguồn dữ liệu

Insight được sinh từ:

- DashboardKpiSnapshots
- UnitPerformanceSnapshots
- PortalAlerts

## 10.4. Cơ chế vận hành insight

- Có thể sinh theo batch định kỳ hoặc trigger thủ công sau khi dữ liệu được cập nhật.
- Insight sinh tự động cần có trạng thái `Draft` trước khi dùng chính thức.
- Cán bộ tổng hợp hoặc người được phân quyền có thể duyệt/chỉnh sửa nhẹ trước khi `Publish`.
- Có thể lưu nhiều phiên bản insight trong cùng tháng nếu cần tái sinh do thay đổi dữ liệu.

## 10.5. Bảng chức năng chi tiết

| Chức năng | Mô tả | Phạm vi hiển thị | Ghi chú |
|---|---|---|---|
| Sinh câu tóm tắt tự động | Tạo câu insight dựa trên snapshot và cảnh báo | Nội bộ cấp cao | Có thể sinh theo batch |
| Hiển thị insight tháng | Hiển thị insight nổi bật của tháng hiện tại hoặc tháng gần nhất | Nội bộ cấp cao | Có thể giới hạn 1-3 insight |
| Tái sử dụng cho báo cáo giao ban | Dùng text insight cho báo cáo, email, briefing | Nội bộ cấp cao | Nên lưu bản đã sinh thay vì sinh lại mọi lúc |
| Duyệt insight | Duyệt/publish insight trước khi dùng chính thức | Nội bộ cấp cao | Tránh dùng insight auto chưa kiểm chứng |
| Trigger sinh lại | Cho phép sinh lại insight khi dữ liệu thay đổi | Nội bộ cấp cao | Giữ version và audit |
| Lưu thời điểm sinh | Ghi nhận GeneratedAt để audit | Nội bộ cấp cao | Hữu ích khi đối soát dữ liệu |

---

# 11. Yêu cầu dữ liệu và tích hợp

## 11.1. Luồng xử lý dữ liệu đề xuất

1. Trích xuất dữ liệu từ bảng nghiệp vụ gốc theo lịch.
2. Chuẩn hóa dữ liệu theo Tenant và kỳ báo cáo.
3. Tính toán KPI tổng hợp.
4. Ghi snapshot vào các bảng aggregate.
5. Sinh cảnh báo nếu vượt rule.
6. Sinh insight tự động theo tháng.
7. Đồng bộ dữ liệu an toàn sang bảng tra cứu công khai.

## 11.2. Lưu ý tích hợp API

- API Portal nội bộ nên đọc chủ yếu từ bảng snapshot.
- API công khai chỉ đọc từ bảng PublicDocLookup.
- Không để API công khai truy cập trực tiếp bảng nghiệp vụ gốc.
- Cần có cơ chế cache ngắn hạn cho các màn hình ít thay đổi.
- Drill-down nội bộ phải đi qua service layer có kiểm tra phạm vi quyền.

---

# 12. Quy tắc phân quyền hiển thị

## 12.1. Portal nội bộ

- Lãnh đạo Bộ: xem toàn hệ thống.
- Lãnh đạo khối/đơn vị: chỉ xem phạm vi tenant được phân quyền.
- Bộ phận tổng hợp: xem theo phạm vi được cấu hình.
- Người duyệt insight: được thao tác trên `MonthlyInsights`.

## 12.2. Portal công khai

- Không yêu cầu đăng nhập đối với tra cứu cơ bản nếu quy định cho phép.
- Có thể bổ sung cơ chế captcha/rate limit để tránh lạm dụng tra cứu.
- Chỉ trả về trường dữ liệu được whitelist.

---

# 13. Phi chức năng

## 13.1. Hiệu năng

- Màn hình KPI và biểu đồ phải tải nhanh từ dữ liệu snapshot.
- Các truy vấn phổ biến phải có index phù hợp theo Tenant, Period, PeriodType.
- Bảng tra cứu công khai phải tối ưu cho tìm kiếm theo DocCode.

## 13.2. Khả năng mở rộng

- Có thể mở rộng thêm snapshot theo tuần hoặc năm trong tương lai.
- Có thể bổ sung thêm dimension theo loại văn bản, mức độ khẩn, nhóm đơn vị.
- Có thể tách rule cảnh báo thành engine riêng khi nhu cầu tăng.
- Có thể mở rộng export ra dashboard PDF/Excel theo mẫu báo cáo.

## 13.3. Kiểm toán dữ liệu

- Mọi bảng aggregate vẫn có audit fields đầy đủ.
- Có soft delete để đáp ứng yêu cầu kiểm soát dữ liệu.
- Cần lưu thời điểm sinh snapshot để hỗ trợ đối soát nếu số liệu thay đổi.
- Insight phải có lịch sử version và trạng thái publish.

---

# 14. Database DBML cho Dashboard Portal

> Lưu ý: DBML dưới đây dùng cho bảng aggregate/snapshot của Dashboard Portal, không phải bảng nghiệp vụ gốc. Thiết kế tuân theo convention của project: PK kiểu `uniqueidentifier`, có `TenantId`, audit fields, soft delete, tên bảng PascalCase, tên cột PascalCase.

```dbml
Project dashboard_portal {
  database_type: "SQLServer"
  Note: '''
    Dashboard Portal aggregate and snapshot schema for QLVB Bộ Tài chính.
    Tables in this schema are optimized for KPI, trend, ranking, alerts,
    public lookup and monthly insight generation.
  '''
}

Table DashboardKpiSnapshots [headercolor: #42A5F5] {
  DashboardKpiSnapshotId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  Period datetime [not null, note: 'Ngày đại diện cho kỳ snapshot']
  PeriodType varchar(20) [not null, note: 'day/month/quarter']
  TotalIncoming int [not null, default: 0]
  TotalOutgoing int [not null, default: 0]
  TotalSubmissions int [not null, default: 0]
  TotalTasks int [not null, default: 0]
  OnTimeRate decimal(5,2) [not null, default: 0]
  OverdueCount int [not null, default: 0]
  ProcessedCount int [not null, default: 0]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, Period, PeriodType) [unique, name: 'UX_DashboardKpiSnapshots_Tenant_Period_PeriodType']
    (TenantId, PeriodType, Period) [name: 'IX_DashboardKpiSnapshots_Tenant_PeriodType_Period']
  }

  Note: 'Lưu snapshot KPI tổng hợp toàn hệ thống hoặc theo tenant tại từng kỳ.'
}

Table UnitPerformanceSnapshots [headercolor: #66BB6A] {
  UnitPerformanceSnapshotId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  UnitId uniqueidentifier [not null, note: 'Tham chiếu tenant hoặc đơn vị được đánh giá']
  Period datetime [not null, note: 'Ngày đại diện cho kỳ snapshot']
  Category varchar(50) [not null, default: 'All', note: 'All/Incoming/Outgoing/Submission/Task']
  OnTimeCount int [not null, default: 0]
  OverdueCount int [not null, default: 0]
  PendingCount int [not null, default: 0]
  OnTimeRate decimal(5,2) [not null, default: 0]
  Rank int [not null, default: 0]
  PreviousRank int [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, UnitId, Period, Category) [unique, name: 'UX_UnitPerformanceSnapshots_Tenant_Unit_Period_Category']
    (TenantId, Period) [name: 'IX_UnitPerformanceSnapshots_Tenant_Period']
    (TenantId, Rank, Period) [name: 'IX_UnitPerformanceSnapshots_Tenant_Rank_Period']
  }

  Note: 'Lưu hiệu quả xử lý của từng đơn vị theo kỳ để phục vụ xếp hạng và so sánh.'
}

Table PortalAlerts [headercolor: #FFA726] {
  PortalAlertId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  AlertType varchar(50) [not null]
  Severity varchar(20) [not null, note: 'info/warning/critical']
  Title nvarchar(255) [not null]
  Description nvarchar(1000) [not null]
  UnitId uniqueidentifier [null]
  IsResolved bit [not null, default: 0]
  ResolvedAt datetime [null]
  ResolvedBy uniqueidentifier [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, IsResolved, Severity) [name: 'IX_PortalAlerts_Tenant_IsResolved_Severity']
    (TenantId, AlertType) [name: 'IX_PortalAlerts_Tenant_AlertType']
    (UnitId) [name: 'IX_PortalAlerts_UnitId']
  }

  Note: 'Lưu các cảnh báo điều hành phát sinh từ rule tổng hợp hoặc kiểm tra dữ liệu snapshot.'
}

Table PublicDocLookup [headercolor: #AB47BC] {
  PublicDocLookupId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  DocCode nvarchar(100) [not null]
  DocType nvarchar(50) [not null]
  ReceivedDate datetime [not null]
  Status nvarchar(50) [not null, note: 'Đang xử lý/Đã hoàn thành']
  ExpectedCompletionDate datetime [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, DocCode) [unique, name: 'UX_PublicDocLookup_Tenant_DocCode']
    (DocCode) [name: 'IX_PublicDocLookup_DocCode']
    (Status) [name: 'IX_PublicDocLookup_Status']
  }

  Note: 'Bảng tra cứu công khai chỉ chứa thông tin an toàn, không lưu nội dung nhạy cảm của văn bản.'
}

Table MonthlyInsights [headercolor: #26C6DA] {
  MonthlyInsightId uniqueidentifier [pk, not null]
  TenantId uniqueidentifier [not null]
  Month int [not null]
  Year int [not null]
  VersionNo int [not null, default: 1]
  Status varchar(20) [not null, default: 'Draft', note: 'Draft/Published/Archived']
  InsightText nvarchar(2000) [not null]
  GeneratedAt datetime [not null]
  PublishedAt datetime [null]
  CreatedAt datetime [not null]
  CreatedBy uniqueidentifier [not null]
  UpdatedAt datetime [not null]
  UpdatedBy uniqueidentifier [not null]
  IsDeleted bit [not null, default: 0]
  DeletedAt datetime [null]
  DeletedBy uniqueidentifier [null]

  Indexes {
    (TenantId, Month, Year, VersionNo) [unique, name: 'UX_MonthlyInsights_Tenant_Month_Year_VersionNo']
    (TenantId, Month, Year, Status) [name: 'IX_MonthlyInsights_Tenant_Month_Year_Status']
  }

  Note: 'Lưu insight tự động theo tháng để tái sử dụng cho giao ban và báo cáo tổng hợp.'
}

Ref: DashboardKpiSnapshots.TenantId > Tenants.TenantId
Ref: UnitPerformanceSnapshots.TenantId > Tenants.TenantId
Ref: PortalAlerts.TenantId > Tenants.TenantId
Ref: PublicDocLookup.TenantId > Tenants.TenantId
Ref: MonthlyInsights.TenantId > Tenants.TenantId

Ref: UnitPerformanceSnapshots.UnitId > Tenants.TenantId
Ref: PortalAlerts.UnitId > Tenants.TenantId

Ref: DashboardKpiSnapshots.CreatedBy > Staffs.StaffId
Ref: DashboardKpiSnapshots.UpdatedBy > Staffs.StaffId
Ref: DashboardKpiSnapshots.DeletedBy > Staffs.StaffId

Ref: UnitPerformanceSnapshots.CreatedBy > Staffs.StaffId
Ref: UnitPerformanceSnapshots.UpdatedBy > Staffs.StaffId
Ref: UnitPerformanceSnapshots.DeletedBy > Staffs.StaffId

Ref: PortalAlerts.CreatedBy > Staffs.StaffId
Ref: PortalAlerts.UpdatedBy > Staffs.StaffId
Ref: PortalAlerts.DeletedBy > Staffs.StaffId
Ref: PortalAlerts.ResolvedBy > Staffs.StaffId

Ref: PublicDocLookup.CreatedBy > Staffs.StaffId
Ref: PublicDocLookup.UpdatedBy > Staffs.StaffId
Ref: PublicDocLookup.DeletedBy > Staffs.StaffId

Ref: MonthlyInsights.CreatedBy > Staffs.StaffId
Ref: MonthlyInsights.UpdatedBy > Staffs.StaffId
Ref: MonthlyInsights.DeletedBy > Staffs.StaffId
```

---

# 15. Giải thích vai trò từng bảng DBML

| Bảng | Vai trò | Phục vụ module |
|---|---|---|
| DashboardKpiSnapshots | Bảng snapshot KPI tổng hợp toàn hệ thống hoặc theo tenant. Mỗi bản ghi đại diện cho một kỳ báo cáo (ngày/tháng/quý) của một tenant, lưu các chỉ số tổng hợp như tổng văn bản đến, đi, tờ trình, phiếu giao việc, tỷ lệ đúng hạn và số lượng quá hạn. Phục vụ hiển thị KPI nhanh, biểu đồ xu hướng và sinh insight tự động. | P1, P2, P6 |
| UnitPerformanceSnapshots | Bảng snapshot hiệu quả xử lý của từng đơn vị (tenant con) theo kỳ và loại nghiệp vụ. Mỗi bản ghi lưu số liệu đúng hạn, quá hạn, tồn đọng và thứ hạng của một đơn vị trong một kỳ. Phục vụ biểu đồ so sánh đơn vị, bảng xếp hạng, phát hiện cảnh báo điều hành và sinh insight tháng. | P2, P3, P4, P6 |
| PortalAlerts | Bảng lưu các cảnh báo điều hành được sinh tự động từ rule engine hoặc kiểm tra dữ liệu snapshot. Mỗi bản ghi là một cảnh báo cụ thể với loại, mức độ nghiêm trọng, mô tả và trạng thái xử lý. Phục vụ module cảnh báo điều hành và cung cấp ngữ cảnh cho insight tháng. | P4, P6 |
| PublicDocLookup | Bảng tra cứu công khai chỉ chứa các trường thông tin an toàn của văn bản/hồ sơ. Dữ liệu được đồng bộ có kiểm soát từ bảng nghiệp vụ gốc, loại bỏ hoàn toàn nội dung nhạy cảm. Phục vụ tính năng tra cứu công khai tối giản mà không để lộ dữ liệu nội bộ. | P5 |
| MonthlyInsights | Bảng lưu các câu tóm tắt insight tự động theo tháng, có hỗ trợ versioning và workflow duyệt (Draft → Published → Archived). Phục vụ tái sử dụng nội dung cho báo cáo giao ban, email tổng hợp và briefing lãnh đạo mà không cần sinh lại mỗi lần. | P6 |

---

# 15.1. Mô tả chi tiết các trường thông tin từng bảng

> Các trường base `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`, `DeletedAt`, `DeletedBy` không mô tả lại ở đây vì đã được chuẩn hóa theo convention chung của project.

## 15.1.1. DashboardKpiSnapshots

| Trường | Kiểu | Mục đích lưu |
|---|---|---|
| DashboardKpiSnapshotId | uniqueidentifier | PK định danh duy nhất bản ghi snapshot. |
| TenantId | uniqueidentifier | Xác định tenant (đơn vị/tổ chức) mà snapshot này thuộc về. Dùng để phân vùng dữ liệu và kiểm soát phạm vi hiển thị theo quyền. |
| Period | datetime | Ngày đại diện cho kỳ snapshot. Ví dụ: `2026-05-01` đại diện cho tháng 5/2026 khi `PeriodType = month`. |
| PeriodType | varchar(20) | Loại kỳ báo cáo: `day` / `month` / `quarter`. Dùng để lọc và nhóm dữ liệu khi hiển thị biểu đồ xu hướng. |
| TotalIncoming | int | Tổng số văn bản đến phát sinh trong kỳ. |
| TotalOutgoing | int | Tổng số văn bản đi phát sinh trong kỳ. |
| TotalSubmissions | int | Tổng số tờ trình phát sinh trong kỳ. |
| TotalTasks | int | Tổng số phiếu giao việc phát sinh trong kỳ. |
| OnTimeRate | decimal(5,2) | Tỷ lệ đúng hạn (%) tính theo công thức chuẩn hóa tại mục 5.3.1. Làm tròn 1 chữ số thập phân. |
| OverdueCount | int | Số hồ sơ/văn bản/công việc đang quá hạn tại thời điểm sinh snapshot. |
| ProcessedCount | int | Tổng số bản ghi đã hoàn thành trong kỳ. Dùng làm mẫu số khi tính `OnTimeRate` và hiển thị khối lượng xử lý. |

## 15.1.2. UnitPerformanceSnapshots

| Trường | Kiểu | Mục đích lưu |
|---|---|---|
| UnitPerformanceSnapshotId | uniqueidentifier | PK định danh duy nhất bản ghi. |
| TenantId | uniqueidentifier | Tenant cha (cấp Bộ hoặc cấp trên) dùng để phân vùng dữ liệu và kiểm soát phạm vi xem theo quyền. |
| UnitId | uniqueidentifier | Tham chiếu đến tenant/đơn vị được đánh giá trong kỳ này (FK đến `Tenants`). Là đơn vị con trong cây tenant. |
| Period | datetime | Ngày đại diện cho kỳ snapshot. Kết hợp với `TenantId`, `UnitId`, `Category` tạo thành khóa duy nhất. |
| Category | varchar(50) | Loại nghiệp vụ được tổng hợp: `All` / `Incoming` / `Outgoing` / `Submission` / `Task`. Cho phép xem xếp hạng riêng theo từng loại nghiệp vụ. |
| OnTimeCount | int | Số bản ghi hoàn thành đúng hạn trong kỳ. Là tử số của công thức tỷ lệ đúng hạn. |
| OverdueCount | int | Số bản ghi đang quá hạn tại thời điểm sinh snapshot. Dùng để phát hiện cảnh báo `OverdueRateHigh`. |
| PendingCount | int | Số bản ghi đang chờ xử lý (backlog) tại thời điểm sinh snapshot. Dùng để phát hiện cảnh báo `BacklogSpike`. |
| OnTimeRate | decimal(5,2) | Tỷ lệ đúng hạn (%) của đơn vị trong kỳ. Là chỉ số chính để xếp hạng. |
| Rank | int | Thứ hạng của đơn vị trong kỳ này, tính theo `OnTimeRate` và tiêu chí phụ (xem mục 7.3). |
| PreviousRank | int | Thứ hạng của đơn vị trong kỳ trước. Dùng để hiển thị xu hướng tăng/giảm hạng trên bảng xếp hạng. |

## 15.1.3. PortalAlerts

| Trường | Kiểu | Mục đích lưu |
|---|---|---|
| PortalAlertId | uniqueidentifier | PK định danh duy nhất cảnh báo. |
| TenantId | uniqueidentifier | Tenant mà cảnh báo này thuộc về. Dùng để lọc cảnh báo theo phạm vi quyền của người xem. |
| AlertType | varchar(50) | Loại cảnh báo theo danh sách chuẩn: `OverdueRateHigh` / `BacklogSpike` / `MissingUpdate` / `SubmissionStuck` / `NoActivity`. Dùng để nhóm và lọc cảnh báo. |
| Severity | varchar(20) | Mức độ nghiêm trọng: `info` / `warning` / `critical`. Dùng để sắp xếp ưu tiên hiển thị và tô màu cảnh báo. |
| Title | nvarchar(255) | Tiêu đề ngắn gọn của cảnh báo, hiển thị trực tiếp trên danh sách điều hành. |
| Description | nvarchar(1000) | Mô tả chi tiết nguyên nhân và ngữ cảnh của cảnh báo. Dùng khi người xem cần hiểu rõ hơn về vấn đề. |
| UnitId | uniqueidentifier | Đơn vị liên quan đến cảnh báo (nullable). Một số cảnh báo có thể ở cấp toàn hệ thống nên không gắn với đơn vị cụ thể. |
| IsResolved | bit | Cờ đánh dấu cảnh báo đã được xem/xử lý ở cấp quản trị. Không thay thế xử lý nghiệp vụ thực tế. |
| ResolvedAt | datetime | Thời điểm cảnh báo được đánh dấu đã xử lý. Dùng để audit và thống kê thời gian phản hồi. |
| ResolvedBy | uniqueidentifier | Người đã đánh dấu cảnh báo là đã xử lý (FK đến `Staffs`). |

## 15.1.4. PublicDocLookup

| Trường | Kiểu | Mục đích lưu |
|---|---|---|
| PublicDocLookupId | uniqueidentifier | PK định danh duy nhất bản ghi tra cứu. |
| TenantId | uniqueidentifier | Tenant sở hữu văn bản/hồ sơ này. Dùng để phân vùng dữ liệu và hỗ trợ tìm kiếm theo đơn vị nếu cần. |
| DocCode | nvarchar(100) | Mã tra cứu công khai của văn bản/hồ sơ. Là trường tìm kiếm chính khi người dùng nhập mã để tra cứu. |
| DocType | nvarchar(50) | Loại văn bản/hồ sơ ở mức tổng quát, an toàn để công khai. Không tiết lộ phân loại nội bộ nhạy cảm. |
| ReceivedDate | datetime | Ngày tiếp nhận hoặc ngày ghi nhận văn bản/hồ sơ. Hiển thị cho người tra cứu để xác nhận đúng hồ sơ. |
| Status | nvarchar(50) | Trạng thái xử lý tổng quát theo bộ giá trị công khai: `Đang xử lý` / `Đã hoàn thành`. Không dùng trạng thái nội bộ chi tiết. |
| ExpectedCompletionDate | datetime | Ngày dự kiến hoàn thành. Hiển thị cho người tra cứu nếu phù hợp công khai; để null nếu chưa xác định hoặc không phù hợp công bố. |

## 15.1.5. MonthlyInsights

| Trường | Kiểu | Mục đích lưu |
|---|---|---|
| MonthlyInsightId | uniqueidentifier | PK định danh duy nhất bản ghi insight. |
| TenantId | uniqueidentifier | Tenant mà insight này thuộc về. Dùng để lọc insight theo phạm vi quyền. |
| Month | int | Tháng của kỳ báo cáo (1–12). Kết hợp với `Year` xác định kỳ insight. |
| Year | int | Năm của kỳ báo cáo. |
| VersionNo | int | Số phiên bản insight trong cùng tháng/năm, bắt đầu từ 1 và tăng mỗi lần sinh lại. Cho phép lưu nhiều phiên bản khi dữ liệu thay đổi. |
| Status | varchar(20) | Trạng thái workflow: `Draft` (mới sinh, chưa duyệt) / `Published` (đã duyệt, dùng chính thức) / `Archived` (đã thay thế bởi version mới hơn). |
| InsightText | nvarchar(2000) | Nội dung câu tóm tắt insight, thường gồm nhiều câu mô tả các điểm nổi bật của tháng. Dùng trực tiếp cho báo cáo giao ban, email, briefing. |
| GeneratedAt | datetime | Thời điểm insight được sinh ra (bởi job tự động hoặc trigger thủ công). Dùng để audit và đối soát khi dữ liệu thay đổi sau khi sinh. |
| PublishedAt | datetime | Thời điểm insight được duyệt và chuyển sang trạng thái `Published`. Dùng để xác định insight nào đang có hiệu lực chính thức. |

---

# 16. Quy tắc triển khai đề xuất

## 16.1. ETL / Job snapshot

Nên có các job nền riêng:

1. Job tổng hợp KPI ngày/tháng/quý
2. Job tổng hợp hiệu suất đơn vị
3. Job phát hiện cảnh báo
4. Job đồng bộ tra cứu công khai
5. Job sinh insight tháng
6. Job xuất snapshot PDF/Excel nếu cần cache báo cáo định kỳ

## 16.2. Trình tự ưu tiên MVP

Nếu triển khai theo giai đoạn, nên ưu tiên:

- Giai đoạn 1: P1 + P2 + P5
- Giai đoạn 2: P3 + P4
- Giai đoạn 3: P6 + Export

Lý do:

- P1 và P2 tạo được giá trị tổng quan tức thì
- P5 phục vụ mục tiêu minh bạch cơ bản
- P3 và P4 cần dữ liệu snapshot ổn định hơn
- P6 phụ thuộc vào chất lượng dữ liệu tổng hợp và rule diễn giải

---

# 17. Kết luận

Dashboard Portal của hệ thống QLVB Bộ Tài chính cần được định vị là lớp hiển thị tổng hợp và báo cáo cấp cao, không phải màn hình vận hành chi tiết. Thiết kế nên bám theo nguyên tắc:

- Tổng hợp rõ ràng
- Dễ theo dõi xu hướng
- Có cảnh báo điều hành
- Công khai có kiểm soát
- Drill-down theo quyền
- Tối ưu bằng dữ liệu snapshot
- Có khả năng xuất báo cáo và tái sử dụng insight