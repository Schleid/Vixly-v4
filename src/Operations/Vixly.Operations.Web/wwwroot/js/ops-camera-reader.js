// =========================================================
// FILE: ops-camera-reader.js
//
// PURPOSE:
// Reads barcodes with a phone or tablet camera, for the dock scan and the station pages (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Open the back camera sharp enough for a small label (Full HD asked, continuous focus where the phone allows)
// - Read with the browser's own reader, asking only for the formats it supports; otherwise the bundled reader (ZXing), trying harder
// - A light button when the camera has a torch; a short message when the camera cannot open
// - Check about six times a second, not on every frame, so older phones keep up
//
// IMPORTANT NOTES:
// - window.VixlyCamera.open(video, onCode, onMessage) returns a handle with close(); onCode gets the text read
// - Needs a secure page (https) and the person's permission for the camera
// - No inline script: CSP script-src 'self'
// =========================================================
(function () {
    'use strict';
    var wanted = ['code_128', 'code_39', 'qr_code', 'data_matrix', 'ean_13', 'upc_a', 'itf', 'pdf417'];

    function words(name) {
        var lang = document.documentElement.lang;
        var table = {
            light: { en: 'Light', fr: 'Lumière', es: 'Luz' },
            denied: { en: 'The camera could not open: allow it in the browser settings, then try again.', fr: 'La caméra n’a pas pu s’ouvrir : autorisez-la dans les réglages du navigateur, puis réessayez.', es: 'La cámara no se pudo abrir: permítala en la configuración del navegador y vuelva a intentarlo.' },
            none: { en: 'This browser cannot read barcodes with the camera. Type the code instead.', fr: 'Ce navigateur ne lit pas les codes-barres avec la caméra. Saisissez le code.', es: 'Este navegador no lee códigos de barras con la cámara. Escriba el código.' }
        };
        return table[name][lang] || table[name].en;
    }

    function loadZxing() {
        return new Promise(function (resolve) {
            if (window.ZXing) { resolve(); return; }
            var script = document.createElement('script');
            script.src = '/lib/zxing/zxing-0.21.3.min.js';
            script.onload = resolve;
            script.onerror = resolve;
            document.head.appendChild(script);
        });
    }

    async function nativeDetector() {
        if (!('BarcodeDetector' in window)) { return null; }
        try {
            var supported = window.BarcodeDetector.getSupportedFormats ? await window.BarcodeDetector.getSupportedFormats() : wanted;
            var formats = wanted.filter(function (format) { return supported.indexOf(format) >= 0; });
            return formats.length > 0 ? new window.BarcodeDetector({ formats: formats }) : null;
        } catch (error) {
            return null;
        }
    }

    async function open(video, onCode, onMessage) {
        var handle = { running: true, stream: null, reader: null, torch: null };
        var say = onMessage || function () { };
        handle.close = function () {
            handle.running = false;
            try { if (handle.reader) { handle.reader.reset(); } } catch (error) { /* already stopped */ }
            if (handle.stream) { handle.stream.getTracks().forEach(function (track) { track.stop(); }); }
            if (handle.torch) { handle.torch.remove(); }
            video.srcObject = null;
        };
        video.setAttribute('playsinline', '');
        video.muted = true;

        try {
            handle.stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } },
                audio: false
            });
        } catch (error) {
            say(words('denied'));
            handle.close();
            return null;
        }

        var track = handle.stream.getVideoTracks()[0];
        var abilities = track && track.getCapabilities ? track.getCapabilities() : {};
        if (abilities.focusMode && abilities.focusMode.indexOf('continuous') >= 0) {
            track.applyConstraints({ advanced: [{ focusMode: 'continuous' }] }).catch(function () { });
        }
        if (abilities.torch) {
            var light = document.createElement('button');
            light.type = 'button';
            light.className = 'vx-button vx-button--secondary ops-camera-light';
            light.textContent = words('light');
            light.setAttribute('aria-pressed', 'false');
            light.addEventListener('click', function () {
                var on = light.getAttribute('aria-pressed') !== 'true';
                track.applyConstraints({ advanced: [{ torch: on }] }).then(function () { light.setAttribute('aria-pressed', on ? 'true' : 'false'); }).catch(function () { });
            });
            video.after(light);
            handle.torch = light;
        }

        var detector = await nativeDetector();
        if (detector) {
            video.srcObject = handle.stream;
            try { await video.play(); } catch (error) { /* plays once visible */ }
            var tick = async function () {
                if (!handle.running) { return; }
                try {
                    if (video.readyState >= 2) {
                        var codes = await detector.detect(video);
                        var code = codes.find(function (item) { return item.rawValue && item.rawValue.trim().length >= 3; });
                        if (code) { onCode(code.rawValue.trim()); }
                    }
                } catch (error) { /* the next frame */ }
                setTimeout(tick, 160);
            };
            tick();
            return handle;
        }

        await loadZxing();
        if (!window.ZXing || !window.ZXing.BrowserMultiFormatReader) {
            say(words('none'));
            handle.close();
            return null;
        }
        var hints = new Map();
        var formats = window.ZXing.BarcodeFormat;
        hints.set(window.ZXing.DecodeHintType.POSSIBLE_FORMATS, [formats.CODE_128, formats.CODE_39, formats.QR_CODE, formats.DATA_MATRIX, formats.EAN_13, formats.UPC_A, formats.ITF, formats.PDF_417]);
        hints.set(window.ZXing.DecodeHintType.TRY_HARDER, true);
        var reader = new window.ZXing.BrowserMultiFormatReader(hints, 160);
        handle.reader = reader;
        try {
            await reader.decodeFromStream(handle.stream, video, function (result) {
                if (result && handle.running && result.getText().trim().length >= 3) { onCode(result.getText().trim()); }
            });
        } catch (error) {
            say(words('none'));
            handle.close();
            return null;
        }
        return handle;
    }

    window.VixlyCamera = { open: open };
}());
