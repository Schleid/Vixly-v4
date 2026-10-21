using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentChoices;

// =========================================================
// FILE: GetMyOperationsShipmentChoicesUseCase.cs
//
// PURPOSE:
// What a customer may choose when creating a shipment (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetMyOperationsShipmentChoicesUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsShipmentIntake _intake;

    private readonly TimeProvider _timeProvider;

    private readonly Vixly.Operations.Application.Insurance.OperationsLiability _liability;

    public GetMyOperationsShipmentChoicesUseCase(
        OperationsCustomerAccounts accounts,
        OperationsShipmentIntake intake,
        TimeProvider timeProvider,
        Vixly.Operations.Application.Insurance.OperationsLiability liability)
    {
        _liability = liability ?? throw new ArgumentNullException(nameof(liability));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetMyOperationsShipmentChoicesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsShipmentChoicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsShipmentChoicesResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        OperationsDeliveryModes modes = await _intake.DeliveryModesAsync(cancellationToken);

        // The customer receives by default; they change it for someone else.
        IReadOnlyList<OperationsChoice> branches = await _intake.PickupBranchesAsync(cancellationToken);

        // Home delivery goes to the countries the company serves: those of its destination branches.
        return GetMyOperationsShipmentChoicesResponse.Success(await _intake.OriginsAsync(signedIn.Customer, cancellationToken), branches,
            modes.HasFlag(OperationsDeliveryModes.Pickup), modes.HasFlag(OperationsDeliveryModes.HomeDelivery), signedIn.Customer.FullName, signedIn.Customer.Phone,
            await _intake.DestinationCountriesAsync(cancellationToken), await _intake.DestinationsAsync(cancellationToken), await _intake.InsuranceAsync(cancellationToken),
            _timeProvider.GetUtcNow(), await _intake.CurrencyAsync(cancellationToken), await _intake.ServicesAsync(signedIn.Customer.Id, cancellationToken))
            .WithLiability(await _liability.ListAsync(cancellationToken));
    }
}
