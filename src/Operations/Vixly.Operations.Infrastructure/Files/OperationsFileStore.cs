using System.Text.RegularExpressions;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Files;

// =========================================================
// FILE: OperationsFileStore.cs
//
// PURPOSE:
// Stores tenant files through the provider each tenant uses (D-R7, owner Q-R4).
//
// RESPONSIBILITIES:
// - Choose the provider, make the key, and read a key back from the provider that wrote it
//
// IMPORTANT NOTES:
// - Keys look like folder:parcels/2026/10/<id>.jpg; anything else is refused when read
// - Today every tenant uses the server folder; a tenant cloud provider plugs in by name without changing callers
// =========================================================
internal sealed class OperationsFileStore : IOperationsFileStore
{
    private static readonly Regex KeyPattern = new("^(?<provider>[a-z0-9]{2,20}):(?<path>[a-z]{2,20}/[0-9]{4}/[0-9]{2}/[0-9a-f]{32}\\.(jpg|png|webp|gif|pdf|bin))$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AreaPattern = new("^[a-z]{2,20}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly Dictionary<string, IOperationsFileProvider> _providers;

    private readonly TimeProvider _timeProvider;

    public OperationsFileStore(IEnumerable<IOperationsFileProvider> providers, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToDictionary(provider => provider.Name, StringComparer.Ordinal);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<string> SaveAsync(Guid tenantId, string area, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!AreaPattern.IsMatch(area ?? string.Empty))
        {
            throw new ArgumentException("The file area is not valid.", nameof(area));
        }

        IOperationsFileProvider provider = ProviderFor(tenantId);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string path = $"{area}/{utcNow:yyyy}/{utcNow:MM}/{Guid.NewGuid():N}.{Extension(contentType)}";
        await provider.SaveAsync(tenantId, path, content, cancellationToken);
        return provider.Name + ":" + path;
    }

    public async Task<byte[]?> ReadAsync(Guid tenantId, string key, CancellationToken cancellationToken)
    {
        if (KeyPattern.Match(key ?? string.Empty) is not { Success: true } match
            || !_providers.TryGetValue(match.Groups["provider"].Value, out IOperationsFileProvider? provider))
        {
            return null;
        }

        return await provider.ReadAsync(tenantId, match.Groups["path"].Value, cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, string key, CancellationToken cancellationToken)
    {
        if (KeyPattern.Match(key ?? string.Empty) is { Success: true } match && _providers.TryGetValue(match.Groups["provider"].Value, out IOperationsFileProvider? provider))
        {
            await provider.DeleteAsync(tenantId, match.Groups["path"].Value, cancellationToken);
        }
    }

    // The tenant's own storage will be looked up here once tenants can connect one (D-R7).
    private IOperationsFileProvider ProviderFor(Guid tenantId) =>
        _providers.TryGetValue("folder", out IOperationsFileProvider? folder) ? folder : throw new InvalidOperationException("No file store is configured.");

    private static string Extension(string? contentType) => contentType switch
    {
        "image/jpeg" => "jpg",
        "image/png" => "png",
        "image/webp" => "webp",
        "image/gif" => "gif",
        "application/pdf" => "pdf",
        _ => "bin"
    };
}
