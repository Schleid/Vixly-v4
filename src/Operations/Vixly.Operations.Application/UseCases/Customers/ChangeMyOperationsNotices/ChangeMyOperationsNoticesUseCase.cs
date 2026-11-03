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

namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsNotices;

// =========================================================
// FILE: ChangeMyOperationsNoticesUseCase.cs
//
// PURPOSE:
// The signed-in customer chooses how the company’s notices reach them (Slice 9): email, SMS or WhatsApp, with their consent to texts.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The customer’s own session only; a text channel needs their consent and a channel the company offers
// - Replying STOP takes them back to email (OperationsTextDesk)
// =========================================================
public sealed class ChangeMyOperationsNoticesUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsTextDesk _texts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ChangeMyOperationsNoticesUseCase(
        OperationsCustomerAccounts accounts,
        OperationsTextDesk texts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeMyOperationsNoticesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeMyOperationsNoticesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ChangeMyOperationsNoticesResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (!(await _texts.OfferedAsync(signedIn.Customer.TenantId, cancellationToken)).Contains(request.Channel))
        {
            return ChangeMyOperationsNoticesResponse.Failure(OperationsTextErrors.ChannelUnavailable);
        }

        if (request.Channel != OperationsNoticeChannel.Email && !request.Consent)
        {
            return ChangeMyOperationsNoticesResponse.Failure(OperationsTextErrors.ConsentRequired);
        }

        OperationsNoticeChannel before = signedIn.Customer.NoticeChannel;
        signedIn.Customer.ChangeNoticeChannel(request.Channel, _accounts.UtcNow);
        _audit.Record(null, "ops.customers.notices_changed", nameof(OperationsCustomer), signedIn.Customer.Id,
            before: new { channel = before.ToString() }, after: new { channel = request.Channel.ToString(), consent = request.Channel != OperationsNoticeChannel.Email });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeMyOperationsNoticesResponse.Success()
            : ChangeMyOperationsNoticesResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
