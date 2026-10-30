using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCountryContactConfiguration.cs
//
// PURPOSE:
// Maps country contacts to ops.CountryContacts (Slice 1B).
//
// RESPONSIBILITIES:
// - Size the columns
//
// IMPORTANT NOTES:
// - Several contacts per country are allowed
// =========================================================
internal sealed class OperationsCountryContactConfiguration : IEntityTypeConfiguration<OperationsCountryContact>
{
    public void Configure(EntityTypeBuilder<OperationsCountryContact> builder)
    {
        builder.ToTable("CountryContacts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.Label).HasMaxLength(OperationsLocationLimits.LabelMaxLength);
        builder.Ignore(item => item.Phones);
        builder.Property<string>("PhonesText").HasColumnName("Phones")
            .HasMaxLength(OperationsLocationLimits.MaxPhonesPerContact * (OperationsLocationLimits.PhoneMaxLength + 1)).IsUnicode(false).IsRequired();
        builder.Property(item => item.WhatsApp).HasMaxLength(OperationsLocationLimits.PhoneMaxLength).IsUnicode(false);
        builder.Property(item => item.Email).HasMaxLength(OperationsLocationLimits.EmailMaxLength);
        builder.Property(item => item.Hours).HasMaxLength(OperationsLocationLimits.HoursMaxLength);
    }
}
