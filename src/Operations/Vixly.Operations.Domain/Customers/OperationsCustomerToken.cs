using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Customers;

// =========================================================
// FILE: OperationsCustomerToken.cs
//
// PURPOSE:
// A single-use customer link: email confirmation, password reset or staff invitation (Slice 2).
//
// RESPONSIBILITIES:
// - Keep only the hash; expire; be used once
//
// IMPORTANT NOTES:
// - Lifetimes in OperationsCustomerLimits
// =========================================================
public sealed class OperationsCustomerToken : OperationsEntity
{
    private OperationsCustomerToken()
    {
        TokenHash = string.Empty;
    }

    private OperationsCustomerToken(Guid tenantId, Guid customerId, OperationsCustomerTokenPurpose purpose, string tokenHash, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        CustomerId = customerId;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresUtc = createdUtc.Add(OperationsCustomerLimits.LinkLifetime(purpose));
    }

    public Guid CustomerId { get; private set; }

    public OperationsCustomerTokenPurpose Purpose { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public static OperationsCustomerToken Issue(Guid tenantId, Guid customerId, OperationsCustomerTokenPurpose purpose, string tokenHash, DateTimeOffset utcNow)
    {
        if (customerId == Guid.Empty || !Enum.IsDefined(purpose))
        {
            throw new DomainException(OperationsCustomerErrors.LinkInvalid);
        }

        return new OperationsCustomerToken(tenantId, customerId, purpose, OperationsTokenHash.Require(tokenHash), utcNow);
    }

    public void Consume(OperationsCustomerTokenPurpose purpose, DateTimeOffset utcNow)
    {
        if (purpose != Purpose || UsedUtc is not null || utcNow >= ExpiresUtc)
        {
            throw new DomainException(OperationsCustomerErrors.LinkInvalid);
        }

        UsedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
