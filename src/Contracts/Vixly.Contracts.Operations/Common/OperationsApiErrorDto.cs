namespace Vixly.Contracts.Operations.Common;

// =========================================================
// FILE: OperationsApiErrorDto.cs
//
// PURPOSE:
// The one error shape Operations.Api answers with (S-9, as ADR-0014).
//
// RESPONSIBILITIES:
// - Carry the error code, its arguments and the correlation id
//
// IMPORTANT NOTES:
// - Never a stack trace or exception text
// - Consumers show their own text for the code
// =========================================================
public sealed record OperationsApiErrorDto(string Code, IReadOnlyDictionary<string, string>? Arguments, string CorrelationId);
