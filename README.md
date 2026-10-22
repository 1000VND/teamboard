# TeamBoard

App quản lý công việc theo board (Board → Task), dùng làm **app xuyên suốt** cho lộ trình 6 tuần học AWS, DevOps và System Design.
Mỗi tuần thêm một lớp hạ tầng lên chính app này, đồng thời ôn theo đề thi **AWS Certified Solutions Architect – Associate (SAA-C03)**.

> Mục tiêu: tự đưa một app ASP.NET Core lên AWS bằng Docker và CI/CD, rồi bảo vệ được thiết kế hệ thống của nó.

## Công nghệ

| Lớp | Hiện tại | Sẽ thêm |
|---|---|---|
| API | ASP.NET Core Web API (.NET 10), Swagger, `/health` | Logging có cấu trúc |
| Dữ liệu | EF Core 10 + SQL Server | Amazon RDS, Redis/ElastiCache |
| Đóng gói | Dockerfile, docker compose (api + db) | Redis (tuần 1) |
| Hạ tầng | — | ECR, App Runner/ECS, VPC, IAM (tuần 2–3), AWS CDK bằng C# (tuần 5) |
| CI/CD | — | GitHub Actions + OIDC, staging → production (tuần 4) |

## Chạy trên máy

Yêu cầu: Docker Desktop (thêm .NET SDK 10 nếu chạy API ngoài Docker).

### Cách 1 — Docker Compose (API + SQL Server)

```bash
# 1. Tạo .env từ mẫu rồi đặt mật khẩu SA thật (.env đã nằm trong .gitignore)
cp .env.example .env

# 2. Build và chạy 2 container: api, db
docker compose up -d --build
```

- Swagger: http://localhost:8080/swagger
- Health check: http://localhost:8080/health → `Healthy` (200) khi kết nối được DB, `Unhealthy` (503) khi không.
- Dữ liệu SQL Server nằm trong named volume `sqldata`, nên `docker compose down` rồi `up` lại vẫn còn. Chỉ `docker compose down -v` mới xoá dữ liệu.
- API chỉ khởi động sau khi `db` báo healthy (healthcheck chạy `SELECT 1` bằng `sqlcmd`), và tự áp dụng migration khi khởi động.
- Connection string của API đọc từ biến môi trường `ConnectionStrings__Default`, do `docker-compose.yml` ghép từ `MSSQL_SA_PASSWORD` trong `.env`.
- `db` mở cổng 1433 ra máy; nếu đang có container SQL Server khác chiếm cổng này thì dừng nó trước.

### Cách 2 — `dotnet run` (API ngoài Docker)

```bash
# 1. Chỉ chạy SQL Server bằng compose
docker compose up -d db

# 2. Lưu mật khẩu sa (giống trong .env) vào user-secrets
dotnet user-secrets set DbPassword "<mật khẩu trong .env>" --project src/TeamBoard.Api

# 3. Chạy API — dùng ConnectionStrings:Default trong appsettings.json + DbPassword
dotnet run --project src/TeamBoard.Api --launch-profile http
```

Mở Swagger: http://localhost:5012/swagger · Health: http://localhost:5012/health

### API

| Endpoint | Ghi chú |
|---|---|
| `GET /api/boards` | Danh sách board, kèm `taskCount` |
| `GET /api/boards/{id}` | Board kèm toàn bộ tasks; 404 nếu không có |
| `POST /api/boards` | Tạo board |
| `PUT /api/boards/{id}` | Sửa board |
| `DELETE /api/boards/{id}` | Xoá board và toàn bộ tasks của nó |
| `GET /api/boards/{id}/tasks` | Tasks của board; 404 nếu board không có |
| `POST /api/boards/{id}/tasks` | Tạo task trong board (body không cần `boardId`); 404 nếu board không có, 400 nếu thiếu `title` |
| `GET /api/tasks?boardId=&status=` | Danh sách task, lọc tuỳ chọn |
| `GET /api/tasks/{id}` | Một task; 404 nếu không có |
| `POST /api/tasks` | Tạo task (body có `boardId`) |
| `PUT /api/tasks/{id}` | Sửa task |
| `DELETE /api/tasks/{id}` | Xoá task |

