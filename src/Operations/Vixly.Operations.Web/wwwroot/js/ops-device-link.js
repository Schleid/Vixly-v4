// =========================================================
// FILE: ops-device-link.js
//
// PURPOSE:
// Moves the token of an emailed device link from the address
// (after "#") into the form, then removes it from the address
// (Slice 1C).
//
// IMPORTANT NOTES:
// - The token is after "#", so it never reaches a server log;
//   the form posts it once (Platform Slice 7)
// - No inline script: CSP script-src 'self'
// =========================================================
(function () {
    "use strict";

    var form = document.querySelector("[data-device-link]");

    if (!form) {
        return;
    }

    var field = form.querySelector("[data-device-token]");
    var token = window.location.hash.length > 1 ? decodeURIComponent(window.location.hash.substring(1)) : "";

    if (token) {
        field.value = token;
        history.replaceState(null, "", window.location.pathname);
    } else {
        form.querySelector("[data-device-token-missing]").hidden = false;
    }
})();
