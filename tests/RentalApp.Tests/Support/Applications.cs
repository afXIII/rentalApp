using RentalApp.Web.Domain;

namespace RentalApp.Tests.Support;

// Builders that walk an application through real domain methods to a given status.
public static class Applications
{
    public const string ApplicantId = "applicant-1";
    public const string ManagerId = "manager-1";
    public static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
    public static DateOnly Today => FixedClock.Today;

    public static Unit NewUnit(int id = 1) => new() { Id = id, UnitNumber = "101", Bedrooms = 1, MonthlyRent = 1000m };

    public static RentalApplication Draft(Unit? unit = null)
    {
        unit ??= NewUnit();
        var application = RentalApplication.Start(unit.Id, ApplicantId, Now);
        application.Unit = unit;
        return application;
    }

    public static RentalApplication ReadyToSubmit(Unit? unit = null)
    {
        var application = Draft(unit);
        application.SaveApplicantInfo(ApplicantId, "Ann Applicant", "555-0100", "ann@test", "1 Main St", Now);
        application.Residences.Add(new Residence
        {
            Address = "9 Elm St",
            LandlordName = "Lee Landlord",
            LandlordPhone = "555-0199",
            MoveInDate = new DateOnly(2023, 1, 1),
            MoveOutDate = new DateOnly(2026, 9, 30),
        });
        application.SaveResidenceHistory(ApplicantId, Now);
        return application;
    }

    public static RentalApplication Submitted(Unit? unit = null)
    {
        var application = ReadyToSubmit(unit);
        application.Submit(ApplicantId, application.Unit, Today, Now);
        return application;
    }

    public static RentalApplication UnderReview(Unit? unit = null)
    {
        var application = Submitted(unit);
        application.Claim(ManagerId, Now);
        return application;
    }

    public static RentalApplication InStatus(ApplicationStatus status)
    {
        switch (status)
        {
            case ApplicationStatus.Draft:
                return Draft();
            case ApplicationStatus.Submitted:
                return Submitted();
            case ApplicationStatus.UnderReview:
                return UnderReview();
            case ApplicationStatus.Returned:
            {
                var application = UnderReview();
                application.ReturnToApplicant(ManagerId, "Fix it", Now);
                return application;
            }
            case ApplicationStatus.Approved:
            {
                var application = UnderReview();
                application.Approve(ManagerId, application.Unit, Today, Now, null);
                return application;
            }
            case ApplicationStatus.Denied:
            {
                var application = UnderReview();
                application.Deny(ManagerId, "No", Now);
                return application;
            }
            case ApplicationStatus.Withdrawn:
            {
                var application = Draft();
                application.Withdraw(ApplicantId, Now);
                return application;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    public static Lease LeaseFrom(DateOnly start) => new()
    {
        TenantId = "someone-else",
        StartDate = start,
        EndDate = Lease.EndDateFor(start),
    };
}
