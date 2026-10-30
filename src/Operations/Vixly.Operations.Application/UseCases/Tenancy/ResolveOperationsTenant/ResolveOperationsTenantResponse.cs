using Vixly.Operations.Application.Tenancy;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tenancy.ResolveOperationsTenant;

// =========================================================
// FILE: ResolveOperationsTenantResponse.cs
//
// PURPOSE:
// Output of ResolveOperationsTenant.
//
// RESPONSIBILITIES:
// - Return the resolved host or an error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResolveOperationsTenantResponse
{
    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsTenantHost? Host { get; private init; }

    public static ResolveOperationsTenantResponse Success(OperationsTenantHost host)
    {
        return new ResolveOperationsTenantResponse { Succeeded = true, Host = host };
    }

    public static ResolveOperationsTenantResponse Failure(Error error)
    {
        return new ResolveOperationsTenantResponse { Succeeded = false, Error = error };
    }
}
