namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: PlatformSecret.cs
//
// PURPOSE:
// A freshly generated secret and the hash that is stored.
//
// RESPONSIBILITIES:
// - Carry the raw value (sent to the user once) and its hash
//   (kept in the database)
//
// IMPORTANT NOTES:
// - The raw value must never be logged or persisted
// =========================================================
public sealed record PlatformSecret(string Value, string Hash);
