namespace Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcel;

// =========================================================
// FILE: GetMyOperationsParcelRequest.cs
//
// PURPOSE:
// One of a customer's own packages: history and photos (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsParcelRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ParcelId { get; init; }
}
