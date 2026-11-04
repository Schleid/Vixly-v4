using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tickets;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Plans;

// =========================================================
// FILE: OperationsShipmentCapHandler.cs
//
// PURPOSE:
// Emails the Owner a shipment-cap warning (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Load the Owner and the company's name; send the warning in the Owner's language with the link to the plan page
//
// IMPORTANT NOTES:
// - The link opens the company's billing page in the workspace
// =========================================================
public sealed class OperationsShipmentCapHandler : IOperationsOutboxHandler
{
    private readonly OperationsTicketPeople _people;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsIdentityMailer _mailer;

    private readonly IOperationsPlatformClient _platform;

    private readonly IOperationsPortalLinks _links;

    public OperationsShipmentCapHandler(OperationsTicketPeople people, IOperationsBrandRepository brands, IOperationsIdentityMailer mailer, IOperationsPlatformClient platform,
        IOperationsPortalLinks links)
    {
        _people = people ?? throw new ArgumentNullException(nameof(people));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    public string MessageType => OperationsShipmentCapNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        OperationsShipmentCapNotice notice = OperationsOutboxWriter.Read<OperationsShipmentCapNotice>(message);

        if (message.TenantId is not Guid tenantId || await _platform.GetTenantAsync(tenantId, cancellationToken) is not { Slug.Length: > 0 } account)
        {
            return;
        }

        string company = (await _brands.FindDisplayNameAsync(cancellationToken)) ?? string.Empty;
        string price = notice.ExtraShipmentPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " " + (notice.CurrencyCode ?? string.Empty);
        string link = _links.Page(account.Slug, "/staff/billing");

        foreach (OperationsUser owner in await _people.OwnersAsync(cancellationToken))
        {
            await _mailer.SendShipmentCapAsync(new OperationsShipmentCapMail(owner.Email.Value, owner.FullName, owner.PreferredLanguage, company, notice.Month, notice.Level,
                notice.Used, notice.Cap, notice.BlockAt, price.Trim(), notice.GraceMonth, link), cancellationToken);
        }
    }
}
