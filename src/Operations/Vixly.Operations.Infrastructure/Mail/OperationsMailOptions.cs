namespace Vixly.Operations.Infrastructure.Mail;

// =========================================================
// FILE: OperationsMailOptions.cs
//
// PURPOSE:
// Outgoing mail settings for Operations (section OperationsMail).
//
// RESPONSIBILITIES:
// - Hold the sender, SMTP server or development pickup folder
//
// IMPORTANT NOTES:
// - The password is a secret: host environment only (S-11)
// - The sender display name is the tenant name, set per message
// =========================================================
public sealed class OperationsMailOptions
{
    public const string SectionName = "OperationsMail";

    public string FromAddress { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? PickupDirectory { get; set; }
}
