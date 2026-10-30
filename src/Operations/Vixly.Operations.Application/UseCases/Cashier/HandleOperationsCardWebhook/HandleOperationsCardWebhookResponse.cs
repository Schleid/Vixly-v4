using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.HandleOperationsCardWebhook;

// =========================================================
// FILE: HandleOperationsCardWebhookResponse.cs
//
// PURPOSE:
// Takes an event from the company's Stripe account and records a card payment it confirms (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class HandleOperationsCardWebhookResponse
{
    private HandleOperationsCardWebhookResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static HandleOperationsCardWebhookResponse Success()
    {
        return new HandleOperationsCardWebhookResponse { Succeeded = true };
    }

    public static HandleOperationsCardWebhookResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new HandleOperationsCardWebhookResponse { Error = error };
    }
}
