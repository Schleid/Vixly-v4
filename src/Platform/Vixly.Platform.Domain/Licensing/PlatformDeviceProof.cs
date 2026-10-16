namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceProof.cs
//
// PURPOSE:
// How a device proves it is the licensed device (D-L5, owner: both methods).
//
// RESPONSIBILITIES:
// - Name the how a device proves it is the licensed device (D-L5, owner: both methods)
//
// IMPORTANT NOTES:
// - KeyPair: signs a challenge with a key that never leaves the device
// - Secret: a rotating random secret; Platform keeps only its hash
// - Values are stored; never renumber
// =========================================================
public enum PlatformDeviceProof
{
    KeyPair = 1,
    Secret = 2
}
