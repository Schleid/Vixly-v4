namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsInternalOptions.cs
//
// PURPOSE:
// Settings of the Operations internal channel (section OperationsInternal).
//
// RESPONSIBILITIES:
// - Hold the allowed host names and callers
//
// IMPORTANT NOTES:
// - A request to any other host name gets 404 (cross-plane rule 3)
// =========================================================
public sealed class OperationsInternalOptions
{
    public const string SectionName = "OperationsInternal";

    public string[] Hosts { get; set; } = Array.Empty<string>();

    public Dictionary<string, OperationsCallerOptions> Callers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
