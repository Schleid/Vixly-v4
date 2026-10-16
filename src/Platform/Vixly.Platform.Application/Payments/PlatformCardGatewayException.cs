namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardGatewayException.cs
//
// PURPOSE:
// A provider call that failed: unreachable, refused or misconfigured.
//
// RESPONSIBILITIES:
// - Carry a provider call that failed: unreachable, refused or misconfigured
//
// IMPORTANT NOTES:
// - The message is a code; provider text stays in the logs of the adapter only
// =========================================================
public sealed class PlatformCardGatewayException : Exception
{
    public PlatformCardGatewayException()
        : this("platform.payments.provider_unavailable")
    {
    }

    public PlatformCardGatewayException(string code)
        : base(code)
    {
        Code = code;
    }

    public PlatformCardGatewayException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
    }

    public string Code { get; } = "platform.payments.provider_unavailable";

    // A timeout or network error: the outcome is unknown and must be checked later, never treated as failed.
    public bool OutcomeUnknown => Code == "platform.payments.provider_unavailable";
}
