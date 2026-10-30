// =========================================================
// FILE: vixly-forms.js
// PURPOSE: Double-click protection for pages that post plain
// HTML forms (sign-in pages, the public site): ADR-0019.
// NOTES:
// - The first submit goes through; repeats are blocked and the
//   submit buttons are disabled until the next page arrives
// - Buttons are disabled after the submit starts, so the
//   clicked button's name and value are still posted
// - Released when the page comes back from the browser cache
//   (Back button) and after 15 seconds, so a lost answer never
//   leaves the form stuck
// - A form opts out with data-vx-allow-repeat
// - A form with a bot check (.cf-turnstile) is not sent until
//   the check has answered; its [data-vx-bot-check-message]
//   is shown instead. Platform still checks every answer
// - Live (interactive) console pages do not load this file:
//   their components guard clicks on the server
// =========================================================
(function () {
    "use strict";
    var HOLD_MS = 15000;
    var BUTTONS = 'button[type="submit"], button:not([type]), input[type="submit"]';

    function release(form) {
        form.removeAttribute("data-vx-submitting");
        var held = form.querySelectorAll("[data-vx-held]");
        for (var i = 0; i < held.length; i++) {
            held[i].disabled = false;
            held[i].removeAttribute("data-vx-held");
            held[i].removeAttribute("aria-busy");
        }
    }

    function hold(form) {
        var buttons = form.querySelectorAll(BUTTONS);
        for (var i = 0; i < buttons.length; i++) {
            if (!buttons[i].disabled) {
                buttons[i].disabled = true;
                buttons[i].setAttribute("data-vx-held", "");
                buttons[i].setAttribute("aria-busy", "true");
            }
        }
    }

    function botCheckPending(form) {
        if (!form.querySelector(".cf-turnstile")) {
            return false;
        }
        var answer = form.querySelector('input[name="cf-turnstile-response"]');
        var pending = !answer || !answer.value;
        var message = form.querySelector("[data-vx-bot-check-message]");
        if (message) {
            message.hidden = !pending;
        }
        return pending;
    }

    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || event.defaultPrevented || form.hasAttribute("data-vx-allow-repeat")) {
            return;
        }
        if (form.hasAttribute("data-vx-submitting")) {
            event.preventDefault();
            return;
        }
        if (botCheckPending(form)) {
            event.preventDefault();
            return;
        }
        form.setAttribute("data-vx-submitting", "");
        setTimeout(function () { hold(form); }, 0);
        setTimeout(function () { release(form); }, HOLD_MS);
    });

    window.addEventListener("pageshow", function () {
        var forms = document.querySelectorAll("form[data-vx-submitting]");
        for (var i = 0; i < forms.length; i++) {
            release(forms[i]);
        }
    });
})();
