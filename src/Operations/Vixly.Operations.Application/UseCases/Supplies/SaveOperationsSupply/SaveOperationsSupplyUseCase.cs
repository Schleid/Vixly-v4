using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupply;

// =========================================================
// FILE: SaveOperationsSupplyUseCase.cs
//
// PURPOSE:
// Create a shipping supply with its first price, or change its details and barcodes (Slice 5E, S-1 to S-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.charges.configure: the people who set prices manage supplies (Slice 5E, S-10)
// - The item code and every barcode are unique across the company's supplies, item codes and barcodes together
// =========================================================
public sealed class SaveOperationsSupplyUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsSupplyUseCase(
        OperationsAccess access,
        IOperationsSupplyRepository supplies,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsSupplyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsSupplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsSupplyResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSupplyDetails details = new(request.Code, request.NameEn, request.NameFr, request.NameEs, request.Unit, request.Category, request.Taxed);
        OperationsSupply supply;

        try
        {
            if (request.SupplyId is { } supplyId)
            {
                supply = await _supplies.GetAsync(supplyId, cancellationToken) ?? throw new DomainException(OperationsSupplyErrors.NotFound);
                supply.Change(details, request.Active, utcNow);
                supply.ChangeBarcodes(request.Barcodes, utcNow);
            }
            else
            {
                if ((await _supplies.ListAsync(cancellationToken)).Count >= OperationsSupplyLimits.MaxSupplies)
                {
                    return SaveOperationsSupplyResponse.Failure(OperationsSupplyErrors.TooMany);
                }

                OperationsSupplyPrice price = await OperationsSupplyPricing.PriceAsync(request.Price, request.Breaks, request.WarehousePrices, request.EffectiveFrom,
                    _settings, _locations, context.RequiredTenantId, utcNow, cancellationToken);
                supply = OperationsSupply.Create(context.RequiredTenantId, details, request.Barcodes, price, utcNow);
                supply.Change(details, request.Active, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsSupplyResponse.Failure(exception.Error);
        }

        // One code per item in the company: neither another supply's item code nor its barcode (a scan must find one supply).
        if (await _supplies.CodeOwnerAsync(supply.Code, supply.Id, cancellationToken) is { } owner)
        {
            // The same new supply sent twice (a double click, ADR-0019): the one already there.
            return request.SupplyId is null && await _supplies.GetAsync(owner, cancellationToken) is { } existing && existing.Code == supply.Code && existing.NameEn == supply.NameEn
                ? SaveOperationsSupplyResponse.Success(existing.Id)
                : SaveOperationsSupplyResponse.Failure(OperationsSupplyErrors.CodeDuplicate);
        }

        foreach (OperationsSupplyBarcode barcode in supply.Barcodes)
        {
            if (barcode.Code == supply.Code || await _supplies.CodeOwnerAsync(barcode.Code, supply.Id, cancellationToken) is not null)
            {
                return SaveOperationsSupplyResponse.Failure(OperationsSupplyErrors.BarcodeDuplicate);
            }
        }

        if (request.SupplyId is null)
        {
            _supplies.Add(supply);
        }

        _audit.Record(access!.User.Id, "ops.supplies.saved", nameof(OperationsSupply), supply.Id,
            after: new { code = supply.Code, name = supply.NameEn, category = supply.Category.ToString(), taxed = supply.IsTaxed, active = supply.IsActive, barcodes = supply.Barcodes.Select(barcode => barcode.Code).ToList() });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsSupplyResponse.Success(supply.Id);
        }

        // Two saves at once: the unique indexes keep one code per item; the same new supply sent twice gives the one already there.
        _unitOfWork.DiscardChanges();
        return request.SupplyId is null && await _supplies.CodeOwnerAsync(supply.Code, null, cancellationToken) is { } raced
            ? SaveOperationsSupplyResponse.Success(raced)
            : SaveOperationsSupplyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
