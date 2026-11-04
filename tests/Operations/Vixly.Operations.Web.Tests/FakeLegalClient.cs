using Vixly.Operations.Application.Legal;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeLegalClient.cs
//
// PURPOSE:
// Stands in for Platform's Terms calls in the HTTP tests (Slice 1C).
//
// RESPONSIBILITIES:
// - Answer a Terms status and record acceptances
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FakeLegalClient : IOperationsLegalClient
{
    public static readonly Guid TermsId = Guid.Parse("8b0b3c35-0000-4000-8000-000000000001");

    public OperationsLegalStatus Status { get; set; } = new("not_required", null, false, null);

    public List<(OperationsPlatformActor Actor, Guid DocumentId, string Sha256, string Language)> Acceptances { get; } = new();

    public Action<Guid>? OnAccepted { get; set; }

    public static OperationsLegalStatus Overdue() => new("overdue", DateTimeOffset.UtcNow.AddDays(-2), true,
        new OperationsLegalTerms(TermsId, "2026-12", "en", new string('f', 64), "Shorter notice periods.", new[] { new OperationsLegalPart(Guid.NewGuid(), "privacy", "2026-10", null) }));

    public Task<OperationsLegalStatus> GetStatusAsync(OperationsPlatformActor actor, string language, CancellationToken cancellationToken) => Task.FromResult(Status);

    public Task<OperationsPlatformCall<bool>> AcceptAsync(OperationsPlatformActor actor, Guid documentId, string sha256, string language, CancellationToken cancellationToken)
    {
        if (actor.Role != "owner" || Status.Terms is not { } terms || terms.DocumentId != documentId || terms.Sha256 != sha256)
        {
            return Task.FromResult(OperationsPlatformCall<bool>.Refused(OperationsLegalErrors.VersionChanged));
        }

        Acceptances.Add((actor, documentId, sha256, language));
        Status = new OperationsLegalStatus("accepted", null, false, null);
        OnAccepted?.Invoke(actor.TenantId);
        return Task.FromResult(OperationsPlatformCall<bool>.Ok(true));
    }

    public Task<OperationsLegalFile?> GetFileAsync(Guid documentId, string language, CancellationToken cancellationToken) =>
        Task.FromResult<OperationsLegalFile?>(documentId == TermsId ? new OperationsLegalFile("%PDF-1.7 terms"u8.ToArray(), "application/pdf") : null);
}
