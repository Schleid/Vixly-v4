namespace Vixly.Operations.Application.UseCases.Bundles.FindOperationsBundleCode;

// =========================================================
// FILE: FindOperationsBundleCodeRequest.cs
//
// PURPOSE:
// The fill station's first scan (Slice 6A, D-3): the bundle or the repack box whose barcode was scanned.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FindOperationsBundleCodeRequest
{
    public string? Code { get; init; }
}
