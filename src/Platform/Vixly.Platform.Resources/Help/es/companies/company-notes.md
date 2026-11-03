---
slug: company-notes
title: Añada una nota interna de empresa
summary: Deje una nota objetiva para el resto del personal de Vixly.
section: companies
order: 35
permissions: [platform.tenants.add_notes]
conditions: []
routes: []
related: [company-page]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantNotes.razor
  - docs/platform/SLICE-2-TENANTS-DESIGN.md
updated: 2026-10-23
review: needs-review
---

Las notas internas proporcionan al personal de Vixly contexto para continuar el trabajo sobre una cuenta. Utilícelas para registrar el resultado de una intervención, un seguimiento acordado o una corrección. Permanecen dentro de Control y no son mensajes para la empresa.

Las notas forman un historial. Corrija un error añadiendo una nota nueva que explique la corrección. No espere sobrescribir una entrada anterior ni incluya contraseñas, códigos de recuperación o información de tarjetas de pago.

## Pasos
1. Abra la empresa correspondiente y compruebe su identidad antes de introducir información sobre la cuenta.
2. Busque **Notas internas**. Lea las últimas entradas para añadir contexto útil en lugar de repetir una pregunta sin resolver.
3. Complete **Nueva nota**. Indique qué ocurrió y qué queda por hacer. Distinga los hechos confirmados de la información pendiente de comprobar.
4. Elija **Añadir nota**. Espere el resultado y compruebe la nueva entrada en la lista.

## Qué ocurre después
La nota guardada aparece con su autor y su fecha. Otros empleados que consulten la cuenta podrán utilizarla para continuar el trabajo. Enviar un correo o modificar la cuenta sigue siendo una acción independiente; escribir una nota no realiza ninguna de esas tareas.