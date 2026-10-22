# ADR 0001 — Dùng SQL Server + EF Core

- **Trạng thái:** Đã chấp nhận
- **Ngày:** 2026-10-20

## Bối cảnh
TeamBoard cần lưu Board và Task có quan hệ 1–n. Team quen .NET; trên AWS sẽ chạy bằng Amazon RDS.

## Quyết định
Dùng SQL Server qua EF Core (code-first, migrations). Local chạy SQL Server trong Docker; mật khẩu nằm trong user-secrets.

## Phương án đã cân nhắc
- **PostgreSQL:** rẻ hơn trên RDS (không tốn license), nhưng team ít kinh nghiệm hơn.
- **DynamoDB:** serverless, scale tốt, nhưng dữ liệu có quan hệ và cần truy vấn linh hoạt.

## Hệ quả
- RDS SQL Server có phí license → cần tắt instance khi không dùng (D4 Cost).
- Đổi sang PostgreSQL sau này chỉ cần đổi provider EF Core và tạo lại migration.

---
_Mẫu cho ADR mới: copy file này, đổi số thứ tự, giữ 4 mục Bối cảnh / Quyết định / Phương án / Hệ quả._
