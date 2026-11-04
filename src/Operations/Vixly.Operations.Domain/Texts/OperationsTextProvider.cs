using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Texts;

// =========================================================
// FILE: OperationsTextProvider.cs
//
// PURPOSE:
// The company’s own Twilio account for SMS and WhatsApp (Slice 9; owner, 2026-10-22: each company uses its own account and pays Twilio directly).
//
// RESPONSIBILITIES:
// - Keep the account id, the protected auth token (only its last characters shown), the SMS sender, the WhatsApp sender and its approved templates
// - Check their shape before Twilio is asked
//
// IMPORTANT NOTES:
// - The token is protected with the server’s key and never shown or logged
// - Messages come from the company’s own number
// =========================================================
public sealed partial class OperationsTextProvider : OperationsEntity
{
    public const int NameMaxLength = 120;

    public const int CallbackUrlMaxLength = 300;

    private readonly List<OperationsTextTemplate> _templates = new();

    private OperationsTextProvider()
    {
        AccountSid = string.Empty;
        AuthTokenProtected = string.Empty;
        TokenHint = string.Empty;
    }

    private OperationsTextProvider(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        AccountSid = string.Empty;
        AuthTokenProtected = string.Empty;
        TokenHint = string.Empty;
    }

    public string AccountSid { get; private set; }

    public string AuthTokenProtected { get; private set; }

    public string TokenHint { get; private set; }

    public string? AccountName { get; private set; }

    // A phone number (E.164) or a messaging service (MG...); none: no SMS.
    public string? SmsFrom { get; private set; }

    // The WhatsApp sender's number (E.164); none: no WhatsApp.
    public string? WhatsAppFrom { get; private set; }

    public IReadOnlyList<OperationsTextTemplate> Templates => _templates;

    public Guid ConnectedByUserId { get; private set; }

    // Where Twilio reports what became of each message (the company's own address, as at connection): signed with the token.
    public string? CallbackUrl { get; private set; }

    public bool SendsSms => SmsFrom is not null;

    public bool SendsWhatsApp => WhatsAppFrom is not null;

    public string? Template(OperationsTextNotices notice) => _templates.FirstOrDefault(template => template.Notice == notice)?.ContentSid;

    public static bool KeysLookRight(string? accountSid, string? authToken) =>
        accountSid is not null && AccountPattern().IsMatch(accountSid) && authToken is not null && TokenPattern().IsMatch(authToken);

    public static OperationsTextProvider Connect(Guid tenantId, string accountSid, string authToken, string authTokenProtected, string? accountName, string? smsFrom, string? whatsAppFrom,
        Guid by, DateTimeOffset utcNow)
    {
        OperationsTextProvider provider = new(tenantId, utcNow);
        provider.Change(accountSid, authToken, authTokenProtected, accountName, smsFrom, whatsAppFrom, by, utcNow);
        return provider;
    }

    // A new token or account; the senders checked and kept.
    public void Change(string accountSid, string authToken, string authTokenProtected, string? accountName, string? smsFrom, string? whatsAppFrom, Guid by, DateTimeOffset utcNow)
    {
        if (!KeysLookRight(accountSid, authToken) || string.IsNullOrWhiteSpace(authTokenProtected))
        {
            throw new DomainException(OperationsTextErrors.KeysInvalid);
        }

        // Another account (review): its templates are not this one's.
        if (AccountSid.Length > 0 && AccountSid != accountSid)
        {
            _templates.Clear();
        }

        (AccountSid, AuthTokenProtected, TokenHint) = (accountSid, authTokenProtected, authToken[^4..]);
        AccountName = accountName is { Length: > 0 } name ? name.Trim()[..Math.Min(name.Trim().Length, NameMaxLength)] : null;
        Senders(smsFrom, whatsAppFrom, utcNow);
        ConnectedByUserId = by;
    }

    public void ReportTo(string? callbackUrl, DateTimeOffset utcNow)
    {
        CallbackUrl = callbackUrl is { Length: > 0 and <= CallbackUrlMaxLength } url && url.StartsWith("https://", StringComparison.Ordinal) ? url : null;
        MarkAsModified(utcNow);
    }

    public void Senders(string? smsFrom, string? whatsAppFrom, DateTimeOffset utcNow)
    {
        string? sms = Clean(smsFrom);
        string? whatsApp = Clean(whatsAppFrom);

        if ((sms is not null && !PhonePattern().IsMatch(sms) && !ServicePattern().IsMatch(sms)) || (whatsApp is not null && !PhonePattern().IsMatch(whatsApp)))
        {
            throw new DomainException(OperationsTextErrors.SenderInvalid);
        }

        (SmsFrom, WhatsAppFrom) = (sms, whatsApp);
        MarkAsModified(utcNow);
    }

    public void ChangeTemplates(IReadOnlyDictionary<OperationsTextNotices, string?> templates, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(templates);
        List<OperationsTextTemplate> kept = new();

        foreach ((OperationsTextNotices notice, string? sid) in templates)
        {
            if (Clean(sid) is not { } contentSid)
            {
                continue;
            }

            if (!Enum.IsDefined(notice) || notice == OperationsTextNotices.None || !TemplatePattern().IsMatch(contentSid))
            {
                throw new DomainException(OperationsTextErrors.TemplateInvalid);
            }

            kept.Add(new OperationsTextTemplate(notice, contentSid));
        }

        _templates.Clear();
        _templates.AddRange(kept);
        MarkAsModified(utcNow);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);

    [GeneratedRegex("^AC[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex AccountPattern();

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex("^MG[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex ServicePattern();

    [GeneratedRegex("^HX[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex TemplatePattern();
}
