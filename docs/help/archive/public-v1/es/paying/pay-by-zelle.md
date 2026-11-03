---
slug: pay-by-zelle
title: Pagar con Zelle
summary: Use los datos del destinatario mostrados y comunique el pago para confirmarlo.
section: paying
order: 140
permissions: []
conditions: []
routes: []
related: [pay-an-invoice-link, pay-to-start, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Features/Pay/ZellePanel.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Resources/PublicText.resx
updated: 2026-10-22
review: needs-review
---

Cuando una factura ofrece Zelle, realice la transferencia mediante su banco y comuníquela después a Vixly. Enviar el aviso no transfiere dinero desde su banco ni liquida la factura inmediatamente.

## Pasos
1. Lea **Destinatario** y los datos de correo, identificador o teléfono mostrados. Use los datos de la factura actual, no los copiados de un pago anterior. También puede aparecer un código QR.
2. Envíe el importe de la factura mediante el servicio Zelle de su banco. Compruebe el importe y conserve la confirmación bancaria.
3. En **Envié un pago por Zelle**, complete **Importe enviado**, **Fecha de envío**, **Nombre en la cuenta de origen** y **Código de confirmación de Zelle**. Estos datos identifican la transferencia ya enviada.
4. Seleccione **Avisar a Vixly**. Lea el mensaje al volver a la factura o al registro.

## Qué ocurre después
Vixly compara el aviso con el pago recibido en el banco. La página puede mostrar el importe y la fecha que se están comprobando. No vuelva a enviar el mismo dinero porque la factura no cambie inmediatamente.

Si ya hay un aviso pendiente, la página muestra su estado en lugar de otro formulario. Si cometió un error o necesita ayuda para identificar una transferencia, contacte con facturación de Vixly con los datos de la factura y la confirmación bancaria.
