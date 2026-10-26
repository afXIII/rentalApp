# RentalApp

Rental applications for a property management company. Property managers maintain properties and units and review applications; applicants apply for available units, and an approval issues a twelve-month lease.

ASP.NET Core MVC on .NET 10, Razor views, partial views and view components, EF Core (code-first, SQL Server), ASP.NET Identity, Bogus, xUnit. Bootstrap and jQuery validation on the client; no SPA framework.

## Requirements

- .NET 10 SDK
- SQL Server: LocalDB, SQL Server Express, or a container

## Setup

### 1. Database

**Windows (LocalDB):** nothing to do. The default connection string in `src/RentalApp.Web/appsettings.json` is
`Server=(localdb)\MSSQLLocalDB;Database=RentalApp;Trusted_Connection=True;...`

**macOS / Linux:** run SQL Server in a container (Docker shown; Apple's `container` CLI takes the same flags). On Apple Silicon add `--platform linux/amd64` (and `--rosetta` with `container`).

```bash
docker run -d --name rentalapp-sql -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=RentalApp!Local1' \
  -p 1434:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Then point the app at it, without committing the password, using user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Default" \
  'Server=127.0.0.1,1434;Database=RentalApp;User Id=sa;Password=RentalApp!Local1;TrustServerCertificate=True;MultipleActiveResultSets=true' \
  --project src/RentalApp.Web
```

An environment variable works too: `ConnectionStrings__Default=...`.

### 2. Run

```bash
dotnet run --project src/RentalApp.Web
```

Open http://localhost:5155. On start the app creates the database, applies the migrations, and seeds demo data. Seeding is idempotent, so restarts do not duplicate anything. You do not need to run `dotnet ef database update`.

### 3. Test

```bash
dotnet test
```

The tests use in-memory SQLite, so they need no SQL Server.

## Seeded accounts

All seeded accounts use the password `Passw0rd!`.

| Email | Role | Notes |
|---|---|---|
| `manager@rentalapp.test` | Property manager | Has claimed at least one application in Under Review |
| `manager2@rentalapp.test`, `manager3@rentalapp.test` | Property manager | |
| `applicant@rentalapp.test` | Applicant | Has one application in **every** status |
| `applicant2@rentalapp.test` … `applicant10@rentalapp.test` | Applicant | |

You can also sign up and choose either role.

The seed data (generated with Bogus, using a fixed random seed) contains:
- Unit types Apartment, Studio, and Townhouse, plus Loft, which is **inactive** but still used by two units.
- 6 properties with 5 to 8 units each.
- 3 applications in each of the 7 statuses, with residences and a complete status history.
- Approvals with active leases (so those units are unavailable), and one old approval whose lease has ended (so that unit is available again).

## Where each requirement lives

| Requirement | Implementation |
|---|---|
| Roles and permissions | `[Authorize(Roles = ...)]` on controllers and actions, plus a global `AuthorizeFilter`. Ownership checks in `ApplicationsController` and `ResidencesController` (another user's application returns 404). Views show only the actions the role can take. |
| Sign up / log in / log out, role chosen at sign-up | `Controllers/AccountController.cs` |
| Properties and units through modals | `PropertiesController`, `UnitsController`, `Views/Properties/*`, `Views/Units/_UnitForm.cshtml` |
| Modal protocol (partial in → 422 + same partial on invalid → 204 then refresh region on success) | `Controllers/AppController.cs`, `wwwroot/js/site.js` |
| Unit Type lookup with inactive values, enforced on the server | `Domain/UnitType.IsSelectableFor`, `UnitsController.ValidateAsync`, `UnitTypesController` |
| Availability (no lease covering today) | `Domain/Unit.IsAvailableOn` (an expression translated to SQL), `AvailableUnitsController` |
| One-page application: one view model, one form, one action, the button decides | `ApplicationsController.Details` (GET/POST), `Models/Applications/ApplicationPageViewModel.cs`, `Views/Applications/Details.cshtml` |
| Sections: a partial view and a view component, editable or read-only decided on the server | `Views/Applications/_ApplicantInfoSection.cshtml`, `ViewComponents/ResidenceHistoryViewComponent.cs`, `_SummarySection.cshtml` |
| Residences through a modal | `ResidencesController` + `Views/Residences/_ResidenceForm.cshtml`. The list refreshes through the view component. |
| Status rules, terminal statuses, active-lease checks at submit and approval | `Domain/RentalApplication.cs` (the status machine), `Services/ApplicationWorkflow.cs` |
| Review modal (Approve / Return / Deny, comment required for Return and Deny) | `ReviewController.Complete`, `Views/Review/_ReviewForm.cshtml` |
| History of status changes (who, when, comment), managers only | `ViewComponents/StatusHistoryViewComponent.cs` |
| Application list filtered by status and property in the database | `Services/ApplicationListService.cs` |
| Startup migration and idempotent Bogus seeding | `Data/Seed/DatabaseInitializer.cs`, `Data/Seed/DataSeeder.cs` |
| Unit tests for business logic | `tests/RentalApp.Tests` (domain rules, workflow including a concurrent-approval race, list queries, seeder idempotency) |

## Bonus items

| # | Item | Status |
|---|---|---|
| 1 | Paging and sorting in the database; reusable grid view component over a JSON endpoint returning the page and the filtered total; OpenAPI | Done. `GET /api/applications` (`Controllers/Api/ApplicationsApiController.cs`), `ViewComponents/DataGridViewComponent.cs` + `wwwroot/js/grid.js`. The document is at `/openapi/v1.json` and Swagger UI at `/swagger` (Development). |
| 2 | Review queue: claim (Under Review) and release | Done. `ReviewController.Claim` / `Release`. Only the claiming manager can complete or release. "Review queue" in the nav is the list filtered to Submitted. |
| 3 | Property manager notes, never rendered or returned to applicants | Done. `ViewComponents/ManagerNotesViewComponent.cs`, `ReviewController.Notes`. Manager-only endpoints; the component renders nothing for other roles; the application page and the API are projections that never select the column. |
| 4 | Save invalid sections and list blocking errors on the Summary | Not done. The Summary does list what blocks submission (unsaved sections, no residences). |
| 5 | Multiple applicants per application with stale-save detection | Not done. The `Version` concurrency token on the application is the starting point for it. |

## Design decisions

- **The status machine lives on the entity.** `RentalApplication.Status` has a private setter. Every change goes through a method (`Submit`, `Withdraw`, `Claim`, `Release`, `Approve`, `ReturnToApplicant`, `Deny`) that checks the actor and the current status, writes a history row, and bumps a version. Controllers never set a status, and the rules are unit tested without a database. A broken rule throws `DomainRuleException`, which controllers show to the user.
- **Two approvals cannot both lease a unit.** The active-lease check runs at submit and at approval, but two managers approving different applications for the same unit at the same moment could both pass it. `Unit.LeaseVersion` is an optimistic concurrency token that every approval increments. The second save matches zero rows, EF throws `DbUpdateConcurrencyException`, the transaction rolls back, and the manager sees "changed at the same time, reload". This is covered by `Two_concurrent_approvals_for_the_same_unit_issue_only_one_lease`. Other open applications for a leased unit are left as they are; approving one later is refused.
- **The lease starts on the approval date** and ends the day before the anniversary (`Lease.EndDateFor`, including the Feb 29 case). Because every lease starts on its approval day, "a lease covering today" is enough to prevent overlap.
- **The server decides read-only vs editable.** `ApplicationPageBuilder` computes `CanEdit` (owner, and status Draft or Returned). The same partial and view component render inputs or plain text from it. POSTs check again in the domain, so a crafted request against a submitted application is rejected.
- **Continue / Back / Submit** are buttons named `command` in one form that posts to one action. Continue validates only the current section and saves it only when valid (Post/Redirect/Get). Back never saves and skips client validation. Submit is available only on the Summary, once both sections are saved; the domain re-checks this and the residence count.
- **The Unit Type rule is checked on POST, not just in the dropdown.** Options are the active types plus the unit's current type (labelled "inactive"). The POST rejects an inactive type unless it is the type the unit already has.
- **Read-side projections.** List, page, and table data are built with `Select` projections, so there is no N+1 and no over-fetching. This is also what keeps manager notes structurally out of everything an applicant receives.
- **Deletes keep history.** A unit or property with applications or leases cannot be removed; the modal explains why instead of failing on the foreign key.
- **Time.** Timestamps are stored in UTC (a model-wide converter restores `DateTimeKind.Utc` on read). "Today" for leases comes from an injected `TimeProvider`, which tests freeze.
- **Seeding goes through the domain.** Seeded applications are driven through the same methods the app uses, so their history, timestamps, and leases are consistent.

## Project layout

```
src/RentalApp.Web
  Controllers/        MVC controllers; Api/ holds the JSON endpoint
  Domain/             Entities and business rules (no framework dependencies beyond Identity's user)
  Data/               AppDbContext, EF configurations, migrations, seeding
  Services/           Use cases (ApplicationWorkflow) and read-side queries
  Models/             View models, grouped by feature
  ViewComponents/     ResidenceHistory, StatusHistory, ManagerNotes, DataGrid
  Views/              Razor views and partials
  wwwroot/js/         site.js (modal protocol), grid.js (data grid)
tests/RentalApp.Tests Domain, Services, and Data tests
LARAVEL-MAP.md        Personal notes mapping each .NET concept used here to its Laravel equivalent
```
