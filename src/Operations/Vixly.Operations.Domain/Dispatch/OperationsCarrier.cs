using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsCarrier.cs
//
// PURPOSE:
// One of the company's carriers: an airline, a forwarder, an ocean line or a trucking company (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Hold its name, kind, account number and, for an airline, its three-digit prefix
// - Be switched off without losing its history
//
// IMPORTANT NOTES:
// - Its rates are OperationsCarrierRate rows
// =========================================================
public sealed class OperationsCarrier : OperationsEntity
{
    public const int NameMaxLength = 80;

    public const int AccountMaxLength = 40;

    private OperationsCarrier()
    {
        Name = string.Empty;
    }

    private OperationsCarrier(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public OperationsCarrierKind Kind { get; private set; }

    public string? AccountNumber { get; private set; }

    // The airline's three-digit prefix of its air waybill numbers (air only).
    public string? AirlinePrefix { get; private set; }

    public bool IsActive { get; private set; }

    public static OperationsCarrier Create(Guid tenantId, string? name, OperationsCarrierKind kind, string? accountNumber, string? airlinePrefix, DateTimeOffset utcNow)
    {
        OperationsCarrier carrier = new(tenantId, utcNow) { IsActive = true };
        carrier.Change(name, kind, accountNumber, airlinePrefix, true, utcNow);
        return carrier;
    }

    public void Change(string? name, OperationsCarrierKind kind, string? accountNumber, string? airlinePrefix, bool active, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(OperationsDispatchErrors.CarrierInvalid);
        }

        string? prefix = OperationsDispatchText.Optional(airlinePrefix, 3);

        if (prefix is not null && (kind != OperationsCarrierKind.Air || prefix.Length != 3 || !prefix.All(char.IsAsciiDigit)))
        {
            throw new DomainException(OperationsDispatchErrors.PrefixInvalid);
        }

        (Name, Kind, AccountNumber, AirlinePrefix, IsActive) = (OperationsDispatchText.Required(name, NameMaxLength, OperationsDispatchErrors.CarrierInvalid), kind,
            OperationsDispatchText.Optional(accountNumber, AccountMaxLength), prefix, active);
        MarkAsModified(utcNow);
    }
}
