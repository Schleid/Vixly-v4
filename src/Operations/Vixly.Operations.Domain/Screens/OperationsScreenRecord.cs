using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Screens;

// =========================================================
// FILE: OperationsScreenRecord.cs
//
// PURPOSE:
// The customer's own acceptance, made on a customer screen (Slice 5F step 3, P-9).
//
// RESPONSIBILITIES:
// - Keep what was accepted or signed, when, on which screen, and the clerk who sent it
//
// IMPORTANT NOTES:
// - Never changed once made
// - A signature on the screen is the shipment's own signature (Slice 5D, RecordSignature); this keeps acceptances
// =========================================================
public sealed class OperationsScreenRecord : OperationsEntity
{
    private OperationsScreenRecord()
    {
        ScreenName = string.Empty;
        Language = string.Empty;
    }

    private OperationsScreenRecord(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        ScreenName = string.Empty;
        Language = string.Empty;
    }

    // An acceptance counts at the cashier only this long (review): the customer at the counter now, not one from weeks ago.
    public static readonly TimeSpan ZelleTermsLifetime = TimeSpan.FromHours(4);

    public OperationsScreenRecordKind Kind { get; private set; }

    public Guid ScreenId { get; private set; }

    public string ScreenName { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? InvoiceId { get; private set; }

    public Guid? ClerkUserId { get; private set; }

    // The language the customer read it in.
    public string Language { get; private set; }

    // The terms the customer read (SHA-256): a change of the company's terms makes the acceptance count no more.
    public string TermsHash { get; private set; } = string.Empty;

    public static OperationsScreenRecord ZelleTerms(OperationsCustomerScreen screen, Guid invoiceId, string language, string termsHash, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentException.ThrowIfNullOrWhiteSpace(termsHash);
        return new OperationsScreenRecord(screen.TenantId, utcNow)
        {
            Kind = OperationsScreenRecordKind.ZelleTerms, ScreenId = screen.Id, ScreenName = screen.Name, WarehouseId = screen.WarehouseId, InvoiceId = invoiceId,
            ClerkUserId = screen.SentByUserId, Language = language, TermsHash = termsHash
        };
    }
}
