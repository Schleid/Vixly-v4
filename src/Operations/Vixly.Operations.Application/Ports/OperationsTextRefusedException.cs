namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsTextRefusedException.cs
//
// PURPOSE:
// Twilio refused a request (Slice 9): wrong keys, a number that cannot receive, a customer who replied STOP.
//
// RESPONSIBILITIES:
// - Carry Twilio's code (21610: the customer opted out)
//
// IMPORTANT NOTES:
// - Never the keys or the message
// =========================================================
public sealed class OperationsTextRefusedException : Exception
{
    public OperationsTextRefusedException()
    {
    }

    public OperationsTextRefusedException(string message)
        : base(message)
    {
    }

    public OperationsTextRefusedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? Code { get; init; }

    // The customer replied STOP (SMS) or blocked the business (WhatsApp).
    public bool OptedOut => Code is "21610" or "63024";
}
