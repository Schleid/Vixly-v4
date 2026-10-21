using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.TakeOperationsPrintJobs;

// =========================================================
// FILE: TakeOperationsPrintJobsUseCase.cs
//
// PURPOSE:
// Hands waiting labels to a dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the jobs of printers at the print program's own warehouse
// - A job not reported within a minute is offered again, at most five times
// =========================================================
public sealed class TakeOperationsPrintJobsUseCase
{
    private readonly OperationsPrintAgentKeys _keys;

    private readonly IOperationsPrintingRepository _printing;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public TakeOperationsPrintJobsUseCase(
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

    public async Task<TakeOperationsPrintJobsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TakeOperationsPrintJobsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _keys.AuthenticateAsync(request.AgentKey, cancellationToken) is not { } agent)
        {
            return TakeOperationsPrintJobsResponse.Failure(OperationsPrintingErrors.AgentNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<OperationsPrintJob> jobs = await _printing.ListWaitingJobsAsync(agent.WarehouseId, utcNow, MaxJobs, cancellationToken);
        Dictionary<Guid, OperationsLabelPrinter> printers = (await _printing.ListPrintersByIdAsync(jobs.Select(job => job.PrinterId).Distinct().ToList(), cancellationToken))
            .Where(printer => printer.IsActive)
            .ToDictionary(printer => printer.Id);
        List<OperationsPrintJobView> taken = new();

        foreach (OperationsPrintJob job in jobs.Where(job => printers.ContainsKey(job.PrinterId)))
        {
            job.Take(utcNow);
            OperationsLabelPrinter printer = printers[job.PrinterId];
            // Seconds rather than a time, so the dock computer's clock does not matter.
            int expiresIn = (int)Math.Max(0, (job.CreatedUtc + OperationsPrintJob.FreshFor - utcNow).TotalSeconds);
            taken.Add(new OperationsPrintJobView(job.Id, printer.Connection, printer.Address, job.Payload, expiresIn));
        }

        agent.Seen(utcNow);

        // Two print programs of one warehouse asking at once: one of them gets the jobs (row version), the other asks again.
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? TakeOperationsPrintJobsResponse.Success(taken)
            : TakeOperationsPrintJobsResponse.Success(Array.Empty<OperationsPrintJobView>());
    }

    public const int MaxJobs = 20;
    
}
