---
slug: superuser-actions
title: Responsabilidades del SuperUser
summary: Comprenda las decisiones excepcionales reservadas al SuperUser.
section: team-and-security
order: 90
permissions: [superuser]
conditions: []
routes: []
related: [company-page, sign-in-and-second-factor]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecycle.razor
  - src/Platform/Vixly.Platform.Web/Features/Users/UserInviteForm.razor
  - src/Platform/Vixly.Platform.Web/Features/Users/SignInSecurityPanel.razor
  - src/Platform/Vixly.Platform.Application/Security/PlatformPermissions.cs
updated: 2026-10-23
review: needs-review
---

El SuperUser tiene responsabilidades excepcionales en Vixly Control. La asignación de un rol no convierte a un empleado ordinario en SuperUser. Utilice su propia cuenta y deje un motivo claro para las decisiones que afectan al acceso.

La prohibición de una empresa es definitiva. Es distinta de la suspensión o desactivación, que tienen su propia ayuda. No utilice una prohibición cuando solo se pretende una pausa temporal.

## Pasos
1. Antes de prohibir una empresa, abra su ficha y compruebe la identidad, el historial y el motivo de la decisión. Busque **Acceso y estado**.
2. Elija **Vetar**, complete **Motivo** y revise la confirmación. Elija de nuevo la acción y confirme su identidad cuando se solicite.
3. Al invitar personal, revise **Nivel de autoridad** antes del envío. Conceda únicamente el nivel previsto; elegir un rol es una responsabilidad independiente.
4. Revise los cambios de política de acceso en su propio panel de configuración. Lea la explicación y confirme su identidad antes de aplicar el cambio.

## Qué ocurre después
Una empresa prohibida no puede volver a activa mediante la reactivación ordinaria. El motivo interno pertenece al historial de la cuenta. La autoridad elegida en una invitación afecta a la cuenta invitada; no modifica otra cuenta existente.