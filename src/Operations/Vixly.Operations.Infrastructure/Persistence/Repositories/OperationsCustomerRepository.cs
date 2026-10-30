using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsCustomerRepository.cs
//
// PURPOSE:
// Reads and adds the tenant's customers, sessions and links (Slice 2).
//
// RESPONSIBILITIES:
// - Search by name, email, phone or code, paged
//
// IMPORTANT NOTES:
// - The unit of work saves
// =========================================================
internal sealed class OperationsCustomerRepository : IOperationsCustomerRepository
{
    // A page far past the end is still a valid number for SQL Server (review L5).
    private const int MaxPage = 100_000;

    private const int MaxSearchLength = 100;

    private readonly OperationsDbContext _context;

    private readonly TimeProvider _timeProvider;

    public OperationsCustomerRepository(OperationsDbContext context, TimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Add(OperationsCustomer customer) => _context.Customers.Add(customer ?? throw new ArgumentNullException(nameof(customer)));

    public void Add(OperationsCustomerSession session) => _context.CustomerSessions.Add(session ?? throw new ArgumentNullException(nameof(session)));

    public void Add(OperationsCustomerToken token) => _context.CustomerTokens.Add(token ?? throw new ArgumentNullException(nameof(token)));

    public Task<OperationsCustomer?> GetAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.Customers.FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);

    public Task<OperationsCustomer?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        _context.Customers.FirstOrDefaultAsync(customer => customer.Code == code, cancellationToken);

    public async Task<IReadOnlyList<OperationsCustomer>> ListByIdsAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken) =>
        customerIds is { Count: > 0 }
            ? await _context.Customers.AsNoTracking().Where(customer => customerIds.Contains(customer.Id)).ToListAsync(cancellationToken)
            : Array.Empty<OperationsCustomer>();

    public Task<OperationsCustomer?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        _context.Customers.FirstOrDefaultAsync(customer => customer.Email == email, cancellationToken);

    // Across companies on purpose (one of two reviewed cross-tenant reads, design note item 3); only a yes or a no leaves this method.
    // Only a confirmed account that is not closed holds the email: an unconfirmed sign-up blocks nobody.
    public Task<bool> IsCustomerEmailAnywhereAsync(EmailAddress email, CancellationToken cancellationToken) =>
        _context.Customers.IgnoreQueryFilters().AnyAsync(customer => customer.Email == email && customer.EmailVerifiedUtc != null
            && customer.Status != OperationsCustomerStatus.Closed, cancellationToken);

    public Task<OperationsCustomerSession?> FindSessionAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.CustomerSessions.FirstOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<OperationsCustomerSession>> ListOpenSessionsAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.CustomerSessions.Where(session => session.CustomerId == customerId && session.EndedUtc == null).ToListAsync(cancellationToken);

    public Task<OperationsCustomerToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.CustomerTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<OperationsCustomerToken>> ListOpenTokensAsync(Guid customerId, OperationsCustomerTokenPurpose purpose, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        return await _context.CustomerTokens
            .Where(token => token.CustomerId == customerId && token.Purpose == purpose && token.UsedUtc == null && token.ExpiresUtc > utcNow)
            .ToListAsync(cancellationToken);
    }

    // One atomic statement, so failures sent at the same time all count (review M2).
    public async Task<bool> RecordFailedSignInAsync(Guid customerId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        int limit = OperationsCustomerLimits.MaxFailedSignIns;
        DateTimeOffset lockoutEnds = utcNow.Add(OperationsCustomerLimits.LockoutDuration);

        await _context.Customers
            .Where(customer => customer.Id == customerId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(customer => customer.LockoutEndsUtc, customer => customer.FailedSignInCount + 1 >= limit ? lockoutEnds : customer.LockoutEndsUtc)
                .SetProperty(customer => customer.FailedSignInCount, customer => customer.FailedSignInCount + 1 >= limit ? 0 : customer.FailedSignInCount + 1),
                cancellationToken);

        DateTimeOffset? ends = await _context.Customers.AsNoTracking().Where(customer => customer.Id == customerId).Select(customer => customer.LockoutEndsUtc).FirstOrDefaultAsync(cancellationToken);
        return ends is { } end && end > utcNow;
    }

    public async Task<IReadOnlyList<OperationsCustomer>> FindForCounterAsync(string text, int take, CancellationToken cancellationToken)
    {
        string value = (text ?? string.Empty).Trim();
        value = value.Length > MaxSearchLength ? value[..MaxSearchLength] : value;
        IQueryable<OperationsCustomer> query = _context.Customers.AsNoTracking();

        if (EmailAddress.TryCreate(value, out EmailAddress? email))
        {
            query = query.Where(customer => customer.Email == email);
        }
        else
        {
            string digits = new(value.Where(char.IsAsciiDigit).ToArray());
            string code = value.ToUpperInvariant();

            if (digits.Length < 7 && code.Length == 0)
            {
                return Array.Empty<OperationsCustomer>();
            }

            query = query.Where(customer => customer.Code == code || (digits.Length >= 7 && customer.Phone.EndsWith(digits)));
        }

        return await query.OrderBy(customer => customer.LastName).ThenBy(customer => customer.FirstName).Take(Math.Clamp(take, 1, 20)).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OperationsCustomer> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<OperationsCustomer> query = _context.Customers.AsNoTracking();
        string text = search?.Trim() ?? string.Empty;
        text = text.Length > MaxSearchLength ? text[..MaxSearchLength] : text;

        if (text.Length > 0)
        {
            if (EmailAddress.TryCreate(text, out EmailAddress? email))
            {
                query = query.Where(customer => customer.Email == email);
            }
            else
            {
                string digits = new(text.Where(char.IsAsciiDigit).ToArray());
                query = query.Where(customer => customer.Code == text.ToUpperInvariant()
                    || customer.FirstName.Contains(text) || customer.LastName.Contains(text) || (customer.CompanyName != null && customer.CompanyName.Contains(text))
                    || (digits.Length >= 4 && customer.Phone.Contains(digits)));
            }
        }

        int total = await query.CountAsync(cancellationToken);
        List<OperationsCustomer> items = await query
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .Skip((Math.Clamp(page, 1, MaxPage) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
