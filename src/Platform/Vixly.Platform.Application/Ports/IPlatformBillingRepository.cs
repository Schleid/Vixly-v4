using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformBillingRepository.cs
//
// PURPOSE:
// Loads and stores billing settings, the company profile, tax rates, number sequences and stored documents.
//
// RESPONSIBILITIES:
// - One port for the small billing records
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; the use case commits
// - Documents are written once (D-B10)
// =========================================================
public interface IPlatformBillingRepository
{
    Task<PlatformBillingSettings?> GetSettingsAsync(CancellationToken cancellationToken);

    void AddSettings(PlatformBillingSettings settings);

    Task<PlatformCompanyProfile?> GetProfileAsync(CancellationToken cancellationToken);

    void AddProfile(PlatformCompanyProfile profile);

    Task<IReadOnlyList<PlatformTaxRate>> ListTaxRatesAsync(string? countryCode, CancellationToken cancellationToken);

    void AddTaxRate(PlatformTaxRate rate);

    Task<PlatformNumberSequence?> GetSequenceAsync(PlatformBillingDocumentKind kind, int year, CancellationToken cancellationToken);

    void AddSequence(PlatformNumberSequence sequence);

    Task<PlatformBillingDocument?> GetDocumentAsync(PlatformBillingDocumentKind kind, Guid ownerId, CancellationToken cancellationToken);

    void AddDocument(PlatformBillingDocument document);
}
