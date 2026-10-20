namespace RentalApp.Tests.Support;

// A TimeProvider frozen at one instant, in UTC. Laravel: Carbon::setTestNow().
public class FixedClock(DateTimeOffset now) : TimeProvider
{
    public static readonly DateOnly Today = new(2026, 10, 23);

    public FixedClock() : this(new DateTimeOffset(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero))
    {
    }

    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
