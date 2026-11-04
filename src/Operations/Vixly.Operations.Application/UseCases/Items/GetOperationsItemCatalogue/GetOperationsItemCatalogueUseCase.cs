using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Items;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.GetOperationsItemCatalogue;

// =========================================================
// FILE: GetOperationsItemCatalogueUseCase.cs
//
// PURPOSE:
// The company's item catalogue and whether items must be complete before bundling (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Loaded with the starter list the first time someone who manages it opens it
// =========================================================
public sealed class GetOperationsItemCatalogueUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsItemCatalogue _catalogue;

    private readonly IOperationsItemRepository _items;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetOperationsItemCatalogueUseCase(OperationsAccess access, OperationsItemCatalogue catalogue, IOperationsItemRepository items, IOperationsSettingsRepository settings, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsItemCatalogueResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsItemCatalogueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ServicesView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsItemCatalogueResponse.Failure(refused);
        }

        bool canEdit = access!.HasTenantWide(OperationsPermissions.ServicesManage);
        (IReadOnlyList<OperationsItemCatalogueEntry> entries, bool loaded) = await _catalogue.ListAsync(context.RequiredTenantId, canEdit, _timeProvider.GetUtcNow(), cancellationToken);

        // The starter list went in just now (once per company).
        if (loaded && !await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            entries = await _items.ListCatalogueAsync(cancellationToken);
        }

        return GetOperationsItemCatalogueResponse.Success(entries, (await _settings.FindAsync(cancellationToken))?.ItemsRequiredSinceUtc, canEdit);
    }
}
