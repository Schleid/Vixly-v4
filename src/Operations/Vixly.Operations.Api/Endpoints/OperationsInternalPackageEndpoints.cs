using Vixly.Contracts.Operations.Internal;
using Vixly.Contracts.Operations.Packages;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.UseCases.Receiving.CancelMyOperationsPreAlert;
using Vixly.Operations.Application.UseCases.Receiving.CreateMyOperationsPreAlert;
using Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcelPhoto;
using Vixly.Operations.Application.UseCases.Receiving.ListMyOperationsPackages;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalPackageEndpoints.cs
//
// PURPOSE:
// The customer portal's package calls (Slice 3B, design section 7).
//
// RESPONSIBILITIES:
// - Pre-alerts: write and withdraw
// - Packages: list, one package with its history and photos, a photo file
//
// IMPORTANT NOTES:
// - Customer scope only; the session header names the customer and every read checks ownership (S-3)
// - Weight and sizes leave in the company's units; history leaves already reduced to what the customer may see
// =========================================================
public static class OperationsInternalPackageEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalPackageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder packages = endpoints.MapGroup("/internal/v1/customers/me")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        packages.MapGet("/packages", async (HttpContext http, ListMyOperationsPackagesUseCase useCase, CancellationToken cancellationToken) =>
        {
            ListMyOperationsPackagesResponse response = await useCase.ExecuteAsync(Context(http), new ListMyOperationsPackagesRequest { SessionToken = Session(http) }, cancellationToken);

            return response.Succeeded
                ? Results.Ok(new CustomerPackagesDto(
                    response.PreAlerts.Select(PreAlert).ToList(),
                    response.Parcels.Select(parcel => Parcel(parcel, response.WeightUnit, response.DimensionUnit)).ToList(),
                    Weight(response.WeightUnit),
                    Length(response.DimensionUnit),
                    response.Shipments.Select(Shipment).ToList()))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        packages.MapPost("/pre-alerts", async (HttpContext http, CustomerPreAlertBody body, CreateMyOperationsPreAlertUseCase useCase, CancellationToken cancellationToken) =>
        {
            CreateMyOperationsPreAlertResponse response = await useCase.ExecuteAsync(Context(http), new CreateMyOperationsPreAlertRequest
            {
                SessionToken = Session(http),
                Carrier = body.Carrier,
                TrackingNumber = body.TrackingNumber,
                Store = body.Store,
                Contents = body.Contents,
                DeclaredValue = body.DeclaredValue,
                ExpectedDate = body.ExpectedDate,
                ShipmentId = body.ShipmentId,
                WarehouseId = body.WarehouseId
            }, cancellationToken);

            return response.Succeeded ? Results.Ok(new CustomerPreAlertCreatedDto(response.PreAlertId, response.ParcelId)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        // Slice 3E: the customer answers a hold; a document travels in the body (up to 10 MB).
        packages.MapPost("/packages/{parcelId:guid}/answers", async (HttpContext http, Guid parcelId, CustomerHoldAnswerBody body,
            Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold.AnswerMyOperationsHoldUseCase useCase, CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold.AnswerMyOperationsHoldResponse response = await useCase.ExecuteAsync(Context(http),
                new Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold.AnswerMyOperationsHoldRequest
                {
                    SessionToken = Session(http),
                    ParcelId = parcelId,
                    Reason = Enum.TryParse(body.Reason, ignoreCase: false, out Vixly.Operations.Domain.Tracking.OperationsHoldReasons reason) && !body.Reason.Any(char.IsDigit)
                        ? reason
                        : Vixly.Operations.Domain.Tracking.OperationsHoldReasons.None,
                    Contents = body.Contents,
                    Value = body.Value,
                    Choice = body.Choice,
                    Note = body.Note,
                    FileName = body.FileName,
                    Document = body.Document
                }, cancellationToken);

            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        // Slice 4D: a package (pre-alert or parcel) into one of the customer's shipments, moved, or taken out.
        packages.MapPost("/packages/move", async (HttpContext http, CustomerPackageMoveBody body,
            Vixly.Operations.Application.UseCases.Shipments.PlaceMyOperationsPackage.PlaceMyOperationsPackageUseCase useCase, CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Shipments.PlaceMyOperationsPackage.PlaceMyOperationsPackageResponse response = await useCase.ExecuteAsync(Context(http),
                new Vixly.Operations.Application.UseCases.Shipments.PlaceMyOperationsPackage.PlaceMyOperationsPackageRequest
                {
                    SessionToken = Session(http), PreAlertId = body.PreAlertId, ParcelId = body.ParcelId, ShipmentId = body.ShipmentId
                }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        packages.MapDelete("/pre-alerts/{preAlertId:guid}", async (HttpContext http, Guid preAlertId, CancelMyOperationsPreAlertUseCase useCase, CancellationToken cancellationToken) =>
        {
            CancelMyOperationsPreAlertResponse response = await useCase.ExecuteAsync(Context(http),
                new CancelMyOperationsPreAlertRequest { SessionToken = Session(http), PreAlertId = preAlertId }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        packages.MapGet("/packages/{parcelId:guid}", async (HttpContext http, Guid parcelId, GetMyOperationsParcelUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsParcelResponse response = await useCase.ExecuteAsync(Context(http), new GetMyOperationsParcelRequest { SessionToken = Session(http), ParcelId = parcelId }, cancellationToken);

            return response.Parcel is { } parcel
                ? Results.Ok(new CustomerParcelDetailDto(
                    Parcel(parcel, response.WeightUnit, response.DimensionUnit),
                    response.History.Select(line => new CustomerTrackingLineDto(line.Code, line.OccurredUtc, line.Arguments)).ToList(),
                    response.Photos.Select(photo => new CustomerParcelPhotoDto(photo.PhotoId, photo.Kind, photo.TakenUtc)).ToList(),
                    response.ReceiptNumber,
                    Weight(response.WeightUnit),
                    Length(response.DimensionUnit),
                    response.ActionNeeded,
                    response.Fee,
                    response.FeeCurrency,
                    response.Answerable.Select(reason => reason.ToString()).ToList(),
                    response.Answers.Select(answer => new CustomerHoldAnswerDto(answer.Reason.ToString(), answer.Kind.ToString(), answer.Status.ToString(), answer.SentUtc, answer.DeclinedBecause)).ToList(),
                    response.StorageFee,
                    response.StorageCurrency))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        packages.MapGet("/packages/{parcelId:guid}/photos/{photoId:guid}", async (HttpContext http, Guid parcelId, Guid photoId, GetMyOperationsParcelPhotoUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsParcelPhotoResponse response = await useCase.ExecuteAsync(Context(http),
                new GetMyOperationsParcelPhotoRequest { SessionToken = Session(http), ParcelId = parcelId, PhotoId = photoId }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.Bytes(response.Content, response.ContentType);
        });

        return endpoints;
    }

    internal static CustomerPreAlertDto PreAlert(OperationsPreAlertView preAlert) =>
        new(preAlert.PreAlertId, preAlert.Carrier, preAlert.TrackingNumber, preAlert.Store, preAlert.Contents, preAlert.DeclaredValue, preAlert.CurrencyCode, preAlert.ExpectedDate,
            preAlert.CreatedUtc, preAlert.ShipmentId, preAlert.WarehouseId);

    internal static CustomerShipmentDto Shipment(OperationsCustomerShipment shipment) =>
        new(shipment.ShipmentId, shipment.HasInstructions, shipment.IsOpen, shipment.OriginName,
            shipment.HasInstructions ? (shipment.BranchId is null ? "home" : "pickup") : null,
            shipment.BranchId, shipment.BranchName,
            shipment.Address is { } address ? new CustomerAddressDto(address.Line1, address.Line2, address.City, address.Region, address.PostalCode, address.CountryCode) : null,
            shipment.CountryCode, shipment.RecipientName, shipment.RecipientPhone,
            shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate), shipment.Options.HasFlag(OperationsShipmentOptions.Refrigerated), shipment.Options.HasFlag(OperationsShipmentOptions.Insured),
            shipment.Announced, shipment.Arrived, shipment.CreatedUtc, shipment.DestinationId, shipment.ServiceCode, shipment.OriginId, shipment.ParcelCount, shipment.Booked,
            shipment.BookedTotal, shipment.BookedCurrency, shipment.InsurancePlanId, shipment.CollectorName, shipment.CollectorPhone, shipment.PickedUpUtc);

    internal static CustomerParcelDto Parcel(OperationsCustomerParcel parcel, OperationsWeightUnit weight, OperationsDimensionUnit dimension) =>
        new(parcel.ParcelId, parcel.Number, parcel.Carrier, parcel.CarrierTrackingNumber, parcel.DeclaredContents, parcel.DeclaredValue, parcel.CurrencyCode,
            parcel.WeightKg is { } kg ? decimal.Round(OperationsUnits.ToShownWeight(kg, weight), 3) : null,
            Side(parcel.LengthCm, dimension), Side(parcel.WidthCm, dimension), Side(parcel.HeightCm, dimension),
            parcel.Milestone?.ToString(),
            Enum.GetValues<OperationsHoldReasons>().Where(reason => reason != OperationsHoldReasons.None && parcel.HoldReasons.HasFlag(reason)).Select(reason => reason.ToString()).ToList(),
            parcel.WarehouseName,
            parcel.ReceivedUtc,
            parcel.ShipmentId);

    private static decimal? Side(decimal? centimetres, OperationsDimensionUnit dimension) =>
        centimetres is { } cm ? decimal.Round(OperationsUnits.ToShownLength(cm, dimension), 1) : null;

    internal static string Weight(OperationsWeightUnit unit) => unit == OperationsWeightUnit.Pound ? "lb" : "kg";

    internal static string Length(OperationsDimensionUnit unit) => unit == OperationsDimensionUnit.Inch ? "in" : "cm";

    internal static OperationsAuthorizationContext Context(HttpContext http) =>
        OperationsAuthorizationContext.ForTenant(OperationsInternalAuthentication.Token(http).TenantId ?? throw new InvalidOperationException("The endpoint needs a tenant."));

    internal static string Session(HttpContext http) => http.Request.Headers[OperationsCallerClaims.SessionHeader].ToString();
}
