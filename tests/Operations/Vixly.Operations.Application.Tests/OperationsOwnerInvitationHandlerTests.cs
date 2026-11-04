using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsOwnerInvitationHandlerTests.cs
//
// PURPOSE:
// Proves the owner invitation of a new tenant (Slice 1, D-O3).
//
// RESPONSIBILITIES:
// - Owner, brand and link from Platform data
// - Once only
// - A missing owner contact or Platform down retries
//
// IMPORTANT NOTES:
// - In-memory fakes
// =========================================================
public sealed class OperationsOwnerInvitationHandlerTests
{
    private readonly IdentityTestKit _kit = new(settingUp: true);

    [Fact]
    public async Task InvitesTheOwner_InTheTenantLanguage_ToTheTenantHost()
    {
        _kit.PlatformKnowsTheOwner();

        await _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None);

        OperationsUser owner = Assert.Single(_kit.Store.Users);
        Assert.True(owner.IsOwner);
        Assert.Equal("Grace Hopper", owner.FullName);
        Assert.Equal("fr", owner.PreferredLanguage.Value);
        Assert.Equal("Northwind Freight", Assert.Single(_kit.Store.Brands).DisplayName);
        OperationsAccountToken token = Assert.Single(_kit.Store.Tokens);
        Assert.Equal(OperationsAccountTokenPurpose.Invitation, token.Purpose);
        Assert.Equal(_kit.Clock.GetUtcNow().AddHours(72), token.ExpiresUtc);
        var mail = Assert.Single(_kit.Services.Mails);
        Assert.StartsWith("https://northwind.techcrafters.test/invitation?token=", mail.Link, StringComparison.Ordinal);
        Assert.Equal("Northwind Freight", mail.CompanyName);
        Assert.Equal(72, mail.Hours);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.setup.owner_invited");
    }

    [Fact]
    public async Task ReplayedEvent_InvitesNobodyTwice()
    {
        _kit.PlatformKnowsTheOwner();

        await _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None);
        await _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None);

        Assert.Single(_kit.Store.Users);
        Assert.Single(_kit.Services.Mails);
    }

    [Fact]
    public async Task NoOwnerContact_FailsWithACode()
    {
        _kit.PlatformKnowsTheOwner(ownerEmail: null);

        DomainException error = await Assert.ThrowsAsync<DomainException>(() => _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None));

        Assert.Equal("ops.setup.owner_contact_missing", error.Error.Code);
        Assert.Empty(_kit.Store.Users);
    }

    [Fact]
    public async Task MailDown_SavesNothing_SoTheRetryInvitesAgain()
    {
        _kit.PlatformKnowsTheOwner();
        _kit.Services.MailFails = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None));
        _kit.Store.DiscardChanges();
        _kit.Services.MailFails = false;
        await _kit.InvitationHandler().HandleAsync(_kit.InvitationMessage(), CancellationToken.None);

        Assert.Single(_kit.Store.Users);
        Assert.Single(_kit.Services.Mails);
    }
}
