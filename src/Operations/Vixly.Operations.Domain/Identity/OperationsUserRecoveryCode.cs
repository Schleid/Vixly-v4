using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsUserRecoveryCode.cs
//
// PURPOSE:
// One single-use recovery code of a staff member, hashed (Slice 1).
//
// RESPONSIBILITIES:
// - Hold the hash and when it was used
//
// IMPORTANT NOTES:
// - Ten per user, replaced together
// =========================================================
public sealed class OperationsUserRecoveryCode : OperationsEntity
{
    private OperationsUserRecoveryCode()
    {
        CodeHash = string.Empty;
    }

    internal OperationsUserRecoveryCode(Guid tenantId, Guid userId, string codeHash, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        UserId = userId;
        CodeHash = codeHash;
    }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public bool IsUsed => UsedUtc is not null;

    internal void MarkUsed(DateTimeOffset utcNow)
    {
        UsedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
