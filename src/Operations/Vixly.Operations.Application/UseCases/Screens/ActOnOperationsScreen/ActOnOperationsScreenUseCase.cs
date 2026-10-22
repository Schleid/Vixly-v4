using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Screens;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.ActOnOperationsScreen;

// =========================================================
// FILE: ActOnOperationsScreenUseCase.cs
//
// PURPOSE:
// What the customer does on the customer screen (Slice 5F step 3, P-2, P-3, P-9): choose Zelle and accept its terms, pay by card on Stripe’s page, go back, or sign the declaration.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - No sign-in: the screen is known only by its own secret; each action only from the step that offers it
// - Card numbers never pass here: the customer pays on Stripe's page (P-4)
// =========================================================
public sealed class ActOnOperationsScreenUseCase
{
    private readonly OperationsScreenDesk _desk;

    private readonly IOperationsScreenRepository _screens;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsCardDesk _cards;

    private readonly IOperationsFileStore _files;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ActOnOperationsScreenUseCase(
        OperationsScreenDesk desk,
        IOperationsScreenRepository screens,
        IOperationsSettingsRepository settings,
        OperationsCardDesk cards,
        IOperationsFileStore files,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    private const int SignatureMaxBytes = 200 * 1024;

    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public async Task<ActOnOperationsScreenResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ActOnOperationsScreenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (context.TenantId is null || await _desk.FromSecretAsync(request.Secret, cancellationToken) is not { } screen)
        {
            return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.NotPaired);
        }

        OperationsScreenState current = await _desk.StateAsync(screen, cancellationToken);

        if (!string.Equals(request.Version, current.Version, StringComparison.Ordinal))
        {
            return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.StepInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsInvoice? invoice = current.Invoice;

        try
        {
            switch (request.Action)
            {
                case OperationsScreenAction.Back:
                    screen.Back(utcNow);
                    break;

                case OperationsScreenAction.Zelle:
                    if (invoice is not { Owed: > 0m } || await _settings.FindAsync(cancellationToken) is not { } settings || !settings.Accepts(OperationsPaymentMethod.Zelle))
                    {
                        return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.NotOffered);
                    }

                    screen.ChooseZelle(utcNow);
                    break;

                case OperationsScreenAction.AcceptZelle:
                    if (invoice is null || await _settings.FindAsync(cancellationToken) is not { } zelleSettings)
                    {
                        return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.NotOffered);
                    }

                    screen.AcceptZelle(utcNow);
                    _screens.Add(OperationsScreenRecord.ZelleTerms(screen, invoice.Id, invoice.Language, _desk.ZelleTermsHash(zelleSettings, invoice.Language), utcNow));
                    _audit.Record(null, "ops.payments.zelle_terms_accepted", nameof(OperationsInvoice), screen.InvoiceId,
                        after: new { invoice = invoice?.Number, screen = screen.Name, clerk = screen.SentByUserId });
                    break;

                case OperationsScreenAction.Card:
                    return await CardAsync(screen, invoice, request.ReturnUrl, utcNow, cancellationToken);

                case OperationsScreenAction.Sign:
                    return request.Accepted
                        ? await SignAsync(screen, current.Shipment, request.Signature, utcNow, cancellationToken)
                        : ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.AcceptRequired);

                default:
                    return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.StepInvalid);
            }
        }
        catch (DomainException exception)
        {
            return ActOnOperationsScreenResponse.Failure(exception.Error);
        }

        return await SaveAsync(cancellationToken);
    }

    // Stripe's page for what the invoice owes; the one already open for it on a page is shown again rather than a new one.
    private async Task<ActOnOperationsScreenResponse> CardAsync(OperationsCustomerScreen screen, OperationsInvoice? invoice, string? returnUrl, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        string? secretKey = await _cards.SecretKeyAsync(cancellationToken);

        if (invoice is not { Owed: > 0m } || secretKey is null || !OperationsCardDesk.Offered(await _settings.FindAsync(cancellationToken), true)
            || returnUrl is null || !returnUrl.StartsWith("https://", StringComparison.Ordinal))
        {
            return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.NotOffered);
        }

        // Review: a reader payment or a portal payment already started is never cancelled from the screen; a page for the same amount is shown again.
        if (await _cards.Charges.FindStartedAsync(invoice.Id, cancellationToken) is { } open)
        {
            if (open.Channel != OperationsCardChannel.PaymentPage)
            {
                return ActOnOperationsScreenResponse.Failure(OperationsCardErrors.AlreadyStarted);
            }

            if (open.Amount == invoice.Owed)
            {
                screen.ChooseCard(open.Id, utcNow);
                return await SaveAsync(cancellationToken);
            }
        }

        (OperationsCardCharge? charge, string? _, Error? refused) = await _cards.StartAsync(secretKey, invoice, OperationsCardChannel.PaymentPage, null, screen.SentByUserId, null,
            _ => returnUrl, cancellationToken);

        if (refused is not null)
        {
            return ActOnOperationsScreenResponse.Failure(refused);
        }

        screen.ChooseCard(charge!.Id, utcNow);
        return await SaveAsync(cancellationToken);
    }

    // The customer's signature on their declaration: the shipment's own signature (Slice 5D), the clerk who sent it as the one present.
    // The shipment is the one read for the version the customer saw: changed since, the version check refused the signature already.
    private async Task<ActOnOperationsScreenResponse> SignAsync(OperationsCustomerScreen screen, OperationsShipment? shown, byte[]? image, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (screen.Step != OperationsScreenStep.Signature || screen.SentByUserId is not { } clerk || shown is not { HasInstructions: true } shipment
            || shipment.Id != screen.ShipmentId || shipment.OriginWarehouseId != screen.WarehouseId)
        {
            return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.StepInvalid);
        }

        if (image is not { Length: > 60 and <= SignatureMaxBytes } || !image.AsSpan(0, 8).SequenceEqual(Png))
        {
            return ActOnOperationsScreenResponse.Failure(OperationsScreenErrors.SignatureRequired);
        }

        string? previous = shipment.CustomerSignatureKey;
        string key = await _files.SaveAsync(shipment.TenantId, "signatures", image, "image/png", cancellationToken);

        try
        {
            shipment.RecordSignature(key, clerk, utcNow);
            screen.Signed(utcNow);
        }
        catch (DomainException exception)
        {
            await _files.DeleteAsync(shipment.TenantId, key, cancellationToken);
            return ActOnOperationsScreenResponse.Failure(exception.Error);
        }

        _audit.Record(null, "ops.shipments.customer_signed", nameof(OperationsShipment), shipment.Id,
            after: new { number = shipment.Number, screen = screen.Name, clerk, insured = shipment.Options.HasFlag(OperationsShipmentOptions.Insured), plan = shipment.InsurancePlanId, accepted = true });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            if (previous is not null && previous != key)
            {
                await _files.DeleteAsync(shipment.TenantId, previous, cancellationToken);
            }

            return ActOnOperationsScreenResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        await _files.DeleteAsync(shipment.TenantId, key, cancellationToken);
        return ActOnOperationsScreenResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private async Task<ActOnOperationsScreenResponse> SaveAsync(CancellationToken cancellationToken)
    {
        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ActOnOperationsScreenResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ActOnOperationsScreenResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
