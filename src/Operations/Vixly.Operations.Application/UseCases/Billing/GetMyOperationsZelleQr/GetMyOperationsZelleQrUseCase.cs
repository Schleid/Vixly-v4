using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsZelleQr;

// =========================================================
// FILE: GetMyOperationsZelleQrUseCase.cs
//
// PURPOSE:
// The company's Zelle QR picture for a signed-in customer paying on the portal (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Read the picture the company uploaded, for a signed-in customer, while it takes Zelle
//
// IMPORTANT NOTES:
// - The company's own payment details, the same as at the cashier
// =========================================================
public sealed class GetMyOperationsZelleQrUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsFileStore _files;

    public GetMyOperationsZelleQrUseCase(OperationsCustomerAccounts accounts, IOperationsSettingsRepository settings, IOperationsFileStore files)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetMyOperationsZelleQrResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsZelleQrRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsZelleQrResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _settings.FindAsync(cancellationToken) is not { } settings || !settings.OffersOnPortal(OperationsPaymentMethod.Zelle, false) || settings.ZelleQrKey is not { } key
            || await _files.ReadAsync(signedIn.Customer.TenantId, key, cancellationToken) is not { } content || OperationsDocumentInspector.TypeOf(content) is not { } type)
        {
            return GetMyOperationsZelleQrResponse.Failure(OperationsPaymentErrors.NotFound);
        }

        return GetMyOperationsZelleQrResponse.Success(content, type);
    }
}
