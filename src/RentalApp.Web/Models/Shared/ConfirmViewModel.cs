namespace RentalApp.Web.Models.Shared;

// Drives Views/Shared/_ConfirmModal.cshtml, used for deletes, withdrawals, and other one-click actions.
public class ConfirmViewModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string ActionUrl { get; init; }
    public string ButtonText { get; init; } = "Delete";
    public string ButtonClass { get; init; } = "btn-danger";
}
