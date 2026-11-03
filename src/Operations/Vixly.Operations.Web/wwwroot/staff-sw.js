/* The pages a driver needs without a signal (Slice 12B, owner H-4): the driver's routes and each route page, and the files they use.
   Pages: the network first, the copy on the phone when there is none. Files: the copy on the phone, refreshed in the background.
   Nothing else under /staff is kept. */
const PAGES = 'vx-drive-pages-v1';
const FILES = 'vx-drive-files-v1';
const page = /^\/staff\/(drive|delivery\/routes\/[0-9a-f-]{36})$/;
const file = /^\/(css|js|_content)\//;

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));
self.addEventListener('message', event => {
    if (event.data === 'clear') event.waitUntil(caches.delete(PAGES));
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') return;
    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    if (request.mode === 'navigate' && page.test(url.pathname)) {
        event.respondWith((async () => {
            const cache = await caches.open(PAGES);
            try {
                const response = await fetch(request);
                if (response.ok && !response.redirected) await cache.put(url.pathname, response.clone());
                return response;
            } catch {
                return (await cache.match(url.pathname)) || new Response('Offline', { status: 503, headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
            }
        })());
        return;
    }

    if (file.test(url.pathname)) {
        event.respondWith((async () => {
            const cache = await caches.open(FILES);
            const kept = await cache.match(request);
            const fresh = fetch(request).then(response => {
                if (response.ok) cache.put(request, response.clone());
                return response;
            }).catch(() => kept);
            return kept || fresh;
        })());
    }
});
