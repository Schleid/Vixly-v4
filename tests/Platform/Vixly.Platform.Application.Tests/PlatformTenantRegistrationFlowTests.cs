using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Application.UseCases.Registration.CheckPortalSlugAvailability;
using Vixly.Platform.Application.UseCases.Registration.CorrectRegistrationEmail;
using Vixly.Platform.Application.UseCases.Registration.GetMyRegistrationStatus;
using Vixly.Platform.Application.UseCases.Registration.ResendMyRegistrationVerification;
using Vixly.Platform.Application.UseCases.Registration.StartPlatformTenantRegistration;
using Vixly.Platform.Application.UseCases.Registration.VerifyRegistrationEmail;
using Vixly.Platform.Application.UseCases.Registrations.CancelPlatformTenantRegistration;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformTenantRegistrationFlowTests.cs
//
// PURPOSE:
// Proves self-service registration end to end in the application
// layer, and the outbox dispatcher with tenant notices.
//
// RESPONSIBILITIES:
// - Bot check, consent and slug rules guard the start
// - Verification creates the Draft tenant and rotates access
// - Links are throttled, single-use and revocable
// - Cancelling frees the slug; deadlines expire registrations
// - Notices reach the owner and billing contacts, once
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformTenantRegistrationFlowTests
{
    private readonly TenantTestKit _kit = new();

    private readonly FakeSalesStore _offers = new();

    [Fact]
    public async Task Start_ChecksTheBotTokenBeforeAnythingElse()
    {
        StartPlatformTenantRegistrationResponse response = await StartAsync(botToken: "robot");

        Assert.Equal(PlatformRegistrationFlowErrors.BotCheckFailed, response.Error);
        Assert.Empty(_kit.Tenants.Registrations);
        Assert.Empty(_kit.Tenants.Mail);
    }

    [Fact]
    public async Task Start_NeedsTheCurrentTerms()
    {
        Assert.Equal(PlatformRegistrationErrors.TermsNotAccepted, (await StartAsync(accepted: false)).Error);
        Assert.Equal(PlatformRegistrationErrors.LegalVersionInvalid, (await StartAsync(terms: "2025-01")).Error);
    }

    [Fact]
    public async Task Start_ClaimsTheSlug_StoresOnlyHashes_AndSendsALink()
    {
        StartPlatformTenantRegistrationResponse response = await StartAsync();

        PlatformTenantRegistration registration = Assert.Single(_kit.Tenants.Registrations);
        PlatformPortalSlug claim = Assert.Single(_kit.Tenants.Slugs);
        (string kind, string to, string link) = Assert.Single(_kit.Tenants.Mail);
        Assert.True(response.Succeeded);
        Assert.Equal(_kit.Crypto.HashToken(response.AccessToken), registration.AccessTokenHash);
        Assert.NotEqual(response.AccessToken, registration.AccessTokenHash);
        Assert.Equal(("link", "grace@northwind.example"), (kind, to));
        Assert.Equal(_kit.Crypto.HashToken(link), Assert.Single(_kit.Tenants.Tokens).TokenHash);
        Assert.True(claim.IsHeldBy(registration.Id));
        Assert.Equal(TenantTestKit.Now.AddHours(24), claim.HeldUntilUtc);
        Assert.Empty(_kit.Tenants.Tenants);
        Assert.Equal("platform.registrations.started", Assert.Single(_kit.Store.AuditEntries).ActionCode);
        Assert.Equal("203.0.113.9", registration.ConsentIpAddress);
    }

    [Fact]
    public async Task Start_RefusesAHeldSlug_ButTakesOverALapsedOne()
    {
        await StartAsync();

        Assert.Equal(PlatformTenantErrors.SlugTaken, (await StartAsync(email: "other@example.com")).Error);

        _kit.Clock.Advance(TimeSpan.FromHours(25));
        StartPlatformTenantRegistrationResponse second = await StartAsync(email: "other@example.com");

        Assert.True(second.Succeeded);
        Assert.True(Assert.Single(_kit.Tenants.Slugs).IsHeldBy(_kit.Tenants.Registrations[1].Id));
    }

    [Fact]
    public async Task SlugCheck_AnswersAReasonOnly()
    {
        _kit.Tenant("northwind");
        CheckPortalSlugAvailabilityUseCase useCase = new(_kit.Slugs(), _kit.Clock);

        CheckPortalSlugAvailabilityResponse taken = await useCase.ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new CheckPortalSlugAvailabilityRequest { Slug = "NorthWind" },
            CancellationToken.None);
        CheckPortalSlugAvailabilityResponse free = await useCase.ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new CheckPortalSlugAvailabilityRequest { Slug = "Southwind" },
            CancellationToken.None);

        Assert.Equal(PlatformTenantErrors.SlugTaken, taken.Error);
        Assert.Equal("southwind", free.Slug);
    }

    [Fact]
    public async Task Verify_CreatesTheDraftTenant_AndRotatesAccess()
    {
        StartPlatformTenantRegistrationResponse started = await StartAsync();
        string link = _kit.Tenants.Mail[0].Value;

        VerifyRegistrationEmailResponse verified = await VerifyAsync(link);

        PlatformTenant tenant = Assert.Single(_kit.Tenants.Tenants);
        PlatformTenantRegistration registration = _kit.Tenants.Registrations[0];
        Assert.True(verified.Succeeded);
        Assert.Equal(tenant.TenantCode, verified.TenantCode);
        Assert.Equal(PlatformTenantStatus.Draft, tenant.Status);
        Assert.Equal(PlatformTenantOrigin.SelfRegistration, tenant.Origin);
        Assert.Equal("grace@northwind.example", tenant.ContactFor(PlatformTenantContactRole.Owner).Email.Value);
        Assert.Equal(PlatformTenantRegistrationStatus.PendingPlanSelection, registration.Status);
        Assert.Equal(PlatformPortalSlugState.Permanent, Assert.Single(_kit.Tenants.Slugs).State);
        Assert.Contains(_kit.Tenants.Events, item => item.EventCode == PlatformTenantEventCodes.TermsAccepted);
        Assert.Equal(PlatformRegistrationErrors.AccessHashInvalid, (await StatusAsync(started.AccessToken)).Error);
        Assert.Equal(PlatformTenantRegistrationStatus.PendingPlanSelection, (await StatusAsync(verified.AccessToken)).Status);
        Assert.Equal(PlatformRegistrationErrors.LinkInvalid, (await VerifyAsync(link)).Error);
    }

    [Fact]
    public async Task Verify_GivesOneErrorForUnknownLinks()
    {
        Assert.Equal(PlatformRegistrationErrors.LinkInvalid, (await VerifyAsync("never-issued")).Error);
        Assert.Equal(PlatformRegistrationErrors.LinkInvalid, (await VerifyAsync(" ")).Error);
    }

    [Fact]
    public async Task Resend_IsThrottled_AndRevokesTheOlderLink()
    {
        StartPlatformTenantRegistrationResponse started = await StartAsync();
        string first = _kit.Tenants.Mail[0].Value;
        ResendMyRegistrationVerificationUseCase resend = new(_kit.Links(), _kit.Tenants, _kit.Audit(), _kit.Store, _kit.Clock);
        ResendMyRegistrationVerificationRequest request = new() { AccessToken = started.AccessToken };

        Assert.True((await resend.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, request, CancellationToken.None)).Succeeded);
        Assert.True((await resend.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, request, CancellationToken.None)).Succeeded);
        Assert.Equal(
            PlatformRegistrationErrors.ResendTooSoon,
            (await resend.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, request, CancellationToken.None)).Error);

        Assert.Equal(PlatformRegistrationErrors.LinkInvalid, (await VerifyAsync(first)).Error);
        Assert.True((await VerifyAsync(_kit.Tenants.Mail[^1].Value)).Succeeded);
    }

    [Fact]
    public async Task CorrectEmail_SendsToTheNewAddress_AndMasksItInTheStatus()
    {
        StartPlatformTenantRegistrationResponse started = await StartAsync();

        CorrectRegistrationEmailResponse corrected = await new CorrectRegistrationEmailUseCase(_kit.Links(), _kit.Tenants, _kit.Audit(), _kit.Store, _kit.Clock)
            .ExecuteAsync(
                PlatformAnonymousAuthorizationContext.Instance,
                new CorrectRegistrationEmailRequest { AccessToken = started.AccessToken, Email = "grace.hopper@northwind.example" },
                CancellationToken.None);
        GetMyRegistrationStatusResponse status = await StatusAsync(started.AccessToken);

        Assert.True(corrected.Succeeded);
        Assert.Equal("grace.hopper@northwind.example", _kit.Tenants.Mail[^1].To);
        Assert.Equal("g***********@northwind.example", status.MaskedEmail);
        Assert.Equal("northwind", status.Slug);
    }

    [Fact]
    public async Task Status_ExpiresAnUnverifiedRegistration_AfterItsDeadline()
    {
        StartPlatformTenantRegistrationResponse started = await StartAsync();

        _kit.Clock.Advance(TimeSpan.FromHours(24));

        Assert.Equal(PlatformRegistrationErrors.Expired, (await StatusAsync(started.AccessToken)).Error);
        Assert.Equal(PlatformTenantRegistrationStatus.Expired, _kit.Tenants.Registrations[0].Status);
        Assert.Equal(PlatformRegistrationErrors.Expired, (await StatusAsync(started.AccessToken)).Error);
    }

    [Fact]
    public async Task Cancel_FreesThePendingSlug()
    {
        await StartAsync();
        PlatformAuthorizationContext staff = _kit.Staff(PlatformPermissions.TenantsEdit);

        CancelPlatformTenantRegistrationResponse cancelled = await new CancelPlatformTenantRegistrationUseCase(
            _kit.Gate(),
            _kit.Tenants,
            _kit.Links(),
            _kit.Audit(),
            _kit.Store,
            _kit.Clock).ExecuteAsync(
                staff,
                new CancelPlatformTenantRegistrationRequest { RegistrationId = _kit.Tenants.Registrations[0].Id, Reason = "Duplicate" },
                CancellationToken.None);

        Assert.True(cancelled.Succeeded);
        Assert.Empty(_kit.Tenants.Slugs);
        Assert.True((await StartAsync(email: "again@northwind.example")).Succeeded);
    }

    [Fact]
    public async Task Dispatcher_SendsNoticesOnce_ToOwnerAndBilling_WithoutTheReason()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        tenant.ReplaceContacts(
            new[]
            {
                PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Grace", EmailAddress.Create("owner@northwind.example"), null),
                PlatformTenantContactDetails.Create(PlatformTenantContactRole.Billing, "Ada", EmailAddress.Create("billing@northwind.example"), null)
            },
            TenantTestKit.Now);
        PlatformTenantLifecycleEvent change = tenant.Suspend(PlatformSuspensionCategory.Compliance, "Secret reason", Guid.NewGuid(), TenantTestKit.Now);
        _kit.Administration().RecordMove(_kit.Staff(), tenant, change, PlatformPermissions.TenantsSuspend, "platform.tenants.suspended");
        PlatformOutboxDispatcher dispatcher = new(_kit.Tenants, new[] { new PlatformTenantNoticeHandler(_kit.Tenants, _kit.Tenants) }, _kit.Store, _kit.Clock);

        Assert.Equal(1, await dispatcher.DispatchAsync(CancellationToken.None));
        Assert.Equal(0, await dispatcher.DispatchAsync(CancellationToken.None));

        Assert.Equal(
            new[] { "owner@northwind.example", "billing@northwind.example" },
            _kit.Tenants.Mail.Select(mail => mail.To));
        Assert.All(_kit.Tenants.Mail, mail => Assert.Equal("Suspended:Compliance", mail.Value));
        Assert.NotNull(Assert.Single(_kit.Tenants.Outbox).ProcessedUtc);
    }

    [Fact]
    public async Task Dispatcher_BacksOffAFailingHandler()
    {
        _kit.Outbox().Write(PlatformMessageTypes.TenantStatusChanged, Guid.NewGuid(), "not a status change");
        PlatformOutboxDispatcher dispatcher = new(_kit.Tenants, new[] { new PlatformTenantNoticeHandler(_kit.Tenants, _kit.Tenants) }, _kit.Store, _kit.Clock);

        Assert.Equal(0, await dispatcher.DispatchAsync(CancellationToken.None));

        PlatformOutboxMessage message = Assert.Single(_kit.Tenants.Outbox);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(TenantTestKit.Now.AddSeconds(30), message.NextAttemptUtc);
        Assert.Equal(0, await dispatcher.DispatchAsync(CancellationToken.None));
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task Dispatcher_ReportsAFailingHandler_WithTheMessageId_AndNoPayload()
    {
        Guid stream = Guid.NewGuid();
        _kit.Outbox().Write(PlatformMessageTypes.TenantStatusChanged, stream, "not a status change");
        PlatformOutboxDispatcher dispatcher = new(_kit.Tenants, new[] { new PlatformTenantNoticeHandler(_kit.Tenants, _kit.Tenants) }, _kit.Store, _kit.Clock);
        List<PlatformOutboxFailure> failures = new();

        Assert.Equal(0, await dispatcher.DispatchAsync(failures.Add, CancellationToken.None));

        PlatformOutboxMessage message = Assert.Single(_kit.Tenants.Outbox);
        PlatformOutboxFailure failure = Assert.Single(failures);
        Assert.Equal(message.Id, failure.MessageId);
        Assert.Equal(PlatformMessageTypes.TenantStatusChanged, failure.Type);
        Assert.Equal(message.LastErrorCode, failure.ErrorCode);
        Assert.Equal(1, failure.Attempts);
        Assert.NotNull(failure.Exception);
    }

    [Fact]
    public async Task InviteOnly_WithoutAnOfferLink_StoresNothing()
    {
        _kit.Tenants.RegistrationInviteOnly = true;

        StartPlatformTenantRegistrationResponse response = await StartAsync();

        Assert.Equal(PlatformRegistrationFlowErrors.InvitationRequired.Code, response.Error?.Code);
        Assert.Empty(_kit.Tenants.Registrations);
    }

    [Theory]
    [InlineData("grace@northwind.example", true)]
    [InlineData("GRACE@northwind.example", true)]
    [InlineData("someone@else.example", false)]
    public async Task InviteOnly_NeedsALinkBoundToTheOwnerEmail(string email, bool starts)
    {
        _kit.Tenants.RegistrationInviteOnly = true;
        string token = IssueOffer("grace@northwind.example");

        StartPlatformTenantRegistrationResponse response = await StartAsync(email: email, offerToken: token);

        Assert.Equal(starts, response.Succeeded);
        Assert.Equal(starts ? null : PlatformRegistrationFlowErrors.InvitationRequired.Code, response.Error?.Code);
    }

    [Fact]
    public async Task InviteOnly_RefusesAnUnknownLink_WithTheSameAnswer()
    {
        _kit.Tenants.RegistrationInviteOnly = true;
        IssueOffer(null);

        StartPlatformTenantRegistrationResponse response = await StartAsync(offerToken: "guessed-token");

        Assert.Equal(PlatformRegistrationFlowErrors.InvitationRequired.Code, response.Error?.Code);
    }

    [Theory]
    [InlineData(PlatformTenantStatus.Draft, PlatformTenantStatus.Active, false)]
    [InlineData(PlatformTenantStatus.Suspended, PlatformTenantStatus.Active, true)]
    [InlineData(PlatformTenantStatus.Active, PlatformTenantStatus.Deactivated, true)]
    [InlineData(PlatformTenantStatus.Active, PlatformTenantStatus.Banned, true)]
    public void OnlyAccessChanges_AreNoticed(PlatformTenantStatus from, PlatformTenantStatus to, bool noticed)
    {
        Assert.Equal(noticed, PlatformTenantNoticeHandler.IsNoticed(from, to));
    }

    private Task<StartPlatformTenantRegistrationResponse> StartAsync(
        string botToken = "human",
        bool accepted = true,
        string terms = "2026-10",
        string email = "grace@northwind.example",
        string? offerToken = null)
    {
        return new StartPlatformTenantRegistrationUseCase(
            _kit.Tenants,
            _kit.Store,
            _kit.Tenants,
            _kit.Slugs(),
            _kit.Tenants,
            _kit.Tenants,
            _kit.Links(),
            _kit.Crypto,
            _kit.Tenants,
            _kit.Audit(),
            _kit.Store,
            _kit.Clock,
            _kit.Legal,
            _offers).ExecuteAsync(
                PlatformAnonymousAuthorizationContext.Instance,
                new StartPlatformTenantRegistrationRequest
                {
                    Company = TenantTestKit.Input(),
                    OwnerName = "Grace Hopper",
                    OwnerEmail = email,
                    OwnerPhone = "+1 514 555 0101",
                    Language = "es",
                    TimeZone = "America/Toronto",
                    Slug = "Northwind",
                    AcceptedTerms = accepted,
                    TermsVersion = terms,
                    PrivacyVersion = "2026-09",
                    BotToken = botToken,
                    OfferToken = offerToken
                },
                CancellationToken.None);
    }

    // An issued offer link bound to one email, as Sales sends it (D-S21).
    private string IssueOffer(string? email)
    {
        const string Token = "offer-link-token";
        Vixly.Platform.Domain.Sales.PlatformOffer offer = Vixly.Platform.Domain.Sales.PlatformOffer.Prepare(
            "Northwind", email, "Invitation", Guid.NewGuid(), Vixly.Platform.Domain.Subscriptions.PlatformSubscriptionTerm.Monthly, 3, null, null,
            Array.Empty<(string, decimal)>(), false, TenantTestKit.Now.AddDays(14), 1, Guid.NewGuid(), TenantTestKit.Now);
        offer.Submit(false, _kit.Crypto.HashToken(Token), TenantTestKit.Now);
        _offers.Offers.Add(offer);
        return Token;
    }

    private Task<VerifyRegistrationEmailResponse> VerifyAsync(string link)
    {
        return new VerifyRegistrationEmailUseCase(
            _kit.Tenants,
            _kit.Tenants,
            _kit.Tenants,
            _kit.Administration(),
            _kit.Crypto,
            _kit.Audit(),
            _kit.Store,
            _kit.Clock).ExecuteAsync(
                PlatformAnonymousAuthorizationContext.Instance,
                new VerifyRegistrationEmailRequest { Token = link },
                CancellationToken.None);
    }

    private Task<GetMyRegistrationStatusResponse> StatusAsync(string accessToken)
    {
        return new GetMyRegistrationStatusUseCase(_kit.Links(), _kit.Store, _kit.Clock).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new GetMyRegistrationStatusRequest { AccessToken = accessToken },
            CancellationToken.None);
    }
}
