using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeLegalStore.cs
//
// PURPOSE:
// In-memory legal documents, files, acceptances, reminders, read models and mailer for the Slice 8 tests.
//
// RESPONSIBILITIES:
// - Answer the repository and query ports from lists
// - Record every legal email instead of sending it
//
// IMPORTANT NOTES:
// - The register is worked out in memory with the same rules as the SQL query
// =========================================================
internal sealed class FakeLegalStore : IPlatformLegalRepository, IPlatformLegalQueries, IPlatformLegalMailer
{
    private readonly FakeTenantStore _tenants;

    public FakeLegalStore(FakeTenantStore tenants)
    {
        _tenants = tenants;
    }

    public List<PlatformLegalDocument> Documents { get; } = new();

    public List<PlatformLegalDocumentFile> Files { get; } = new();

    public List<PlatformTenantTermsAcceptance> Acceptances { get; } = new();

    public List<PlatformTermsReminder> Reminders { get; } = new();

    public List<(string To, PlatformLegalMail Mail)> Mails { get; } = new();

    Task<PlatformLegalDocument?> IPlatformLegalRepository.GetDocumentAsync(Guid documentId, CancellationToken cancellationToken) => Task.FromResult(Documents.FirstOrDefault(item => item.Id == documentId));

    public Task<IReadOnlyList<PlatformLegalDocument>> GetDocumentsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformLegalDocument>>(Documents.Where(item => documentIds.Contains(item.Id)).ToArray());
    }

    public void AddDocument(PlatformLegalDocument document) => Documents.Add(document);

    public Task RemoveDraftAsync(PlatformLegalDocument document, CancellationToken cancellationToken)
    {
        Files.RemoveAll(item => item.PlatformLegalDocumentId == document.Id);
        Documents.Remove(document);
        return Task.CompletedTask;
    }

    public Task<PlatformLegalDocument?> GetPublishedAsync(PlatformLegalDocumentKind kind, CancellationToken cancellationToken)
    {
        return Task.FromResult(Documents.FirstOrDefault(item => item.Kind == kind && item.Status == PlatformLegalDocumentStatus.Published));
    }

    public Task<IReadOnlyList<PlatformLegalDocumentKind>> ListPublishedKindsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformLegalDocumentKind>>(Documents.Where(item => item.Status == PlatformLegalDocumentStatus.Published).Select(item => item.Kind).Distinct().ToArray());
    }

    public Task<bool> VersionExistsAsync(PlatformLegalDocumentKind kind, string version, Guid? exceptDocumentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Documents.Any(item => item.Kind == kind && item.Version == version && item.Id != exceptDocumentId));
    }

    public Task<PlatformLegalDocument?> LatestRequiredTermsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Documents.Where(item => item.Kind == PlatformLegalDocumentKind.Terms && item.AcceptanceRequired && item.Status != PlatformLegalDocumentStatus.Draft)
            .OrderByDescending(item => item.PublishedUtc).FirstOrDefault());
    }

    public Task<PlatformLegalDocument?> FindTermsByVersionAsync(string version, CancellationToken cancellationToken)
    {
        return Task.FromResult(Documents.FirstOrDefault(item => item.Kind == PlatformLegalDocumentKind.Terms && item.Version == version && item.Status != PlatformLegalDocumentStatus.Draft));
    }

    public Task<PlatformLegalDocumentFile?> GetFileAsync(Guid documentId, string language, CancellationToken cancellationToken)
    {
        return Task.FromResult(Files.FirstOrDefault(item => item.PlatformLegalDocumentId == documentId && item.Language == language));
    }

    public Task<IReadOnlyList<string>> ListFileLanguagesAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<string>>(Files.Where(item => item.PlatformLegalDocumentId == documentId).Select(item => item.Language).ToArray());
    }

    public void AddFile(PlatformLegalDocumentFile file) => Files.Add(file);

    public Task<bool> HasAcceptedSinceAsync(Guid platformTenantId, DateTimeOffset publishedUtc, CancellationToken cancellationToken) => Task.FromResult(AcceptedSince(platformTenantId, publishedUtc) is not null);

    public Task<bool> HasAcceptedAsync(Guid platformTenantId, Guid documentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Acceptances.Any(item => item.PlatformTenantId == platformTenantId && item.PlatformLegalDocumentId == documentId));
    }

    public void AddAcceptance(PlatformTenantTermsAcceptance acceptance)
    {
        if (Acceptances.Any(item => item.PlatformTenantId == acceptance.PlatformTenantId && item.PlatformLegalDocumentId == acceptance.PlatformLegalDocumentId))
        {
            throw new InvalidOperationException("Unique index: one acceptance per tenant and version.");
        }

        Acceptances.Add(acceptance);
    }

    public Task<IReadOnlyList<PlatformTermsReminderStep>> ListReminderStepsAsync(Guid platformTenantId, Guid documentId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformTermsReminderStep>>(Reminders.Where(item => item.PlatformTenantId == platformTenantId && item.PlatformLegalDocumentId == documentId).Select(item => item.Step).ToArray());
    }

    public Task<DateTimeOffset?> LastManualReminderAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Reminders.Where(item => item.PlatformTenantId == platformTenantId && item.Step == PlatformTermsReminderStep.Manual).Max(item => (DateTimeOffset?)item.CreatedUtc));
    }

    public void AddReminder(PlatformTermsReminder reminder) => Reminders.Add(reminder);

    public Task<PagedResult<PlatformLegalDocumentItem>> ListDocumentsAsync(PlatformLegalDocumentKind? kind, PagedRequest page, CancellationToken cancellationToken)
    {
        PlatformLegalDocumentItem[] items = Documents.Where(item => kind is null || item.Kind == kind).Select(Item).ToArray();
        return Task.FromResult(new PagedResult<PlatformLegalDocumentItem>(items.Skip(page.Skip).Take(page.PageSize).ToArray(), page, items.Length));
    }

    Task<PlatformLegalDocumentItem?> IPlatformLegalQueries.GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Documents.FirstOrDefault(item => item.Id == documentId) is { } document ? Item(document) : null);
    }

    public Task<IReadOnlyList<PlatformLegalDocumentItem>> ListPublishedAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformLegalDocumentItem>>(Documents.Where(item => item.Status == PlatformLegalDocumentStatus.Published).Select(Item).ToArray());
    }

    public Task<IReadOnlyList<PlatformLegalFileItem>> ListFilesAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformLegalFileItem>>(Files.Where(item => item.PlatformLegalDocumentId == documentId)
            .Select(item => new PlatformLegalFileItem(item.Language, item.Sha256, item.SizeBytes, item.Summary, item.CreatedUtc)).ToArray());
    }

    public Task<PlatformLegalFileContent?> GetFileContentAsync(Guid documentId, string language, CancellationToken cancellationToken)
    {
        PlatformLegalDocument? document = Documents.FirstOrDefault(item => item.Id == documentId);
        PlatformLegalDocumentFile? file = Files.FirstOrDefault(item => item.PlatformLegalDocumentId == documentId && item.Language == language);
        return Task.FromResult(document is null || file is null ? null : new PlatformLegalFileContent(file.Content, file.Sha256, file.Language, document.Kind, document.Version, document.Status));
    }

    public Task<PagedResult<PlatformLegalRegisterItem>> RegisterAsync(PlatformLegalDocument required, PlatformLegalRegisterFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateTimeOffset published = required.PublishedUtc!.Value;
        PlatformLegalRegisterItem[] items = Asked()
            .Select(tenant =>
            {
                PlatformTenantTermsAcceptance? accepted = AcceptedSince(tenant.Id, published);
                DateTimeOffset deadline = PlatformLegalRules.Deadline(required, tenant.ActivatedUtc);
                return new PlatformLegalRegisterItem(tenant.Id, tenant.Company.DisplayName, tenant.TenantCode, tenant.Status, PlatformLegalRules.Status(true, accepted is not null, deadline, utcNow),
                    deadline, accepted?.AcceptedUtc, accepted?.Version, null);
            })
            .Where(item => filter.Status is null || item.Status == filter.Status)
            .ToArray();

        return Task.FromResult(new PagedResult<PlatformLegalRegisterItem>(items.Skip(page.Skip).Take(page.PageSize).ToArray(), page, items.Length));
    }

    public Task<IReadOnlyList<PlatformTermsAcceptanceItem>> ListAcceptancesAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformTermsAcceptanceItem>>(Acceptances.Where(item => item.PlatformTenantId == platformTenantId)
            .Select(item => new PlatformTermsAcceptanceItem(item.Id, item.Version, item.Language, item.Source, item.AcceptedUtc, item.ActorName, item.ActorRole, item.ClientIp, item.UserAgent, item.Sha256, null, item.Reason, item.Evidence is not null))
            .ToArray());
    }

    public Task<IReadOnlyList<PlatformTermsReminderItem>> ListRemindersAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformTermsReminderItem>>(Reminders.Where(item => item.PlatformTenantId == platformTenantId)
            .Select(item => new PlatformTermsReminderItem(item.Step, Documents.First(document => document.Id == item.PlatformLegalDocumentId).Version, item.Language, item.CreatedUtc, null, item.Reason))
            .ToArray());
    }

    public Task<IReadOnlyList<PlatformLegalTenantRow>> ListTenantsToRemindAsync(Guid documentId, DateTimeOffset? acceptedSinceUtc, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformLegalTenantRow>>(Asked()
            .Where(tenant => acceptedSinceUtc is { } since
                ? AcceptedSince(tenant.Id, since) is null
                : !Reminders.Any(reminder => reminder.PlatformTenantId == tenant.Id && reminder.PlatformLegalDocumentId == documentId))
            .Take(take)
            .Select(tenant => new PlatformLegalTenantRow(tenant.Id, tenant.ActivatedUtc, tenant.DefaultLanguage.Value))
            .ToArray());
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformLegalMail mail, CancellationToken cancellationToken)
    {
        Mails.Add((recipient.Email.Value, mail));
        return Task.CompletedTask;
    }

    private IEnumerable<PlatformTenant> Asked() => _tenants.Tenants.Where(tenant => tenant.Status is PlatformTenantStatus.Active or PlatformTenantStatus.Suspended);

    private PlatformTenantTermsAcceptance? AcceptedSince(Guid tenantId, DateTimeOffset publishedUtc)
    {
        return Acceptances.Where(item => item.PlatformTenantId == tenantId && Documents.Any(document => document.Id == item.PlatformLegalDocumentId && document.PublishedUtc >= publishedUtc))
            .OrderByDescending(item => item.AcceptedUtc).FirstOrDefault();
    }

    private static PlatformLegalDocumentItem Item(PlatformLegalDocument document)
    {
        return new PlatformLegalDocumentItem(document.Id, document.Kind, document.Version, document.Status, document.AcceptanceRequired, document.EffectiveUtc, document.DeadlineUtc, document.PublishedUtc, null, document.CreatedUtc);
    }
}
