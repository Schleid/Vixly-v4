using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsNumberAllocator.cs
//
// PURPOSE:
// Takes the next number of a kind and year for the current tenant (D-O12).
//
// RESPONSIBILITIES:
// - Return a number no other caller gets, even under concurrency
//
// IMPORTANT NOTES:
// - Implemented with one atomic statement in Infrastructure
// =========================================================
public interface IOperationsNumberAllocator
{
    Task<long> NextAsync(OperationsNumberKind kind, int year, CancellationToken cancellationToken);
}
