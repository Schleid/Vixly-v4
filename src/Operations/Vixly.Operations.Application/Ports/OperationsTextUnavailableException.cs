namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsTextUnavailableException.cs
//
// PURPOSE:
// Twilio could not be reached or was overloaded (Slice 9).
//
// RESPONSIBILITIES:
// - Say so; the notice goes by email instead
//
// IMPORTANT NOTES:
// - No secret in the message
// =========================================================
public sealed class OperationsTextUnavailableException : Exception
{
    public OperationsTextUnavailableException()
    {
    }

    public OperationsTextUnavailableException(string message)
        : base(message)
    {
    }

    public OperationsTextUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
