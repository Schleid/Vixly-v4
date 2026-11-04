using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsQuote.cs
//
// PURPOSE:
// A quote made by staff for a customer, with an optional preferred price (owner, Slice 4: "offer a preferred price to a valued customer").
//
// RESPONSIBILITIES:
// - Keep the number, customer, service, destination, pieces and the lines as priced
// - Keep the standard total, the preferred total and its reason, and the validity
// - Say whether it is still valid
//
// IMPORTANT NOTES:
// - The preferred price lowers the total as its own line; government fees are never discounted (O-10)
// - Withdrawn, never deleted
// =========================================================
public sealed class OperationsQuote : OperationsEntity
{
    public const int DefaultValidDays = 14;

    public const int MaxValidDays = 90;

    private readonly List<OperationsQuotePiece> _pieces = new();

    private readonly List<OperationsQuoteLine> _lines = new();

    private OperationsQuote()
    {
        Number = string.Empty;
        ServiceCode = string.Empty;
        CurrencyCode = string.Empty;
    }

    private OperationsQuote(Guid tenantId, string number, Guid customerId, string serviceCode, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (Number, CustomerId, ServiceCode) = (number, customerId, serviceCode);
        CurrencyCode = string.Empty;
    }

    public string Number { get; private set; }

    public Guid CustomerId { get; private set; }

    public string ServiceCode { get; private set; }

    public string? CountryCode { get; private set; }

    // Slice 4C: the route the quote was priced for.
    public Guid? OriginWarehouseId { get; private set; }

    public Guid? DestinationWarehouseId { get; private set; }

    public bool Consolidated { get; private set; }

    public bool Insured { get; private set; }

    public IReadOnlyList<OperationsQuotePiece> Pieces => _pieces;

    public IReadOnlyList<OperationsQuoteLine> Lines => _lines;

    public decimal StandardTotal { get; private set; }

    public decimal Total { get; private set; }

    public string CurrencyCode { get; private set; }

    public string? PreferredReason { get; private set; }

    public DateTimeOffset ValidUntilUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    // The form's one-time reference: the same save sent twice gives one quote (ADR-0019).
    public string? RequestKey { get; private set; }

    public OperationsQuoteStatus Status { get; private set; }

    public bool IsPreferred => Total != StandardTotal;

    // The lines come from the rating engine; a preferred total below the standard one adds its own line.
    public static OperationsQuote Create(Guid tenantId, string number, Guid customerId, string serviceCode, string? countryCode, bool consolidated, bool insured,
        IReadOnlyList<OperationsQuotePiece> pieces, IReadOnlyList<OperationsQuoteLine> lines, string currencyCode, decimal? preferredTotal, string? reason, int validDays,
        Guid createdByUserId, DateTimeOffset utcNow, string? requestKey = null, Guid? originWarehouseId = null, Guid? destinationWarehouseId = null)
    {
        ArgumentNullException.ThrowIfNull(pieces);
        ArgumentNullException.ThrowIfNull(lines);

        if (validDays is < 1 or > MaxValidDays)
        {
            throw new DomainException(OperationsChargeErrors.DaysInvalid);
        }

        if (pieces.Count is 0 or > OperationsChargeLimits.MaxPieces || lines.Count == 0)
        {
            throw new DomainException(OperationsChargeErrors.PiecesInvalid);
        }

        OperationsQuote quote = new(tenantId, number, customerId, serviceCode, utcNow)
        {
            CountryCode = countryCode,
            Consolidated = consolidated,
            Insured = insured,
            CurrencyCode = currencyCode,
            CreatedByUserId = createdByUserId,
            RequestKey = requestKey,
            OriginWarehouseId = originWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            ValidUntilUtc = utcNow.AddDays(validDays),
            Status = OperationsQuoteStatus.Open
        };
        quote._pieces.AddRange(pieces);
        quote._lines.AddRange(lines);
        quote.StandardTotal = lines.Sum(line => line.Amount);
        quote.Total = quote.StandardTotal;

        if (preferredTotal is { } preferred && preferred != quote.StandardTotal)
        {
            string text = OperationsChargeLimits.Text(reason, OperationsChargeLimits.ReasonMaxLength, OperationsChargeErrors.TextInvalid)
                ?? throw new DomainException(OperationsChargeErrors.ReasonRequired);
            decimal floor = lines.Where(line => line.IsGovernmentFee).Sum(line => line.Amount);

            // Lower only, never below the government fees passed through.
            if (preferred > quote.StandardTotal || preferred < floor || decimal.Round(preferred, 2) != preferred)
            {
                throw new DomainException(OperationsChargeErrors.PreferredInvalid);
            }

            quote._lines.Add(new OperationsQuoteLine(OperationsChargeLineKind.Preferred, null, null, 1, null, preferred - quote.StandardTotal, preferred - quote.StandardTotal, false));
            quote.Total = preferred;
            quote.PreferredReason = text;
        }

        return quote;
    }

    public bool IsValid(DateTimeOffset utcNow) => Status == OperationsQuoteStatus.Open && utcNow <= ValidUntilUtc;

    public void Withdraw(DateTimeOffset utcNow)
    {
        if (Status == OperationsQuoteStatus.Open)
        {
            Status = OperationsQuoteStatus.Withdrawn;
            MarkAsModified(utcNow);
        }
    }
}
