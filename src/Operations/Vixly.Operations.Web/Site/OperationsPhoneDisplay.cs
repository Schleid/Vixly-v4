namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsPhoneDisplay.cs
//
// PURPOSE:
// Shows a stored international phone number the way people read it.
//
// RESPONSIBILITIES:
// - Group North American (+1) and Haitian (+509) numbers
// - Leave other numbers as stored
//
// IMPORTANT NOTES:
// - Display only: links (tel:) always use the stored number
// =========================================================
public static class OperationsPhoneDisplay
{
    public static string Format(string phone)
    {
        ArgumentNullException.ThrowIfNull(phone);

        if (phone.Length == 12 && phone.StartsWith("+1", StringComparison.Ordinal))
        {
            return $"+1 {phone[2..5]}-{phone[5..8]}-{phone[8..]}";
        }

        if (phone.Length == 12 && phone.StartsWith("+509", StringComparison.Ordinal))
        {
            return $"+509 {phone[4..8]}-{phone[8..]}";
        }

        return phone;
    }
}
