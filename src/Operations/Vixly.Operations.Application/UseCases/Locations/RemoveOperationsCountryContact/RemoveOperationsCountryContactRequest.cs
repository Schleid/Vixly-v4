namespace Vixly.Operations.Application.UseCases.Locations.RemoveOperationsCountryContact;

// =========================================================
// FILE: RemoveOperationsCountryContactRequest.cs
//
// PURPOSE:
// Removes a country contact (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsCountryContactRequest
{
    public Guid ContactId { get; init; }
}
