using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.FindOperationsBundleCode;

// =========================================================
// FILE: FindOperationsBundleCodeUseCase.cs
//
// PURPOSE:
// The fill station's first scan (Slice 6A, D-3): the bundle or the repack box whose barcode was scanned.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.fill
// =========================================================
public sealed class FindOperationsBundleCodeUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    public FindOperationsBundleCodeUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
    }

    public async Task<FindOperationsBundleCodeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FindOperationsBundleCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return FindOperationsBundleCodeResponse.Failure(refused);
        }
        string code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();

        if (code.Length is 0 or > 40)
        {
            return FindOperationsBundleCodeResponse.Failure(OperationsBundleErrors.UnknownCode);
        }

        if (await _bundles.FindBundleByNumberAsync(code, cancellationToken) is { } bundle && warehouses.Any(item => item.Id == bundle.WarehouseId))
        {
            return bundle.IsOpen ? FindOperationsBundleCodeResponse.Success(bundle.Id, null) : FindOperationsBundleCodeResponse.Failure(OperationsBundleErrors.NotOpen);
        }

        return await _bundles.FindBoxByNumberAsync(code, cancellationToken) is { } box && warehouses.Any(item => item.Id == box.WarehouseId)
            ? FindOperationsBundleCodeResponse.Success(null, box.Id)
            : FindOperationsBundleCodeResponse.Failure(OperationsBundleErrors.UnknownCode);
    }
}
