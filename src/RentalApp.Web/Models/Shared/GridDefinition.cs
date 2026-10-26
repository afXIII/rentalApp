using Microsoft.AspNetCore.Mvc.Rendering;

namespace RentalApp.Web.Models.Shared;

// Describes a data grid for the DataGrid view component. The component renders the
// filters, header, and pager. wwwroot/js/grid.js fetches pages of rows from DataUrl, which
// must accept filter names plus sort, direction, page, pageSize, and return { rows, total }.
public class GridDefinition
{
    public required string Id { get; init; }
    public required string DataUrl { get; init; }
    public required IReadOnlyList<GridColumn> Columns { get; init; }
    public IReadOnlyList<GridFilter> Filters { get; init; } = [];
    public string DefaultSort { get; init; } = "";
    public string DefaultDirection { get; init; } = "desc";
    public int PageSize { get; init; } = 10;

    // Optional link for the first column, with {field} placeholders, e.g. "/Applications/Details/{id}".
    public string? RowLink { get; init; }
    public string EmptyText { get; init; } = "Nothing found.";
}

// Format: "text" (default), "date", or "status" (renders the status badge).
// Key is the JSON field to display. SortKey is the value sent as ?sort= (null: not sortable).
public record GridColumn(string Key, string Title, string? SortKey = null, string Format = "text");

public record GridFilter(string Name, string Label, IReadOnlyList<SelectListItem> Options, string? Selected = null);
