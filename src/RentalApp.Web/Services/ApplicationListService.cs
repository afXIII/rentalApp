using System.ComponentModel;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Services;

// Query-string input of the application list endpoint. Bound with [FromQuery].
public class ApplicationListRequest
{
    public const int MaxPageSize = 100;

    [Description("Only applications in this status.")]
    public ApplicationStatus? Status { get; set; }

    [Description("Only applications for units of this property.")]
    public int? PropertyId { get; set; }

    [Description("Sort column: id, status, property, unit, applicant, created, submitted. Default: created.")]
    public string? Sort { get; set; }

    [Description("asc or desc. Default: desc.")]
    public string? Direction { get; set; }

    [Description("1-based page number.")]
    public int Page { get; set; } = 1;

    [Description("Rows per page, 1 to 100.")]
    public int PageSize { get; set; } = 10;
}

public record ApplicationListRow(
    int Id,
    ApplicationStatus Status,
    string StatusLabel,
    string PropertyName,
    string UnitNumber,
    string ApplicantName,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    string? ClaimedByName);

// One page of rows plus the total that matched the filters (for the pager).
public record PagedResult<T>(IReadOnlyList<T> Rows, int Total, int Page, int PageSize);

// Filtering, sorting, and paging all happen in SQL. Only one page of rows is ever loaded.
public class ApplicationListService(AppDbContext db)
{
    // Whitelist of sortable columns. Anything else falls back to the default, so the
    // query string can never sort by an arbitrary expression.
    private static readonly Dictionary<string, Expression<Func<RentalApplication, object?>>> SortColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = a => a.Id,
            ["status"] = a => a.Status,
            ["property"] = a => a.Unit.Property.Name,
            ["unit"] = a => a.Unit.UnitNumber,
            ["applicant"] = a => a.Applicant.FullName,
            ["created"] = a => a.CreatedAt,
            ["submitted"] = a => a.SubmittedAt,
        };

    public async Task<PagedResult<ApplicationListRow>> GetPageAsync(ApplicationListRequest request, string userId, bool isManager)
    {
        var query = db.RentalApplications.AsNoTracking();

        // Applicants see their own applications; property managers see all of them.
        if (!isManager)
        {
            query = query.Where(a => a.ApplicantId == userId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (request.PropertyId is { } propertyId)
        {
            query = query.Where(a => a.Unit.PropertyId == propertyId);
        }

        var total = await query.CountAsync();

        var sort = SortColumns.GetValueOrDefault(request.Sort ?? "", SortColumns["created"]);
        var descending = !string.Equals(request.Direction, "asc", StringComparison.OrdinalIgnoreCase);
        var ordered = descending ? query.OrderByDescending(sort) : query.OrderBy(sort);

        var pageSize = Math.Clamp(request.PageSize, 1, ApplicationListRequest.MaxPageSize);
        var page = Math.Max(1, request.Page);

        var rows = await ordered
            .ThenByDescending(a => a.Id) // stable order across pages when the sort column has ties
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Status,
                PropertyName = a.Unit.Property.Name,
                a.Unit.UnitNumber,
                ApplicantName = a.Applicant.FullName,
                a.CreatedAt,
                a.SubmittedAt,
                ClaimedByName = a.ClaimedBy != null ? a.ClaimedBy.FullName : null,
            })
            .ToListAsync();

        return new PagedResult<ApplicationListRow>(
            rows.Select(r => new ApplicationListRow(r.Id, r.Status, r.Status.DisplayName(), r.PropertyName, r.UnitNumber,
                r.ApplicantName, r.CreatedAt, r.SubmittedAt, r.ClaimedByName)).ToList(),
            total, page, pageSize);
    }
}
