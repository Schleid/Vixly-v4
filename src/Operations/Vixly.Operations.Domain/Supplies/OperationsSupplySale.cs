using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplySale.cs
//
// PURPOSE:
// Supplies sold to a customer at the counter (Slice 5E, S-11 to S-19): on a shipment being processed, or ordered on their own.
//
// RESPONSIBILITIES:
// - Keep the customer, the counter's warehouse, the shipment when it is part of one, the lines and the totals
// - Add, count, discount and remove lines while open; confirm (numbered when ordered on its own) or cancel
//
// IMPORTANT NOTES:
// - A shipment's supplies go on the shipment's invoice; an order on its own has its own invoice (owner, F-1)
// - The stock moves when the sale is confirmed (StockMoved), and comes back when a confirmed sale is cancelled
// =========================================================
public sealed class OperationsSupplySale : OperationsEntity
{
    public const int MaxLines = 50;

    private readonly List<OperationsSupplySaleLine> _lines = new();

    private OperationsSupplySale()
    {
        CurrencyCode = string.Empty;
    }

    private OperationsSupplySale(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        CurrencyCode = string.Empty;
    }

    public Guid CustomerId { get; private set; }

    public Guid WarehouseId { get; private set; }

    // Null: ordered on its own (Order supplies).
    public Guid? ShipmentId { get; private set; }

    // Given when an order on its own is confirmed (the company's prefix).
    public string? Number { get; private set; }

    public OperationsSupplySaleStatus Status { get; private set; }

    public string CurrencyCode { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset? ConfirmedUtc { get; private set; }

    public bool StockMoved { get; private set; }

    public IReadOnlyList<OperationsSupplySaleLine> Lines => _lines;

    // The key of the last form applied: a scan or a change sent twice (a double click) is applied once (ADR-0019).
    public Guid? LastKey { get; private set; }

    public bool Applied(Guid key) => key != Guid.Empty && LastKey == key;

    public void Keyed(Guid key) => LastKey = key == Guid.Empty ? LastKey : key;

    public decimal Gross => _lines.Sum(line => line.Gross);

    public decimal DiscountTotal => _lines.Sum(line => line.Discount);

    public decimal Total => _lines.Sum(line => line.Amount);

    public static OperationsSupplySale Start(Guid tenantId, Guid customerId, Guid warehouseId, Guid? shipmentId, string currencyCode, Guid byUserId, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        if (customerId == Guid.Empty || warehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsSupplyStockErrors.WarehouseInvalid);
        }

        return new OperationsSupplySale(tenantId, utcNow)
        {
            CustomerId = customerId, WarehouseId = warehouseId, ShipmentId = shipmentId, CurrencyCode = currencyCode, CreatedByUserId = byUserId, Status = OperationsSupplySaleStatus.Open
        };
    }

    // One more of a supply (a scan), or a quantity: the same supply again adds to its line. Returns the line.
    public OperationsSupplySaleLine Add(OperationsSupply supply, int quantity, Guid priceId, decimal unitPriceForTotal, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(supply);
        EnsureOpen();
        OperationsSupplySaleLine? line = _lines.FirstOrDefault(item => item.SupplyId == supply.Id);

        if (line is null)
        {
            if (_lines.Count >= MaxLines)
            {
                throw new DomainException(OperationsSupplySaleErrors.TooManyLines);
            }

            line = new OperationsSupplySaleLine(supply.Id, supply.Code, supply.NameEn, supply.IsTaxed, priceId);
            line.Count(quantity, unitPriceForTotal);
            _lines.Add(line);
        }
        else
        {
            line.Count(line.Quantity + quantity, unitPriceForTotal);
        }

        MarkAsModified(utcNow);
        return line;
    }

    public void Count(Guid lineId, int quantity, decimal unitPrice, DateTimeOffset utcNow)
    {
        EnsureOpen();
        Line(lineId).Count(quantity, unitPrice);
        MarkAsModified(utcNow);
    }

    public void Discount(Guid lineId, decimal percent, string? reason, Guid? byUserId, Guid? approvedByUserId, DateTimeOffset utcNow)
    {
        EnsureOpen();
        Line(lineId).Discounted(percent, reason, byUserId, approvedByUserId);
        MarkAsModified(utcNow);
    }

    public void Remove(Guid lineId, DateTimeOffset utcNow)
    {
        EnsureOpen();
        _lines.Remove(Line(lineId));
        MarkAsModified(utcNow);
    }

    public OperationsSupplySaleLine Line(Guid lineId) => _lines.FirstOrDefault(line => line.Id == lineId) ?? throw new DomainException(OperationsSupplySaleErrors.LineNotFound);

    // Confirmed with its stock moved (or not, when the company does not count it); an order on its own gets its number.
    public void Confirm(string? number, bool stockMoved, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (_lines.Count == 0)
        {
            throw new DomainException(OperationsSupplySaleErrors.Empty);
        }

        (Status, ConfirmedUtc, StockMoved) = (OperationsSupplySaleStatus.Confirmed, utcNow, stockMoved);
        Number = ShipmentId is null ? number : null;
        MarkAsModified(utcNow);
    }

    // Returns whether the stock must come back (it had moved).
    public bool Cancel(DateTimeOffset utcNow)
    {
        if (Status == OperationsSupplySaleStatus.Cancelled)
        {
            throw new DomainException(OperationsSupplySaleErrors.NotOpen);
        }

        bool back = StockMoved;
        (Status, StockMoved) = (OperationsSupplySaleStatus.Cancelled, false);
        MarkAsModified(utcNow);
        return back;
    }

    private void EnsureOpen()
    {
        if (Status != OperationsSupplySaleStatus.Open)
        {
            throw new DomainException(OperationsSupplySaleErrors.NotOpen);
        }
    }
}
