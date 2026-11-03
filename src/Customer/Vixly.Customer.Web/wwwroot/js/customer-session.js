// =========================================================
// FILE: customer-session.js
//
// PURPOSE:
// Warns a signed-in customer before the company's inactivity sign-out, then signs them out (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Show the warning with its countdown, then sign out through the page's sign-out form
// - "Stay signed in" calls /customer/session/keepalive and starts again
// - Several tabs: activity in one tab keeps the others signed in
//
// IMPORTANT NOTES:
// - The same behaviour as the staff workspace (ops-session.js)
// - Times come from data-idle-warn and data-idle-end (seconds) on body, set by the company in Settings
// - The server ends the session on its own as well; this makes the page follow it
// =========================================================
(function () {
    'use strict';
    var body = document.body;
    var warnAfter = Number(body.getAttribute('data-idle-warn'));
    var endAfter = Number(body.getAttribute('data-idle-end'));
    if (!warnAfter || !endAfter) {
        return;
    }
    var key = 'vx.customer.active';
    var box = document.querySelector('[data-idle-warning]');
    var stay = document.querySelector('[data-idle-stay]');
    var countdown = document.querySelector('[data-idle-countdown]');
    var warnTimer = 0;
    var endTimer = 0;
    var tick = 0;
    var since = 0;
    function format(seconds) {
        var s = Math.max(0, Math.ceil(seconds));
        return Math.floor(s / 60) + ':' + (s % 60 < 10 ? '0' : '') + (s % 60);
    }
    function showCountdown() {
        if (countdown) { countdown.textContent = format(endAfter - (Date.now() - since) / 1000); }
    }
    function show() {
        if (box) { box.hidden = false; }
        showCountdown();
        if (!tick) { tick = window.setInterval(showCountdown, 1000); }
    }
    function hide() {
        window.clearInterval(tick);
        tick = 0;
        if (box) { box.hidden = true; }
    }
    function now() { return Date.now(); }
    function mark() {
        try { window.localStorage.setItem(key, String(now())); } catch (e) { /* storage blocked: this tab counts alone */ }
    }
    function lastActive() {
        try { return Number(window.localStorage.getItem(key)) || 0; } catch (e) { return 0; }
    }
    function signOut() {
        var form = document.querySelector('form[data-sign-out]');
        if (!form) {
            window.location.assign('/customer/sign-in?done=idle');
            return;
        }
        var reason = document.createElement('input');
        reason.type = 'hidden';
        reason.name = 'reason';
        reason.value = 'idle';
        form.appendChild(reason);
        form.submit();
    }
    function arm(from) {
        since = from;
        var elapsed = (now() - from) / 1000;
        window.clearTimeout(warnTimer);
        window.clearTimeout(endTimer);
        if (elapsed < warnAfter) { hide(); } else { show(); }
        warnTimer = window.setTimeout(show, Math.max(0, warnAfter - elapsed) * 1000);
        endTimer = window.setTimeout(function () {
            // Another tab was used since: follow its clock instead of signing out.
            var other = lastActive();
            if (other > from && (now() - other) / 1000 < endAfter) {
                arm(other);
                return;
            }
            signOut();
        }, Math.max(0, endAfter - elapsed) * 1000);
    }
    window.addEventListener('storage', function (event) {
        if (event.key === key && event.newValue) { arm(Number(event.newValue)); }
    });
    if (stay) {
        stay.addEventListener('click', function () {
            window.fetch('/customer/session/keepalive', { credentials: 'same-origin', cache: 'no-store' }).then(function (response) {
                if (!response.ok || response.redirected) {
                    window.location.assign('/customer/sign-in');
                    return;
                }
                mark();
                arm(now());
            });
        });
    }
    mark();
    arm(now());
}());
