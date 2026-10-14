# Slice 0: Foundation - design note and verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Built, reworked for ADR-0005, v1.3 addendum added (section 7). Verified with real NuGet and xUnit on 2026-10-13. Awaiting owner sign-off.** |
| Date | 2026-10-06, addendum 2026-10-13 |
| Plane document | [PLATFORM-PLANE.md](PLATFORM-PLANE.md) |

## 1. What was built

| Area | Contents |
|---|---|
| Root build | `global.json` (SDK 10.0.100, roll forward within the feature band), `Directory.Build.props` (net10.0, nullable, warnings are errors, test packages for every `*.Tests` project), `Directory.Packages.props` (central package versions), `Vixly.Platform.slnx` |
| `Vixly.Shared.Kernel` | `Error`, `KernelErrors`, `LanguageCode`, `DomainException(Error)`, `AuditableEntity`, `EmailAddress`, `PhoneNumber`, `PagedRequest`, `PagedResult<T>` (ADR-0004, ADR-0005) |
| `Vixly.Contracts.Platform` | Empty project. Contracts are added by the slice that needs them |
| `Vixly.Platform.Domain` | `PlatformUserAuthorityLevel` (persisted enum, values pinned by test) |
| `Vixly.Platform.Application` | `PlatformAuthorizationContext`, `IPlatformAuthorizationService`, `IPlatformAuthorizationGate`, `PlatformAuthorizationGate`, `PlatformSecurityErrors` |
| `Vixly.Architecture.Tests` | Dependency rules (Standard 3), header and formatting rules (5.1, 5.4), one type per file, entity prefix (4.1), Domain purity, use-case shape (5.2: Request/Response beside UseCase, init-only Request, private-init Response, `Failure(Error)`, context first and token last, null-checked constructor), and localization (5.5: no resource assemblies in Domain or Application, `DomainException` takes an `Error`, every declared error code well formed, unique and correctly prefixed) |
| Unit tests | Kernel (error, language, email, phone, paging, entity), Domain (enum values), Application (gate and context) |

## 2. Decisions taken in this slice

- **Rework for ADR-0005 (multilingual).** The first build used English sentences in `DomainException` and the gate. They were replaced by `Error` codes before sign-off. Domain and Application now produce no user-facing text; the gate returns the `platform.security.access_denied` code without naming the missing permission.

- The authorization context carries only the user id and authority level. Permissions are resolved from persistence on each check, so a forged or stale context cannot widen access (S-1, S-2). Legacy did the same.
- The gate denies by default: an empty or blank permission set denies, and the denial message does not name the permission (S-9).
- `PlatformAuthorizationGate` is in Application. The `IPlatformAuthorizationService` implementation arrives with the Identity slice.
- Architecture rules read `.csproj` files rather than compiled assemblies for dependency checks, so they cover planes that are not built yet.
- Only Application assemblies are inspected for use-case shape, and the rules are vacuous until the first use case exists. A scan-reach test proves the assemblies are actually loaded.

## 3. Evidence

Run in the build sandbox (.NET SDK 10.0.112):

- All four source projects build with 0 warnings, 0 errors, with warnings treated as errors (re-run after the ADR-0005 rework).
- 199 checks pass (each `Theory` row counts as one check): Kernel, Domain and Application unit tests plus the architecture rules, which run once per project and once per file.
- Rules were shown to fail on purpose: a Domain reference to Application, a missing header section, LF line endings, a string constructor added to `DomainException`, and a reference from Application to a Resources project each produced a failure of the matching rule, then were reverted.

## 4. What was NOT verified here, and why

*Items 1 and 2 were closed on 2026-10-13 (section 7.3). Items 3 and 4 still stand.*

The sandbox cannot restore NuGet packages, so:

1. The tests were executed through a small stand-in runner, not the real xUnit runner. They use only basic xUnit APIs (`Fact`, `Theory`, `InlineData`, `MemberData`, `TheoryData`, `Assert.*`), but real xUnit has not run them.
2. The pinned package versions in `Directory.Packages.props` (`Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4) have not been resolved against NuGet.
3. The entity-prefix rule has no entity to inspect yet.
4. Resource completeness (en, fr, es keys, placeholders, plural categories) and the no-literal-text-in-markup rule cannot run yet: no Resources project or Razor project exists. They are added with Slice 1, which creates `Vixly.Platform.Resources` and `Vixly.Shared.Localization`.

## 5. Owner verification steps (on a machine with NuGet access)

```
dotnet restore Vixly.Platform.slnx
dotnet build   Vixly.Platform.slnx -c Release
dotnet test    Vixly.Platform.slnx -c Release
```

Expected: build with no warnings, all tests green. If a package version cannot be resolved, change it in `Directory.Packages.props` only. Report any failure and it is fixed before Slice 1 starts.

## 6. Sign-off

Sign after running section 5 on your machine; the expected result is in 7.3.

| Check | Result |
|---|---|
| Builds warning-free | |
| Architecture tests pass | |
| Unit tests pass | |
| Owner | |
| Date | |

## 7. Addendum for Global Standard v1.3 (ADR-0010), 2026-10-13

### 7.1 What was added

| Area | Contents |
|---|---|
| `Vixly.Platform.Infrastructure` | `PlatformDatabase` (schema `platform`, history table `__EFMigrationsHistory`, connection string name `Platform`); `PlatformDbContext` (default schema `platform`, configurations from the assembly, D-5 convention applied last); `ApplicationAssignedKeyConvention` (every `AuditableEntity` Id is `ValueGeneratedNever`); `AddPlatformInfrastructure(configuration)` (reads `ConnectionStrings:Platform`, fails fast if missing, SQL Server with the history table in `platform` and retry on transient faults) |
| `Vixly.Platform.Infrastructure.Tests` | Default schema is `platform`; the history table script targets `[platform].[__EFMigrationsHistory]`; registration fails without the connection string; every `AuditableEntity` in the model has a never-generated key; on a fixture parent/child model, a child added to a tracked parent is `Added` (INSERT) |
| Packages | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Extensions.Configuration(.Abstractions)`, `Microsoft.Extensions.DependencyInjection` 10.0.13, central in `Directory.Packages.props` |
| Solution | Infrastructure and its tests added to `Vixly.Platform.slnx` |

No migration exists yet: the first one arrives with the first entity in Slice 1, and is applied by the pipeline, never at startup (D-1).

### 7.2 Moved to Slice 1

- **`UseVixlySecurityHeaders()`** (S-10, implemented once). It needs a home that every host can reference, and the standard has none yet for hosting code. Slice 1 creates the first host (`Platform.Web`) and proposes that home by ADR at the same time.
- **The Data Protection key table** in the `platform` schema (S-5) also arrives with the first host.

### 7.3 Evidence (real NuGet restore and xUnit, .NET SDK 10.0.402)

- `dotnet build Vixly.Platform.slnx -c Release`: 0 warnings, 0 errors (warnings are errors).
- `dotnet test`: 238 checks pass. Kernel 81, Domain 1, Application 10, Architecture 139, Infrastructure 7.
- The original Slice 0 (before the addendum) also passed with real xUnit: 199 checks, matching section 3.
- Rules shown to fail on purpose, then reverted:
  - A use case whose `ExecuteAsync` has no authorization context fails three use-case rules.
  - The convention test model without the D-5 convention reports the new child as Modified. That is the V3 collections defect reproduced: it would be sent as an UPDATE.
