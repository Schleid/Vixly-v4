namespace Vixly.Operations.Application.Messaging;

// =========================================================
// FILE: OperationsOutboxFailure.cs
//
// PURPOSE:
// A failed outbox delivery, for the host to log (as Platform, B10).
//
// RESPONSIBILITIES:
// - Carry the message id, type, error code and attempt
//
// IMPORTANT NOTES:
// - Never the payload
// =========================================================
public sealed record OperationsOutboxFailure(Guid MessageId, string Type, string ErrorCode, int Attempts, Exception Exception);
