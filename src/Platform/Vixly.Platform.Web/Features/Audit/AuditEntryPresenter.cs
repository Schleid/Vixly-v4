using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Audit.GetAuditEntry;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Audit;

// =========================================================
// FILE: AuditEntryPresenter.cs
//
// PURPOSE:
// Behaviour of the audit entry page.
//
// RESPONSIBILITIES:
// - Load one entry
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class AuditEntryPresenter : PlatformPresenter<AuditEntryState>
{
    private readonly GetAuditEntryUseCase _get;

    public AuditEntryPresenter(
        GetAuditEntryUseCase get,
        PlatformUseCaseScope scope,
        ILogger<AuditEntryPresenter> logger)
        : base(scope, AuditEntryState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
    }

    public async Task LoadAsync(Guid auditEntryId)
    {
        GetAuditEntryResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetAuditEntryRequest { AuditEntryId = auditEntryId }, cancellationToken);

            return response.Error;
        });

        SetState(new AuditEntryState(error is null ? response?.Entry : null, error));
    }
}
