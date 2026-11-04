namespace Vixly.Operations.Application.Platform;

// =========================================================
// FILE: OperationsPlatformUnavailableException.cs
//
// PURPOSE:
// Platform did not answer, or answered with an error Operations cannot use.
//
// RESPONSIBILITIES:
// - Let callers show "try again" instead of a wrong answer
//
// IMPORTANT NOTES:
// - Carries no response body (S-9)
// =========================================================
public sealed class OperationsPlatformUnavailableException : Exception
{
    public OperationsPlatformUnavailableException()
    {
    }

    public OperationsPlatformUnavailableException(string message)
        : base(message)
    {
    }

    public OperationsPlatformUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
