namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsCountryContact;

// =========================================================
// FILE: SaveOperationsCountryContactRequest.cs
//
// PURPOSE:
// Creates or changes how customers in one country reach the tenant (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsCountryContactRequest
{
    public Guid? ContactId { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public string? Label { get; init; }

    // One or more numbers that all reach the call centre.
    public IReadOnlyList<string> Phones { get; init; } = Array.Empty<string>();

    public string? WhatsApp { get; init; }

    public string? Email { get; init; }

    public string? Hours { get; init; }

    public int SortOrder { get; init; }
}
