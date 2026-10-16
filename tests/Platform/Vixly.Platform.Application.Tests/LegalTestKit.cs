using System.Text;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Application.UseCases.Legal.CreatePlatformLegalDraft;
using Vixly.Platform.Application.UseCases.Legal.PublishPlatformLegalDocument;
using Vixly.Platform.Application.UseCases.Legal.SetPlatformLegalFile;
using Vixly.Platform.Application.UseCases.Legal.UpdatePlatformLegalDraft;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: LegalTestKit.cs
//
// PURPOSE:
// Builds the legal administration over the shared fakes, on top of the licensing kit.
//
// RESPONSIBILITIES:
// - Publish documents through the use cases, as staff would
// - Deliver legal emails through the outbox handler
//
// IMPORTANT NOTES:
// - The licensing kit is reused so portal access sees the same tenants and Terms
// =========================================================
internal sealed class LegalTestKit
{
    public static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n% Vixly terms\n");

    public LicensingTestKit Licensing { get; } = new();

    public SubscriptionTestKit Kit => Licensing.Kit;

    public FakeLegalStore Store => Kit.Tenants.Legal;

    public DateTimeOffset Now => Kit.Tenants.Clock.GetUtcNow();

    public PlatformLegalAdministration Administration()
    {
        return new PlatformLegalAdministration(
            Kit.Tenants.Gate(),
            new PlatformStepUpGuard(Kit.Tenants.Store, Kit.Tenants.Clock),
            Kit.Tenants.Store,
            Store,
            Store,
            Kit.Tenants.LegalGate(),
            Kit.Tenants.Tenants,
            Kit.Tenants.Tenants,
            Kit.Sales,
            Kit.Tenants.Audit(),
            Kit.Tenants.Outbox(),
            Kit.Tenants.Store,
            Kit.Tenants.Clock);
    }

    private (DateTimeOffset At, PlatformAuthorizationContext Context)? _staff;

    // One signed-in staff member per clock time: step-up stays fresh and the fake session tokens do not run out.
    public PlatformAuthorizationContext Staff()
    {
        if (_staff is not { } staff || staff.At != Now)
        {
            _staff = (Now, Kit.Staff(PlatformPermissions.LegalView, PlatformPermissions.LegalManage, PlatformPermissions.LegalPublish, PlatformPermissions.LegalRemind, PlatformPermissions.LegalRecordAcceptance));
        }

        return _staff.Value.Context;
    }

    public async Task<Guid> DraftAsync(PlatformLegalDocumentKind kind, string version, bool acceptanceRequired = true, string[]? languages = null)
    {
        PlatformAuthorizationContext staff = Staff();
        var created = await new CreatePlatformLegalDraftUseCase(Administration()).ExecuteAsync(staff, new CreatePlatformLegalDraftRequest { Kind = kind, Version = version }, CancellationToken.None);
        Assert.True(created.Succeeded, created.Error?.Code);
        PlatformLegalDocument document = Store.Documents.Single(item => item.Id == created.DocumentId);

        foreach (string language in languages ?? new[] { "en", "fr", "es" })
        {
            var file = await new SetPlatformLegalFileUseCase(Administration()).ExecuteAsync(staff,
                new SetPlatformLegalFileRequest { DocumentId = document.Id, Language = language, Content = Pdf, Summary = "What changed (" + language + ")" }, CancellationToken.None);
            Assert.True(file.Succeeded, file.Error?.Code);
        }

        if (kind == PlatformLegalDocumentKind.Terms)
        {
            var updated = await new UpdatePlatformLegalDraftUseCase(Administration()).ExecuteAsync(staff, new UpdatePlatformLegalDraftRequest
            {
                DocumentId = document.Id, Version = version, AcceptanceRequired = acceptanceRequired, IncludedDocumentIds = document.Parts.Select(part => part.IncludedDocumentId).ToArray()
            }, CancellationToken.None);
            Assert.True(updated.Succeeded, updated.Error?.Code);
        }

        return document.Id;
    }

    public async Task<string?> PublishAsync(Guid documentId)
    {
        var response = await new PublishPlatformLegalDocumentUseCase(Administration()).ExecuteAsync(Staff(), new PublishPlatformLegalDocumentRequest { DocumentId = documentId }, CancellationToken.None);
        return response.Error?.Code;
    }

    // Privacy and DPA, then Terms that includes them.
    public async Task<PlatformLegalDocument> PublishTermsAsync(string version = "2026-11", bool acceptanceRequired = true)
    {
        if (Store.Documents.All(item => item.Kind != PlatformLegalDocumentKind.Privacy))
        {
            Assert.Null(await PublishAsync(await DraftAsync(PlatformLegalDocumentKind.Privacy, "p-1")));
            Assert.Null(await PublishAsync(await DraftAsync(PlatformLegalDocumentKind.DataProcessing, "d-1")));
        }

        Guid terms = await DraftAsync(PlatformLegalDocumentKind.Terms, version, acceptanceRequired);
        Assert.Null(await PublishAsync(terms));
        return Store.Documents.Single(item => item.Id == terms);
    }

    public async Task DeliverAsync()
    {
        PlatformLegalNoticeHandler handler = new(Administration(), Store);

        foreach (PlatformOutboxMessage message in Kit.Tenants.Tenants.Outbox.Where(item => item.Type == PlatformMessageTypes.LegalNotice && item.ProcessedUtc is null).ToArray())
        {
            await handler.HandleAsync(message, CancellationToken.None);
            message.MarkProcessed(Now);
        }
    }
}
