using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Internal;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerPassword;
using Vixly.Operations.Application.UseCases.Customers.GetMyOperationsCustomer;
using Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomerPortalBrand;
using Vixly.Operations.Application.UseCases.Customers.RequestOperationsCustomerPasswordReset;
using Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerVerification;
using Vixly.Operations.Application.UseCases.Customers.ResetOperationsCustomerPassword;
using Vixly.Operations.Application.UseCases.Customers.SignInOperationsCustomer;
using Vixly.Operations.Application.UseCases.Customers.SignOutOperationsCustomer;
using Vixly.Operations.Application.UseCases.Customers.SignUpOperationsCustomer;
using Vixly.Operations.Application.UseCases.Customers.UpdateMyOperationsCustomer;
using Vixly.Operations.Application.UseCases.Customers.VerifyOperationsCustomerEmail;
using Vixly.Operations.Domain.Branding;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalCustomerEndpoints.cs
//
// PURPOSE:
// The customer portal's calls into Operations (Slice 2, design section 5).
//
// RESPONSIBILITIES:
// - Sign-up, confirmation, sign-in and out, password reset
// - The signed-in customer: profile, mailbox addresses, password
// - The published brand of the portal
//
// IMPORTANT NOTES:
// - Caller customer with a tenant in the token; the session travels in X-Customer-Session and is checked on every call (D-C4)
// =========================================================
public static class OperationsInternalCustomerEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder customers = endpoints.MapGroup("/internal/v1/customers")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        customers.MapGet("/brand", async (HttpContext http, GetOperationsCustomerPortalBrandUseCase useCase, Vixly.Operations.Application.Ports.IOperationsIdleTimesReader idle,
            CancellationToken cancellationToken) =>
        {
            GetOperationsCustomerPortalBrandResponse response = await useCase.ExecuteAsync(Context(http), new GetOperationsCustomerPortalBrandRequest(), cancellationToken);

            if (response.Settings is not { } brand)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            // The portal's warning and sign-out follow the company's inactivity time for customers (owner, 2026-10-27).
            Vixly.Operations.Application.Identity.OperationsIdleTimes times = await idle.GetAsync(cancellationToken);
            return Results.Ok(new CustomerPortalBrandDto(brand.DisplayName, brand.LogoImageId, brand.FaviconImageId, brand.PrimaryColour, brand.AccentColour,
                (int)times.Customer.TotalMinutes, (int)times.Warn.TotalMinutes));
        });

        customers.MapPost("/sign-up", async (HttpContext http, CustomerSignUpBody body, SignUpOperationsCustomerUseCase useCase, CancellationToken cancellationToken) =>
            Done(http, (await useCase.ExecuteAsync(Context(http), new SignUpOperationsCustomerRequest
            {
                FirstName = body.FirstName ?? string.Empty,
                LastName = body.LastName ?? string.Empty,
                CompanyName = body.CompanyName,
                Email = body.Email ?? string.Empty,
                Phone = body.Phone ?? string.Empty,
                Language = body.Language ?? "en",
                Password = body.Password ?? string.Empty,
                PortalSlug = body.PortalSlug ?? string.Empty
            }, cancellationToken)).Error));

        customers.MapPost("/verify", async (HttpContext http, CustomerTokenBody body, VerifyOperationsCustomerEmailUseCase useCase, CancellationToken cancellationToken) =>
        {
            VerifyOperationsCustomerEmailResponse response = await useCase.ExecuteAsync(Context(http), new VerifyOperationsCustomerEmailRequest { Token = body.Token ?? string.Empty }, cancellationToken);
            return response.Customer is { } customer ? Results.Ok(new CustomerSessionDto(response.SessionToken, Profile(customer))) : OperationsApiErrors.Problem(http, response.Error!);
        });

        customers.MapPost("/verification", async (HttpContext http, CustomerEmailBody body, ResendOperationsCustomerVerificationUseCase useCase, CancellationToken cancellationToken) =>
            Done(http, (await useCase.ExecuteAsync(Context(http), new ResendOperationsCustomerVerificationRequest { Email = body.Email ?? string.Empty, PortalSlug = body.PortalSlug ?? string.Empty }, cancellationToken)).Error));

        customers.MapPost("/sessions", async (HttpContext http, CustomerSignInBody body, SignInOperationsCustomerUseCase useCase, CancellationToken cancellationToken) =>
        {
            SignInOperationsCustomerResponse response = await useCase.ExecuteAsync(Context(http), new SignInOperationsCustomerRequest { Email = body.Email ?? string.Empty, Password = body.Password ?? string.Empty }, cancellationToken);
            return response.Customer is { } customer ? Results.Ok(new CustomerSessionDto(response.SessionToken, Profile(customer))) : OperationsApiErrors.Problem(http, response.Error!);
        });

        customers.MapDelete("/sessions/current", async (HttpContext http, SignOutOperationsCustomerUseCase useCase, CancellationToken cancellationToken) =>
            Done(http, (await useCase.ExecuteAsync(Context(http), new SignOutOperationsCustomerRequest { SessionToken = Session(http) }, cancellationToken)).Error));

        customers.MapPost("/password-reset", async (HttpContext http, CustomerEmailBody body, RequestOperationsCustomerPasswordResetUseCase useCase, CancellationToken cancellationToken) =>
            Done(http, (await useCase.ExecuteAsync(Context(http), new RequestOperationsCustomerPasswordResetRequest { Email = body.Email ?? string.Empty, PortalSlug = body.PortalSlug ?? string.Empty }, cancellationToken)).Error));

        customers.MapPost("/password-reset/complete", async (HttpContext http, CustomerPasswordResetBody body, ResetOperationsCustomerPasswordUseCase useCase, CancellationToken cancellationToken) =>
        {
            ResetOperationsCustomerPasswordResponse response = await useCase.ExecuteAsync(Context(http), new ResetOperationsCustomerPasswordRequest { Token = body.Token ?? string.Empty, Password = body.Password ?? string.Empty }, cancellationToken);
            return response.Customer is { } customer ? Results.Ok(new CustomerSessionDto(response.SessionToken, Profile(customer))) : OperationsApiErrors.Problem(http, response.Error!);
        });

        customers.MapGet("/me", async (HttpContext http, GetMyOperationsCustomerUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsCustomerResponse response = await useCase.ExecuteAsync(Context(http), new GetMyOperationsCustomerRequest { SessionToken = Session(http) }, cancellationToken);
            return response.Customer is { } customer ? Results.Ok(Profile(customer)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        customers.MapPut("/me", async (HttpContext http, CustomerProfileBody body, UpdateMyOperationsCustomerUseCase useCase, CancellationToken cancellationToken) =>
        {
            UpdateMyOperationsCustomerResponse response = await useCase.ExecuteAsync(Context(http), new UpdateMyOperationsCustomerRequest
            {
                SessionToken = Session(http),
                FirstName = body.FirstName ?? string.Empty,
                LastName = body.LastName ?? string.Empty,
                CompanyName = body.CompanyName,
                Phone = body.Phone ?? string.Empty,
                Language = body.Language ?? "en"
            }, cancellationToken);
            return response.Customer is { } customer ? Results.Ok(Profile(customer)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        customers.MapPost("/me/tour/dismiss", async (HttpContext http,
            Vixly.Operations.Application.UseCases.Customers.DismissMyOperationsCustomerTour.DismissMyOperationsCustomerTourUseCase useCase, CancellationToken cancellationToken) =>
        {
            var response = await useCase.ExecuteAsync(Context(http),
                new Vixly.Operations.Application.UseCases.Customers.DismissMyOperationsCustomerTour.DismissMyOperationsCustomerTourRequest { SessionToken = Session(http) }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        // The home address, asked at the first sign-in (owner, 2026-10-26).
        customers.MapPut("/me/address", async (HttpContext http, Vixly.Contracts.Operations.Packages.CustomerAddressDto body,
            Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerAddress.ChangeMyOperationsCustomerAddressUseCase useCase, CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerAddress.ChangeMyOperationsCustomerAddressResponse response = await useCase.ExecuteAsync(Context(http),
                new Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerAddress.ChangeMyOperationsCustomerAddressRequest
                {
                    SessionToken = Session(http),
                    Line1 = body.Line1,
                    Line2 = body.Line2,
                    City = body.City,
                    Region = body.Region,
                    PostalCode = body.PostalCode,
                    CountryCode = body.CountryCode
                }, cancellationToken);
            return response.Customer is { } customer ? Results.Ok(Profile(customer)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        // Slice 9: how the customer gets the company's notices (email, SMS or WhatsApp), with their consent to texts.
        customers.MapGet("/me/notices", async (HttpContext http, Vixly.Operations.Application.UseCases.Customers.GetMyOperationsNotices.GetMyOperationsNoticesUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            Vixly.Operations.Application.UseCases.Customers.GetMyOperationsNotices.GetMyOperationsNoticesResponse response = await useCase.ExecuteAsync(Context(http),
                new Vixly.Operations.Application.UseCases.Customers.GetMyOperationsNotices.GetMyOperationsNoticesRequest { SessionToken = Session(http) }, cancellationToken);
            return response.Succeeded
                ? Results.Ok(new CustomerNoticesDto(Channel(response.Channel), response.Offered.Select(Channel).ToList(), response.Phone))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        customers.MapPut("/me/notices", async (HttpContext http, CustomerNoticesBody body,
            Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsNotices.ChangeMyOperationsNoticesUseCase useCase, CancellationToken cancellationToken) =>
            Done(http, (await useCase.ExecuteAsync(Context(http), new Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsNotices.ChangeMyOperationsNoticesRequest
            {
                SessionToken = Session(http),
                Channel = body.Channel switch
                {
                    "sms" => Vixly.Operations.Domain.Texts.OperationsNoticeChannel.Sms,
                    "whatsapp" => Vixly.Operations.Domain.Texts.OperationsNoticeChannel.WhatsApp,
                    "email" => Vixly.Operations.Domain.Texts.OperationsNoticeChannel.Email,
                    _ => (Vixly.Operations.Domain.Texts.OperationsNoticeChannel)99
                },
                Consent = body.Consent
            }, cancellationToken)).Error));

        customers.MapPost("/me/password", async (HttpContext http, CustomerPasswordChangeBody body, ChangeMyOperationsCustomerPasswordUseCase useCase, CancellationToken cancellationToken) =>
            Done(http, (await useCase.ExecuteAsync(Context(http), new ChangeMyOperationsCustomerPasswordRequest
            {
                SessionToken = Session(http),
                CurrentPassword = body.CurrentPassword ?? string.Empty,
                NewPassword = body.NewPassword ?? string.Empty
            }, cancellationToken)).Error));

        return endpoints;
    }

    private static OperationsAuthorizationContext Context(HttpContext http) =>
        OperationsAuthorizationContext.ForTenant(OperationsInternalAuthentication.Token(http).TenantId ?? throw new InvalidOperationException("The endpoint needs a tenant."));

    private static string Session(HttpContext http) => http.Request.Headers[OperationsCallerClaims.SessionHeader].ToString();

    private static IResult Done(HttpContext http, Error? error) => error is null ? Results.NoContent() : OperationsApiErrors.Problem(http, error);

    private static string Channel(Vixly.Operations.Domain.Texts.OperationsNoticeChannel channel) => channel switch
    {
        Vixly.Operations.Domain.Texts.OperationsNoticeChannel.Sms => "sms",
        Vixly.Operations.Domain.Texts.OperationsNoticeChannel.WhatsApp => "whatsapp",
        _ => "email"
    };

    private static CustomerProfileDto Profile(OperationsCustomerProfile customer) =>
        new(customer.CustomerId, customer.Code, customer.FirstName, customer.LastName, customer.CompanyName, customer.Email, customer.Phone, customer.Language,
            customer.Mailboxes.Select(mailbox => new CustomerMailboxDto(mailbox.WarehouseName, mailbox.Addressee, mailbox.Line1, mailbox.Line2, mailbox.City, mailbox.Region, mailbox.PostalCode, mailbox.CountryCode)).ToList(),
            customer.HomeAddress is { } home ? new Vixly.Contracts.Operations.Packages.CustomerAddressDto(home.Line1, home.Line2, home.City, home.Region, home.PostalCode, home.CountryCode) : null,
            customer.WelcomeTourPending);
}
