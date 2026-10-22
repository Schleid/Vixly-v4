using Vixly.Operations.Domain.Bundles;

namespace Vixly.Operations.Application.UseCases.Bundles.SaveOperationsBundleSettings;

// =========================================================
// FILE: SaveOperationsBundleSettingsRequest.cs
//
// PURPOSE:
// Save the company's bundle settings (Slice 6A, D-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsBundleSettingsRequest
{
    public IReadOnlyList<string?> Types { get; init; } = Array.Empty<string?>();

    public OperationsBundleTargetRule TargetRule { get; init; }

    public OperationsConsolidationWay Way { get; init; }
}
