using Vixly.Operations.Application.Audit;
using Vixly.Operations.Domain.Audit;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsAuditReader.cs
//
// PURPOSE:
// Reads the tenant's audit entries, newest first (Slice 1E).
//
// RESPONSIBILITIES:
// - Filter and page the entries
//
// IMPORTANT NOTES:
// - Read-only: entries are never changed (D-2)
// =========================================================
public interface IOperationsAuditReader
{
    Task<(IReadOnlyList<OperationsAuditEntry> Items, int TotalCount)> ListAsync(OperationsAuditFilter filter, int page, int pageSize, CancellationToken cancellationToken);
}
