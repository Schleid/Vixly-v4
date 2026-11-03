using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Receiving.ScanOperationsShelfCount;

// =========================================================
// FILE: ScanOperationsShelfCountUseCase.cs
//
// PURPOSE:
// Records one code scanned on the shelf being counted (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A code scanned twice counts once
// - Two scanners at once: the second is asked to scan again (row version)
// =========================================================
public sealed class ScanOperationsShelfCountUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsShelfCountRepository _counts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ScanOperationsShelfCountUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsShelfCountRepository counts,
        IOperationsReceivingRepository receiving,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScanOperationsShelfCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ScanOperationsShelfCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ScanOperationsShelfCountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _counts.GetAsync(request.CountId, cancellationToken) is not { } count
            || await _scope.CoveredAsync(access, OperationsPermissions.ReceivingReceive, count.WarehouseId, cancellationToken) is null)
        {
            return ScanOperationsShelfCountResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        // Scanners may add a symbology prefix (]C0 to ]C9).
        string code = (request.Code ?? string.Empty).Trim();
        code = code.Length > 3 && code[0] == ']' ? code[3..] : code;
        bool added;

        try
        {
            added = count.Scan(code, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return ScanOperationsShelfCountResponse.Failure(exception.Error);
        }

        return !added || await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ScanOperationsShelfCountResponse.Success(added)
            : ScanOperationsShelfCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
