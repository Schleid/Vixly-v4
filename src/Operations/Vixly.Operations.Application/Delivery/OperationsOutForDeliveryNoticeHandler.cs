using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsOutForDeliveryNoticeHandler.cs
//
// PURPOSE:
// Sends the customer the out-for-delivery email, with the delivery code when the company asks for one (Slice 7 step 3, R-15).
//
// RESPONSIBILITIES:
// - Name the shipment and link to their shipments
// - Give the code still valid, or a new one saved before it is sent (a retry sends the same code)
//
// IMPORTANT NOTES:
// - A customer no longer active gets nothing
// - The code is the shipment's pickup code: one code, at the door or at the counter
// =========================================================
public sealed class OperationsOutForDeliveryNoticeHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsPickupCodes _codes;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsOutForDeliveryNoticeHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts, IOperationsReceivingRepository receiving,
        OperationsPickupCodes codes, IOperationsUnitOfWork unitOfWork)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public string MessageType => OperationsOutForDeliveryNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsOutForDeliveryNotice notice = OperationsOutboxWriter.Read<OperationsOutForDeliveryNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer
            || await _receiving.GetShipmentAsync(message.StreamId, cancellationToken) is not { DeliveredUtc: null } shipment)
        {
            return;
        }

        Dictionary<string, string> extra = new(StringComparer.Ordinal) { ["shipment"] = notice.ShipmentNumber };
        OperationsCustomerMailKind kind = OperationsCustomerMailKind.OutForDelivery;

        if (notice.WithCode)
        {
            string? code = _codes.Current(shipment);

            if (code is null)
            {
                code = _codes.Issue(shipment);

                if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
                {
                    throw new InvalidOperationException("The delivery code of " + shipment.Id + " could not be saved; the notice is tried again.");
                }
            }

            extra["code"] = code;
            kind = OperationsCustomerMailKind.OutForDeliveryCode;
        }

        await _accounts.SendAsync(customer, kind, _accounts.PortalPage(notice.PortalSlug, "/customer/shipments"), extra, new Dictionary<string, string>(StringComparer.Ordinal),
            cancellationToken);
    }
}
