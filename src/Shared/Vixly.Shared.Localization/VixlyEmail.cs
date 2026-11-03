using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyEmail.cs
//
// PURPOSE:
// Turns a plain-text email into a designed HTML email (header with the sender's name and logo, a button for
// the main link, the link again to copy, a footer), sent together with the plain text (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Find the main link: the first line that is only a link ("{link}"), or a short "Label: link" line
//   ("Pay online: ..."), and show it as a button with the link to copy below it
// - Keep every other line as written: paragraphs, "- " lists, links and email addresses made clickable
// - Build the message: plain text and HTML alternatives, the logo embedded (cid), never fetched from a server
// - Record which text keys an email used, so the sender can find the button words ("email.x.body" -> "email.x.button")
//
// IMPORTANT NOTES:
// - The texts stay plain text in the resources: one source for both versions, and nothing to escape by hand
// - Everything is HTML-encoded; only the layout's own tags are written
// - Table layout and inline styles: the only form every mail program (Outlook included) shows the same
// - The brand comes from the sender: Platform mail is Vixly's, Operations mail is the company's (never Vixly)
// =========================================================
public static class VixlyEmail
{
    public const string LogoContentId = "brand-logo";

    private const string Font = "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    private static readonly Regex LoneUrl = new(@"^https?://\S+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LabelledUrl = new(@"^(?<label>[^:\-][^:]{0,39}):\s*(?<url>https?://\S+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Linkable = new(@"https?://[^\s<>""]+|[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // The button key of an email: "email.invitation.body" -> "email.invitation.button", "..._body" -> "..._button".
    public static string? ButtonKey(IEnumerable<string> usedKeys)
    {
        ArgumentNullException.ThrowIfNull(usedKeys);
        string? body = usedKeys.LastOrDefault(key => key.EndsWith(".body", StringComparison.Ordinal) || key.EndsWith("_body", StringComparison.Ordinal));
        return body is null ? null : body[..^4] + "button";
    }

    public static VixlyEmailAction? FindAction(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        foreach (string raw in body.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw.Trim();

            if (LoneUrl.IsMatch(line))
            {
                return new VixlyEmailAction(line, null);
            }

            if (LabelledUrl.Match(line) is { Success: true } labelled && !labelled.Groups["label"].Value.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                return new VixlyEmailAction(labelled.Groups["url"].Value, labelled.Groups["label"].Value.Trim());
            }
        }

        return null;
    }

    public static string Html(string subject, string body, VixlyEmailBrand brand, VixlyEmailWords words, string language, string? buttonLabel)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(brand);
        ArgumentNullException.ThrowIfNull(words);

        string text = body.Replace("\r\n", "\n").Trim();
        VixlyEmailAction? action = FindAction(text);
        StringBuilder content = new();
        bool buttonDone = false;

        foreach (string block in Regex.Split(text, @"\n\s*\n"))
        {
            List<string> lines = block.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToList();
            List<string> pending = new();

            foreach (string line in lines)
            {
                if (!buttonDone && action is not null && IsActionLine(line, action))
                {
                    Paragraph(content, pending, brand);
                    pending.Clear();
                    Button(content, action, action.Label ?? buttonLabel ?? words.Button, words, brand);
                    buttonDone = true;
                    continue;
                }

                pending.Add(line);
            }

            Paragraph(content, pending, brand);
        }

