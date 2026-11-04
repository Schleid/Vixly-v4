---
slug: items-and-customs-codes
title: Artículos y códigos aduaneros
summary: Los clientes declaran el contenido artículo por artículo; el personal añade el código aduanero y el país de origen antes de agrupar.
section: setting-up
order: 72
permissions: [ops.services.view, ops.inspection.check]
conditions: []
routes: [/staff/items, /staff/settings/items]
related: [shipping-services, inspection-rules]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Items/ItemCataloguePage.razor
  - src/Operations/Vixly.Operations.Web/Components/Items/ItemsReviewPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Items/ParcelItemsPanel.razor
updated: 2026-11-01
review: needs-review
---

Cada paquete indica ahora su contenido artículo por artículo: las palabras del cliente, la cantidad, el valor de una unidad y si es nuevo o usado. El cliente nunca escribe un código aduanero. {brandName} lo añade, con el país de origen leído en el artículo, para preparar correctamente los documentos de aduana.

## Pasos
1. Abra **Catálogo de artículos** en la navegación. La primera vez contiene artículos habituales y sus códigos aduaneros. Cambie, añada o quite artículos según su empresa. Las palabras de «también coincide con» son las que usan los clientes, en cualquier idioma.
2. Cuando las palabras de un cliente coinciden con un artículo del catálogo, la descripción, el código aduanero y la unidad se rellenan solos. El país de origen nunca se adivina.
3. Abra **Artículos por revisar** para ver los paquetes en origen con un artículo que aún no tiene código aduanero o país de origen, del más antiguo al más reciente.
4. Abra un paquete de la lista. En sus artículos, elija la **Descripción en los documentos** entre las sugerencias para rellenar el **Código aduanero** y escriba el **Origen** con dos letras según la etiqueta «Made in…». Marque **Añadir al catálogo** para un artículo habitual nuevo.
5. Elija **Guardar los artículos**.
6. Cuando su equipo esté listo, abra **Catálogo de artículos** y elija **Exigir códigos y origen**.

## Qué ocurre después
Desde que la regla está activa, un paquete recibido a partir de entonces no puede entrar en un lote ni en una caja, ni cerrarse su despacho, hasta que cada artículo tenga código aduanero y país de origen. El mensaje indica el paquete y el artículo que completar. Los paquetes recibidos antes de activar la regla nunca se detienen. La declaración del cliente (palabras, cantidad y valor) nunca la cambia el personal.
