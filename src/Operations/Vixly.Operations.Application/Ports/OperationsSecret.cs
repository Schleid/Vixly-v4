namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsSecret.cs
//
// PURPOSE:
// A new secret and its stored hash.
//
// RESPONSIBILITIES:
// - Carry both
//
// IMPORTANT NOTES:
// - Only the hash is stored; the value goes to the user once
// =========================================================
public sealed record OperationsSecret(string Value, string Hash);
