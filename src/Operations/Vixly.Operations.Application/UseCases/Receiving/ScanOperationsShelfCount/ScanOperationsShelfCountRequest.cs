namespace Vixly.Operations.Application.UseCases.Receiving.ScanOperationsShelfCount;

// =========================================================
// FILE: ScanOperationsShelfCountRequest.cs
//
// PURPOSE:
// Records one code scanned on the shelf being counted (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ScanOperationsShelfCountRequest
{
    public Guid CountId { get; init; }

    public string? Code { get; init; }
}
