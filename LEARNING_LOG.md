# Learning Log

## T2 19/10 — Dựng TeamBoard

**Mục tiêu:** Web API chạy local với CRUD cho Board và Task.

### Đã làm
- Tạo ASP.NET Core Web API (.NET 10) tại `src/TeamBoard.Api`, bật Swagger ở Development.
- Entity `Board` và `TaskItem` (quan hệ 1 Board – n Task), EF Core + SQL Server (SQL Server 2022 chạy trong Docker), migration `InitialCreate`.
- Endpoint CRUD cho `/api/boards` và `/api/tasks`; DTO tách khỏi entity, validation bằng DataAnnotations.

### Đã học
- Đặt tên entity là `TaskItem` (bảng `Tasks`) để không trùng `System.Threading.Tasks.Task`.
- Lưu enum `Status` dạng chuỗi trong DB (`HasConversion<string>()`) để đọc dữ liệu dễ hơn; JSON trả về cũng dùng tên enum nhờ `JsonStringEnumConverter`.
- Mật khẩu `sa` không nằm trong `appsettings.json`: lấy từ user-secrets rồi gán vào connection string lúc khởi động.
- `[ApiController]` tự trả 400 khi DTO vi phạm `[Required]`, không cần kiểm tra thủ công trong action.
- Xoá board: chọn cascade delete, lý do ghi trong README.

### Còn vướng / câu hỏi
- Chưa có test tự động; mới kiểm tra qua Swagger.
- Cascade delete không hoàn tác được; có nên chuyển sang soft delete ở tuần sau không?

### Việc tiếp theo
- Tuần 1: Dockerfile + docker compose (api + SQL Server + Redis).
