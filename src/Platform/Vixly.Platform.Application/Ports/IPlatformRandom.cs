namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformRandom.cs
//
// PURPOSE:
// A source of cryptographically strong random bytes.
//
// RESPONSIBILITIES:
// - Fill a buffer with random bytes
//
// IMPORTANT NOTES:
// - Used for tenant codes (D-13); tests supply fixed bytes
// =========================================================
public interface IPlatformRandom
{
    void Fill(Span<byte> buffer);
}
