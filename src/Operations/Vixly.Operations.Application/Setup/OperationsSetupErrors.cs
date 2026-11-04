using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Setup;

// =========================================================
// FILE: OperationsSetupErrors.cs
//
// PURPOSE:
// Error codes of tenant setup (Slice 1).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code
//
// IMPORTANT NOTES:
// - Recorded on the outbox message when the owner cannot be invited
// =========================================================
public static class OperationsSetupErrors
{
    public static Error OwnerContactMissing { get; } = Error.Create("ops.setup.owner_contact_missing");
}
