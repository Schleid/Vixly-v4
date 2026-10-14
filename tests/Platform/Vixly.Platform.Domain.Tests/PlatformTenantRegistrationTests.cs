using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;
using static Vixly.Platform.Domain.Tests.TenantTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformTenantRegistrationTests.cs
//
// PURPOSE:
// Proves self-service registration (D-T4, D-T9, D-T18), its
// verification links and the outbox message rules (D-T17).
//
// RESPONSIBILITIES:
// - Registration needs accepted terms and records consent
// - Verification emails are throttled; links work once
// - Deadlines expire the registration
// - A verified registration becomes a Draft tenant with its owner
//   contact and consent history
// - Outbox messages back off and are set aside
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformTenantRegistrationTests
{
    [Fact]
    public void Start_NeedsAcceptedTerms_AndRecordsConsent()
    {
        AssertError(PlatformRegistrationErrors.TermsNotAccepted, () => Registration(termsAccepted: false));

        PlatformTenantRegistration registration = Registration();

        Assert.Equal(PlatformTenantRegistrationStatus.PendingEmailVerification, registration.Status);
        Assert.Equal("northwind", registration.Slug);
        Assert.Equal("2026-10", registration.TermsVersion);
        Assert.Equal("203.0.113.7", registration.ConsentIpAddress);
        Assert.Equal(Now.AddDays(7), registration.AccessExpiresUtc);
    }

    [Fact]
    public void VerificationEmails_AreLimitedPerHour()
    {
        PlatformTenantRegistration registration = Registration();

        registration.RecordVerificationSent(Now);
        registration.RecordVerificationSent(Now.AddMinutes(5));
        registration.RecordVerificationSent(Now.AddMinutes(10));

        AssertError(PlatformRegistrationErrors.ResendTooSoon, () => registration.RecordVerificationSent(Now.AddMinutes(20)));

        registration.RecordVerificationSent(Now.AddMinutes(61));
        Assert.Equal(Now.AddMinutes(61).AddHours(24), registration.VerificationDeadlineUtc);
    }

    [Fact]
    public void UnverifiedRegistration_ExpiresAfterItsDeadline()
    {
        PlatformTenantRegistration registration = Registration();
        registration.RecordVerificationSent(Now);

        Assert.False(registration.ExpireIfDue(Now.AddHours(23)));
        Assert.True(registration.ExpireIfDue(Now.AddHours(24)));
        Assert.Equal(PlatformTenantRegistrationStatus.Expired, registration.Status);
        AssertError(PlatformRegistrationErrors.Expired, () => registration.MarkEmailVerified(Guid.NewGuid(), Now.AddHours(25)));
    }

    [Fact]
    public void Verification_MovesToPlanSelection_AndExtendsAccess()
    {
        PlatformTenantRegistration registration = Registration();
        Guid tenantId = Guid.NewGuid();

        registration.MarkEmailVerified(tenantId, Now.AddHours(1));

        Assert.Equal(PlatformTenantRegistrationStatus.PendingPlanSelection, registration.Status);
        Assert.Equal(tenantId, registration.PlatformTenantId);
        Assert.Equal(Now.AddHours(1).AddDays(30), registration.AccessExpiresUtc);
        AssertError(PlatformRegistrationErrors.AlreadyVerified, () => registration.MarkEmailVerified(tenantId, Now.AddHours(2)));
        Assert.True(registration.ExpireIfDue(Now.AddHours(1).AddDays(30)));
    }

    [Fact]
    public void OwnerEmail_CanBeCorrected_OnlyBeforeVerification()
    {
        PlatformTenantRegistration registration = Registration();

        registration.CorrectOwnerEmail(EmailAddress.Create("grace.hopper@northwind.example"), Now);

        Assert.Equal("grace.hopper@northwind.example", registration.OwnerEmail.Value);
        AssertError(
            PlatformRegistrationErrors.EmailUnchanged,
            () => registration.CorrectOwnerEmail(EmailAddress.Create("grace.hopper@northwind.example"), Now));

        registration.MarkEmailVerified(Guid.NewGuid(), Now);
        AssertError(
            PlatformRegistrationErrors.AlreadyVerified,
            () => registration.CorrectOwnerEmail(EmailAddress.Create("x@northwind.example"), Now));
    }

    [Fact]
    public void Cancel_ClosesTheRegistration_WithAReason()
    {
        PlatformTenantRegistration registration = Registration();

        AssertError(PlatformTenantErrors.ReasonRequired, () => registration.Cancel(Actor, " ", Now));

        registration.Cancel(Actor, "Duplicate sign-up", Now);

        Assert.True(registration.IsClosed);
        Assert.False(registration.AcceptsAccess(Now));
        AssertError(PlatformRegistrationErrors.Cancelled, () => registration.Cancel(Actor, "again", Now));
    }

    [Fact]
    public void VerifiedRegistration_BecomesADraftTenant_WithOwnerAndConsent()
    {
        PlatformTenantRegistration registration = Registration();

        PlatformTenantCreation creation = PlatformTenant.CreateFromRegistration("TEN-7K3M9Q", registration, Now.AddHours(1));

        Assert.Equal(PlatformTenantStatus.Draft, creation.Tenant.Status);
        Assert.Equal(PlatformTenantOrigin.SelfRegistration, creation.Tenant.Origin);
        Assert.Equal(LanguageCode.Es, creation.Tenant.DefaultLanguage);
        Assert.Equal("grace@northwind.example", creation.Tenant.ContactFor(PlatformTenantContactRole.Owner).Email.Value);
        Assert.Equal(
            new[] { PlatformTenantEventCodes.CreatedFromRegistration, PlatformTenantEventCodes.TermsAccepted },
            creation.Events.Select(item => item.EventCode));
        Assert.Equal("2026-09", creation.Events[1].Arguments["privacyVersion"]);
    }

    [Fact]
    public void VerificationLink_WorksOnce_AndNotAfterExpiry()
    {
        Guid registration = Guid.NewGuid();
        PlatformRegistrationToken token = PlatformRegistrationToken.Issue(registration, Hash, Now);

        token.Consume(Now.AddHours(1));

        AssertError(PlatformRegistrationErrors.LinkInvalid, () => token.Consume(Now.AddHours(2)));

        PlatformRegistrationToken late = PlatformRegistrationToken.Issue(registration, Hash, Now);
        AssertError(PlatformRegistrationErrors.LinkInvalid, () => late.Consume(Now.AddHours(24)));

        PlatformRegistrationToken revoked = PlatformRegistrationToken.Issue(registration, Hash, Now);
        revoked.Revoke(Now);
        AssertError(PlatformRegistrationErrors.LinkInvalid, () => revoked.Consume(Now));
    }

    [Theory]
    [InlineData("platform.tenant.status_changed")]
    [InlineData("Platform.Tenant.v1")]
    [InlineData("")]
    public void OutboxType_MustBeVersioned(string type)
    {
        AssertError(PlatformMessagingErrors.TypeInvalid, () => PlatformOutboxMessage.Create(type, Guid.NewGuid(), "{}", Now));
    }

    [Fact]
    public void FailedOutboxMessage_BacksOff_ThenIsSetAside()
    {
        PlatformOutboxMessage message = PlatformOutboxMessage.Create("platform.tenant.status_changed.v1", Guid.NewGuid(), "{}", Now);

        message.MarkFailed("platform.mail.unavailable", Now);
        Assert.Equal(Now.AddSeconds(30), message.NextAttemptUtc);

        for (int attempt = 2; attempt <= PlatformOutboxMessage.MaxAttempts; attempt++)
        {
            message.MarkFailed("platform.mail.unavailable", Now);
        }

        Assert.True(message.IsSetAside);
        Assert.Equal(TimeSpan.FromHours(1), PlatformOutboxMessage.BackOff(9));

        message.Retry(Now);
        message.MarkProcessed(Now);

        Assert.False(message.IsSetAside);
        Assert.Equal(Now, message.ProcessedUtc);
    }
}
