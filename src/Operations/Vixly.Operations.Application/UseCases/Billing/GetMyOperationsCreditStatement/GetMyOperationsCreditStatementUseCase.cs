using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditStatement;

// =========================================================
// FILE: GetMyOperationsCreditStatementUseCase.cs
//
// PURPOSE:
// The customer's own credit statement as a PDF on the portal (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The signed-in customer only; none without an account
// =========================================================
public sealed class GetMyOperationsCreditStatementUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsCreditStatements _statements;

    public GetMyOperationsCreditStatementUseCase(
        OperationsCustomerAccounts accounts,
        OperationsCreditStatements statements)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _statements = statements ?? throw new ArgumentNullException(nameof(statements));
    }

    public async Task<GetMyOperationsCreditStatementResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsCreditStatementRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsCreditStatementResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        return await _statements.RenderAsync(signedIn.Customer.TenantId, signedIn.Customer.Id, cancellationToken) is { } pdf
            ? GetMyOperationsCreditStatementResponse.Success(pdf)
            : GetMyOperationsCreditStatementResponse.Failure(OperationsCreditErrors.NotFound);
    }
}
