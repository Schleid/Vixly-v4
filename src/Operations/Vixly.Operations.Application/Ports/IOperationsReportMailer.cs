namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsReportMailer.cs
//
// PURPOSE:
// Emails a scheduled report file to a staff member (Slice 11).
//
// RESPONSIBILITIES:
// - Send the file attached, with a subject and a short body in the recipient’s language
//
// IMPORTANT NOTES:
// - From the platform’s sending address, under the company’s name
// =========================================================
public interface IOperationsReportMailer
{
    Task SendAsync(OperationsReportMail mail, CancellationToken cancellationToken);
}
