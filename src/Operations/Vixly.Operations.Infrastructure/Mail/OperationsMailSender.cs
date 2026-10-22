using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vixly.Operations.Resources;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Infrastructure.Mail;

// =========================================================
// FILE: OperationsMailSender.cs
//
// PURPOSE:
// Sends one plain-text email in the recipient language, under the tenant name.
//
// RESPONSIBILITIES:
// - Compose under the recipient culture
// - Deliver through SMTP, or to a pickup folder in development
//
// IMPORTANT NOTES:
// - Never logs addresses or bodies (S-11)
// =========================================================
public sealed class OperationsMailSender
{
    private readonly IStringLocalizer<OperationsText> _text;

    private readonly OperationsMailOptions _mail;

    public OperationsMailSender(IStringLocalizer<OperationsText> text, IOptions<OperationsMailOptions> mail)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _mail = mail?.Value ?? throw new ArgumentNullException(nameof(mail));
    }

    public async Task SendAsync(string toAddress, string toName, string fromName, LanguageCode language, Func<IStringLocalizer, (string Subject, string Body)> compose, CancellationToken cancellationToken)
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
            From = new MailAddress(_mail.FromAddress, fromName),
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
                PickupDirectoryLocation = Path.GetFullPath(_mail.PickupDirectory),
                DeliveryFormat = SmtpDeliveryFormat.International
            };
        }

        SmtpClient client = new(_mail.SmtpHost, _mail.SmtpPort) { EnableSsl = _mail.UseSsl, DeliveryFormat = SmtpDeliveryFormat.International };

        if (!string.IsNullOrEmpty(_mail.UserName))
        {
            client.Credentials = new NetworkCredential(_mail.UserName, _mail.Password);
        }

        return client;
    }
}
