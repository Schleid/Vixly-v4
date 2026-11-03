---
slug: save-a-card
title: Guardar una tarjeta para pagos automáticos
summary: Revise el guardado de tarjeta y el consentimiento de cobros en el enlace de facturación.
section: paying
order: 130
permissions: []
conditions: []
routes: [/cards/{Token}]
related: [pay-an-invoice-link, pay-to-start, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Components/Pages/SaveCardPage.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Resources/PublicText.resx
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: needs-review
---

Facturación de Vixly puede enviarle un enlace para guardar una tarjeta. La página indica que no se cobra nada ahora y le lleva al proveedor de pagos para añadirla. Guardar la tarjeta y autorizar cobros automáticos son decisiones distintas.

## Pasos
1. Abra el enlace del correo y compruebe el nombre de la empresa. Asegúrese de que es la cuenta que desea actualizar.
2. Lea la información bajo **Guardar una tarjeta**. Si aparece una casilla de consentimiento de cobros automáticos, léala antes de seleccionarla. Se refiere a futuras facturas de Vixly, no solo a guardar la tarjeta.
3. Seleccione **Continuar** y complete la página del proveedor de pagos. Los datos de tarjeta se introducen allí, no en el formulario público de Vixly.
4. Vuelva a la página de resultado y lea el mensaje. **Gracias. Su tarjeta se está guardando.** significa que se está procesando la confirmación del guardado.

## Qué ocurre después
Si aceptó los cobros automáticos, el consentimiento mostrado explica que se aplican a las facturas de Vixly en su vencimiento y que puede desactivarlos contactando con facturación. El pago de una factura también puede ofrecer **Guardar esta tarjeta para próximas facturas**; revise esa elección por separado.

**Este enlace ya se usó para guardar una tarjeta.** indica que el enlace ya se utilizó. Contacte con facturación si necesita actualizar la tarjeta en vez de reenviar repetidamente el enlace anterior.

## En esta pantalla

1 marca la sección para guardar una tarjeta; 2 señala cómo continuar al proveedor. Revise cualquier opción de pago automático antes de salir.

[![Pantalla anotada que indica dónde actuar: Acción para guardar una tarjeta](/help/media/v2/es-save-card.png)](/help/media/v2/es-save-card.png)

Pantalla ilustrativa con datos ficticios. Los precios, fechas y opciones disponibles pueden variar. Seleccione la imagen para verla a tamaño completo.
