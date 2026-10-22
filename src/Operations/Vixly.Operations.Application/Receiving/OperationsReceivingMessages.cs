namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsReceivingMessages.cs
//
// PURPOSE:
// Outbox messages of receiving (Slice 3B).
//
// RESPONSIBILITIES:
// - Name the message that emails a customer about a parcel
//
// IMPORTANT NOTES:
// - Sent after the commit by the outbox worker, retried on failure
// =========================================================
public static class OperationsReceivingMessages
{
    public const string ParcelNotice = "ops.receiving.parcel_notice.v1";
}
