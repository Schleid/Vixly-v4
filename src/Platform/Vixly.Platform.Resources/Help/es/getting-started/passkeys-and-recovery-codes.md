---
slug: passkeys-and-recovery-codes
title: Passkeys y códigos de recuperación
summary: Gestione sus passkeys y conserve sus nuevos códigos de recuperación de forma segura.
section: getting-started
order: 30
permissions: []
conditions: []
routes: []
related: [sign-in-and-second-factor]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Account/MySecondFactors.razor
  - src/Platform/Vixly.Platform.Web/Features/Account/MySecondFactorsPresenter.cs
updated: 2026-10-23
review: needs-review
---

Una passkey permite utilizar el método de acceso que ofrece su dispositivo o su llave de seguridad. Los códigos de recuperación le permiten recuperar el acceso cuando su segundo factor habitual no está disponible. Ambos pertenecen a su propia cuenta.

Prepare un lugar seguro antes de sustituir los códigos. Nunca los envíe en un mensaje de soporte ni los deje donde otra persona pueda leerlos. Un compañero que le ayude a recuperar el acceso no debe pedirle que revele un código.

## Pasos
1. Abra **Mi cuenta** y busque **Segundos factores**. Compruebe si el autenticador está activado y cuántos códigos de recuperación quedan sin utilizar.
2. Para añadir una passkey, complete **Nombre de la nueva llave de acceso (por ejemplo, Portátil del trabajo)** y elija **Añadir una llave de acceso**. Siga las instrucciones del navegador o dispositivo. Utilice un nombre que pueda reconocer después.
3. Para quitar una passkey, elija **Eliminar** junto a su nombre. Confirme su identidad si se solicita.
4. Elija **Obtener nuevos códigos de recuperación** para sustituir los códigos. Guarde los nuevos códigos de forma segura antes de elegir **Los he guardado**.

## Qué ocurre después
Los códigos nuevos sustituyen al conjunto anterior. Conserve el conjunto nuevo en lugar de depender de una copia antigua. Si el navegador no admite passkeys, lea el mensaje mostrado y utilice los otros métodos de acceso disponibles.