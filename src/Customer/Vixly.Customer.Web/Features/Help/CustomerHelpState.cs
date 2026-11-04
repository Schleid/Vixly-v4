using Vixly.Contracts.Operations.Customers;
using Vixly.Shared.Help;

namespace Vixly.Customer.Web.Features.Help;

// =========================================================
// FILE: CustomerHelpState.cs
// PURPOSE:
// Carries verified, company-branded help to the portal components.
// RESPONSIBILITIES:
// - Keep article and search results immutable for one request.
// IMPORTANT NOTES:
// - Built only after the help endpoint checks the customer session.
// =========================================================
public sealed record CustomerHelpState(CustomerPortalBrandDto Brand, bool SignedIn, HelpArticle? Article,
    string Html, IReadOnlyList<HelpArticle> Articles, IReadOnlyList<HelpArticle> Related, string Query, bool NotFound);
