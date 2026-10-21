# Demo staff for local testing (Development only)

Two companies also get a sample working day of customers, shipments and parcels: see [DEVELOPMENT-DEMO-DATA.md](DEVELOPMENT-DEMO-DATA.md).

When Operations.Web starts in **Development**, it signs in nothing by itself. It prepares staff who can sign in to every active demo tenant listed in `OperationsDemoData:Slugs` (`appsettings.Development.json`), for example `https://andes-courier.localhost:7404/staff`.

Platform.Api must be running first. Operations.Web asks it which demo tenants are active.

## Who can sign in

| Person | Email | Password | Second step |
|---|---|---|---|
| Owner (if their invitation was never used) | the Owner contact Platform has for the tenant (Platform console, tenant page) | `Harbour demo 2026!` | code from the demo key |
| Administrator | `admin@{slug}.example`, for example `admin@andes-courier.example` | `Harbour demo 2026!` | code from the demo key |
| Front desk (the counter: find customers, create their shipments, enter their parcels; staff, locations, services and website, read only) | `staff@{slug}.example` | `Harbour demo 2026!` | none |

## Customer portal

| Person | Email | Password | Where |
|---|---|---|---|
| Demo Customer (active, with a code such as `C00001`) | `customer@{slug}.example`, for example `customer@andes-courier.example` | `Harbour demo 2026!` | `https://{slug}.localhost:7404/customer/sign-in` |

- The portal needs Operations.Api and Customer.Web running, and Operations.Api needs the Platform caller key in its user secrets (see the [Slice 2 record](SLICE-2-VERIFICATION.md), section 5).
- The mailbox address appears once the tenant has an active receiving warehouse that accepts deliveries.

- **Demo key:** add `JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP` once to an authenticator app (Microsoft or Google Authenticator: "Enter a setup key", time-based). The same 6-digit code then works for the Owner and the Administrator of every demo tenant.
- **An Owner who already accepted their invitation** keeps the password they chose; the seed never changes it. Use "Forgot your password?" on the sign-in page; the email lands in the `.mail` folder.
- **Restarting** adds only what is missing. Change `Password` or `AuthenticatorKey` before the first start if you want other values.

## What is different from production

- **Licensed computers:** the demo staff have none, so `OperationsDemoData:SkipDeviceGate` lets them through the device check.
  - This works only when the environment is Development. Production and Staging ignore the setting, and it is off unless a settings file turns it on.
  - To test the device pages (`/devices/enrol`, `/staff/devices`), set it to `false`.
- **Seeding:** nothing is seeded outside Development (`OperationsDemoData:SeedOnStartup`, `SeedStaff`).
