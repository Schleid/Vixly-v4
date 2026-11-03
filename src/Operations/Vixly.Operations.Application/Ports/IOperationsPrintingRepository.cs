using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPrintingRepository.cs
//
// PURPOSE:
// Stores label printers, dock print programs and their print jobs (Slice 3B).
//
// RESPONSIBILITIES:
// - Add and find printers and print programs; queue and hand out print jobs
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// - A job is handed to one print program at a time (row version)
// =========================================================
public interface IOperationsPrintingRepository
{
    void Add(OperationsLabelPrinter printer);

    void Add(OperationsPrintAgent agent);

    void Add(OperationsPrintJob job);

    Task<OperationsLabelPrinter?> GetPrinterAsync(Guid printerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsLabelPrinter>> ListPrintersAsync(Guid warehouseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsLabelPrinter>> ListPrintersByIdAsync(IReadOnlyCollection<Guid> printerIds, CancellationToken cancellationToken);

    Task<OperationsPrintAgent?> GetAgentAsync(Guid agentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsPrintAgent>> ListAgentsAsync(Guid warehouseId, CancellationToken cancellationToken);

    // Jobs of the warehouse's printers that may be taken now, oldest first.
    Task<IReadOnlyList<OperationsPrintJob>> ListWaitingJobsAsync(Guid warehouseId, DateTimeOffset utcNow, int max, CancellationToken cancellationToken);

    Task<OperationsPrintJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);

    // Slice 12E: the waiting labels one print program may take: a phone only its portable printers', a computer only the others'.
    Task<IReadOnlyList<OperationsPrintJob>> ListWaitingJobsForAsync(Guid warehouseId, Guid? phoneAgentId, DateTimeOffset utcNow, int max, CancellationToken cancellationToken);

    void Add(OperationsPrintPairing pairing);

    Task<OperationsPrintPairing?> FindPairingAsync(string codeHash, CancellationToken cancellationToken);

    Task<OperationsLabelPrinter?> FindPhonePrinterAsync(Guid agentId, string address, CancellationToken cancellationToken);
}
