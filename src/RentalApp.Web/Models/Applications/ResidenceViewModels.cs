using System.ComponentModel.DataAnnotations;
using RentalApp.Web.Services;

namespace RentalApp.Web.Models.Applications;

public class ResidenceRowViewModel
{
    public int Id { get; init; }
    public required string Address { get; init; }
    public required string LandlordName { get; init; }
    public required string LandlordPhone { get; init; }
    public DateOnly MoveInDate { get; init; }
    public DateOnly MoveOutDate { get; init; }
}

public class ResidenceHistoryViewModel
{
    public int ApplicationId { get; init; }
    public bool IsEditable { get; init; }
    public required IReadOnlyList<ResidenceRowViewModel> Rows { get; init; }
}

public class ResidenceFormViewModel : IValidatableObject
{
    public int? Id { get; set; }
    public int ApplicationId { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Landlord name")]
    public string LandlordName { get; set; } = "";

    [Required, Phone, StringLength(30)]
    [Display(Name = "Landlord phone")]
    public string LandlordPhone { get; set; } = "";

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Move-in date")]
    public DateOnly? MoveInDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Move-out date")]
    public DateOnly? MoveOutDate { get; set; }

    // Rules that span fields. Runs after the attribute rules pass. Laravel: 'after:move_in_date'.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var clock = validationContext.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        var today = clock.Today();

        if (MoveInDate > MoveOutDate)
        {
            yield return new ValidationResult("Move-out must be on or after move-in.", [nameof(MoveOutDate)]);
        }

        if (MoveOutDate > today)
        {
            yield return new ValidationResult("A prior residence cannot end in the future.", [nameof(MoveOutDate)]);
        }
    }
}
