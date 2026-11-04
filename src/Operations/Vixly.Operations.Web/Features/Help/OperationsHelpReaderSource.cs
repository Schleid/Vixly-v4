using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Staff.GetOperationsMenu;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;
using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Features.Help;

// =========================================================
// FILE: OperationsHelpReaderSource.cs
//
// PURPOSE:
// Shares the current request's help reader with its header and page.
//
// RESPONSIBILITIES:
// - Load current menu permissions, known plan features and branding.
// - Fail closed when permissions or entitlements cannot be loaded.
//
// IMPORTANT NOTES:
// - Tenant and staff identity come only from the checked request.
// - Never treat the cosmetic branding fallback as an entitlement.
// =========================================================
public sealed class OperationsHelpReaderSource : IOperationsHelpReaderSource
{
    private readonly IHttpContextAccessor _http;
    private readonly OperationsAllowed _allowed;
    private readonly IOperationsPlanFeatures _features;
    private readonly GetOperationsSignInBrandUseCase _brand;
    private readonly IOperationsPortalLinks _links;
    private readonly ILogger<OperationsHelpReaderSource> _logger;
    private Task<OperationsHelpReaderContext>? _loaded;

    public OperationsHelpReaderSource(IHttpContextAccessor http, OperationsAllowed allowed, IOperationsPlanFeatures features,
        GetOperationsSignInBrandUseCase brand, IOperationsPortalLinks links, ILogger<OperationsHelpReaderSource> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _allowed = allowed ?? throw new ArgumentNullException(nameof(allowed));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _brand = brand ?? throw new ArgumentNullException(nameof(brand));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<OperationsHelpReaderContext> GetAsync(string? company = null) => _loaded ??= LoadAsync(company);

    private async Task<OperationsHelpReaderContext> LoadAsync(string? company)
    {
        HttpContext http = _http.HttpContext ?? throw new InvalidOperationException("No help request.");
        if (!OperationsCurrentUser.IsSignedIn(http) || OperationsTenantMiddleware.Host(http)?.TenantId is not { } tenantId)
            return new(new([], [], signedIn: false), string.Empty, string.Empty);

        OperationsAuthorizationContext context = OperationsCurrentUser.Context(http);
        GetOperationsMenuResponse menu;
        try { menu = await _allowed.GetAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException && !http.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Help permissions could not be loaded; only unrestricted articles are available.");
            _allowed.UseNoPermissionsForHelp();
            menu = GetOperationsMenuResponse.Nothing;
        }

        OperationsPlanFeatures features;
        try { features = await _features.GetAsync(tenantId, http.RequestAborted); }
        catch (Exception exception) when (exception is not OutOfMemoryException && !http.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Help plan conditions could not be loaded; conditional articles are hidden.");
            features = OperationsPlanFeatures.Unknown;
        }

        string name = company ?? string.Empty;
        if (company is null)
        {
            try { name = (await _brand.ExecuteAsync(context, new(), http.RequestAborted)).DisplayName; }
            catch (Exception exception) when (exception is not OutOfMemoryException && !http.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Help branding could not be loaded; article content is unavailable.");
            }
        }
        string portal = OperationsTenantMiddleware.Host(http)?.CanonicalSlug is { } slug ? _links.Page(slug, "/customer") : string.Empty;
        return new(new(menu.Permissions, Conditions(features)), name, portal);
    }

    public static IReadOnlySet<string> Conditions(OperationsPlanFeatures features)
    {
        HashSet<string> conditions = new(StringComparer.Ordinal);
        if (!features.IsKnown) return conditions;
        if (features.PartnerAgents) conditions.Add("partner_agents");
        if (features.TextMessages) conditions.Add("text_messages");
        if (features.TenantApi) conditions.Add("tenant_api");
        if (features.ThemeCustomization) conditions.Add("website_theme");
        if (features.ContentCustomization) conditions.Add("website_content");
        return conditions;
    }
}
