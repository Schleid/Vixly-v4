using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Vixly.Operations.Infrastructure.Maps;

// =========================================================
// FILE: OperationsMapTiles.cs
//
// PURPOSE:
// Fetches map pictures from the configured provider for the pages' maps (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Fetch one tile, keep it in memory for a day, give it back as PNG bytes
//
// IMPORTANT NOTES:
// - The pages keep their strict Content-Security-Policy (img-src 'self'): tiles come through the site
// - At most 2,000 tiles kept (about 30 MB); an unknown tile or a provider error answers none
// =========================================================
public sealed class OperationsMapTiles : IDisposable
{
    public const string HttpClientName = "operations-map-tiles";

    private static readonly TimeSpan Kept = TimeSpan.FromDays(1);

    private readonly IHttpClientFactory _http;

    private readonly OperationsMapOptions _options;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 2000 });

    public OperationsMapTiles(IHttpClientFactory http, IOptions<OperationsMapOptions> options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public bool Enabled => !string.IsNullOrWhiteSpace(_options.TileUrl);

    public string Attribution => _options.Attribution;

    public int MaxZoom => Math.Clamp(_options.MaxZoom, 1, 19);

    public async Task<byte[]?> GetAsync(int z, int x, int y, CancellationToken cancellationToken)
    {
        if (!Enabled || z < 0 || z > MaxZoom || x < 0 || y < 0 || x >= (1 << z) || y >= (1 << z))
        {
            return null;
        }

        string key = z + "/" + x + "/" + y;

        if (_cache.TryGetValue(key, out byte[]? kept))
        {
            return kept;
        }

        string address = _options.TileUrl.Replace("{z}", z.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{x}", x.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{y}", y.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, address);
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);
            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > 1_000_000)
            {
                return null;
            }

            byte[] tile = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            _cache.Set(key, tile, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = Kept });
            return tile;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public void Dispose() => _cache.Dispose();
}
