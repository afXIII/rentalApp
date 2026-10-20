using RentalApp.Web.Domain;

namespace RentalApp.Tests.Domain;

public class LeaseTests
{
    [Theory]
    [InlineData("2026-10-23", "2027-10-22")]
    [InlineData("2026-01-01", "2026-12-31")]
    [InlineData("2028-02-29", "2029-02-28")] // leap-day start: the anniversary is Mar 1
    [InlineData("2027-03-01", "2028-02-29")] // term that ends in a leap year
    public void A_term_is_twelve_months_ending_the_day_before_the_anniversary(string start, string expectedEnd)
    {
        Assert.Equal(DateOnly.Parse(expectedEnd), Lease.EndDateFor(DateOnly.Parse(start)));
    }

    [Theory]
    [InlineData("2026-10-22", false)]
    [InlineData("2026-10-23", true)]  // first day
    [InlineData("2027-10-22", true)]  // last day, inclusive
    [InlineData("2027-10-23", false)]
    public void Covers_includes_both_ends_of_the_term(string date, bool covered)
    {
        var start = new DateOnly(2026, 10, 23);
        var lease = new Lease { TenantId = "t", StartDate = start, EndDate = Lease.EndDateFor(start) };

        Assert.Equal(covered, lease.Covers(DateOnly.Parse(date)));
    }
}

public class UnitTypeTests
{
    private static readonly UnitType Active = new() { Id = 1, Name = "Apartment", IsActive = true };
    private static readonly UnitType Inactive = new() { Id = 2, Name = "Loft", IsActive = false };

    [Fact]
    public void An_active_type_is_selectable_for_any_unit()
    {
        Assert.True(Active.IsSelectableFor(currentUnitTypeId: null));
        Assert.True(Active.IsSelectableFor(currentUnitTypeId: Inactive.Id));
    }

    [Fact]
    public void An_inactive_type_cannot_be_chosen_for_a_new_unit()
    {
        Assert.False(Inactive.IsSelectableFor(currentUnitTypeId: null));
    }

    [Fact]
    public void An_inactive_type_cannot_be_switched_to()
    {
        Assert.False(Inactive.IsSelectableFor(currentUnitTypeId: Active.Id));
    }

    [Fact]
    public void A_unit_keeps_the_inactive_type_it_already_has()
    {
        Assert.True(Inactive.IsSelectableFor(currentUnitTypeId: Inactive.Id));
    }
}
