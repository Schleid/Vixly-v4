namespace Vixly.Platform.Application.UseCases.MyAccount.GetPlatformAttentionCount;

// =========================================================
// FILE: GetPlatformAttentionCountRequest.cs
//
// PURPOSE:
// Input for GetPlatformAttentionCount.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Which menu entry to count
// =========================================================
public sealed class GetPlatformAttentionCountRequest
{
    public Vixly.Platform.Application.Queries.PlatformAttention Attention { get; init; }
}
