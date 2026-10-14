namespace Vixly.Platform.Application.UseCases.Sales.SetPlatformPromotionName;

// =========================================================
// FILE: SetPlatformPromotionNameRequest.cs
//
// PURPOSE:
// Sets a promotion's name in one language (ADR-0005).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformPromotionNameRequest
{
    public Guid PromotionId { get; init; }

    public string Language { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
