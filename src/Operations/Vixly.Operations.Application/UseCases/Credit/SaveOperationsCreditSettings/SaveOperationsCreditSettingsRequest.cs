using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.UseCases.Credit.SaveOperationsCreditSettings;

// =========================================================
// FILE: SaveOperationsCreditSettingsRequest.cs
//
// PURPOSE:
// Save the company's credit settings (Slice 5F step 2b, K-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsCreditSettingsRequest
{
    public int TermsDays { get; init; }

    public OperationsCreditOverLimit OverLimit { get; init; }

    public bool Statements { get; init; }
}
