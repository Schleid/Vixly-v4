---
slug: pay-an-invoice-link
title: Pagar una factura desde un correo
summary: Abra el enlace de factura de Vixly, revise el importe y elija un método disponible.
section: paying
order: 120
permissions: []
conditions: []
routes: [/pay/{Token}]
related: [pay-by-zelle, save-a-card, contact-sales]
sources:
  - src/Public/Vixly.Public.Web/Components/Pages/PayLinkPage.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentPanel.razor
  - src/Public/Vixly.Public.Web/Features/Pay/PaymentTarget.cs
updated: 2026-10-22
review: needs-review
---

Un correo de factura puede contener un enlace a una página de pago de Vixly. Se refiere a la factura de Vixly de su empresa, separada de las facturas que pagan sus propios clientes en el portal con su marca.

## Pasos
1. Abra el enlace del correo. Compruebe el nombre de la empresa, **Factura**, **Importe pendiente**, la moneda y cualquier **Vencimiento**.
2. Si se ofrece **Pagar con tarjeta**, revise las opciones de guardar la tarjeta y permitir cobros automáticos. Seleccione **Continuar al pago** para abrir la página del proveedor de pagos.
3. Si se ofrece **Pagar con Zelle**, pague mediante su banco con los datos mostrados y envíe después el aviso en esta página.
4. Al volver, lea el estado. Un pago con tarjeta puede seguir confirmándose y un aviso Zelle espera la comprobación bancaria. Conserve el recibo cuando llegue por correo.

## Si algo falla
**Esta factura está pagada. Gracias.** significa que la factura ya está pagada. **El pago en línea no está disponible para esta factura. Contacte a facturación de Vixly.** le dirige a facturación de Vixly si no hay métodos en línea disponibles. Puede reintentar un pago cancelado si aún debe pagar. Si el enlace no abre o los datos requieren revisión, contacte con facturación en lugar de adivinar otra dirección. Use los enlaces de idioma si prefiere francés o español.
