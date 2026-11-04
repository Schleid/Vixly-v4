---
slug: card-payments-stripe
title: Conectar su cuenta de Stripe
summary: Los pagos con tarjeta van a la propia cuenta de Stripe de su empresa; la conecta con una clave restringida.
section: setting-up
order: 151
permissions: [ops.settings.view]
conditions: []
routes: []
related: [payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Cashier/CardSettingsPanel.razor
  - src/Operations/Vixly.Operations.Infrastructure/Payments/OperationsStripeGateway.cs
updated: 2026-11-03
review: needs-review
---

Los pagos con tarjeta, en la página de pago y en los lectores, los cobra la propia cuenta de Stripe de su empresa y se abonan en ella. {brandName} nunca guarda el dinero. Conecta la cuenta una sola vez, con tres valores de Stripe. Empiece en el modo de prueba de Stripe, pruebe un pago y luego haga lo mismo con la cuenta real.

## Pasos
1. En Stripe, abra *Desarrolladores*, *Claves API* y luego *Crear clave restringida*. Elija la integración propia y los permisos personalizados.
2. Ponga *Checkout Sessions*, *PaymentIntents* y *Charges and Refunds* en escritura (Write). Si usa lectores de tarjetas en el mostrador, ponga también *Terminal Readers* en escritura. Deje todo lo demás en *Ninguno (None)*, cree la clave y cópiela (empieza por rk_).
3. En Stripe, abra *Desarrolladores*, **Webhooks** y agregue un endpoint en la dirección que muestra el panel **Tarjetas (Stripe)**, para los eventos que allí se indican. Copie su secreto de firma (empieza por whsec_).
4. Copie la clave publicable (pk_) en *Desarrolladores*, *Claves API*, en el mismo modo que la clave restringida.
5. En **Métodos de pago**, **Tarjetas (Stripe)**, pegue la clave restringida, la clave publicable y el secreto de firma, y guarde.

## Qué pasa después
Las claves se comprueban con Stripe antes de guardarlas. Una clave restringida a la que le falte uno de los permisos anteriores se rechaza, indicando el permiso que hay que añadir. Una vez guardadas, las claves no se vuelven a mostrar (solo sus últimos caracteres) y se guardan cifradas. Marque las tarjetas en los métodos que acepta la empresa, haga un pequeño pago de prueba, reembólselo y compruebe en Stripe que los envíos del webhook se realizaron. Una clave secreta completa (sk_) también funciona, pero da mucho más acceso del necesario: prefiera la clave restringida. Para cambiar las claves, guarde las nuevas sobre las anteriores; para dejar de usarlas, use **Desconectar Stripe**.
