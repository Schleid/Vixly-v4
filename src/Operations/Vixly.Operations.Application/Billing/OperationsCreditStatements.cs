using System.Globalization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsCreditStatements.cs
//
// PURPOSE:
// Makes a credit account's statement (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Gather the company, the customer, the open items and the position on the company's day
// - Render it in the customer's language
//
// IMPORTANT NOTES:
// - None without an account; read only
// =========================================================
public sealed class OperationsCreditStatements
{
    private readonly IOperationsCreditRepository _credit;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCreditBook _book;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public OperationsCreditStatements(IOperationsCreditRepository credit, IOperationsCustomerRepository customers, OperationsCreditBook book, IOperationsSettingsRepository settings,
        IOperationsBrandRepository brands, IOperationsLabelRenderer renderer, TimeProvider timeProvider)
    {
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _book = book ?? throw new ArgumentNullException(nameof(book));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<byte[]?> RenderAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        if (await _customers.GetAsync(customerId, cancellationToken) is not { } customer || await _credit.FindByCustomerAsync(customerId, cancellationToken) is not { } account)
        {
            return null;
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(tenantId, _timeProvider.GetUtcNow());
        DateOnly today = OperationsCreditBook.Today(settings, _timeProvider.GetUtcNow());
        (IReadOnlyList<Domain.Billing.OperationsInvoice> items, OperationsCreditPosition position) = await _book.ReadAsync(account, customer.Id, today, cancellationToken);
        OperationsCreditStatement statement = new((await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, customer, account, today, settings.CurrencyCode,
            account.TermsOr(settings.CreditTermsDays), items.Where(item => item.OnAccount == OperationsAccountCharge.Charged).ToList(), position);
        CultureInfo before = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(customer.PreferredLanguage.Value);
            return _renderer.RenderCreditStatement(statement);
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
