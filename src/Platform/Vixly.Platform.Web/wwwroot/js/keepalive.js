// =========================================================
// FILE: keepalive.js
// PURPOSE: Keeps the server session alive while the user is
// active in the console (ADR-0006: 5-minute idle limit), and
// warns before the idle sign-out with a countdown (owner, 2026-10-25).
// NOTES:
// - At most one call a minute, only after real activity, so
//   an idle tab still times out; activity inside that minute
//   is sent when the minute is over
// - The clock starts at the last call the server answered:
//   the server ends the session no sooner than 5 minutes
//   after it, so the warning (data-idle-warn) and the sign-out
//   (data-idle-end, seconds, on body) never come too late
// - The page signs out on its own (form data-sign-out, reason
//   idle) instead of being thrown out mid-screen
// - Several tabs share the last call in localStorage; when
//   storage is blocked, each tab counts on its own
// - The countdown only shows what the server enforces anyway
// =========================================================
(function () {
    "use strict";
    var body = document.body;
    var warnAfter = Number(body.getAttribute("data-idle-warn"));
    var endAfter = Number(body.getAttribute("data-idle-end"));
    var key = "vx.control.touched";
    var box = document.querySelector("[data-idle-warning]");
    var stay = document.querySelector("[data-idle-stay]");
    var countdown = document.querySelector("[data-idle-countdown]");
    var lastCall = 0;
    var pending = 0;
    var touched = Date.now();
    var warnTimer = 0;
    var endTimer = 0;
    var tick = 0;

    function store(at) {
        try { window.localStorage.setItem(key, String(at)); } catch (e) { /* storage blocked: this tab counts alone */ }
    }
    function stored() {
        try { return Number(window.localStorage.getItem(key)) || 0; } catch (e) { return 0; }
    }
    function format(seconds) {
        var s = Math.max(0, Math.ceil(seconds));
        return Math.floor(s / 60) + ":" + (s % 60 < 10 ? "0" : "") + (s % 60);
    }
    function showCountdown() {
        if (countdown) { countdown.textContent = format(endAfter - (Date.now() - touched) / 1000); }
    }
    function hide() {
        window.clearInterval(tick);
        tick = 0;
        if (box) { box.hidden = true; }
    }
    function show() {
        if (box && box.hidden) { box.hidden = false; }
        showCountdown();
        if (!tick) { tick = window.setInterval(showCountdown, 1000); }
    }
    function signOut() {
        var form = document.querySelector("form[data-sign-out]");
        if (!form) {
            window.location.assign("/sign-in?done=idle");
            return;
        }
        var reason = document.createElement("input");
        reason.type = "hidden";
        reason.name = "reason";
        reason.value = "idle";
        form.appendChild(reason);
        form.submit();
    }
    function arm(from) {
        if (!warnAfter || !endAfter) { return; }
        touched = Math.max(touched, from);
        var elapsed = (Date.now() - touched) / 1000;
        window.clearTimeout(warnTimer);
        window.clearTimeout(endTimer);
        if (elapsed < warnAfter) { hide(); } else { show(); }
        warnTimer = window.setTimeout(show, Math.max(0, warnAfter - elapsed) * 1000);
        endTimer = window.setTimeout(function () {
            // Another tab reached the server since: follow its clock.
            var other = stored();
            if (other > touched && (Date.now() - other) / 1000 < endAfter) {
                arm(other);
                return;
            }
            signOut();
        }, Math.max(0, endAfter - elapsed) * 1000);
    }
    function call() {
        pending = 0;
        lastCall = Date.now();
        var started = lastCall;
        return fetch("/session/keepalive", { credentials: "same-origin", cache: "no-store" })
            .then(function (response) {
                if (response.redirected || response.status === 401) {
                    window.location.assign("/sign-in");
                    return;
                }
                if (response.ok) {
                    store(started);
                    arm(started);
                }
            })
            .catch(function () { });
    }
    function activity() {
        var wait = 60000 - (Date.now() - lastCall);
        if (wait <= 0) { call(); return; }
        if (!pending) { pending = window.setTimeout(call, wait); }
    }
    ["pointerdown", "keydown", "scroll", "touchstart"].forEach(function (name) {
        window.addEventListener(name, activity, { passive: true });
    });
    window.addEventListener("storage", function (event) {
        if (event.key === key && event.newValue) { arm(Number(event.newValue)); }
    });
    if (stay) {
        stay.addEventListener("click", function () {
            // The click's own pointerdown or keydown usually made the call already.
            if (Date.now() - lastCall > 2000) {
                window.clearTimeout(pending);
                call();
            }
        });
    }
    // The page itself was a call the server answered.
    store(touched);
    arm(touched);
})();
