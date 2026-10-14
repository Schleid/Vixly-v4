using System.Security.Cryptography;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Security;

// =========================================================
// FILE: PlatformRandom.cs
//
// PURPOSE:
// IPlatformRandom backed by the operating system's cryptographic
// generator.
//
// RESPONSIBILITIES:
// - Fill buffers with strong random bytes
//
// IMPORTANT NOTES:
// - Thread-safe; registered as a singleton
// =========================================================
internal sealed class PlatformRandom : IPlatformRandom
{
    public void Fill(Span<byte> buffer)
    {
        RandomNumberGenerator.Fill(buffer);
    }
}
