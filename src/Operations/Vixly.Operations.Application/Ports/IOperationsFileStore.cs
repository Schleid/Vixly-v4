namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsFileStore.cs
//
// PURPOSE:
// Keeps the tenant's files (photos, documents) outside the database (Slice 3B, D-R7).
//
// RESPONSIBILITIES:
// - Save bytes and get a key; read them back by key
//
// IMPORTANT NOTES:
// - Each tenant may have its own storage (a folder by default; Azure Blob, Amazon S3 or Cloudflare R2 later): the store decides per tenant
// - Keys never contain anything the caller typed
// =========================================================
public interface IOperationsFileStore
{
    Task<string> SaveAsync(Guid tenantId, string area, byte[] content, string contentType, CancellationToken cancellationToken);

    Task<byte[]?> ReadAsync(Guid tenantId, string key, CancellationToken cancellationToken);

    // Removes a file whose record was never saved (a refused or failed command); unknown keys are ignored.
    Task DeleteAsync(Guid tenantId, string key, CancellationToken cancellationToken);
}
