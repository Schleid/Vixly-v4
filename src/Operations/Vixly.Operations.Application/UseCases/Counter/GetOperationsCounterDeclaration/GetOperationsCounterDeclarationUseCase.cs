using System.Globalization;
using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterDeclaration;

// =========================================================
// FILE: GetOperationsCounterDeclarationUseCase.cs
//
// PURPOSE:
// The page the customer signs at the counter: their declaration of each parcel and the insurance they chose or declined (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve
// - Printed in the customer language; the company keeps the signed paper
// =========================================================
public sealed class GetOperationsCounterDeclarationUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsDeclarationWriter _declarations;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetOperationsCounterDeclarationUseCase(
        OperationsCounterDesk desk,
        IOperationsReceivingRepository receiving,
        IOperationsCustomerRepository customers,
        OperationsDeclarationWriter declarations,
        IOperationsLabelRenderer renderer,
        OperationsNumbering numbering,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsCounterDeclarationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCounterDeclarationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCounterDeclarationResponse.Failure(refused);
        }

        // Only a shipment leaving from one of the clerk's counters (as every counter action).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { HasInstructions: true } shipment
            || !counters.Any(counter => counter.Id == shipment.OriginWarehouseId)
            || await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { } customer)
        {
            return GetOperationsCounterDeclarationResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // The page carries the shipment's number and barcode: a shipment without one gets it now (as its other documents); two at once: the second reads it back.
        if (shipment.Number is null)
        {
            shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));

            if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                _unitOfWork.DiscardChanges();

                if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { Number: not null } numbered)
                {
                    return GetOperationsCounterDeclarationResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
                }

                shipment = numbered;
            }
        }

        OperationsCustomerDeclaration declaration = await _declarations.BuildAsync(shipment, customer, cancellationToken);

        // In the customer's language: they read and sign it.
        CultureInfo before = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(customer.PreferredLanguage.Value);
            return GetOperationsCounterDeclarationResponse.Success(_renderer.RenderCustomerDeclaration(declaration), "declaration-" + (shipment.Number ?? shipment.Id.ToString("N")) + ".pdf");
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
