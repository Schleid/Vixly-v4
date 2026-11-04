using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.SalesFeed;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSalesFeedMailer.cs
//
// PURPOSE:
// Emails staff when the sales feed to TechOS needs a person.
//
// RESPONSIBILITIES:
// - Send one alert: events parked, sending stopped, or no answer for 24 hours
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; links to the Control sales feed page
// - Staff email, in the recipient's language
// =========================================================
public interface IPlatformSalesFeedMailer
{
    Task SendAlertAsync(PlatformPermissionHolderItem recipient, PlatformSalesFeedAlert alert, CancellationToken cancellationToken);
}
