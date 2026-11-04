namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: IOperationsAuthorizationGate.cs
//
// PURPOSE:
// Checks a permission before a use case runs (S-1).
//
// RESPONSIBILITIES:
// - Refuse a caller that lacks the permission or acts outside its tenant
//
// IMPORTANT NOTES:
// - Throws UnauthorizedAccessException on refusal, as Platform's gate
// - The permission catalogue and the implementation arrive in Slice 1
// =========================================================
public interface IOperationsAuthorizationGate
{
    Task AuthorizeAsync(OperationsAuthorizationContext context, string permissionCode, CancellationToken cancellationToken);
}
