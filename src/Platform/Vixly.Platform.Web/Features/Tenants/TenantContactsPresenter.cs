using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantContacts;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantContactsPresenter.cs
//
// PURPOSE:
// Behaviour of the contacts section (D-T15).
//
// RESPONSIBILITIES:
// - Save the owner and any billing or technical contact
//
// IMPORTANT NOTES:
// - Blank optional rows are left out, so they fall back to the
//   owner
// =========================================================
public sealed class TenantContactsPresenter : PlatformPresenter<TenantContactsState>
{
    private readonly UpdatePlatformTenantContactsUseCase _update;

    public TenantContactsPresenter(UpdatePlatformTenantContactsUseCase update, PlatformUseCaseScope scope, ILogger<TenantContactsPresenter> logger)
        : base(scope, TenantContactsState.Initial, logger)
    {
        _update = update ?? throw new ArgumentNullException(nameof(update));
    }

    public void StartEdit()
    {
        SetState(new TenantContactsState(true, null, null));
    }

    public void Cancel()
    {
        SetState(TenantContactsState.Initial);
    }

    public async Task<bool> SaveAsync(Guid platformTenantId, IEnumerable<TenantContactRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(
            context,
            new UpdatePlatformTenantContactsRequest
            {
                PlatformTenantId = platformTenantId,
                Contacts = rows.Where(row => !row.IsBlank).Select(row => row.ToInput()).ToList()
            },
            cancellationToken)).Error);

        SetState(error is null ? new TenantContactsState(false, "ui.tenants.contacts_saved", null) : State with { Error = error });

        return error is null;
    }
}
