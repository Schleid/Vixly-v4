namespace Vixly.Operations.Application.UseCases.Receiving.ListMyOperationsPackages;

// =========================================================
// FILE: ListMyOperationsPackagesRequest.cs
//
// PURPOSE:
// A customer's pre-alerts and packages, newest first (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListMyOperationsPackagesRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
