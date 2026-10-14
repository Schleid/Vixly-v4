using Vixly.Platform.Domain.Audit;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformAuditRepository.cs
//
// PURPOSE:
// Appends audit entries.
//
// RESPONSIBILITIES:
// - Add an entry to the current unit of work
//
// IMPORTANT NOTES:
// - Add only. There is no update or delete, by design
// =========================================================
public interface IPlatformAuditRepository
{
    void Add(PlatformAuditEntry entry);
}
