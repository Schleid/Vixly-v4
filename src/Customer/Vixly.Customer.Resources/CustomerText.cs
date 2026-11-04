namespace Vixly.Customer.Resources;

// =========================================================
// FILE: CustomerText.cs
//
// PURPOSE:
// Marker type for the Customer plane texts (CustomerText.resx in en, fr and es).
//
// RESPONSIBILITIES:
// - Anchor IStringLocalizer<CustomerText>
//
// IMPORTANT NOTES:
// - No code beyond the marker (Global Standard 2)
// - Customer-facing texts never say "Vixly"; they use {brandName} (ADR-0012 W-1)
// =========================================================
public sealed class CustomerText
{
    private CustomerText()
    {
    }
}
