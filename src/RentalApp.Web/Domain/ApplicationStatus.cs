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
    // An array so queries can use Terminal.Contains(a.Status), which EF translates to SQL IN (...).
    public static readonly ApplicationStatus[] Terminal =
        [ApplicationStatus.Approved, ApplicationStatus.Denied, ApplicationStatus.Withdrawn];

    public static bool IsTerminal(this ApplicationStatus status) => Terminal.Contains(status);

    // The applicant may change the application only in these statuses.
    public static bool IsEditable(this ApplicationStatus status) =>
        status is ApplicationStatus.Draft or ApplicationStatus.Returned;

    public static string DisplayName(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.UnderReview => "Under Review",
        _ => status.ToString(),
    };
}
