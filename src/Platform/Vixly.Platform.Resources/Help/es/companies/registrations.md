---
slug: registrations
title: Consulte las inscripciones
summary: Consulte el avance de una inscripción de autoservicio.
section: companies
order: 50
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants/registrations, /tenants/registrations/{RegistrationId:guid}]
related: [find-a-company]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Registrations/RegistrationList.razor
  - src/Platform/Vixly.Platform.Web/Features/Registrations/RegistrationSummary.razor
updated: 2026-10-23
review: needs-review
---

La lista de inscripciones muestra las empresas que empezaron el registro de autoservicio. Utilícela para conocer la etapa alcanzada antes de pedir al propietario que vuelva a empezar.

Una inscripción y una cuenta de empresa establecida son registros distintos. Algunas inscripciones siguen esperando la confirmación del correo o el pago. Una inscripción finalizada puede enlazar con la empresa creada. Consulte el estado actual en lugar de suponer que todas las entradas ya pueden utilizar su espacio de trabajo.

## Pasos
1. Abra **Registros** y encuentre la inscripción correspondiente a la solicitud.
2. Abra el nombre de la empresa y revise **Registro**. Compare el nombre de su dirección y los datos del propietario con la solicitud.
3. Consulte las fechas de **Iniciado**, **Último enlace enviado** y **Correo verificado**. Permiten distinguir un mensaje enviado de una confirmación completada.
4. Revise **Consentimiento** y el estado actual. Cuando aparezca un enlace a la empresa, utilice **Abrir el inquilino** para consultar la cuenta creada.

## Qué ocurre después
Consultar una inscripción no reenvía correos, cancela el registro ni confirma pagos. Son acciones independientes. Mantenga al propietario en el registro existente cuando siga siendo válido y explique qué falta a partir de la etapa registrada.