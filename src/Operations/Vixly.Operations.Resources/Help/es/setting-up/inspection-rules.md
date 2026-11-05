---
slug: inspection-rules
title: Reglas de inspección y artículos restringidos
summary: La configuración define controles e indicaciones sobre artículos restringidos de su empresa.
section: setting-up
order: 130
permissions: [ops.inspection.configure]
conditions: []
routes: [/staff/inspection/setup]
related: [shipping-services, branches-and-warehouses]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Inspection/InspectionSetupPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Inspection/RestrictedItemFields.razor
updated: 2026-11-04
review: needs-review
---

La configuración define controles e indicaciones sobre artículos restringidos de {brandName}. Revise estas opciones con los responsables de recepción y cumplimiento. Una lista inicial es una base para su revisión, no una confirmación de que cubra todos los requisitos de destinos y transportistas.

## Pasos
1. En la parte Ajustes del menú, abra **Ajustes de inspección** y lea la explicación inicial.
2. Revise **Porcentaje aleatorio**. Introduzca la proporción elegida en **Paquetes de cada cien** y elija **Guardar**.
3. En **Reglas**, elija tipo y valor y seleccione **Agregar la regla**. Revise las reglas antes de cambiar su activación.
4. Revise **Artículos restringidos**. Si se ofrece una lista inicial, lea la explicación antes de utilizar **Empezar con la lista sugerida**.
5. Revise nombre, palabras de coincidencia, restricción, destinos y transportistas antes de guardar cada artículo.

## Qué ocurre después
Compruebe las reglas y artículos resultantes. Estos ajustes orientan el trabajo; no inspeccionan paquetes, resuelven retenciones existentes ni certifican que una mercancía pueda viajar. El personal debe registrar la comprobación real y seguir las instrucciones de la empresa para artículos restringidos.