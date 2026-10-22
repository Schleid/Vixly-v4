using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCustomerConfiguration.cs
//
// PURPOSE:
// Maps customers to ops.Customers (Slice 2).
//
// RESPONSIBILITIES:
// - Email and code unique per tenant; search indexes
//
// IMPORTANT NOTES:
// - The sign-in fields live on the customer row
// =========================================================
internal sealed class OperationsCustomerConfiguration : IEntityTypeConfiguration<OperationsCustomer>
{
    public void Configure(EntityTypeBuilder<OperationsCustomer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Code).HasMaxLength(OperationsCustomerLimits.CodeMaxLength).IsUnicode(false);
        builder.Property(item => item.FirstName).HasMaxLength(OperationsCustomerLimits.NameMaxLength).IsRequired();
        builder.Property(item => item.LastName).HasMaxLength(OperationsCustomerLimits.NameMaxLength).IsRequired();
        builder.Property(item => item.CompanyName).HasMaxLength(OperationsCustomerLimits.CompanyMaxLength);
        builder.Property(item => item.Email).HasConversion(OperationsValueConverters.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
        builder.Property(item => item.Phone).HasMaxLength(OperationsCustomerLimits.PhoneMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.PreferredLanguage).HasConversion(OperationsValueConverters.Language).HasMaxLength(8).IsRequired();
        builder.Property(item => item.StatusReason).HasMaxLength(OperationsCustomerLimits.ReasonMaxLength);
        builder.Property(item => item.CreditReason).HasMaxLength(OperationsCustomerLimits.ReasonMaxLength);
        builder.Property(item => item.PasswordHash).HasMaxLength(OperationsIdentityLimits.PasswordHashMaxLength).IsUnicode(false);
        builder.Ignore(item => item.FullName);
        builder.HasIndex(item => new { item.TenantId, item.Email }).IsUnique();
        // Review item 3: the check across companies (a work email is never a customer email) looks up by email alone.
        builder.HasIndex(item => item.Email).HasDatabaseName("IX_Customers_Email_AnyTenant");
        builder.HasIndex(item => new { item.TenantId, item.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
        builder.HasIndex(item => new { item.TenantId, item.LastName, item.FirstName });
        builder.HasIndex(item => new { item.TenantId, item.Phone });
    }
}
