namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCardRefusedException.cs
//
// PURPOSE:
// Stripe refused a request (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry Stripe's short code for the logs, never the keys
//
// IMPORTANT NOTES:
// - The person sees a plain message (ops.cards.provider_refused)
// =========================================================
public sealed class OperationsCardRefusedException : Exception
{
    public OperationsCardRefusedException()
    {
    }

    public OperationsCardRefusedException(string message)
        : base(message)
    {
    }

    public OperationsCardRefusedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
