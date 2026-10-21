namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsLegalClient.cs
//
// PURPOSE:
// Platform's Terms calls for Operations (Slice 8, exposed by Slice 9).
//
// RESPONSIBILITIES:
// - Read the Terms status
// - Record the Owner's acceptance
// - Read a published PDF
//
// IMPORTANT NOTES:
// - Platform checks the Owner role and that the version shown is current
// =========================================================
public interface IOperationsLegalClient
{
    Task<OperationsLegalStatus> GetStatusAsync(OperationsPlatformActor actor, string language, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<bool>> AcceptAsync(OperationsPlatformActor actor, Guid documentId, string sha256, string language, CancellationToken cancellationToken);

    Task<OperationsLegalFile?> GetFileAsync(Guid documentId, string language, CancellationToken cancellationToken);
}
