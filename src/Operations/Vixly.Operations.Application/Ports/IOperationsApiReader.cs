using Vixly.Operations.Application.Api;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsApiReader.cs
//
// PURPOSE:
// Reads the company’s data as the company API and its webhooks show it (Slice 10).
//
// RESPONSIBILITIES:
// - A page of a resource after a place; one row by id
//
// IMPORTANT NOTES:
// - Tenant-filtered like every Operations read; no tracking, read only
// =========================================================
public interface IOperationsApiReader
{
    Task<IReadOnlyList<OperationsApiRow>> ListAsync(OperationsApiResource resource, OperationsApiQuery query, CancellationToken cancellationToken);

    Task<object?> GetAsync(OperationsApiResource resource, Guid id, CancellationToken cancellationToken);

    // The records an event batch is about, by id (tracking events included).
    Task<IReadOnlyDictionary<Guid, object>> GetManyAsync(OperationsApiResource resource, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
