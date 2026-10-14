namespace Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanCatalogue;

// =========================================================
// FILE: SetPlatformPlanCatalogueRequest.cs
//
// PURPOSE:
// Sets whether a plan shows in the public catalogue, and its order.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformPlanCatalogueRequest
{
    public Guid PlatformPlanId { get; init; }

    public bool IsPublic { get; init; }

    public int DisplayOrder { get; init; }
}
