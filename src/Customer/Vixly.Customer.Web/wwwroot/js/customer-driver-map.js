/* The driver bringing the shipment (Slice 12C, owner H-5): the words and, once it is the next stop, the map; asked again every 30 seconds. */
(() => {
    'use strict';
    const panel = document.querySelector('[data-driver-refresh]');
    if (!panel) return;
    const line = panel.querySelector('[data-driver-line]');
    const box = panel.querySelector('[data-driver-map]');
    let map = null;
    let marker = null;
    const place = (lat, lng) => {
        if (!box || !window.L || lat == null || lng == null) { if (box) box.hidden = true; return; }
        box.hidden = false;
        const at = [Number(lat), Number(lng)];
        if (!map) {
            map = L.map(box).setView(at, 15);
            L.tileLayer('/maps/tiles/{z}/{x}/{y}.png', { maxZoom: 18, attribution: panel.dataset.attribution || '' }).addTo(map);
            marker = L.marker(at).addTo(map);
        } else {
            marker.setLatLng(at);
            map.panTo(at);
        }
    };
    window.addEventListener('load', () => place(panel.dataset.lat || null, panel.dataset.lng || null));
    setInterval(async () => {
        try {
            const response = await fetch(panel.dataset.driverRefresh, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
            if (response.status !== 200) return;
            const data = await response.json();
            line.textContent = data.line;
            place(data.lat, data.lng);
        } catch { /* no signal: try again later */ }
    }, 30000);
})();
