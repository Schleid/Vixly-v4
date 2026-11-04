using Vixly.Contracts.Operations.Internal;
using Vixly.Contracts.Operations.Reviews;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.UseCases.Site.DeleteMyOperationsReview;
using Vixly.Operations.Application.UseCases.Site.DismissMyOperationsReviewInvite;
using Vixly.Operations.Application.UseCases.Site.GetMyOperationsReview;
using Vixly.Operations.Application.UseCases.Site.SaveMyOperationsReview;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalReviewEndpoints.cs
//
// PURPOSE:
// The customer portal's review calls (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Read, write, change and delete the customer's own review
// - Close the portal's invitation to review
//
// IMPORTANT NOTES:
// - Customer scope only; a customer only ever reaches their own review (S-3)
// - Why the company hid a review is never sent to the customer
// =========================================================
public static class OperationsInternalReviewEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder review = endpoints.MapGroup("/internal/v1/customers/me/review")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        review.MapGet("", async (HttpContext http, GetMyOperationsReviewUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsReviewResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new GetMyOperationsReviewRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            return response.Succeeded ? Results.Ok(Dto(response)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        review.MapPut("", async (HttpContext http, CustomerReviewBody body, SaveMyOperationsReviewUseCase useCase, CancellationToken cancellationToken) =>
        {
            SaveMyOperationsReviewResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http), new SaveMyOperationsReviewRequest
            {
                SessionToken = OperationsInternalPackageEndpoints.Session(http),
                Rating = body.Rating,
                Quote = body.Quote,
                Place = body.Place,
                NameStyle = Enum.TryParse(body.NameStyle, out OperationsReviewNameStyle style) && Enum.IsDefined(style) ? style : OperationsReviewNameStyle.FirstAndInitial,
                Agreed = body.Agreed
            }, cancellationToken);

            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        review.MapDelete("", async (HttpContext http, DeleteMyOperationsReviewUseCase useCase, CancellationToken cancellationToken) =>
        {
            DeleteMyOperationsReviewResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new DeleteMyOperationsReviewRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        review.MapPost("/dismiss", async (HttpContext http, DismissMyOperationsReviewInviteUseCase useCase, CancellationToken cancellationToken) =>
        {
            DismissMyOperationsReviewInviteResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new DismissMyOperationsReviewInviteRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        return endpoints;
    }

    private static CustomerReviewDto Dto(GetMyOperationsReviewResponse response)
    {
        OperationsSiteTestimonial? review = response.Review;
        string? status = review is null ? null : review.IsNew ? "New" : review.Shown ? "Published" : "Hidden";
        return new CustomerReviewDto(response.Asked, response.Eligible, response.InvitePending, review is not null, review?.Rating, review?.Quote, review?.Place,
            (review?.NameStyle ?? OperationsReviewNameStyle.FirstAndInitial).ToString(), review?.CustomerAgreedUtc is not null, status, review?.Name, review?.SubmittedUtc);
    }
}
