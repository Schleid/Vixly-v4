namespace Vixly.Operations.Application.UseCases.Legal.GetOperationsLegalStatus;

// =========================================================
// FILE: GetOperationsLegalStatusRequest.cs
//
// PURPOSE:
// Reads the tenant's Terms status and the version to accept (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsLegalStatusRequest
{
    public string Language { get; init; } = "en";
}
