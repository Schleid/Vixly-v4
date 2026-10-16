using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Application.UseCases.Legal.AcceptPlatformLegalTerms;
using Vixly.Platform.Application.UseCases.Legal.CreatePlatformLegalDraft;
using Vixly.Platform.Application.UseCases.Legal.DeletePlatformLegalDraft;
using Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalRegister;
using Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegalStatus;
using Vixly.Platform.Application.UseCases.Legal.GetPublicLegalFile;
using Vixly.Platform.Application.UseCases.Legal.GetPublicLegalTerms;
using Vixly.Platform.Application.UseCases.Legal.RecordPlatformTermsAcceptance;
using Vixly.Platform.Application.UseCases.Legal.SendPlatformTermsReminder;
using Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantEntitlements;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformLegalUseCaseTests.cs
//
// PURPOSE:
// Proves Slice 8 legal over fakes: publishing, acceptance, the read-only rule, reminders and registration.
//
// RESPONSIBILITIES:
// - Drafts and publishing need their permissions; publishing supersedes; drafts only are deleted
// - Overdue tenants are read-only until the owner accepts; accepting restores full access
// - Reminders follow the steps once each; manual ones wait a day; notice-only versions send one notice
// - The registration acceptance is recorded when the tenant becomes active
//
// IMPORTANT NOTES:
// - Tests assert codes and states, never text (L-1)
// =========================================================
public sealed class PlatformLegalUseCaseTests
{
    private readonly LegalTestKit _kit = new();

    private (PlatformTenant Tenant, PlatformSubscription Subscription) Tenant() => _kit.Licensing.Tenant();

    private Task<GetPlatformTenantEntitlementsResponse> EntitlementsAsync(Guid tenantId)
    {
        return new GetPlatformTenantEntitlementsUseCase(_kit.Kit.Subscriptions(), _kit.Kit.Collections, _kit.Kit.Tenants.LegalGate()).ExecuteAsync(
            _kit.Kit.Staff(PlatformPermissions.SubscriptionsView), new GetPlatformTenantEntitlementsRequest { PlatformTenantId = tenantId }, CancellationToken.None);
    }

    private async Task<PlatformLegalStatusAnswer> StatusAsync(Guid tenantId, string language = "fr")
    {
        return (await new GetPlatformTenantLegalStatusUseCase(_kit.Administration()).ExecuteAsync(LicensingTestKit.Caller(tenantId),
            new GetPlatformTenantLegalStatusRequest { Language = language }, CancellationToken.None)).Answer!;
    }

    private async Task<string?> AcceptAsync(Guid tenantId, Guid documentId, string? sha, string language = "fr", string role = "owner")
    {
        var response = await new AcceptPlatformLegalTermsUseCase(_kit.Administration()).ExecuteAsync(LicensingTestKit.Caller(tenantId, PlatformTenantAccess.ParseRole(role)),
            new AcceptPlatformLegalTermsRequest { DocumentId = documentId, Sha256 = sha, Language = language }, CancellationToken.None);
        return response.Error?.Code;
    }

