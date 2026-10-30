namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsCallerOptions.cs
//
// PURPOSE:
// One registered caller of Operations.Api (section OperationsInternal:Callers).
//
// RESPONSIBILITIES:
// - Hold the scopes and public keys of a caller
//
// IMPORTANT NOTES:
// - Keys are P-256 SubjectPublicKeyInfo in base64; public, not secret
// =========================================================
public sealed class OperationsCallerOptions
{
    public string[] Scopes { get; set; } = Array.Empty<string>();

    public Dictionary<string, string> Keys { get; set; } = new(StringComparer.Ordinal);
}
