using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RentalApp.Web.Data;

// Every DateTime in the model is UTC. SQL Server's datetime2 does not store the "kind", so
// values read back would be Unspecified and ToLocalTime() / JSON would treat them as local time.
// This marks them as UTC on the way out. Laravel: the 'datetime' cast with app timezone UTC.
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
