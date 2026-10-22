using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Leads.SubmitPlatformSalesLead;

// =========================================================
// FILE: SubmitPlatformSalesLeadUseCase.cs
//
// PURPOSE:
// A visitor of the public site asks Vixly sales to get in touch (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous (public site, S-6): bot check first; rate-limited by the API per visitor IP
// - The visitor gets no email, so the form cannot be used to send mail to others
// =========================================================
public sealed class SubmitPlatformSalesLeadUseCase
{
    private readonly PlatformLeadAdministration _leads;

    public SubmitPlatformSalesLeadUseCase(
        PlatformLeadAdministration leads)
    {
        _leads = leads ?? throw new ArgumentNullException(nameof(leads));
    }

    public async Task<SubmitPlatformSalesLeadResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        SubmitPlatformSalesLeadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The bot check stands in for authorization on this anonymous form.
        if (!await _leads.BotCheck.VerifyAsync(request.BotToken, _leads.ClientIp, cancellationToken))
        {
            return SubmitPlatformSalesLeadResponse.Failure(PlatformSalesLeadErrors.BotCheckFailed);
        }

        PlatformSalesLead lead;

        try
        {
            lead = PlatformSalesLead.Create(request.CompanyName, request.ContactName, request.Email, request.Phone, request.CountryCode, request.Kind, request.Message,
                LanguageCode.TryCreate(request.Language, out LanguageCode? language) ? language : LanguageCode.Default, request.SourcePage, _leads.ClientIp, _leads.UtcNow);
        }
        catch (DomainException exception)
        {
            return SubmitPlatformSalesLeadResponse.Failure(exception.Error);
        }

        _leads.Store.Add(lead);
        _leads.Audit.Record(null, "platform.leads.received", null, nameof(PlatformSalesLead), lead.Id, after: new { company = lead.CompanyName, kind = lead.Kind.ToString(), language = lead.Language.Value });
        _leads.Outbox.Write(PlatformMessageTypes.SalesLeadReceived, lead.Id, new PlatformSalesLeadReceived(lead.Id, _leads.UtcNow));

        return await _leads.SaveAsync(cancellationToken)
            ? SubmitPlatformSalesLeadResponse.Success()
            : SubmitPlatformSalesLeadResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
