---
slug: ticket-desk
title: La mesa de tickets
summary: Cada ticket va al departamento que atiende su tema, alguien lo toma, y se avisa a los responsables cuando espera demasiado.
section: setting-up
order: 71
permissions: [ops.parcels.view, ops.invoices.view, ops.customers.manage, ops.settings.edit]
conditions: []
routes: [/staff/tickets/]
related: [support-calls]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Tickets/TicketsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Tickets/TicketPage.razor
updated: 2026-10-29
review: needs-review
---

**Tickets** reúne los tickets de paquetes faltantes de los clientes y los tickets que los agentes de soporte abren durante las llamadas. Cada ticket tiene un tema, y solo el departamento de ese tema lo ve.

## Quién ve qué
- **Paquete faltante o equivocado:** el personal del almacén (que puede ver los paquetes allí).
- **Facturación y pago:** Finanzas (que puede ver facturas o cobrar).
- **Retiro, entrega, destinatario:** atención al cliente, y el personal de envíos de la sucursal.
- **Daño o reclamo:** atención al cliente y seguros.
- **Otro:** atención al cliente.

El propietario y los administradores ven todos los tickets.

## Atender un ticket
1. Abra **Tickets**. Filtre por estado (**Nuevos**, **Tomados**, **Escalados**...), tema, almacén o **Tomados por mí**.
2. Abra un ticket y seleccione **Me encargo**, o elija a un colega y seleccione **Asignar**.
3. Un ticket en el departamento equivocado: elija otro tema y seleccione **Mover**.
4. Responda, o **Añadir una nota** en el ticket de una llamada (el cliente no recibe correo por esos), y resuélvalo.

## Cuándo se avisa a los responsables
En **Cuándo se avisa a los responsables**, indique después de cuántas horas nadie tomó un ticket, y después de cuántos días sigue sin resolverse. Los responsables del departamento reciben un correo en cada paso; el segundo también va al propietario. Los tickets escalados muestran **Escalado** y aparecen primero.
