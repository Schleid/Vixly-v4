using Vixly.Contracts.Platform.Internal.Legal;
using Vixly.Operations.Application.Legal;
using Vixly.Operations.Application.Ports;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsLegalClient.cs
//
// PURPOSE:
// Calls Platform's Terms endpoints for Operations (Slice 1C).
//
// RESPONSIBILITIES:
// - Read the status and the version to accept
// - Record the Owner's acceptance
// - Read a published PDF
//
// IMPORTANT NOTES:
// - Refusals become ops.legal.* errors
// =========================================================
public sealed class OperationsLegalClient : IOperationsLegalClient
{
    private readonly OperationsPlatformSender _sender;

    public OperationsLegalClient(OperationsPlatformSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<OperationsLegalStatus> GetStatusAsync(OperationsPlatformActor actor, string language, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformLegalStatusInfo? info, _) = await _sender.SendAsync<PlatformLegalStatusInfo>(
            HttpMethod.Get, "internal/v1/tenant/legal?language=" + Uri.EscapeDataString(language), actor.TenantId, actor, null, cancellationToken);

        if (info is null)
        {
            return new OperationsLegalStatus("not_required", null, false, null);
        }

        return new OperationsLegalStatus(info.Status, info.DeadlineUtc, info.ReadOnly, info.Terms is { } terms
            ? new OperationsLegalTerms(terms.DocumentId, terms.Version, terms.Language, terms.Sha256, terms.Summary,
                terms.Parts.Select(part => new OperationsLegalPart(part.DocumentId, part.Kind, part.Version, part.Summary)).ToList())
            : null);
    }

    public async Task<OperationsPlatformCall<bool>> AcceptAsync(OperationsPlatformActor actor, Guid documentId, string sha256, string language, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (_, string? refused) = await _sender.SendAsync<object>(
            HttpMethod.Post, "internal/v1/tenant/legal/accept", actor.TenantId, actor, new AcceptPlatformLegalTermsBody(documentId, sha256, language), cancellationToken);

        return refused is null ? OperationsPlatformCall<bool>.Ok(true) : OperationsPlatformCall<bool>.Refused(Translate(refused));
    }

    public Task<OperationsLegalFile?> GetFileAsync(Guid documentId, string language, CancellationToken cancellationToken)
    {
        return _sender.GetFileAsync($"legal/documents/{documentId:D}/{Uri.EscapeDataString(language)}", cancellationToken);
    }

    public static Error Translate(string code) => code switch
    {
        "platform.legal.version_changed" => OperationsLegalErrors.VersionChanged,
        "platform.legal.nothing_to_accept" => OperationsLegalErrors.NothingToAccept,
        _ => OperationsLegalErrors.Refused
    };
}
