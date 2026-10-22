namespace Vixly.Operations.Infrastructure.Files;

// =========================================================
// FILE: IOperationsFileProvider.cs
//
// PURPOSE:
// One place files can live: the server folder today, a tenant's own cloud storage later (D-R7).
//
// RESPONSIBILITIES:
// - Write and read a file by its key within a tenant
//
// IMPORTANT NOTES:
// - A provider never chooses the key; the store does, so keys cannot point outside the tenant
// =========================================================
public interface IOperationsFileProvider
{
    // Short name written at the start of every key, so a file is always read from where it was written.
    string Name { get; }

    Task SaveAsync(Guid tenantId, string path, byte[] content, CancellationToken cancellationToken);

    Task<byte[]?> ReadAsync(Guid tenantId, string path, CancellationToken cancellationToken);

    Task DeleteAsync(Guid tenantId, string path, CancellationToken cancellationToken);
}
