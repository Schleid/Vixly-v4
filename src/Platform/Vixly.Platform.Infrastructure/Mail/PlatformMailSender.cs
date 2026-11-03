using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using Vixly.Platform.Resources;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: PlatformMailSender.cs
//
// PURPOSE:
// Sends one email in the recipient's language: the text, and the same text designed under the Vixly header.
//
// RESPONSIBILITIES:
// - Compose the subject and body under the recipient's culture
// - Add the designed version: Vixly logo and header, the main link as a button with the link to copy,
//   the company footer (owner, 2026-10-25; Shared.Localization VixlyEmail)
// - Deliver through SMTP, or to a pickup folder in development
//
// IMPORTANT NOTES:
// - Shared by every Platform mailer
// - The button words are the email's own "email.x.button" text, else "email.layout.button"
// - Never logs addresses or bodies (S-11)
// =========================================================
public sealed class PlatformMailSender
{
    private readonly IStringLocalizer<PlatformText> _text;

    private readonly PlatformMailOptions _mail;

    public PlatformMailSender(IStringLocalizer<PlatformText> text, IOptions<PlatformMailOptions> mail)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _mail = mail?.Value ?? throw new ArgumentNullException(nameof(mail));
    }

    public string SupportAddress => string.IsNullOrWhiteSpace(_mail.SupportAddress) ? _mail.FromAddress : _mail.SupportAddress;

    public Task SendAsync(
        string toAddress,
        string toName,
        LanguageCode language,
        Func<IStringLocalizer, (string Subject, string Body)> compose,
        CancellationToken cancellationToken)
    {
        return SendAsync(toAddress, toName, language, compose, null, cancellationToken);
    }

    // Billing (Slice 4): the same mail with one PDF attached (invoice, credit note or receipt).
    public async Task SendAsync(
        string toAddress,
        string toName,
        LanguageCode language,
        Func<IStringLocalizer, (string Subject, string Body)> compose,
        (string FileName, byte[] Content)? attachment,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(compose);

        CultureInfo previous = CultureInfo.CurrentUICulture;
        (string Subject, string Body, string? Button) content;
        VixlyEmailWords words;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language.Value);
            content = VixlyEmail.Compose(_text, compose);
            words = new VixlyEmailWords(
                _text["email.layout.button"].Value,
                _text["email.layout.copy_link"].Value,
                new[] { VixlyText.Format(_text, "email.layout.footer_support", "support", SupportAddress), _text["email.layout.footer_company"].Value });
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        using MailMessage message = new()
        {
            From = new MailAddress(_mail.FromAddress, _mail.FromName)
        };
        message.To.Add(new MailAddress(toAddress, toName));
        VixlyEmail.Fill(message, content.Subject, content.Body, Brand, words, language.Value, content.Button);

        if (attachment is { } file)
        {
            message.Attachments.Add(new Attachment(new MemoryStream(file.Content), file.FileName, "application/pdf"));
        }

        using SmtpClient client = CreateClient();
        await client.SendMailAsync(message, cancellationToken);
    }

    // Vixly's own mail: its logo and colours (Platform mail never goes out under a company's name).
    private static readonly VixlyEmailBrand Brand = new("Vixly", "#2563EB", "#2563EB", Logo());

    private static byte[] Logo()
    {
        using Stream stream = typeof(PlatformMailSender).Assembly.GetManifestResourceStream("Vixly.Platform.Mail.Logo.png")
            ?? throw new InvalidOperationException("The email logo is missing from the build.");
        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private SmtpClient CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_mail.PickupDirectory))
        {
            Directory.CreateDirectory(_mail.PickupDirectory);
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = Path.GetFullPath(_mail.PickupDirectory),
                DeliveryFormat = SmtpDeliveryFormat.International
            };
        }

        // International: addresses such as lucía@example.com are valid and must not fail the message (SMTPUTF8).
        SmtpClient client = new(_mail.SmtpHost, _mail.SmtpPort) { EnableSsl = _mail.UseSsl, DeliveryFormat = SmtpDeliveryFormat.International };

        if (!string.IsNullOrEmpty(_mail.UserName))
        {
            client.Credentials = new NetworkCredential(_mail.UserName, _mail.Password);
        }

        return client;
    }
}
