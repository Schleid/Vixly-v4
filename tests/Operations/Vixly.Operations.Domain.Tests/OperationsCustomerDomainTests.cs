using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsCustomerDomainTests.cs
//
// PURPOSE:
// Proves the customer rules of Slice 2 (design sections 2 and 3).
//
// RESPONSIBILITIES:
// - Sign-up waits for the email; confirmation gives the code once
// - Lockout after 5 failures for 15 minutes
// - Status moves with a reason; credit with a reason
// - Sessions end after 60 idle minutes and after 12 hours; links work once and expire
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsCustomerDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly string Hash = new('a', 64);

    [Fact]
    public void SignUp_WaitsForTheEmail_AndConfirmationGivesTheCodeOnce()
    {
        OperationsCustomer customer = SignUp();

        Assert.Equal(OperationsCustomerStatus.PendingVerification, customer.Status);
        Assert.Null(customer.Code);
        Assert.False(customer.CanSignIn(Now));

        customer.ConfirmEmail("C00042", Now);

        Assert.Equal(OperationsCustomerStatus.Active, customer.Status);
        Assert.Equal("C00042", customer.Code);
        Assert.True(customer.CanSignIn(Now));
        Assert.Equal(OperationsCustomerErrors.NotPending.Code, Assert.Throws<DomainException>(() => customer.ConfirmEmail("C00043", Now)).Error.Code);
        Assert.Equal("C00042", customer.Code);
    }

    [Theory]
    [InlineData(" ", "Pierre", "+50937123456", "ops.customers.name_required")]
    [InlineData("Ana", "<b>", "+50937123456", "ops.customers.name_invalid")]
    [InlineData("Visit https://evil.example", "Pierre", "+50937123456", "ops.customers.name_invalid")]
    [InlineData("Ana", "www.evil.example", "+50937123456", "ops.customers.name_invalid")]
    [InlineData("Ana", "Pierre", "37123456", "ops.customers.phone_invalid")]
    public void Details_AreChecked_BeforeACodeIsTaken(string first, string last, string phone, string code)
    {
        Assert.Equal(code, Assert.Throws<DomainException>(() => OperationsCustomer.CheckDetails(first, last, null, phone)).Error.Code);
        Assert.Equal(code, Assert.Throws<DomainException>(() =>
            OperationsCustomer.SignUp(Tenant, first, last, null, EmailAddress.Create("ana@example.com"), phone, LanguageCode.En, "$2a$12$hash", Now)).Error.Code);
    }

    [Fact]
    public void Code_IsLettersAndDigitsOnly()
    {
        Assert.Equal(OperationsCustomerErrors.CodeInvalid.Code, Assert.Throws<DomainException>(() => SignUp().ConfirmEmail("C-1", Now)).Error.Code);
    }

    [Fact]
    public void FiveFailures_LockTheAccount_For15Minutes()
    {
        OperationsCustomer customer = Active();

        for (int attempt = 0; attempt < 4; attempt++)
        {
            customer.RecordFailedSignIn(Now);
        }

        Assert.False(customer.IsLockedOut(Now));
        customer.RecordFailedSignIn(Now);
        Assert.True(customer.IsLockedOut(Now.AddMinutes(14)));
        Assert.False(customer.CanSignIn(Now.AddMinutes(14)));
        Assert.False(customer.IsLockedOut(Now.AddMinutes(15)));
        Assert.True(customer.CanSignIn(Now.AddMinutes(15)));
    }

    [Fact]
    public void Status_MovesOnlyAsAllowed_AndNeedsAReason()
    {
        OperationsCustomer customer = Active();

        Assert.Equal(OperationsCustomerErrors.ReasonRequired.Code, Assert.Throws<DomainException>(() => customer.ChangeStatus(OperationsCustomerStatus.Suspended, " ", Now)).Error.Code);
        Assert.Equal(OperationsCustomerErrors.StatusChangeInvalid.Code, Assert.Throws<DomainException>(() => customer.ChangeStatus(OperationsCustomerStatus.Active, "Again", Now)).Error.Code);

        customer.ChangeStatus(OperationsCustomerStatus.Suspended, "Unpaid", Now);
        Assert.False(customer.CanSignIn(Now));
        customer.ChangeStatus(OperationsCustomerStatus.Active, "Paid", Now);
        customer.ChangeStatus(OperationsCustomerStatus.Closed, "Moved away", Now);

        Assert.Equal("Moved away", customer.StatusReason);
        Assert.Equal(OperationsCustomerErrors.StatusChangeInvalid.Code, Assert.Throws<DomainException>(() => customer.ChangeStatus(OperationsCustomerStatus.Active, "Back", Now)).Error.Code);
        Assert.Equal(OperationsCustomerErrors.StatusChangeInvalid.Code, Assert.Throws<DomainException>(() => SignUp().ChangeStatus(OperationsCustomerStatus.Suspended, "Spam", Now)).Error.Code);
    }

    [Fact]
    public void Credit_NeedsAReason()
    {
        OperationsCustomer customer = Active();

        Assert.Equal(OperationsCustomerErrors.ReasonRequired.Code, Assert.Throws<DomainException>(() => customer.SetCredit(true, "", Now)).Error.Code);
        Assert.False(customer.CreditAllowed);
        customer.SetCredit(true, "Good customer", Now);
        Assert.True(customer.CreditAllowed);
    }

    [Fact]
    public void Session_EndsAfter60IdleMinutes_AndAfter12Hours()
    {
        OperationsCustomerSession session = OperationsCustomerSession.Start(Tenant, Guid.NewGuid(), Hash, "203.0.113.5", "Browser", Now);

        Assert.True(session.IsValidAt(Now.AddMinutes(59)));
        Assert.Equal(OperationsSessionEndReason.IdleTimeout, session.ExpiryReasonAt(Now.AddMinutes(60)));

        DateTimeOffset seen = Now;

        for (int hour = 0; hour < 12; hour++)
        {
            seen = seen.AddMinutes(55);
            session.Touch(seen);
        }

        Assert.Equal(OperationsSessionEndReason.AbsoluteLifetime, session.ExpiryReasonAt(Now.AddHours(12)));
        session.End(OperationsSessionEndReason.SignedOut, Now.AddHours(1));
        Assert.False(session.IsValidAt(Now.AddHours(1)));
        Assert.Equal(OperationsCustomerErrors.SessionEnded.Code, Assert.Throws<DomainException>(() => session.Touch(Now.AddHours(2))).Error.Code);
    }

    [Theory]
    [InlineData(OperationsCustomerTokenPurpose.Verification, 24 * 60)]
    [InlineData(OperationsCustomerTokenPurpose.PasswordReset, 60)]
    public void Link_WorksOnce_ForItsPurpose_UntilItExpires(OperationsCustomerTokenPurpose purpose, int minutes)
    {
        OperationsCustomerTokenPurpose other = purpose == OperationsCustomerTokenPurpose.PasswordReset ? OperationsCustomerTokenPurpose.Verification : OperationsCustomerTokenPurpose.PasswordReset;
        OperationsCustomerToken expired = OperationsCustomerToken.Issue(Tenant, Guid.NewGuid(), purpose, Hash, Now);
        OperationsCustomerToken token = OperationsCustomerToken.Issue(Tenant, Guid.NewGuid(), purpose, Hash, Now);

        Assert.Throws<DomainException>(() => expired.Consume(purpose, Now.AddMinutes(minutes)));
        Assert.Throws<DomainException>(() => token.Consume(other, Now));
        token.Consume(purpose, Now.AddMinutes(minutes - 1));
        Assert.Equal(OperationsCustomerErrors.LinkInvalid.Code, Assert.Throws<DomainException>(() => token.Consume(purpose, Now.AddMinutes(minutes - 1))).Error.Code);
    }

    private static OperationsCustomer SignUp() =>
        OperationsCustomer.SignUp(Tenant, "Ana", "Pierre", null, EmailAddress.Create("ana@example.com"), "+50937123456", LanguageCode.En, "$2a$12$hash", Now);

    private static OperationsCustomer Active()
    {
        OperationsCustomer customer = SignUp();
        customer.ConfirmEmail("C00001", Now);
        return customer;
    }
}
