using Vixly.Operations.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsHelpTestReader.cs
// PURPOSE:
// Supplies request-specific readers without a database.
// RESPONSIBILITIES:
// - Allow tests to change company and permissions between loads.
// IMPORTANT NOTES:
// - Never substitutes for production authorization middleware.
// =========================================================
internal sealed class OperationsHelpTestReader : IOperationsHelpReaderSource
{
    public OperationsHelpReaderContext Context { get; set; } = new(new HelpReader([], []), "Example Freight", "https://example.test/customer");

    public Task<OperationsHelpReaderContext> GetAsync(string? company = null) => Task.FromResult(Context);
}
