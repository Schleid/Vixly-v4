# Vixly.Public.Web.Tests

Owner: Control team. Tests the Public HTTP surface and its help content.

May reference Public.Web, its transitively exposed contracts/shared types, and test/ASP.NET test-host libraries.
Must not use production data, send real email or complete real payments. PublicSiteTestHost replaces only the Platform transport and key provider.

Public help checks cover all 48 embedded articles, exact localized resource labels,
source paths, actual endpoint routes, anonymous reachability, contextual links,
404 rendering, search/caching, sitemap alternates and the named help read limit.
