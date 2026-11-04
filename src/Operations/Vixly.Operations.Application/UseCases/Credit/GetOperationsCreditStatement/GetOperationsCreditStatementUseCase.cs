using System.Globalization;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditStatement;

// =========================================================
// FILE: GetOperationsCreditStatementUseCase.cs
//
// PURPOSE:
// A credit account's statement as a PDF (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customers.credit
// - In the customer's language
// =========================================================
public sealed class GetOperationsCreditStatementUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsCreditStatements _statements;

    public GetOperationsCreditStatementUseCase(
        OperationsAccess access,
        OperationsCreditStatements statements)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _statements = statements ?? throw new ArgumentNullException(nameof(statements));
    }

    public async Task<GetOperationsCreditStatementResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCreditStatementRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCreditStatementResponse.Failure(refused);
        }

        return await _statements.RenderAsync(context.RequiredTenantId, request.CustomerId, cancellationToken) is { } pdf
            ? GetOperationsCreditStatementResponse.Success(pdf)
            : GetOperationsCreditStatementResponse.Failure(OperationsCreditErrors.NotFound);
    }
}
