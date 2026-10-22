using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Screens;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.GetOperationsScreen;

// =========================================================
// FILE: GetOperationsScreenUseCase.cs
//
// PURPOSE:
// What a customer screen shows now (Slice 5F step 3, P-2, P-3, P-9): the invoice and the methods, the Zelle steps, the card, or the declaration to sign.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - No sign-in: the screen is known only by its own secret, and shows only what a clerk sent it now
// - Never the customer's other invoices, never staff pages
// =========================================================
public sealed class GetOperationsScreenUseCase
{
    private readonly OperationsScreenDesk _desk;

    private readonly OperationsCardDesk _cards;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsDeclarationWriter _declarations;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetOperationsScreenUseCase(
        OperationsScreenDesk desk,
        OperationsCardDesk cards,
        IOperationsSettingsRepository settings,
        IOperationsCustomerRepository customers,
        OperationsDeclarationWriter declarations,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsScreenResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsScreenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (context.TenantId is null || await _desk.FromSecretAsync(request.Secret, cancellationToken) is not { } screen)
        {
            return GetOperationsScreenResponse.Failure(OperationsScreenErrors.NotPaired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateTimeOffset? seen = screen.LastSeenUtc;
        screen.Seen(utcNow);
        bool seenNow = screen.LastSeenUtc != seen;
        (OperationsInvoice? invoice, OperationsCardCharge? charge, OperationsShipment? shipment, string _) = await _desk.StateAsync(screen, cancellationToken);

        // Review: paid meanwhile, the thank-you page; left too long, cleared. Never the last customer's invoice or declaration for the next one.
        if ((screen.Tidy(invoice is { } shown && shown.Owed <= 0m, utcNow) || seenNow) && !await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return GetOperationsScreenResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        // Waiting for the card: Stripe is asked when the page opens and about once a minute while it waits (the webhook is the usual way), saved apart from the screen.
        if (screen.Step == OperationsScreenStep.Card && charge is { Status: OperationsCardChargeStatus.Started } && (!request.VersionOnly || seenNow)
            && await _cards.SecretKeyAsync(cancellationToken) is { } secretKey)
        {
            try
            {
                await _cards.RefreshAsync(secretKey, charge, cancellationToken);
            }
            catch (OperationsCardUnavailableException)
            {
                // Asked again on the next look.
            }
        }

        string version = OperationsScreenDesk.Version(screen, invoice, charge, shipment);

        if (request.VersionOnly)
        {
            return GetOperationsScreenResponse.Success(screen.Id, screen.Name, screen.Step, version, null, null);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        OperationsScreenView view = new() { Invoice = invoice, CardStatus = charge?.Status, CardPageUrl = charge?.Status == OperationsCardChargeStatus.Started ? charge.PageUrl : null };
        string? language = invoice?.Language;

        if (invoice is not null && settings is not null)
        {
            bool connected = await _cards.SecretKeyAsync(cancellationToken) is not null;
            view = view with
            {
                Settled = invoice.Owed <= 0m,
                CardOffered = OperationsCardDesk.Offered(settings, connected),
                ZelleOffered = settings.Accepts(OperationsPaymentMethod.Zelle),
                ZelleHasQr = settings.ZelleQrKey is not null,
                ZelleTag = settings.ZelleTag,
                ZelleContact = settings.ZelleContact,
                ZelleName = settings.ZelleName,
                ZelleTerms = settings.ZelleTerms(invoice.Language),
                ZelleAccepted = screen.ZelleAcceptedUtc is not null
            };
        }

        if (shipment is not null && await _customers.GetAsync(shipment.CustomerId, cancellationToken) is { } customer)
        {
            language = customer.PreferredLanguage.Value;
            view = view with { Declaration = await _declarations.BuildAsync(shipment, customer, cancellationToken) };
        }

        return GetOperationsScreenResponse.Success(screen.Id, screen.Name, screen.Step, version, language, view);
    }
}
