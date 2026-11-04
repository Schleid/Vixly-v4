using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Texts;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Application.UseCases.Texts.HandleOperationsTextStatus;

// =========================================================
// FILE: HandleOperationsTextStatusUseCase.cs
//
// PURPOSE:
// Takes Twilio's report of what became of a text message (Slice 9; review): delivered, or not delivered after Twilio accepted it.
//
// RESPONSIBILITIES:
// - Trust it only when signed with the company's own token, at the address kept for it
// - Mark the log line; a number that can never get texts (STOP, unknown, a landline, blocked) takes the customer back to email
//
// IMPORTANT NOTES:
// - No signed-in person: the tenant comes from the host
// - Other statuses and unknown messages are answered OK so Twilio stops sending them
// - Codes are on their way by email too, so an undelivered text never leaves a customer without one
// =========================================================
public sealed class HandleOperationsTextStatusUseCase
{
    // Twilio's codes for a number that will not take texts again: replied STOP, unknown or unreachable for good, a landline, not on WhatsApp.
    private static readonly HashSet<string> Unreachable = new(StringComparer.Ordinal) { "21610", "21211", "21614", "30004", "30005", "30006", "63024" };

    private readonly OperationsTextDesk _texts;

    private readonly IOperationsSecretProtector _protector;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public HandleOperationsTextStatusUseCase(
        OperationsTextDesk texts,
        IOperationsSecretProtector protector,
        IOperationsCustomerRepository customers,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<HandleOperationsTextStatusResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        HandleOperationsTextStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Anonymous, from Twilio: trusted only when signed with the company's own token, for the address kept at connection.
        if (context.TenantId is not { } tenantId || await _texts.Texts.GetProviderAsync(cancellationToken) is not { } provider || provider.TenantId != tenantId
            || provider.CallbackUrl is not { } url || !string.Equals(url, request.Url, StringComparison.Ordinal)
            || _protector.Unprotect(provider.AuthTokenProtected, OperationsSecretPurposes.TwilioToken) is not { } token
            || !_texts.Gateway.SignatureValid(token, request.Url, request.Form, request.Signature))
        {
            return HandleOperationsTextStatusResponse.Failure(OperationsTextErrors.ProviderRefused);
        }

        string? Field(string name) => request.Form.FirstOrDefault(item => item.Key == name).Value;
        OperationsTextStatus? status = Field("MessageStatus") switch
        {
            "delivered" or "read" => OperationsTextStatus.Delivered,
            "undelivered" or "failed" => OperationsTextStatus.Undelivered,
            _ => null
        };
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (status is not { } reported || Field("MessageSid") is not { Length: > 0 and <= 64 } sid || await _texts.Texts.FindMessageAsync(sid, cancellationToken) is not { } message
            || !message.Report(reported, Field("ErrorCode") is { Length: > 0 } code ? code : null, utcNow))
        {
            return HandleOperationsTextStatusResponse.Success();
        }

        if (reported == OperationsTextStatus.Undelivered && message.ErrorCode is { } error && Unreachable.Contains(error)
            && await _customers.GetAsync(message.CustomerId, cancellationToken) is { } customer && customer.NoticeChannel == message.Channel)
        {
            customer.ChangeNoticeChannel(OperationsNoticeChannel.Email, utcNow);
            _audit.Record(null, "ops.customers.texts_stopped", nameof(OperationsCustomer), customer.Id, before: new { channel = message.Channel.ToString(), code = error });
        }

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return HandleOperationsTextStatusResponse.Failure(OperationsTextErrors.ProviderUnavailable);
        }

        return HandleOperationsTextStatusResponse.Success();
    }
}
