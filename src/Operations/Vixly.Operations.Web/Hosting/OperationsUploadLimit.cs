using Microsoft.AspNetCore.Http.Metadata;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsUploadLimit.cs
//
// PURPOSE:
// Lets one route accept a request body up to a given size.
//
// RESPONSIBILITIES:
// - Carry the largest body size of a route
//
// IMPORTANT NOTES:
// - Used by the site image upload (Slice 1B)
// =========================================================
public sealed class OperationsUploadLimit : IRequestSizeLimitMetadata
{
    public OperationsUploadLimit(long maxRequestBodySize) => MaxRequestBodySize = maxRequestBodySize;

    public long? MaxRequestBodySize { get; }
}
