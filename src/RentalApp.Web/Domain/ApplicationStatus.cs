namespace RentalApp.Web.Domain;

public enum ApplicationStatus
{
    Draft,
    Submitted,

    // Claimed by a property manager from the review queue.
    UnderReview,
    Returned,
    Approved,
    Denied,
    Withdrawn,
}

public static class ApplicationStatusExtensions
{
    // Approved, Denied, and Withdrawn are final. No transition leaves them.
    public static bool IsTerminal(this ApplicationStatus status) =>
        status is ApplicationStatus.Approved or ApplicationStatus.Denied or ApplicationStatus.Withdrawn;

    // The applicant may change the application only in these statuses.
    public static bool IsEditable(this ApplicationStatus status) =>
        status is ApplicationStatus.Draft or ApplicationStatus.Returned;

    public static string DisplayName(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.UnderReview => "Under Review",
        _ => status.ToString(),
    };
}