    [Fact]
    public async Task Publishing_NeedsItsPermissions_EveryLanguage_AndSupersedesThePreviousVersion()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new CreatePlatformLegalDraftUseCase(_kit.Administration()).ExecuteAsync(
            _kit.Kit.Staff(PlatformPermissions.LegalView), new CreatePlatformLegalDraftRequest { Kind = PlatformLegalDocumentKind.Privacy, Version = "p-1" }, CancellationToken.None));

        Guid missing = await _kit.DraftAsync(PlatformLegalDocumentKind.Privacy, "p-1", languages: new[] { "en", "fr" });
        Assert.Equal("platform.legal.languages_missing", await _kit.PublishAsync(missing));
        Assert.Equal("platform.legal.version_taken", (await new CreatePlatformLegalDraftUseCase(_kit.Administration()).ExecuteAsync(
            _kit.Staff(), new CreatePlatformLegalDraftRequest { Kind = PlatformLegalDocumentKind.Privacy, Version = "p-1" }, CancellationToken.None)).Error!.Code);
        Assert.True((await new DeletePlatformLegalDraftUseCase(_kit.Administration()).ExecuteAsync(_kit.Staff(), new DeletePlatformLegalDraftRequest { DocumentId = missing }, CancellationToken.None)).Succeeded);
        Assert.Empty(_kit.Store.Files);

        Guid onlyTerms = await _kit.DraftAsync(PlatformLegalDocumentKind.Terms, "t-0");
        Assert.Equal("platform.legal.parts_invalid", await _kit.PublishAsync(onlyTerms));

        PlatformLegalDocument first = await _kit.PublishTermsAsync("2026-11");
        Assert.Equal(new[] { PlatformLegalDocumentKind.Privacy, PlatformLegalDocumentKind.DataProcessing }, first.Parts.Select(part => part.Kind));
        PlatformLegalDocument second = await _kit.PublishTermsAsync("2026-12");
        Assert.Equal(PlatformLegalDocumentStatus.Superseded, first.Status);
        Assert.Equal(PlatformLegalDocumentStatus.Published, second.Status);
        Assert.Equal("platform.legal.not_draft", (await new DeletePlatformLegalDraftUseCase(_kit.Administration()).ExecuteAsync(_kit.Staff(), new DeletePlatformLegalDraftRequest { DocumentId = second.Id }, CancellationToken.None)).Error!.Code);
        Assert.Contains(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.LegalVersionPublished);

        var publicTerms = await new GetPublicLegalTermsUseCase(_kit.Administration()).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new GetPublicLegalTermsRequest { Language = "es" }, CancellationToken.None);
        Assert.Equal("2026-12", publicTerms.Terms!.Version);
        Assert.Equal("es", publicTerms.Terms.Language);
        Assert.Equal(2, publicTerms.Terms.Parts.Count);
        Assert.Equal("platform.legal.document_not_found", (await new GetPublicLegalFileUseCase(_kit.Administration()).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new GetPublicLegalFileRequest { DocumentId = onlyTerms, Language = "en" }, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task Overdue_MakesTheTenantReadOnly_UntilTheOwnerAccepts()
    {
        (PlatformTenant tenant, _) = Tenant();
        PlatformLegalDocument terms = await _kit.PublishTermsAsync();

        PlatformLegalStatusAnswer due = await StatusAsync(tenant.Id);
        Assert.Equal(PlatformLegalStatus.Due, due.Status);
        Assert.Equal(_kit.Now.AddDays(30), due.DeadlineUtc);
        Assert.False(due.ReadOnly);
        Assert.Equal("fr", due.Terms!.Language);
        Assert.Equal(PlatformSubscriptionAccess.Full, (await EntitlementsAsync(tenant.Id)).Entitlements!.Access);

        _kit.Kit.Tenants.Clock.Advance(TimeSpan.FromDays(31));
        var overdue = (await EntitlementsAsync(tenant.Id)).Entitlements!;
        Assert.Equal(PlatformSubscriptionAccess.ReadOnly, overdue.Access);
        Assert.Equal(PlatformLegalStatus.Overdue, overdue.Legal!.Status);
        Assert.Equal(PlatformSubscriptionAccess.ReadOnly, await _kit.Licensing.Administration().AccessAsync(tenant.Id, CancellationToken.None));
        Assert.True((await StatusAsync(tenant.Id)).ReadOnly);

        string sha = (await StatusAsync(tenant.Id)).Terms!.Sha256;
        Assert.Equal("platform.legal.version_changed", await AcceptAsync(tenant.Id, terms.Id, "0000"));
        Assert.Equal("platform.legal.owner_only", await AcceptAsync(tenant.Id, terms.Id, sha, role: "admin"));
        Assert.Null(await AcceptAsync(tenant.Id, terms.Id, sha));
        Assert.Null(await AcceptAsync(tenant.Id, terms.Id, sha));

        PlatformTenantTermsAcceptance acceptance = Assert.Single(_kit.Store.Acceptances);
        Assert.Equal("fr", acceptance.Language);
        Assert.Equal("198.51.100.4", acceptance.ClientIp);
        Assert.Equal("ops-user-1", acceptance.ActorReference);
        Assert.Equal(PlatformSubscriptionAccess.Full, (await EntitlementsAsync(tenant.Id)).Entitlements!.Access);
        Assert.Null((await EntitlementsAsync(tenant.Id)).Entitlements!.Legal);
        Assert.Contains(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.TermsAccepted);

        // A notice-only version asks nothing more.
        await _kit.PublishTermsAsync("2026-12-fix", acceptanceRequired: false);
        Assert.Equal(PlatformLegalStatus.Accepted, (await StatusAsync(tenant.Id)).Status);
    }

    [Fact]
    public async Task Reminders_FollowTheSteps_OnceEach_AndStopWhenAccepted()
    {
        (PlatformTenant tenant, _) = Tenant();
        (PlatformTenant accepting, _) = Tenant();
        PlatformLegalDocument terms = await _kit.PublishTermsAsync();
        PlatformLegalClock clock = new(_kit.Administration());

        Assert.Equal(2, await clock.RunAsync(CancellationToken.None));
        Assert.Equal(0, await clock.RunAsync(CancellationToken.None));
        await _kit.DeliverAsync();
        (string to, PlatformLegalMail mail) = _kit.Store.Mails.First();
        Assert.Equal(PlatformTermsReminderStep.Published, mail.Step);
        Assert.Equal(tenant.ContactFor(PlatformTenantContactRole.Owner).Email.Value, to);
        Assert.EndsWith("/legal", mail.Link);

        Assert.Null(await AcceptAsync(accepting.Id, terms.Id, (await StatusAsync(accepting.Id)).Terms!.Sha256));
        _kit.Kit.Tenants.Clock.Advance(TimeSpan.FromDays(17));
        Assert.Equal(1, await clock.RunAsync(CancellationToken.None));
        Assert.Contains(_kit.Store.Reminders, item => item.PlatformTenantId == tenant.Id && item.Step == PlatformTermsReminderStep.Before14Days);
        Assert.DoesNotContain(_kit.Store.Reminders, item => item.PlatformTenantId == accepting.Id && item.Step != PlatformTermsReminderStep.Published);

        SendPlatformTermsReminderUseCase manual = new(_kit.Administration());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => manual.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.LegalView), new SendPlatformTermsReminderRequest { PlatformTenantId = tenant.Id, Reason = "Call" }, CancellationToken.None));
        Assert.True((await manual.ExecuteAsync(_kit.Staff(), new SendPlatformTermsReminderRequest { PlatformTenantId = tenant.Id, Reason = "Owner asked for the link" }, CancellationToken.None)).Succeeded);
        Assert.Equal("platform.legal.reminder_too_soon", (await manual.ExecuteAsync(_kit.Staff(), new SendPlatformTermsReminderRequest { PlatformTenantId = tenant.Id, Reason = "Again" }, CancellationToken.None)).Error!.Code);
        Assert.Equal("platform.legal.tenant_not_asked", (await manual.ExecuteAsync(_kit.Staff(), new SendPlatformTermsReminderRequest { PlatformTenantId = accepting.Id, Reason = "No" }, CancellationToken.None)).Error!.Code);

        var register = await new GetPlatformLegalRegisterUseCase(_kit.Administration()).ExecuteAsync(_kit.Staff(), new GetPlatformLegalRegisterRequest { Status = PlatformLegalStatus.Due }, CancellationToken.None);
        Assert.Equal(tenant.Id, Assert.Single(register.Tenants!.Items).PlatformTenantId);
    }

    [Fact]
    public async Task NoticeOnlyVersions_SendOneNoticeToEveryTenant()
    {
        Tenant();
        Tenant();
        await _kit.PublishTermsAsync("notice-1", acceptanceRequired: false);
        PlatformLegalClock clock = new(_kit.Administration());

        Assert.Equal(2, await clock.RunAsync(CancellationToken.None));
        Assert.Equal(0, await clock.RunAsync(CancellationToken.None));
        await _kit.DeliverAsync();
        Assert.All(_kit.Store.Mails, item => Assert.False(item.Mail.AcceptanceRequired));
    }

    [Fact]
    public async Task StaffRecord_OfASignedPaper_NeedsStepUpReasonAndEvidence()
    {
        (PlatformTenant tenant, _) = Tenant();
        await _kit.PublishTermsAsync();
        RecordPlatformTermsAcceptanceUseCase record = new(_kit.Administration());

        Assert.Equal("platform.legal.evidence_invalid", (await record.ExecuteAsync(_kit.Staff(), new RecordPlatformTermsAcceptanceRequest
        { PlatformTenantId = tenant.Id, Language = "en", OwnerName = "Ada", Reason = "Signed at the office" }, CancellationToken.None)).Error!.Code);
        Assert.True((await record.ExecuteAsync(_kit.Staff(), new RecordPlatformTermsAcceptanceRequest
        { PlatformTenantId = tenant.Id, Language = "en", OwnerName = "Ada", Reason = "Signed at the office", Evidence = LegalTestKit.Pdf, EvidenceContentType = "application/pdf" }, CancellationToken.None)).Succeeded);
        Assert.Equal("platform.legal.already_accepted", (await record.ExecuteAsync(_kit.Staff(), new RecordPlatformTermsAcceptanceRequest
        { PlatformTenantId = tenant.Id, Language = "en", OwnerName = "Ada", Reason = "Again", Evidence = LegalTestKit.Pdf, EvidenceContentType = "application/pdf" }, CancellationToken.None)).Error!.Code);
        Assert.Equal(PlatformTermsAcceptanceSource.StaffRecord, Assert.Single(_kit.Store.Acceptances).Source);
        Assert.Equal(PlatformLegalStatus.Accepted, (await StatusAsync(tenant.Id)).Status);
    }

    [Fact]
    public async Task RegistrationAcceptance_IsRecorded_WhenTheTenantBecomesActive()
    {
        PlatformLegalDocument terms = await _kit.PublishTermsAsync("2026-11");
        (PlatformTenant tenant, _) = Tenant();
        PlatformTenantRegistration registration = PlatformTenantRegistration.Start(
            TenantTestKit.Company(), "Grace Hopper", Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create("grace@orbit.example"), Vixly.Shared.Kernel.ValueObjects.PhoneNumber.Create("+1 514 555 0101"),
            Vixly.Shared.Kernel.Localization.LanguageCode.Es, "America/Toronto", "orbit", new string('a', 64), true, "2026-11", "p-1", "203.0.113.7", _kit.Now);
        registration.MarkEmailVerified(tenant.Id, _kit.Now);
        _kit.Kit.Tenants.Tenants.Registrations.Add(registration);

        _kit.Kit.Tenants.Outbox().Write(PlatformMessageTypes.TenantStatusChanged, tenant.Id,
            new PlatformTenantStatusChanged(tenant.Id, tenant.TenantCode, PlatformTenantStatus.PendingActivation, PlatformTenantStatus.Active, null, _kit.Now));
        PlatformOutboxMessage message = _kit.Kit.Tenants.Tenants.Outbox.Last();
        await new PlatformLegalRegistrationHandler(_kit.Administration()).HandleAsync(message, CancellationToken.None);
        await new PlatformLegalRegistrationHandler(_kit.Administration()).HandleAsync(message, CancellationToken.None);

        PlatformTenantTermsAcceptance acceptance = Assert.Single(_kit.Store.Acceptances);
        Assert.Equal(PlatformTermsAcceptanceSource.Registration, acceptance.Source);
        Assert.Equal("es", acceptance.Language);
        Assert.Equal("203.0.113.7", acceptance.ClientIp);
        Assert.Equal(terms.Id, acceptance.PlatformLegalDocumentId);
        Assert.Equal(PlatformLegalStatus.Accepted, (await StatusAsync(tenant.Id)).Status);
    }
}
