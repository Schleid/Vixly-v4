using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsRequireMfaTests.cs
//
// PURPOSE:
// The company's Require MFA (owner, 2026-10-22): Yes by default;
// with No, nobody is made to set up an authenticator, people
// without one confirm with the password, people with one keep
// using it; only the Owner or an Administrator changes it, after
// confirming.
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
public sealed class OperationsRequireMfaTests
{
    private const string Password = "a long enough password";

    private readonly IdentityTestKit _kit = new();

    private readonly FakeRoleStore _roles = new();

    [Fact]
    public async Task ByDefault_TheOwnerWithoutAnApp_MustSetOneUp()
    {
        _kit.Owner(withApp: false);

        BeginOperationsSignInResponse response = await Begin();

        Assert.True(response.EnrolmentRequired);
        Assert.Empty(response.SessionToken);
    }

    [Fact]
    public async Task RequireMfaNo_TheOwnerWithoutAnApp_SignsInWithThePassword()
    {
        RequireMfa(false);
        _kit.Owner(withApp: false);

        BeginOperationsSignInResponse response = await Begin();

        Assert.True(response.Succeeded);
        Assert.NotEmpty(response.SessionToken);
        Assert.Single(_kit.Store.Sessions);
    }

    [Fact]
    public async Task RequireMfaNo_TheOwnerWithAnApp_StillUsesIt()
    {
        RequireMfa(false);
        _kit.Owner(withApp: true);

        BeginOperationsSignInResponse response = await Begin();

        Assert.Empty(response.SessionToken);
        Assert.NotEmpty(response.ChallengeToken);
        Assert.False(response.EnrolmentRequired);
    }

    [Fact]
    public async Task RequireMfaNo_StaffWideRuleDoesNotApply()
    {
        RequireMfa(false);
        _kit.Store.Settings!.RequireSecondFactor(true, _kit.Clock.GetUtcNow());
        _kit.Owner(withApp: false);

        Assert.NotEmpty((await Begin()).SessionToken);
    }

    [Fact]
    public async Task StepUp_WithThePassword_OnlyWhenRequireMfaIsNo_AndNoApp()
    {
        OperationsUser owner = _kit.Owner(withApp: false);
        OperationsStepUp stepUp = new(_kit.Services, _kit.Clock, _kit.Store, _kit.Services);

        Assert.False(await stepUp.PasswordConfirmsAsync(owner, CancellationToken.None));
        Assert.Equal("ops.auth.code_invalid", (await stepUp.CheckAsync(owner, Password, CancellationToken.None))?.Code);

        RequireMfa(false);

        Assert.True(await stepUp.PasswordConfirmsAsync(owner, CancellationToken.None));
        Assert.Equal("ops.auth.code_invalid", (await stepUp.CheckAsync(owner, "not the password at all", CancellationToken.None))?.Code);
        Assert.Null(await stepUp.CheckAsync(owner, Password, CancellationToken.None));
    }

    [Fact]
    public async Task StepUp_SomeoneWithAnApp_CannotUseThePassword()
    {
        RequireMfa(false);
        OperationsUser owner = _kit.Owner(withApp: true);
        OperationsStepUp stepUp = new(_kit.Services, _kit.Clock, _kit.Store, _kit.Services);

        Assert.False(await stepUp.PasswordConfirmsAsync(owner, CancellationToken.None));
        Assert.Equal("ops.auth.code_invalid", (await stepUp.CheckAsync(owner, Password, CancellationToken.None))?.Code);
    }

    [Fact]
    public async Task SensitiveActions_WithoutAnApp_AreAllowedOnlyWhenRequireMfaIsNo()
    {
        OperationsUser owner = _kit.Owner(withApp: false);
        OperationsAuthorizationContext context = Fresh(owner);

        Assert.Equal("ops.access.step_up_required", (await Access().RequireAsync(context, OperationsPermissions.SettingsEdit, true, CancellationToken.None)).Refused?.Code);

        RequireMfa(false);

        Assert.Null((await Access().RequireAsync(context, OperationsPermissions.SettingsEdit, true, CancellationToken.None)).Refused);
    }

