# ADR 0002 — Chia dự án theo Clean Architecture

- **Trạng thái:** Đã chấp nhận
- **Ngày:** 2026-10-22

## Bối cảnh
TeamBoard ban đầu là một project `TeamBoard.Api` duy nhất: controller gọi thẳng `DbContext`, entity, DTO và migration nằm chung. Các tuần sau sẽ thêm Redis, S3, Secrets Manager, queue; nếu giữ một project thì logic nghiệp vụ và code hạ tầng AWS sẽ trộn vào controller.

## Quyết định
Tách thành 4 project, phụ thuộc chỉ đi vào trong:

- `Domain` — entity, không tham chiếu gì.
- `Application` — service theo tính năng (`Boards/`, `Tasks/`), DTO, `IAppDbContext`, `Result`. Chỉ tham chiếu `Domain` và gói `Microsoft.EntityFrameworkCore` (không có provider SQL Server).
- `Infrastructure` — `TeamBoardDbContext`, cấu hình entity, migration, đăng ký SQL Server và health check.
- `Api` — controller mỏng, chỉ đổi `Result`/`Error` thành mã HTTP.

Kèm `tests/TeamBoard.Api.Tests` (test tích hợp với EF InMemory), `Directory.Build.props` và `Directory.Packages.props` để các project dùng chung framework và version NuGet.

## Phương án đã cân nhắc
- **Vertical slice trong 1 project:** gọn, ít file hơn, nhưng không có ranh giới compile-time — controller vẫn có thể gọi thẳng SQL Server.
- **Layered trong 1 project (Controllers/Services/Repositories):** quen thuộc, nhưng mỗi tính năng bị rải ra nhiều thư mục và vẫn không có ranh giới compile-time.
- **Repository pattern trên EF Core:** không dùng — `DbSet` đã là repository; `IAppDbContext` đủ để tách Application khỏi SQL Server và để test.

## Hệ quả
- Thêm dịch vụ AWS (S3, SQS…) chỉ cần interface trong `Application` + cài đặt trong `Infrastructure`, controller và service không đổi.
- Nhiều file và project hơn cho một app nhỏ; `dotnet ef` cần `--project src/TeamBoard.Infrastructure --startup-project src/TeamBoard.Api`.
- Migration `InitialCreate` được giữ nguyên ID khi chuyển sang `Infrastructure`, nên database đã có không bị áp dụng lại.
- Docker build dùng thư mục gốc làm context; `.dockerignore` ở gốc loại `.env`, `bin/`, `obj/`, `tests/`, `docs/`.
