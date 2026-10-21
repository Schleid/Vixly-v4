using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsPrintAgent.cs
//
// PURPOSE:
// The print program running on a computer at a warehouse dock (Slice 3B).
//
// RESPONSIBILITIES:
// - Keep the hash of its key, when it was last seen, and whether it may still collect jobs
//
// IMPORTANT NOTES:
// - Only the SHA-256 hash of the key is stored; the key is shown once when created
// - A stopped agent collects nothing
// =========================================================
public sealed class OperationsPrintAgent : OperationsEntity
{
    public const int NameMaxLength = 60;

    private OperationsPrintAgent()
    {
        Name = string.Empty;
        KeyHash = string.Empty;
    }

    private OperationsPrintAgent(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
        KeyHash = string.Empty;
    }

    public Guid WarehouseId { get; private set; }

    public string Name { get; private set; }

    public string KeyHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastSeenUtc { get; private set; }

    public static OperationsPrintAgent Register(Guid tenantId, Guid warehouseId, string? name, string keyHash, DateTimeOffset utcNow)
    {
        string text = name?.Trim() ?? string.Empty;

        if (text.Length is 0 or > NameMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0)
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(OperationsPrintingErrors.NameInvalid);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(keyHash);
        return new(tenantId, utcNow) { WarehouseId = warehouseId, Name = text, KeyHash = keyHash, IsActive = true };
    }

    public void Seen(DateTimeOffset utcNow) => LastSeenUtc = utcNow;

    public void Stop(DateTimeOffset utcNow)
    {
        IsActive = false;
        MarkAsModified(utcNow);
    }
}
