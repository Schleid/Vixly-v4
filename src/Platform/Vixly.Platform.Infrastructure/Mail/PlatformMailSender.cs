using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using Vixly.Platform.Resources;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: PlatformMailSender.cs
//
// PURPOSE:
// Sends one plain-text email in the recipient's language.
//
// RESPONSIBILITIES:
// - Compose the subject and body under the recipient's culture
// - Deliver through SMTP, or to a pickup folder in development
//
// IMPORTANT NOTES:
// - Shared by the identity and tenant mailers
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

    public async Task SendAsync(
        string toAddress,
        string toName,
        LanguageCode language,
        Func<IStringLocalizer, (string Subject, string Body)> compose,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(compose);

        CultureInfo previous = CultureInfo.CurrentUICulture;
        (string Subject, string Body) content;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language.Value);
            content = compose(_text);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        using MailMessage message = new()
        {
            From = new MailAddress(_mail.FromAddress, _mail.FromName),
            Subject = content.Subject,
            Body = content.Body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(toAddress, toName));

        using SmtpClient client = CreateClient();
        await client.SendMailAsync(message, cancellationToken);
    }

    private SmtpClient CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_mail.PickupDirectory))
        {
            Directory.CreateDirectory(_mail.PickupDirectory);
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = Path.GetFullPath(_mail.PickupDirectory)
            };
        }

        SmtpClient client = new(_mail.SmtpHost, _mail.SmtpPort) { EnableSsl = _mail.UseSsl };

        if (!string.IsNullOrEmpty(_mail.UserName))
        {
            client.Credentials = new NetworkCredential(_mail.UserName, _mail.Password);
        }

        return client;
    }
}
