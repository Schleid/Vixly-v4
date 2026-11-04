---
slug: company-page
title: Consulte la ficha de una empresa
summary: Comprenda el resumen de la empresa y sus secciones disponibles.
section: companies
order: 20
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants/{TenantId:guid}]
related: [find-a-company, edit-company-details]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantSummary.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantCompanyFacts.razor
updated: 2026-10-23
review: needs-review
---

La ficha de una empresa es el punto de partida para atender su cuenta de Vixly. Reúne el resumen, los contactos y el historial de la cuenta. Otras secciones, como suscripción, facturación o licencias, aparecen según su acceso.

Utilice los enlaces de sección para recorrer la página sin buscar en todos los paneles. El estado de la empresa, el de la suscripción y el saldo de una factura describen cosas distintas. Consulte la sección adecuada antes de decidir qué requiere atención.

## Pasos
1. Busque la empresa en **Inquilinos** y abra su nombre.
2. Revise **Resumen**. Compare el código y la identidad de la empresa con la solicitud que está atendiendo.
3. Consulte **Empresa** y **Contactos** para conocer el perfil y los contactos registrados.
4. Lea **Notas internas** y **Historial** para comprender el contexto disponible. Siga un enlace de sección cuando corresponda a su tarea.

## Qué ocurre después
Consultar una ficha no cambia el estado de la empresa. Cada acción disponible tiene su propia confirmación y su resultado. Distinga las notas internas de los mensajes destinados a la empresa. Si otra sección no está disponible, pida al compañero responsable que la atienda con su propia cuenta.