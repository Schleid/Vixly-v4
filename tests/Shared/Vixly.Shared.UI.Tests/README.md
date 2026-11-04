# Vixly.Shared.UI.Tests

Owner: Frontend/UI. Static HTML rendering checks for shared presentational primitives.

May reference Shared.UI, ASP.NET Core rendering and repository test dependencies. Must not reference plane projects, application use cases or infrastructure.

Run: dotnet test tests/Shared/Vixly.Shared.UI.Tests/Vixly.Shared.UI.Tests.csproj

Covers current-step accessibility semantics, text-plus-colour status categories, consumer-text escaping and loading-table busy state. No database, HTTP services or real tenant data.
