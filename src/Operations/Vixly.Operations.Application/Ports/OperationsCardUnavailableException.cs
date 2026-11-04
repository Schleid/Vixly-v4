namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCardUnavailableException.cs
//
// PURPOSE:
// Stripe could not be reached (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Tell the caller to say "try again"
//
// IMPORTANT NOTES:
// - Nothing was paid on our side because of it
// =========================================================
public sealed class OperationsCardUnavailableException : Exception
{
    public OperationsCardUnavailableException()
    {
    }

    public OperationsCardUnavailableException(string message)
        : base(message)
    {
    }

    public OperationsCardUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
