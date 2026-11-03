using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsDeviceBlockedHandler.cs
//
// PURPOSE:
// Emails every administrator of the company that a device is blocked and needs their attention (owner, R-6).
//
// RESPONSIBILITIES:
// - Load the block, the company's active administrators and its name
// - Send each one the device, the person, the countries and the link to the device's page, in their language
//
// IMPORTANT NOTES:
// - Administrators are the Owner and the holders of the Administrator role, the only ones who may lift a block
// - A block already lifted when the message runs sends nothing
// =========================================================
public sealed class OperationsDeviceBlockedHandler : IOperationsOutboxHandler
{
    private readonly IOperationsDeviceBlockRepository _blocks;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsIdentityMailer _mailer;

    private readonly IOperationsPortalLinks _links;

    public OperationsDeviceBlockedHandler(IOperationsDeviceBlockRepository blocks, IOperationsUserRepository users, IOperationsBrandRepository brands,
        IOperationsIdentityMailer mailer, IOperationsPortalLinks links)
    {
        _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    public string MessageType => OperationsDeviceBlockedNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        OperationsDeviceBlockedNotice notice = OperationsOutboxWriter.Read<OperationsDeviceBlockedNotice>(message);

        if (await _blocks.GetAsync(notice.BlockId, cancellationToken) is not { IsOpen: true } block || string.IsNullOrWhiteSpace(notice.PortalSlug))
        {
            return;
        }

        string company = (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty;
        string person = block.UserId is { } userId && await _users.GetAsync(userId, cancellationToken) is { } user ? user.FullName : "-";
        string link = _links.Page(notice.PortalSlug, "/staff/devices/" + block.DeviceId);

        foreach (OperationsUser administrator in await _users.ListAdministratorsAsync(cancellationToken))
        {
            await _mailer.SendDeviceBlockedAsync(new OperationsDeviceBlockedMail(administrator.Email.Value, administrator.FullName, administrator.PreferredLanguage, company,
                block.DeviceName ?? block.DeviceNumber ?? "-", person, block.RegisteredCountry, block.Country, block.CreatedUtc, link), cancellationToken);
        }
    }
}
