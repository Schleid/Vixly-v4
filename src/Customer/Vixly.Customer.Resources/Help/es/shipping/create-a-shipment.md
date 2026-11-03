---
slug: create-a-shipment
title: Cree un envío
summary: Un envío indica a su empresa adónde debe mandar sus mercancías después de que lleguen al almacén.
section: shipping
order: 30
permissions: []
conditions: []
routes: [/customer/packages/shipments/new]
related: [your-mailbox-address, announce-a-package]
sources:
  - src/Customer/Vixly.Customer.Web/Components/Portal/ShipmentFormPage.razor
  - src/Customer/Vixly.Customer.Web/Components/Portal/ShipmentDestinationFields.razor
  - src/Customer/Vixly.Customer.Web/Portal/CustomerShipmentEndpoints.cs
updated: 2026-10-27
review: needs-review
signed_out: false
---

Un envío indica a {brandName} adónde debe mandar sus mercancías después de que lleguen al almacén. Es distinto de anunciar un paquete. Anuncie los paquetes nuevos en su propia página; el formulario de envío permite seleccionar los disponibles sin volver a introducir sus seguimientos.

## Pasos
1. Abra **Mis paquetes** y elija **Nuevo envío**.
2. Elija el destino mostrado. Seleccione recogida o entrega a domicilio cuando se ofrezcan. Para domicilio, complete la dirección del destinatario, no la del almacén.
3. Revise **Servicio** si aparece. En **Número de paquetes**, indique cuántos paquetes espera en este envío. El valor previsto ayuda a estimar; no es un pago.
4. Compruebe **Destinatario** y **Teléfono del destinatario (con el código del país, por ejemplo +509)**. Corresponden a quien recibe las mercancías en el destino final.
5. Revise las opciones. Elija seguro después de leer las condiciones y completar la aceptación solicitada.
6. En **Paquetes para poner en él**, seleccione los paquetes anunciados o recibidos correspondientes, si aparecen.
7. Revise destino y selección y elija **Crear el envío**.

## Qué ocurre después
Abra el envío guardado y compruebe los detalles. Crearlo no significa que la mercancía haya salido, llegado o sido pagada. Si falta un paquete, compruebe si está anunciado o asignado a otro envío antes de crear otro anuncio.

## En esta pantalla
1. Elija destino final y método de entrega.
2. Indique cuántos paquetes espera.
3. Compruebe nombre y teléfono del destinatario.
4. Revise las opciones.
5. Seleccione los paquetes correspondientes.
6. Cree después de revisar las instrucciones.

[![Cree un envío](/customer/help/media/v2/es-shipment.png)](/customer/help/media/v2/es-shipment.png)

Ejemplo ficticio. Utilice los datos de su propia cuenta. Seleccione la imagen para ampliarla.