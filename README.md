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

### Chạy test

```bash
dotnet test
```

Test tích hợp chạy API thật trong bộ nhớ (`WebApplicationFactory`) với EF Core InMemory thay cho SQL Server, nên không cần Docker.
Riêng hành vi xoá cascade do SQL Server thực hiện, InMemory không mô phỏng được, nên kiểm tra bằng compose.

## Cấu trúc dự án

Chia theo Clean Architecture: phụ thuộc chỉ đi vào trong, `Domain` không biết gì về EF Core hay HTTP. Lý do xem [ADR 0002](docs/adr/0002-clean-architecture.md).

```
TeamBoard.slnx
Directory.Build.props        # net10.0, nullable, implicit usings cho mọi project
Directory.Packages.props     # version NuGet dùng chung (central package management)
docker-compose.yml · .env.example · .dockerignore
src/
├── TeamBoard.Domain/            # Entity + enum, không tham chiếu project/package nào
│   ├── Boards/                  #   Board
│   └── Tasks/                   #   TaskItem, TaskItemStatus
├── TeamBoard.Application/       # Nghiệp vụ: service, DTO, interface — → Domain
│   ├── Common/                  #   IAppDbContext, Result/Error
│   ├── Boards/                  #   BoardService, BoardDtos
│   ├── Tasks/                   #   TaskService, TaskDtos, TaskMappings
│   └── DependencyInjection.cs   #   AddApplication()
├── TeamBoard.Infrastructure/    # EF Core + SQL Server — → Application
│   ├── Persistence/             #   TeamBoardDbContext, Configurations/, Migrations/
│   └── DependencyInjection.cs   #   AddInfrastructure(): DbContext, health check, migrate
└── TeamBoard.Api/               # HTTP: controller, Program.cs, Dockerfile — → Application, Infrastructure
    ├── Controllers/             #   ApiControllerBase (Result/Error → envelope + status), Boards, Tasks
    ├── Responses/               #   ApiResponse { success, data, message }, ApiResponseClientErrorFactory
    ├── Setup/                   #   AddApiControllers(), UseApiErrorResponses(), thông báo lỗi tiếng Việt
    └── Json/                    #   UtcDateTimeJsonConverter (thời gian luôn UTC, có "Z")
tests/
└── TeamBoard.Api.Tests/         # Test tích hợp qua HTTP
```

Thêm tính năng mới: entity vào `Domain/<Feature>/`, service + DTO vào `Application/<Feature>/`, cấu hình EF vào `Infrastructure/Persistence/Configurations/`, controller vào `Api/Controllers/`.

Tạo migration (chạy từ thư mục gốc):

```bash
dotnet ef migrations add <TênMigration> --project src/TeamBoard.Infrastructure --startup-project src/TeamBoard.Api --output-dir Persistence/Migrations
```

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

### Định dạng response

Mọi response (trừ `/health`) có cùng một khung `{ success, data, message }`, **và HTTP status vẫn đúng** (200/201/400/404/500…). Không bao giờ trả 200 cho lỗi — load balancer, CloudWatch alarm và health check của AWS dựa vào status code.

```json
// 200 — GET /api/tasks/5
{ "success": true, "data": { "id": 5, "title": "Viết API", "status": "InProgress", "createdAt": "2026-10-22T01:25:14.85Z", ... }, "message": null }

// 201 — POST /api/boards/1/tasks (kèm header Location: /api/tasks/6)
{ "success": true, "data": { "id": 6, ... }, "message": "Đã tạo task." }

// 200 — PUT / DELETE
{ "success": true, "data": null, "message": "Đã xoá task." }

// 404 — id không tồn tại
{ "success": false, "data": null, "message": "Board 99999 không tồn tại.", "traceId": "00-…" }

// 400 — dữ liệu không hợp lệ: key trong "errors" trùng tên field JSON
{ "success": false, "data": null, "message": "Dữ liệu không hợp lệ.",
  "errors": { "title": ["Không được để trống."], "assignee": ["Tối đa 100 ký tự."] }, "traceId": "00-…" }
```

- `errors` chỉ có ở lỗi validation; `traceId` chỉ có khi lỗi, dùng để tìm request trong log.
- Lỗi do framework sinh ra cũng theo khung này: route không tồn tại (404), sai method (405), sai Content-Type (415), exception không bắt (500 — chỉ trả thông báo chung, chi tiết nằm trong log).
- JSON: field camelCase, enum dạng chữ, thời gian luôn là UTC có hậu tố `Z`. Gửi lên có offset (`+07:00`) sẽ được đổi sang UTC; không có offset thì coi là UTC.
- `/health` giữ dạng text `Healthy` / `Unhealthy` cho health check của Docker/AWS.

Code: `Api/Responses/ApiResponse.cs` (khung), `ApiControllerBase` (`OkOrFail`, `CreatedOrFail`, `Fail`), `Setup/ControllerSetup.cs` (400), `Setup/ErrorResponseSetup.cs` (404/405/500 ngoài controller).

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
