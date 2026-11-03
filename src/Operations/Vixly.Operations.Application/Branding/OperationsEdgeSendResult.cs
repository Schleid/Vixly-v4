namespace Vixly.Operations.Application.Branding;

// =========================================================
// FILE: OperationsEdgeSendResult.cs
//
// PURPOSE:
// What became of a brand sent to the Cloudflare Worker.
//
// RESPONSIBILITIES:
// - Sent: the Worker keeps it
// - Stale: the Worker already has a newer one
// - NotConfigured: no Worker set up for this installation (staging, a developer's machine)
//
// IMPORTANT NOTES:
// - Only Sent counts in the nightly round
// =========================================================
public enum OperationsEdgeSendResult
{
    Sent = 0,
    Stale = 1,
    NotConfigured = 2
}
