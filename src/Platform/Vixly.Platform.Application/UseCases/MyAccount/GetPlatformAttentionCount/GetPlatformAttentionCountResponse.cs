using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetPlatformAttentionCount;

// =========================================================
// FILE: GetPlatformAttentionCountResponse.cs
//
// PURPOSE:
// Result of GetPlatformAttentionCount.
//
// RESPONSIBILITIES:
// - Report the count, or none when the person may not open the page
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformAttentionCountResponse
{
    private GetPlatformAttentionCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    // None: the person may not open the page, so the menu shows no number.
    public int? Count { get; private init; }

    public static GetPlatformAttentionCountResponse Success(int? count)
    {
        return new GetPlatformAttentionCountResponse { Succeeded = true, Count = count };
    }

    public static GetPlatformAttentionCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformAttentionCountResponse { Error = error };
    }
}
