using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsCustomerAccounts.cs
//
// PURPOSE:
// What every customer use case shares: sessions, links, emails and mailbox addresses (Slice 2).
//
// RESPONSIBILITIES:
// - Start and check sessions; end them all
// - Issue a link and email it in the brand of the tenant
// - Give a profile with its mailbox addresses
//
// IMPORTANT NOTES:
// - A session is checked on every call; an expired one is ended (D-C4)
// =========================================================
public sealed class OperationsCustomerAccounts
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsCustomerMailer _mailer;

    private readonly IOperationsPortalLinks _links;

    private readonly IOperationsRequestMetadata _request;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsCustomerAccounts(
        IOperationsCustomerRepository customers,
        IOperationsLocationRepository locations,
        IOperationsBrandRepository brands,
        IOperationsSecretGenerator secrets,
        IOperationsCustomerMailer mailer,
        IOperationsPortalLinks links,
        IOperationsRequestMetadata request,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    // Starts a session; the caller saves. Returns the token for the cookie.
    public string StartSession(OperationsCustomer customer, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(customer);
        OperationsSecret secret = _secrets.NewToken();
        _customers.Add(OperationsCustomerSession.Start(customer.TenantId, customer.Id, secret.Hash, _request.ClientIp, _request.UserAgent, utcNow));
        customer.RecordSignIn(utcNow);
        return secret.Value;
    }

    // The signed-in customer, or null: no token, unknown, ended or expired session, or a customer who may no longer sign in.
    public async Task<(OperationsCustomer Customer, OperationsCustomerSession Session)?> SignedInAsync(string? sessionToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken) || sessionToken.Length > 200)
        {
            return null;
        }

        DateTimeOffset utcNow = UtcNow;
        OperationsCustomerSession? session = await _customers.FindSessionAsync(_secrets.HashToken(sessionToken), cancellationToken);

        if (session is null || session.IsEnded)
        {
            return null;
        }

        OperationsCustomer? customer = await _customers.GetAsync(session.CustomerId, cancellationToken);

        if (session.ExpiryReasonAt(utcNow) is { } reason || customer is null || customer.Status != OperationsCustomerStatus.Active)
        {
            session.End(session.ExpiryReasonAt(utcNow) ?? OperationsSessionEndReason.AccountRestricted, utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (session.Touch(utcNow))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return (customer, session);
    }

    // Ends every open session of the customer, except one; the caller saves.
    public async Task EndSessionsAsync(Guid customerId, OperationsSessionEndReason reason, Guid? keep, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = UtcNow;

        foreach (OperationsCustomerSession session in await _customers.ListOpenSessionsAsync(customerId, cancellationToken))
        {
            if (session.Id != keep)
            {
                session.End(reason, utcNow);
            }
        }
    }

    // After a new password, no older reset link may set another one (review L3); the caller saves.
    public async Task CancelPasswordLinksAsync(Guid customerId, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = UtcNow;

        foreach (OperationsCustomerToken open in await _customers.ListOpenTokensAsync(customerId, OperationsCustomerTokenPurpose.PasswordReset, cancellationToken))
        {
            // The link just used is already spent in this unit of work.
            if (open.UsedUtc is null)
            {
                open.Consume(OperationsCustomerTokenPurpose.PasswordReset, utcNow);
            }
        }
    }

    // Issues a single-use link and emails it after the save. Older open links of the same kind stop working.
    public async Task<bool> SendLinkAsync(OperationsCustomer customer, OperationsCustomerTokenPurpose purpose, string portalSlug, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        DateTimeOffset utcNow = UtcNow;

        foreach (OperationsCustomerToken open in await _customers.ListOpenTokensAsync(customer.Id, purpose, cancellationToken))
        {
            if (open.UsedUtc is null)
            {
                open.Consume(purpose, utcNow);
            }
        }

        OperationsSecret secret = _secrets.NewToken();
        _customers.Add(OperationsCustomerToken.Issue(customer.TenantId, customer.Id, purpose, secret.Hash, utcNow));

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return false;
        }

        string page = purpose == OperationsCustomerTokenPurpose.Verification ? "/customer/verify?token=" : "/customer/reset-password?token=";
        OperationsCustomerMailKind kind = purpose == OperationsCustomerTokenPurpose.Verification ? OperationsCustomerMailKind.Verification : OperationsCustomerMailKind.PasswordReset;

        await SendAsync(customer, kind, _links.Page(portalSlug, page + Uri.EscapeDataString(secret.Value)), cancellationToken);
        return true;
    }

    public Task SendAsync(OperationsCustomer customer, OperationsCustomerMailKind kind, string link, CancellationToken cancellationToken) =>
        SendAsync(customer, kind, link, new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), cancellationToken);

    public async Task SendAsync(OperationsCustomer customer, OperationsCustomerMailKind kind, string link, IReadOnlyDictionary<string, string> extra, IReadOnlyDictionary<string, string> worded,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(extra);
        ArgumentNullException.ThrowIfNull(worded);
        string company = (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty;
        await _mailer.SendAsync(new OperationsCustomerMail(kind, customer.Email.Value, customer.FullName, customer.PreferredLanguage, company, link) { Extra = extra, Worded = worded },
            cancellationToken);
    }

    // Review item 3: no account exists for a work email, so the mail goes to the address as typed, worded in the language chosen.
    public async Task SendNotAllowedAsync(EmailAddress email, string? name, LanguageCode language, string portalSlug, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(language);
        string company = (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty;
        string toName = string.IsNullOrWhiteSpace(name) ? email.Value : name.Trim();
        await _mailer.SendAsync(new OperationsCustomerMail(OperationsCustomerMailKind.EmailNotAllowed, email.Value, toName, language, company, _links.Page(portalSlug, "/customer/sign-up")),
            cancellationToken);
    }

    public string SignInLink(string portalSlug) => _links.Page(portalSlug, "/customer/sign-in");

    // A page of the customer portal on the tenant's host (Slice 3B: "your package arrived").
    public string PortalPage(string portalSlug, string path) => _links.Page(portalSlug, path);

    public async Task<OperationsCustomerProfile> ProfileAsync(OperationsCustomer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return OperationsCustomerProfile.Of(customer, OperationsMailboxAddresses.For(customer, await _locations.ListWarehousesAsync(cancellationToken)));
    }

    public string HashToken(string token) => _secrets.HashToken(token);
}
