using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLeadMailer.cs
//
// PURPOSE:
// Tells sales staff about a new contact request (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Email one staff member about one lead
//
// IMPORTANT NOTES:
// - Never emails the visitor, so the form cannot send mail to strangers
// =========================================================
public interface IPlatformLeadMailer
{
    Task SendLeadReceivedAsync(PlatformPermissionHolderItem recipient, PlatformSalesLead lead, CancellationToken cancellationToken);
}