    [Fact]
    public async Task OnlyTheOwnerOrAnAdministrator_ChangesIt_AfterConfirming()
    {
        OperationsUser editor = Editor();
        Assert.Equal("ops.access.permission_required", (await Update(Fresh(editor), false)).Error?.Code);
        Assert.True(_kit.Store.Settings?.RequireMfa ?? true);

        OperationsUser owner = _kit.Owner(withApp: true);
        OperationsAuthorizationContext stale = Fresh(owner);
        _kit.Clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal("ops.access.step_up_required", (await Update(stale, false)).Error?.Code);

        Assert.True((await Update(Fresh(owner), false)).Succeeded);
        Assert.False(_kit.Store.Settings!.RequireMfa);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.settings.changed");

        Assert.True((await Update(Fresh(owner), null)).Succeeded);
        Assert.False(_kit.Store.Settings.RequireMfa);
    }

    private void RequireMfa(bool value)
    {
        _kit.Store.Settings ??= OperationsTenantSettings.CreateDefault(_kit.TenantId, _kit.Clock.GetUtcNow());
        _kit.Store.Settings.ChangeRequireMfa(value, _kit.Clock.GetUtcNow());
    }

    private Task<BeginOperationsSignInResponse> Begin() =>
        _kit.Begin().ExecuteAsync(_kit.Context, new BeginOperationsSignInRequest { Email = "grace@northwind.example", Password = Password }, CancellationToken.None);

    // A staff member with the settings right through a role, and an authenticator app.
    private OperationsUser Editor()
    {
        DateTimeOffset now = _kit.Clock.GetUtcNow();
        OperationsUser user = OperationsUser.InviteStaff(_kit.TenantId, "Ada Lovelace", EmailAddress.Create("ada@northwind.example"), LanguageCode.En, now);
        user.AcceptInvitation(_kit.Services.Hash(Password), now);
        user.BeginAuthenticatorEnrolment("enc-app", now);
        user.ConfirmAuthenticatorApp(1, now);
        _kit.Store.Users.Add(user);
        OperationsRole role = OperationsRole.Create(_kit.TenantId, "Settings", new[] { OperationsPermissions.SettingsEdit }, now);
        _roles.Add(role);
        _roles.Add(OperationsRoleGrant.Grant(_kit.TenantId, user.Id, role.Id, OperationsRoleScope.Tenant, null, null, now));
        return user;
    }

    // A session that confirmed its second factor now.
    private OperationsAuthorizationContext Fresh(OperationsUser user)
    {
        OperationsUserSession session = OperationsUserSession.Start(_kit.TenantId, user.Id, Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), "127.0.0.1", "test", false, _kit.Clock.GetUtcNow());
        _kit.Store.Sessions.Add(session);
        return _kit.Context.WithUser(user.Id, session.Id);
    }

    private OperationsAccess Access() => new(_kit.Store, _roles, _kit.Store, _kit.Clock, new FakeSiteStore(), new FakePlanFeatures(), _kit.Store);

    private Task<UpdateOperationsTenantSettingsResponse> Update(OperationsAuthorizationContext context, bool? requireMfa) =>
        new UpdateOperationsTenantSettingsUseCase(new FakeChargeStore(), new FakeBillingStore(), Access(), _kit.Store, _kit.Audit, _kit.Store, _kit.Clock, new FakeIdleTimes()).ExecuteAsync(context, new UpdateOperationsTenantSettingsRequest
        {
            CurrencyCode = "USD",
            WeightUnit = OperationsWeightUnit.Kilogram,
            DimensionUnit = OperationsDimensionUnit.Centimetre,
            DeliveryModes = OperationsDeliveryModes.Pickup,
            RequireMfa = requireMfa,
            ShipmentPrefix = "SH",
            ReceivingPrefix = "RC",
            InvoicePrefix = "INV",
            PaymentPrefix = "PAY"
        }, CancellationToken.None);
}
