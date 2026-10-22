namespace Vixly.Public.Web.Features.Contact;

// =========================================================
// FILE: ContactFormModel.cs
//
// PURPOSE:
// What the contact form posts.
//
// RESPONSIBILITIES:
// - Hold the visitor's entries between post and redisplay
//
// IMPORTANT NOTES:
// - Nothing here is trusted: Platform checks every field
// =========================================================
public sealed class ContactFormModel
{
    public string? CompanyName { get; set; }

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Kind { get; set; } = "courier";

    public string? Message { get; set; }
}
