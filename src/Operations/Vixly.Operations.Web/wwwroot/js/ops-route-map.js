/* The live map of a route that is out (Slice 12C, owner H-5): the driver's latest place, the way so far, refreshed every 30 seconds.
   Tiles come through the site; the texts come from the page. */
(() => {
    'use strict';
    const box = document.querySelector('[data-route-map]');
    if (!box || !window.L) return;
    const status = document.querySelector('[data-route-map-status]');
    const map = L.map(box, { attributionControl: true }).setView([0, 0], 2);
    L.tileLayer('/maps/tiles/{z}/{x}/{y}.png', { maxZoom: 18, attribution: box.dataset.attribution }).addTo(map);
    let marker = null;
    let line = null;
    let first = true;
    const draw = async () => {
        let data;
        try {
            const response = await fetch(box.dataset.routeMap, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
            if (!response.ok) return;
            data = await response.json();
        } catch { return; }
        const points = data.points.map(point => [Number(point.lat), Number(point.lng)]);
        if (points.length === 0) {
            status.textContent = box.dataset.none;
            return;
        }
        const last = data.points[data.points.length - 1];
        line ? line.setLatLngs(points) : (line = L.polyline(points, { weight: 3 }).addTo(map));
        marker ? marker.setLatLng(points[points.length - 1]) : (marker = L.marker(points[points.length - 1]).addTo(map));
        if (first) { map.setView(points[points.length - 1], 14); first = false; }
        const seen = box.dataset.seen.replace('{time}', new Date(last.at).toLocaleTimeString());
        status.textContent = data.next ? seen + ' ' + box.dataset.next.replace('{stop}', data.next).replace('{address}', data.address || '') : seen;
    };
    draw();
    setInterval(draw, 30000);
})();
