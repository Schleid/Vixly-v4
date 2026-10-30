using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsStorageLocation.cs
//
// PURPOSE:
// A shelf or area of a warehouse (ADR-0022 putaway).
//
// RESPONSIBILITIES:
// - A code unique in its warehouse, a zone, active or not
//
// IMPORTANT NOTES:
// - Codes are capitals, digits and dashes, for example A-03-2
// =========================================================
public sealed class OperationsStorageLocation : OperationsEntity
{
    private static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9-]{0,19}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private OperationsStorageLocation()
    {
        Code = string.Empty;
    }

    private OperationsStorageLocation(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Code = string.Empty;
    }

    public Guid WarehouseId { get; private set; }

    public string Code { get; private set; }

    public OperationsStorageZone Zone { get; private set; }

    public bool IsActive { get; private set; }

    public static OperationsStorageLocation Create(Guid tenantId, Guid warehouseId, string? code, OperationsStorageZone zone, DateTimeOffset utcNow) =>
        new(tenantId, utcNow) { WarehouseId = warehouseId, Code = CheckCode(code), Zone = CheckZone(zone), IsActive = true };

    public void Change(OperationsStorageZone zone, bool active, DateTimeOffset utcNow)
    {
        Zone = CheckZone(zone);
        IsActive = active;
        MarkAsModified(utcNow);
    }

    public static string CheckCode(string? code)
    {
        string text = code?.Trim().ToUpperInvariant() ?? string.Empty;
        return CodePattern.IsMatch(text) ? text : throw new DomainException(OperationsReceivingErrors.LocationCodeInvalid);
    }

    private static OperationsStorageZone CheckZone(OperationsStorageZone zone) =>
        Enum.IsDefined(zone) ? zone : throw new DomainException(OperationsReceivingErrors.LocationCodeInvalid);
}
