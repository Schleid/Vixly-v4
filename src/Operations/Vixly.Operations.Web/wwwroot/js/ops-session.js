// =========================================================
// FILE: ops-session.js
//
// PURPOSE:
// Warns a signed-in staff member before the idle sign-out, then signs them out (Slice 1, D-O6; owner, 2026-10-20).
//
// RESPONSIBILITIES:
// - Show the warning, then sign out for real and land on the plain sign-in page; the times are the company's (owner, 2026-10-27)
// - "Stay signed in" calls /session/keepalive and starts again
// - Several tabs: activity in one tab keeps the others signed in
// - While the warning shows, a countdown (data-idle-countdown) shows the time left (owner, 2026-10-25)
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
