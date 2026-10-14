using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Tenants.AssignPlatformTenantAccountManager;
using Vixly.Platform.Application.UseCases.Tenants.ListAccountManagerCandidates;
using Vixly.Platform.Application.UseCases.Tenants.SetPlatformTenantTags;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantManagementPresenter.cs
//
// PURPOSE:
// Behaviour of the account management section (D-T16).
//
// RESPONSIBILITIES:
// - Offer active staff as account manager
// - Save the account manager, then the tags
//
// IMPORTANT NOTES:
// - Tags are typed comma-separated and normalised by the domain
// =========================================================
public sealed class TenantManagementPresenter : PlatformPresenter<TenantManagementState>
{
    private readonly ListAccountManagerCandidatesUseCase _candidates;

    private readonly AssignPlatformTenantAccountManagerUseCase _assign;

    private readonly SetPlatformTenantTagsUseCase _tags;

    public TenantManagementPresenter(
        ListAccountManagerCandidatesUseCase candidates,
        AssignPlatformTenantAccountManagerUseCase assign,
        SetPlatformTenantTagsUseCase tags,
        PlatformUseCaseScope scope,
        ILogger<TenantManagementPresenter> logger)
        : base(scope, TenantManagementState.Initial, logger)
    {
        _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        _assign = assign ?? throw new ArgumentNullException(nameof(assign));
        _tags = tags ?? throw new ArgumentNullException(nameof(tags));
    }

    public async Task StartEditAsync()
    {
        ListAccountManagerCandidatesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _candidates.ExecuteAsync(context, new ListAccountManagerCandidatesRequest(), cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? new TenantManagementState(true, response.Staff, null, null)
            : State with { Error = error });
    }

    public void Cancel()
    {
        SetState(TenantManagementState.Initial);
    }

    public async Task<bool> SaveAsync(Guid platformTenantId, Guid? accountManagerId, string? tags)
    {
        string[] values = (tags ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            Error? failed = (await _assign.ExecuteAsync(
                context,
                new AssignPlatformTenantAccountManagerRequest { PlatformTenantId = platformTenantId, PlatformUserId = accountManagerId },
                cancellationToken)).Error;

            return failed ?? (await _tags.ExecuteAsync(
                context,
                new SetPlatformTenantTagsRequest { PlatformTenantId = platformTenantId, Tags = values },
                cancellationToken)).Error;
        });

        SetState(error is null ? TenantManagementState.Initial with { DoneKey = "ui.tenants.management_saved" } : State with { Error = error });

        return error is null;
    }
}
