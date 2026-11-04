using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.GetOperationsReportRules;

// =========================================================
// FILE: GetOperationsReportRulesResponse.cs
//
// PURPOSE:
// The answer to getOperationsReportRules.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsReportRulesResponse
{
    private GetOperationsReportRulesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int DispatchGraceHours { get; private init; }

    public IReadOnlyList<OperationsShippingServiceSetup> Services { get; private init; } = Array.Empty<OperationsShippingServiceSetup>();

    public bool CanEdit { get; private init; }

    public static GetOperationsReportRulesResponse Success(int dispatchGraceHours, IReadOnlyList<OperationsShippingServiceSetup> services, bool canEdit)
    {
        return new GetOperationsReportRulesResponse { Succeeded = true, DispatchGraceHours = dispatchGraceHours, Services = services, CanEdit = canEdit };
    }

    public static GetOperationsReportRulesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsReportRulesResponse { Error = error };
    }
}
