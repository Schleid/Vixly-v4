---
slug: edit-company-details
title: Edite los contactos y ajustes de una empresa
summary: Actualice contactos, idioma, zona horaria y datos de gestión de la cuenta.
section: companies
order: 30
permissions: [platform.tenants.edit]
conditions: []
routes: []
related: [company-page, company-change-requests]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantProfileEditor.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantContactsEditor.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantAccountManagement.razor
updated: 2026-10-23
review: needs-review
---

Utilice la ficha para mantener los contactos y los ajustes de cuenta de una empresa. Estos cambios son distintos de aprobar una modificación de su identidad legal o su dirección.

Los datos legales están bloqueados en esta página. Para modificarlos, revise una solicitud de cambio presentada por la empresa. Las notas internas y los ajustes fiscales también tienen sus propias acciones; cambiar un contacto no actualiza ninguno de esos datos.

## Pasos
1. Abra la empresa correcta y busque **Empresa**. Elija **Editar ajustes** para cambiar el idioma o la zona horaria.
2. Revise los valores y elija **Guardar**. Utilice **Cancelar** si no quiere conservar la modificación.
3. En **Contactos**, elija **Editar**. Actualice el nombre, el correo o el teléfono opcional del grupo de contactos correspondiente y guarde.
4. En **Gestión de la cuenta**, elija **Editar** para cambiar **Gestor de la cuenta** o **Etiquetas**. Revise los datos antes de guardar.

## Qué ocurre después
La ficha recarga sus datos después de un cambio correcto. Compruebe el resultado mostrado antes de pasar a otra sección. Utilice datos de contacto actuales facilitados para esa empresa. No escriba explicaciones confidenciales de soporte en un campo de contacto ni en una etiqueta.