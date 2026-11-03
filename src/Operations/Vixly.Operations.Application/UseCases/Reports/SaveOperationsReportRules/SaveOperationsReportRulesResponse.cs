using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportRules;

// =========================================================
// FILE: SaveOperationsReportRulesResponse.cs
//
// PURPOSE:
// The answer to saveOperationsReportRules.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsReportRulesResponse
{
    private SaveOperationsReportRulesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsReportRulesResponse Success()
    {
        return new SaveOperationsReportRulesResponse { Succeeded = true };
    }

    public static SaveOperationsReportRulesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsReportRulesResponse { Error = error };
    }
}
