Nền tảng runtime đã cũ- .NET Framework 4.8 là legacy, không còn phù hợp để scale cloud-native bằng Linux/K8s như .NET 6/8.
- Phụ thuộc mạnh vào Windows, nên bị khóa công nghệ, khó tối ưu hạ tầng.

2. Phải dùng Windows container
- Không chạy nhẹ như Linux container.
- Image rất nặng, thời gian pull và start lâu.
- Tốn CPU, RAM, disk hơn nhiều so với app .NET mới trên Linux.

3. Scale ngang không hiệu quả bằng Linux app
- Khi tăng số container, thời gian khởi động instance mới chậm.
- Khó đáp ứng các đợt tăng tải đột biến.
- Chi phí scale cao hơn vì mỗi node Windows tốn tài nguyên hơn.

4. Khó tách service thật sự
Hệ .NET 4.8 cũ là monolith. Dù đóng container, bên trong vẫn có thể còn phụ thuộc chéo:
- Chung DB
- Chung thư mục file
- Chung config
- Gọi nội bộ chặt
Kết quả là “container hóa” nhưng chưa thật sự microservice.

5. Phụ thuộc IIS / System.Web / thư viện cũ
Ứng dụng .NET 4.8 gắn với:
- IIS
- OWIN cũ
- Thư viện Windows-only
- COM / GAC / registry / driver đặc thù
Những thứ này làm việc đóng gói và scale container khó ổn định hơn.

6. DB là nút cổ chai
App hiện tại dù scale thêm container, nhưng nếu vẫn dùng:
- 1 SQL Server chung
- Stored procedure nặng
Transaction lớn thì DB sẽ là điểm nghẽn chính, app scale không mang lại hiệu quả tương ứng.

7. Khó autoscale theo tải thực tế
Windows container scale được, nhưng:
- Chậm hơn
- Nặng hơn
- Ít linh hoạt hơn
nên autoscale không “mượt”, đặc biệt với tải tăng nhanh.

8. Rủi ro vận hành cao hơn khi mở rộng nhiều đơn vị
Với mô hình phục vụ nhiều đơn vị/người dùng:
- Dễ phát sinh xung đột cấu hình
- Khó tách tenant sạch sẽ
- Khó quản lý tài nguyên riêng từng đơn vị
- Khó cô lập lỗi giữa các tenant

9. Khó tích hợp các thành phần hiện đại
- Service mesh
- Event-driven architecture
- Serverless job
- AI models, AI service tách rời
- API gateway hiện đại
đều làm được, nhưng hệ .NET 4.8 Windows thường ghép nối khó hơn, công vận hành nhiều hơn.

10. Khả năng mở rộng dài hạn không tốt
Hệ thống vẫn có thể chạy ổn trong ngắn hạn.
Nhưng về dài hạn, càng thêm nghiệp vụ, càng nhiều người dùng thì:
- Nợ kỹ thuật tăng
- Chi phí tăng
- Tốc độ phát triển tính năng giảm
- Khó tuyển người maintain hơn so với stack mới