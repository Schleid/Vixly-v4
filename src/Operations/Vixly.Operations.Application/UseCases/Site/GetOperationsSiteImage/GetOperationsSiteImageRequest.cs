namespace Vixly.Operations.Application.UseCases.Site.GetOperationsSiteImage;

// =========================================================
// FILE: GetOperationsSiteImageRequest.cs
//
// PURPOSE:
// One image of the tenant site (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSiteImageRequest
{
    public Guid ImageId { get; init; }
}
