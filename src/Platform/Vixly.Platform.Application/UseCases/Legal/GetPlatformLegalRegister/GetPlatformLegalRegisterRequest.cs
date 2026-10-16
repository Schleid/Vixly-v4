using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalRegister;

// =========================================================
// FILE: GetPlatformLegalRegisterRequest.cs
//
// PURPOSE:
// Pages the tenants that must accept the current required Terms, with their status (D-Lg12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformLegalRegisterRequest
{
    public PlatformLegalStatus? Status { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
