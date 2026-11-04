namespace Vixly.Operations.Application.UseCases.Texts.HandleOperationsTextStatus;

// =========================================================
// FILE: HandleOperationsTextStatusRequest.cs
//
// PURPOSE:
// Takes Twilio's report of what became of a text message (Slice 9; review).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case: the signature covers the address and every field
// =========================================================
public sealed class HandleOperationsTextStatusRequest
{
    // The address as Twilio called it.
    public string Url { get; init; } = string.Empty;

    public IReadOnlyList<KeyValuePair<string, string>> Form { get; init; } = Array.Empty<KeyValuePair<string, string>>();

    public string? Signature { get; init; }
}
