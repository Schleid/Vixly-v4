---
slug: pay-to-start
title: Pagar su primera factura
summary: Pague la primera factura cuando corresponda y siga la página de confirmación.
section: sign-up
order: 80
permissions: []
conditions: []
routes: [/en/register/pay, /fr/register/pay, /es/register/pay, /register/payment]
related: [pay-by-zelle, save-a-card, after-sign-up]
sources:
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationPay.razor
  - src/Public/Vixly.Public.Web/Features/Registration/RegistrationProgress.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Features/Pay/ZellePanel.razor
  - build/HelpCapture/capture.cjs
  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json
updated: 2026-10-25
review: needs-review
---

Si su suscripción requiere un pago inicial, el registro abre la primera factura. Cuando aparezca **Estamos preparando su factura. Esta página se actualiza sola en unos segundos.**, deje que la página se actualice mientras se prepara. Una prueba o un primer período sin importe pendiente puede omitir este paso.

## Pasos
1. Compruebe el nombre de la empresa, **Factura**, **Importe pendiente** y cualquier **Vencimiento**. Use el importe y la moneda de esta factura.
2. Si se ofrece tarjeta, revise por separado **Guardar esta tarjeta para próximas facturas** y el consentimiento de cobros automáticos. Guardar una tarjeta no equivale a autorizar cargos automáticos. Seleccione **Continuar al pago** para continuar a la página del proveedor de pagos.
3. Si se ofrece Zelle, realice el pago mediante su banco con los datos del destinatario mostrados y complete después **Envié un pago por Zelle** en esta página.
4. Vuelva a la página de progreso tras pagar y lea su estado antes de realizar otra acción.

## Si algo falla
**El pago se canceló. No se cobró nada.** significa que se canceló el pago. Use **Ir al pago** si todavía debe pagar. Un pago Zelle comunicado se comprueba antes de contabilizarse. Por eso, un mensaje de confirmación puede aparecer antes de que el espacio esté listo. La página de retorno no demuestra que todos los pagos estén ya recibidos: siga el estado actual y el correo del recibo.

## En esta pantalla

1 marca la empresa y la factura que debe revisar; 2 la sección de tarjeta; 3 la acción que lleva al pago alojado. No se introduce ningún número de tarjeta en esta pantalla.

[![Pantalla anotada que indica dónde actuar: Primera factura y acción de pago](/help/media/v2/es-first-invoice.png)](/help/media/v2/es-first-invoice.png)

Pantalla ilustrativa con datos ficticios. Los precios, fechas y opciones disponibles pueden variar. Seleccione la imagen para verla a tamaño completo.
