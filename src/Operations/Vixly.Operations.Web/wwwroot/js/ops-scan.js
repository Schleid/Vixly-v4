// =========================================================
// FILE: ops-scan.js
//
// PURPOSE:
// The scan app of the dock (Slice 3B, owner 2026-10-18):
// scan a carrier barcode, the system finds the customer and
// the parcel ID label prints.
//
// RESPONSIBILITIES:
// - Open or continue a carrier delivery, scan, close it
// - Read barcodes from a scanner (it types) or the camera
// - Print the label here (browser) or let the dock printer do it
// - Keep scans made without network and send them later
//
// IMPORTANT NOTES:
// - No inline script: CSP script-src 'self'
// - Every scan carries its own reference: sent twice, it counts once
// - A scan sent late (after an outage) prints no label: a late
//   label would go on the wrong package; the clerk scans it again
// - Printing here relies on the browser's kiosk printing for
//   one label per scan without a dialog
// =========================================================
(function () {
    "use strict";

    var root = document.querySelector("[data-scan]");
    var deliveriesStep = document.querySelector('[data-step="deliveries"]');

    document.querySelectorAll("[data-autosubmit]").forEach(function (select) {
        select.addEventListener("change", function () { select.form.submit(); });
    });

    if (!root || !deliveriesStep) {
        return;
    }

    var scanStep = document.querySelector('[data-step="scanning"]');
    var closedStep = document.querySelector('[data-step="closed"]');
    var tokenField = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenField ? tokenField.value : "";
    var warehouse = deliveriesStep.getAttribute("data-warehouse");
    var printer = document.querySelector("[data-printer]");
    var input = document.querySelector("[data-barcode]");
    var damagedButton = document.querySelector("[data-damaged]");
    var result = document.querySelector("[data-result]");
    var recent = document.querySelector("[data-recent]");
    var queueBadge = document.querySelector("[data-queue]");
    var video = document.querySelector("[data-video]");
    var label = document.querySelector("[data-label]");
    var state = { delivery: null, carrier: "", count: 0, damaged: false, last: "", lastAt: 0, camera: null };
    var queueKey = "vx.scan.queue";
    var freshFor = 120000;
    var sending = 0;
    var deliveryKey = "vx.scan.delivery." + warehouse;
    var printerKey = "vx.scan.printer." + warehouse;
    var audio = null;

    function text(name) { return root.getAttribute("data-text-" + name) || ""; }

    function load(key, fallback) {
        try { var value = localStorage.getItem(key); return value === null ? fallback : value; } catch (error) { return fallback; }
    }

    function save(key, value) {
        try { if (value === null) { localStorage.removeItem(key); } else { localStorage.setItem(key, value); } } catch (error) { /* private mode: no memory */ }
    }

    function newKey() {
        var bytes = new Uint8Array(16);
        crypto.getRandomValues(bytes);
        return Array.prototype.map.call(bytes, function (b) { return ("0" + b.toString(16)).slice(-2); }).join("");
    }

    function show(step) {
        deliveriesStep.hidden = step !== "deliveries";
        scanStep.hidden = step !== "scanning";
        closedStep.hidden = step !== "closed";
        if (step === "scanning") { focusInput(); }
    }

    function focusInput() {
        if (!scanStep.hidden && document.activeElement !== input) { input.focus({ preventScroll: true }); }
    }

    function beep(kind) {
        try {
            audio = audio || new (window.AudioContext || window.webkitAudioContext)();
            var tones = kind === "ok" ? [[880, 0.12]] : kind === "warn" ? [[440, 0.15], [330, 0.2]] : [[200, 0.35]];
            var at = audio.currentTime;
            tones.forEach(function (tone) {
                var oscillator = audio.createOscillator();
                var gain = audio.createGain();
                oscillator.frequency.value = tone[0];
                gain.gain.value = 0.25;
                oscillator.connect(gain);
                gain.connect(audio.destination);
                oscillator.start(at);
                oscillator.stop(at + tone[1]);
                at += tone[1] + 0.05;
            });
        } catch (error) { /* no sound on this device */ }
        if (navigator.vibrate) { navigator.vibrate(kind === "ok" ? 60 : [120, 60, 120]); }
    }

    async function post(url, body) {
        var response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token },
            body: JSON.stringify(body || {})
        });
        var data = null;
        try { data = await response.json(); } catch (error) { data = null; }
        return { ok: response.ok, status: response.status, data: data };
    }

    // ---- printer choice, remembered per device and warehouse
    if (printer) {
        var chosen = load(printerKey, "browser");
        if (Array.prototype.some.call(printer.options, function (option) { return option.value === chosen; })) { printer.value = chosen; }
        printer.addEventListener("change", function () { save(printerKey, printer.value); focusInput(); });
    }

    function printerId() {
        return printer && printer.value !== "browser" && printer.value !== "none" ? printer.value : null;
    }

    // ---- deliveries
    function start(id, carrier, count) {
        state.delivery = id;
        state.carrier = carrier || "";
        state.count = count || 0;
        save(deliveryKey, JSON.stringify({ id: id, carrier: state.carrier }));
        document.querySelector("[data-delivery-carrier]").textContent = state.carrier;
        updateCount();
        show("scanning");
    }

    function updateCount() {
        document.querySelector("[data-count]").textContent = text("pieces").replace("{count}", String(state.count));
    }

    document.querySelectorAll("[data-open]").forEach(function (button) {
        button.addEventListener("click", async function () {
            var carrier = button.getAttribute("data-open");
            if (!carrier) { carrier = (window.prompt(button.textContent) || "").trim(); }
            button.disabled = true;
            try {
                var answer = await post("/staff/scan/api/deliveries", { warehouseId: warehouse, carrier: carrier, key: newKey() });
                if (answer.ok) { start(answer.data.deliveryId, carrier, 0); } else { fail(answer); }
            } catch (error) {
                note(text("offline"), "error");
            } finally {
                button.disabled = false;
            }
        });
    });

    document.querySelectorAll("[data-continue]").forEach(function (button) {
        button.addEventListener("click", function () {
            start(button.getAttribute("data-continue"), button.getAttribute("data-carrier"), parseInt(button.getAttribute("data-pieces"), 10) || 0);
        });
    });

    // A delivery left open on this device continues where it stopped.
    try {
        var kept = JSON.parse(load(deliveryKey, "null"));
        var listed = kept && document.querySelector('[data-continue="' + kept.id + '"]');
        if (listed) { listed.click(); } else { save(deliveryKey, null); }
    } catch (error) { save(deliveryKey, null); }

    // ---- scanning
    document.querySelector("[data-scan-form]").addEventListener("submit", function (event) {
        event.preventDefault();
        var barcode = input.value.trim();
        input.value = "";
        if (barcode) { scan(barcode); }
    });

    input.addEventListener("blur", function () { setTimeout(focusInput, 150); });

    document.querySelector("[data-keyboard]").addEventListener("click", function () {
        input.setAttribute("inputmode", input.getAttribute("inputmode") === "none" ? "text" : "none");
        input.blur();
        focusInput();
    });

    damagedButton.addEventListener("click", function () {
        state.damaged = !state.damaged;
        damagedButton.setAttribute("aria-pressed", state.damaged ? "true" : "false");
        focusInput();
    });

    // The same code seen again within 2.5 seconds is the same package: a camera reports it on every frame while it is in view.
    function scan(barcode) {
        var now = Date.now();
        var same = barcode === state.last && now - state.lastAt < 2500;
        state.last = barcode;
        state.lastAt = now;
        if (same) { return; }
        var item = { url: "/staff/scan/api/deliveries/" + state.delivery + "/scans", body: { barcode: barcode, key: newKey(), damaged: state.damaged, printerId: printerId() }, print: printer ? printer.value : "browser", at: now };
        state.damaged = false;
        damagedButton.setAttribute("aria-pressed", "false");
        send(item, true);
    }

    // Worth sending again: no network, the server busy or down, or an expired form token (a plain 400 without an answer).
    function retryable(answer) {
        return answer.status >= 500 || answer.status === 429 || answer.status === 408 || (answer.status === 400 && !(answer.data && answer.data.code));
    }

    // Answers "sent" or "retry". A queued scan is sent without a printer once it is no longer fresh.
    async function send(item, live) {
        var fresh = Date.now() - (item.at || 0) < freshFor;
        var body = fresh ? item.body : Object.assign({}, item.body, { printerId: null });
        sending += 1;
        try {
            var answer = await post(item.url, body);
            if (answer.status === 401) { window.location.reload(); return "retry"; }
            if (answer.ok) { done(answer.data, item, live, fresh); return "sent"; }
            if (retryable(answer)) {
                if (live) { enqueue(item); }
                return "retry";
            }
            if (live) { fail(answer); } else { refused(item, answer); }
            return "sent";
        } catch (error) {
            if (live) { enqueue(item); }
            return "retry";
        } finally {
            sending -= 1;
        }
    }

    // A queued scan the server refused (the delivery was closed meanwhile, say): shown, never dropped silently.
    function refused(item, answer) {
        fail(answer);
        var line = document.createElement("li");
        line.setAttribute("data-outcome", "error");
        line.textContent = text("failed").replace("{barcode}", item.body.barcode).replace("{message}", (answer.data && answer.data.message) || text("outcome-error"));
        recent.insertBefore(line, recent.firstChild);
    }

    function done(data, item, live, fresh) {
        var outcome = data.outcome === "pre_alert" ? "matched" : data.outcome;
        // A queued scan answered "repeat" was saved before its answer was lost: it still counts and still needs its label here.
        var first = data.outcome !== "reprint" && (data.outcome !== "repeat" || !live);
        if (first) { state.count += 1; updateCount(); }
        result.setAttribute("data-outcome", outcome);
        document.querySelector("[data-result-code]").textContent = data.customerCode || text("unidentified");
        document.querySelector("[data-result-name]").textContent = data.customerName || data.outcomeText;
        document.querySelector("[data-result-number]").textContent = data.number;
        document.querySelector("[data-result-note]").textContent = data.customerName ? data.outcomeText : "";
        beep(outcome === "matched" || outcome === "reprint" ? "ok" : "warn");
        var line = document.createElement("li");
        line.setAttribute("data-outcome", outcome);
        line.textContent = data.number + " · " + (data.customerCode || text("unidentified")) + (fresh ? "" : " · " + text("label-needed"));
        recent.insertBefore(line, recent.firstChild);
        while (recent.children.length > 8) { recent.removeChild(recent.lastChild); }
        if (!fresh) { note(text("label-needed"), "queued"); return; }
        if (item.print === "browser" && (data.outcome !== "repeat" || !live)) { printLabel(data); }
    }

    function fail(answer) {
        result.setAttribute("data-outcome", "error");
        document.querySelector("[data-result-code]").textContent = "!";
        document.querySelector("[data-result-name]").textContent = (answer.data && answer.data.message) || text("outcome-error");
        document.querySelector("[data-result-number]").textContent = "";
        document.querySelector("[data-result-note]").textContent = "";
        beep("error");
    }

    function note(message, outcome) {
        result.setAttribute("data-outcome", outcome);
        document.querySelector("[data-result-note]").textContent = message;
    }

    // ---- printing on this device: the label area is the only thing printed (ops-scan.css @media print)
    function printLabel(data) {
        label.querySelector("[data-label-barcode]").innerHTML = data.barcode;
        label.querySelector("[data-label-number]").textContent = data.number;
        label.querySelector("[data-label-code]").textContent = data.customerCode || text("unidentified");
        window.print();
        focusInput();
    }

    // ---- scans made without network wait on the device and leave in order
    function queue() {
        try { return JSON.parse(load(queueKey, "[]")) || []; } catch (error) { return []; }
    }

    function enqueue(item) {
        var items = queue();
        items.push(item);
        save(queueKey, JSON.stringify(items));
        showQueue();
        note(text("queued"), "queued");
        beep("warn");
    }

    function showQueue() {
        var waiting = queue().length;
        queueBadge.hidden = waiting === 0;
        queueBadge.textContent = text("offline") + " · " + waiting;
    }

    var flushing = false;

    async function flush() {
        if (flushing) { return; }
        flushing = true;
        try {
            var items = queue();
            while (items.length > 0) {
                if ((await send(items[0], false)) !== "sent") { break; }
                items.shift();
                save(queueKey, JSON.stringify(items));
            }
        } finally {
            flushing = false;
            showQueue();
        }
    }

    window.addEventListener("online", flush);
    setInterval(flush, 5000);
    showQueue();

    // ---- camera: the browser's own reader where it has one, otherwise the bundled one
    document.querySelector("[data-camera]").addEventListener("click", async function () {
        if (state.camera) { stopCamera(); return; }
        try {
            var stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" }, audio: false });
            video.srcObject = stream;
            video.hidden = false;
            await video.play();
            state.camera = { stream: stream, running: true };
            if ("BarcodeDetector" in window) {
                var detector = new window.BarcodeDetector({ formats: ["code_128", "code_39", "ean_13", "upc_a", "itf", "data_matrix", "qr_code", "pdf417"] });
                var loop = async function () {
                    if (!state.camera || !state.camera.running) { return; }
                    try {
                        var found = await detector.detect(video);
                        if (found.length > 0) { scan(found[0].rawValue); }
                    } catch (error) { /* frame not ready */ }
                    setTimeout(loop, 250);
                };
                loop();
            } else if (window.ZXing && window.ZXing.BrowserMultiFormatReader) {
                var reader = new window.ZXing.BrowserMultiFormatReader();
                state.camera.reader = reader;
                reader.decodeFromStream(stream, video, function (found) { if (found) { scan(found.getText()); } });
            }
        } catch (error) {
            note(text("camera-denied"), "error");
            stopCamera();
        }
    });

    function stopCamera() {
        if (state.camera) {
            state.camera.running = false;
            if (state.camera.reader) { state.camera.reader.reset(); }
            if (state.camera.stream) { state.camera.stream.getTracks().forEach(function (track) { track.stop(); }); }
        }
        state.camera = null;
        video.hidden = true;
        video.srcObject = null;
        focusInput();
    }

    // ---- leave or close the delivery
    document.querySelector("[data-leave]").addEventListener("click", function () {
        stopCamera();
        save(deliveryKey, null);
        window.location.reload();
    });

    document.querySelector("[data-close]").addEventListener("click", async function (event) {
        var button = event.currentTarget;
        if (queue().length > 0) { await flush(); }
        // Scans still on their way would be missing from the receipt.
        for (var waited = 0; sending > 0 && waited < 20; waited += 1) { await new Promise(function (resolve) { setTimeout(resolve, 250); }); }
        if (sending > 0) { note(text("wait-sending"), "error"); return; }
        if (queue().length > 0 || !window.confirm(text("close-confirm").replace("{count}", String(state.count)))) { return; }
        button.disabled = true;
        try {
            var answer = await post("/staff/scan/api/deliveries/" + state.delivery + "/close", {});
            if (answer.ok) {
                stopCamera();
                save(deliveryKey, null);
                document.querySelector("[data-receipt-number]").textContent = answer.data.number;
                document.querySelector("[data-receipt-link]").href = answer.data.url;
                show("closed");
            } else {
                fail(answer);
            }
        } catch (error) {
            note(text("offline"), "error");
        } finally {
            button.disabled = false;
        }
    });

    document.querySelector("[data-again]").addEventListener("click", function () { window.location.reload(); });
})();
