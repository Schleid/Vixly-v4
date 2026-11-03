---
slug: publish-prices
title: Muestre precios en el sitio y el portal
summary: Los precios públicos permiten a su empresa elegir qué tarifas estándar ven visitantes y clientes.
section: setting-up
order: 110
permissions: [ops.charges.configure]
conditions: []
routes: []
related: [price-lists, company-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/ChargesPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Charges/PriceListPanel.razor
updated: 2026-10-25
review: needs-review
---

Los precios públicos permiten a {brandName} elegir qué tarifas estándar ven visitantes y clientes. Guardar y publicar una lista son decisiones distintas. Revise servicio, ruta y precios vigentes antes de publicarla; un acuerdo específico de un cliente no es una lista estándar pública.

## Pasos
1. Abra los cargos y seleccione el modo de transporte previsto.
2. Abra la lista estándar y compruebe servicio, ruta, versión actual y estado activo.
3. Revise **Publicar en el sitio público**. Esta opción aparece para listas estándar, no para las de un cliente concreto.
4. Seleccione la publicación y guarde con **Guardar**, revisando los campos de versión y fecha del formulario.
5. Compruebe que los precios públicos estén activados en los ajustes de empresa. Si esa página es de solo lectura, pida al responsable de ajustes que revise la opción.

## Qué ocurre después
Revise los precios visibles para los clientes cuando ambas opciones sean correctas. Una lista pública no garantiza el importe total de cada envío: siguen influyendo los suplementos y sus características. Retirar la publicación es distinto de desactivar una lista utilizada por el personal.