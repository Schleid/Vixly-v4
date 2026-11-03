---
slug: support-calls
title: Llamadas de clientes y cuentas externas
summary: Permita que un centro de llamadas atienda a los clientes sobre un envío a la vez, sin ver sus datos.
section: setting-up
order: 70
permissions: [ops.support.lookup, ops.staff.edit, ops.customers.manage]
conditions: []
routes: [/staff/support, /staff/support/requests]
related: [branches-and-warehouses, ticket-desk]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Support/SupportPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Staff/StaffMemberPage.razor
updated: 2026-10-29
review: needs-review
---

Un agente de soporte atiende llamadas sobre un envío o un paquete a la vez. Nunca ve una lista de clientes, una dirección completa, un teléfono completo ni un correo, y no cambia nada: abre un ticket de llamada para su personal.

## Configurar un agente
1. Invite al agente como a cualquier miembro del personal y dele el rol **Agente de soporte** para toda la empresa.
2. Abra la persona y, en **Cuenta externa**, marque **Es una cuenta externa**.
3. Elija la **Fecha de fin** del contrato, los **Días de trabajo** y las horas **Desde** y **Hasta** en la zona horaria de su empresa. Seleccione **Guardar**.

Una cuenta externa solo puede usar **Llamadas de clientes**, solo en sus días y horas, y hasta su fecha de fin. Fuera de ellos no puede iniciar sesión y una sesión abierta deja de funcionar.

## Atender una llamada
1. Abra **Llamadas de clientes** y escriba el número de teléfono completo de la persona. Seleccione **Buscar a la persona**. Solo ve su nombre, la inicial del apellido, su código y si está al corriente.
2. Pida el número de seguimiento: el de su empresa, o el del transportista (Amazon, UPS, USPS...) para un paquete. Seleccione **Abrir**.
3. Solo se abre si el envío o el paquete es de la persona que llama, como remitente o destinatario. Ve dónde está, las retenciones, el destino y el importe pendiente.
4. Si la persona pide un cambio, escríbalo y seleccione **Abrir un ticket de llamada**.

Una persona sin cuenta, como un destinatario, solo obtiene el estado, con el número de seguimiento de su empresa.

## Qué pasa después
El agente elige el tema del ticket; el departamento que atiende ese tema lo ve en **Tickets** (vea *La mesa de tickets*). Cada búsqueda por teléfono o número de seguimiento queda en el registro de auditoría, y un agente puede hacer como máximo 60 por hora.
