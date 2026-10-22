namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsPlatformOptions.cs
//
// PURPOSE:
// Settings for calling Platform.Api (section OperationsPlatform).
//
// RESPONSIBILITIES:
// - Hold the base address, caller key and timeout
//
// IMPORTANT NOTES:
// - The private key is a secret: host environment only (S-11)
// - Empty key: Platform calls are refused and logged at start
// =========================================================
public sealed class OperationsPlatformOptions
{
    public const string SectionName = "OperationsPlatform";

    public string BaseUrl { get; set; } = string.Empty;

    public string CallerKeyId { get; set; } = string.Empty;

    public string CallerPrivateKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}
