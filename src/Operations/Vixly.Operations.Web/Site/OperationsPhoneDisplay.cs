namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsPhoneDisplay.cs
//
// PURPOSE:
// Shows a stored international phone number the way people read it.
//
// RESPONSIBILITIES:
// - Show it in international form (Shared.Localization VixlyPhone, owner 2026-10-25)
//
// IMPORTANT NOTES:
// - Display only: links (tel:) always use the stored number
// - Kept so existing pages read the same; new code may call VixlyPhone directly
// =========================================================
public static class OperationsPhoneDisplay
{
    public static string Format(string? phone) => Vixly.Shared.Localization.VixlyPhone.International(phone);
}
