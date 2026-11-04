using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Insurance;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsInsurancePolicyConfiguration.cs
//
// PURPOSE:
// Maps the company's insurance policies to ops.InsurancePolicies (Slice 5B).
//
// RESPONSIBILITIES:
// - The documents as JSON inside the policy; amounts with two decimals
//
// IMPORTANT NOTES:
// - A row version, so two people changing a policy never overwrite each other
// =========================================================
internal sealed class OperationsInsurancePolicyConfiguration : IEntityTypeConfiguration<OperationsInsurancePolicy>
{
    public void Configure(EntityTypeBuilder<OperationsInsurancePolicy> builder)
    {
        builder.ToTable("InsurancePolicies");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.Name);

        foreach (string name in new[] { nameof(OperationsInsurancePolicy.InsurerName), nameof(OperationsInsurancePolicy.BrokerName) })
        {
            builder.Property(name).HasMaxLength(OperationsInsurancePolicy.NameMaxLength);
        }

        foreach (string contact in new[]
        {
            nameof(OperationsInsurancePolicy.InsurerEmail), nameof(OperationsInsurancePolicy.InsurerPhone), nameof(OperationsInsurancePolicy.ClaimsEmail),
            nameof(OperationsInsurancePolicy.ClaimsPhone), nameof(OperationsInsurancePolicy.BrokerEmail), nameof(OperationsInsurancePolicy.BrokerPhone)
        })
        {
            builder.Property(contact).HasMaxLength(OperationsInsurancePolicy.ContactMaxLength);
        }

        builder.Property(item => item.PolicyNumber).HasMaxLength(OperationsInsurancePolicy.NumberMaxLength);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(item => item.Territory).HasMaxLength(OperationsInsurancePolicy.TerritoryMaxLength);
        builder.Property(item => item.OtherExclusions).HasMaxLength(OperationsInsurancePolicy.TextMaxLength);
        builder.Property(item => item.Notes).HasMaxLength(OperationsInsurancePolicy.TextMaxLength);

        foreach (string amount in new[]
        {
            nameof(OperationsInsurancePolicy.LimitPerShipment), nameof(OperationsInsurancePolicy.LimitPerConveyance), nameof(OperationsInsurancePolicy.LimitPerLocation),
            nameof(OperationsInsurancePolicy.LimitPerYear), nameof(OperationsInsurancePolicy.DeductibleAmount), nameof(OperationsInsurancePolicy.DeductibleMinimum)
        })
        {
            builder.Property(amount).HasPrecision(14, 2);
        }

        builder.Property(item => item.DeductiblePercent).HasPrecision(6, 3);
        builder.OwnsMany(item => item.Documents, document =>
        {
            document.ToJson("Documents");
            document.Property(item => item.FileName).HasMaxLength(OperationsPolicyDocument.NameMaxLength);
        });
        builder.Navigation(item => item.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(item => new { item.TenantId, item.IsArchived, item.EndsOn });
    }
}
