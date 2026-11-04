using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsCustomerScreenDomainTests.cs
//
// PURPOSE:
// Proves the customer screen rules (Slice 5F step 3, P-2, P-3, P-9).
//
// RESPONSIBILITIES:
// - Paired once with a code that expires; a new code stops the screen paired before
// - The customer's steps only from the step that offers them; Zelle terms before the Zelle details
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsCustomerScreenDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Clerk = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Screen_IsPairedOnce_WithACodeThatExpires_AndANewCodeStopsTheOldScreen()
    {
        Assert.Equal(OperationsScreenErrors.NameInvalid, Assert.Throws<DomainException>(() => OperationsCustomerScreen.Add(Tenant, Guid.NewGuid(), " ", "code", Clerk, Now)).Error);
        OperationsCustomerScreen screen = OperationsCustomerScreen.Add(Tenant, Guid.NewGuid(), " Front tablet ", "code-hash", Clerk, Now);
        Assert.Equal(("Front tablet", false), (screen.Name, screen.Paired));

        Assert.Equal(OperationsScreenErrors.NotPaired, Assert.Throws<DomainException>(() => screen.Pair("secret", Now + OperationsCustomerScreen.PairingLifetime)).Error);
        screen.Pair("secret-hash", Now.AddMinutes(5));
        Assert.Equal((true, (string?)null), (screen.Paired, screen.PairingCodeHash));
        Assert.Equal(OperationsScreenErrors.NotPaired, Assert.Throws<DomainException>(() => screen.Pair("other", Now.AddMinutes(6))).Error);

        screen.ShowInvoice(Guid.NewGuid(), Clerk, Now);
        screen.NewCode("new-code-hash", Clerk, Now.AddMinutes(7));
        Assert.Equal((false, OperationsScreenStep.Idle, (Guid?)null), (screen.Paired, screen.Step, screen.InvoiceId));
    }

    [Fact]
    public void Customer_AcceptsTheZelleTerms_BeforeTheDetails_AndActsOnlyFromTheStepShown()
    {
        OperationsCustomerScreen screen = OperationsCustomerScreen.Add(Tenant, Guid.NewGuid(), "Front tablet", "code-hash", Clerk, Now);
        screen.Pair("secret-hash", Now);
        Assert.Equal(OperationsScreenErrors.StepInvalid, Assert.Throws<DomainException>(() => screen.ChooseZelle(Now)).Error);

        Guid invoice = Guid.NewGuid();
        screen.ShowInvoice(invoice, Clerk, Now);
        Assert.Equal((OperationsScreenStep.Summary, (Guid?)Clerk), (screen.Step, screen.SentByUserId));
        Assert.Equal(OperationsScreenErrors.StepInvalid, Assert.Throws<DomainException>(() => screen.AcceptZelle(Now)).Error);
        screen.ChooseZelle(Now);
        Assert.Equal(OperationsScreenStep.ZelleTerms, screen.Step);
        screen.AcceptZelle(Now);
        Assert.Equal((OperationsScreenStep.Zelle, (DateTimeOffset?)Now), (screen.Step, screen.ZelleAcceptedUtc));

        // Back and Zelle again: the terms already accepted for this invoice are not asked twice.
        screen.Back(Now);
        screen.ChooseZelle(Now);
        Assert.Equal(OperationsScreenStep.Zelle, screen.Step);

        // A signature only from the declaration; a new send forgets the earlier acceptance.
        Assert.Equal(OperationsScreenErrors.StepInvalid, Assert.Throws<DomainException>(() => screen.Signed(Now)).Error);
        screen.ShowSignature(Guid.NewGuid(), Clerk, Now);
        Assert.Null(screen.ZelleAcceptedUtc);
        screen.Signed(Now);
        Assert.Equal(OperationsScreenStep.Done, screen.Step);
        screen.Clear(Now);
        Assert.Equal((OperationsScreenStep.Idle, (Guid?)null, (Guid?)null), (screen.Step, screen.ShipmentId, screen.SentByUserId));
    }

    [Fact]
    public void Screen_TidiesItself_PaidThenThankYou_AndNothingLeftForTheNextCustomer()
    {
        OperationsCustomerScreen screen = OperationsCustomerScreen.Add(Tenant, Guid.NewGuid(), "Front tablet", "code-hash", Clerk, Now);
        screen.Pair("secret-hash", Now);
        screen.ShowInvoice(Guid.NewGuid(), Clerk, Now);
        Assert.False(screen.Tidy(false, Now.AddMinutes(1)));
        Assert.True(screen.Tidy(true, Now.AddMinutes(2)));
        Assert.Equal(OperationsScreenStep.Done, screen.Step);
        Assert.True(screen.Tidy(true, Now.AddMinutes(4)));
        Assert.Equal((OperationsScreenStep.Idle, (Guid?)null), (screen.Step, screen.InvoiceId));

        screen.ShowSignature(Guid.NewGuid(), Clerk, Now.AddMinutes(5));
        Assert.False(screen.Tidy(false, Now.AddMinutes(20)));
        Assert.True(screen.Tidy(false, Now.AddMinutes(26)));
        Assert.Equal((OperationsScreenStep.Idle, (Guid?)null), (screen.Step, screen.ShipmentId));
    }
}
