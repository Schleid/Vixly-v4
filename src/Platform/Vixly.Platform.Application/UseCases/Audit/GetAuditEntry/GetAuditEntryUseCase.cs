using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Audit.GetAuditEntry;

// =========================================================
// FILE: GetAuditEntryUseCase.cs
//
// PURPOSE:
// Loads one audit entry with everything it recorded.
//
// RESPONSIBILITIES:
// - Require platform.audit.view
// - Answer NotFound for an unknown entry
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed class GetAuditEntryUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformAuditQueries _queries;

    public GetAuditEntryUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformAuditQueries queries)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<GetAuditEntryResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetAuditEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.AuditView, cancellationToken);

        PlatformAuditEntryDetail? entry = await _queries.GetAsync(request.AuditEntryId, cancellationToken);

        return entry is null
            ? GetAuditEntryResponse.Failure(PlatformCommonErrors.NotFound)
            : GetAuditEntryResponse.Success(entry);
    }
}
