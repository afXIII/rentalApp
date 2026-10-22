namespace RentalApp.Web.Services;

public static class TimeProviderExtensions
{
    // "Today" for lease terms is the calendar date where the business operates (server local time).
    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
