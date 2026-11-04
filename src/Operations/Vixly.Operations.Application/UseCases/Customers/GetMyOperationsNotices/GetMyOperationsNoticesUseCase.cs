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
// FILE: GetMyOperationsNoticesUseCase.cs
//
// PURPOSE:
// How the signed-in customer gets the company’s notices, and the channels the company offers (Slice 9).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The customer’s own session only
// =========================================================
public sealed class GetMyOperationsNoticesUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsTextDesk _texts;

    public GetMyOperationsNoticesUseCase(
        OperationsCustomerAccounts accounts,
        OperationsTextDesk texts)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
    }

    public async Task<GetMyOperationsNoticesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsNoticesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsNoticesResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        IReadOnlyList<OperationsNoticeChannel> offered = await _texts.OfferedAsync(signedIn.Customer.TenantId, cancellationToken);
        return GetMyOperationsNoticesResponse.Success(offered.Contains(signedIn.Customer.NoticeChannel) ? signedIn.Customer.NoticeChannel : OperationsNoticeChannel.Email, offered,
            signedIn.Customer.Phone);
    }
}