### Xoá board có task: xoá theo (cascade)

`DELETE /api/boards/{id}` xoá luôn mọi task của board đó (cấu hình `OnDelete(DeleteBehavior.Cascade)` trong `TeamBoardDbContext`).

Lý do chọn xoá theo thay vì chặn (trả 409):
- Task không có ý nghĩa khi đứng ngoài board; để lại task mồ côi thì vi phạm khoá ngoại, còn chặn xoá thì buộc người dùng gọi xoá từng task trước, rất bất tiện.
- Cascade do SQL Server thực hiện trong cùng một transaction, nên không bao giờ xảy ra trạng thái board đã xoá mà task còn sót lại.
- Nhược điểm đã biết: xoá nhầm là mất cả task, chưa có hoàn tác. Nếu cần, sau này thêm soft delete (cờ `IsDeleted`) thay vì đổi sang chặn xoá.

## Lộ trình 6 tuần

Mỗi tuần: làm trên một nhánh riêng `week-N/...`, nộp bằng **Pull Request**, cần ít nhất 1 người approve trước Chủ nhật.

| Tuần | Chủ đề | Mốc (qua khi) | Domain SAA-C03 liên quan | Ghi chú |
|---|---|---|---|---|
| 0 | Nền tảng: API + EF Core + SQL Server | CRUD chạy qua Swagger | — | ✅ xong |
| 1 | Docker: Dockerfile, Compose, SQL Server, Redis | `docker compose up` chạy được trên máy người khác | — | [docs/week-1](docs/week-1/) |
| 2 | AWS cơ bản: IAM, VPC, RDS, ECR, App Runner | App chạy trên AWS với URL public, dùng RDS | D1 Secure, D2 Resilient | [docs/week-2](docs/week-2/) |
| 3 | AWS vận hành: Secrets Manager, S3, CloudWatch, alarm | Không có secret trong repo, có dashboard + alarm | D1 Secure, D2 Resilient | [docs/week-3](docs/week-3/) |
| 4 | CI/CD: GitHub Actions, OIDC, staging, duyệt production | Merge PR là lên staging, production có bước duyệt | D1 Secure | [docs/week-4](docs/week-4/) |
| 5 | IaC: AWS CDK (C#), diễn tập sự cố | Dựng lại staging hoàn toàn từ code | D2 Resilient, D4 Cost | [docs/week-5](docs/week-5/) |
| 6 | System design: scale, cache, queue, capstone | Bảo vệ thiết kế 20 phút trước team | D3 Performance, D4 Cost | [docs/week-6](docs/week-6/) |

**Bốn domain của SAA-C03:**
D1 Design Secure Architectures (30%) ·
D2 Design Resilient Architectures (26%) ·
D3 Design High-Performing Architectures (24%) ·
D4 Design Cost-Optimized Architectures (20%).

## Cấu trúc tài liệu

```
docs/
├── week-1 … week-6/   # ghi chú, sơ đồ, kết quả lab từng tuần
├── adr/               # Architecture Decision Records — mỗi quyết định thiết kế một file
├── exam/              # ôn thi SAA-C03: ghi chú theo domain + câu sai cần xem lại
└── system-design/     # bài toán thiết kế: đề bài → yêu cầu → phương án → đánh đổi
```

## Quy tắc

- **Không commit secret.** Mật khẩu và access key để trong user-secrets (local) hoặc AWS Secrets Manager (cloud).
- **Tắt tài nguyên AWS khi không dùng**, nhất là RDS. Luôn bật budget alert.
- Kẹt quá 30 phút thì hỏi trên nhóm chat chung.
