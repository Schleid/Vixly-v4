using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Application.Texts;

// =========================================================
// FILE: OperationsTextDesk.cs
//
// PURPOSE:
// Sends a customer's notice by SMS or WhatsApp when they chose it, the company sends that notice by text, its plan includes it and its Twilio
// account is connected (Slice 9; owner, 2026-10-22).
//
// RESPONSIBILITIES:
// - Say which notice an email kind is; word it; send it through the company's own account; keep a line in the log
// - A customer who replied STOP gets notices by email from now on
//
// IMPORTANT NOTES:
// - Returns false whenever the text was not sent: the caller sends the email instead, so a notice is never lost
// - Its log line is saved on its own (IOperationsTextLog), never with or against the caller's work (review)
// =========================================================
public sealed class OperationsTextDesk
{
    private readonly IOperationsTextRepository _texts;

    private readonly IOperationsTextGateway _gateway;

    private readonly IOperationsTextWriter _writer;

    private readonly IOperationsSecretProtector _protector;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsPlanFeatures _features;

    private readonly IOperationsTextLog _log;

    private readonly TimeProvider _timeProvider;

    public OperationsTextDesk(IOperationsTextRepository texts, IOperationsTextGateway gateway, IOperationsTextWriter writer, IOperationsSecretProtector protector,
        IOperationsSettingsRepository settings, IOperationsPlanFeatures features, IOperationsTextLog log, TimeProvider timeProvider)
    {
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public IOperationsTextRepository Texts => _texts;

    public IOperationsTextGateway Gateway => _gateway;

    public static OperationsTextNotices NoticeOf(OperationsCustomerMailKind kind) => kind switch
    {
        OperationsCustomerMailKind.ParcelReceived => OperationsTextNotices.ParcelReceived,
        OperationsCustomerMailKind.ParcelActionNeeded or OperationsCustomerMailKind.ParcelReminder or OperationsCustomerMailKind.ParcelFinalNotice => OperationsTextNotices.ActionNeeded,
        OperationsCustomerMailKind.ReadyForPickup => OperationsTextNotices.ReadyForPickup,
        OperationsCustomerMailKind.PickupCode => OperationsTextNotices.PickupCode,
        OperationsCustomerMailKind.PickupReminder or OperationsCustomerMailKind.PickupFinalNotice => OperationsTextNotices.PickupReminder,
        OperationsCustomerMailKind.OutForDelivery => OperationsTextNotices.OutForDelivery,
        OperationsCustomerMailKind.OutForDeliveryCode => OperationsTextNotices.DeliveryCode,
        OperationsCustomerMailKind.ShipmentUpdate => OperationsTextNotices.ShipmentUpdate,
        OperationsCustomerMailKind.PaymentNotReceived => OperationsTextNotices.PaymentNotReceived,
        _ => OperationsTextNotices.None
    };

    public async Task<bool> InPlanAsync(Guid tenantId, CancellationToken cancellationToken) => (await _features.GetAsync(tenantId, cancellationToken)).TextMessages;

    // The channels the company offers its customers now (for the portal): its plan, its account, a sender, and at least one notice.
    public async Task<IReadOnlyList<OperationsNoticeChannel>> OfferedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        List<OperationsNoticeChannel> offered = new() { OperationsNoticeChannel.Email };

        if (!(await _features.GetAsync(tenantId, cancellationToken)).TextMessages || (await _settings.FindAsync(cancellationToken))?.TextNotices is null or OperationsTextNotices.None
            || await _texts.GetProviderAsync(cancellationToken) is not { } provider)
        {
            return offered;
        }

        if (provider.SendsSms)
        {
            offered.Add(OperationsNoticeChannel.Sms);
        }

        if (provider.SendsWhatsApp && provider.Templates.Count > 0)
        {
            offered.Add(OperationsNoticeChannel.WhatsApp);
        }

        return offered;
    }

    // Codes reach the customer by email as well as by text (review).
    public static bool AlsoByEmail(OperationsCustomerMailKind kind) => kind is OperationsCustomerMailKind.PickupCode or OperationsCustomerMailKind.OutForDeliveryCode;

    public async Task<bool> TrySendAsync(OperationsCustomer customer, OperationsCustomerMailKind kind, string company, string link, IReadOnlyDictionary<string, string> extra,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(extra);
        OperationsTextNotices notice = NoticeOf(kind);
        OperationsTextProvider provider;
        string token;
        string sentence;

        // Anything wrong before the text goes (review): the email goes instead.
        try
        {
            if (notice == OperationsTextNotices.None || customer.NoticeChannel == OperationsNoticeChannel.Email || string.IsNullOrWhiteSpace(customer.Phone)
                || !(await _features.GetAsync(customer.TenantId, cancellationToken)).TextMessages
                || await _settings.FindAsync(cancellationToken) is not { } settings || !settings.TextNotices.HasFlag(notice)
                || await _texts.GetProviderAsync(cancellationToken) is not { } found || found.TenantId != customer.TenantId
                || _protector.Unprotect(found.AuthTokenProtected, OperationsSecretPurposes.TwilioToken) is not { } unprotected)
            {
                return false;
            }

            (provider, token) = (found, unprotected);
            sentence = _writer.Sentence(notice, customer.PreferredLanguage, extra);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }

        string? providerId;

        try
        {
            if (customer.NoticeChannel == OperationsNoticeChannel.Sms && provider.SmsFrom is { } from)
            {
                providerId = await _gateway.SendSmsAsync(provider.AccountSid, token, from, customer.Phone, company + ": " + sentence + " " + link, provider.CallbackUrl,
                    cancellationToken);
            }
            else if (customer.NoticeChannel == OperationsNoticeChannel.WhatsApp && provider.WhatsAppFrom is { } sender && provider.Template(notice) is { } template)
            {
                providerId = await _gateway.SendWhatsAppAsync(provider.AccountSid, token, sender, customer.Phone, template, new[] { company, sentence, link }, provider.CallbackUrl,
                    cancellationToken);
            }
            else
            {
                return false;
            }
        }
        catch (OperationsTextRefusedException refused)
        {
            // STOP: the customer's choice is taken back to email, and kept on record (on its own: never undoes the caller's work).
            await _log.RecordAsync(Line(customer, notice, refused.OptedOut ? OperationsTextStatus.OptedOut : OperationsTextStatus.Failed, null, refused.Code), refused.OptedOut,
                cancellationToken);
            return false;
        }
        catch (OperationsTextUnavailableException)
        {
            await _log.RecordAsync(Line(customer, notice, OperationsTextStatus.Failed, null, "unavailable"), false, cancellationToken);
            return false;
        }

        // Sent: whatever happens to the log line, the text is not sent again.
        await _log.RecordAsync(Line(customer, notice, OperationsTextStatus.Sent, providerId, null), false, cancellationToken);
        return true;
    }

    private OperationsTextMessage Line(OperationsCustomer customer, OperationsTextNotices notice, OperationsTextStatus status, string? providerId, string? error) =>
        OperationsTextMessage.Record(customer.TenantId, customer.Id, customer.NoticeChannel, notice, customer.Phone, status, providerId, error, _timeProvider.GetUtcNow());
}
