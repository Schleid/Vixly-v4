---
slug: company-change-requests
title: Revise solicitudes de cambio de empresa
summary: Compare los datos legales propuestos con los actuales y con el justificante.
section: companies
order: 60
permissions: [platform.tenants.review_changes]
conditions: []
routes: [/tenants/changes, /tenants/changes/{RequestId:guid}]
related: [company-page, edit-company-details]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantChangeView.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantChangesOverview.razor
updated: 2026-10-23
review: needs-review
---

Las solicitudes de cambio permiten revisar modificaciones propuestas para los datos legales de una empresa. Compare la solicitud con la ficha actual y con su justificante antes de decidir.

Una explicación de rechazo no es una nota privada de trabajo. Lea la finalidad del campo antes de escribir y proporcione una explicación clara que permita actuar a la empresa. No apruebe una solicitud únicamente porque la escritura propuesta parezca razonable.

## Pasos
1. Abra **Cambios de empresa**, seleccione el estado adecuado y aplique el filtro. Abra la solicitud que va a revisar.
2. Compare **Datos actuales** con **Datos solicitados**. Compruebe la identidad de la empresa y el justificante adjunto a la solicitud.
3. Para aceptar el cambio, complete **Nota (opcional)** si es necesario y elija **Aprobar**. Confirme su identidad cuando se solicite.
4. Para rechazarlo, complete **Motivo del rechazo (se envía al cliente)** y elija **Rechazar**. Explique qué debe corregirse en lugar de dejar un rechazo poco claro.

## Qué ocurre después
Lea el estado resultante y revise los datos de la empresa tras la aprobación. Una solicitud ya resuelta no se convierte en una nueva solicitud pendiente. Si la pantalla indica que la ficha cambió, recárguela y revise la información actual antes de decidir.