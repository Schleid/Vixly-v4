namespace Vixly.Platform.Application.UseCases.Registration.GetMyRegistrationStatus;

// =========================================================
// FILE: GetMyRegistrationStatusRequest.cs
//
// PURPOSE:
// Input for GetMyRegistrationStatus.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The access token Public.Web holds
// =========================================================
public sealed class GetMyRegistrationStatusRequest
{
    public string AccessToken { get; init; } = string.Empty;
}
