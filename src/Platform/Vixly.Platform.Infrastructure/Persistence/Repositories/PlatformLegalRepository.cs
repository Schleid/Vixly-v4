using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformLegalRepository.cs
//
// PURPOSE:
// EF Core store for legal documents, files, acceptances and reminders.
//
// RESPONSIBILITIES:
// - Load tracked rows; answer the rule questions
//
// IMPORTANT NOTES:
// - File bytes are loaded only when a file is asked for
// =========================================================
internal sealed class PlatformLegalRepository : IPlatformLegalRepository
{
    private readonly PlatformDbContext _context;

    public PlatformLegalRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlatformLegalDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _context.LegalDocuments.Local.FirstOrDefault(item => item.Id == documentId)
            ?? await _context.LegalDocuments.FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformLegalDocument>> GetDocumentsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken)
    {
        return await _context.LegalDocuments.Where(item => documentIds.Contains(item.Id)).ToListAsync(cancellationToken);
    }

    public void AddDocument(PlatformLegalDocument document) => _context.LegalDocuments.Add(document);

    public async Task RemoveDraftAsync(PlatformLegalDocument document, CancellationToken cancellationToken)
    {
        _context.LegalDocumentFiles.RemoveRange(await _context.LegalDocumentFiles.Where(item => item.PlatformLegalDocumentId == document.Id).ToListAsync(cancellationToken));
        _context.LegalDocuments.Remove(document);
    }

    public async Task<PlatformLegalDocument?> GetPublishedAsync(PlatformLegalDocumentKind kind, CancellationToken cancellationToken)
    {
        return _context.LegalDocuments.Local.FirstOrDefault(item => item.Kind == kind && item.Status == PlatformLegalDocumentStatus.Published)
            ?? await _context.LegalDocuments.FirstOrDefaultAsync(item => item.Kind == kind && item.Status == PlatformLegalDocumentStatus.Published, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformLegalDocumentKind>> ListPublishedKindsAsync(CancellationToken cancellationToken)
    {
        return await _context.LegalDocuments.AsNoTracking().Where(item => item.Status == PlatformLegalDocumentStatus.Published).Select(item => item.Kind).Distinct().ToListAsync(cancellationToken);
    }

    public Task<bool> VersionExistsAsync(PlatformLegalDocumentKind kind, string version, Guid? exceptDocumentId, CancellationToken cancellationToken)
    {
        return _context.LegalDocuments.AnyAsync(item => item.Kind == kind && item.Version == version && item.Id != exceptDocumentId, cancellationToken);
    }

    public Task<PlatformLegalDocument?> LatestRequiredTermsAsync(CancellationToken cancellationToken)
    {
        return _context.LegalDocuments
            .Where(item => item.Kind == PlatformLegalDocumentKind.Terms && item.AcceptanceRequired && item.Status != PlatformLegalDocumentStatus.Draft)
            .OrderByDescending(item => item.PublishedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PlatformLegalDocument?> FindTermsByVersionAsync(string version, CancellationToken cancellationToken)
    {
        return _context.LegalDocuments.FirstOrDefaultAsync(item => item.Kind == PlatformLegalDocumentKind.Terms && item.Version == version && item.Status != PlatformLegalDocumentStatus.Draft, cancellationToken);
    }

    public async Task<PlatformLegalDocumentFile?> GetFileAsync(Guid documentId, string language, CancellationToken cancellationToken)
    {
        return _context.LegalDocumentFiles.Local.FirstOrDefault(item => item.PlatformLegalDocumentId == documentId && item.Language == language)
            ?? await _context.LegalDocumentFiles.FirstOrDefaultAsync(item => item.PlatformLegalDocumentId == documentId && item.Language == language, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListFileLanguagesAsync(Guid documentId, CancellationToken cancellationToken)
    {
        List<string> stored = await _context.LegalDocumentFiles.AsNoTracking().Where(item => item.PlatformLegalDocumentId == documentId).Select(item => item.Language).ToListAsync(cancellationToken);
        return stored.Union(_context.LegalDocumentFiles.Local.Where(item => item.PlatformLegalDocumentId == documentId).Select(item => item.Language), StringComparer.Ordinal).ToArray();
    }

    public void AddFile(PlatformLegalDocumentFile file) => _context.LegalDocumentFiles.Add(file);

    public Task<bool> HasAcceptedSinceAsync(Guid platformTenantId, DateTimeOffset publishedUtc, CancellationToken cancellationToken)
    {
        return (from acceptance in _context.TenantTermsAcceptances
                join document in _context.LegalDocuments on acceptance.PlatformLegalDocumentId equals document.Id
                where acceptance.PlatformTenantId == platformTenantId && document.PublishedUtc >= publishedUtc
                select acceptance.Id).AnyAsync(cancellationToken);
    }

    public Task<bool> HasAcceptedAsync(Guid platformTenantId, Guid documentId, CancellationToken cancellationToken)
    {
        return _context.TenantTermsAcceptances.AnyAsync(item => item.PlatformTenantId == platformTenantId && item.PlatformLegalDocumentId == documentId, cancellationToken);
    }

    public void AddAcceptance(PlatformTenantTermsAcceptance acceptance) => _context.TenantTermsAcceptances.Add(acceptance);

    public async Task<IReadOnlyList<PlatformTermsReminderStep>> ListReminderStepsAsync(Guid platformTenantId, Guid documentId, CancellationToken cancellationToken)
    {
        return await _context.TermsReminders.AsNoTracking().Where(item => item.PlatformTenantId == platformTenantId && item.PlatformLegalDocumentId == documentId).Select(item => item.Step).ToListAsync(cancellationToken);
    }

    public Task<DateTimeOffset?> LastManualReminderAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.TermsReminders.AsNoTracking().Where(item => item.PlatformTenantId == platformTenantId && item.Step == PlatformTermsReminderStep.Manual).MaxAsync(item => (DateTimeOffset?)item.CreatedUtc, cancellationToken);
    }

    public void AddReminder(PlatformTermsReminder reminder) => _context.TermsReminders.Add(reminder);
}
