using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsAddressMapping.cs
//
// PURPOSE:
// Maps a postal address into its owner table.
//
// RESPONSIBILITIES:
// - Name and size the address columns
//
// IMPORTANT NOTES:
// - Shared by branches and warehouses
// =========================================================
internal static class OperationsAddressMapping
{
    public static void Map<TOwner>(OwnedNavigationBuilder<TOwner, OperationsPostalAddress> address)
        where TOwner : class
    {
        address.Property(item => item.Line1).HasColumnName("Address_Line1").HasMaxLength(OperationsLocationLimits.AddressLineMaxLength).IsRequired();
        address.Property(item => item.Line2).HasColumnName("Address_Line2").HasMaxLength(OperationsLocationLimits.AddressLineMaxLength);
        address.Property(item => item.City).HasColumnName("Address_City").HasMaxLength(OperationsLocationLimits.CityMaxLength).IsRequired();
        address.Property(item => item.Region).HasColumnName("Address_Region").HasMaxLength(OperationsLocationLimits.RegionMaxLength);
        address.Property(item => item.PostalCode).HasColumnName("Address_PostalCode").HasMaxLength(OperationsLocationLimits.PostalCodeMaxLength);
        address.Property(item => item.CountryCode).HasColumnName("Address_CountryCode").HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
    }
}
