namespace Vixly.Customer.Web.Operations;

// =========================================================
// FILE: CustomerOperationsOptions.cs
//
// PURPOSE:
// Settings for calling Operations.Api (section CustomerOperations).
//
// RESPONSIBILITIES:
// - Hold the base address, caller key and timeout
//
// IMPORTANT NOTES:
// - The private key is a secret: host environment only (S-11)
// =========================================================
public sealed class CustomerOperationsOptions
{
    public const string SectionName = "CustomerOperations";

    public string BaseUrl { get; set; } = string.Empty;

    public string CallerKeyId { get; set; } = string.Empty;

    public string CallerPrivateKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}
