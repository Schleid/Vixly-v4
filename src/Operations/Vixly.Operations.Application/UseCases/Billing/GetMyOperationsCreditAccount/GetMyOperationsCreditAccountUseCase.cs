using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Settings;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditAccount;

// =========================================================
// FILE: GetMyOperationsCreditAccountUseCase.cs
//
// PURPOSE:
// The customer's own credit account on the portal (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The signed-in customer only
// =========================================================
public sealed class GetMyOperationsCreditAccountUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsCreditRepository _credit;

    private readonly OperationsCreditBook _book;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetMyOperationsCreditAccountUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsCreditRepository credit,
        OperationsCreditBook book,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _book = book ?? throw new ArgumentNullException(nameof(book));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetMyOperationsCreditAccountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsCreditAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsCreditAccountResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsCreditAccount? account = await _credit.FindByCustomerAsync(signedIn.Customer.Id, cancellationToken);
        DateOnly today = OperationsCreditBook.Today(settings, _timeProvider.GetUtcNow());
        (IReadOnlyList<OperationsInvoice> items, OperationsCreditPosition position) = await _book.ReadAsync(account, signedIn.Customer.Id, today, cancellationToken);
        return GetMyOperationsCreditAccountResponse.Success(account, items, position, account?.TermsOr(settings.CreditTermsDays) ?? settings.CreditTermsDays, settings.CurrencyCode, today);
    }
}
