using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyProfile;

// =========================================================
// FILE: UpdateOperationsCompanyProfileRequest.cs
//
// PURPOSE:
// The company profile as the owner saves it: trading name, email, phone and address (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The legal name, reason and proof are not used here (SubmitOperationsCompanyChange)
// =========================================================
public sealed class UpdateOperationsCompanyProfileRequest
{
    public OperationsCompanyChangeInput Input { get; init; } = new();
}
