using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.Settings.GetOperationsSetupChecklist;
using Vixly.Operations.Application.UseCases.Settings.ListOperationsShippingServices;
using Vixly.Operations.Application.UseCases.Settings.SaveOperationsShippingServices;
using Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsSettingsUseCaseTests.cs
//
// PURPOSE:
// Proves the Slice 1E settings use cases and the staff-wide second factor.
//
// RESPONSIBILITIES:
// - Saving needs the permission; turning the second-factor rule off needs step-up
// - With the rule on, staff without an app must enrol at sign-in
// - The checklist follows what the tenant has done
//
// IMPORTANT NOTES:
// - Use cases with in-memory stores
// =========================================================
public sealed class OperationsSettingsUseCaseTests
{
    private readonly IdentityTestKit _kit = new();

    private readonly FakeRoleStore _roles = new();

    private readonly FakeShippingServiceStore _services = new();

    [Fact]
    public async Task StaffWideRule_MakesStaffWithoutAnAppEnrol()
    {
        OperationsUser staff = Staff("ada@northwind.example");

        BeginOperationsSignInResponse before = await Begin("ada@northwind.example");
        Assert.NotEmpty(before.SessionToken);

        _kit.Store.Settings = OperationsTenantSettings.CreateDefault(_kit.TenantId, _kit.Clock.GetUtcNow());
        _kit.Store.Settings.RequireSecondFactor(true, _kit.Clock.GetUtcNow());

        BeginOperationsSignInResponse after = await Begin("ada@northwind.example");
        Assert.Empty(after.SessionToken);
        Assert.NotEmpty(after.ChallengeToken);
        Assert.True(after.EnrolmentRequired);
        Assert.False(staff.RequiresSecondFactor);
    }

    [Fact]
    public async Task Saving_NeedsThePermission_AndAudits()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        OperationsUser clerk = Staff("ada@northwind.example");

