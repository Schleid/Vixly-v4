namespace Vixly.Public.Web.Features.Pay;

// =========================================================
// FILE: ZelleFormModel.cs
//
// PURPOSE:
// What the "I paid with Zelle" part of a payment page posts.
//
// RESPONSIBILITIES:
// - Hold the amount, date, sender and confirmation code
//
// IMPORTANT NOTES:
// - Staff check the money in the bank before it counts (Zelle update)
// =========================================================
public sealed class ZelleFormModel
{
    public decimal? Amount { get; set; }

    public DateTime? SentOn { get; set; }

    public string? SenderName { get; set; }

    public string? Reference { get; set; }
}
