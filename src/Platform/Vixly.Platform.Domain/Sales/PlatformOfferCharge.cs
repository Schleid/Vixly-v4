namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformOfferCharge.cs
//
// PURPOSE:
// A one-time charge agreed in an offer link.
//
// RESPONSIBILITIES:
// - Hold a description and an amount
//
// IMPORTANT NOTES:
// - Owned by PlatformOffer; becomes a one-time charge on the subscription when the offer is used
// =========================================================
public sealed class PlatformOfferCharge
{
    private PlatformOfferCharge()
    {
        Description = string.Empty;
    }

    internal PlatformOfferCharge(string description, decimal amount)
    {
        Description = description;
        Amount = amount;
    }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }
}
