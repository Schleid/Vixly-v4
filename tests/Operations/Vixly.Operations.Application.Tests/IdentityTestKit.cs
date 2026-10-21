using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Setup;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.SignIn.AcceptOperationsInvitation;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithRecoveryCode;
using Vixly.Operations.Application.UseCases.SignIn.SignOutOperations;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Messaging;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: IdentityTestKit.cs
//
// PURPOSE:
// Wires the Slice 1A use cases over in-memory fakes for one tenant.
//
// RESPONSIBILITIES:
// - Build each use case
// - Invite an owner through the real handler
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class IdentityTestKit
{
    public IdentityTestKit(bool settingUp = false)
    {
        Scope.Enter(TenantId);
        Context = OperationsAuthorizationContext.ForTenant(TenantId, settingUp);
        Audit = new OperationsAuditTrail(Store, Scope, Services, Clock);
        Steps = new OperationsSignInSteps(Store, Store, Store, Services, Services, Audit, Store, Clock, Store);
    }

    public Guid TenantId { get; } = Guid.NewGuid();

    public FixedTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));

    public FakeTenantScope Scope { get; } = new();

    public FakeIdentityStore Store { get; } = new();

    public FakeIdentityServices Services { get; } = new();

    public FakePlatformClient Platform { get; } = new();

    public OperationsAuthorizationContext Context { get; }

    public OperationsAuditTrail Audit { get; }

    public OperationsSignInSteps Steps { get; }

    public BeginOperationsSignInUseCase Begin() => new(Store, Services, Steps, Audit, Store);

    public CompleteOperationsSignInWithAuthenticatorCodeUseCase Verify() => new(Steps, Services);

    public CompleteOperationsSignInWithRecoveryCodeUseCase Recovery() => new(Steps, Services);

    public BeginOperationsSignInEnrolmentUseCase BeginEnrolment() => new(Steps, Services, Store, Store);

    public CompleteOperationsSignInEnrolmentUseCase CompleteEnrolment() => new(Steps, Services, Services);

    public AcceptOperationsInvitationUseCase Accept() => new(Store, Store, Services, Services, Steps, Audit, Store);

    public SignOutOperationsUseCase SignOut() => new(Store, Audit, Store, Clock);

    public OperationsSessionValidator Validator() => new(Store, Store, Services, Store, Clock);

    public OperationsOwnerInvitationHandler InvitationHandler() => new(Platform, Store, Store, Store, Services, Services, Services, Audit, Store, Clock);

    public OperationsOutboxMessage InvitationMessage() =>
        OperationsOutboxMessage.Create(OperationsSetupMessages.OwnerInvitationRequested, TenantId, TenantId, "{}", Clock.GetUtcNow());

    public void PlatformKnowsTheOwner(string? ownerEmail = "grace@northwind.example")
    {
        Platform.Accounts[TenantId] = new OperationsTenantAccount(TenantId, "northwind", "Northwind Freight LLC", "Northwind Freight", "fr", "Grace Hopper", ownerEmail);
    }

    // An owner who chose a password; with an authenticator app when asked.
    public OperationsUser Owner(bool withApp, string password = "a long enough password")
    {
        OperationsUser owner = OperationsUser.InviteOwner(TenantId, "Grace Hopper", EmailAddress.Create("grace@northwind.example"), LanguageCode.Fr, Clock.GetUtcNow());
        owner.AcceptInvitation(Services.Hash(password), Clock.GetUtcNow());

        if (withApp)
        {
            owner.BeginAuthenticatorEnrolment("enc-app", Clock.GetUtcNow());
            owner.ConfirmAuthenticatorApp(1, Clock.GetUtcNow());
        }

        Store.Users.Add(owner);
        return owner;
    }

    public async Task<string> PasswordAsync(string password = "a long enough password")
    {
        BeginOperationsSignInResponse response = await Begin().ExecuteAsync(Context, new BeginOperationsSignInRequest { Email = "grace@northwind.example", Password = password }, CancellationToken.None);
        return response.Succeeded ? response.ChallengeToken : throw new InvalidOperationException(response.Error!.Code);
    }
}
