using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.SaveOperationsPrinter;

// =========================================================
// FILE: SaveOperationsPrinterUseCase.cs
//
// PURPOSE:
// Adds a label printer to a warehouse, or changes one (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.printers.manage for that warehouse
// =========================================================
public sealed class SaveOperationsPrinterUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsPrintingRepository _printing;

    private readonly Vixly.Operations.Application.Audit.OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsPrinterUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsPrintingRepository printing,
        Vixly.Operations.Application.Audit.OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsPrinterResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsPrinterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return SaveOperationsPrinterResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.PrintersManage, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return SaveOperationsPrinterResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsLabelPrinter printer;

        try
        {
            if (request.PrinterId is { } printerId)
            {
                if (await _printing.GetPrinterAsync(printerId, cancellationToken) is not { } found || found.WarehouseId != warehouse.Id)
                {
                    return SaveOperationsPrinterResponse.Failure(OperationsPrintingErrors.PrinterNotFound);
                }

                printer = found;
                printer.Change(request.Name, request.Connection, request.Address, request.Dpi, request.Active, utcNow, request.Media);
            }
            else
            {
                printer = OperationsLabelPrinter.Create(context.RequiredTenantId, warehouse.Id, request.Name, request.Connection, request.Address, request.Dpi, utcNow, request.Media);
                _printing.Add(printer);
            }
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException exception)
        {
            return SaveOperationsPrinterResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.printers.saved", nameof(OperationsLabelPrinter), printer.Id,
            after: new { warehouse = warehouse.Code, name = printer.Name, connection = printer.Connection.ToString(), address = printer.Address, dpi = printer.Dpi, active = printer.IsActive, media = printer.Media.ToString() });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsPrinterResponse.Success(printer.Id)
            : SaveOperationsPrinterResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
