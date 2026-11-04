---
slug: announce-a-package
title: Anuncie un paquete
summary: Anunciar un paquete indica a su empresa qué compró y qué entrega debe esperar.
section: your-packages
order: 20
permissions: []
conditions: []
routes: [/customer/packages/new]
related: [your-mailbox-address, create-a-shipment]
sources:
  - src/Customer/Vixly.Customer.Web/Components/Portal/PreAlertFormPage.razor
  - src/Customer/Vixly.Customer.Web/Components/Portal/PackageFields.razor
  - src/Customer/Vixly.Customer.Web/Portal/CustomerPackageEndpoints.cs
updated: 2026-10-27
review: needs-review
signed_out: false
---

Anunciar un paquete indica a {brandName} qué compró y qué entrega debe esperar. No envía el paquete ni crea las instrucciones para su siguiente trayecto. Prepare el número de seguimiento del transportista; no es lo mismo que el número de pedido de la tienda.

## Pasos
1. Abra **Mis paquetes** y elija **Anunciar un paquete**.
2. Complete **Transportista** con el transportista que entrega al almacén. En **Tienda de compra**, indique dónde compró los productos, no una dirección de destino.
3. Copie **Número de rastreo** del transportista o del aviso de despacho. Utilice el seguimiento de este paquete, no el número de pedido de toda la compra.
4. Describa los productos en **Contenido**. Introduzca **Valor** y la fecha prevista si la conoce.
5. Si aparece un envío existente, selecciónelo solo si los productos corresponden a él. También puede elegirlo después.
6. Compruebe los datos y elija **Avisar**.

## Qué ocurre después
Lea la confirmación y revise la lista. Un anuncio no demuestra que el almacén haya recibido la mercancía. Si una compra llega en varios paquetes con seguimientos diferentes, anuncie cada paquete por separado. Después cree o elija el envío hacia el destino final.

## En esta pantalla
1. El transportista y la tienda de compra son distintos.
2. Introduzca el seguimiento del transportista.
3. Describa los productos reales.
4. Envíe después de revisar todos los campos.

[![Anuncie un paquete](/customer/help/media/v2/es-announce.png)](/customer/help/media/v2/es-announce.png)

Ejemplo ficticio. Utilice los datos de su propia cuenta. Seleccione la imagen para ampliarla.