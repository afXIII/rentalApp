using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Data.Seed;

// Demo data generated with Bogus. Laravel: a DatabaseSeeder calling model factories.
//
// Idempotent: every step checks what already exists before inserting, so it runs on every
// start without creating duplicates. Lookups and users are matched by name / email. Properties
// and applications are seeded only into empty tables, because a manager may have edited them.
//
// Applications are driven through the same domain methods the app uses (Submit, Claim,
// Approve, ...), so every seeded status has a consistent history, and approvals have leases.
public class DataSeeder(AppDbContext db, UserManager<ApplicationUser> userManager, TimeProvider clock)
{
    public const string DemoPassword = "Passw0rd!";
    public const string DemoManagerEmail = "manager@rentalapp.test";
    public const string DemoApplicantEmail = "applicant@rentalapp.test";

    private const int ApplicationsPerStatus = 3;

    private static readonly (string Name, bool IsActive)[] UnitTypeLookups =
    [
        ("Apartment", true),
        ("Studio", true),
        ("Townhouse", true),
        ("Loft", false), // inactive: still shown on the units that have it, not selectable for others
    ];

    private static readonly string[] ReturnComments =
    [
        "Please add the landlord phone number for your previous address.",
        "Your residence history has a gap. Please add the missing address.",
        "The current address is incomplete. Please include the unit number.",
    ];

    private static readonly string[] DenyComments =
    [
        "Income does not meet the requirement of three times the monthly rent.",
        "We could not verify the rental history provided.",
        "The unit was offered to an applicant earlier in the queue.",
    ];

    // Fixed seed: the same names, addresses, and numbers on every machine.
    private readonly Faker faker = new("en") { Random = new Randomizer(20261023) };

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task SeedAsync()
    {
        await SeedUnitTypesAsync();
        var managers = await SeedUsersAsync(Roles.PropertyManager, DemoManagerEmail, "manager", count: 3);
        var applicants = await SeedUsersAsync(Roles.Applicant, DemoApplicantEmail, "applicant", count: 10);

        if (!await db.Properties.AnyAsync())
        {
            await SeedPropertiesAsync();
        }

        if (!await db.RentalApplications.AnyAsync())
        {
            await SeedApplicationsAsync(managers, applicants);
        }
    }

    private async Task SeedUnitTypesAsync()
    {
        var existing = await db.UnitTypes.Select(t => t.Name).ToListAsync();
        foreach (var (name, isActive) in UnitTypeLookups.Where(t => !existing.Contains(t.Name)))
        {
            db.UnitTypes.Add(new UnitType { Name = name, IsActive = isActive });
        }

        await db.SaveChangesAsync();
    }

    private async Task<List<ApplicationUser>> SeedUsersAsync(string role, string demoEmail, string prefix, int count)
    {
        var users = new List<ApplicationUser>();
        for (var i = 1; i <= count; i++)
        {
            var email = i == 1 ? demoEmail : $"{prefix}{i}@rentalapp.test";
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = faker.Name.FullName(),
                    PhoneNumber = faker.Phone.PhoneNumber("(###) ###-####"),
                };
                EnsureSucceeded(await userManager.CreateAsync(user, DemoPassword));
                EnsureSucceeded(await userManager.AddToRoleAsync(user, role));
            }

