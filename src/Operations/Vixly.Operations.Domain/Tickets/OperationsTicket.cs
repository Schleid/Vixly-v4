using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tickets;

// =========================================================
// FILE: OperationsTicket.cs
//
// PURPOSE:
// A customer's ticket about a package that the carrier delivered but that is not in its account (Slice 4D step 4; owner, 2026-10-19, A-6).
//
// RESPONSIBILITIES:
// - What the customer knows, the carrier's proof of delivery, and the messages between the customer and staff
// - Staff answer, link the package they found, and resolve it; the customer answers back
//
// IMPORTANT NOTES:
// - Customers never search the warehouse: only staff look, and nothing of the warehouse is shown before staff link the package
// - Written by the customer only for its details (D-C6)
// =========================================================
public sealed class OperationsTicket : OperationsEntity
{
    public const int TextMaxLength = 1000;

    public const int MaxMessages = 100;

    private readonly List<OperationsTicketMessage> _messages = new();

    private OperationsTicket()
    {
        Carrier = string.Empty;
        TrackingNumber = string.Empty;
        Contents = string.Empty;
    }

    private OperationsTicket(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Carrier = string.Empty;
        TrackingNumber = string.Empty;
        Contents = string.Empty;
    }

    public Guid CustomerId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public OperationsTicketStatus Status { get; private set; }

    public string Carrier { get; private set; }

    public string TrackingNumber { get; private set; }

    public string? Store { get; private set; }

    public string? OrderNumber { get; private set; }

    public DateOnly? DeliveredOn { get; private set; }

    public string Contents { get; private set; }

    // The carrier's proof of delivery, in the company's file store (PDF, PNG or JPEG).
    public string? ProofKey { get; private set; }

    public string? ProofContentType { get; private set; }

    public string? ProofSha256 { get; private set; }

    // The package staff found and gave to the customer.
    public Guid? ParcelId { get; private set; }

    public DateTimeOffset? ResolvedUtc { get; private set; }

    // The form that wrote last: the same form sent again changes nothing (ADR-0019).
    public string? LastCommandKey { get; private set; }

    public bool Repeats(string commandKey) => LastCommandKey is not null && string.Equals(LastCommandKey, commandKey, StringComparison.Ordinal);

    public void Keyed(string commandKey) => LastCommandKey = commandKey;

    public IReadOnlyList<OperationsTicketMessage> Messages => _messages;

    public static OperationsTicket Open(Guid tenantId, Guid customerId, Guid warehouseId, OperationsTicketDetails details, string? message, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (customerId == Guid.Empty || warehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsTicketErrors.DetailsInvalid);
        }

        OperationsTicket ticket = new(tenantId, utcNow)
        {
            CustomerId = customerId,
            WarehouseId = warehouseId,
            Status = OperationsTicketStatus.Open,
            Carrier = OperationsReceivingText.Required(details.Carrier, OperationsReceivingLimits.CarrierMaxLength, OperationsTicketErrors.DetailsInvalid),
            TrackingNumber = OperationsCarrierTracking.Normalize(details.TrackingNumber),
            Store = OperationsReceivingText.Optional(details.Store, OperationsReceivingLimits.StoreMaxLength),
            OrderNumber = OperationsReceivingText.Optional(details.OrderNumber, 60),
            DeliveredOn = details.DeliveredOn is { } day && (day > DateOnly.FromDateTime(utcNow.UtcDateTime).AddDays(1) || day < DateOnly.FromDateTime(utcNow.UtcDateTime).AddYears(-1))
                ? throw new DomainException(OperationsTicketErrors.DetailsInvalid)
                : details.DeliveredOn,
            Contents = OperationsReceivingText.Required(details.Contents, OperationsReceivingLimits.ContentsMaxLength, OperationsTicketErrors.DetailsInvalid)
        };

        if (Words(message) is { } first)
        {
            ticket._messages.Add(new OperationsTicketMessage(false, null, first, utcNow));
        }

        return ticket;
    }

    public void AttachProof(string key, string contentType, string sha256, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        (ProofKey, ProofContentType, ProofSha256) = (key, contentType, sha256);
        MarkAsModified(utcNow);
    }

    // The customer writes back: the ticket is open again for staff, even when resolved.
    public void CustomerReply(string? text, DateTimeOffset utcNow)
    {
        Add(false, null, text, utcNow);
        (Status, ResolvedUtc) = (OperationsTicketStatus.Open, null);
    }

    public void StaffReply(Guid userId, string? text, DateTimeOffset utcNow)
    {
        Add(true, userId, text, utcNow);
        Status = Status == OperationsTicketStatus.Resolved ? Status : OperationsTicketStatus.Answered;
    }

    // Resolved, with the package found (now the customer's) or with a last word.
    public void Resolve(Guid userId, Guid? parcelId, string? text, DateTimeOffset utcNow)
    {
        if (Words(text) is not null)
        {
            Add(true, userId, text, utcNow);
        }

        // A ticket links one package: never replaced by another (review).
        if (parcelId is { } linked && ParcelId is { } current && linked != current)
        {
            throw new DomainException(OperationsTicketErrors.AlreadyLinked);
        }

        (Status, ResolvedUtc, ParcelId) = (OperationsTicketStatus.Resolved, utcNow, parcelId ?? ParcelId);
        MarkAsModified(utcNow);
    }

    private void Add(bool fromStaff, Guid? userId, string? text, DateTimeOffset utcNow)
    {
        string words = Words(text) ?? throw new DomainException(OperationsTicketErrors.MessageInvalid);

        if (_messages.Count >= MaxMessages)
        {
            throw new DomainException(OperationsTicketErrors.TooManyMessages);
        }

        _messages.Add(new OperationsTicketMessage(fromStaff, userId, words, utcNow));
        MarkAsModified(utcNow);
    }

    private static string? Words(string? text)
    {
        string? words = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return words is null || (words.Length <= TextMaxLength && !words.Any(character => char.IsControl(character) && character is not '\n' and not '\r'))
            ? words
            : throw new DomainException(OperationsTicketErrors.MessageInvalid);
    }
}
