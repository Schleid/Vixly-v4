using Microsoft.Extensions.Localization;
using Vixly.Operations.Resources;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsErrorText.cs
//
// PURPOSE:
// Turns error codes into text for the static pages (L-1).
//
// RESPONSIBILITIES:
// - Describe an error in the current language
//
// IMPORTANT NOTES:
// - OperationsText first, then the shared texts
// =========================================================
public sealed class OperationsErrorText
{
    private readonly IStringLocalizer<OperationsText> _text;

    private readonly IStringLocalizer<SharedText> _shared;

    public OperationsErrorText(IStringLocalizer<OperationsText> text, IStringLocalizer<SharedText> shared)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _shared = shared ?? throw new ArgumentNullException(nameof(shared));
    }

    public string? Describe(Error? error) => error is null ? null : VixlyText.Describe(_text, _shared, error);
}
