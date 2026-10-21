using Microsoft.Extensions.Options;

namespace Vixly.Operations.Infrastructure.Files;

// =========================================================
// FILE: OperationsFolderFileProvider.cs
//
// PURPOSE:
// Keeps files in a folder on the server, one sub-folder per tenant (D-R7 default).
//
// RESPONSIBILITIES:
// - Write a file atomically and read it back
//
// IMPORTANT NOTES:
// - The path is made by the store from safe parts; it is checked again to stay inside the tenant folder
// - The root is required: Operations.Web and Operations.Api must share it
// =========================================================
internal sealed class OperationsFolderFileProvider : IOperationsFileProvider
{
    private readonly string _root;

    public OperationsFolderFileProvider(IOptions<OperationsFileOptions> options)
    {
        string configured = options?.Value.Root ?? string.Empty;
        _root = string.IsNullOrWhiteSpace(configured) ? throw new InvalidOperationException("OperationsFiles:Root is not configured.") : Path.GetFullPath(configured);
    }

    public string Name => "folder";

    public async Task SaveAsync(Guid tenantId, string path, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        string file = Resolve(tenantId, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        string temporary = file + ".tmp";
        await File.WriteAllBytesAsync(temporary, content, cancellationToken);
        File.Move(temporary, file, overwrite: false);
    }

    public async Task<byte[]?> ReadAsync(Guid tenantId, string path, CancellationToken cancellationToken)
    {
        string file = Resolve(tenantId, path);
        return File.Exists(file) ? await File.ReadAllBytesAsync(file, cancellationToken) : null;
    }

    public Task DeleteAsync(Guid tenantId, string path, CancellationToken cancellationToken)
    {
        File.Delete(Resolve(tenantId, path));
        return Task.CompletedTask;
    }

    private string Resolve(Guid tenantId, string path)
    {
        string tenantFolder = Path.GetFullPath(Path.Combine(_root, tenantId.ToString("N")));
        string file = Path.GetFullPath(Path.Combine(tenantFolder, path));

        return file.StartsWith(tenantFolder + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? file
            : throw new InvalidOperationException("A file path left its tenant folder.");
    }
}
