using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.SignIn.AcceptOperationsInvitation;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithRecoveryCode;
using Vixly.Operations.Application.UseCases.SignIn.SignOutOperations;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsSignInUseCaseTests.cs
//
// PURPOSE:
// Proves staff sign-in (Slice 1A).
//
// RESPONSIBILITIES:
// - An Owner never signs in with a password alone
// - Wrong email and wrong password look the same; five wrong passwords lock
// - Codes, recovery codes and enrolment
// - Invitation, sign-out, idle sign-out and setup sessions
//
// IMPORTANT NOTES:
// - In-memory fakes; database behaviour is in Infrastructure tests
// =========================================================
public sealed class OperationsSignInUseCaseTests
{
    private const string Password = "a long enough password";

    private readonly IdentityTestKit _kit = new();

    [Fact]
    public async Task Owner_WithAnApp_NeedsTheCode()
    {
        _kit.Owner(withApp: true);

        BeginOperationsSignInResponse response = await Begin(Password);

        Assert.True(response.Succeeded);
        Assert.False(response.EnrolmentRequired);
        Assert.Empty(response.SessionToken);
        Assert.Empty(_kit.Store.Sessions);
    }

    [Fact]
    public async Task Owner_WithoutAnApp_MustEnrolFirst()
    {
        _kit.Owner(withApp: false);

        BeginOperationsSignInResponse response = await Begin(Password);

        Assert.True(response.EnrolmentRequired);
        Assert.Empty(_kit.Store.Sessions);
    }

    [Fact]
    public async Task WrongEmail_AndWrongPassword_LookTheSame()
    {
        _kit.Owner(withApp: true);

        BeginOperationsSignInResponse wrongPassword = await Begin("not the password at all");
        BeginOperationsSignInResponse wrongEmail = await _kit.Begin().ExecuteAsync(_kit.Context, new BeginOperationsSignInRequest { Email = "nobody@northwind.example", Password = Password }, CancellationToken.None);

        Assert.Equal("ops.auth.sign_in_failed", wrongPassword.Error!.Code);
        Assert.Equal("ops.auth.sign_in_failed", wrongEmail.Error!.Code);
    }

    [Fact]
    public async Task FiveWrongPasswords_LockEvenTheRightOne()
    {
        _kit.Owner(withApp: true);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await Begin("not the password at all");
        }

        Assert.Equal("ops.auth.sign_in_failed", (await Begin(Password)).Error!.Code);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.auth.sign_in_refused");

