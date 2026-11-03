# Vixly.Shared.Help

Owner: Architecture. Shared in-app help foundation (ADR-0023, help stage H0).

May reference: BCL and Markdig only.
Must not reference: ASP.NET Core, Shared.UI, Contracts or any plane.
Applications own embedded articles, route and permission catalogues, reader context and help addresses.
The catalogue is immutable and built once at startup. Filter direct article requests and related links with the reader.

Build `HelpOptions` from the application's section, permission, condition and endpoint catalogues.
Allow no tokens in Public/Control, only `brandName` in Customer, and `brandName` plus
`portalAddress` in Operations. Public callers use a reader with empty rules; signed-out
Customer readers explicitly set `signedIn: false`. Control alone may set `superUser`.

Read embedded documents with `HelpArticleSource.Read`, then construct one `HelpCatalogue`.
`Find`, `Visible`, `Related`, `ForRoute` and `HelpSearch.Find` apply the supplied reader.
`ForRoute` accepts the endpoint route template; the host resolves the current endpoint
and supplies the home address when no visible article matches. Never cache reader rules
or rendered company token values on the singleton catalogue.

`HelpCheck.Run` returns publication problems. The catalogue refuses invalid documents.
The renderer escapes raw HTML for direct use; publication checks reject it entirely.
Token values are HTML-encoded after rendering and only replaced in text, never in
attributes or code. Article links must use relative or HTTPS addresses; tokens are text,
so write the portal address as `{portalAddress}`, rather than as a link destination.

The application Web tests must run the checks against the actual endpoint data source
and permission catalogue. Architecture checks add source-level coverage of all four folders.
