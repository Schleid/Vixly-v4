# ADR-0020: One shared security library for Platform and Operations

- Status: **Rejected** (owner, 2026-10-17): "Platform and Operations are two separate apps; the only shared data is through API communication." Each plane keeps its own security code.
- Date: 2026-10-17
- Deciders: Jude (owner)
- Affects: new `Vixly.Shared.Security`; `Platform.Infrastructure`, `Operations.Infrastructure`, `Customer` sign-in (Operations-owned); Global Standard section 3 (dependency table)

## Context

Platform Slice 1 built staff sign-in from scratch: BCrypt hashing at cost 12 with upgrade on sign-in (M-4), a password policy with a breached-password check, TOTP codes with no reuse, passkey (WebAuthn) ceremonies, secure random tokens and hashed single-use links. Operations Slice 1 needs the same pieces for tenant staff, and Slice 2 needs most of them for customers. The standard lets a plane reference only its own projects, `Shared.Kernel` (BCL only) and `Contracts`, so today Operations would have to copy the code. Two copies of security code drift: a fix in one is missed in the other.

## Decision

1. Add `src/Shared/Vixly.Shared.Security`, a class library holding only plane-agnostic security mechanics:
   - password hashing (BCrypt, cost 12, "needs upgrade" check);
   - the password policy (length, breached-password list) returning `Error` codes;
   - TOTP verification with a time-step window and last-step tracking supplied by the caller;
   - passkey ceremony helpers over Fido2NetLib (options, verification), with the relying party (host) supplied by the caller;
   - secure random tokens, token hashing and constant-time comparison.
2. It holds **no** users, sessions, tables, texts or plane vocabulary. Each plane keeps its own entities, repositories, sessions and errors, and wraps the library in its own ports (`IPlatformPasswordHasher`, `IOperationsPasswordHasher` and so on).
3. Only `*.Infrastructure` projects may reference it. It may reference the BCL, `Shared.Kernel`, BCrypt.Net-Next, Fido2NetLib and Otp.NET, pinned in `Directory.Packages.props`.
4. Platform's existing adapters (`BCryptPlatformPasswordHasher`, `TotpPlatformAuthenticatorCodes`, `Fido2PlatformPasskeys`, `PlatformSecretGenerator`, `PlatformRandom`) become thin wrappers; their tests move into `Vixly.Shared.Security.Tests` and Platform keeps its behaviour tests.

## Outcome

Rejected. Operations builds its own password hashing, password policy, TOTP, passkey ceremonies, random tokens and service-token signing and checking in `Operations.Infrastructure`, following the same rules as Platform (ADR-0006, ADR-0014) and tested on its own. A security fix found in one plane is checked in the other by review.

## Consequences (had it been accepted)

- One place to fix and test security mechanics; Operations Slice 1 builds on tested code.
- Global Standard section 3 gains the row "`Shared.Security`: BCL, `Shared.Kernel` and the three pinned security packages; referenced by `*.Infrastructure` only"; `ProjectDependencyRulesTests` enforces it.
- Moving Platform's adapters is a refactor with no behaviour change; Platform's full test suite must stay green.
