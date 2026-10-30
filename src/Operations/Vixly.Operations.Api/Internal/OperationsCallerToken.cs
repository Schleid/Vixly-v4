namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsCallerToken.cs
//
// PURPOSE:
// A checked caller token.
//
// RESPONSIBILITIES:
// - Carry the caller, scopes, tenant and customer
//
// IMPORTANT NOTES:
// - Built only by OperationsCallerTokenValidator
// =========================================================
public sealed record OperationsCallerToken(string Caller, IReadOnlyCollection<string> Scopes, Guid? TenantId, Guid? CustomerId);
