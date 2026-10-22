namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformOutboxFailure.cs
//
// PURPOSE:
// One outbox message whose handler failed in a dispatch run.
//
// RESPONSIBILITIES:
// - Carry what the host logs: message id, type, error code, attempt count and the exception
//
// IMPORTANT NOTES:
// - The payload is never carried: it may hold personal data
// =========================================================
public sealed record PlatformOutboxFailure(Guid MessageId, string Type, string ErrorCode, int Attempts, Exception Exception);
