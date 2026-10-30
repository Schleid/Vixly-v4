using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.CancelOperationsCardPayment;

// =========================================================
// FILE: CancelOperationsCardPaymentUseCase.cs
//
// PURPOSE:
// Cancels a card payment that has not been paid (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.payments.take at the charge’s warehouse
// - A charge Stripe already took is never cancelled here
// =========================================================
public sealed class CancelOperationsCardPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly OperationsCardDesk _cards;

    private readonly IOperationsCardRepository _store;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public CancelOperationsCardPaymentUseCase(
        OperationsBillingDesk desk,
        OperationsCardDesk cards,
        IOperationsCardRepository store,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CancelOperationsCardPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CancelOperationsCardPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return CancelOperationsCardPaymentResponse.Failure(refused);
        }

        if (await _store.GetChargeAsync(request.ChargeId, cancellationToken) is not { } charge || warehouses.FirstOrDefault(warehouse => warehouse.Id == charge.WarehouseId) is not { } at
            || !access!.HandlesMoneyAt(at))
        {
            return CancelOperationsCardPaymentResponse.Failure(OperationsCardErrors.ChargeNotFound);
        }

        string? secret = await _cards.SecretKeyAsync(cancellationToken);

        // Paid meanwhile: it stays paid.
        if (secret is not null)
        {
            await _cards.RefreshAsync(secret, charge, cancellationToken);
        }

        if (charge.Status != OperationsCardChargeStatus.Started)
        {
            return CancelOperationsCardPaymentResponse.Success();
        }

        if (!await _cards.CancelAsync(secret, charge, cancellationToken))
        {
            return CancelOperationsCardPaymentResponse.Failure(OperationsCardErrors.ProviderUnavailable);
        }

        // Paid at the last second: recorded (and saved) by the desk; it stays paid.
        if (charge.Status != OperationsCardChargeStatus.Cancelled)
        {
            return CancelOperationsCardPaymentResponse.Success();
        }

        _audit.Record(access!.User.Id, "ops.payments.card_cancelled", nameof(OperationsCardCharge), charge.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CancelOperationsCardPaymentResponse.Success()
            : CancelOperationsCardPaymentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
