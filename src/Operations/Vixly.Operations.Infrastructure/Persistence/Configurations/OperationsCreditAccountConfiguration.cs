using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCreditAccountConfiguration.cs
//
// PURPOSE:
// Maps OperationsCreditAccount to the database (Slice 5F step 2b).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - One account per customer
// =========================================================
internal sealed class OperationsCreditAccountConfiguration : IEntityTypeConfiguration<OperationsCreditAccount>
{
    public void Configure(EntityTypeBuilder<OperationsCreditAccount> builder)
    {
        builder.ToTable("CreditAccounts");
        builder.HasKey(account => account.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(account => account.Limit).HasPrecision(14, 2);
        builder.Property(account => account.Reason).HasMaxLength(OperationsBillingLimits.ReasonMaxLength);
        builder.Property(account => account.StatementMonth).HasMaxLength(7).IsUnicode(false);
        builder.Ignore(account => account.IsActive);
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(account => account.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(account => new { account.TenantId, account.CustomerId }).IsUnique();
    }
}
