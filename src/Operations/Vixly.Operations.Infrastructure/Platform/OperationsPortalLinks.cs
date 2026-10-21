using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsPortalLinks.cs
//
// PURPOSE:
// Builds absolute links to pages of a tenant portal.
//
// RESPONSIBILITIES:
// - Fill the template and append the page
//
// IMPORTANT NOTES:
// - The slug is lower-cased
// =========================================================
public sealed class OperationsPortalLinks : IOperationsPortalLinks
{
    private readonly OperationsLinkOptions _options;

    public OperationsPortalLinks(IOptions<OperationsLinkOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public string Page(string slug, string pathAndQuery)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentNullException.ThrowIfNull(pathAndQuery);

        string label = slug.Trim().ToLowerInvariant();

        // A host label only: a link never leaves the tenant's own host (review L1).
        if (label.Length > 63 || label[0] == '-' || label[^1] == '-' || !label.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-'))
        {
            throw new ArgumentException("The portal slug is not a host label.", nameof(slug));
        }

        string root = _options.PortalUrlTemplate.Replace("{slug}", label, StringComparison.Ordinal).TrimEnd('/');
        return root + (pathAndQuery.StartsWith('/') ? pathAndQuery : "/" + pathAndQuery);
    }
}
