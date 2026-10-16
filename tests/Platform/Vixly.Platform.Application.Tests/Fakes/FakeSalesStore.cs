using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeSalesStore.cs
//
// PURPOSE:
// In-memory Sales settings, promotions, offers, public links and sales mail for use-case tests.
//
// RESPONSIBILITIES:
// - Keep rows in lists
// - Record the emails it was asked to send
//
// IMPORTANT NOTES:
// - Test double; no database
// =========================================================
internal sealed class FakeSalesStore :
    IPlatformSalesSettingsRepository,
    IPlatformPromotionRepository,
    IPlatformOfferRepository,
    IPlatformPublicLinks,
    IPlatformSalesMailer
{
    public PlatformSalesSettings? Settings { get; set; }

    public List<PlatformPromotion> Promotions { get; } = new();

    public List<PlatformOffer> Offers { get; } = new();

    public List<(string To, string Kind, string Subject)> ApprovalMails { get; } = new();

    public List<(string To, string Plan, DateTimeOffset Ends)> EndingMails { get; } = new();

    public Task<PlatformSalesSettings?> GetAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Settings);
    }

    public void Add(PlatformSalesSettings settings)
    {
        Settings = settings;
    }

    public Task<PlatformPromotion?> GetAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Promotions.FirstOrDefault(item => item.Id == promotionId));
    }

    public Task<PlatformPromotion?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return Task.FromResult(Promotions.FirstOrDefault(item => item.Code == code));
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return Task.FromResult(Promotions.Any(item => item.Code == code));
    }

    public void Add(PlatformPromotion promotion)
    {
        Promotions.Add(promotion);
    }

    Task<PlatformOffer?> IPlatformOfferRepository.GetAsync(Guid offerId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Offers.FirstOrDefault(item => item.Id == offerId));
    }

    public Task<PlatformOffer?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Offers.FirstOrDefault(item => item.TokenHash == tokenHash));
    }

    public void Add(PlatformOffer offer)
    {
        Offers.Add(offer);
    }

    public string OfferLink(string token)
    {
        return "https://vixly.test/register?offer=" + token;
    }

    public string PayLink(string token) => "https://vixly.test/pay/" + token;

    public string SaveCardLink(string token) => "https://vixly.test/cards/" + token;

    public string RegistrationPayment() => "https://vixly.test/register/payment";

    public string DeviceLink(string portalSlug, Vixly.Platform.Domain.Licensing.PlatformDeviceLinkKind kind, string token) => $"https://{portalSlug}.vixly.test/devices/{kind}#{token}";

    public string LegalLink(string portalSlug) => $"https://{portalSlug}.vixly.test/legal";

    public Task SendApprovalRequestAsync(PlatformPermissionHolderItem recipient, string kindKey, string subject, CancellationToken cancellationToken)
    {
        ApprovalMails.Add((recipient.Email, kindKey, subject));
        return Task.CompletedTask;
    }

    public Task SendDiscountEndingAsync(PlatformTenant tenant, PlatformTenantContact recipient, string planName, DateTimeOffset endsUtc, decimal nextPeriodAmount, string currencyCode, CancellationToken cancellationToken)
    {
        EndingMails.Add((recipient.Email.Value, planName, endsUtc));
        return Task.CompletedTask;
    }
}
