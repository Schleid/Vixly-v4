namespace Vixly.Operations.Application.Arrivals;

// =========================================================
// FILE: OperationsArrivalPlace.cs
//
// PURPOSE:
// A warehouse that receives dispatches (Slice 7).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsArrivalPlace(Guid Id, string Code, string Name, bool ClearsCustoms)
{
    public string? TimeZone { get; init; }
}