        _kit.Clock.Advance(TimeSpan.FromMinutes(15));
        Assert.True((await Begin(Password)).Succeeded);
    }

    // Owner, 2026-10-21: a device signing in from another country than its own is flagged and must confirm with a second factor.
    [Fact]
    public async Task Device_InAnotherCountry_IsFlagged_AndConfirmedWithTheCode()
    {
        _kit.Owner(withApp: true);
        Guid device = _kit.Devices.Register("secret", "US");
        _kit.Services.Country = "FR";

        BeginOperationsSignInResponse begun = await _kit.Begin().ExecuteAsync(_kit.Context,
            new BeginOperationsSignInRequest { Email = "grace@northwind.example", Password = Password, DeviceId = device, DeviceSecret = "secret" }, CancellationToken.None);
        _kit.Clock.Advance(TimeSpan.FromSeconds(31));
        CompleteOperationsSignInWithAuthenticatorCodeResponse verified = await Verify(begun.ChallengeToken, FakeCode);

        Assert.True(verified.Succeeded);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.auth.country_changed");
        OperationsUserSession session = Assert.Single(_kit.Store.Sessions);
        Assert.Equal(device, session.DeviceId);
        Assert.Equal("D-000001", session.DeviceNumber);
        Assert.Equal("FR", session.Country);
        Assert.True(session.CountryChanged);
    }

    // Staff who sign in with the password alone: at home, as usual; abroad, refused while they have no second factor to confirm with.
    [Fact]
    public async Task Device_InAnotherCountry_WithoutASecondFactor_IsRefused()
    {
        OperationsUser staff = OperationsUser.InviteStaff(_kit.TenantId, "Ada Lovelace", EmailAddress.Create("ada@northwind.example"), LanguageCode.En, _kit.Clock.GetUtcNow());
        staff.AcceptInvitation(_kit.Services.Hash(Password), _kit.Clock.GetUtcNow());
        _kit.Store.Users.Add(staff);
        Guid device = _kit.Devices.Register("secret", "US");
        BeginOperationsSignInRequest request = new() { Email = "ada@northwind.example", Password = Password, DeviceId = device, DeviceSecret = "secret" };

        _kit.Services.Country = "FR";
        BeginOperationsSignInResponse abroad = await _kit.Begin().ExecuteAsync(_kit.Context, request, CancellationToken.None);
        _kit.Services.Country = "US";
        BeginOperationsSignInResponse home = await _kit.Begin().ExecuteAsync(_kit.Context, request, CancellationToken.None);

        Assert.Equal("ops.auth.country_needs_second_factor", abroad.Error!.Code);
        Assert.NotEmpty(home.SessionToken);
        OperationsUserSession session = Assert.Single(_kit.Store.Sessions);
        Assert.Equal("Front desk", session.DeviceName);
        Assert.False(session.CountryChanged);
    }

    [Fact]
    public async Task RightCode_StartsASession_Once()
    {
        _kit.Owner(withApp: true);
        string challenge = await _kit.PasswordAsync();
        _kit.Clock.Advance(TimeSpan.FromSeconds(31));

        CompleteOperationsSignInWithAuthenticatorCodeResponse first = await Verify(challenge, FakeCode);
        CompleteOperationsSignInWithAuthenticatorCodeResponse again = await Verify(challenge, FakeCode);

        Assert.True(first.Succeeded);
        OperationsUserSession session = Assert.Single(_kit.Store.Sessions);
        Assert.Equal(_kit.TenantId, session.TenantId);
        Assert.False(session.IsSetupSession);
        Assert.Equal("ops.auth.sign_in_expired", again.Error!.Code);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.auth.signed_in");
    }

    [Fact]
    public async Task FiveWrongCodes_EndThePendingSignIn()
    {
        _kit.Owner(withApp: true);
        string challenge = await _kit.PasswordAsync();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal("ops.auth.code_invalid", (await Verify(challenge, "000000")).Error!.Code);
        }

        _kit.Clock.Advance(TimeSpan.FromSeconds(31));
        Assert.Equal("ops.auth.sign_in_expired", (await Verify(challenge, FakeCode)).Error!.Code);
    }

    [Fact]
    public async Task PendingSignIn_LastsFiveMinutes()
    {
        _kit.Owner(withApp: true);
        string challenge = await _kit.PasswordAsync();
        _kit.Clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal("ops.auth.sign_in_expired", (await Verify(challenge, FakeCode)).Error!.Code);
    }

    [Fact]
    public async Task Enrolment_ShowsTheTenantName_ThenGivesTenRecoveryCodes()
    {
        _kit.Store.Brands.Add(OperationsTenantBrand.Create(_kit.TenantId, "Northwind Freight", _kit.Clock.GetUtcNow()));
        OperationsUser owner = _kit.Owner(withApp: false);
        string challenge = await _kit.PasswordAsync();

        BeginOperationsSignInEnrolmentResponse begun = await _kit.BeginEnrolment().ExecuteAsync(_kit.Context, new BeginOperationsSignInEnrolmentRequest { ChallengeToken = challenge }, CancellationToken.None);
        CompleteOperationsSignInEnrolmentResponse done = await _kit.CompleteEnrolment().ExecuteAsync(_kit.Context, new CompleteOperationsSignInEnrolmentRequest { ChallengeToken = challenge, Code = FakeIdentityServices.GoodCode }, CancellationToken.None);

        Assert.True(begun.Succeeded);
        Assert.Equal("Northwind Freight", Assert.Single(_kit.Services.Issuers));
        Assert.DoesNotContain("Vixly", begun.OtpAuthUri, StringComparison.OrdinalIgnoreCase);
        Assert.True(done.Succeeded);
        Assert.Equal(10, done.RecoveryCodes.Count);
        Assert.True(owner.HasSecondFactor);
        Assert.Single(_kit.Store.Sessions);
    }

    [Fact]
    public async Task RecoveryCode_SignsInOnce()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        owner.ReplaceRecoveryCodes(Enumerable.Range(0, 10).Select(index => _kit.Services.HashRecoveryCode("ABCDE" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "XXXX")).ToArray(), _kit.Clock.GetUtcNow());

        CompleteOperationsSignInWithRecoveryCodeResponse first = await Recovery(await _kit.PasswordAsync(), "abcde-3xxxx");
        CompleteOperationsSignInWithRecoveryCodeResponse again = await Recovery(await _kit.PasswordAsync(), "ABCDE3XXXX");

        Assert.True(first.Succeeded);
        Assert.Equal(9, first.RecoveryCodesLeft);
        Assert.Equal("ops.auth.code_invalid", again.Error!.Code);
    }

    [Fact]
    public async Task Invitation_RefusesAWeakPassword_ThenLeadsTheOwnerToEnrolment()
    {
        _kit.PlatformKnowsTheOwner();
        await _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None);
        string token = LinkToken();

        AcceptOperationsInvitationResponse weak = await Accept(token, "short");
        AcceptOperationsInvitationResponse accepted = await Accept(token, Password);
        AcceptOperationsInvitationResponse twice = await Accept(token, Password);

        Assert.Equal("ops.auth.password_too_short", weak.Error!.Code);
        Assert.True(accepted.EnrolmentRequired);
        Assert.Empty(accepted.SessionToken);
        Assert.Equal("ops.auth.link_invalid", twice.Error!.Code);
        Assert.Empty(_kit.Store.Sessions);
    }

    // Review item 3: the Owner, named by the platform, is not refused when the email is also a customer's (staff members are, see the web tests).
    [Fact]
    public async Task Invitation_OfTheOwner_IsAcceptedEvenWithACustomerEmail()
    {
        OperationsCustomer customer = OperationsCustomer.SignUp(Guid.NewGuid(), "Grace", "Hopper", null, EmailAddress.Create("grace@northwind.example"), "+50937123456", LanguageCode.En,
            "$2a$12$hash", _kit.Clock.GetUtcNow());
        customer.ConfirmEmail("C00001", _kit.Clock.GetUtcNow());
        _kit.Customers.Add(customer);
        _kit.PlatformKnowsTheOwner();
        await _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None);

        AcceptOperationsInvitationResponse accepted = await Accept(LinkToken(), Password);

        Assert.True(accepted.Succeeded);
    }

    [Fact]
    public async Task SetupSessions_AreMarked_WhileTheTenantIsBeingSetUp()
    {
        IdentityTestKit setup = new(settingUp: true);
        setup.Owner(withApp: true);
        string challenge = await setup.PasswordAsync();
        setup.Clock.Advance(TimeSpan.FromSeconds(31));

        await setup.Verify().ExecuteAsync(setup.Context, new CompleteOperationsSignInWithAuthenticatorCodeRequest { ChallengeToken = challenge, Code = FakeIdentityServices.GoodCode }, CancellationToken.None);

        Assert.True(Assert.Single(setup.Store.Sessions).IsSetupSession);
    }

    [Fact]
    public async Task Session_EndsAfterFifteenIdleMinutes()
    {
        string token = await SignedInAsync();
        OperationsSessionValidator validator = _kit.Validator();

        _kit.Clock.Advance(TimeSpan.FromMinutes(14));
        Assert.NotNull(await validator.ValidateAsync(token, CancellationToken.None));

        _kit.Clock.Advance(TimeSpan.FromMinutes(15));
        Assert.Null(await validator.ValidateAsync(token, CancellationToken.None));
        Assert.Equal(OperationsSessionEndReason.IdleTimeout, _kit.Store.Sessions.Single().EndReason);
    }

    [Fact]
    public async Task SignOut_EndsTheSession()
    {
        string token = await SignedInAsync();
        OperationsUserSession session = _kit.Store.Sessions.Single();

        await _kit.SignOut().ExecuteAsync(_kit.Context.WithUser(session.UserId, session.Id), new SignOutOperationsRequest(), CancellationToken.None);

        Assert.Null(await _kit.Validator().ValidateAsync(token, CancellationToken.None));
        Assert.Equal(OperationsSessionEndReason.SignedOut, session.EndReason);
    }

    [Fact]
    public async Task UnknownSessionToken_IsNobody()
    {
        Assert.Null(await _kit.Validator().ValidateAsync("not-a-session", CancellationToken.None));
    }

    private string FakeCode => FakeIdentityServices.GoodCode;

    private Task<BeginOperationsSignInResponse> Begin(string password) =>
        _kit.Begin().ExecuteAsync(_kit.Context, new BeginOperationsSignInRequest { Email = "GRACE@northwind.example", Password = password }, CancellationToken.None);

    private Task<CompleteOperationsSignInWithAuthenticatorCodeResponse> Verify(string challenge, string code) =>
        _kit.Verify().ExecuteAsync(_kit.Context, new CompleteOperationsSignInWithAuthenticatorCodeRequest { ChallengeToken = challenge, Code = code }, CancellationToken.None);

    private Task<CompleteOperationsSignInWithRecoveryCodeResponse> Recovery(string challenge, string code) =>
        _kit.Recovery().ExecuteAsync(_kit.Context, new CompleteOperationsSignInWithRecoveryCodeRequest { ChallengeToken = challenge, Code = code }, CancellationToken.None);

    private Task<AcceptOperationsInvitationResponse> Accept(string token, string password) =>
        _kit.Accept().ExecuteAsync(_kit.Context, new AcceptOperationsInvitationRequest { Token = token, Password = password }, CancellationToken.None);

    private string LinkToken() => Uri.UnescapeDataString(_kit.Services.Mails.Single().Link.Split("token=")[1]);

    private async Task<string> SignedInAsync()
    {
        _kit.Owner(withApp: true);
        string challenge = await _kit.PasswordAsync();
        _kit.Clock.Advance(TimeSpan.FromSeconds(31));
        return (await Verify(challenge, FakeCode)).SessionToken;
    }
}
