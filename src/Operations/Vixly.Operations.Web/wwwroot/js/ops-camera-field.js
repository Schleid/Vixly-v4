/* A camera button beside the scan fields of the station pages (Slice 12D, owner H-7), for phones and handhelds without a scanner.
   Rugged handhelds type the scan like a keyboard and need nothing; this is for the camera. The code read goes into the field and the form is sent.
   Reading is done by ops-camera-reader.js (owner, 2026-10-27): a sharper camera, the formats the phone supports, a light, and a message when it cannot open. */
(() => {
    'use strict';
    const stations = /^\/staff\/(inspection|parcels\/(move|shelf-counts|counts|inspect)|bundles|arrivals|pickup)(\/|$)/i;
    if (!stations.test(location.pathname) || !navigator.mediaDevices?.getUserMedia || !window.VixlyCamera) return;
    const fields = document.querySelectorAll('form input[autofocus][autocomplete=off]:not([type=password]):not([type=hidden]):not([inputmode=numeric])');
    if (fields.length === 0) return;
    const label = document.documentElement.lang === 'fr' ? 'Caméra' : document.documentElement.lang === 'es' ? 'Cámara' : 'Camera';
    fields.forEach(field => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'vx-button vx-button--secondary ops-camera-button';
        button.textContent = label;
        field.after(button);
        const message = document.createElement('p');
        message.className = 'ops-camera-message';
        message.setAttribute('role', 'status');
        message.hidden = true;
        let open = null;
        let video = null;
        const close = () => {
            open?.close();
            open = null;
            video?.remove();
            video = null;
            button.setAttribute('aria-pressed', 'false');
        };
        const read = code => {
            close();
            field.value = code;
            field.form.requestSubmit ? field.form.requestSubmit() : field.form.submit();
        };
        const say = text => {
            message.textContent = text;
            message.hidden = false;
            (video || button).after(message);
        };
        button.addEventListener('click', async () => {
            if (video) { close(); return; }
            message.hidden = true;
            video = document.createElement('video');
            video.className = 'ops-camera-video';
            video.style.cssText = 'display:block;width:100%;max-width:480px;margin-top:0.5rem;border-radius:6px;';
            button.after(video);
            button.setAttribute('aria-pressed', 'true');
            open = await window.VixlyCamera.open(video, read, text => { close(); say(text); });
        });
    });
})();
