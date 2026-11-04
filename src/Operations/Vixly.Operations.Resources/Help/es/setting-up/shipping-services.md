---
slug: shipping-services
title: Servicios de envío
summary: Los servicios determinan qué prestaciones ofrece su empresa y cómo se describen.
section: setting-up
order: 70
permissions: [ops.services.view]
conditions: []
routes: [/staff/services]
related: [price-lists, company-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/ShippingServicesPage.razor
updated: 2026-10-31
review: needs-review
---

Los servicios determinan qué prestaciones ofrece {brandName} y cómo se describen. Revise el modo de transporte y las opciones existentes antes de cambiar nada. La lista incluye nombres de servicios; su empresa elige cuáles ofrece y los valores operativos correspondientes.

## Pasos
1. Abra **Servicios de envío** desde la navegación y lea la indicación inicial.
2. Revise **Ofrecido** para cada servicio. Si puede editar, seleccione los servicios que realmente presta su empresa.
3. Compruebe **Su nombre (opcional)** e introduzca el nombre para los clientes cuando corresponda.
4. Compruebe **Código de etiqueta**: exactamente tres letras de la A a la Z, impresas en grande en la etiqueta de envío para que el almacén clasifique los paquetes a simple vista (por ejemplo VXA para aéreo estándar, VXE para aéreo exprés, VXS para marítimo, VXG para terrestre). Dos servicios pueden compartir un código.
5. Revise **Días hábiles mínimos** y **Días hábiles máximos** para el intervalo de tránsito previsto.
6. Revise el divisor volumétrico cuando esté disponible. Los servicios por contenedor muestran su propia explicación.
7. Si aparece, elija **Guardar** y lea el resultado.

## Qué ocurre después
Vuelva a abrir la página para confirmar los servicios ofrecidos y sus valores. Ofrecer un servicio no crea automáticamente una lista de precios ni un calendario de salidas. Configure las tarifas correspondientes antes de utilizar una cotización. Los lectores sin campos editables pueden consultar estas opciones sin guardar cambios. Un nuevo código de etiqueta aparece en las etiquetas impresas a partir de entonces; las que ya están en los paquetes conservan el anterior.