            users.Add(user);
        }

        return users;
    }

    private async Task SeedPropertiesAsync()
    {
        var types = await db.UnitTypes.ToListAsync();
        var activeTypes = types.Where(t => t.IsActive).ToList();
        var loft = types.Single(t => t.Name == "Loft");
        string[] suffixes = ["Apartments", "Residences", "Court", "Commons", "Place", "Gardens"];

        for (var p = 0; p < 6; p++)
        {
            var property = new Property
            {
                Name = $"{faker.Address.StreetName()} {suffixes[p]}",
                AddressLine = faker.Address.StreetAddress(),
                City = faker.Address.City(),
                State = faker.Address.StateAbbr(),
                PostalCode = faker.Address.ZipCode("#####"),
            };

            var unitCount = faker.Random.Int(5, 8);
            for (var u = 0; u < unitCount; u++)
            {
                // The first unit of the first two properties keeps the now-inactive Loft type.
                var type = u == 0 && p < 2 ? loft : faker.PickRandom(activeTypes);
                property.Units.Add(new Unit
                {
                    UnitNumber = $"{(u / 4) + 1}{(u % 4) + 1:00}", // 101, 102, ... 201, 202
                    Bedrooms = type.Name == "Studio" ? 0 : faker.Random.Int(1, 3),
                    MonthlyRent = Math.Round(faker.Random.Decimal(900, 3200) / 25) * 25,
                    UnitType = type,
                });
            }

            db.Properties.Add(property);
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedApplicationsAsync(List<ApplicationUser> managers, List<ApplicationUser> applicants)
    {
        // One unit per application keeps the seeded leases free of conflicts.
        var units = new Queue<Unit>(faker.Random.Shuffle(await db.Units.Include(u => u.Leases).ToListAsync()));
        var demoApplicant = applicants[0];
        var demoManager = managers[0];

        foreach (var status in Enum.GetValues<ApplicationStatus>())
        {
            for (var i = 0; i < ApplicationsPerStatus; i++)
            {
                // The demo applicant gets one application in every status.
                var applicant = i == 0 ? demoApplicant : faker.PickRandom(applicants.Skip(1));
                var manager = i == 0 ? demoManager : faker.PickRandom(managers);
                var createdAt = Now.AddDays(-faker.Random.Int(10, 60));
                db.RentalApplications.Add(BuildApplication(status, variant: i, units.Dequeue(), applicant, manager, createdAt));
            }
        }

        // An approval from last year whose lease has ended: its unit is available again.
        db.RentalApplications.Add(BuildApplication(
            ApplicationStatus.Approved, variant: 0, units.Dequeue(), faker.PickRandom(applicants), demoManager,
            Now.AddMonths(-Lease.TermMonths - 3)));

        await db.SaveChangesAsync();
    }

    private RentalApplication BuildApplication(
        ApplicationStatus target, int variant, Unit unit, ApplicationUser applicant, ApplicationUser manager, DateTime createdAt)
    {
        var application = RentalApplication.Start(unit.Id, applicant.Id, createdAt);
        application.PrefillApplicantInfo(applicant.FullName, applicant.PhoneNumber, applicant.Email);

        // Drafts at different stages: just started, section 1 saved, both sections saved.
        if (target == ApplicationStatus.Draft && variant == 0)
        {
            return application;
        }

        application.SaveApplicantInfo(applicant.Id, applicant.FullName, applicant.PhoneNumber!, applicant.Email!,
            faker.Address.FullAddress(), createdAt.AddMinutes(10));
        if (target == ApplicationStatus.Draft && variant == 1)
        {
            return application;
        }

        AddResidences(application, DateOnly.FromDateTime(createdAt));
        application.SaveResidenceHistory(applicant.Id, createdAt.AddMinutes(20));
        if (target == ApplicationStatus.Draft)
        {
            return application;
        }

        // One withdrawal happens before submitting.
        if (target == ApplicationStatus.Withdrawn && variant == 2)
        {
            application.Withdraw(applicant.Id, createdAt.AddDays(1));
            return application;
        }

        var submittedAt = createdAt.AddMinutes(30);
        application.Submit(applicant.Id, unit, DateOnly.FromDateTime(submittedAt), submittedAt);

        if (variant == 1 && target is ApplicationStatus.Submitted or ApplicationStatus.UnderReview)
        {
            application.ManagerNotes = "Called the previous landlord, waiting to hear back.";
        }

        if (target == ApplicationStatus.Submitted)
        {
            return application;
        }

        if (target == ApplicationStatus.Withdrawn)
        {
            application.Withdraw(applicant.Id, submittedAt.AddDays(1));
            return application;
        }

        var claimedAt = submittedAt.AddDays(1);
        application.Claim(manager.Id, claimedAt);
        var reviewedAt = claimedAt.AddHours(3);

        switch (target)
        {
            case ApplicationStatus.Approved:
                // The lease starts on the approval day, so recent approvals lease their unit today.
                application.Approve(manager.Id, unit, DateOnly.FromDateTime(reviewedAt), reviewedAt, "Welcome home!");
                break;
            case ApplicationStatus.Returned:
                application.ReturnToApplicant(manager.Id, faker.PickRandom(ReturnComments), reviewedAt);
                break;
            case ApplicationStatus.Denied:
                application.Deny(manager.Id, faker.PickRandom(DenyComments), reviewedAt);
                break;
        }

        return application;
    }

    // One to three prior residences, back to back, ending before the application was created.
    private void AddResidences(RentalApplication application, DateOnly before)
    {
        var moveOut = before.AddDays(-faker.Random.Int(0, 30));
        for (var i = faker.Random.Int(1, 3); i > 0; i--)
        {
            var moveIn = moveOut.AddMonths(-faker.Random.Int(8, 36));
            application.Residences.Add(new Residence
            {
                Address = faker.Address.FullAddress(),
                LandlordName = faker.Name.FullName(),
                LandlordPhone = faker.Phone.PhoneNumber("(###) ###-####"),
                MoveInDate = moveIn,
                MoveOutDate = moveOut,
            });
            moveOut = moveIn.AddDays(-1);
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Seeding a user failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
