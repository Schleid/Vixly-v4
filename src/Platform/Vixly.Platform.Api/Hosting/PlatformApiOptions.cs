namespace Vixly.Platform.Api.Hosting;

// =========================================================
// FILE: PlatformApiOptions.cs
//
// PURPOSE:
// Configuration of the Platform API host (section PlatformApi).
//
// RESPONSIBILITIES:
// - Hold the browser origins allowed by CORS and the body limit
//
// IMPORTANT NOTES:
// - Public.Web calls the API from its server, so CORS is only for
//   the slug check made from the browser
// =========================================================
public sealed class PlatformApiOptions
{
    public const string SectionName = "PlatformApi";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    public long MaxRequestBodyBytes { get; set; } = 16 * 1024;
}
