using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.SaveOperationsCounterSignature;

// =========================================================
// FILE: SaveOperationsCounterSignatureUseCase.cs
//
// PURPOSE:
// The customer signs on screen at the counter for their declaration and insurance choice (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve at the shipment warehouse
// - The customer signs; the clerk present is recorded with it
// =========================================================
public sealed class SaveOperationsCounterSignatureUseCase
{
    public const int MaxBytes = 200 * 1024;

    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsFileStore _files;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsCounterSignatureUseCase(
        OperationsCounterDesk desk,
        IOperationsReceivingRepository receiving,
        IOperationsFileStore files,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsCounterSignatureResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsCounterSignatureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsCounterSignatureResponse.Failure(refused);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { HasInstructions: true } shipment
            || !counters.Any(counter => counter.Id == shipment.OriginWarehouseId))
        {
            return SaveOperationsCounterSignatureResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // A PNG by its first bytes, small: a signature, nothing else (as photos, Slice 3B).
        if (request.Image is not { Length: > 60 and <= MaxBytes } image || !image.AsSpan(0, 8).SequenceEqual(Png))
        {
            return SaveOperationsCounterSignatureResponse.Failure(OperationsShipmentErrors.SignatureInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string? previous = shipment.CustomerSignatureKey;
        string key = await _files.SaveAsync(shipment.TenantId, "signatures", image, "image/png", cancellationToken);

        try
        {
            shipment.RecordSignature(key, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            await _files.DeleteAsync(shipment.TenantId, key, cancellationToken);
            return SaveOperationsCounterSignatureResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.shipments.customer_signed", nameof(OperationsShipment), shipment.Id, after: new { number = shipment.Number });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            // Signed again: the earlier signature goes (the shipment keeps only the latest).
            if (previous is not null && previous != key)
            {
                await _files.DeleteAsync(shipment.TenantId, previous, cancellationToken);
            }

            return SaveOperationsCounterSignatureResponse.Success();
        }

        await _files.DeleteAsync(shipment.TenantId, key, cancellationToken);
        return SaveOperationsCounterSignatureResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
