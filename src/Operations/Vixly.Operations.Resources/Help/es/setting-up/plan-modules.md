---
slug: plan-modules
title: Lo que incluye su plan, e ingresar clientes
summary: Algunos planes dejan fuera partes del espacio, como el portal de clientes o el seguro. Qué cambia y cómo ingresar un cliente usted mismo.
section: setting-up
order: 107
permissions: [ops.customers.manage]
conditions: []
routes: [/staff/customers/new]
related: [plan-shipments, company-settings]
sources:
  - src/Operations/Vixly.Operations.Application/Plans/OperationsPlanModules.cs
  - src/Operations/Vixly.Operations.Web/Components/Customers/CustomerNewPage.razor
updated: 2026-10-30
review: ok
---

El plan de su empresa decide qué partes del espacio de trabajo usa. Cuando una parte no está en su plan, sus páginas no están en el menú y el mostrador no la ofrece.

## Partes que un plan puede dejar fuera
- **Portal de clientes:** los clientes no pueden crear una cuenta, iniciar sesión ni solicitar envíos en línea. Su sitio web no muestra botones de inicio de sesión ni de cuenta; conserva la página de rastreo.
- **Seguro:** ningún producto de seguro en el mostrador y ningún envío asegurado.
- **Cuentas de crédito:** no se factura a cuenta en la caja.
- **Suministros:** no se venden suministros en el mostrador.
- **Entrega a domicilio:** los envíos se retiran en una sucursal.
- **Mesa de soporte y tickets.**
- **Facturas de proveedores y ganancia.**

## Ingresar un cliente usted mismo
Sin el portal de clientes, los clientes no se registran solos. El personal que gestiona clientes los agrega:
1. Abra **Clientes** y elija **Nuevo cliente**.
2. Ingrese el nombre y el apellido, el nombre de la empresa si lo hay, el correo, el teléfono y el idioma.
3. Elija **Crear el cliente**. El cliente recibe un número de cliente y queda activo de inmediato.

## Bueno saber
- El correo es obligatorio y no debe pertenecer a otro cliente: allí van los avisos y los recibos.
- Cuando su plan incluye el portal de clientes, los clientes se registran solos y **Nuevo cliente** no se muestra.
- Su plan se muestra en **Facturación**. Para usar una parte que su plan deja fuera, pida un plan más completo.
