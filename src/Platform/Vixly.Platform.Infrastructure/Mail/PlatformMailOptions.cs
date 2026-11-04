namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: PlatformMailOptions.cs
//
// PURPOSE:
// How the Platform sends email (configuration section
// PlatformMail).
//
// RESPONSIBILITIES:
// - Sender identity, SMTP server, credentials, and an
//   optional pickup directory for development
//
// IMPORTANT NOTES:
// - The password comes from the environment
//   (PlatformMail__Password), never from a file (S-11)
// - When PickupDirectory is set, emails are written there as
//   .eml files instead of being sent: development only
// - Every plane sends from noreply@vixly.ltd (staging: noreply.staging@vixly.ltd, its own
//   account for the provider's sending cap) through V3's mail server (owner, 2026-10-20);
//   white-label companies will send from their own address later
// =========================================================
public sealed class PlatformMailOptions
{
    public const string SectionName = "PlatformMail";

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Vixly Control";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? PickupDirectory { get; set; }

    public string? SupportAddress { get; set; }
}
