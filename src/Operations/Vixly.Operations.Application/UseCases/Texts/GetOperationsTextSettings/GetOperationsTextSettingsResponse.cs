using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Texts;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Texts;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Texts.GetOperationsTextSettings;

// =========================================================
// FILE: GetOperationsTextSettingsResponse.cs
//
// PURPOSE:
// The answer to getOperationsTextSettings.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsTextSettingsResponse
{
    private GetOperationsTextSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool InPlan { get; private init; }

    public OperationsTextProvider? Provider { get; private init; }

    public OperationsTextNotices Notices { get; private init; }

    public IReadOnlyList<OperationsTextMessage> Messages { get; private init; } = Array.Empty<OperationsTextMessage>();

    public bool CanEdit { get; private init; }

    public static GetOperationsTextSettingsResponse Success(bool inPlan, OperationsTextProvider? provider, OperationsTextNotices notices, IReadOnlyList<OperationsTextMessage> messages, bool canEdit)
    {
        return new GetOperationsTextSettingsResponse { Succeeded = true, InPlan = inPlan, Provider = provider, Notices = notices, Messages = messages, CanEdit = canEdit };
    }

    public static GetOperationsTextSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsTextSettingsResponse { Error = error };
    }
}
