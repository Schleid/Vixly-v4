using Vixly.Operations.Domain.Audit;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsAuditRepository.cs
//
// PURPOSE:
// The append-only audit log.
//
// RESPONSIBILITIES:
// - Add an entry
//
// IMPORTANT NOTES:
// - Never updates or deletes
// =========================================================
public interface IOperationsAuditRepository
{
    void Add(OperationsAuditEntry entry);
}
