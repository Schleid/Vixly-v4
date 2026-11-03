namespace Vixly.Operations.Application.UseCases.Customers.DismissMyOperationsCustomerTour;

// =========================================================
// FILE: DismissMyOperationsCustomerTourRequest.cs
// PURPOSE:
// Supplies the opaque customer session to the tour dismissal.
// RESPONSIBILITIES:
// - Accept no customer or company identity from a browser form.
// IMPORTANT NOTES:
// - Operations validates the session before changing its owner.
// =========================================================
public sealed class DismissMyOperationsCustomerTourRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
