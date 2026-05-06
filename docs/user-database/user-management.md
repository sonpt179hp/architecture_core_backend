# Đặc tả yêu cầu / Tài liệu chức năng nghiệp vụ — Quản lý người dùng

| Thuộc tính | Giá trị |
|------------|---------|
| **Hệ thống** | Hệ thống QLVB (tham chiếu mẫu SRS nội bộ) |
| **Module** | Quản lý người dùng (USER MANAGEMENT) |
| **Phiên bản tài liệu** | 1.4 |
| **Ngày** | 06/05/2026 |
| **Trạng thái** | Draft |

> **Nguồn nghiệp vụ:** Thiết kế chức năng Quản lý người dùng.
> **Cấu trúc tài liệu:** Bổ sung theo template SRS QLVB (trang thông tin, lịch sử thay đổi, mục lục, tổng quan, yêu cầu chung, đặc tả chức năng, yêu cầu phi chức năng, phụ lục).
> **Phạm vi:** Quy tắc nghiệp vụ, luồng xử lý, giao diện người dùng cuối — **không** mô tả chi tiết CSDL / DDD.

---

## Lịch sử thay đổi tài liệu

| Phiên bản | Ngày | Người thực hiện | Mô tả thay đổi |
|-----------|------|-----------------|----------------|
| 1.2 | 05/05/2026 | — | Phiên bản khởi tạo |
| 1.3 | 06/05/2026 | — | Bổ sung §8: Avatar, Thùng rác, Export theo lựa chọn, Phê duyệt tự chỉnh sửa |
| 1.4 | 06/05/2026 | — | Bổ sung §8.6: Rà soát hồ sơ (Profile Review) |

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
   1.1. [Mục đích tài liệu](#11-mục-đích-tài-liệu)
   1.2. [Phạm vi](#12-phạm-vi)
   1.3. [Định nghĩa và viết tắt](#13-định-nghĩa-và-viết-tắt)
   1.4. [Tài liệu tham chiếu](#14-tài-liệu-tham-chiếu)
2. [Yêu cầu chung](#2-yêu-cầu-chung)
   2.1. [Mô tả tổng quan phân hệ](#21-mô-tả-tổng-quan-phân-hệ)
   2.2. [Người dùng hệ thống và vai trò](#22-người-dùng-hệ-thống-và-vai-trò)
   2.3. [Luồng nghiệp vụ tổng quát](#23-luồng-nghiệp-vụ-tổng-quát)
   2.4. [Quy tắc nghiệp vụ chung](#24-quy-tắc-nghiệp-vụ-chung)
3. [Đặc tả chức năng — Quản trị](#3-đặc-tả-chức-năng--quản-trị)
   3.1. [Màn hình "Quản lý người dùng" (danh sách)](#31-màn-hình-quản-lý-người-dùng-danh-sách)
   3.2. [Màn hình "Thông tin tài khoản" & "Phân quyền" (sửa người dùng)](#32-màn-hình-thông-tin-tài-khoản--phân-quyền-sửa-người-dùng)
   3.3. [Nghiệp vụ đồng bộ & cấu hình hệ thống (Action)](#33-nghiệp-vụ-đồng-bộ--cấu-hình-hệ-thống-action)
   3.4. [Danh mục tham chiếu](#34-danh-mục-tham-chiếu)
4. [Ảnh hưởng phía người dùng được quản lý](#4-ảnh-hưởng-phía-người-dùng-được-quản-lý)
5. [Yêu cầu phi chức năng](#5-yêu-cầu-phi-chức-năng)
6. [Phụ lục](#6-phụ-lục)
7. [Danh mục trường dữ liệu (Fields) theo UI](#7-danh-mục-trường-dữ-liệu-fields-theo-ui)
8. [Tính năng mới (New Features)](#8-tính-năng-mới-new-features)
   8.1. [Avatar người dùng](#81-avatar-người-dùng)
   8.2. [Thùng rác (Trash / Soft-delete nâng cao)](#82-thùng-rác-trash--soft-delete-nâng-cao)
   8.3. [Xuất khẩu (Export) theo bản ghi đã chọn](#83-xuất-khẩu-export-theo-bản-ghi-đã-chọn)
   8.4. [Luồng phê duyệt khi người dùng tự sửa thông tin](#84-luồng-phê-duyệt-khi-người-dùng-tự-sửa-thông-tin)
   8.5. [Tổng hợp mã hiệu tính năng mới](#85-tổng-hợp-mã-hiệu-tính-năng-mới)
   8.6. [Rà soát hồ sơ (Profile Review)](#86-rà-soát-hồ-sơ-profile-review)

---

## 1. Tổng quan

### 1.1. Mục đích tài liệu

Tài liệu dùng để **thống nhất hiểu biết** đội phân tích, thiết kế và lập trình về phân hệ **Quản lý người dùng**: phạm vi chức năng, luồng xử lý, ràng buộc nghiệp vụ.

### 1.2. Phạm vi

Phân hệ cho phép quản trị viên **tạo, tra cứu, cập nhật, vô hiệu hóa** tài khoản người dùng; **gán quyền theo tổ chức**; **cấu hình chữ ký số**, **phân quyền thao tác (action)**, và **danh sách người nhận trình/chuyển xử lý** theo loại văn bản; đồng thời **nhập/xuất** danh sách người dùng từ Excel.

**Ba nhóm nghiệp vụ chính:**

| Nhóm | Mô tả ngắn |
|------|------------|
| **Định danh & tài khoản** | Thông tin cá nhân, đăng nhập, khóa/mở khóa, đặt lại mật khẩu |
| **Phân quyền & luồng** | Dòng quyền (đơn vị–vai trò–phòng ban–chức vụ), action tùy chỉnh, trình/chuyển đến ai |
| **Danh mục phục vụ** | Đơn vị, phòng ban, vai trò, chức vụ, tìm người để gán làm đích chuyển |

### 1.3. Định nghĩa và viết tắt

| Thuật ngữ | Giải thích |
|-----------|------------|
| **Dòng phân quyền** | Một bản ghi gắn người dùng với bộ **Đơn vị + Vai trò + Phòng ban + Chức vụ** (phòng ban/chức vụ có thể tuỳ theo ràng buộc danh mục). |
| **Action / Hành động** | Quyền thao tác chi tiết (module/menu/màn hình/nút…) trong hệ thống. |
| **Action mặc định của vai trò** | Tập action gán cho **vai trò**; dòng phân quyền **không tùy chỉnh** sẽ kế thừa tập này. |
| **Xóa mềm** | Đánh dấu người dùng **đã xóa**, không xóa vật lý bản ghi. |
| **Thùng rác** | Khu vực chứa các bản ghi đã xóa mềm; hỗ trợ khôi phục hoặc xoá vĩnh viễn. |
| **Trình / Chuyển xử lý** | Danh sách người nhận đích khi trình hoặc chuyển xử lý, **phân theo loại văn bản** trên từng dòng phân quyền. |
| **Yêu cầu chỉnh sửa** | Bản ghi lưu thay đổi thông tin do người dùng tự gửi, chờ quản trị viên phê duyệt trước khi có hiệu lực. |
| **Hồ sơ chưa hoàn thiện** | Tài khoản còn thiếu một hoặc nhiều trường/cấu hình theo tiêu chí rà soát định sẵn. |

### 1.4. Tài liệu tham chiếu

---

## 2. Yêu cầu chung

### 2.1. Mô tả tổng quan phân hệ

Phân hệ phục vụ duy trì hồ sơ người dùng, trạng thái tài khoản, và các **phân quyền** kèm tuỳ chọn action và danh sách Trình/Chuyển.

### 2.3. Luồng nghiệp vụ tổng quát

1. **Onboarding:** Quản trị tạo người dùng (hoặc import) → gán đơn vị cơ sở → thêm các **dòng phân quyền** → (tuỳ chọn) tinh chỉnh **action** từng dòng → (tuỳ chọn) cấu hình **Trình/Chuyển** theo loại văn bản → cấu hình **chữ ký số** nếu cần.
2. **Vận hành:** Khóa/mở khóa, đặt lại mật khẩu khi có yêu cầu; cập nhật thông tin cá nhân khi thay đổi tổ chức.
3. **Offboarding:** Xóa mềm khi không còn sử dụng → bản ghi chuyển vào **Thùng rác**; quản trị viên có thể khôi phục hoặc xoá vĩnh viễn.
4. **Rà soát chất lượng hồ sơ:** Quản trị định kỳ chạy **rà soát hồ sơ** → xem thống kê hồ sơ chưa hoàn thiện → gửi thông báo yêu cầu bổ sung.

### 2.4. Quy tắc nghiệp vụ chung

| Quy tắc | Chi tiết |
|---------|----------|
| **Tên đăng nhập** | Sau khi tạo tài khoản, **không cho phép đổi** (trường **read-only/disabled** trên form sửa). |
| **Xóa người dùng** | Chỉ **xóa mềm**, bản ghi chuyển vào **Thùng rác**; không xóa vật lý khỏi hệ thống trừ khi thực hiện "Xoá vĩnh viễn" từ Thùng rác. |
| **Khóa tài khoản** | **Khóa/mở khóa** độc lập với cờ xóa mềm; khi **đã xóa**, không thao tác khóa trên UI. |
| **Email / SĐT** | Email **bắt buộc và duy nhất**; SĐT **bắt buộc**. |
| **Đơn vị cơ sở** | Mỗi người dùng gắn **một** đơn vị cơ sở. |
| **Tự chỉnh sửa** | Người dùng tự sửa thông tin cá nhân → **chờ phê duyệt** trước khi có hiệu lực (§8.4). |

---

## 3. Đặc tả chức năng — Quản trị

### 3.1. Màn hình "Quản lý người dùng" (danh sách)

#### 3.1.1. Giao diện & điều khiển (theo UI)

- **Tiêu đề:** "Quản lý người dùng".
- **Nút chức năng trên:** **Import**, **Export**, **Thùng rác**, **Rà soát hồ sơ**, **Thêm mới**.
- **Tìm kiếm:** Ô nhập với placeholder *"Tìm kiếm theo họ tên, tên đăng nhập, số điện thoại, chức vụ"* + nút **Tìm kiếm**.
- **Lọc:**
  - **Trạng thái** (dropdown, có xoá lựa chọn).
  - **Đơn vị** (select có tìm kiếm, cho phép clear — ví dụ chọn đơn vị cấp trên như "Bộ Tài Chính").
- **Bảng dữ liệu:**
  - Chọn nhiều (**checkbox** cột đầu + chọn tất cả).
  - **Hiển thị cột:** nút **Cột** (cấu hình hiển thị cột).
  - **Tiêu đề cột:** STT; Avatar; Họ và tên; Tên đăng nhập; Email; Phòng ban - chức vụ; Số điện thoại; Giới tính; Đã khoá; Đã xoá; **Thao tác** (cố định bên phải).
  - **Đã khoá:** điều khiển dạng **switch** trên từng dòng (có thể **disabled** theo quyền).
  - **Thao tác:** nút **Sửa**, **Xóa** (icon).
- **Phân trang:** chọn **số bản ghi/trang** (ví dụ 20/trang), điều hướng trang.

#### 3.1.2. Bảng chức năng (mã hiệu)

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **US-LIST-01** | Xem danh sách có phân trang | Hiển thị danh sách người dùng; **tìm kiếm**, **lọc** theo trạng thái, đơn vị; cấu hình **cột** hiển thị. |
| **US-LIST-02** | Thêm người dùng mới | Tạo tài khoản với **thông tin cơ bản**; có thể bổ sung **phân quyền** trên luồng màn hình tạo mới. |
| **US-LIST-03** | Xóa (mềm) → Thùng rác | Đánh dấu **đã xóa**; bản ghi chuyển vào **Thùng rác** (§8.2); dữ liệu lịch sử vẫn lưu. |
| **US-LIST-04** | Khóa / Mở khóa | Bật/tắt **khóa đăng nhập** từ danh sách khi không vi phạm quy tắc "đã xóa". |
| **US-LIST-05** | Import Excel | Tạo **hàng loạt** người dùng theo **mẫu quy định**; xử lý lỗi/báo cáo theo chính sách triển khai. |
| **US-LIST-06** | Export Excel | Xuất file Excel; nếu có dòng được chọn thì chỉ xuất dòng đó (§8.3); nếu không chọn thì xuất theo bộ lọc hiện tại. |

---

### 3.2. Màn hình "Thông tin tài khoản" & "Phân quyền" (sửa người dùng)

#### 3.2.1. Form "Thông tin tài khoản" (theo UI)

| Nhóm | Trường / điều khiển | Ghi chú UI |
|------|---------------------|------------|
| **Avatar** | Ảnh đại diện | Thumbnail + nút **Tải ảnh lên** / **Xoá ảnh** (§8.1). |
| **Nhận diện** | Họ *, Tên *, Tên đăng nhập * | Tên đăng nhập **disabled** khi sửa. |
| **Liên hệ** | Số điện thoại *, Email * | — |
| **Cá nhân** | Giới tính * | Select (vd: Nam/Nữ). |
| **Khác** | Số thứ tự | Input số (min 0), có nút tăng/giảm. |
| **Định danh mở rộng** | Mã số CCCD, Ngày sinh, Địa chỉ | Tuỳ chọn (không đánh dấu * trên UI mẫu). |
| **Tổ chức** | Đơn vị cơ sở | Select có **search** và **clear**. |
| **Chữ ký số** | Loại chữ ký số | **Token** (checkbox) + **Sim PKI** (checkbox); mỗi loại có **radio "Đặt làm mặc định"** (chỉ một default toàn form); Sim PKI có ô **"Nhập số điện thoại sim PKI"**. |
| **Trạng thái** | Trạng thái khóa * | Switch — nhãn trạng thái kèm theo (vd: **Đã mở khoá**). |
| | Trạng thái tài khoản * | Switch — nhãn **Hoạt động** / tương đương cho trạng thái không xóa vs đã xóa (theo mapping nghiệp vụ). |
| **Bảo mật** | Đặt lại mật khẩu | Nút **Đặt lại** — mở luồng nhập **mật khẩu mới** (form/popup theo thiết kế). |

#### 3.2.2. Khối "Phân quyền"

- **Tiêu đề:** "Phân quyền".
- **Lưới mỗi dòng:** cột **Đơn vị**, **Vai trò**, **Phòng ban**, **Chức vụ** — các ô **select có tìm kiếm** (tuỳ cột).
- **Cột thao tác dòng (icon):**
  - **Quyền**: mở cấu hình **Action** cho dòng (US-EDIT-04).
  - **Nhóm/người nhận** (icon team): mở chọn **danh sách Trình/Chuyển xử lý** (US-EDIT-05).
  - **Xóa dòng** (icon delete).
- **Khu vực dưới mỗi dòng:** nhãn **"Danh sách Trình / Chuyển xử lý:"** + **Loại:** + danh sách **thẻ người dùng** (họ tên + dòng phụ chức danh/đơn vị) và nút **xoá** từng người.

#### 3.2.3. Bảng chức năng (mã hiệu)

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **US-EDIT-01** | Cập nhật thông tin cá nhân | Sửa họ, tên, SĐT, email, giới tính, ngày sinh, địa chỉ, CCCD, **số thứ tự**, đơn vị cơ sở. **Không** sửa tên đăng nhập. Quản trị sửa người khác → áp dụng ngay; người dùng tự sửa → qua phê duyệt (§8.4). |
| **US-EDIT-02** | Quản lý chữ ký số | Bật **Token** và/hoặc **Sim PKI**; **chỉ một** loại được **mặc định** tại một thời điểm; Sim PKI **bắt buộc có SĐT sim** khi kích hoạt Sim. |
| **US-EDIT-03** | Phân quyền theo dòng | Mỗi dòng: **Đơn vị + Vai trò + Phòng ban + Chức vụ**; thêm/sửa/xóa nhiều dòng. **Không trùng** tổ hợp **cùng người + đơn vị + phòng ban + vai trò**. |
| **US-EDIT-04** | Action trên từng dòng | Tùy chỉnh tập **hành động**; không tùy chỉnh thì **kế thừa action mặc định của vai trò**; có tùy chỉnh thì chỉ áp dụng tập đã lưu cho dòng đó. |
| **US-EDIT-05** | Danh sách Trình / Chuyển xử lý | Cấu hình người dùng **theo loại văn bản** trên từng dòng. |
| **US-EDIT-06** | Đặt lại mật khẩu | Hiển thị luồng nhập **mật khẩu mới** và áp dụng cho tài khoản (theo chính sách độ phức tạp mật khẩu). |

---

### 3.3. Nghiệp vụ đồng bộ & cấu hình hệ thống (Action)

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **ACT-SYNC-01** | Đồng bộ cây Action | Cây hành động đồng bộ từ **nguồn hệ thống**; không tạo thủ công tùy tiện. |

---

### 3.4. Danh mục tham chiếu

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **REF-01** | Đơn vị | Chọn trong đơn vị cơ sở & cột Đơn vị trên dòng phân quyền. |
| **REF-02** | Phòng ban | Danh mục **theo đơn vị** khi gán dòng phân quyền. |
| **REF-03** | Vai trò, chức vụ | Tra cứu vai trò, chức vụ (có thể lọc đang hoạt động). |
| **REF-04** | Tìm người để chuyển xử lý | Tìm người để thêm vào danh sách Trình/Chuyển. |

---

## 4. Ảnh hưởng phía người dùng được quản lý

| Tình huống | Hệ quả mong đợi |
|------------|-----------------|
| **Khóa tài khoản** | Người dùng **không đăng nhập** được (hoặc bị chặn phiên). |
| **Đặt lại mật khẩu** | Mật khẩu cập nhật theo giá trị quản trị nhập; người dùng đăng nhập bằng mật khẩu mới sau khi có hiệu lực. |
| **Yêu cầu chỉnh sửa được duyệt** | Thông tin mới có hiệu lực ngay; người dùng nhận thông báo. |
| **Yêu cầu chỉnh sửa bị từ chối** | Thông tin giữ nguyên giá trị cũ; người dùng nhận thông báo kèm lý do (nếu có). |
| **Nhận thông báo rà soát hồ sơ** | Người dùng nhận yêu cầu bổ sung thông tin còn thiếu; đăng nhập và cập nhật hồ sơ theo hướng dẫn. |

---

## 5. Yêu cầu phi chức năng

| Mã | Yêu cầu | Mô tả |
|----|---------|--------|
| **NFR-01** | Hiệu năng danh sách | Danh sách người dùng phải **phân trang**; thao tác lọc/tìm không làm treo giao diện với khối lượng dữ liệu lớn (tối ưu truy vấn phía server). |
| **NFR-02** | Nhật ký thao tác quản trị | Các thao tác nhạy cảm (khóa, xóa mềm, xóa vĩnh viễn, khôi phục, đặt lại MK, đổi phân quyền/action, phê duyệt/từ chối chỉnh sửa, gửi thông báo rà soát) cần **ghi nhật ký** phục vụ kiểm tra an ninh. |
| **NFR-03** | Kiểm soát truy cập | Chỉ người có **Action** tương ứng mới vào được màn hình và thực hiện nút chức năng. |
| **NFR-04** | Dữ liệu nhạy cảm | Tránh hiển thị lộ thông tin không cần thiết trong toast/log (đặc biệt quanh đặt lại mật khẩu). |
| **NFR-05** | Import an toàn | Giới hạn kích thước file, định dạng, và **báo lỗi theo dòng** khi import hàng loạt. |
| **NFR-06** | Upload avatar an toàn | Kiểm tra định dạng, kích thước file avatar phía server; không cho phép upload file thực thi hoặc nội dung độc hại. |
| **NFR-07** | Thùng rác — tự động dọn | Có thể cấu hình thời hạn lưu giữ bản ghi trong thùng rác (mặc định đề xuất 90 ngày); cần xác nhận ngưỡng trước khi triển khai. |
| **NFR-08** | Gửi thông báo hàng loạt — hiệu năng | Gửi thông báo rà soát hồ sơ hàng loạt phải xử lý **bất đồng bộ** (async/queue); không chặn giao diện; hiển thị trạng thái tiến trình. |

---

## 6. Phụ lục

---

## 7. Danh mục trường dữ liệu (Fields) theo UI

### 7.1. Màn "Quản lý người dùng" — tra cứu & điều khiển

| Field | Mô tả | Bắt buộc | Ghi chú / Rule |
|-------|--------|----------|----------------|
| **Từ khóa tìm kiếm** | Ô tìm theo họ tên, tên đăng nhập, SĐT, chức vụ | Không | Placeholder UI: *"Tìm kiếm theo họ tên, tên đăng nhập, số điện thoại, chức vụ"*; kết hợp nút **Tìm kiếm** (submit tra cứu). |
| **Trạng thái** | Lọc theo trạng thái người dùng (composite trên UI: có thể gồm khóa/xóa hoặc nhóm trạng thái khác) | Không | Select có **clear** (xoá lọc). Giá trị cụ thể theo cấu hình backend. |
| **Đơn vị** | Lọc người dùng thuộc đơn vị trong cây tổ chức | Không | Select có **search** và **clear** (vd: "Bộ Tài Chính"). |
| **Import** | Nút nhập Excel | — | Không phải field dữ liệu; kích hoạt upload file mẫu (US-LIST-05). |
| **Export** | Nút xuất Excel | — | Xuất theo bản ghi đã chọn (nếu có) hoặc xuất theo filter hiện tại (§8.3). |
| **Thêm mới** | Nút tạo người dùng | — | Điều hướng/form tạo (US-LIST-02). |
| **Thùng rác** | Nút mở màn hình Thùng rác | — | Điều hướng sang màn Thùng rác (§8.2). |
| **Rà soát hồ sơ** | Nút mở màn hình Rà soát hồ sơ | — | Điều hướng sang màn Rà soát hồ sơ (§8.6). |
| **Cột** | Cấu hình hiển thị cột bảng | — | Ảnh hưởng view, không lưu vào bản ghi người dùng (tuỳ có lưu preference hay không). |

### 7.2. Màn "Quản lý người dùng" — bảng danh sách & phân trang

| Field | Mô tả | Bắt buộc | Ghi chú / Rule |
|-------|--------|----------|----------------|
| **Chọn** | Checkbox chọn một/nhiều dòng; header chọn tất cả | Không | Phục vụ Export theo lựa chọn (§8.3) và thao tác hàng loạt. |
| **STT** | Số thứ tự dòng trên trang hiện tại | — | Hiển thị (read-only). |
| **Avatar** | Ảnh đại diện người dùng dạng thumbnail | — | Read-only trên lưới; hiển thị placeholder nếu chưa có ảnh; bật/tắt qua cấu hình cột (§8.1). |
| **Họ và tên** | Họ tên hiển thị của người dùng | — | Read-only trên lưới; có thể ghép từ Họ + Tên ở backend. |
| **Tên đăng nhập** | Username đăng nhập | — | Read-only trên lưới. |
| **Email** | Email người dùng | — | Read-only trên lưới; rule **duy nhất** toàn hệ thống (§2.4). |
| **Phòng ban - chức vụ** | Một hoặc nhiều dòng phân quyền (text gộp) | — | Read-only; có thể rỗng nếu chưa gán. |
| **Số điện thoại** | SĐT liên hệ | — | Read-only; rule **bắt buộc** khi sửa (§2.4). |
| **Giới tính** | Nam / Nữ (hoặc mã tương đương) | — | Read-only trên lưới. |
| **Đã khoá** | Trạng thái khóa đăng nhập | — | **Switch** inline; **disabled** khi **đã xóa** hoặc theo quyền/tài khoản đặc biệt. |
| **Đã xoá** | Trạng thái xóa mềm | — | **Switch** inline; bật = đánh dấu đã xóa, bản ghi vào Thùng rác (US-LIST-03). |
| **Thao tác — Sửa** | Mở màn/form chi tiết người dùng | — | Icon nút. |
| **Thao tác — Xóa** | Xóa mềm / đưa vào Thùng rác | — | Icon nút; xác nhận theo UX; bản ghi không mất vĩnh viễn. |
| **Kích thước trang** | Số bản ghi / trang | Không | Select (vd: 20/trang). |
| **Phân trang** | Trang hiện tại / tổng số bản ghi | — | Điều hướng trang; nhãn tổng theo ngữ cảnh **người dùng**. |

### 7.3. Form "Thông tin tài khoản" (sửa người dùng)

| Field | `id` UI (tham chiếu) | Mô tả | Bắt buộc | Ghi chú / Rule |
|-------|----------------------|--------|----------|----------------|
| **Avatar** | `AvatarUrl` | Ảnh đại diện | Không | Upload + crop; placeholder khi null; xem §8.1. |
| **Họ** | `FirstName` | Họ người dùng | **Có** (`*`) | Text. |
| **Tên** | `LastName` | Tên người dùng | **Có** (`*`) | Text. |
| **Tên đăng nhập** | `Username` | Username | **Có** (`*`) | **Disabled** khi sửa; không đổi sau khởi tạo (§2.4). |
| **Số điện thoại** | `Mobile` | SĐT liên hệ | **Có** (`*`) | Text; có thể thêm rule định dạng số VN ở tầng kỹ thuật. |
| **Email** | `Email` | Email | **Có** (`*`) | **Duy nhất** trong hệ thống (§2.4). |
| **Giới tính** | `Gender` | Giới tính | **Có** (`*`) | Select (vd: Nam/Nữ). |
| **Số thứ tự** | `DisplayOrder` | Thứ tự hiển thị / sắp xếp nội bộ | Không | `InputNumber`, `min = 0`, bước 1. |
| **Mã số CCCD** | `IDCard` | Căn cước / định danh | Không | Text; tooltip nhãn "Mã số CCCD". |
| **Ngày sinh** | `BirthOfDay` | Ngày sinh | Không | Date picker; placeholder *"Lựa chọn ngày sinh"*. |
| **Địa chỉ** | `Address` | Địa chỉ liên hệ | Không | Text. |
| **Đơn vị cơ sở** | `UnitId` | Đơn vị gốc của người dùng | Cần thống nhất (§2.4 yêu cầu một đơn vị cơ sở) | Select **search + clear**; giá trị từ danh mục đơn vị. |
| **Token — bật** | (checkbox nhóm `SignType`, value `1`) | Cho phép ký bằng Token | Không | Checkbox độc lập với Sim PKI; có thể bật cả hai. |
| **Token — mặc định** | (radio cùng nhóm) | Đặt Token làm chữ ký mặc định | Tuỳ | **Chỉ một** trong hai loại (Token hoặc Sim PKI) được **mặc định** tại một thời điểm (US-EDIT-02). |
| **Sim PKI — bật** | (checkbox value `2`) | Cho phép ký bằng Sim PKI | Không | — |
| **Số điện thoại sim PKI** | (input kề Sim PKI) | MSISDN gắn Sim ký số | **Khi bật Sim PKI** | Bắt buộc nhập khi Sim PKI được tick (US-EDIT-02). |
| **Sim PKI — mặc định** | (radio cùng nhóm) | Đặt Sim làm chữ ký mặc định | Tuỳ | XOR default với Token (US-EDIT-02). |
| **Trạng thái khóa** | `IsLocked` | Khóa / mở khóa đăng nhập | **Có** (`*`) | Switch; nhãn ví dụ **Đã mở khoá** khi off; không thao tác khi tài khoản **đã xóa** (§2.4). |
| **Trạng thái tài khoản** | `IsDeleted` | Hoạt động vs đã xóa mềm | **Có** (`*`) | Switch; nhãn ví dụ **Hoạt động**; mapping cụ thể on/off với cờ xóa mềm do backend quy ước. |
| **Đặt lại mật khẩu** | — | Nút kích hoạt luồng đặt lại MK | — | Nút **Đặt lại**; mở form nhập **mật khẩu mới** (US-EDIT-06). |

### 7.4. Khối "Phân quyền" (theo dòng)

Áp dụng **mỗi dòng** trong danh sách phân quyền; có thể có **nhiều dòng** cho một người dùng.

| Field | Mô tả | Bắt buộc | Ghi chú / Rule |
|-------|--------|----------|----------------|
| **Đơn vị** | Đơn vị của dòng phân quyền | **Có** (theo nghiệp vụ dòng hợp lệ) | Select **search**; giá trị từ REF-01. |
| **Vai trò** | Vai trò trên dòng | **Có** | Select **search**; REF-03. **Không trùng** bộ (`Người`,`Đơn vị`,`Phòng ban`,`Vai trò`) với dòng khác (US-EDIT-03). |
| **Phòng ban** | Phòng ban trong đơn vị | Tuỳ danh mục | Select **search**; REF-02; thường phụ thuộc **Đơn vị**. |
| **Chức vụ** | Chức vụ gán trên dòng | Tuỳ danh mục | Select **search**; REF-03. |
| **Phân quyền Action (dòng)** | Cấu hình cây/thao tác được phép | Không | Mở qua icon **safety-certificate**; không override → kế thừa vai trò (US-EDIT-04). |
| **Danh sách Trình/Chuyển — chọn người** | Thêm người vào danh sách đích | Không | Mở qua icon **team** / picker; REF-04. |
| **Xóa dòng phân quyền** | Xóa dòng hiện tại | — | Icon delete; xác nhận tuỳ UX. |
| **Loại (văn bản)** | Loại văn bản áp dụng danh sách Trình/Chuyển | Tuỳ | Select nhỏ (vd **"Văn bản đến"**); áp dụng cho block ngay dưới dòng (US-EDIT-05). |
| **Người nhận Trình/Chuyển** | Danh sách người được gán (định danh + hiển thị phụ) | Không | Thẻ (chip): họ tên + mô tả chức danh/đơn vị; nút **xoá** từng người; rule trùng/ngăn trùng do nghiệp vụ quy định. |

---

## 8. Tính năng mới (New Features)

> **Trạng thái:** Draft — bổ sung từ phiên bản 1.3, cập nhật 1.4.
> **Phạm vi:** Năm nhóm tính năng mở rộng; giữ nguyên các quy tắc nghiệp vụ đã định ở §2.4.

---

### 8.1. Avatar người dùng

#### 8.1.1. Mô tả

Mỗi tài khoản người dùng có thể gắn một **ảnh đại diện (avatar)** hiển thị trên danh sách, form chi tiết và các màn hình liên quan trong hệ thống.

#### 8.1.2. Giao diện & điều khiển

- Trên **form Thông tin tài khoản (§3.2.1):** hiển thị ô avatar (mặc định placeholder icon người dùng); nút **Tải ảnh lên** và nút **Xoá ảnh**.
- Khi nhấn **Tải ảnh lên**: mở hộp thoại chọn file hoặc hỗ trợ kéo–thả (drag & drop).
- Sau khi chọn file: hiển thị **preview** ảnh và cho phép **crop/cắt** khung tỉ lệ 1:1 trước khi lưu.
- Trên **bảng danh sách (§3.1.1):** hiển thị avatar dạng thumbnail nhỏ ở đầu cột Họ và tên (tuỳ chọn bật/tắt qua cấu hình cột).

#### 8.1.3. Quy tắc nghiệp vụ

| Quy tắc | Chi tiết |
|---------|----------|
| **Định dạng** | Chấp nhận `JPG`, `PNG`, `WEBP`. |
| **Kích thước file** | Tối đa **5 MB** sau khi chọn; báo lỗi rõ nếu vượt. |
| **Tỉ lệ lưu** | Lưu theo khung vuông sau khi crop; độ phân giải tối thiểu 200 × 200 px. |
| **Xoá ảnh** | Trả về ảnh placeholder mặc định; không xóa lịch sử upload. |
| **Quyền thao tác** | Quản trị viên sửa avatar của bất kỳ người dùng (áp dụng ngay). Người dùng tự sửa avatar của chính họ → áp dụng **luồng phê duyệt §8.4**. |

#### 8.1.4. Bảng chức năng

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **US-AVT-01** | Tải ảnh đại diện | Upload và crop ảnh; lưu vào hồ sơ người dùng. |
| **US-AVT-02** | Xoá ảnh đại diện | Xoá ảnh hiện tại, trả về ảnh placeholder. |
| **US-AVT-03** | Hiển thị avatar trên danh sách | Hiển thị thumbnail bên cạnh họ tên trên bảng danh sách. |

#### 8.1.5. Danh mục trường dữ liệu

| Field | `id` UI | Mô tả | Bắt buộc | Ghi chú / Rule |
|-------|---------|--------|----------|----------------|
| **Avatar** | `AvatarUrl` | Đường dẫn ảnh đại diện | Không | Lưu URL hoặc path nội bộ; placeholder khi null. |

---

### 8.2. Thùng rác (Trash / Soft-delete nâng cao)

#### 8.2.1. Mô tả

Thay vì chỉ đánh cờ xóa mềm thuần tuý (§2.4), hệ thống bổ sung **màn hình Thùng rác** để quản trị viên xem lại, **khôi phục**, hoặc **xoá vĩnh viễn** các tài khoản đã xóa.

#### 8.2.2. Giao diện & điều khiển

**Trên màn hình "Quản lý người dùng" (§3.1.1):**
- Bổ sung nút **Thùng rác** (icon trash) trên thanh công cụ trên.
- Nhấn mở **màn hình Thùng rác** (tách riêng hoặc modal/drawer).

**Màn hình Thùng rác:**
- **Tiêu đề:** "Thùng rác — Người dùng đã xoá".
- **Tìm kiếm / Lọc:** tương tự màn danh sách chính (họ tên, tên đăng nhập, đơn vị).
- **Bảng danh sách:** STT; Họ và tên; Tên đăng nhập; Email; Đơn vị cơ sở; **Ngày xoá**; **Người xoá**; **Thao tác**.
- **Cột Thao tác:** nút **Khôi phục** (icon restore) và nút **Xoá vĩnh viễn** (icon delete-permanent) trên từng dòng.
- **Thao tác hàng loạt:** checkbox chọn nhiều → nút **Khôi phục tất cả đã chọn** / **Xoá vĩnh viễn tất cả đã chọn**.
- **Phân trang** theo cùng chuẩn danh sách chính.

#### 8.2.3. Quy tắc nghiệp vụ

| Quy tắc | Chi tiết |
|---------|----------|
| **Xoá vào thùng rác** | Thao tác "Xóa" (US-LIST-03) đặt cờ `IsDeleted = true` **và** ghi nhận `DeletedAt`, `DeletedBy`; bản ghi xuất hiện trong Thùng rác. |
| **Khôi phục** | Đặt lại `IsDeleted = false`, xoá `DeletedAt`/`DeletedBy`; tài khoản trở về danh sách chính với trạng thái **khóa** (an toàn mặc định) — quản trị viên cần mở khóa thủ công sau khôi phục. |
| **Xoá vĩnh viễn** | Xoá vật lý bản ghi; **không thể hoàn tác**; bắt buộc hiển thị hộp xác nhận với cảnh báo rõ ràng. |
| **Tự động dọn thùng rác** | (Tuỳ chọn triển khai) Bản ghi trong thùng rác quá **90 ngày** có thể bị xoá vĩnh viễn tự động theo cấu hình hệ thống. |
| **Nhật ký** | Cả khôi phục lẫn xoá vĩnh viễn phải **ghi nhật ký** (NFR-02). |
| **Quyền** | Chỉ người có Action tương ứng mới truy cập màn hình Thùng rác và thực hiện xoá vĩnh viễn. |

#### 8.2.4. Bảng chức năng

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **US-TRASH-01** | Xem danh sách thùng rác | Hiển thị người dùng đã xóa mềm; tìm kiếm, lọc, phân trang. |
| **US-TRASH-02** | Khôi phục người dùng | Đưa tài khoản về trạng thái hoạt động (mặc định khóa sau khôi phục). |
| **US-TRASH-03** | Xoá vĩnh viễn | Xoá vật lý khỏi hệ thống; yêu cầu xác nhận; không hoàn tác. |
| **US-TRASH-04** | Thao tác hàng loạt | Khôi phục / xoá vĩnh viễn nhiều bản ghi cùng lúc. |

#### 8.2.5. Danh mục trường dữ liệu bổ sung

| Field | `id` UI | Mô tả | Ghi chú |
|-------|---------|--------|---------|
| **Ngày xoá** | `DeletedAt` | Thời điểm đánh dấu xoá | Hiển thị trên bảng thùng rác. |
| **Người xoá** | `DeletedBy` | Tài khoản thực hiện xoá | Hiển thị trên bảng thùng rác. |

---

### 8.3. Xuất khẩu (Export) theo bản ghi đã chọn

#### 8.3.1. Mô tả

Nâng cấp tính năng **Export (US-LIST-06)**: Hiện tại xuất khẩu đang xuất khẩu toàn bộ dữ liệu. Cải tiến cho phép quản trị viên chọn theo bộ lọc trên danh sách rồi chỉ xuất những bản ghi đó.

#### 8.3.2. Giao diện & điều khiển

- Khi **đã chọn** ít nhất một dòng (checkbox): nút **Export** hiển thị badge số lượng đã chọn (vd: **Export (5)**); hành động xuất chỉ áp dụng cho các dòng đã tick.
- (Tuỳ chọn UX) Khi nhấn Export có thể hiện dropdown:
  - **Xuất bản ghi đã chọn (N)**
  - **Xuất tất cả theo bộ lọc**

#### 8.3.3. Quy tắc nghiệp vụ

| Quy tắc | Chi tiết |
|---------|----------|
| **Ưu tiên lựa chọn** | Nếu có dòng được chọn, export **chỉ** các dòng đó, bất kể bộ lọc hiện tại. |
| **Cùng cấu trúc file** | File Excel xuất theo lựa chọn có **cùng cấu trúc cột** với export toàn bộ. |
| **Giới hạn** | (Tuỳ chọn) Cảnh báo nếu số bản ghi chọn vượt ngưỡng (vd > 1 000 dòng). |

#### 8.3.4. Bảng chức năng

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **US-EXP-01** | Export theo lựa chọn | Xuất file Excel chỉ chứa các bản ghi người dùng đã chọn qua checkbox. |
| **US-EXP-02** | Export theo bộ lọc | Xuất toàn bộ theo bộ lọc/tìm kiếm hiện tại khi không có dòng nào được chọn. |

---

### 8.4. Luồng phê duyệt khi người dùng tự sửa thông tin

#### 8.4.1. Mô tả

Khi **người dùng đăng nhập sửa thông tin chính họ** (không phải quản trị viên sửa người khác), các thay đổi **không áp dụng ngay** mà chuyển sang trạng thái **"Chờ phê duyệt"** cho đến khi quản trị viên có thẩm quyền xét duyệt.

#### 8.4.2. Giao diện & điều khiển

**Phía người dùng tự sửa:**
- Form thông tin tài khoản hiển thị **banner/thông báo**: *"Thay đổi của bạn cần được phê duyệt trước khi có hiệu lực."*
- Sau khi nhấn **Lưu**: hiển thị thông báo *"Yêu cầu thay đổi đã được gửi, đang chờ phê duyệt."*
- Trong thời gian chờ: form hiển thị **giá trị hiện tại đang áp dụng**; vùng **"Thay đổi đang chờ"** liệt kê các trường đã gửi kèm giá trị mới (badge hoặc highlight).
- Người dùng có thể **huỷ yêu cầu** đang chờ trước khi được phê duyệt.

**Phía quản trị viên / người phê duyệt:**
- Màn hình **"Yêu cầu chỉnh sửa chờ duyệt"** (hoặc widget/thông báo trên dashboard quản trị) liệt kê các yêu cầu đang chờ: Tên người dùng; Thời gian gửi; Trường thay đổi; Giá trị cũ → Giá trị mới.
- Thao tác trên từng yêu cầu: **Phê duyệt** (áp dụng thay đổi) hoặc **Từ chối** (kèm lý do tuỳ chọn).
- Hỗ trợ phê duyệt / từ chối **hàng loạt**.

#### 8.4.3. Quy tắc nghiệp vụ

| Quy tắc | Chi tiết |
|---------|----------|
| **Phân biệt actor** | Quản trị viên sửa tài khoản người khác → **áp dụng ngay** (không qua phê duyệt). Người dùng sửa chính mình → **vào hàng đợi phê duyệt**. |
| **Phạm vi trường áp dụng** | Các trường **cá nhân** (Họ, Tên, SĐT, Email, Ngày sinh, Địa chỉ, CCCD, Avatar) đều qua phê duyệt. Trường **nhạy cảm / hệ thống** (trạng thái khóa, phân quyền) **không** cho phép tự sửa. |
| **Trạng thái chờ** | Trong lúc chờ duyệt, **giá trị cũ vẫn là giá trị hiệu lực**; không áp dụng giá trị mới cho đến khi phê duyệt. |
| **Một yêu cầu tại một thời điểm** | Mỗi người dùng chỉ có **một yêu cầu chỉnh sửa đang chờ** tại một thời điểm; gửi yêu cầu mới sẽ **ghi đè** (hoặc yêu cầu huỷ yêu cầu cũ trước). |
| **Thông báo** | Sau khi phê duyệt hoặc từ chối, hệ thống **thông báo** đến người dùng (trong hệ thống hoặc qua email theo cấu hình). |
| **Hết hạn yêu cầu** | (Tuỳ chọn) Yêu cầu chờ quá **N ngày** (cấu hình) tự động hết hạn và bị từ chối. |
| **Nhật ký** | Toàn bộ hành động gửi, phê duyệt, từ chối, huỷ đều **ghi nhật ký** (NFR-02). |

#### 8.4.4. Bảng chức năng

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **US-SELF-01** | Gửi yêu cầu chỉnh sửa | Người dùng lưu thay đổi cá nhân; tạo bản ghi yêu cầu trạng thái **Chờ duyệt**. |
| **US-SELF-02** | Huỷ yêu cầu đang chờ | Người dùng tự huỷ yêu cầu trước khi được xử lý. |
| **US-APPR-01** | Xem danh sách yêu cầu chờ duyệt | Quản trị viên xem toàn bộ yêu cầu đang chờ; lọc theo người dùng, ngày gửi, trạng thái. |
| **US-APPR-02** | Phê duyệt yêu cầu | Áp dụng giá trị mới vào hồ sơ; ghi nhật ký; thông báo người dùng. |
| **US-APPR-03** | Từ chối yêu cầu | Giữ giá trị cũ; ghi nhận lý do (tuỳ chọn); thông báo người dùng. |
| **US-APPR-04** | Phê duyệt / Từ chối hàng loạt | Xử lý nhiều yêu cầu cùng lúc. |

#### 8.4.5. Danh mục trường dữ liệu — bản ghi yêu cầu chỉnh sửa

| Field | Mô tả | Ghi chú |
|-------|--------|---------|
| **Người yêu cầu** | UserId của người gửi thay đổi | — |
| **Thời gian gửi** | Timestamp lúc gửi yêu cầu | — |
| **Danh sách thay đổi** | JSON / danh sách cặp `{field, oldValue, newValue}` | Lưu snapshot tại thời điểm gửi. |
| **Trạng thái yêu cầu** | `Pending` / `Approved` / `Rejected` / `Cancelled` / `Expired` | — |
| **Người xử lý** | UserId quản trị viên phê duyệt/từ chối | Null khi chờ. |
| **Thời gian xử lý** | Timestamp lúc phê duyệt/từ chối | Null khi chờ. |
| **Lý do từ chối** | Text tuỳ chọn nhập khi từ chối | — |

---


### 8.5. Rà soát hồ sơ (Profile Review)

#### 8.6.1. Mô tả

Chức năng cho phép quản trị viên **thống kê, rà soát** danh sách người dùng có hồ sơ **chưa hoàn thiện** theo một hoặc nhiều tiêu chí định sẵn (thiếu avatar, thiếu CCCD), đồng thời **gửi thông báo hàng loạt** đến các tài khoản liên quan, yêu cầu họ bổ sung thông tin.

Mục đích: duy trì chất lượng dữ liệu hồ sơ, hỗ trợ hoạt động kiểm soát nội bộ và tuân thủ quy định.

#### 8.6.2. Giao diện & điều khiển

**Điểm truy cập:**
- Trên màn hình "Quản lý người dùng" (§3.1.1), bổ sung nút **Rà soát hồ sơ** (icon checklist/audit) trên thanh công cụ trên.
- Nhấn mở **màn hình Rà soát hồ sơ** (trang riêng hoặc drawer/modal toàn màn).

**Khu vực bộ lọc tiêu chí:**

| Điều khiển | Mô tả |
|------------|-------|
| **Tiêu chí hồ sơ chưa hoàn thiện** | Checkbox đa chọn; danh sách tiêu chí chuẩn (xem §8.6.3); áp dụng logic **OR** hoặc **AND** |
| **Đơn vị** | Select search + clear — lọc trong phạm vi đơn vị. |
| **Trạng thái tài khoản** | Chỉ rà soát người dùng **đang hoạt động** (mặc định); tuỳ chọn mở rộng sang đã khoá. |
| **Tìm kiếm** | Ô tìm theo họ tên, tên đăng nhập (thu hẹp thêm trong kết quả rà soát). |
| **Nút Rà soát / Thống kê** | Chạy truy vấn, cập nhật dashboard tóm tắt và bảng kết quả. |

**Bảng danh sách kết quả rà soát:**
- **Checkbox** chọn một/nhiều/tất cả.
- **Cột:** STT; Avatar; Họ và tên; Tên đăng nhập; Đơn vị cơ sở; **Tiêu chí còn thiếu** (danh sách tag/badge từng tiêu chí chưa đạt); Ngày cập nhật hồ sơ gần nhất; Lần thông báo gần nhất; **Thao tác**.
- **Cột Thao tác:** nút **Xem hồ sơ** (điều hướng sang form §3.2), nút **Gửi thông báo** (gửi cho một người).
- **Nút hàng loạt (trên bảng):** **Gửi thông báo đến các tài khoản đã chọn**.
- **Phân trang** theo chuẩn chung.

#### 8.6.3. Tiêu chí rà soát hồ sơ chưa hoàn thiện

| Mã tiêu chí | Nhãn hiển thị | Điều kiện "chưa hoàn thiện" |
|-------------|---------------|------------------------------|
| **CRI-01** | Chưa có ảnh đại diện | `AvatarUrl` null hoặc rỗng. |
| **CRI-02** | Thiếu số CCCD | `IDCard` null hoặc rỗng. |
| **CRI-03** | Thiếu ngày sinh | `BirthOfDay` null. |
| **CRI-04** | Thiếu địa chỉ | `Address` null hoặc rỗng. |
| **CRI-06** | Chưa cấu hình chữ ký số | Cả Token và Sim PKI đều tắt (US-EDIT-02). |

> **Lưu ý thiết kế:** Danh sách tiêu chí có thể mở rộng theo yêu cầu triển khai; nên tổ chức dưới dạng danh mục cấu hình (configurable checklist) để tránh hard-code.

#### 8.6.4. Quy tắc nghiệp vụ

| Quy tắc | Chi tiết |
|---------|----------|
| **Phạm vi rà soát** | Chỉ rà soát tài khoản **chưa xóa mềm** (`IsDeleted = false`); tài khoản trong Thùng rác không xuất hiện trong kết quả. |
| **Logic tiêu chí** | Mặc định áp dụng **OR** (lấy người dùng thiếu ít nhất một tiêu chí đã chọn). Có thể bổ sung tuỳ chọn **AND** (chỉ lấy người thiếu tất cả tiêu chí đã chọn) nếu nghiệp vụ yêu cầu. |
| **Tần suất gửi thông báo** | Hệ thống ghi nhận `LastNotifiedAt` cho từng người dùng; tuỳ chọn cảnh báo nếu admin gửi lại trong vòng **N ngày** (cấu hình) để tránh spam. |
| **Nội dung cá nhân hoá** | Nội dung thông báo tự động liệt kê chỉ những tiêu chí **chưa đạt của từng người nhận** (không gửi toàn bộ danh sách tiêu chí chung). |
| **Không áp dụng luồng phê duyệt** | Hành động gửi thông báo là thao tác của admin → áp dụng ngay, không qua phê duyệt. |
| **Quyền truy cập** | Chỉ người có Action tương ứng mới truy cập màn hình Rà soát hồ sơ và gửi thông báo (NFR-03). |
| **Nhật ký** | Mỗi lần gửi thông báo (đơn lẻ hoặc hàng loạt) phải **ghi nhật ký**: người thực hiện, thời điểm, danh sách người nhận, tiêu chí rà soát, kênh gửi (NFR-02). |

#### 8.6.5. Bảng chức năng

| Mã | Chức năng | Mô tả nghiệp vụ |
|----|-----------|-----------------|
| **US-REV-01** | Rà soát hồ sơ chưa hoàn thiện | Lọc và liệt kê người dùng chưa đạt một hoặc nhiều tiêu chí hồ sơ; hiển thị dashboard tóm tắt số lượng/tỉ lệ theo từng tiêu chí. |
| **US-REV-02** | Xem chi tiết hồ sơ từ kết quả | Điều hướng sang form chi tiết người dùng (§3.2) ngay từ kết quả rà soát. |
| **US-REV-03** | Gửi thông báo yêu cầu hoàn thiện | Admin gửi thông báo hoàn thiện hồ sơ, liệt kê các trường còn thiếu. |

#### 8.6.6. Danh mục trường dữ liệu

**Bộ lọc & tham số rà soát:**

| Field | Mô tả | Bắt buộc | Ghi chú |
|-------|--------|----------|---------|
| **Tiêu chí lọc** | Tập tiêu chí được chọn (CRI-01 … CRI-08) | Có ít nhất 1 | Multi-select; mã hoá dưới dạng mảng mã tiêu chí. |
| **Đơn vị** | Lọc theo đơn vị | Không | Select search + clear. |
| **Trạng thái tài khoản** | Hoạt động / Đã khoá / Tất cả | Không | Mặc định: Hoạt động. |
| **Từ khoá** | Tìm theo họ tên, tên đăng nhập | Không | — |

**Kết quả hiển thị theo từng người dùng:**

| Field | `id` UI | Mô tả | Ghi chú |
|-------|---------|--------|---------|
| **Tiêu chí còn thiếu** | `MissingCriteria` | Danh sách mã tiêu chí chưa đạt | Hiển thị dạng badge/tag trên lưới. |
| **Ngày cập nhật hồ sơ gần nhất** | `ProfileUpdatedAt` | Thời điểm sửa thông tin lần cuối | Hỗ trợ ưu tiên gửi thông báo. |
| **Lần thông báo gần nhất** | `LastNotifiedAt` | Thời điểm admin gửi thông báo rà soát gần nhất | Dùng để kiểm tra ngưỡng chống spam. |
---

### 8.6. Tổng hợp mã hiệu tính năng mới

| Mã | Nhóm | Chức năng tóm tắt |
|----|------|-------------------|
| US-AVT-01 | Avatar | Tải ảnh đại diện |
| US-AVT-02 | Avatar | Xoá ảnh đại diện |
| US-AVT-03 | Avatar | Hiển thị avatar trên danh sách |
| US-TRASH-01 | Thùng rác | Xem danh sách thùng rác |
| US-TRASH-02 | Thùng rác | Khôi phục người dùng |
| US-TRASH-03 | Thùng rác | Xoá vĩnh viễn |
| US-TRASH-04 | Thùng rác | Thao tác hàng loạt (khôi phục / xoá vĩnh viễn) |
| US-EXP-01 | Export nâng cao | Xuất theo bản ghi đã chọn |
| US-EXP-02 | Export nâng cao | Xuất theo bộ lọc hiện tại |
| US-SELF-01 | Tự chỉnh sửa | Gửi yêu cầu chỉnh sửa |
| US-SELF-02 | Tự chỉnh sửa | Huỷ yêu cầu đang chờ |
| US-APPR-01 | Phê duyệt | Xem danh sách yêu cầu chờ duyệt |
| US-APPR-02 | Phê duyệt | Phê duyệt yêu cầu |
| US-APPR-03 | Phê duyệt | Từ chối yêu cầu |
| US-APPR-04 | Phê duyệt | Phê duyệt / Từ chối hàng loạt |
| US-REV-01 | Rà soát hồ sơ | Rà soát hồ sơ chưa hoàn thiện, thống kê theo tiêu chí |
| US-REV-02 | Rà soát hồ sơ | Xem chi tiết hồ sơ từ kết quả rà soát |
| US-REV-03 | Rà soát hồ sơ | Gửi thông báo yêu cầu hoàn thiện (đơn lẻ) |
| US-REV-04 | Rà soát hồ sơ | Gửi thông báo yêu cầu hoàn thiện (hàng loạt) |
| US-REV-05 | Rà soát hồ sơ | Xem lịch sử gửi thông báo rà soát |

---

> **Điểm cần thống nhất trước khi thiết kế kỹ thuật:**
>
> 1. **Thùng rác — tự động dọn:** Xác nhận thời gian lưu giữ (90 ngày hay khác) và ai có quyền cấu hình ngưỡng.
> 2. **Phê duyệt — Email:** `Email` (duy nhất toàn hệ thống) khi đang chờ duyệt có thể xung đột nếu người khác đăng ký cùng email — cần xử lý riêng (vd: giữ chỗ hoặc kiểm tra lại lúc phê duyệt).
> 3. **Export — ngưỡng cảnh báo:** Xác nhận con số phù hợp với hiệu năng thực tế.
