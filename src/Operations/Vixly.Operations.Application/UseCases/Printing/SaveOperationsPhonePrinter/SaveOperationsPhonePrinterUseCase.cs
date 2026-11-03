using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Printing.SaveOperationsPhonePrinter;

// =========================================================
// FILE: SaveOperationsPhonePrinterUseCase.cs
//
// PURPOSE:
// A phone running Vixly Print adds or changes the portable printer next to it (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only a phone print program (Vixly Print), with its key
// - The printer belongs to the phone and its warehouse
// =========================================================
public sealed class SaveOperationsPhonePrinterUseCase
{
    private readonly OperationsPrintAgentKeys _keys;

    private readonly IOperationsPrintingRepository _printing;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsPhonePrinterUseCase(
        OperationsPrintAgentKeys keys,
        IOperationsPrintingRepository printing,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsPhonePrinterResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsPhonePrinterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _keys.AuthenticateAsync(request.AgentKey, cancellationToken) is not { IsPhone: true } agent)
        {
            return SaveOperationsPhonePrinterResponse.Failure(OperationsPrintingErrors.AgentNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsLabelPrinter printer;

        try
        {
            if (await _printing.FindPhonePrinterAsync(agent.Id, (request.Address ?? string.Empty).Trim(), cancellationToken) is { } known)
            {
                known.ChangeFromPhone(request.Name, request.Dpi, request.Media, request.Active, utcNow);
                printer = known;
            }
            else
            {
                printer = OperationsLabelPrinter.ForPhone(context.RequiredTenantId, agent.WarehouseId, agent.Id, request.Name, request.Address, request.Dpi, request.Media, utcNow);
                _printing.Add(printer);
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsPhonePrinterResponse.Failure(exception.Error);
        }

        agent.Seen(utcNow);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsPhonePrinterResponse.Success(printer.Id)
            : SaveOperationsPhonePrinterResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
