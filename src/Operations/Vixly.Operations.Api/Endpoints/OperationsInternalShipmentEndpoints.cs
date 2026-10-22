using Vixly.Contracts.Operations.Internal;
using Vixly.Contracts.Operations.Packages;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.UseCases.Shipments.CancelMyOperationsShipment;
using Vixly.Operations.Application.UseCases.Shipments.ChangeMyOperationsShipment;
using Vixly.Operations.Application.UseCases.Shipments.CreateMyOperationsShipment;
using Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipment;
using Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentChoices;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalShipmentEndpoints.cs
//
// PURPOSE:
// The customer's own shipments, for the Customer portal (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Choices for a new shipment; create one with its first package; read, change and cancel one
//
// IMPORTANT NOTES:
// - Customer scope only; the session header names the customer and every read checks ownership (S-3)
// - The customer writes every instruction (D-C6); staff never type them
// =========================================================
public static class OperationsInternalShipmentEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalShipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder shipments = endpoints.MapGroup("/internal/v1/customers/me/shipments")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        shipments.MapGet("/choices", async (HttpContext http, GetMyOperationsShipmentChoicesUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsShipmentChoicesResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new GetMyOperationsShipmentChoicesRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            return response.Succeeded
                ? Results.Ok(new CustomerShipmentChoicesDto(response.Origins.Select(Choice).ToList(), response.Branches.Select(Choice).ToList(), response.Pickup, response.HomeDelivery,
                    response.RecipientName, response.RecipientPhone, response.Countries,
                    response.Destinations.Select(OperationsCustomerRouteDtos.Destination).ToList(),
                    response.Insurance.Select(plan => OperationsCustomerRouteDtos.Insurance(plan, response.UtcNow, response.CurrencyCode)).OfType<CustomerInsuranceTermsDto>().ToList(),
                    response.Services,
                    // Only the modes of the services the company offers the customer (review).
                    response.Liability.Where(item => response.Services.Count == 0 || response.Services.Any(service => Vixly.Operations.Domain.Charges.OperationsTransportModes.Of(service) == item.Mode))
                        .Select(item => new CustomerLiabilityDto(item.Mode.ToString(), item.Basis.ToString(), item.Amount,
                        item.WeightUnit == Vixly.Operations.Domain.Settings.OperationsWeightUnit.Pound ? "lb" : "kg", item.Cap, item.CustomerText, response.CurrencyCode, item.CapPerShipment)).ToList(),
                    response.AllowCollect))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        shipments.MapPost("/", async (HttpContext http, CustomerShipmentBody body, CreateMyOperationsShipmentUseCase useCase, CancellationToken cancellationToken) =>
        {
            CreateMyOperationsShipmentResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http), new CreateMyOperationsShipmentRequest
            {
                SessionToken = OperationsInternalPackageEndpoints.Session(http),
                OriginWarehouseId = body.OriginWarehouseId,
                Instructions = Draft(body.Instructions),
                // Slice 4D: a new package only when the customer typed one.
                Package = body.Package is { } package && !string.IsNullOrWhiteSpace(package.TrackingNumber)
                    ? new OperationsPackageDraft(package.Carrier, package.TrackingNumber, package.Store, package.Contents, package.DeclaredValue, package.ExpectedDate)
                    : null,
                PreAlertIds = body.PreAlertIds ?? Array.Empty<Guid>(),
                ParcelIds = body.ParcelIds ?? Array.Empty<Guid>(),
                RequestKey = body.RequestKey
            }, cancellationToken);

            return response.Succeeded
                ? Results.Ok(new CustomerShipmentCreatedDto(response.ShipmentId, response.PreAlertId, response.ParcelId))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        shipments.MapGet("/{shipmentId:guid}", async (HttpContext http, Guid shipmentId, GetMyOperationsShipmentUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsShipmentResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new GetMyOperationsShipmentRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http), ShipmentId = shipmentId }, cancellationToken);

            return response.Shipment is { } shipment
                ? Results.Ok(new CustomerShipmentDetailDto(
                    OperationsInternalPackageEndpoints.Shipment(shipment),
                    response.PreAlerts.Select(OperationsInternalPackageEndpoints.PreAlert).ToList(),
                    response.Parcels.Select(parcel => OperationsInternalPackageEndpoints.Parcel(parcel, response.WeightUnit, response.DimensionUnit)).ToList(),
                    response.CanCancel,
                    OperationsInternalPackageEndpoints.Weight(response.WeightUnit),
                    OperationsInternalPackageEndpoints.Length(response.DimensionUnit)))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        shipments.MapPut("/{shipmentId:guid}", async (HttpContext http, Guid shipmentId, CustomerShipmentInstructionsBody body, ChangeMyOperationsShipmentUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            ChangeMyOperationsShipmentResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http), new ChangeMyOperationsShipmentRequest
            {
                SessionToken = OperationsInternalPackageEndpoints.Session(http),
                ShipmentId = shipmentId,
                Instructions = Draft(body)
            }, cancellationToken);

            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        // Slice 7 step 2 (R-6): who may collect the customer's booked shipment.
        shipments.MapPut("/{shipmentId:guid}/collector", async (HttpContext http, Guid shipmentId, CustomerCollectorBody body,
            Vixly.Operations.Application.UseCases.Pickup.ChangeMyOperationsCollector.ChangeMyOperationsCollectorUseCase useCase, CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Pickup.ChangeMyOperationsCollector.ChangeMyOperationsCollectorResponse response = await useCase.ExecuteAsync(
                OperationsInternalPackageEndpoints.Context(http), new Vixly.Operations.Application.UseCases.Pickup.ChangeMyOperationsCollector.ChangeMyOperationsCollectorRequest
                {
                    SessionToken = OperationsInternalPackageEndpoints.Session(http), ShipmentId = shipmentId, Name = body?.Name, Phone = body?.Phone
                }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        shipments.MapDelete("/{shipmentId:guid}", async (HttpContext http, Guid shipmentId, CancelMyOperationsShipmentUseCase useCase, CancellationToken cancellationToken) =>
        {
            CancelMyOperationsShipmentResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new CancelMyOperationsShipmentRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http), ShipmentId = shipmentId }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        // Slice 5B: the insurance certificate of the customer's own insured shipment.
        shipments.MapGet("/{shipmentId:guid}/certificate", async (HttpContext http, Guid shipmentId,
            Vixly.Operations.Application.UseCases.Insurance.GetMyOperationsInsuranceCertificate.GetMyOperationsInsuranceCertificateUseCase useCase, CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Insurance.GetMyOperationsInsuranceCertificate.GetMyOperationsInsuranceCertificateResponse response = await useCase.ExecuteAsync(
                OperationsInternalPackageEndpoints.Context(http),
                new Vixly.Operations.Application.UseCases.Insurance.GetMyOperationsInsuranceCertificate.GetMyOperationsInsuranceCertificateRequest
                {
                    SessionToken = OperationsInternalPackageEndpoints.Session(http), ShipmentId = shipmentId
                }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content, "application/pdf", response.FileName);
        });

        // Slice 5A: the packing list or the pro forma commercial invoice of the customer's own shipment.
        shipments.MapGet("/{shipmentId:guid}/documents/{kind}", async (HttpContext http, Guid shipmentId, string kind,
            Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentDocument.GetMyOperationsShipmentDocumentUseCase useCase, CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentDocument.GetMyOperationsShipmentDocumentResponse response = await useCase.ExecuteAsync(
                OperationsInternalPackageEndpoints.Context(http),
                new Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentDocument.GetMyOperationsShipmentDocumentRequest
                {
                    SessionToken = OperationsInternalPackageEndpoints.Session(http), ShipmentId = shipmentId, Kind = DocumentKind(kind)
                }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content, "application/pdf", response.FileName);
        });

        return endpoints;
    }

    // The document's name in addresses; anything else is no document.
    public static OperationsShipmentDocumentKind DocumentKind(string? kind) => kind switch
    {
        "packing-list" => OperationsShipmentDocumentKind.PackingList,
        "commercial-invoice" => OperationsShipmentDocumentKind.CommercialInvoice,
        _ => 0
    };

    private static CustomerChoiceDto Choice(OperationsChoice choice) => new(choice.Id, choice.Name, choice.Detail);

    internal static OperationsShipmentDraft Draft(CustomerShipmentInstructionsBody? body) =>
        new(body?.Delivery, body?.BranchId, body?.Address?.Line1, body?.Address?.Line2, body?.Address?.City, body?.Address?.Region, body?.Address?.PostalCode, body?.Address?.CountryCode,
            body?.RecipientName, body?.RecipientPhone, body?.Consolidate ?? false, body?.Refrigerated ?? false, body?.Insured ?? false, body?.DestinationId,
            body?.InsuranceAccepted ?? false, body?.Service, body?.ParcelCount, body?.InsurancePlanId, body?.Collect ?? false);
}
