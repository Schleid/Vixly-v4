using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsApiKey.cs
//
// PURPOSE:
// A key a company gives its own programs to read its data through the API (Slice 10).
//
// RESPONSIBILITIES:
// - Keep the key’s public id, the hash of its secret and its last characters; revoke it
// - Note when it was last used (at most once a minute)
//
// IMPORTANT NOTES:
// - The secret is shown once, when the key is made, and never kept
// - Read only: the API changes nothing (owner, 2026-10-22)
// =========================================================
public sealed class OperationsApiKey : OperationsEntity
{
    public const int NameMaxLength = 80;

    public const int MaxActivePerCompany = 20;

    private OperationsApiKey()
    {
        Name = string.Empty;
        KeyId = string.Empty;
        SecretHash = string.Empty;
        Hint = string.Empty;
    }

    private OperationsApiKey(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
        KeyId = string.Empty;
        SecretHash = string.Empty;
        Hint = string.Empty;
    }

    public string Name { get; private set; }

    // The public part of the key: how it is found.
    public string KeyId { get; private set; }

    // SHA-256 of the secret part, in hex.
    public string SecretHash { get; private set; }

    // The secret's last four characters, to tell keys apart.
    public string Hint { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset? LastUsedUtc { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public bool Active => RevokedUtc is null;

    public static OperationsApiKey Create(Guid tenantId, string? name, string keyId, string secretHash, string hint, Guid by, DateTimeOffset utcNow)
    {
        string clean = name?.Trim() ?? string.Empty;

        if (clean.Length is 0 or > NameMaxLength)
        {
            throw new DomainException(OperationsApiErrors.NameRequired);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(hint);

        return new OperationsApiKey(tenantId, utcNow) { Name = clean, KeyId = keyId, SecretHash = secretHash, Hint = hint, CreatedByUserId = by };
    }

    public void Revoke(Guid by, DateTimeOffset utcNow)
    {
        if (RevokedUtc is not null)
        {
            return;
        }

        (RevokedUtc, RevokedByUserId) = (utcNow, by);
        MarkAsModified(utcNow);
    }

    // True when the moment is worth saving (not more than once a minute).
    public bool Used(DateTimeOffset utcNow)
    {
        if (LastUsedUtc is { } last && utcNow - last < TimeSpan.FromMinutes(1))
        {
            return false;
        }

        LastUsedUtc = utcNow;
        return true;
    }
}
