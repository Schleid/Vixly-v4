using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Infrastructure.Security;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsDemoStaffSeeder.cs
//
// PURPOSE:
// Gives each demo tenant staff who can sign in at once (Development only).
//
// RESPONSIBILITIES:
// - The Owner, while still invited: the demo password and the demo authenticator key
// - An Administrator (admin@{slug}.example) with the same password and key
// - A front-desk clerk (staff@{slug}.example) with the password only
// - A customer (customer@{slug}.example) for the customer portal, active with a code (Slice 2)
//
// IMPORTANT NOTES:
// - Never registered outside Development; never changes an Owner who already chose a password
// - The key and password are in appsettings.Development.json and in docs/operations/DEVELOPMENT-DEMO-USERS.md
// =========================================================
public sealed class OperationsDemoStaffSeeder
{
    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsPlatformClient _platform;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly IDataProtector _protector;

    private readonly OperationsDemoDataOptions _options;

    private readonly TimeProvider _timeProvider;

    public OperationsDemoStaffSeeder(
        IOperationsUserRepository users,
        IOperationsRoleRepository roles,
        IOperationsPlatformClient platform,
        IOperationsCustomerRepository customers,
        OperationsNumbering numbering,
        IOperationsPasswordHasher hasher,
        IOperationsUnitOfWork unitOfWork,
        IDataProtectionProvider dataProtection,
        IOptions<OperationsDemoDataOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProtection);
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _protector = dataProtection.CreateProtector(TotpOperationsAuthenticatorCodes.ProtectorPurpose);
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // Returns how many people were made ready to sign in.
    public async Task<int> SeedAsync(Guid tenantId, string slug, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string hash = _hasher.Hash(_options.Password);
        int ready = 0;

        // The Owner the invitation created: ready only while the invitation is still unused.
        OperationsTenantAccount? account = await _platform.GetTenantAsync(tenantId, cancellationToken);

        if (account is not null && EmailAddress.TryCreate(account.OwnerEmail, out EmailAddress? ownerEmail)
            && await _users.FindByEmailAsync(ownerEmail!, cancellationToken) is { } owner && owner.Account.Status == OperationsUserAccountStatus.Invited)
        {
            owner.AcceptInvitation(hash, utcNow);
            AddAuthenticator(owner, utcNow);
            ready++;
        }

        OperationsRole administrator = await _roles.FindAdministratorAsync(cancellationToken) ?? Add(OperationsRole.CreateAdministrator(tenantId, utcNow));
        OperationsRole clerk = (await _roles.ListAsync(cancellationToken)).FirstOrDefault(role => role.Name == "Front desk")
            ?? Add(OperationsRole.Create(tenantId, "Front desk", new[]
            {
                OperationsPermissions.StaffView, OperationsPermissions.BranchesView, OperationsPermissions.WarehousesView, OperationsPermissions.ServicesView, OperationsPermissions.BrandView,
                // Slice 5C: the counter (find customers, create their shipments, enter their parcels).
                OperationsPermissions.CounterServe
            }, utcNow));

        ready += await StaffAsync(tenantId, "admin@" + slug + ".example", "Demo Administrator", administrator, hash, utcNow, cancellationToken);
        ready += await StaffAsync(tenantId, "staff@" + slug + ".example", "Demo Front Desk", clerk, hash, utcNow, cancellationToken);
        ready += await CustomerAsync(tenantId, "customer@" + slug + ".example", hash, utcNow, cancellationToken);

        return await _unitOfWork.SaveChangesAsync(cancellationToken) ? ready : 0;
    }

    private async Task<int> StaffAsync(Guid tenantId, string address, string name, OperationsRole role, string hash, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        EmailAddress email = EmailAddress.Create(address);

        if (await _users.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return 0;
        }

        OperationsUser user = OperationsUser.InviteStaff(tenantId, name, email, LanguageCode.En, utcNow);
        user.SetAdministrator(role.IsAdministrator, utcNow);
        user.AcceptInvitation(hash, utcNow);

        if (role.IsAdministrator)
        {
            AddAuthenticator(user, utcNow);
        }

        _users.Add(user);
        _roles.Add(OperationsRoleGrant.Grant(tenantId, user.Id, role.Id, OperationsRoleScope.Tenant, null, null, utcNow));
        return 1;
    }

    // An active customer with a code and the demo password, as if they had confirmed their email.
    private async Task<int> CustomerAsync(Guid tenantId, string address, string hash, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        EmailAddress email = EmailAddress.Create(address);

        if (await _customers.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return 0;
        }

        OperationsCustomer customer = OperationsCustomer.SignUp(tenantId, "Demo", "Customer", null, email, "+15555550100", LanguageCode.En, hash, utcNow);
        customer.ConfirmEmail(await _numbering.NextCustomerCodeAsync(cancellationToken), utcNow);
        _customers.Add(customer);
        return 1;
    }

    // The known key, confirmed a minute ago, so the next code from the app is accepted.
    private void AddAuthenticator(OperationsUser user, DateTimeOffset utcNow)
    {
        user.BeginAuthenticatorEnrolment(_protector.Protect(_options.AuthenticatorKey), utcNow);
        user.ConfirmAuthenticatorApp(utcNow.ToUnixTimeSeconds() / 30 - 2, utcNow);
    }

    private OperationsRole Add(OperationsRole role)
    {
        _roles.Add(role);
        return role;
    }
}
