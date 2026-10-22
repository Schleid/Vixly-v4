using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.UseCases.Company.SubmitOperationsCompanyChange;

// =========================================================
// FILE: SubmitOperationsCompanyChangeRequest.cs
//
// PURPOSE:
// Asks Vixly to change the company's legal details, with proof (B12, Owner only).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SubmitOperationsCompanyChangeRequest
{
    public OperationsCompanyChangeInput Input { get; init; } = new();
}
