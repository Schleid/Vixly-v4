---
slug: signing-in-from-another-country
title: Inicie sesión desde otro país
summary: Comprenda las comprobaciones adicionales y los mensajes de bloqueo del dispositivo.
section: getting-started
order: 50
permissions: []
conditions: []
routes: []
related: [this-device, your-account, first-sign-in]
sources:
  - src/Operations/Vixly.Operations.Web/Components/SignIn/VerifyPage.razor
  - src/Operations/Vixly.Operations.Web/Components/SignIn/PasswordCheckPage.razor
  - src/Operations/Vixly.Operations.Web/Components/SignIn/SignInPage.razor
  - src/Operations/Vixly.Operations.Web/Security/OperationsDeviceGate.cs
updated: 2026-10-25
review: needs-review
---

Cuando trabaja desde otro país, {brandName} puede pedirle que vuelva a iniciar sesión o complete una comprobación adicional. Lea el mensaje de la página actual en lugar de suponer que la sesión anterior sigue siendo válida.

También puede bloquearse el acceso de un dispositivo al espacio. La confirmación de cuenta y la decisión sobre el dispositivo son cosas distintas. Completar una no garantiza que se haya eliminado la otra restricción.

## Pasos
1. Vuelva a la página de acceso del personal y utilice su propia cuenta. Compruebe el nombre de la empresa antes de introducir información.
2. Si la página solicita un código de autenticador, utilice el suyo y siga las instrucciones. Si solicita la contraseña, realice esa comprobación en su lugar.
3. Lea el resultado. Si el dispositivo está bloqueado o no puede continuar, contacte con el responsable de soporte de dispositivos y explique el mensaje mostrado.
4. Continúe únicamente cuando la página lo permita. Mantenga privados los códigos de recuperación y los enlaces mientras solicita ayuda.

## Qué ocurre después
Puede volver al espacio después de las comprobaciones o necesitar asistencia de la empresa para el dispositivo. No registre repetidamente dispositivos de reemplazo, no comparta otra cuenta ni prometa a un cliente que el acceso está restablecido antes de poder entrar en la pantalla de su tarea.