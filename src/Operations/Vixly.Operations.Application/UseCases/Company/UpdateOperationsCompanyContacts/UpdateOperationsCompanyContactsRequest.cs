using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyContacts;

// =========================================================
// FILE: UpdateOperationsCompanyContactsRequest.cs
//
// PURPOSE:
// Saves the company's billing and technical contacts, language and time zone at Vixly (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdateOperationsCompanyContactsRequest
{
    public IReadOnlyList<OperationsCompanyContact> Contacts { get; init; } = Array.Empty<OperationsCompanyContact>();

    public string Language { get; init; } = "en";

    public string TimeZone { get; init; } = string.Empty;
}
