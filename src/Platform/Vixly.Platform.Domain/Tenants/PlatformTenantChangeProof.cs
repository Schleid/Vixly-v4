using System.Security.Cryptography;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantChangeProof.cs
//
// PURPOSE:
// The document a tenant sends to prove a change of its legal details (B12).
//
// RESPONSIBILITIES:
// - Accept only PDF, JPEG or PNG, judged by the file’s first bytes, not its name
// - Keep a SHA-256 of the content
//
// IMPORTANT NOTES:
// - Stored in its own table so lists never load the bytes
// - At most 10 MB
// =========================================================
public sealed class PlatformTenantChangeProof
{
    private PlatformTenantChangeProof()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        Content = Array.Empty<byte>();
        Sha256 = string.Empty;
    }

    public Guid PlatformTenantChangeRequestId { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public byte[] Content { get; private set; }

    public int SizeBytes { get; private set; }

    public string Sha256 { get; private set; }

    internal static PlatformTenantChangeProof Create(Guid requestId, string? fileName, byte[]? content)
    {
        if (content is null || content.Length == 0)
        {
            throw new DomainException(PlatformTenantChangeErrors.ProofRequired);
        }

        if (content.Length > PlatformTenantChangeLimits.ProofMaxBytes)
        {
            throw new DomainException(PlatformTenantChangeErrors.ProofTooLarge);
        }

        string contentType = Detect(content) ?? throw new DomainException(PlatformTenantChangeErrors.ProofTypeInvalid);
        string name = Path.GetFileName(fileName?.Trim() ?? string.Empty);

        if (name.Length == 0 || name.Any(char.IsControl))
        {
            name = "proof";
        }

        return new PlatformTenantChangeProof
        {
            PlatformTenantChangeRequestId = requestId,
            FileName = name.Length <= PlatformTenantChangeLimits.FileNameMaxLength ? name : name[..PlatformTenantChangeLimits.FileNameMaxLength],
            ContentType = contentType,
            Content = content,
            SizeBytes = content.Length,
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(content))
        };
    }

    // The first bytes decide the type; a renamed file of another kind is refused.
    private static string? Detect(byte[] content)
    {
        if (content.AsSpan().StartsWith("%PDF-"u8))
        {
            return "application/pdf";
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        return content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "image/png" : null;
    }
}
