using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PlanChoices.cs
//
// PURPOSE:
// Loads the active plans for sales forms.
//
// RESPONSIBILITIES:
// - List up to 100 active plans by name
//
// IMPORTANT NOTES:
// - A helper used by presenters; not a presenter itself
// =========================================================
public static class PlanChoices
{
    public static async Task<(IReadOnlyList<PlanChoice> Plans, Error? Error)> LoadAsync(
        ListPlatformPlansUseCase list,
        PlatformAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);

        ListPlatformPlansResponse response = await list.ExecuteAsync(
            context,
            new ListPlatformPlansRequest { Status = PlatformPlanStatus.Active, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Page = 1, PageSize = 100 },
            cancellationToken);

        return response.Plans is { } plans
            ? (plans.Items.Where(plan => plan.CurrentVersionNumber is not null).Select(plan => new PlanChoice(plan.PlatformPlanId, plan.Code, plan.Name, plan.CurrencyCode)).ToArray(), null)
            : (Array.Empty<PlanChoice>(), response.Error);
    }
}
