using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Data.Configurations;

public class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        // Store the enum as its name ("Submitted"), not its number, so the table is readable.
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(a => a.Status);

        builder.Property(a => a.FullName).HasMaxLength(100);
        builder.Property(a => a.Phone).HasMaxLength(30);
        builder.Property(a => a.Email).HasMaxLength(256);
        builder.Property(a => a.CurrentAddress).HasMaxLength(300);
        builder.Property(a => a.ManagerNotes).HasMaxLength(4000);

        // Optimistic concurrency: EF puts the original value in the WHERE clause of every UPDATE.
        builder.Property(a => a.Version).IsConcurrencyToken();

        // A unit or user with applications cannot be deleted.
        builder.HasOne(a => a.Unit)
            .WithMany(u => u.Applications)
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Applicant)
            .WithMany()
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ClaimedBy)
            .WithMany()
            .HasForeignKey(a => a.ClaimedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Residences and history rows belong to the application and go with it.
        builder.HasMany(a => a.Residences)
            .WithOne(r => r.RentalApplication)
            .HasForeignKey(r => r.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.History)
            .WithOne(h => h.RentalApplication)
            .HasForeignKey(h => h.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ResidenceConfiguration : IEntityTypeConfiguration<Residence>
{
    public void Configure(EntityTypeBuilder<Residence> builder)
    {
        builder.Property(r => r.Address).HasMaxLength(300);
        builder.Property(r => r.LandlordName).HasMaxLength(100);
        builder.Property(r => r.LandlordPhone).HasMaxLength(30);
    }
}

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Comment).HasMaxLength(1000);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasOne(l => l.Unit)
            .WithMany(u => u.Leases)
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-one: an application produces at most one lease.
        // The unique index is the database-level guard against a double approval.
        builder.HasOne(l => l.RentalApplication)
            .WithOne(a => a.Lease)
            .HasForeignKey<Lease>(l => l.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Tenant)
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Availability lookups: "does this unit have a lease covering today?"
        builder.HasIndex(l => new { l.UnitId, l.StartDate, l.EndDate });
    }
}
