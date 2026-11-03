/* The driver's position while the route is out (Slice 12C, owner H-5): sent every 30 seconds while this page is open.
   A browser only knows the position while the page is on screen; the server keeps it only from the route's driver and stops us when it is not. */
(() => {
    'use strict';
    const script = document.currentScript;
    const route = script?.dataset.route;
    if (!route || !('geolocation' in navigator)) return;
    let latest = null;
    let sentAt = 0;
    let stopped = false;
    const send = async () => {
        if (stopped || !latest || !navigator.onLine || Date.now() - sentAt < 25000) return;
        const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
        const body = new FormData();
        body.append('lat', latest.coords.latitude.toFixed(6));
        body.append('lng', latest.coords.longitude.toFixed(6));
        body.append('accuracy', String(Math.round(latest.coords.accuracy || 0)));
        body.append('at', new Date(latest.timestamp).toISOString());
        if (token) body.append('__RequestVerificationToken', token);
        sentAt = Date.now();
        try {
            const response = await fetch('/staff/delivery/routes/' + route + '/position', { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
            const json = (response.headers.get('content-type') || '').includes('application/json') ? await response.json() : null;
            if (json && json.stop) { stopped = true; navigator.geolocation.clearWatch(watch); }
        } catch { /* no signal: the next one goes later */ }
    };
    const watch = navigator.geolocation.watchPosition(position => { latest = position; send(); }, () => { }, { enableHighAccuracy: true, maximumAge: 15000, timeout: 30000 });
    setInterval(send, 30000);
})();
