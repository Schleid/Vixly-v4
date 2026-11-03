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

namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsNotices;

// =========================================================
// FILE: GetMyOperationsNoticesResponse.cs
//
// PURPOSE:
// The answer to getMyOperationsNotices.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsNoticesResponse
{
    private GetMyOperationsNoticesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsNoticeChannel Channel { get; private init; }

    public IReadOnlyList<OperationsNoticeChannel> Offered { get; private init; } = Array.Empty<OperationsNoticeChannel>();

    public string Phone { get; private init; } = string.Empty;

    public static GetMyOperationsNoticesResponse Success(OperationsNoticeChannel channel, IReadOnlyList<OperationsNoticeChannel> offered, string phone)
    {
        return new GetMyOperationsNoticesResponse { Succeeded = true, Channel = channel, Offered = offered, Phone = phone };
    }

    public static GetMyOperationsNoticesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsNoticesResponse { Error = error };
    }
}
