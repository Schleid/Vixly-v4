using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformBillingRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformBillingRepository.
//
// RESPONSIBILITIES:
// - Load and add settings, profile, tax rates, sequences and documents
//
// IMPORTANT NOTES:
// - Documents are never updated
// =========================================================
internal sealed class PlatformBillingRepository : IPlatformBillingRepository
{
    private readonly PlatformDbContext _context;

    public PlatformBillingRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Unsaved rows of this unit of work come first, so one commit never sees two settings rows.
    public async Task<PlatformBillingSettings?> GetSettingsAsync(CancellationToken cancellationToken)
    {
        return _context.BillingSettings.Local.FirstOrDefault(item => item.Key == PlatformBillingSettings.SingletonKey)
            ?? await _context.BillingSettings.FirstOrDefaultAsync(item => item.Key == PlatformBillingSettings.SingletonKey, cancellationToken);
    }

    public void AddSettings(PlatformBillingSettings settings)
    {
        _context.BillingSettings.Add(settings ?? throw new ArgumentNullException(nameof(settings)));
    }

    public Task<PlatformCompanyProfile?> GetProfileAsync(CancellationToken cancellationToken)
    {
        return _context.CompanyProfiles.FirstOrDefaultAsync(item => item.Key == PlatformCompanyProfile.SingletonKey, cancellationToken);
    }

    public void AddProfile(PlatformCompanyProfile profile)
    {
        _context.CompanyProfiles.Add(profile ?? throw new ArgumentNullException(nameof(profile)));
    }

    public async Task<IReadOnlyList<PlatformTaxRate>> ListTaxRatesAsync(string? countryCode, CancellationToken cancellationToken)
    {
        IQueryable<PlatformTaxRate> rates = _context.TaxRates.AsNoTracking();

        if (countryCode is not null)
        {
            rates = rates.Where(item => item.CountryCode == countryCode);
        }

        return await rates.OrderBy(item => item.CountryCode).ThenByDescending(item => item.ValidFromUtc).Take(500).ToListAsync(cancellationToken);
    }

    public void AddTaxRate(PlatformTaxRate rate)
    {
        _context.TaxRates.Add(rate ?? throw new ArgumentNullException(nameof(rate)));
    }

    // A sequence started in this unit of work is not in the database yet; find it first so one commit
    // taking two numbers of a new kind and year continues the same sequence (D-B2).
    public async Task<PlatformNumberSequence?> GetSequenceAsync(PlatformBillingDocumentKind kind, int year, CancellationToken cancellationToken)
    {
        return _context.NumberSequences.Local.FirstOrDefault(item => item.Kind == kind && item.Year == year)
            ?? await _context.NumberSequences.FirstOrDefaultAsync(item => item.Kind == kind && item.Year == year, cancellationToken);
    }

    public void AddSequence(PlatformNumberSequence sequence)
    {
        _context.NumberSequences.Add(sequence ?? throw new ArgumentNullException(nameof(sequence)));
    }

    public Task<PlatformBillingDocument?> GetDocumentAsync(PlatformBillingDocumentKind kind, Guid ownerId, CancellationToken cancellationToken)
    {
        return _context.BillingDocuments.AsNoTracking().FirstOrDefaultAsync(item => item.Kind == kind && item.OwnerId == ownerId, cancellationToken);
    }

    public void AddDocument(PlatformBillingDocument document)
    {
        _context.BillingDocuments.Add(document ?? throw new ArgumentNullException(nameof(document)));
    }
}
