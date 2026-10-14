using Microsoft.Extensions.Localization;
using Vixly.Platform.Resources;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Web.Endpoints;

// =========================================================
// FILE: PlatformErrorText.cs
//
// PURPOSE:
// Turns an Error code into the sentence shown to the user,
// in the request's language (L-1, L-4).
//
// RESPONSIBILITIES:
// - Look in PlatformText, then SharedText
//
// IMPORTANT NOTES:
// - The only place sign-in pages get error text from
// =========================================================
public sealed class PlatformErrorText
{
    private readonly IStringLocalizer<PlatformText> _platform;

    private readonly IStringLocalizer<SharedText> _shared;

    public PlatformErrorText(IStringLocalizer<PlatformText> platform, IStringLocalizer<SharedText> shared)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _shared = shared ?? throw new ArgumentNullException(nameof(shared));
    }

    public string? Describe(Error? error)
    {
        return error is null ? null : VixlyText.Describe(_platform, _shared, error);
    }

    public string Text(string key)
    {
        return VixlyText.Format(_platform, key);
    }
}
