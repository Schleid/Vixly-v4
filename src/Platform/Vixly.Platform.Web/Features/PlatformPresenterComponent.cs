using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Vixly.Platform.Web.Security;
using Vixly.Shared.UI.Presenters;

namespace Vixly.Platform.Web.Features;

// =========================================================
// FILE: PlatformPresenterComponent.cs
//
// PURPOSE:
// The base of every Platform feature component with a
// presenter.
//
// RESPONSIBILITIES:
// - Read the signed-in principal from the cascading
//   authentication state
// - Copy it, with the circuit's request facts, into the
//   component's own DI scope before the presenter is created
//
// IMPORTANT NOTES:
// - Components load in OnParametersSetAsync, which Blazor calls
//   after this class has finished OnInitializedAsync
// - Needed because OwningComponentBase opens a new scope, which
//   would otherwise see no signed-in user
// =========================================================
public abstract class PlatformPresenterComponent<TPresenter> : PresenterComponent<TPresenter>
    where TPresenter : PresenterBase
{
    private ClaimsPrincipal _user = new();

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Inject]
    private PlatformInteractiveRequest CircuitRequest { get; set; } = default!;

    protected sealed override async Task OnInitializedAsync()
    {
        if (AuthenticationState is not null)
        {
            _user = (await AuthenticationState).User;
        }
    }

    protected override void PrepareScope(IServiceProvider scopedServices)
    {
        ArgumentNullException.ThrowIfNull(scopedServices);

        scopedServices.GetRequiredService<PlatformInteractiveRequest>().CopyFrom(CircuitRequest, _user);
    }
}
