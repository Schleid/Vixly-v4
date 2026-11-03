namespace Vixly.Operations.Web.Features.Help;

// =========================================================
// FILE: IOperationsHelpReaderSource.cs
//
// PURPOSE:
// Supplies request-owned reader rules to the help presentation.
//
// RESPONSIBILITIES:
// - Reuse the menu's authoritative effective permissions.
//
// IMPORTANT NOTES:
// - No HTTP or Blazor types enter the presenter through this port.
// =========================================================
public interface IOperationsHelpReaderSource
{
    Task<OperationsHelpReaderContext> GetAsync(string? company = null);
}
