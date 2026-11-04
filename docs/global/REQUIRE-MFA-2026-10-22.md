# Require MFA: Yes/No in Control and Operations (2026-10-22)

Owner's request (2026-10-22, 16:30): "can I have MFA deactivated on Staging. Make it a settings that adm on both control and operations can required user to do. Require MFA: Yes/No".

## Owner decisions (2026-10-22)

| Question | Decision |
|---|---|
| With No, people who already set up an authenticator | Keep using it |
| Sensitive actions (step-up) with No, for people without an authenticator | Password again |
| Who may change it | Top administrators, after confirming it is them, recorded in the history |
| Operations device licensing | Stays on; only the authenticator step is affected |

## Behaviour

**Default Yes everywhere.** Existing rows and new companies keep today's rules.

### Control

- **Where:** Users page › *Sign-in security*.
- **Who may change it:** a SuperUser only, after step-up. The right is `platform.users.change_authority`, which no role carries.
- **Storage:** `platform.SecuritySettings`, one row; no row means Yes.
- **History:** `platform.security.require_mfa_changed`, before and after.
- **With No, a person without a passkey or authenticator:**
  - signs in with the password alone (`platform.auth.signed_in`, method `password`);
  - after accepting an invitation, is signed in without setting one up;
  - is asked for the password in the step-up dialog (`platform.auth.step_up_confirmed`, method `password`).
- **People with a passkey or authenticator** keep the second step, and cannot confirm with the password.

### Operations

- **Where:** Company settings › *Security* › *Require MFA* (Yes/No).
- **Storage:** `ops.TenantSettings.RequireMfa`; existing companies get Yes.
- **Yes:** as before. The Owner and Administrators always need an authenticator; other staff only when *every staff member* is ticked.
- **No:** nobody is made to set one up, and the *every staff member* box has no effect.
- **Who may change it:** the Owner or an Administrator, always after confirming. Anyone else with the settings right is refused. Recorded in `ops.settings.changed` (`requireMfa` before and after).
- **People without an authenticator, with No:**
  - confirm sensitive actions with their password: the confirm page, unblocking, approving and releasing a device, and a manager approving a supplies discount;
  - sessions opened with the password count as freshly confirmed for the usual 5 minutes.
- **Device licensing and the other-country block (R-6) are unchanged.**

## Turning it off on staging

**In the applications (recorded in the history):**

1. **Control:** sign in as a SuperUser, open *Users*, set *Sign-in security* to **No**, and confirm.
2. **Operations:** sign in as the Owner of each staging company, open *Company settings*, set *Require MFA* to **No**, save, and confirm.

**Or, by SQL from the owner's PC.** This leaves no history row, so use it only on staging, never in production:

```sql
USE [VixlyV4];
IF EXISTS (SELECT 1 FROM [platform].[SecuritySettings] WHERE [Key] = 'default')
    UPDATE [platform].[SecuritySettings] SET [RequireMfa] = 0, [LastModifiedUtc] = SYSDATETIMEOFFSET() WHERE [Key] = 'default';
ELSE
    INSERT [platform].[SecuritySettings] ([Id], [Key], [RequireMfa], [CreatedUtc]) VALUES (NEWID(), 'default', 0, SYSDATETIMEOFFSET());
UPDATE [ops].[TenantSettings] SET [RequireMfa] = 0;
```

## Database

- Platform migration `SecuritySettings` and Operations migration `RequireMfa` (column default 1).
- Both are in the global SQL script, which was checked on an empty database and run twice.
- The column has no default in the EF model on purpose. A store default of `true` would make EF skip writing `false` on a new row.

## Tests

- **Control (`PlatformRequireMfaTests`, 7):**
  - by default, a person without a factor must set one up;
  - with No, password sign-in, but a person with a factor keeps it;
  - a wrong password is still refused;
  - step-up by password only with No and no factor;
  - a SuperUser only, after step-up, recorded.
- **Operations (`OperationsRequireMfaTests`, 8):**
  - the same rules for sign-in, step-up and access;
  - the staff-wide box has no effect with No;
  - an editor with the settings right is refused;
  - the Owner needs a fresh confirmation;
  - leaving the field out keeps the value.
- **Operations HTTP:** `RequireMfaNo_IsSavedByTheOwner_AndStaffWithoutAnAppConfirmWithThePassword`.