        string preheader = Preheader(text, action);
        StringBuilder html = new();
        html.Append("<!DOCTYPE html><html lang=\"").Append(Encode(language)).Append("\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta name=\"color-scheme\" content=\"light\">")
            .Append("<meta name=\"supported-color-schemes\" content=\"light\"><title>").Append(Encode(subject)).Append("</title></head>")
            .Append("<body style=\"margin:0;padding:0;background-color:#F1F5F9;-webkit-text-size-adjust:100%;\">")
            .Append("<div style=\"display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;\">").Append(Encode(preheader)).Append("</div>")
            .Append("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"background-color:#F1F5F9;\"><tr><td align=\"center\" style=\"padding:32px 12px;\">")
            .Append("<table role=\"presentation\" width=\"600\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"width:100%;max-width:600px;\">");

        // Header: a company's own wide logo on white under a band of its colour; else logo and name on the brand colour.
        if (brand.WideLogo is { } wide)
        {
            html.Append("<tr><td style=\"background-color:#FFFFFF;border-top:6px solid ").Append(brand.HeaderColor)
                .Append(";border-left:1px solid #E2E8F0;border-right:1px solid #E2E8F0;border-bottom:1px solid #E2E8F0;border-radius:14px 14px 0 0;padding:20px 32px;\">")
                .Append("<img src=\"cid:").Append(LogoContentId).Append("\" width=\"").Append(wide.HeaderWidth).Append("\" height=\"").Append(wide.ShownHeight)
                .Append("\" alt=\"").Append(Encode(brand.Name)).Append("\" style=\"display:block;border:0;width:").Append(wide.HeaderWidth).Append("px;height:").Append(wide.ShownHeight)
                .Append("px;font-family:").Append(Font).Append(";font-size:20px;font-weight:700;color:#0F172A;\"></td></tr>");
        }
        else
        {
            html.Append("<tr><td style=\"background-color:").Append(brand.HeaderColor).Append(";border-radius:14px 14px 0 0;padding:22px 32px;\">")
                .Append("<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\"><tr>");

            if (brand.LogoPng is not null)
            {
                html.Append("<td style=\"padding-right:12px;vertical-align:middle;\"><div style=\"background-color:#FFFFFF;border-radius:12px;padding:3px;\"><img src=\"cid:").Append(LogoContentId)
                    .Append("\" width=\"40\" height=\"40\" alt=\"\" style=\"display:block;width:40px;height:40px;border:0;border-radius:9px;\"></div></td>");
            }

            html.Append("<td style=\"vertical-align:middle;font-family:").Append(Font).Append(";font-size:22px;font-weight:700;letter-spacing:-0.01em;color:#FFFFFF;\">")
                .Append(Encode(brand.Name)).Append("</td></tr></table></td></tr>");
        }

        // Body.
        html.Append("<tr><td style=\"background-color:#FFFFFF;padding:32px 32px 16px;font-family:").Append(Font)
            .Append(";font-size:16px;line-height:1.6;color:#0F172A;border:1px solid #E2E8F0;border-top:0;border-radius:0 0 14px 14px;\">")
            .Append(content).Append("</td></tr>");

        // Footer, outside the card.
        html.Append("<tr><td align=\"center\" style=\"padding:20px 24px 0;font-family:").Append(Font).Append(";font-size:12px;line-height:1.6;color:#64748B;\">");

        foreach (string line in words.Footer.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            html.Append("<p style=\"margin:0 0 4px;\">").Append(Linkify(line, "#64748B")).Append("</p>");
        }

        html.Append("</td></tr></table></td></tr></table></body></html>");
        return html.ToString();
    }

    // The message: plain text first, the designed version second (mail programs show the last one they can).
    public static void Fill(MailMessage message, string subject, string body, VixlyEmailBrand brand, VixlyEmailWords words, string language, string? buttonLabel)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(brand);

