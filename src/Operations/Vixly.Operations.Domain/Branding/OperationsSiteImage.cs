using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteImage.cs
//
// PURPOSE:
// An uploaded site image, never changed (Slice 1B; images in the database, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Accept PNG, JPEG or WebP only, within size and dimension limits
//
// IMPORTANT NOTES:
// - SVG is refused: it can carry script
// - The type and dimensions come from the file content (Infrastructure), never from the upload name
// =========================================================
public sealed class OperationsSiteImage : OperationsEntity
{
    public const int MaxBytes = 1024 * 1024;

    public const int MaxFaviconBytes = 256 * 1024;

    public static readonly IReadOnlyCollection<string> ContentTypes = new[] { "image/png", "image/jpeg", "image/webp" };

    private OperationsSiteImage()
    {
        ContentType = string.Empty;
        Content = Array.Empty<byte>();
        Sha256 = string.Empty;
    }

    private OperationsSiteImage(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        ContentType = string.Empty;
        Content = Array.Empty<byte>();
        Sha256 = string.Empty;
    }

    public OperationsSiteImageKind Kind { get; private set; }

    public string ContentType { get; private set; }

    public byte[] Content { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public string Sha256 { get; private set; }

    public static OperationsSiteImage Create(Guid tenantId, OperationsSiteImageKind kind, string contentType, byte[] content, int width, int height, string sha256, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!Enum.IsDefined(kind) || !ContentTypes.Contains(contentType))
        {
            throw new DomainException(OperationsBrandingErrors.ImageTypeNotAllowed);
        }

        if (content.Length == 0 || content.Length > (kind == OperationsSiteImageKind.Favicon ? MaxFaviconBytes : MaxBytes))
        {
            throw new DomainException(OperationsBrandingErrors.ImageTooLarge);
        }

        (int maxWidth, int maxHeight) = kind switch
        {
            OperationsSiteImageKind.Favicon => (512, 512),
            OperationsSiteImageKind.Hero => (4000, 3000),
            _ => (2000, 2000)
        };

        if (width < 16 || height < 16 || width > maxWidth || height > maxHeight || (kind == OperationsSiteImageKind.Favicon && width != height))
        {
            throw new DomainException(OperationsBrandingErrors.ImageSizeInvalid);
        }

        return new OperationsSiteImage(tenantId, utcNow)
        {
            Kind = kind,
            ContentType = contentType,
            Content = content.ToArray(),
            Width = width,
            Height = height,
            Sha256 = sha256
        };
    }
}
