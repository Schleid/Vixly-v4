using System.Globalization;
using Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;
using Vixly.Operations.Web.Security;
using Vixly.Shared.UI.Presenters;

namespace Vixly.Operations.Web.Features.SignIn.LoginBrand;

// =========================================================
// FILE: OperationsLoginBrandPresenter.cs
//
// PURPOSE:
// Load the existing published tenant logo for staff entry.
//
// RESPONSIBILITIES:
// - Read published branding through the existing application use case
// - Retain the initials fallback if no published logo is available
//
// IMPORTANT NOTES:
// - Tenant identity comes only from the resolved request host
// - Never request draft branding or change authentication behavior
// =========================================================
public sealed class OperationsLoginBrandPresenter : PresenterBase
{
    private readonly GetOperationsPublicSiteUseCase _site;
    private readonly IHttpContextAccessor _http;

    public OperationsLoginBrandPresenter(GetOperationsPublicSiteUseCase site, IHttpContextAccessor http, ILogger<OperationsLoginBrandPresenter> logger)
        : base(logger)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public OperationsLoginBrandState State { get; private set; } = new(null);

    public Task<PresenterRunOutcome> LoadAsync() => RunAsync(async lifetime =>
    {
        HttpContext http = _http.HttpContext ?? throw new InvalidOperationException("No request context.");
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, http.RequestAborted);
        var context = OperationsCurrentUser.Context(http);
        if (context.IsSettingUp)
        {
            return;
        }

        GetOperationsPublicSiteResponse response = await _site.ExecuteAsync(context,
            new GetOperationsPublicSiteRequest { Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Preview = false }, linked.Token);
        State = new(response is { Succeeded: true, Site.Settings.LogoImageId: { } logo } ? "/site/image/" + logo.ToString("N") : null);
    });
}
