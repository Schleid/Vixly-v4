using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLegalRepository.cs
//
// PURPOSE:
// Stores legal documents, their files, acceptances and reminders.
//
// RESPONSIBILITIES:
// - Load and add rows for the use cases
// - Answer the rule questions: current versions, acceptance, reminders sent
//
// IMPORTANT NOTES:
// - Saved by the unit of work
// =========================================================
public interface IPlatformLegalRepository
{
    Task<PlatformLegalDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformLegalDocument>> GetDocumentsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken);

    void AddDocument(PlatformLegalDocument document);

    Task RemoveDraftAsync(PlatformLegalDocument document, CancellationToken cancellationToken);

    Task<PlatformLegalDocument?> GetPublishedAsync(PlatformLegalDocumentKind kind, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformLegalDocumentKind>> ListPublishedKindsAsync(CancellationToken cancellationToken);

    Task<bool> VersionExistsAsync(PlatformLegalDocumentKind kind, string version, Guid? exceptDocumentId, CancellationToken cancellationToken);

    // The newest Terms version that needs acceptance, published or superseded.
    Task<PlatformLegalDocument?> LatestRequiredTermsAsync(CancellationToken cancellationToken);

    Task<PlatformLegalDocument?> FindTermsByVersionAsync(string version, CancellationToken cancellationToken);

    Task<PlatformLegalDocumentFile?> GetFileAsync(Guid documentId, string language, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListFileLanguagesAsync(Guid documentId, CancellationToken cancellationToken);

    void AddFile(PlatformLegalDocumentFile file);

    // Accepted any Terms version published at or after this time.
    Task<bool> HasAcceptedSinceAsync(Guid platformTenantId, DateTimeOffset publishedUtc, CancellationToken cancellationToken);

    Task<bool> HasAcceptedAsync(Guid platformTenantId, Guid documentId, CancellationToken cancellationToken);

    void AddAcceptance(PlatformTenantTermsAcceptance acceptance);

    Task<IReadOnlyList<PlatformTermsReminderStep>> ListReminderStepsAsync(Guid platformTenantId, Guid documentId, CancellationToken cancellationToken);

    Task<DateTimeOffset?> LastManualReminderAsync(Guid platformTenantId, CancellationToken cancellationToken);

    void AddReminder(PlatformTermsReminder reminder);
}
