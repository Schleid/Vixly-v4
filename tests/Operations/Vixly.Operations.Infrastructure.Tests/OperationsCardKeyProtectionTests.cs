using Microsoft.AspNetCore.DataProtection;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Infrastructure.Security;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsCardKeyProtectionTests.cs
//
// PURPOSE:
// Proves a company's card keys are kept under their own purpose, bound to the company (2026-11-03).
//
// RESPONSIBILITIES:
// - The company's own row reads its keys back
// - The same protected value copied to another company's row cannot be read
// - Keys saved before (the shared purpose) still read
//
// IMPORTANT NOTES:
// - Ephemeral Data Protection keys: nothing is stored
// =========================================================
public sealed class OperationsCardKeyProtectionTests
{
    private const string Secret = "sk_test_51abcdefghijklmnopqrstuvwxyz";

    private const string Publishable = "pk_test_51abcdefghijklmnopqrstuvwxyz";

    private const string Webhook = "whsec_abcdefghijklmnopqrstuvwxyz";

    private static readonly DateTimeOffset Now = new(2026, 11, 3, 0, 0, 0, TimeSpan.Zero);

    private readonly OperationsSecretProtector _protector = new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Keys_ReadBackOnlyForTheirOwnCompany()
    {
        Guid galeo = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        OperationsCardProvider own = OperationsCardProvider.Connect(galeo, Secret, OperationsCardDesk.ProtectSecretKey(_protector, galeo, Secret), Publishable,
            OperationsCardDesk.ProtectWebhookSecret(_protector, galeo, Webhook), "Galeo", Guid.NewGuid(), Now);
        OperationsCardProvider copied = OperationsCardProvider.Connect(other, Secret, own.SecretKeyProtected, Publishable, own.WebhookSecretProtected, "Other", Guid.NewGuid(), Now);

        Assert.Equal(Secret, OperationsCardDesk.ReadSecretKey(_protector, own));
        Assert.Equal(Webhook, OperationsCardDesk.ReadWebhookSecret(_protector, own));
        Assert.Null(OperationsCardDesk.ReadSecretKey(_protector, copied));
        Assert.Null(OperationsCardDesk.ReadWebhookSecret(_protector, copied));
        Assert.Null(_protector.Unprotect(own.SecretKeyProtected));
    }

    [Fact]
    public void KeysSavedTheOldWay_StillRead()
    {
        Guid galeo = Guid.NewGuid();
        OperationsCardProvider legacy = OperationsCardProvider.Connect(galeo, Secret, _protector.Protect(Secret), Publishable, _protector.Protect(Webhook), "Galeo", Guid.NewGuid(), Now);

        Assert.Equal(Secret, OperationsCardDesk.ReadSecretKey(_protector, legacy));
        Assert.Equal(Webhook, OperationsCardDesk.ReadWebhookSecret(_protector, legacy));
    }
}
