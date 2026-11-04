using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Resources;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Mail;

// =========================================================
// FILE: OperationsMailSender.cs
//
// PURPOSE:
// Sends one email in the recipient language, under the company's name: the text, and the same text designed.
//
// RESPONSIBILITIES:
// - Compose under the recipient culture
// - Add the designed version: the company's name as the header, the main link as a button with the link to
//   copy, a footer naming the company (owner, 2026-10-25; Shared.Localization VixlyEmail)
// - With the company's published website look: its logo in the header and its main colour (OperationsMailBrandReader)
// - Deliver through SMTP, or to a pickup folder in development; with a file attached when given (Slice 11)
//
// IMPORTANT NOTES:
// - White label: these emails go to a company's customers and staff; they never show Vixly
// - The button words are the email's own "email.x.button" text, else "email.layout.button"
// - Never logs addresses or bodies (S-11)
// - A colour too light for white text falls back to the plain colours
// - Scoped: the company comes from the request's or the worker's tenant scope
// =========================================================
public sealed class OperationsMailSender
{
    private readonly IStringLocalizer<OperationsText> _text;

    private const string PlainHeader = "#0F172A";

    private const string PlainButton = "#2563EB";

    private readonly OperationsMailOptions _mail;

    private readonly OperationsMailBrandReader? _brands;

    public OperationsMailSender(IStringLocalizer<OperationsText> text, IOptions<OperationsMailOptions> mail, OperationsMailBrandReader? brands = null)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _mail = mail?.Value ?? throw new ArgumentNullException(nameof(mail));
        _brands = brands;
    }

    public async Task SendAsync(string toAddress, string toName, string fromName, LanguageCode language, Func<IStringLocalizer, (string Subject, string Body)> compose, CancellationToken cancellationToken,
        (string Name, string ContentType, byte[] Content)? attachment = null)
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
                new[] { VixlyText.Format(_text, "email.layout.footer", "company", fromName) });
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        using MailMessage message = new()
        {
            From = new MailAddress(_mail.FromAddress, fromName)
        };
        message.To.Add(new MailAddress(toAddress, toName));
        VixlyEmail.Fill(message, content.Subject, content.Body, await BrandAsync(fromName, cancellationToken), words, language.Value, content.Button);

        // Slice 11: a scheduled report's file.
        if (attachment is { } file)
        {
            message.Attachments.Add(new Attachment(new MemoryStream(file.Content), file.Name, file.ContentType));
        }

        using SmtpClient client = CreateClient();
        await client.SendMailAsync(message, cancellationToken);
    }

    // The company's look when it has one; any trouble reading it keeps the plain header.
    private async Task<VixlyEmailBrand> BrandAsync(string fromName, CancellationToken cancellationToken)
    {
        (string? Colour, VixlyEmailLogo? Logo)? look = null;
        if (_brands is not null)
        {
            try
            {
                look = await _brands.ReadAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                look = null;
            }
        }

        string colour = Readable(look?.Colour) ?? PlainHeader;
        return new VixlyEmailBrand(fromName, colour, Readable(look?.Colour) ?? PlainButton, null, look?.Logo);
    }

    // White text must stay readable on the colour (WCAG AA, 4.5:1).
    public static string? Readable(string? colour)
    {
        try
        {
            return colour is null ? null : OperationsColour.Contrast(colour, OperationsColour.White) >= 4.5 ? OperationsColour.Normalize(colour) : null;
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException)
        {
            return null;
        }
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
