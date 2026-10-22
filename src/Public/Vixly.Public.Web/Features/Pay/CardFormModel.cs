namespace Vixly.Public.Web.Features.Pay;

// =========================================================
// FILE: CardFormModel.cs
//
// PURPOSE:
// What the card part of a payment page posts.
//
// RESPONSIBILITIES:
// - Hold the save-card and automatic-charge choices
//
// IMPORTANT NOTES:
// - Platform refuses automatic charging without a saved card
// =========================================================
public sealed class CardFormModel
{
    public bool SaveCard { get; set; }

    public bool AutoCharge { get; set; }
}
