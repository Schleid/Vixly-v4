using Vixly.Operations.Application.Plans;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsPlanModulesTests.cs
//
// PURPOSE:
// Checks the modules a plan may leave out (Vixly Lite, owner 2026-10-30).
//
// RESPONSIBILITIES:
// - A module left out withholds its permissions and hides its staff pages
// - The plan gate answers for the company in scope; without one, everything is included
// - Without insurance: no products, an insured shipment refused; without home delivery: pickup only
//
// IMPORTANT NOTES:
// - Platform not reachable (Unknown features): every module stays on
// =========================================================
public sealed class OperationsPlanModulesTests
{
    private static readonly Guid Tenant = Guid.Parse("01930000-0000-7000-8000-0000000000d1");

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 15, 0, 0, TimeSpan.Zero);

    private static OperationsPlanFeatures Lite() =>
        OperationsPlanFeatures.All with { ModulesOff = OperationsPlanModules.All.ToHashSet(StringComparer.Ordinal) };

    [Fact]
    public void ModulesLeftOut_WithholdTheirPermissions_AndHideTheirPages()
    {
        IReadOnlySet<string> withheld = OperationsPlanModules.WithheldPermissions(Lite());
        Assert.Contains(OperationsPermissions.InsuranceManage, withheld);
        Assert.Contains(OperationsPermissions.CustomersCredit, withheld);
        Assert.Contains(OperationsPermissions.BillsApprove, withheld);
        Assert.DoesNotContain(OperationsPermissions.CustomersManage, withheld);
        Assert.Empty(OperationsPlanModules.WithheldPermissions(OperationsPlanFeatures.All));
        Assert.Empty(OperationsPlanModules.WithheldPermissions(OperationsPlanFeatures.Unknown));

        IReadOnlyList<string> hidden = OperationsPlanModules.HiddenPages(Lite());
        Assert.True(OperationsPlanModules.Hides(hidden, "/staff/insurance"));
        Assert.True(OperationsPlanModules.Hides(hidden, "/staff/credit/customers/1"));
        Assert.True(OperationsPlanModules.Hides(hidden, "/staff/settings/delivery"));
        Assert.False(OperationsPlanModules.Hides(hidden, "/staff/credits"));
        Assert.False(OperationsPlanModules.Hides(hidden, "/staff/customers"));
        Assert.False(OperationsPlanModules.Hides(OperationsPlanModules.HiddenPages(OperationsPlanFeatures.All), "/staff/insurance"));
    }

    [Fact]
    public async Task TheGate_AnswersForTheCompanyInScope()
    {
        FakePlanFeatures features = new();
        features.Tenants[Tenant] = Lite();
        FakeTenantScope scope = new();
        OperationsPlanGate gate = new(scope, features);

        Assert.True(await gate.IncludesAsync(OperationsPlanModules.Supplies, CancellationToken.None));

        scope.Enter(Tenant);
        Assert.False(await gate.IncludesAsync(OperationsPlanModules.Supplies, CancellationToken.None));

        features.Tenants[Tenant] = OperationsPlanFeatures.Unknown;
        Assert.True(await gate.IncludesAsync(OperationsPlanModules.Supplies, CancellationToken.None));
    }

    [Fact]
    public async Task WithoutInsuranceAndHomeDelivery_PickupOnly_AndNoInsuredShipment()
    {
        FakeIdentityStore store = new() { Settings = OperationsTenantSettings.CreateDefault(Tenant, Now.AddMonths(-1)) };
        store.Settings.ChangeWork(OperationsWeightUnit.Kilogram, OperationsDimensionUnit.Centimetre, false, false,
            OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery, Now);
        FakePlanFeatures features = new();
        FakeTenantScope scope = new();
        scope.Enter(Tenant);
        OperationsShipmentIntake intake = new(new FakeSiteStore(), store, new FakeChargeStore(), new FixedTimeProvider(Now), scope, features);

        Assert.Equal(OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery, await intake.DeliveryModesAsync(CancellationToken.None));

        features.Tenants[Tenant] = Lite();
        Assert.Equal(OperationsDeliveryModes.Pickup, await intake.DeliveryModesAsync(CancellationToken.None));
        Assert.Empty(await intake.InsuranceAsync(CancellationToken.None));

        OperationsShipmentDraft insured = new("pickup", Guid.NewGuid(), null, null, null, null, null, null, "Ana", "+13055550100", false, false, true, InsuranceAccepted: true);
        (OperationsShipmentInstructions? instructions, Shared.Kernel.Errors.Error? error) = await intake.InstructionsAsync(insured, CancellationToken.None);
        Assert.Null(instructions);
        Assert.Equal("ops.shipments.insurance_unavailable", error?.Code);

        (_, Shared.Kernel.Errors.Error? home) = await intake.InstructionsAsync(insured with { Delivery = "home", Insured = false, CountryCode = "US" }, CancellationToken.None);
        Assert.Equal("ops.shipments.delivery_mode_unavailable", home?.Code);
    }
}
