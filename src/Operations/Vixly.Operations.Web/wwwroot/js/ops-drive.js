/* The driver's route on the phone (Slice 12B, owner H-4): every stop is kept on the phone first, then sent in order.
   Without a signal the stops wait and leave by themselves when it is back; the page itself opens from the phone (service worker).
   Each stop is sent once: the server answers a resend of the same result as done. */
(() => {
    'use strict';
    const text = document.querySelector('[data-drive-text]');
    if (!text || !('indexedDB' in window)) return;
    const route = text.dataset.route;
    const say = (key, count) => (text.dataset[key] || '').replace('{count}', String(count ?? ''));

    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/staff-sw.js', { scope: '/staff/' }).catch(() => { });
        // Signing out clears the pages kept on the phone (stops not yet sent stay, they are the company's record).
        document.querySelectorAll('form[action="/staff/sign-out"]').forEach(form => form.addEventListener('submit', () => {
            navigator.serviceWorker.controller?.postMessage('clear');
        }));
    }

    // ---- the queue (IndexedDB) ----
    const open = () => new Promise((resolve, reject) => {
        const request = indexedDB.open('vx-drive', 1);
        request.onupgradeneeded = () => request.result.createObjectStore('stops', { keyPath: 'key' });
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
    const store = async mode => (await open()).transaction('stops', mode).objectStore('stops');
    const done = request => new Promise((resolve, reject) => { request.onsuccess = () => resolve(request.result); request.onerror = () => reject(request.error); });
    const all = async () => (await done((await store('readonly')).getAll())).sort((a, b) => a.happened.localeCompare(b.happened));
    const put = async item => done((await store('readwrite')).put(item));
    const remove = async key => done((await store('readwrite')).delete(key));

    // ---- what the page shows ----
    const banner = document.createElement('p');
    banner.className = 'ops-drive-queue';
    banner.setAttribute('role', 'status');
    banner.hidden = true;
    document.querySelector('.ops-delivery__stops')?.before(banner);

    const mark = (stopId, state, message) => {
        const item = document.getElementById('stop-' + stopId.replaceAll('-', ''));
        if (!item) return;
        item.dataset.local = state;
        item.querySelectorAll('.ops-delivery__record').forEach(form => { form.hidden = state === 'waiting' || state === 'sent'; });
        let note = item.querySelector('.ops-drive-note');
        if (!note) {
            note = document.createElement('p');
            note.className = 'ops-drive-note';
            item.querySelector('h3')?.after(note);
        }
        note.textContent = state === 'waiting' ? say('waitingOne') : state === 'sent' ? say('sent') : message || '';
        note.hidden = !note.textContent;
    };

    const show = async () => {
        const waiting = (await all()).filter(item => item.route === route);
        waiting.forEach(item => mark(item.stop, 'waiting'));
        banner.hidden = waiting.length === 0;
        banner.textContent = waiting.length ? say(navigator.onLine ? 'sending' : 'waiting', waiting.length) : '';
    };

    // ---- keep a stop on the phone ----
    document.querySelectorAll('.ops-delivery__record form').forEach(form => form.addEventListener('submit', async event => {
        if (event.defaultPrevented) return;
        event.preventDefault();
        const stop = form.action.split('/stops/')[1];
        const fields = [];
        const files = [];
        for (const [name, value] of new FormData(form).entries()) {
            if (name === '__RequestVerificationToken') continue;
            if (value instanceof File) { if (value.size > 0) files.push([name, value, value.name || 'photo.jpg']); }
            else fields.push([name, value]);
        }
        await put({ key: route + ':' + stop, route, stop, url: form.action, fields, files, happened: new Date().toISOString(), queued: !navigator.onLine });
        mark(stop, 'waiting');
        await flush();
    }));

    // ---- send what waits, oldest first ----
    let flushing = false;
    let sentAny = false;
    const flush = async () => {
        if (flushing) return;
        flushing = true;
        try {
            for (const item of await all()) {
                const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
                const body = new FormData();
                item.fields.forEach(([name, value]) => body.append(name, value));
                item.files.forEach(([name, blob, file]) => body.append(name, blob, file));
                body.append('happenedUtc', item.happened);
                body.append('queued', item.queued ? '1' : '0');
                if (token) body.append('__RequestVerificationToken', token);
                let response;
                try {
                    response = await fetch(item.url + '/sync', { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
                } catch {
                    // No signal: this one and the rest wait, and are counted as recorded away from the network.
                    if (!item.queued) { item.queued = true; await put(item); }
                    break;
                }
                const json = (response.headers.get('content-type') || '').includes('application/json') ? await response.json() : null;
                if (!json) {
                    // Signed out (idle, or the session ended) or the form token is old: the stops wait for a sign-in.
                    banner.hidden = false;
                    banner.textContent = say('signIn');
                    return;
                }
                await remove(item.key);
                if (json.ok) { sentAny = item.route === route || sentAny; mark(item.stop, 'sent'); }
                else mark(item.stop, 'refused', json.error);
            }
        } finally {
            flushing = false;
        }
        await show();
        if (sentAny && navigator.onLine && (await all()).filter(item => item.route === route).length === 0) {
            sentAny = false;
            location.reload();
        }
    };

    window.addEventListener('online', flush);
    window.addEventListener('offline', show);
    setInterval(() => { if (navigator.onLine) flush(); }, 30000);
    show().then(() => { if (navigator.onLine) flush(); });
})();
