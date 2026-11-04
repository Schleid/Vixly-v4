using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsQuickSignIn;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithPassword;
using Vixly.Operations.Application.UseCases.SignIn.ListOperationsDevicePeople;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsQuickSignInTests.cs
//
// PURPOSE:
// Badge and PIN sign-in on a licensed device (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Only on a licensed device; the authenticator once a day; a revoked badge opens nothing
// - Five wrong PINs lock the PIN until a full sign-in; the daily password for a person without an authenticator
//
// IMPORTANT NOTES:
// - In-memory fakes; the device is the fake Platform's
// =========================================================
public sealed class OperationsQuickSignInTests
{
    private const string Password = "a long enough password";

    private readonly IdentityTestKit _kit = new();

    [Fact]
    public async Task Badge_AsksTheAuthenticatorOncePerDay_ThenSignsInAlone_AsAQuickSession()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        Guid device = _kit.Devices.Register("secret", null);
        string code = Badge(owner);

        BeginOperationsQuickSignInResponse first = await Scan(code, device);
        Assert.True(first.Succeeded);
        Assert.Empty(first.SessionToken);
        Assert.False(first.PasswordCheck);

        _kit.Clock.Advance(TimeSpan.FromSeconds(31));
        CompleteOperationsSignInWithAuthenticatorCodeResponse verified = await _kit.Verify().ExecuteAsync(_kit.Context,
            new CompleteOperationsSignInWithAuthenticatorCodeRequest { ChallengeToken = first.ChallengeToken, Code = Fakes.FakeIdentityServices.GoodCode }, CancellationToken.None);
        Assert.True(verified.Succeeded);
        Assert.False(Assert.Single(_kit.Store.Sessions).IsQuick);

        _kit.Clock.Advance(TimeSpan.FromHours(2));
        BeginOperationsQuickSignInResponse later = await Scan(" ]C0" + code.ToLowerInvariant() + " ", device);

        Assert.NotEmpty(later.SessionToken);
        OperationsUserSession quick = _kit.Store.Sessions.Last();
        Assert.True(quick.IsQuick);
        Assert.Equal(DateTimeOffset.MinValue, quick.SecondFactorUtc);
        Assert.Equal(device, quick.DeviceId);

