using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TeamBoard.Infrastructure.Persistence.Converters;

// SQL Server datetime2 has no time zone, so values come back as DateTimeKind.Unspecified and
// serialize without "Z". All DateTimes are stored as UTC and read back marked as UTC.
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
