using Vixly.Operations.Application.UseCases.Staff.GetOperationsMenu;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsAllowed.cs
//
// PURPOSE:
// Tells staff components what the signed-in person may do, so menus, links and buttons for anything else are not shown (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Load the person's permissions once per request and share them with every component of the page
//
// IMPORTANT NOTES:
// - A second layer only: every endpoint and use case still checks permissions; hiding is never the protection
// - Outside a signed-in staff request it holds nothing
// =========================================================
public sealed class OperationsAllowed
{
    private readonly IHttpContextAccessor _http;

    private readonly GetOperationsMenuUseCase _menu;

    private Task<GetOperationsMenuResponse>? _loaded;

    public OperationsAllowed(IHttpContextAccessor http, GetOperationsMenuUseCase menu)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _menu = menu ?? throw new ArgumentNullException(nameof(menu));
    }

    public Task<GetOperationsMenuResponse> GetAsync() => _loaded ??= LoadAsync();

    private async Task<GetOperationsMenuResponse> LoadAsync()
    {
        if (_http.HttpContext is not { } http || !OperationsCurrentUser.IsSignedIn(http) || Tenancy.OperationsTenantMiddleware.Host(http) is not { TenantId: not null })
        {
            return GetOperationsMenuResponse.Nothing;
        }

        return await _menu.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsMenuRequest(), http.RequestAborted);
    }
}
