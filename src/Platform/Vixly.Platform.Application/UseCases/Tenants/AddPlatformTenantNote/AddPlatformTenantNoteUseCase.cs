using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.AddPlatformTenantNote;

// =========================================================
// FILE: AddPlatformTenantNoteUseCase.cs
//
// PURPOSE:
// Adds an internal note to a tenant (D-T16).
//
// RESPONSIBILITIES:
// - Check platform.tenants.add_notes
// - Validate the note and store it
// - Audit that a note was added (not its text)
//
// IMPORTANT NOTES:
// - Notes are append-only and never leave the console
// =========================================================
public sealed class AddPlatformTenantNoteUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantRepository _tenants;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddPlatformTenantNoteUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantRepository tenants,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddPlatformTenantNoteResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        AddPlatformTenantNoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsAddNotes, cancellationToken);

        if (await _tenants.GetAsync(request.PlatformTenantId, cancellationToken) is null)
        {
            return AddPlatformTenantNoteResponse.Failure(PlatformCommonErrors.NotFound);
        }

        PlatformTenantNote note;

        try
        {
            note = PlatformTenantNote.Write(request.PlatformTenantId, request.Body, context.PlatformUserId, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return AddPlatformTenantNoteResponse.Failure(exception.Error);
        }

        _tenants.AddNote(note);
        _audit.Record(
            context.PlatformUserId,
            "platform.tenants.note_added",
            PlatformPermissions.TenantsAddNotes,
            nameof(PlatformTenant),
            request.PlatformTenantId,
            arguments: new { noteId = note.Id, length = note.Body.Length });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AddPlatformTenantNoteResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return AddPlatformTenantNoteResponse.Success(note.Id);
    }
}
