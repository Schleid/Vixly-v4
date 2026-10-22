using Microsoft.AspNetCore.Components;
using Vixly.Operations.Application.UseCases.Staff.GetOperationsMenu;
using Vixly.Operations.Web.Security;

namespace Vixly.Operations.Web.Components.Editor;

// =========================================================
// FILE: OperationsAllowedComponent.cs
//
// PURPOSE:
// The base of staff components that show a menu item, link or button only to people allowed to use it (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Load what the signed-in person may do before the markup renders
// - Answer Can(permission) and CanAny(permissions) for the markup
//
// IMPORTANT NOTES:
// - A second layer only: the endpoints and use cases still check every permission
// - Permissions count in any scope; a page then keeps to the person's branches and warehouses
// =========================================================
public abstract class OperationsAllowedComponent : ComponentBase
{
    [Inject]
    private OperationsAllowed Allowed { get; set; } = null!;

    protected GetOperationsMenuResponse Allowance { get; private set; } = GetOperationsMenuResponse.Nothing;

    protected bool Can(string permission) => Allowance.Has(permission);

    protected bool CanAny(params string[] permissions) => Allowance.HasAny(permissions);

    protected override async Task OnInitializedAsync() => Allowance = await Allowed.GetAsync();
}
