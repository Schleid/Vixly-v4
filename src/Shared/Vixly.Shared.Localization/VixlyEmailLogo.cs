namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyEmailLogo.cs
//
// PURPOSE:
// A company's own logo for the header of its emails (VixlyEmail; owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - The picture, its type and its size, so the email can show it at the right width
//
// IMPORTANT NOTES:
// - PNG or JPEG only: many mail programs do not show WebP
// =========================================================
public sealed record VixlyEmailLogo(byte[] Content, string ContentType, int Width, int Height)
{
    public const int HeaderHeight = 44;

    public const int MaxHeaderWidth = 240;

    // The width shown at the header height, never wider than the header allows.
    public int HeaderWidth => Height <= 0 ? MaxHeaderWidth : Math.Clamp((int)Math.Round(Width * (double)HeaderHeight / Height), 1, MaxHeaderWidth);

    public int ShownHeight => Height <= 0 || Width * (double)HeaderHeight / Height <= MaxHeaderWidth
        ? HeaderHeight
        : Math.Max(1, (int)Math.Round(Height * (double)MaxHeaderWidth / Width));
}
