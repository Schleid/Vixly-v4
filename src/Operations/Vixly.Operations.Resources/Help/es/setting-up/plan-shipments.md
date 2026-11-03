---
slug: plan-shipments
title: Los envíos de su plan cada mes
summary: Algunos planes incluyen un número de envíos al mes. Vea dónde está, cuánto cuestan los envíos adicionales y cuándo se detienen los nuevos envíos.
section: setting-up
order: 106
permissions: [ops.settings.edit]
conditions: []
routes: [/staff/billing]
related: [company-settings]
sources:
  - src/Operations/Vixly.Operations.Application/Plans/OperationsShipmentCap.cs
  - src/Operations/Vixly.Operations.Web/Components/SignIn/WorkspacePage.razor
updated: 2026-10-30
review: ok
---

Algunos planes incluyen un número de envíos por mes calendario, contados en la zona horaria de su empresa. Todos los envíos cuentan: los creados en el mostrador, por los clientes en el portal y para los paquetes que llegan a su almacén. Los envíos cancelados no cuentan.

## Qué pasa durante el mes
1. **Al 80 %** de los envíos de su plan, el espacio de trabajo muestra un aviso y el propietario recibe un correo.
2. **Por encima del 100 %**, el trabajo sigue. Cada envío adicional se factura al precio por envío adicional de su plan, en su próxima factura.
3. **Al 125 %**, un último aviso: los nuevos envíos se detienen al 150 % a menos que cambie de plan.
4. **Por encima del 150 %**, los nuevos envíos en el mostrador y en el portal se detienen hasta que cambie de plan o termine el mes. La recepción, los pagos, los despachos y las entregas siguen, y los paquetes que llegan siguen recibiendo su envío.

## El mes de gracia
La primera vez que su empresa supera el 150 %, los nuevos envíos no se detienen ese mes. Los envíos adicionales se siguen facturando. Esto ocurre una sola vez.

## Bueno saber
- Solo las personas que administran la configuración de la empresa ven el aviso en el espacio de trabajo.
- A los clientes que intentan solicitar un envío en el portal mientras está detenido se les pide que lo contacten; no se les menciona su plan.
- Un nuevo mes reinicia la cuenta.
- Su plan aparece en **Facturación**. Para aumentar su límite, solicite un plan superior.
