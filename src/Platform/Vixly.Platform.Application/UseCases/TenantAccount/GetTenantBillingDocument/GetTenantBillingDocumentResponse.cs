using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantBillingDocument;

// =========================================================
// FILE: GetTenantBillingDocumentResponse.cs
//
// PURPOSE:
// One of the tenant's billing PDFs (call 22).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetTenantBillingDocumentResponse
{
    private GetTenantBillingDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[]? Content { get; private init; }

    public string FileName { get; private init; } = string.Empty;

    public static GetTenantBillingDocumentResponse Success(byte[]? content, string fileName)
    {
        return new GetTenantBillingDocumentResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetTenantBillingDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetTenantBillingDocumentResponse { Error = error };
    }
}
