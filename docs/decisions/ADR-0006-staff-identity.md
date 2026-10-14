# ADR-0006: Platform staff identity

- Status: **Accepted** (owner decision, 2026-10-13)
- Date: 2026-10-13
- Deciders: Jude (owner)
- Affects: Platform Slice 1; Global Standard S-4, S-6; Platform plane document

## Context

The Platform console controls every tenant, plan, invoice and payment setting. In V3 it is protected by email and password alone. The Slice 1 sweep found three more gaps: there is no way to offboard staff, an administrator can know a user's password, and department-scoped roles look enforced but are not. The owner decided on 2026-10-13 that admin-set passwords are dropped, staff accounts get lock, suspend and deactivate actions, and departments are labels only.

## Decision

1. **Named accounts only.** Every person has their own account. No shared logins. The one exception is a single **break-glass account**: its passkey is a hardware key kept offline. Every use raises an alert and an audit row, and the account cannot be used for day-to-day work.
2. **Two factors for every staff user, enforced in the app.** Cloudflare Access in front of the console is a second, independent gate; it does not replace this.
   - Preferred: a **passkey** (WebAuthn). It is phishing-resistant and can serve as both factors on its own ("Sign in with a passkey").
   - Fallback: password plus an **authenticator-app code** (TOTP, RFC 6238).
   - Recovery: ten single-use **recovery codes**, shown once and stored hashed.
   - No SMS or email codes.
3. **Enrolment before access.** A new user accepts an invitation link, sets a password and enrols a passkey or authenticator before any console page loads.
4. **Nobody but the user knows a password.** Administrators send invitation or reset links only (D-S1). Passwords: at least 12 characters, checked against a list of breached passwords. Hashing stays BCrypt (decided), at cost 12. A hash with a lower cost is upgraded at the next successful login, so migrated V3 users keep their passwords.
5. **Step-up confirmation.** Sensitive actions require a fresh second factor within the last 5 minutes: changing roles or permissions, account lifecycle actions, resetting another user's second factor, and (in later slices) payment and merchant settings.
6. **Sessions are server-side records.** The cookie carries a session id that is checked against `platform.UserSessions`.
   - A user sees their own active sessions and can end any of them.
   - A security administrator can end all sessions of any user.
   - Lock, suspend and deactivate end all of the user's sessions at once.
   - V3's global switch (`SessionSecurity:CookieVersion`) is kept for emergencies.
   - Idle timeout is 5 minutes, with a warning (V3 behaviour); an absolute limit of 12 hours applies.
7. **Account lifecycle** (D-S2): Invited, Active, Suspended, Locked, Deactivated. Every change needs a reason, step-up and the security-administrator permission. Every change writes an audit row. The domain refuses any change that would leave no active administrator, or that an administrator makes to their own account.
8. **Brute force.** The per-account lockout stays (5 failures, 15 minutes). Per-IP limits use the real client IP: `CF-Connecting-IP` is trusted only when the request comes from Cloudflare's published address ranges. Second-factor attempts are limited per pending sign-in.
9. **Libraries.** Fido2NetLib for WebAuthn, Otp.NET for TOTP and BCrypt.Net-Next for hashing; versions are pinned in `Directory.Packages.props`. TOTP secrets are encrypted with the Platform Data Protection key ring. ASP.NET Core Identity is not used: its user model would compete with `PlatformUser`, and the slice needs only these primitives.

## Consequences

- Slice 1 grows: passkeys, TOTP, recovery codes, sessions and step-up are built and tested in this slice.
- Operators need a passkey-capable device or an authenticator app. The break-glass hardware key must be bought and stored before go-live.
- Migrated V3 staff sign in with their existing password once, then must enrol a second factor.
- Department-scoped roles are not built (D-S3). Roles are platform-wide.
