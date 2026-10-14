// =========================================================
// FILE: passkeys.js
// PURPOSE: Passkeys in the browser (WebAuthn, ADR-0006): turns
// the server's JSON options into browser calls and the
// browser's answers back into JSON.
// NOTES:
// - window.vixlyPasskeys: supported(), create(json), get(json);
//   create and get answer null when the user cancels or the
//   device refuses, so callers never see an exception
// - Sign-in page: progressive enhancement. The passkey button
//   stays hidden unless this script runs and the browser can
//   use passkeys; the password form works without it
// - No inline script anywhere (CSP script-src 'self')
// =========================================================
(function () {
    "use strict";

    function toBuffer(value) {
        var text = value.replace(/-/g, "+").replace(/_/g, "/");
        while (text.length % 4) { text += "="; }
        var binary = atob(text);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) { bytes[i] = binary.charCodeAt(i); }
        return bytes.buffer;
    }

    function toText(buffer) {
        var bytes = new Uint8Array(buffer);
        var binary = "";
        for (var i = 0; i < bytes.length; i++) { binary += String.fromCharCode(bytes[i]); }
        return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
    }

    function withoutNulls(value) {
        if (Array.isArray(value)) { return value.map(withoutNulls); }
        if (value && typeof value === "object") {
            var copy = {};
            Object.keys(value).forEach(function (key) {
                if (value[key] !== null && value[key] !== undefined) { copy[key] = withoutNulls(value[key]); }
            });
            return copy;
        }
        return value;
    }

    function supported() {
        return !!(window.PublicKeyCredential && navigator.credentials && navigator.credentials.create && navigator.credentials.get);
    }

    async function create(json) {
        try {
            var options = withoutNulls(JSON.parse(json));
            options.challenge = toBuffer(options.challenge);
            options.user.id = toBuffer(options.user.id);
            (options.excludeCredentials || []).forEach(function (item) { item.id = toBuffer(item.id); });
            var credential = await navigator.credentials.create({ publicKey: options });
            if (!credential) { return null; }
            return JSON.stringify({
                id: credential.id,
                rawId: toText(credential.rawId),
                type: credential.type,
                response: {
                    attestationObject: toText(credential.response.attestationObject),
                    clientDataJSON: toText(credential.response.clientDataJSON),
                    transports: credential.response.getTransports ? credential.response.getTransports() : []
                },
                clientExtensionResults: credential.getClientExtensionResults()
            });
        } catch (error) {
            return null;
        }
    }

    async function get(json) {
        try {
            var options = withoutNulls(JSON.parse(json));
            options.challenge = toBuffer(options.challenge);
            (options.allowCredentials || []).forEach(function (item) { item.id = toBuffer(item.id); });
            var credential = await navigator.credentials.get({ publicKey: options });
            if (!credential) { return null; }
            var answer = credential.response;
            return JSON.stringify({
                id: credential.id,
                rawId: toText(credential.rawId),
                type: credential.type,
                response: {
                    authenticatorData: toText(answer.authenticatorData),
                    clientDataJSON: toText(answer.clientDataJSON),
                    signature: toText(answer.signature),
                    userHandle: answer.userHandle ? toText(answer.userHandle) : null
                },
                clientExtensionResults: credential.getClientExtensionResults()
            });
        } catch (error) {
            return null;
        }
    }

    function wireSignIn() {
        var block = document.querySelector("[data-passkey-signin]");
        if (!block || !supported()) { return; }
        var form = block.querySelector("form");
        var start = block.querySelector("[data-passkey-start]");
        var failed = block.querySelector("[data-passkey-error]");
        block.hidden = false;
        start.addEventListener("click", async function () {
            start.disabled = true;
            failed.hidden = true;
            try {
                var response = await fetch(form.getAttribute("data-options"), {
                    method: "POST",
                    body: new FormData(form),
                    credentials: "same-origin",
                    cache: "no-store"
                });
                var answer = response.ok ? await get(await response.text()) : null;
                if (answer) {
                    form.querySelector("input[name=assertion]").value = answer;
                    form.submit();
                    return;
                }
            } catch (error) {
                // Shown below: the user can retry or use the password form.
            }
            failed.hidden = false;
            start.disabled = false;
        });
    }

    window.vixlyPasskeys = { supported: supported, create: create, get: get };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", wireSignIn);
    } else {
        wireSignIn();
    }
})();
