---
slug: delivery-settings
title: Ajustes de entrega
summary: Los ajustes definen cómo registra su empresa la entrega a domicilio, los comprobantes requeridos y la actuación tras intentos fallidos.
section: setting-up
order: 200
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/delivery]
related: [pickup-settings, payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Delivery/DeliverySettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Los ajustes definen cómo registra {brandName} la entrega a domicilio, los comprobantes requeridos y la actuación tras intentos fallidos. Revise el procedimiento de la empresa antes de editar. Estas opciones preparan el trabajo; no crean rutas, asignan conductores ni registran entregas realizadas.

## Pasos
1. Abra **Ajustes de entrega a domicilio** y revise **Cómo registran los repartidores sus paradas**. Lea los métodos antes de seleccionar uno cuando pueda editar.
2. Revise **Prueba en la puerta** y su indicación; seleccione los comprobantes previstos.
3. En **Entregas fallidas**, revise **Intentos antes de desistir** y qué ocurre después de esos intentos.
4. Compruebe **Pago**, incluido **Aceptar pago contra entrega**, y lea la explicación sobre efectivo.
5. Revise **Avisar por correo a los clientes cuando su envío sale a reparto** y elija **Guardar** si aparece el botón.

## Qué ocurre después
Lea el resultado y vuelva a abrir el formulario para confirmar las opciones. Un intento y una entrega satisfactoria son resultados distintos. El personal debe registrar el resultado real y sus comprobantes en el proceso de entrega. Seleccionar el cobro en efectivo aquí no recibe dinero ni liquida facturas.