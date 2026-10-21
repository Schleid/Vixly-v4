// =========================================================
// FILE: ops-session.js
//
// PURPOSE:
// Warns a signed-in staff member before the 15-minute idle sign-out, then signs them out (Slice 1, D-O6; owner, 2026-10-20).
//
// RESPONSIBILITIES:
// - Show the warning at 13 minutes; at 15, sign out for real and land on the plain sign-in page
// - "Stay signed in" calls /session/keepalive and starts again
// - Several tabs: activity in one tab keeps the others signed in
//
// IMPORTANT NOTES:
// - The server ends the session on its own as well; this makes the page follow it instead of leaving a half-signed-in screen
// - Times come from data-idle-warn and data-idle-end (seconds) on body
// - The last activity is shared between tabs in localStorage; when storage is blocked, each tab counts on its own
// =========================================================
(function () {
    'use strict';
    var body = document.body;
    var warnAfter = Number(body.getAttribute('data-idle-warn'));
    var endAfter = Number(body.getAttribute('data-idle-end'));
    if (!warnAfter || !endAfter) {
        return;
    }
    var key = 'vx.staff.active';
    var box = document.querySelector('[data-idle-warning]');
    var stay = document.querySelector('[data-idle-stay]');
    var warnTimer = 0;
    var endTimer = 0;
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
            window.location.assign('/staff?idle=1');
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
        var elapsed = (now() - from) / 1000;
        window.clearTimeout(warnTimer);
        window.clearTimeout(endTimer);
        if (box) { box.hidden = elapsed < warnAfter; }
        warnTimer = window.setTimeout(function () { if (box) { box.hidden = false; } }, Math.max(0, warnAfter - elapsed) * 1000);
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
            window.fetch('/session/keepalive', { credentials: 'same-origin' }).then(function (response) {
                if (!response.ok || response.redirected) {
                    window.location.assign('/staff?ended=1');
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
