using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.DismissMyOperationsCustomerTour;

// =========================================================
// FILE: DismissMyOperationsCustomerTourResponse.cs
// PURPOSE:
// Reports durable tour dismissal or an expected refusal.
// RESPONSIBILITIES:
// - Keep application results free of user-facing text.
// IMPORTANT NOTES:
// - A refusal never appears as a successful dismissal.
// =========================================================
public sealed class DismissMyOperationsCustomerTourResponse
{
    private DismissMyOperationsCustomerTourResponse() { }
    public bool Succeeded { get; private init; }
    public Error? Error { get; private init; }
    public static DismissMyOperationsCustomerTourResponse Success() => new() { Succeeded = true };
    public static DismissMyOperationsCustomerTourResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new() { Error = error };
    }
}
