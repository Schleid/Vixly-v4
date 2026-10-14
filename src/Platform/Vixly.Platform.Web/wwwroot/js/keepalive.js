// =========================================================
// FILE: keepalive.js
// PURPOSE: Keeps the server session alive while the user is
// active in the console (ADR-0006: 5-minute idle limit).
// NOTES:
// - At most one call a minute, only after real activity, so
//   an idle tab still times out
// - The Frontend/UI team adds the "about to sign out" warning
// =========================================================
(function () {
    "use strict";
    var last = 0;
    function activity() {
        var now = Date.now();
        if (now - last < 60000) { return; }
        last = now;
        fetch("/session/keepalive", { credentials: "same-origin", cache: "no-store" })
            .then(function (response) {
                if (response.redirected || response.status === 401) { window.location.assign("/sign-in"); }
            })
            .catch(function () { });
    }
    ["pointerdown", "keydown", "scroll", "touchstart"].forEach(function (name) {
        window.addEventListener(name, activity, { passive: true });
    });
})();