        Assert.Equal("ops.access.permission_required", (await Update(As(clerk), secondFactor: false)).Error?.Code);
        Assert.True((await Update(As(owner), secondFactor: true)).Succeeded);
        Assert.True(_kit.Store.Settings!.RequireSecondFactorForAllStaff);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.settings.changed");
    }

    [Fact]
    public async Task TurningTheRuleOff_NeedsAFreshCode()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        OperationsAuthorizationContext context = As(owner);
        Assert.True((await Update(context, secondFactor: true)).Succeeded);

        _kit.Clock.Advance(TimeSpan.FromMinutes(10));

        Assert.True((await Update(context, secondFactor: true)).Succeeded);
        Assert.Equal("ops.access.step_up_required", (await Update(context, secondFactor: false)).Error?.Code);
        Assert.Equal("ops.access.step_up_required", (await Update(context, secondFactor: true, currency: "HTG")).Error?.Code);
        Assert.True(_kit.Store.Settings!.RequireSecondFactorForAllStaff);
    }

    [Fact]
    public async Task Checklist_IsForTheOwner_AndFollowsTheSettings()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        OperationsUser clerk = Staff("ada@northwind.example");

        Assert.Empty((await Checklist(As(clerk))).Items);

        GetOperationsSetupChecklistResponse first = await Checklist(As(owner));
        Assert.False(Assert.Single(first.Items, item => item.Code == "settings").Done);
        Assert.True(Assert.Single(first.Items, item => item.Code == "staff").Done);

        Assert.True((await Update(As(owner), secondFactor: false)).Succeeded);
        Assert.True(Assert.Single((await Checklist(As(owner))).Items, item => item.Code == "settings").Done);
    }

    [Fact]
    public async Task ShippingServices_AreShownAndSaved_InTheTenantUnits()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        OperationsAuthorizationContext context = As(owner);
        _kit.Store.Settings = OperationsTenantSettings.CreateDefault(_kit.TenantId, _kit.Clock.GetUtcNow());
        _kit.Store.Settings.ChangeWork(OperationsWeightUnit.Pound, OperationsDimensionUnit.Inch, false, false, OperationsDeliveryModes.Pickup, _kit.Clock.GetUtcNow());

        ListOperationsShippingServicesResponse first = await ListServices(context);
        Assert.Equal(5, first.Services.Count);
        Assert.Equal("in3/lb", first.DivisorUnit);
        Assert.Equal(166m, Assert.Single(first.Services, row => row.Code == "air_express").VolumetricDivisor);
        Assert.Null(Assert.Single(first.Services, row => row.Code == "ocean_full_container").VolumetricDivisor);
        Assert.DoesNotContain(first.Services, row => row.Offered);

        OperationsShippingServiceRow air = Assert.Single(first.Services, row => row.Code == "air_express") with { Offered = true, Label = "Express Miami", VolumetricDivisor = 139m };
        OperationsShippingServiceRow ocean = Assert.Single(first.Services, row => row.Code == "ocean_full_container") with { Offered = true, MinDays = 20, MaxDays = 30 };
        Assert.True((await SaveServices(context, air, ocean)).Succeeded);

        OperationsShippingServiceSetup saved = Assert.Single(_services.Setups, setup => setup.ServiceCode == "air_express");
        Assert.Equal(5021.694m, saved.VolumetricDivisor);
        Assert.Equal("Express Miami", saved.Label);
        Assert.Equal(139m, Assert.Single((await ListServices(context)).Services, row => row.Code == "air_express").VolumetricDivisor);
        Assert.True(Assert.Single((await Checklist(context)).Items, item => item.Code == "services").Done);

        Assert.Equal("ops.services.days_invalid", (await SaveServices(context, air with { MinDays = 9, MaxDays = 3 })).Error?.Code);
        Assert.Equal("ops.services.unknown", (await SaveServices(context, air with { Code = "teleport" })).Error?.Code);
        Assert.Equal("ops.services.divisor_invalid", (await SaveServices(context, air with { VolumetricDivisor = 0 })).Error?.Code);
    }

    private Task<ListOperationsShippingServicesResponse> ListServices(OperationsAuthorizationContext context) =>
        new ListOperationsShippingServicesUseCase(Access(), _services, _kit.Store, _kit.Clock).ExecuteAsync(context, new ListOperationsShippingServicesRequest(), CancellationToken.None);

    private Task<SaveOperationsShippingServicesResponse> SaveServices(OperationsAuthorizationContext context, params OperationsShippingServiceRow[] rows) =>
        new SaveOperationsShippingServicesUseCase(Access(), _services, _kit.Store, _kit.Audit, _kit.Store, _kit.Clock).ExecuteAsync(context, new SaveOperationsShippingServicesRequest { Services = rows }, CancellationToken.None);

    private OperationsUser Staff(string email)
    {
        OperationsUser user = OperationsUser.InviteStaff(_kit.TenantId, "Ada Lovelace", EmailAddress.Create(email), LanguageCode.En, _kit.Clock.GetUtcNow());
        user.AcceptInvitation(_kit.Services.Hash("a long enough password"), _kit.Clock.GetUtcNow());
        _kit.Store.Users.Add(user);
        return user;
    }

    // A session that confirmed its second factor now.
    private OperationsAuthorizationContext As(OperationsUser user)
    {
        OperationsUserSession session = OperationsUserSession.Start(_kit.TenantId, user.Id, new string('b', 64), "127.0.0.1", "test", false, _kit.Clock.GetUtcNow());
        _kit.Store.Sessions.Add(session);
        return _kit.Context.WithUser(user.Id, session.Id);
    }

    private OperationsAccess Access() => new(_kit.Store, _roles, _kit.Store, _kit.Clock, new FakeSiteStore(), new FakePlanFeatures());

    private Task<BeginOperationsSignInResponse> Begin(string email) =>
        _kit.Begin().ExecuteAsync(_kit.Context, new BeginOperationsSignInRequest { Email = email, Password = "a long enough password" }, CancellationToken.None);

    private Task<UpdateOperationsTenantSettingsResponse> Update(OperationsAuthorizationContext context, bool secondFactor, string currency = "USD") =>
        new UpdateOperationsTenantSettingsUseCase(new Fakes.FakeChargeStore(), new Fakes.FakeBillingStore(), Access(), _kit.Store, _kit.Audit, _kit.Store, _kit.Clock).ExecuteAsync(context, new UpdateOperationsTenantSettingsRequest
        {
            CurrencyCode = currency,
            WeightUnit = OperationsWeightUnit.Kilogram,
            DimensionUnit = OperationsDimensionUnit.Centimetre,
            DeliveryModes = OperationsDeliveryModes.Pickup,
            RequireSecondFactorForAllStaff = secondFactor,
            ShipmentPrefix = "SH",
            ReceivingPrefix = "RC",
            InvoicePrefix = "INV",
            PaymentPrefix = "PAY"
        }, CancellationToken.None);

    private Task<GetOperationsSetupChecklistResponse> Checklist(OperationsAuthorizationContext context) =>
        new GetOperationsSetupChecklistUseCase(Access(), _kit.Store, new FakeSiteStore(), _kit.Store, _kit.Store, _services).ExecuteAsync(context, new GetOperationsSetupChecklistRequest(), CancellationToken.None);
}
