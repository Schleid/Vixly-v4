using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsPrintPairing.cs
//
// PURPOSE:
// A one-time code that lets a phone running Vixly Print join a warehouse as a print program (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Keep the code's hash, the warehouse, the name to give the phone and who made it
// - Be used once, within 10 minutes
//
// IMPORTANT NOTES:
// - The code is shown once, as a QR code on the Printers page; only its hash is kept
// - Bound to its tenant by OperationsEntity
// =========================================================
public sealed class OperationsPrintPairing : OperationsEntity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private OperationsPrintPairing()
    {
        CodeHash = string.Empty;
        Name = string.Empty;
    }

    private OperationsPrintPairing(Guid tenantId, Guid warehouseId, string codeHash, string name, Guid userId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        WarehouseId = warehouseId;
        CodeHash = codeHash;
        Name = name;
        CreatedByUserId = userId;
        ExpiresUtc = utcNow + Lifetime;
    }

    public Guid WarehouseId { get; private set; }

    public string CodeHash { get; private set; }

    public string Name { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public Guid? AgentId { get; private set; }

    public static OperationsPrintPairing Create(Guid tenantId, Guid warehouseId, string codeHash, string? name, Guid userId, DateTimeOffset utcNow)
    {
        string text = name?.Trim() ?? string.Empty;

        if (text.Length is 0 or > OperationsPrintAgent.NameMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException(OperationsPrintingErrors.NameInvalid);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        return new OperationsPrintPairing(tenantId, warehouseId, codeHash, text, userId, utcNow);
    }

    public bool IsUsableAt(DateTimeOffset utcNow) => UsedUtc is null && utcNow < ExpiresUtc;

    public void Use(Guid agentId, DateTimeOffset utcNow)
    {
        if (!IsUsableAt(utcNow))
        {
            throw new DomainException(OperationsPrintingErrors.PairingInvalid);
        }

        (UsedUtc, AgentId) = (utcNow, agentId);
        MarkAsModified(utcNow);
    }
}
