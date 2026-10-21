using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsSettingsDomainTests.cs
//
// PURPOSE:
// Proves the tenant settings rules of Slice 1E.
//
// RESPONSIBILITIES:
// - Defaults, currency, units, delivery modes and prefixes
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsSettingsDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    public void Defaults_ArePaidFirst_PickupOnly_WithDistinctPrefixes()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        Assert.Equal("USD", settings.CurrencyCode);
        Assert.False(settings.AllowCollect);
        Assert.False(settings.AllowCredit);
        Assert.Equal(OperationsDeliveryModes.Pickup, settings.DeliveryModes);
        Assert.False(settings.RequireSecondFactorForAllStaff);
        Assert.Equal(4, new[] { OperationsNumberKind.Shipment, OperationsNumberKind.Receiving, OperationsNumberKind.Invoice, OperationsNumberKind.Payment }.Select(settings.Prefix).Distinct().Count());
    }

    [Fact]
    public void Currency_IsAnIsoCode_AndLocksOnceAnInvoiceExists()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        settings.ChangeCurrency(" htg ", false, Now);
        Assert.Equal("HTG", settings.CurrencyCode);
        Assert.Equal("ops.settings.currency_invalid", Assert.Throws<DomainException>(() => settings.ChangeCurrency("XYZ", false, Now)).Error.Code);
        Assert.Equal("ops.settings.currency_locked", Assert.Throws<DomainException>(() => settings.ChangeCurrency("EUR", true, Now)).Error.Code);

        settings.ChangeCurrency("HTG", true, Now);
        Assert.Equal("HTG", settings.CurrencyCode);
    }

    [Fact]
    public void Work_NeedsKnownUnits_AndAtLeastOneDeliveryMode()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        settings.ChangeWork(OperationsWeightUnit.Pound, OperationsDimensionUnit.Inch, true, false, OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery, Now);

        Assert.Equal(OperationsWeightUnit.Pound, settings.WeightUnit);
        Assert.True(settings.AllowCollect);
        Assert.Equal("ops.settings.delivery_mode_required", Assert.Throws<DomainException>(() =>
            settings.ChangeWork(OperationsWeightUnit.Kilogram, OperationsDimensionUnit.Centimetre, false, false, OperationsDeliveryModes.None, Now)).Error.Code);
        Assert.Equal("ops.settings.unit_invalid", Assert.Throws<DomainException>(() =>
            settings.ChangeWork((OperationsWeightUnit)9, OperationsDimensionUnit.Centimetre, false, false, OperationsDeliveryModes.Pickup, Now)).Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1SH")]
    [InlineData("SHIPMENT")]
    [InlineData("S-H")]
    public void Prefix_IsOneToSixLettersOrDigits_StartingWithALetter(string prefix)
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        Assert.Equal("ops.settings.prefix_invalid", Assert.Throws<DomainException>(() => settings.ChangePrefixes(prefix, "RC", "INV", "PAY", Now)).Error.Code);
    }

    [Fact]
    public void ShippingService_NeedsSaneDays_AndADivisorOnlyWhereItApplies()
    {
        OperationsShippingServiceSetup setup = OperationsShippingServiceSetup.Create(Tenant, "air_express", Now);

        setup.Change(true, "  Express  ", 2, 3, 6000.12345m, true, Now);
        Assert.Equal("Express", setup.Label);
        Assert.Equal(6000.123m, setup.VolumetricDivisor);

        Assert.Equal("ops.services.days_invalid", Assert.Throws<DomainException>(() => setup.Change(true, null, 0, 3, 6000, true, Now)).Error.Code);
        Assert.Equal("ops.services.days_invalid", Assert.Throws<DomainException>(() => setup.Change(true, null, 5, 3, 6000, true, Now)).Error.Code);
        Assert.Equal("ops.services.divisor_invalid", Assert.Throws<DomainException>(() => setup.Change(true, null, 2, 3, null, true, Now)).Error.Code);
        Assert.Equal("ops.services.divisor_invalid", Assert.Throws<DomainException>(() => setup.Change(true, null, 2, 3, 6000, false, Now)).Error.Code);
        Assert.Equal("ops.services.label_too_long", Assert.Throws<DomainException>(() => setup.Change(true, "<b>x</b>", 2, 3, 6000, true, Now)).Error.Code);
    }

    [Fact]
    public void Prefixes_AreUpperCased_AndMustDiffer()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        settings.ChangePrefixes("ship", "rcv1", "fac", "pmt", Now);

        Assert.Equal("SHIP", settings.Prefix(OperationsNumberKind.Shipment));
        Assert.Equal("RCV1", settings.Prefix(OperationsNumberKind.Receiving));
        Assert.Equal("ops.settings.prefixes_must_differ", Assert.Throws<DomainException>(() => settings.ChangePrefixes("A", "A", "B", "C", Now)).Error.Code);
    }

    [Fact]
    public void Prefixes_MaySwapInOneSave_ButNeverRepeat()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Guid.NewGuid(), DateTimeOffset.UtcNow);

        settings.ChangePrefixes("C", "RC", "INV", "PAY", "CU", DateTimeOffset.UtcNow);

        Assert.Equal("C", settings.Prefix(OperationsNumberKind.Shipment));
        Assert.Equal("CU", settings.Prefix(OperationsNumberKind.Customer));
        Assert.Throws<DomainException>(() => settings.ChangePrefixes("SH", "RC", "INV", "PAY", "SH", DateTimeOffset.UtcNow));
    }
}
