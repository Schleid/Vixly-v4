using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Company;

// =========================================================
// FILE: OperationsCompanyView.cs
//
// PURPOSE:
// What the company page shows, and what the viewer may do (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the account, the latest change request and the allowed actions
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsCompanyView(OperationsCompany Company, OperationsCompanyChange? Change, bool CanEditContacts, bool CanRequestChange);
