namespace Vixly.Operations.Domain.Common;

// =========================================================
// FILE: IOperationsSystemEntity.cs
//
// PURPOSE:
// Marks the few Operations tables that do not belong to one tenant (Stage 3, Slice 0, D-F1).
//
// RESPONSIBILITIES:
// - Let the architecture test and the DbContext tell system tables from tenant tables
//
// IMPORTANT NOTES:
// - The list is closed and written in the Slice 0 design: outbox, job runs, Platform feed position (and the Data Protection keys, which are not entities)
// - Adding to it needs a design-note change and review
// =========================================================
public interface IOperationsSystemEntity
{
}
