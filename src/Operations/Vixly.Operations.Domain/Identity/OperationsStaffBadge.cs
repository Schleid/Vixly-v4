using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsStaffBadge.cs
//
// PURPOSE:
// A staff member's badge card: scanned on a licensed device to sign in (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Keep the hash of the badge's code, who issued it and when
// - Be revoked once, by hand or when a new badge is printed
//
// IMPORTANT NOTES:
// - The code itself is printed on the card and never stored
// - One badge in use per person (unique filtered index); revoked badges stay as history
// - Bound to its tenant by OperationsEntity
// =========================================================
public sealed class OperationsStaffBadge : OperationsEntity
{
    private OperationsStaffBadge()
    {
        CodeHash = string.Empty;
    }

    private OperationsStaffBadge(Guid tenantId, Guid userId, string codeHash, Guid issuedByUserId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        UserId = userId;
        CodeHash = codeHash;
        IssuedByUserId = issuedByUserId;
    }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; }

    public Guid IssuedByUserId { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public bool IsActive => RevokedUtc is null;

    public static OperationsStaffBadge Issue(Guid tenantId, Guid userId, string codeHash, Guid issuedByUserId, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(OperationsIdentityErrors.UserRequired);
        }

        return new OperationsStaffBadge(tenantId, userId, OperationsTokenHash.Require(codeHash), issuedByUserId, utcNow);
    }

    public bool Revoke(Guid byUserId, DateTimeOffset utcNow)
    {
        if (!IsActive)
        {
            return false;
        }

        RevokedUtc = utcNow;
        RevokedByUserId = byUserId;
        MarkAsModified(utcNow);
        return true;
    }
}
