using RentalApp.Tests.Support;
using RentalApp.Web.Domain;
using static RentalApp.Tests.Support.Applications;

namespace RentalApp.Tests.Domain;

public class RentalApplicationTests
{
    [Fact]
    public void Start_creates_a_draft_with_a_first_history_row()
    {
        var application = Draft();

        Assert.Equal(ApplicationStatus.Draft, application.Status);
        var history = Assert.Single(application.History);
        Assert.Null(history.FromStatus);
        Assert.Equal(ApplicationStatus.Draft, history.ToStatus);
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, true)]
    [InlineData(ApplicationStatus.Returned, true)]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.UnderReview, false)]
    [InlineData(ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Denied, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    public void Only_draft_and_returned_applications_are_editable_by_their_applicant(ApplicationStatus status, bool editable)
    {
        var application = InStatus(status);

        Assert.Equal(editable, application.CanBeEditedBy(ApplicantId));
        Assert.False(application.CanBeEditedBy("someone-else"));
    }

    [Fact]
    public void Saving_a_section_is_rejected_once_submitted()
    {
        var application = Submitted();

        Assert.Throws<DomainRuleException>(() =>
            application.SaveApplicantInfo(ApplicantId, "New", "1", "x@test", "Addr", Now));
    }

    [Fact]
    public void Another_user_cannot_save_a_section()
    {
        var application = Draft();

        Assert.Throws<DomainRuleException>(() =>
            application.SaveApplicantInfo("intruder", "New", "1", "x@test", "Addr", Now));
    }

    [Fact]
    public void Residence_history_cannot_be_saved_empty()
    {
        var application = Draft();

        Assert.Throws<DomainRuleException>(() => application.SaveResidenceHistory(ApplicantId, Now));
        Assert.Null(application.ResidenceHistorySavedAt);
    }

    [Fact]
    public void Submit_requires_both_sections_saved()
    {
        var application = Draft();
        application.SaveApplicantInfo(ApplicantId, "Ann", "555", "ann@test", "1 Main", Now);

        var error = Assert.Throws<DomainRuleException>(() => application.Submit(ApplicantId, application.Unit, Today, Now));
        Assert.Contains("both sections", error.Message);
    }

    [Fact]
    public void Submit_moves_to_submitted_and_records_history()
    {
        var application = ReadyToSubmit();

        application.Submit(ApplicantId, application.Unit, Today, Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(Now, application.SubmittedAt);
        var last = application.History[^1];
        Assert.Equal((ApplicationStatus.Draft, ApplicationStatus.Submitted), (last.FromStatus!.Value, last.ToStatus));
        Assert.Equal(ApplicantId, last.ChangedById);
    }

    [Fact]
    public void Submit_is_rejected_when_the_unit_has_an_active_lease()
    {
        var unit = NewUnit();
        unit.Leases.Add(LeaseFrom(Today.AddMonths(-2)));
        var application = ReadyToSubmit(unit);

        var error = Assert.Throws<DomainRuleException>(() => application.Submit(ApplicantId, unit, Today, Now));
        Assert.Contains("active lease", error.Message);
        Assert.Equal(ApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Submit_is_allowed_when_the_units_lease_has_ended()
    {
        var unit = NewUnit();
        unit.Leases.Add(LeaseFrom(Today.AddMonths(-Lease.TermMonths).AddDays(-1)));
        var application = ReadyToSubmit(unit);

        application.Submit(ApplicantId, unit, Today, Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    [Fact]
    public void Submit_rechecks_residences_removed_after_the_section_was_saved()
    {
        var application = ReadyToSubmit();
        application.Residences.Clear();

        Assert.Throws<DomainRuleException>(() => application.Submit(ApplicantId, application.Unit, Today, Now));
    }

    [Fact]
    public void A_returned_application_can_be_corrected_and_resubmitted()
    {
        var application = InStatus(ApplicationStatus.Returned);

        application.SaveApplicantInfo(ApplicantId, "Ann Corrected", "555", "ann@test", "1 Main St, Apt 2", Now);
        application.Submit(ApplicantId, application.Unit, Today, Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal("Ann Corrected", application.FullName);
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Returned)]
    public void An_open_application_can_be_withdrawn(ApplicationStatus status)
    {
        var application = InStatus(status);

        application.Withdraw(ApplicantId, Now);

        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
        Assert.Null(application.ClaimedById);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void Terminal_statuses_cannot_change(ApplicationStatus status)
    {
        var application = InStatus(status);

        Assert.True(status.IsTerminal());
        Assert.Throws<DomainRuleException>(() => application.Withdraw(ApplicantId, Now));
        Assert.Throws<DomainRuleException>(() => application.Claim(ManagerId, Now));
        Assert.Throws<DomainRuleException>(() => application.Submit(ApplicantId, application.Unit, Today, Now));
        Assert.Equal(status, application.Status);
    }

    [Fact]
    public void Only_the_applicant_can_withdraw()
    {
        var application = Submitted();

        Assert.Throws<DomainRuleException>(() => application.Withdraw("someone-else", Now));
    }

    [Fact]
    public void Claim_puts_a_submitted_application_under_review()
    {
        var application = Submitted();

        application.Claim(ManagerId, Now);

        Assert.Equal(ApplicationStatus.UnderReview, application.Status);
        Assert.True(application.IsClaimedBy(ManagerId));
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Returned)]
    [InlineData(ApplicationStatus.UnderReview)]
    public void Only_a_submitted_application_can_be_claimed(ApplicationStatus status)
    {
        var application = InStatus(status);

        Assert.Throws<DomainRuleException>(() => application.Claim("manager-2", Now));
    }

    [Fact]
    public void Release_returns_the_application_to_the_queue()
    {
        var application = UnderReview();

        application.Release(ManagerId, Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Null(application.ClaimedById);
    }

    [Fact]
    public void Review_must_be_claimed_first()
    {
        var application = Submitted();

        Assert.Throws<DomainRuleException>(() => application.Approve(ManagerId, application.Unit, Today, Now, null));
    }

    [Fact]
    public void Another_manager_cannot_complete_or_release_a_claimed_review()
    {
        var application = UnderReview();

        Assert.Throws<DomainRuleException>(() => application.Deny("manager-2", "No", Now));
        Assert.Throws<DomainRuleException>(() => application.Release("manager-2", Now));
        Assert.Equal(ApplicationStatus.UnderReview, application.Status);
    }

    [Fact]
    public void Approve_issues_a_twelve_month_lease_starting_today()
    {
        var application = UnderReview();
        var versionBefore = application.Unit.LeaseVersion;

        var lease = application.Approve(ManagerId, application.Unit, Today, Now, "Welcome");

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Same(lease, application.Lease);
        Assert.Equal(Today, lease.StartDate);
        Assert.Equal(new DateOnly(2027, 10, 22), lease.EndDate);
        Assert.Equal(ApplicantId, lease.TenantId);
        Assert.Equal(versionBefore + 1, application.Unit.LeaseVersion);
        Assert.Equal("Welcome", application.History[^1].Comment);
    }

    [Fact]
    public void Approve_is_rejected_when_the_unit_has_an_active_lease()
    {
        var unit = NewUnit();
        var application = UnderReview(unit);
        unit.Leases.Add(LeaseFrom(Today.AddDays(-1)));

        var error = Assert.Throws<DomainRuleException>(() => application.Approve(ManagerId, unit, Today, Now, null));
        Assert.Contains("active lease", error.Message);
        Assert.Null(application.Lease);
        Assert.Equal(ApplicationStatus.UnderReview, application.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Return_and_deny_require_a_comment(string? comment)
    {
        var application = UnderReview();

        Assert.Throws<DomainRuleException>(() => application.ReturnToApplicant(ManagerId, comment, Now));
        Assert.Throws<DomainRuleException>(() => application.Deny(ManagerId, comment, Now));
    }

    [Fact]
    public void Return_records_who_when_and_why()
    {
        var application = UnderReview();

        application.ReturnToApplicant(ManagerId, "  Add your landlord's phone.  ", Now);

        Assert.Equal(ApplicationStatus.Returned, application.Status);
        var last = application.History[^1];
        Assert.Equal(ManagerId, last.ChangedById);
        Assert.Equal(Now, last.ChangedAt);
        Assert.Equal("Add your landlord's phone.", last.Comment);
    }

    [Fact]
    public void Every_status_change_bumps_the_concurrency_version()
    {
        var application = ReadyToSubmit();
        var before = application.Version;

        application.Submit(ApplicantId, application.Unit, Today, Now);
        application.Claim(ManagerId, Now);

        Assert.Equal(before + 2, application.Version);
    }
}
