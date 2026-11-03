---
slug: customer-accounts
title: Suspender, reactivar, cerrar o bloquear a un cliente
summary: Cuando un cliente incumple sus condiciones o quiere irse: qué hace cada acción, quién puede hacerla y qué se le dice al cliente.
section: setting-up
order: 109
permissions: [ops.customers.manage]
conditions: []
routes: [/staff/customers]
related: [plan-modules]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Customers/CustomerStatusPanel.razor
  - src/Operations/Vixly.Operations.Domain/Customers/OperationsCustomer.cs
updated: 2026-10-31
review: ok
---

Abra el cliente desde **Clientes**. Al final de la página, elija el cambio e indique un motivo. El motivo queda en el historial del cliente; el cliente recibe un correo sin él.

## Las acciones
- **Suspender**: el cliente no puede iniciar sesión ni enviar nuevos envíos. Puede elegir **Reactivar automáticamente el**: la cuenta vuelve a estar activa sola ese día. Suspenda de nuevo para cambiar el día.
- **Reactivar**: termina una suspensión o reabre una cuenta cerrada.
- **Cerrar la cuenta**: para un cliente que se va o pide cerrarla. Se puede reabrir más tarde.
- **Bloquear (incumple las condiciones, definitivo)**: la cuenta nunca se puede reabrir, y el correo y el teléfono del cliente no pueden abrir una cuenta nueva con su empresa.

Solo el propietario y los administradores pueden bloquear a un cliente o reabrir una cuenta cerrada. Quienes gestionan clientes pueden suspender, reactivar y cerrar.

## Qué pasa con el trabajo en curso
Los paquetes ya recibidos y los envíos ya pagados siguen su curso y se pueden retirar. A un cliente que no está activo no se le puede facturar a crédito: paga antes de que se entreguen sus mercancías.

## Bueno saber
- La página del cliente también muestra sus últimos envíos, con el estado de cada uno.
- Las cuentas de crédito se abren y se cambian desde **Crédito**, no desde la página del cliente.
