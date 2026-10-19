using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Data.Configurations;

public class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(50);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(100);
        builder.Property(p => p.AddressLine).HasMaxLength(200);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.State).HasMaxLength(50);
        builder.Property(p => p.PostalCode).HasMaxLength(20);

        // Deleting a property deletes its units, unless a unit is protected by the Restrict rules below.
        builder.HasMany(p => p.Units)
            .WithOne(u => u.Property)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.Property(u => u.UnitNumber).HasMaxLength(20);

        // Rent: decimal dollars in C#, integer cents in the database.
        builder.Property(u => u.MonthlyRent)
            .HasConversion(new MoneyToCentsConverter())
            .HasColumnName("MonthlyRentCents");

        // Bumped on every approval for this unit, so two approvals cannot both issue a lease.
        builder.Property(u => u.LeaseVersion).IsConcurrencyToken();

        // A unit number is unique within its property.
        builder.HasIndex(u => new { u.PropertyId, u.UnitNumber }).IsUnique();

        // A unit type that is in use cannot be deleted.
        builder.HasOne(u => u.UnitType)
            .WithMany()
            .HasForeignKey(u => u.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
