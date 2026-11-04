using Vixly.Operations.Application.Ports;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Mail;

// =========================================================
// FILE: SmtpOperationsReportMailer.cs
//
// PURPOSE:
// Emails scheduled report files to staff (Slice 11).
//
// RESPONSIBILITIES:
// - Word the subject and body in the recipient’s language; attach the file
//
// IMPORTANT NOTES:
// - Never logs addresses or contents (S-11)
// =========================================================
public sealed class SmtpOperationsReportMailer : IOperationsReportMailer
{
    private readonly OperationsMailSender _sender;

    public SmtpOperationsReportMailer(OperationsMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendAsync(OperationsReportMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);

        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = mail.ToName,
            ["company"] = mail.CompanyName,
            ["report"] = mail.ReportTitle,
            ["period"] = mail.Period
        };

        return _sender.SendAsync(mail.ToAddress, mail.ToName, mail.CompanyName, mail.Language,
            text => (VixlyText.Format(text, "email.report.subject", values), VixlyText.Format(text, "email.report.body", values)), cancellationToken,
            (mail.FileName, mail.ContentType, mail.Content));
    }
}
