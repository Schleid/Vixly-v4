namespace Vixly.Operations.Application.UseCases.Screens.GetOperationsScreenZelleQr;

// =========================================================
// FILE: GetOperationsScreenZelleQrRequest.cs
//
// PURPOSE:
// The company’s Zelle QR picture, for the customer screen while it shows the Zelle step (Slice 5F step 3, P-9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsScreenZelleQrRequest
{
    public string? Secret { get; init; }
}