        message.Subject = subject;
        message.SubjectEncoding = Encoding.UTF8;
        message.BodyEncoding = Encoding.UTF8;
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, MediaTypeNames.Text.Plain));

        AlternateView html = AlternateView.CreateAlternateViewFromString(Html(subject, body, brand, words, language, buttonLabel), Encoding.UTF8, MediaTypeNames.Text.Html);

        if (brand.WideLogo is { } wide)
        {
            html.LinkedResources.Add(new LinkedResource(new MemoryStream(wide.Content), wide.ContentType) { ContentId = LogoContentId, TransferEncoding = TransferEncoding.Base64 });
        }
        else if (brand.LogoPng is { } logo)
        {
            html.LinkedResources.Add(new LinkedResource(new MemoryStream(logo), "image/png") { ContentId = LogoContentId, TransferEncoding = TransferEncoding.Base64 });
        }

        message.AlternateViews.Add(html);
    }

    // Compose under the recipient's culture, recording the keys used (for the button words).
    public static (string Subject, string Body, string? Button) Compose(IStringLocalizer text, Func<IStringLocalizer, (string Subject, string Body)> compose)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(compose);

        RecordingLocalizer recording = new(text);
        (string subject, string body) = compose(recording);
        string? key = ButtonKey(recording.Keys);
        LocalizedString? button = key is null ? null : text[key];
        return (subject, body, button is { ResourceNotFound: false } ? button.Value : null);
    }

    private static bool IsActionLine(string line, VixlyEmailAction action)
    {
        string trimmed = line.Trim();
        return trimmed == action.Url || (action.Label is not null && trimmed.EndsWith(action.Url, StringComparison.Ordinal) && trimmed.StartsWith(action.Label, StringComparison.Ordinal));
    }

    private static void Paragraph(StringBuilder content, List<string> lines, VixlyEmailBrand brand)
    {
        if (lines.Count == 0)
        {
            return;
        }

        if (lines.All(line => line.StartsWith("- ", StringComparison.Ordinal)))
        {
            content.Append("<ul style=\"margin:0 0 16px;padding-left:20px;\">");

            foreach (string line in lines)
            {
                content.Append("<li style=\"margin:0 0 6px;\">").Append(Linkify(line[2..], brand.ButtonColor)).Append("</li>");
            }

            content.Append("</ul>");
            return;
        }

        content.Append("<p style=\"margin:0 0 16px;\">").Append(string.Join("<br>", lines.Select(line => Linkify(line, brand.ButtonColor)))).Append("</p>");
    }

    private static void Button(StringBuilder content, VixlyEmailAction action, string label, VixlyEmailWords words, VixlyEmailBrand brand)
    {
        string url = Encode(action.Url);

        content.Append("<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"margin:8px 0 24px;\"><tr>")
            .Append("<td align=\"center\" bgcolor=\"").Append(brand.ButtonColor).Append("\" style=\"border-radius:10px;background-color:").Append(brand.ButtonColor).Append(";\">")
            .Append("<a href=\"").Append(url).Append("\" target=\"_blank\" style=\"display:inline-block;padding:14px 28px;font-family:").Append(Font)
            .Append(";font-size:16px;font-weight:600;line-height:1.2;color:#FFFFFF;text-decoration:none;border-radius:10px;\">")
            .Append(Encode(label)).Append("</a></td></tr></table>")
            .Append("<p style=\"margin:0 0 4px;font-size:13px;line-height:1.5;color:#64748B;\">").Append(Encode(words.CopyLink)).Append("</p>")
            .Append("<p style=\"margin:0 0 20px;font-size:13px;line-height:1.5;word-break:break-all;\"><a href=\"").Append(url)
            .Append("\" target=\"_blank\" style=\"color:").Append(brand.ButtonColor).Append(";text-decoration:underline;\">").Append(url).Append("</a></p>");
    }

    private static string Linkify(string line, string color)
    {
        StringBuilder html = new();
        int at = 0;

        foreach (Match match in Linkable.Matches(line))
        {
            html.Append(Encode(line[at..match.Index]));
            string value = match.Value.TrimEnd('.', ',', ';', ')');
            string href = value.Contains('@', StringComparison.Ordinal) && !value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? "mailto:" + value : value;
            html.Append("<a href=\"").Append(Encode(href)).Append("\" style=\"color:").Append(color).Append(";\">").Append(Encode(value)).Append("</a>");
            at = match.Index + value.Length;
        }

        html.Append(Encode(line[at..]));
        return html.ToString();
    }

    private static string Preheader(string text, VixlyEmailAction? action)
    {
        string? first = Regex.Split(text, @"\n\s*\n").Skip(1).FirstOrDefault(block => action is null || !block.Contains(action.Url, StringComparison.Ordinal))
            ?? Regex.Split(text, @"\n\s*\n").FirstOrDefault();
        string flat = Regex.Replace(first ?? string.Empty, @"\s+", " ").Trim();
        return flat.Length > 120 ? flat[..117] + "..." : flat;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    // Passes every lookup through and remembers the keys, in order.
    private sealed class RecordingLocalizer : IStringLocalizer
    {
        private readonly IStringLocalizer _inner;

        public RecordingLocalizer(IStringLocalizer inner)
        {
            _inner = inner;
        }

        public List<string> Keys { get; } = new();

        public LocalizedString this[string name]
        {
            get
            {
                Keys.Add(name);
                return _inner[name];
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                Keys.Add(name);
                return _inner[name, arguments];
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _inner.GetAllStrings(includeParentCultures);
    }
}
