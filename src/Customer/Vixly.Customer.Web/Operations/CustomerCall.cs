namespace Vixly.Customer.Web.Operations;

// =========================================================
// FILE: CustomerCall.cs
//
// PURPOSE:
// The answer of one Operations.Api call: the value, or the error code (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the value, or the code Operations gave
//
// IMPORTANT NOTES:
// - Unavailable: Operations could not be reached
// =========================================================
public sealed record CustomerCall<T>(T? Value, string? ErrorCode, bool Unavailable = false, IReadOnlyDictionary<string, string>? Arguments = null)
{
    public bool Succeeded => ErrorCode is null && !Unavailable;
}
