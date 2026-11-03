---
slug: invoice-settings
title: Ajustes de facturas
summary: Los ajustes controlan la presentación de impuestos y las notas al pie de las facturas de su empresa.
section: setting-up
order: 140
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/invoices]
related: [company-settings, payment-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Invoices/InvoiceSettingsPage.razor
updated: 2026-10-25
review: needs-review
---

Los ajustes controlan la presentación de impuestos y las notas al pie de las facturas de {brandName}. Revise los textos y criterios aprobados por la empresa antes de editar. La página puede permitir consultar sin modificar; guarde solo cuando el formulario lo permita.

## Pasos
1. Abra **Facturas e impuestos** y revise **Impuesto**.
2. Compruebe **Nombre del impuesto en las facturas** y **Número fiscal de la empresa** según los registros de la empresa.
3. Revise cada tasa y su indicación. Una tasa cero significa que ese tipo no lleva impuesto; se excluyen cargos gubernamentales y suministros no gravados.
4. En **Impuestos en la factura**, elija **Una sola línea de impuesto** o **Una línea de impuesto por tipo** cuando pueda editar.
5. Revise las notas inglesa, francesa y española en **Nota al pie de cada factura**. Elija **Guardar** si aparece.

## Qué ocurre después
Lea el resultado y vuelva a abrir la página para confirmar los valores. Las notas van al pie de las facturas; revise cada idioma sin reunir todas las traducciones en un campo. La numeración pertenece a los ajustes de empresa. Este formulario no emite facturas ni registra pagos.