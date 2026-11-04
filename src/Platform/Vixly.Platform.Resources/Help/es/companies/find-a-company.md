---
slug: find-a-company
title: Busque una empresa
summary: Busque en el directorio e identifique la cuenta correcta.
section: companies
order: 10
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants]
related: [company-page, registrations]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantList.razor
updated: 2026-10-23
review: needs-review
---

Utilice el directorio para encontrar la cuenta de una empresa que ya existe en Vixly. Compruebe el nombre, el código y el estado actual antes de abrir una ficha. Dos nombres parecidos pueden corresponder a empresas distintas.

La búsqueda de la barra superior también conduce al directorio. Una inscripción que todavía no ha creado una empresa aparece en la lista de inscripciones. Por tanto, una búsqueda vacía no demuestra que nadie haya empezado el registro.

## Pasos
1. Abra **Inquilinos** desde la navegación.
2. Introduzca el nombre o el código en **Buscar nombre, código, dirección, correo o teléfono**. Utilice una parte distintiva del nombre si no conoce su escritura completa.
3. Limite la lista con **Estado** o **Etiqueta** cuando conozca esos datos. Elija **Aplicar** para aplicar los filtros.
4. Abra el nombre que coincida. Confirme la identidad de la empresa en su ficha antes de continuar con una tarea de soporte o facturación.

## Qué ocurre después
La ficha reúne el perfil y las secciones disponibles para su cuenta. Buscar o abrir una ficha no modifica nada. Si no encuentra coincidencias, revise los filtros y las inscripciones antes de iniciar otro registro.