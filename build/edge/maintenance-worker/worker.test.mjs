// =========================================================
// FILE: worker.test.mjs
//
// PURPOSE:
// Tests of the maintenance Worker. Run: node --test build/edge/maintenance-worker
//
// IMPORTANT NOTES:
// - The servers are a fake fetch; the KV store is a Map
// =========================================================
import { test, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import worker, { describe as route, forgetCache, hmacHex, sha256Hex, readWindows, readBrand, pickLanguage, passLink, PASS_COOKIE } from './worker.js';

const MAINTENANCE_SECRET = 'm'.repeat(40);
const BRAND_SECRET = 'b'.repeat(40);
const WINDOW_ID = '3f2b8c1e-0d4a-4b5c-9e6f-7a8b9c0d1e2f';
const PASS = 'pass-code-for-tests';

let store;
let origin;
let originCalls;

function kv() {
  return {
    async get(key, options) {
      const value = store.get(key);
      return value === undefined ? null : options && options.type === 'json' ? JSON.parse(value) : value;
    },
    async put(key, value) { store.set(key, value); },
    async delete(key) { store.delete(key); }
  };
}

function env(extra = {}) {
  return { VIXLY_EDGE: kv(), BASE_DOMAIN: 'vixly.ltd', EDGE_HOST: 'api.vixly.ltd', EDGE_MAINTENANCE_SECRET: MAINTENANCE_SECRET, EDGE_BRAND_SECRET: BRAND_SECRET, ...extra };
}

beforeEach(() => {
  store = new Map();
  originCalls = 0;
  origin = () => new Response('from the server', { status: 200 });
  globalThis.fetch = async request => {
    originCalls++;
    return origin(request);
  };
  forgetCache();
});

async function windowList(starts, ends, sites, extra = {}) {
  return {
    windows: [{
      id: WINDOW_ID,
      startsUtc: new Date(starts).toISOString(),
      endsUtc: new Date(ends).toISOString(),
      sites,
      messages: { en: 'New scanners', fr: 'Nouveaux scanners' },
      passHash: await sha256Hex(PASS),
      ...extra
    }]
  };
}

async function signed(path, data, secret, time = Date.now()) {
  const body = JSON.stringify(data);
  return new Request('https://api.vixly.ltd' + path, {
    method: 'POST',
    body,
    headers: { 'Content-Type': 'application/json', 'X-Vixly-Edge-Time': String(time), 'X-Vixly-Edge-Signature': await hmacHex(secret, `POST\n${path}\n${time}\n${body}`) }
  });
}

async function publish(data, time) {
  const response = await worker.fetch(await signed('/__edge/maintenance', data, MAINTENANCE_SECRET, time), env());
  forgetCache();
  return response;
}

function get(url, headers = {}) {
  return new Request(url, { headers });
}

// ----- Which site -----

test('sites: public, companies, customers, staff; control and the API are never closed', () => {
  const e = env();
  assert.equal(route(get('https://vixly.ltd/plans'), e).site, 'public');
  assert.equal(route(get('https://www.vixly.ltd/'), e).site, 'public');
  assert.equal(route(get('https://galeo.vixly.ltd/'), e).site, 'companies');
  assert.equal(route(get('https://galeo.vixly.ltd/customer/invoices'), e).site, 'customers');
  assert.equal(route(get('https://galeo.vixly.ltd/staff/customers'), e).site, 'staff');
  assert.equal(route(get('https://galeo.vixly.ltd/agent/jobs'), e).site, 'staff');
  assert.equal(route(get('https://galeo.vixly.ltd/maps/x'), e).site, 'staff');
  assert.equal(route(get('https://galeo.vixly.ltd/staffing'), e).site, 'companies');
  assert.equal(route(get('https://control.vixly.ltd/'), e).site, null);
  assert.equal(route(get('https://api.vixly.ltd/maintenance'), e).site, null);
  assert.equal(route(get('https://ops-internal.vixly.ltd/'), e).site, null);
  assert.equal(route(get('https://a.b.vixly.ltd/'), e).site, null);
  assert.equal(route(get('https://galeologistics.net/'), e).site, null);
  assert.equal(route(get('https://galeo.vixly.ltd/css/site.css'), e).site, null);
  assert.equal(route(get('https://galeo.vixly.ltd/customer/_framework/blazor.web.js'), e).site, null);
  assert.equal(route(get('https://galeo.vixly.ltd/'), env({ BASE_DOMAIN: '' })).site, null);
  assert.equal(route(get('https://api.vixly.ltd/__edge/brand'), e).edge, true);
  assert.equal(route(get('https://galeo.vixly.ltd/__edge/brand'), e).edge, false);
});

// ----- Normal days -----

test('without a window, every request goes to the server untouched', async () => {
  const response = await worker.fetch(get('https://galeo.vixly.ltd/staff'), env());
  assert.equal(await response.text(), 'from the server');
  assert.equal(originCalls, 1);
});

test('an announced window does not close anything', async () => {
  const now = Date.now();
  await publish(await windowList(now + 3600_000, now + 7200_000, ['staff']));
  const response = await worker.fetch(get('https://galeo.vixly.ltd/staff'), env());
  assert.equal(response.status, 200);
});

// ----- A running window -----

test('a running window closes its sites without contacting the server', async () => {
  const now = Date.now();
  await publish(await windowList(now - 60_000, now + 3600_000, ['staff', 'customers']));

  const staff = await worker.fetch(get('https://galeo.vixly.ltd/staff/customers', { 'Accept-Language': 'fr-FR,fr;q=0.9' }), env());
  assert.equal(staff.status, 503);
  assert.ok(Number(staff.headers.get('Retry-After')) >= 3500);
  const text = await staff.text();
  assert.match(text, /En maintenance/);
  assert.match(text, /Nouveaux scanners/);
  assert.doesNotMatch(text, /Vixly/);

  const portal = await worker.fetch(get('https://galeo.vixly.ltd/customer/'), env());
  assert.equal(portal.status, 503);

  const website = await worker.fetch(get('https://galeo.vixly.ltd/'), env());
  assert.equal(website.status, 200);
  const files = await worker.fetch(get('https://galeo.vixly.ltd/css/site.css'), env());
  assert.equal(files.status, 200);
  assert.equal(originCalls, 2);
});

test('programs get a short JSON answer', async () => {
  const now = Date.now();
  await publish(await windowList(now - 60_000, now + 600_000, ['staff']));
  const response = await worker.fetch(get('https://galeo.vixly.ltd/agent/jobs'), env());
  assert.equal(response.status, 503);
  assert.match(response.headers.get('Content-Type'), /application\/json/);
  assert.equal((await response.json()).error, 'maintenance');
});

test('the public site shows the Vixly name; a company page shows the company brand, never Vixly', async () => {
  const now = Date.now();
  await publish(await windowList(now - 60_000, now + 600_000, ['public', 'companies']));
  const publicPage = await (await worker.fetch(get('https://vixly.ltd/'), env())).text();
  assert.match(publicPage, /Vixly/);

  const brand = await worker.fetch(await signed('/__edge/brand', { slug: 'galeo', name: 'Galeo Logistics', colour: '#0a7f5a', language: 'fr', logo: 'iVBORw0KGgo=', logoType: 'image/png' }, BRAND_SECRET), env());
  assert.equal(brand.status, 200);
  forgetCache();
  const company = await (await worker.fetch(get('https://galeo.vixly.ltd/'), env())).text();
  assert.match(company, /data:image\/png;base64,iVBORw0KGgo=/);
  assert.match(company, /Galeo Logistics/);
  assert.match(company, /En maintenance/);
  assert.doesNotMatch(company, /Vixly/);

  const unknown = await (await worker.fetch(get('https://other.vixly.ltd/'), env())).text();
  assert.match(unknown, /Under maintenance/);
  assert.doesNotMatch(unknown, /Vixly/);
});

test('the pass link sets the cookie, which opens the closed sites until the end', async () => {
  const now = Date.now();
  await publish(await windowList(now - 60_000, now + 600_000, ['staff']));
  origin = () => new Response(null, { status: 302, headers: { Location: '/' } });

  const wrong = await worker.fetch(get('https://galeo.vixly.ltd/staff/maintenance-pass/nope'), env());
  assert.equal(wrong.status, 404);

  const passed = await worker.fetch(get('https://galeo.vixly.ltd/staff/maintenance-pass/' + PASS), env());
  assert.equal(passed.status, 302);
  const cookie = passed.headers.get('Set-Cookie');
  assert.match(cookie, new RegExp('^' + PASS_COOKIE + '='));
  assert.match(cookie, /Secure; HttpOnly; SameSite=Lax/);

  origin = () => new Response('from the server', { status: 200 });
  const value = cookie.split(';')[0];
  const open = await worker.fetch(get('https://galeo.vixly.ltd/staff', { Cookie: 'a=b; ' + value }), env());
  assert.equal(open.status, 200);

  const forged = value.slice(0, -4) + '0000';
  const closed = await worker.fetch(get('https://galeo.vixly.ltd/staff', { Cookie: forged }), env());
  assert.equal(closed.status, 503);
});

test('the pass link still works while the server is down for the deploy', async () => {
  const now = Date.now();
  await publish(await windowList(now - 60_000, now + 600_000, ['customers']));
  origin = () => new Response('Service Unavailable', { status: 503 });
  const passed = await worker.fetch(get('https://galeo.vixly.ltd/customer/maintenance-pass/' + PASS), env());
  assert.equal(passed.status, 302);
  assert.equal(passed.headers.get('Location'), '/customer/');
  await publish(await windowList(now - 60_000, now + 600_000, ['staff']), Date.now() + 1000);
  const staff = await worker.fetch(get('https://galeo.vixly.ltd/staff/maintenance-pass/' + PASS), env());
  assert.equal(staff.headers.get('Location'), '/staff/');
  assert.ok(passed.headers.get('Set-Cookie'));
});

// ----- The safety net -----

test('a failing server gets the general page; its own maintenance answer and other errors pass through', async () => {
  for (const status of [502, 503, 504, 522, 524]) {
    origin = () => new Response('bad gateway', { status });
    const response = await worker.fetch(get('https://galeo.vixly.ltd/staff', { 'Accept-Language': 'es' }), env());
    assert.equal(response.status, 503, 'status ' + status);
    assert.match(await response.text(), /no está disponible/);
  }

  origin = () => new Response('the site page', { status: 503, headers: { 'X-Vixly-Maintenance': '1' } });
  assert.equal(await (await worker.fetch(get('https://galeo.vixly.ltd/staff'), env())).text(), 'the site page');

  for (const status of [404, 500, 403]) {
    origin = () => new Response('own', { status });
    const response = await worker.fetch(get('https://galeo.vixly.ltd/staff'), env());
    assert.equal(response.status, status);
  }

  origin = () => { throw new Error('network'); };
  assert.equal((await worker.fetch(get('https://galeo.vixly.ltd/'), env())).status, 503);
});

test('live connections and Control pass straight through', async () => {
  origin = () => new Response('down', { status: 502 });
  const socket = await worker.fetch(get('https://galeo.vixly.ltd/staff/_blazor', { Upgrade: 'websocket' }), env());
  assert.equal(socket.status, 502);
  const control = await worker.fetch(get('https://control.vixly.ltd/'), env());
  assert.equal(control.status, 502);
});

test('a broken store or a missing setting never closes a site', async () => {
  const broken = env({ VIXLY_EDGE: { get: async () => { throw new Error('kv'); } } });
  assert.equal((await worker.fetch(get('https://galeo.vixly.ltd/staff'), broken)).status, 200);
  assert.equal((await worker.fetch(get('https://galeo.vixly.ltd/staff'), env({ VIXLY_EDGE: undefined }))).status, 200);
});

// ----- The signed endpoints -----

test('a window list is refused without the right signature, too old, replayed or out of order', async () => {
  const now = Date.now();
  const data = await windowList(now - 60_000, now + 600_000, ['staff']);

  const wrongSecret = await worker.fetch(await signed('/__edge/maintenance', data, 'x'.repeat(40)), env());
  assert.equal(wrongSecret.status, 401);
  const brandSecret = await worker.fetch(await signed('/__edge/maintenance', data, BRAND_SECRET), env());
  assert.equal(brandSecret.status, 401);
  const old = await worker.fetch(await signed('/__edge/maintenance', data, MAINTENANCE_SECRET, now - 6 * 60_000), env());
  assert.equal(old.status, 401);
  const tampered = await signed('/__edge/maintenance', data, MAINTENANCE_SECRET, now);
  const changed = new Request(tampered.url, { method: 'POST', headers: tampered.headers, body: JSON.stringify({ windows: [] }) });
  assert.equal((await worker.fetch(changed, env())).status, 401);
  assert.equal(store.size, 0);

  const first = await signed('/__edge/maintenance', data, MAINTENANCE_SECRET, now);
  const replay = first.clone();
  assert.equal((await worker.fetch(first, env())).status, 200);
  assert.equal((await (await worker.fetch(replay, env())).json()).stale, true);

  // An older list arriving after a newer one changes nothing: the cancel wins.
  assert.equal((await publish({ windows: [] }, now + 1000)).status, 200);
  assert.equal((await (await publish(data, now + 500)).json()).stale, true);
  assert.equal((await worker.fetch(get('https://galeo.vixly.ltd/staff'), env())).status, 200);
});

test('the endpoints answer only on the edge host, by POST, when configured', async () => {
  assert.equal((await worker.fetch(get('https://api.vixly.ltd/__edge/maintenance'), env())).status, 405);
  assert.equal((await worker.fetch(get('https://api.vixly.ltd/__edge/other'), env())).status, 404);
  const unconfigured = await worker.fetch(await signed('/__edge/maintenance', { windows: [] }, 'short'), env({ EDGE_MAINTENANCE_SECRET: 'short' }));
  assert.equal(unconfigured.status, 503);
  assert.equal((await worker.fetch(get('https://api.vixly.ltd/maintenance'), env())).status, 200);
});

test('window lists and brands are checked before they are kept', async () => {
  const now = Date.now();
  assert.equal(readWindows({ windows: 'x' }), null);
  assert.equal(readWindows(await windowList(now, now - 1, ['staff'])), null);
  assert.equal(readWindows(await windowList(now, now + 1, ['control'])), null);
  assert.equal(readWindows(await windowList(now, now + 1, ['staff'], { passHash: 'plain-code' })), null);
  assert.equal(readWindows(await windowList(now, now + 1, ['staff']))[0].id, WINDOW_ID.replace(/-/g, ''));

  assert.equal(readBrand({ slug: 'Bad Slug', name: 'x' }), null);
  assert.equal(readBrand({ slug: 'galeo', name: '' }), null);
  assert.equal(readBrand({ slug: 'galeo', name: 'G', logo: 'PHN2Zz4=', logoType: 'image/svg+xml' }), null);
  assert.equal(readBrand({ slug: 'galeo', name: 'G', logo: 'A'.repeat(200_000), logoType: 'image/png' }), null);
  assert.equal(readBrand({ slug: 'galeo', name: 'G', colour: 'red' }).brand.colour, null);
  assert.equal(readBrand({ slug: 'galeo', deleted: true }).deleted, true);
});

test('a brand can be removed', async () => {
  await worker.fetch(await signed('/__edge/brand', { slug: 'galeo', name: 'Galeo' }, BRAND_SECRET, Date.now() - 10), env());
  assert.ok(store.has('brand:galeo'));
  await worker.fetch(await signed('/__edge/brand', { slug: 'galeo', deleted: true }, BRAND_SECRET), env());
  assert.ok(!store.has('brand:galeo'));
});

test('the page text escapes what IT and companies typed', async () => {
  const now = Date.now();
  const data = await windowList(now - 60_000, now + 600_000, ['companies']);
  data.windows[0].messages.en = '<script>alert(1)</script>';
  await publish(data);
  await worker.fetch(await signed('/__edge/brand', { slug: 'galeo', name: '<b>"G"</b>' }, BRAND_SECRET), env());
  forgetCache();
  const text = await (await worker.fetch(get('https://galeo.vixly.ltd/'), env())).text();
  assert.doesNotMatch(text, /<script>/);
  assert.match(text, /&lt;script&gt;/);
  assert.match(text, /&lt;b&gt;&quot;G&quot;&lt;\/b&gt;/);
});

test('the pass link: at the root or under the site, never a redirect elsewhere', () => {
  assert.deepEqual(passLink('/maintenance-pass/abc'), { home: '/', code: 'abc' });
  assert.deepEqual(passLink('/staff/maintenance-pass/abc/'), { home: '/staff/', code: 'abc' });
  assert.equal(passLink('/a/b/maintenance-pass/abc'), null);
  assert.equal(passLink('/\\evil.com/maintenance-pass/abc'), null);
  assert.equal(passLink('/staff/customers'), null);
});

// The same value is checked by the .NET tests (VixlyEdgeSignatureTests): the two sides sign alike.
test('the signature matches the sites\' signer', async () => {
  assert.equal(await hmacHex('k'.repeat(32), 'POST\n/__edge/brand\n1700000000000\n{"slug":"galeo"}'), 'bf9cb6c4634a9b5fe9e38df0c005f95c390c093a7b0a7433738ae32b55898f0d');
});

test('language: the browser first, then the company, then English', () => {
  assert.equal(pickLanguage('es-MX,es;q=0.9', 'fr'), 'es');
  assert.equal(pickLanguage('de-DE', 'fr'), 'fr');
  assert.equal(pickLanguage('', null), 'en');
});
