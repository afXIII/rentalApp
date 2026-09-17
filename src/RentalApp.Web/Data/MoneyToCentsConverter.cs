using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RentalApp.Web.Data;

// Laravel equivalent: an Eloquent attribute cast (or accessor + mutator pair).
// C# always sees decimal dollars. The database column holds integer cents.
public class MoneyToCentsConverter : ValueConverter<decimal, long>
{
    public MoneyToCentsConverter()
        : base(
            dollars => ToCents(dollars),   // writing to the database
            cents => ToDollars(cents))     // reading from the database
    {
    }

    public static long ToCents(decimal dollars) =>
        (long)Math.Round(dollars * 100m, MidpointRounding.AwayFromZero);

    public static decimal ToDollars(long cents) => cents / 100m;
}
