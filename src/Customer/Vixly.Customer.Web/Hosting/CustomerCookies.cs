namespace Vixly.Customer.Web.Hosting;

// =========================================================
// FILE: CustomerCookies.cs
//
// PURPOSE:
// Names of the cookies Customer.Web writes (S-4, Slice 0 D-F5).
//
// RESPONSIBILITIES:
// - Name every cookie in one place
//
// IMPORTANT NOTES:
// - __Secure- with Path=/customer: never sent to staff pages
// - Customer.Web never reads a staff cookie (__Host-vx4-ops, __Host-vx4-device)
// =========================================================
public static class CustomerCookies
{
    public const string Path = "/customer";

    public const string Session = "__Secure-vx4-customer";

    public const string Antiforgery = "__Secure-vx4-customer-af";

    public const string Culture = "__Secure-vx4-customer-lang";
}
