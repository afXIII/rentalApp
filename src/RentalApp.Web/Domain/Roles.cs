namespace RentalApp.Web.Domain;

// The two roles in the system. Used in [Authorize(Roles = ...)] and when seeding.
public static class Roles
{
    public const string Applicant = "Applicant";
    public const string PropertyManager = "PropertyManager";

    public static readonly string[] All = [Applicant, PropertyManager];
}