        // The next day the authenticator is asked again.
        _kit.Clock.Advance(TimeSpan.FromDays(1));
        BeginOperationsQuickSignInResponse nextDay = await Scan(code, device);
        Assert.Empty(nextDay.SessionToken);
        Assert.NotEmpty(nextDay.ChallengeToken);
    }

    [Fact]
    public async Task Badge_WorksOnlyOnALicensedDevice_AndNeverOnceRevoked()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        Guid device = _kit.Devices.Register("secret", null);
        string code = Badge(owner);

        Assert.Equal("ops.auth.quick_device_required", (await _kit.Quick().ExecuteAsync(_kit.Context, new BeginOperationsQuickSignInRequest { Badge = code }, CancellationToken.None)).Error!.Code);
        Assert.Equal("ops.auth.quick_device_required", (await Scan(code, device, "wrong")).Error!.Code);
        Assert.Equal("ops.auth.quick_failed", (await Scan("SB" + new string('A', 25), device)).Error!.Code);

        Assert.True(_kit.Store.Badges.Single().Revoke(owner.Id, _kit.Clock.GetUtcNow()));
        Assert.Equal("ops.auth.quick_failed", (await Scan(code, device)).Error!.Code);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.auth.badge_refused");
        Assert.Empty(_kit.Store.Sessions);
    }

    [Fact]
    public async Task Badge_UsedFromAnotherCountry_BlocksTheDevice()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        Guid device = _kit.Devices.Register("secret", "US");
        _kit.Services.Country = "FR";

        Assert.Equal("ops.auth.device_blocked", (await Scan(Badge(owner), device)).Error!.Code);
        Assert.Single(_kit.Blocks.Blocks);
    }

    [Fact]
    public async Task FiveWrongPins_LockThePin_UntilAFullSignIn()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        Guid device = _kit.Devices.Register("secret", null);
        _kit.Store.Pins.Add(OperationsStaffPin.Set(_kit.TenantId, owner.Id, _kit.Services.Hash("2580"), _kit.Clock.GetUtcNow()));
        _kit.Store.DevicePeople.Add(OperationsDevicePerson.First(_kit.TenantId, device, owner.Id, _kit.Clock.GetUtcNow()));

        ListOperationsDevicePeopleResponse people = await _kit.DevicePeople().ExecuteAsync(_kit.Context, new ListOperationsDevicePeopleRequest { DeviceId = device, DeviceSecret = "secret" }, CancellationToken.None);
        Assert.Equal("Grace Hopper", Assert.Single(people.People).Name);

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            Assert.Equal("ops.auth.quick_failed", (await Pin(owner.Id, "1111", device)).Error!.Code);
        }

        Assert.Equal("ops.auth.pin_locked", (await Pin(owner.Id, "1111", device)).Error!.Code);
        Assert.Equal("ops.auth.pin_locked", (await Pin(owner.Id, "2580", device)).Error!.Code);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.auth.pin_locked");

        // A full sign-in (password and code) unlocks it.
        string challenge = await _kit.PasswordAsync();
        _kit.Clock.Advance(TimeSpan.FromSeconds(31));
        Assert.True((await _kit.Verify().ExecuteAsync(_kit.Context, new CompleteOperationsSignInWithAuthenticatorCodeRequest { ChallengeToken = challenge, Code = Fakes.FakeIdentityServices.GoodCode },
            CancellationToken.None)).Succeeded);
        Assert.False(_kit.Store.Pins.Single().IsLocked);
        Assert.True((await Pin(owner.Id, "2580", device)).Succeeded);
    }

    [Fact]
    public async Task WithoutAnAuthenticator_AndMfaOff_TheDailyCheckIsThePassword()
    {
        OperationsUser staff = OperationsUser.InviteStaff(_kit.TenantId, "Ada Lovelace", EmailAddress.Create("ada@northwind.example"), LanguageCode.En, _kit.Clock.GetUtcNow());
        staff.AcceptInvitation(_kit.Services.Hash(Password), _kit.Clock.GetUtcNow());
        _kit.Store.Users.Add(staff);
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(_kit.TenantId, _kit.Clock.GetUtcNow());
        settings.ChangeRequireMfa(false, _kit.Clock.GetUtcNow());
        _kit.Store.Settings = settings;
        Guid device = _kit.Devices.Register("secret", null);

        BeginOperationsQuickSignInResponse begun = await Scan(Badge(staff), device);
        Assert.True(begun.PasswordCheck);

        CompleteOperationsSignInWithPasswordUseCase check = _kit.PasswordCheck();
        Assert.Equal("ops.auth.password_wrong", (await check.ExecuteAsync(_kit.Context, new CompleteOperationsSignInWithPasswordRequest { ChallengeToken = begun.ChallengeToken, Password = "nope" },
            CancellationToken.None)).Error!.Code);
        CompleteOperationsSignInWithPasswordResponse done = await check.ExecuteAsync(_kit.Context,
            new CompleteOperationsSignInWithPasswordRequest { ChallengeToken = begun.ChallengeToken, Password = Password }, CancellationToken.None);

        Assert.True(done.Succeeded);
        Assert.True(_kit.Store.DevicePeople.Single().FullSignInOn(DateOnly.FromDateTime(_kit.Clock.GetUtcNow().UtcDateTime), null));
    }

    [Theory]
    [InlineData("2580", true)]
    [InlineData("1234", false)]
    [InlineData("9876", false)]
    [InlineData("7777", false)]
    [InlineData("123", false)]
    [InlineData("12a4", false)]
    public void Pin_MustBeFourDigits_NotARun(string pin, bool acceptable) => Assert.Equal(acceptable, OperationsQuickSignIn.IsAcceptablePin(pin));

    [Fact]
    public void BadgeCodes_AreTidiedFromScanners()
    {
        string code = OperationsBadgeCode.New();

        Assert.Equal(OperationsBadgeCode.Length, code.Length);
        Assert.Equal(code, OperationsBadgeCode.Tidy("]C0" + code.ToLowerInvariant()));
        Assert.Null(OperationsBadgeCode.Tidy("PK2026000001"));
        Assert.Null(OperationsBadgeCode.Tidy(code[..^1] + "0"));
    }

    private string Badge(OperationsUser user)
    {
        string code = OperationsBadgeCode.New();
        _kit.Store.Badges.Add(OperationsStaffBadge.Issue(_kit.TenantId, user.Id, _kit.Services.HashToken(code), user.Id, _kit.Clock.GetUtcNow()));
        return code;
    }

    private Task<BeginOperationsQuickSignInResponse> Scan(string code, Guid device, string secret = "secret") =>
        _kit.Quick().ExecuteAsync(_kit.Context, new BeginOperationsQuickSignInRequest { Badge = code, DeviceId = device, DeviceSecret = secret }, CancellationToken.None);

    private Task<BeginOperationsQuickSignInResponse> Pin(Guid userId, string pin, Guid device) =>
        _kit.Quick().ExecuteAsync(_kit.Context, new BeginOperationsQuickSignInRequest { UserId = userId, Pin = pin, DeviceId = device, DeviceSecret = "secret" }, CancellationToken.None);
}
