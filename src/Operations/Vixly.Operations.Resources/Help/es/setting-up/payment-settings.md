---
slug: payment-settings
title: Métodos de pago: tarjetas, Zelle y transferencias
summary: Los ajustes describen cómo acepta dinero su empresa.
section: setting-up
order: 150
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/payments]
related: [invoice-settings, credit-settings, card-payments-stripe]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Cashier/PaymentSettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Cashier/CardSettingsPanel.razor
updated: 2026-10-25
review: needs-review
---

Los ajustes describen cómo acepta dinero {brandName}. Compruebe los datos reales de la cuenta de empresa antes de activar un método. Estas opciones preparan los pagos; no confirman que un cliente haya enviado dinero ni que una factura esté pagada.

## Pasos
1. Abra **Métodos de pago** y revise **Métodos que acepta la empresa**. Seleccione los métodos aceptados solo cuando pueda editar.
2. Revise los nombres de métodos adicionales para que el personal los identifique de forma coherente.
3. Para Zelle, compruebe destinatario, imagen QR e instrucciones en cada idioma. Utilice únicamente los datos de pago previstos por la empresa.
4. Elija **Guardar** si aparece y lea el resultado.
5. Revise **Tarjetas (Stripe)** por separado. Si aparecen controles de configuración, siga las instrucciones para la cuenta de empresa y confirme su identidad cuando se solicite.

## Qué ocurre después
Vuelva a abrir la página y compruebe métodos y destinatario. Conectar la cuenta, aceptar tarjetas y configurar lectores son decisiones distintas. Mantenga privadas las credenciales. Activar un método no cobra dinero; el personal debe seguir el proceso de factura o caja y comprobar su resultado.