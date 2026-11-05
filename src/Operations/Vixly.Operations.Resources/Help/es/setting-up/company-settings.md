---
slug: company-settings
title: Ajustes de la empresa
summary: Los ajustes definen cómo su empresa registra importes, medidas y trabajo diario.
section: setting-up
order: 20
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings]
related: [invoice-settings, payment-settings, shipping-services]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Settings/SettingsPage.razor
updated: 2026-11-04
review: needs-review
---

Los ajustes definen cómo {brandName} registra importes, medidas y trabajo diario. Estos cambios afectan a varias pantallas; compruebe los valores actuales antes de modificarlos. Algunos lectores pueden consultar sin editar; utilice los campos y el botón de guardado que aparezcan disponibles.

## Pasos
1. En la parte Ajustes del menú, abra **Ajustes generales**.
2. Revise **Moneda de facturación**. Lea el aviso: la moneda queda bloqueada cuando existen facturas.
3. Compruebe **Peso**, **Dimensiones** y **Zona horaria** según el trabajo de su empresa.
4. Revise las opciones de cobro en destino, crédito, precios públicos y entrega. Lea los requisitos de acceso antes de cambiarlos.
5. Revise los prefijos en **Numeración**. Si la página permite editar, elija **Guardar**.

## Qué ocurre después
Lea el resultado y vuelva a abrir la página para confirmar los valores guardados. Guardar un ajuste no crea una factura, recibe un paquete ni configura un proveedor de pagos. Utilice las páginas específicas de facturas y pagos para sus reglas detalladas.