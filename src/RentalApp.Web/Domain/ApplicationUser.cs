using Microsoft.AspNetCore.Identity;

namespace RentalApp.Web.Domain;

// IdentityUser already has Id, UserName, Email, PasswordHash, PhoneNumber, etc.
// We inherit from it to add our own columns to the same users table.
public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }
}
