using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.SaveOperationsCardReader;

// =========================================================
// FILE: SaveOperationsCardReaderUseCase.cs
//
// PURPOSE:
// Adds or removes a Stripe Terminal card reader of a warehouse (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.settings.edit
// - The reader is registered with Stripe first; Vixly keeps its id (tmr_...)
// =========================================================
public sealed class SaveOperationsCardReaderUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCardRepository _cards;

    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsCardReaderUseCase(
        OperationsAccess access,
        IOperationsCardRepository cards,
        IOperationsLocationRepository locations,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsCardReaderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsCardReaderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsCardReaderResponse.Failure(refused);
        }

        if (request.RemoveReaderId is { } removeId)
        {
            if (await _cards.GetReaderAsync(removeId, cancellationToken) is { } old)
            {
                _cards.Remove(old);
                _audit.Record(access!.User.Id, "ops.payments.reader_removed", nameof(OperationsCardReader), old.Id, before: new { old.Label, reader = old.StripeReaderId });
            }
        }
        else
        {
            if (await _locations.GetWarehouseAsync(request.WarehouseId, cancellationToken) is not { Status: OperationsLocationStatus.Active })
            {
                return SaveOperationsCardReaderResponse.Failure(OperationsCardErrors.ReaderInvalid);
            }

            OperationsCardReader reader;

            try
            {
                reader = OperationsCardReader.Add(context.RequiredTenantId, request.WarehouseId, request.Label, request.StripeReaderId, _timeProvider.GetUtcNow());
            }
            catch (DomainException exception)
            {
                return SaveOperationsCardReaderResponse.Failure(exception.Error);
            }

            if ((await _cards.ListReadersAsync(cancellationToken)).Any(item => item.StripeReaderId == reader.StripeReaderId))
            {
                return SaveOperationsCardReaderResponse.Failure(OperationsCardErrors.ReaderInvalid);
            }

            _cards.Add(reader);
            _audit.Record(access!.User.Id, "ops.payments.reader_added", nameof(OperationsCardReader), reader.Id, after: new { reader.Label, reader = reader.StripeReaderId });
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsCardReaderResponse.Success()
            : SaveOperationsCardReaderResponse.Failure(OperationsCardErrors.ReaderInvalid);
    }
}
