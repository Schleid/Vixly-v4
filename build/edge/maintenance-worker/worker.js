// =========================================================
// FILE: worker.js
//
// PURPOSE:
// The Cloudflare Worker in front of the V4 sites (design: docs/platform/MAINTENANCE-WORKER-DESIGN.md,
// owner 2026-10-23 and 2026-10-28).
//
// RESPONSIBILITIES:
// - During a maintenance window published in Control, answer with the maintenance page without contacting the servers
// - Let a person with the window's pass link through (a cookie signed by the Worker)
// - When a server fails (502, 503, 504, Cloudflare 52x), answer with the same page and a general message
// - Receive the window list (from Control) and company brands (from Operations), signed, at {EDGE_HOST}/__edge/*
//
// IMPORTANT NOTES:
// - Everything else goes to the server untouched: the Worker never changes a request
// - Any fault of the Worker itself passes the request through (fail open); set the route to "Fail open" too
// - No Cloudflare API token anywhere: only this Worker writes its KV store
// - A site's own maintenance answer carries X-Vixly-Maintenance and passes through
// - Company pages never say "Vixly" (ADR-0012 W-1); the public site shows Vixly's name
//
// SETTINGS (Cloudflare dashboard → the Worker → Settings):
// - KV binding   VIXLY_EDGE               the Worker's store
// - Variable     BASE_DOMAIN              vixly.ltd
// - Variable     EDGE_HOST                api.vixly.ltd (where Control and Operations send)
// - Secret       EDGE_MAINTENANCE_SECRET  the same value as Control's Edge__MaintenanceSecret
// - Secret       EDGE_BRAND_SECRET        the same value as Operations Web's Edge__BrandSecret
// =========================================================

export const SITES = ['public', 'companies', 'customers', 'staff'];
export const LANGUAGES = ['en', 'fr', 'es'];
export const PASS_PATH = '/maintenance-pass/';
export const PASS_COOKIE = '__Host-vx-edge-pass';
export const SITE_MARKER = 'X-Vixly-Maintenance';
export const MAX_AGE_MS = 5 * 60 * 1000;
export const CACHE_MS = 30 * 1000;
export const FAILURES = [502, 503, 504, 520, 521, 522, 523, 524, 525, 526, 527, 530];

const ALWAYS_OPEN = ['/health', '/css', '/js', '/lib', '/img', '/_content', '/_framework', '/favicon.ico'];
const PROGRAM_PATHS = ['/agent', '/api', '/internal'];
const STAFF_PATHS = ['/staff', '/agent', '/maps'];
const NEVER_CLOSED = ['control', 'api', 'ops-internal'];
const SLUG = /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/;
const GUID = /^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}$/i;
const HEX64 = /^[0-9a-f]{64}$/;
const COLOUR = /^#[0-9a-fA-F]{6}$/;
const LOGO_TYPES = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_LOGO_BYTES = 100 * 1024;
const MAX_MAINTENANCE_BODY = 64 * 1024;
const MAX_BRAND_BODY = 200 * 1024;
const MAX_WINDOWS = 50;
const MAX_MESSAGE = 500;
const MAX_NAME = 200;

const WORDS = {
  en: {
    title: 'Under maintenance',
    planned: 'We are doing planned maintenance. We expect to be back by {time} (UTC).',
    failure: 'This site is not available right now. Please try again in a few minutes.',
    json: 'Under maintenance. Try again later.'
  },
  fr: {
    title: 'En maintenance',
    planned: 'Nous effectuons une maintenance planifiée. Retour prévu vers {time} (UTC).',
    failure: 'Ce site n’est pas disponible pour le moment. Veuillez réessayer dans quelques minutes.',
    json: 'En maintenance. Réessayez plus tard.'
  },
  es: {
    title: 'En mantenimiento',
    planned: 'Estamos realizando un mantenimiento planificado. Esperamos volver hacia las {time} (UTC).',
    failure: 'Este sitio no está disponible en este momento. Vuelva a intentarlo en unos minutos.',
    json: 'En mantenimiento. Inténtelo más tarde.'
  }
};

// Per Worker instance: the copy of the windows and brands, kept 30 seconds.
const memory = { windows: null, windowsAt: 0, brands: new Map() };

export function forgetCache() {
  memory.windows = null;
  memory.windowsAt = 0;
  memory.brands.clear();
}

export default {
  async fetch(request, env, ctx) {
    let route;
    try {
      route = describe(request, env);
    } catch {
      return fetch(request);
    }

    try {
      if (route.edge) {
        return await edge(request, env, route);
      }

      if (!route.site || route.upgrade) {
        return fetch(request);
      }

      return await guarded(request, env, route);
    } catch {
      // The Worker never turns its own fault into an outage. Once the request has gone, the visitor sees the general page.
      return route.sent ? page(request, env, route, null) : fetch(request);
    }
  }
};

// =====================================================
// Which site a request belongs to
// =====================================================
export function describe(request, env) {
  const url = new URL(request.url);
  const host = url.hostname.toLowerCase();
  const base = String(env.BASE_DOMAIN || '').trim().toLowerCase();
  const edgeHost = String(env.EDGE_HOST || '').trim().toLowerCase();
  const path = url.pathname;
  const upgrade = (request.headers.get('Upgrade') || '').toLowerCase() === 'websocket';
  const route = { url, host, path, upgrade, site: null, slug: null, inner: path, edge: false, sent: false };

  if (edgeHost && host === edgeHost && (path === '/__edge' || path.startsWith('/__edge/'))) {
    route.edge = true;
    return route;
  }

  if (!base) {
    return route;
  }

  if (host === base || host === 'www.' + base) {
    route.site = 'public';
  } else if (host.endsWith('.' + base)) {
    const label = host.slice(0, -(base.length + 1));

    if (label.includes('.') || !SLUG.test(label) || NEVER_CLOSED.includes(label) || host === edgeHost) {
      return route;
    }

    route.slug = label;

    if (startsWith(path, '/customer')) {
      route.site = 'customers';
      route.inner = path.slice('/customer'.length) || '/';
    } else {
      route.site = STAFF_PATHS.some(prefix => startsWith(path, prefix)) ? 'staff' : 'companies';
    }
  } else {
    return route;
  }

  if (ALWAYS_OPEN.some(prefix => startsWith(route.inner, prefix))) {
    route.site = null;
  }

  return route;
}

function startsWith(path, prefix) {
  const lower = path.toLowerCase();
  return lower === prefix || lower.startsWith(prefix + '/');
}

// =====================================================
// A request to a site that can be closed
// =====================================================
async function guarded(request, env, route) {
  const now = Date.now();
  const running = (await windows(env)).find(window => window.sites.includes(route.site) && window.starts <= now && now < window.ends) || null;
  const link = passLink(route.inner);

  if (link && running) {
    return pass(request, env, route, running, link);
  }

  if (running && !(await hasPass(request, env, running, now))) {
    return page(request, env, route, running);
  }

  route.sent = true;
  let response;

  try {
    response = await fetch(request);
  } catch {
    return page(request, env, route, null);
  }

  if (FAILURES.includes(response.status) && !response.headers.has(SITE_MARKER)) {
    return page(request, env, route, null);
  }

  return response;
}

// =====================================================
// The pass link: /maintenance-pass/{code}, or under the site's first segment (/staff/maintenance-pass/{code}), as on the sites
// The site's own pass cookie is set when the request reaches it; both are needed while both layers close.
// =====================================================
export function passLink(path) {
  const match = /^(\/[A-Za-z0-9_-]+)?\/maintenance-pass\/([^/]*)\/?$/i.exec(path);
  return match ? { home: (match[1] || '') + '/', code: decodeURIComponent(match[2]) } : null;
}

async function pass(request, env, route, window, link) {
  const code = link.code;

  if (!code || code.length > 200 || (await sha256Hex(code)) !== window.passHash) {
    return new Response('Not found', { status: 404, headers: { 'Cache-Control': 'no-store' } });
  }

  const cookie = await passCookie(env, window);
  route.sent = true;
  let response;

  try {
    response = await fetch(request);
  } catch {
    response = null;
  }

  if (!response || (FAILURES.includes(response.status) && !response.headers.has(SITE_MARKER))) {
    const home = (route.site === 'customers' ? '/customer' : '') + link.home;
    response = new Response(null, { status: 302, headers: { Location: home, 'Cache-Control': 'no-store' } });
  } else {
    response = new Response(response.body, response);
  }

  response.headers.append('Set-Cookie', cookie);
  return response;
}

export async function passCookie(env, window) {
  const value = window.id + '.' + window.ends;
  const signature = await hmacHex(await cookieKey(env), value);
  const maxAge = Math.max(1, Math.floor((window.ends - Date.now()) / 1000));
  return `${PASS_COOKIE}=${value}.${signature}; Path=/; Max-Age=${maxAge}; Secure; HttpOnly; SameSite=Lax`;
}

async function hasPass(request, env, window, now) {
  const value = readCookie(request.headers.get('Cookie') || '', PASS_COOKIE);

  if (!value) {
    return false;
  }

  const parts = value.split('.');

  if (parts.length !== 3 || parts[0] !== window.id) {
    return false;
  }

  const ends = Number(parts[1]);

  if (!Number.isFinite(ends) || ends <= now) {
    return false;
  }

  return equal(await hmacHex(await cookieKey(env), parts[0] + '.' + parts[1]), parts[2]);
}

function readCookie(header, name) {
  for (const part of header.split(';')) {
    const index = part.indexOf('=');

    if (index > 0 && part.slice(0, index).trim() === name) {
      return part.slice(index + 1).trim();
    }
  }

  return null;
}

// The cookie key is derived from the maintenance secret, so there is one secret less to keep.
async function cookieKey(env) {
  return hmacHex(String(env.EDGE_MAINTENANCE_SECRET || '').trim(), 'vixly-edge-pass-cookie');
}

// =====================================================
// The page (or a short JSON answer for programs)
// =====================================================
async function page(request, env, route, window) {
  const now = Date.now();
  const retryAfter = window ? Math.max(60, Math.ceil((window.ends - now) / 1000)) : 120;
  const brand = route.slug ? await brandOf(env, route.slug) : null;
  const language = pickLanguage(request.headers.get('Accept-Language'), brand && brand.language);
  const words = WORDS[language];
  const headers = {
    'Cache-Control': 'no-store',
    'Retry-After': String(retryAfter),
    'X-Robots-Tag': 'noindex'
  };

  if (isProgram(request, route)) {
    headers['Content-Type'] = 'application/json; charset=utf-8';
    return new Response(JSON.stringify({ error: 'maintenance', message: words.json, retryAfter }), { status: 503, headers });
  }

  const title = words.title;
  const body = window ? words.planned.replace('{time}', utcTime(window.ends)) : words.failure;
  const message = window ? messageOf(window, language) : null;
  headers['Content-Type'] = 'text/html; charset=utf-8';
  headers['Content-Security-Policy'] = "default-src 'none'; style-src 'unsafe-inline'; img-src data:; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
  return new Response(html(language, title, body, message, route.site === 'public' ? { name: 'Vixly' } : brand), { status: 503, headers });
}

export function isProgram(request, route) {
  if (PROGRAM_PATHS.some(prefix => startsWith(route.inner, prefix))) {
    return true;
  }

  const accept = (request.headers.get('Accept') || '').toLowerCase();
  return accept.includes('application/json') && !accept.includes('text/html');
}

export function pickLanguage(acceptLanguage, fallback) {
  for (const part of String(acceptLanguage || '').split(',')) {
    const code = part.split(';')[0].split('-')[0].trim().toLowerCase();

    if (LANGUAGES.includes(code)) {
      return code;
    }
  }

  return LANGUAGES.includes(fallback) ? fallback : 'en';
}

function messageOf(window, language) {
  const messages = window.messages || {};
  return messages[language] || messages.en || null;
}

export function utcTime(ms) {
  const date = new Date(ms);
  const two = value => String(value).padStart(2, '0');
  return `${date.getUTCFullYear()}-${two(date.getUTCMonth() + 1)}-${two(date.getUTCDate())} ${two(date.getUTCHours())}:${two(date.getUTCMinutes())}`;
}

export function html(language, title, body, message, brand) {
  const colour = brand && brand.colour && COLOUR.test(brand.colour) ? brand.colour : '#1d4ed8';
  const logo = brand && brand.logo ? `<img class="logo" src="data:${brand.logoType};base64,${brand.logo}" alt="${escape(brand.name || '')}">` : '';
  const name = brand && brand.name && !logo ? `<p class="name">${escape(brand.name)}</p>` : '';
  const extra = message ? `<p>${escape(message)}</p>` : '';
  return `<!DOCTYPE html><html lang="${language}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">`
    + `<meta name="robots" content="noindex"><title>${escape(title)}</title><style>`
    + `:root{--accent:${colour};color-scheme:light dark}body{margin:0;min-height:100vh;display:grid;place-items:center;font-family:system-ui,-apple-system,"Segoe UI",Roboto,sans-serif;background:#eef3f8;color:#0f172a}`
    + `main{max-width:34rem;margin:1rem;padding:2rem;background:#fff;border-radius:12px;border-top:4px solid var(--accent);box-shadow:0 1px 3px rgba(15,23,42,.12)}`
    + `h1{margin:0 0 .75rem;font-size:1.6rem}p{line-height:1.5;margin:.5rem 0}.logo{display:block;max-width:220px;max-height:72px;margin-bottom:1.25rem}.name{font-weight:600;color:var(--accent);margin-bottom:1rem}`
    + `@media (prefers-color-scheme:dark){body{background:#0b1220;color:#e2e8f0}main{background:#111a2e}}`
    + `</style></head><body><main>${logo}${name}<h1>${escape(title)}</h1><p>${escape(body)}</p>${extra}</main></body></html>`;
}

function escape(text) {
  return String(text).replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
}

// =====================================================
// The store
// =====================================================
async function windows(env) {
  const now = Date.now();

  if (memory.windows && now - memory.windowsAt < CACHE_MS) {
    return memory.windows;
  }

  let list = [];

  try {
    const stored = env.VIXLY_EDGE ? await env.VIXLY_EDGE.get('windows', { type: 'json' }) : null;
    list = stored && Array.isArray(stored.windows) ? stored.windows : [];
  } catch {
    // KV unavailable: keep the last copy, or nothing closed. A site is never closed by mistake.
    list = memory.windows || [];
  }

  memory.windows = list;
  memory.windowsAt = now;
  return list;
}

async function brandOf(env, slug) {
  const cached = memory.brands.get(slug);
  const now = Date.now();

  if (cached && now - cached.at < CACHE_MS) {
    return cached.brand;
  }

  let brand = null;

  try {
    brand = env.VIXLY_EDGE ? await env.VIXLY_EDGE.get('brand:' + slug, { type: 'json' }) : null;
  } catch {
    brand = cached ? cached.brand : null;
  }

  memory.brands.set(slug, { brand, at: now });
  return brand;
}

// =====================================================
// The signed endpoints: {EDGE_HOST}/__edge/maintenance and /__edge/brand
// Signature: hex HMAC-SHA256 of "POST\n{path}\n{time}\n{body}", time in Unix milliseconds.
// A message older than 5 minutes, or not newer than the last one accepted, changes nothing.
// =====================================================
async function edge(request, env, route) {
  const kinds = {
    '/__edge/maintenance': { secret: env.EDGE_MAINTENANCE_SECRET, max: MAX_MAINTENANCE_BODY, apply: storeWindows },
    '/__edge/brand': { secret: env.EDGE_BRAND_SECRET, max: MAX_BRAND_BODY, apply: storeBrand }
  };
  const kind = kinds[route.path];

  if (!kind) {
    return answer(404, { error: 'not_found' });
  }

  if (request.method !== 'POST') {
    return answer(405, { error: 'method' });
  }

  const secret = String(kind.secret || '').trim();

  if (secret.length < 32 || !env.VIXLY_EDGE) {
    return answer(503, { error: 'not_configured' });
  }

  const time = Number(request.headers.get('X-Vixly-Edge-Time'));
  const signature = (request.headers.get('X-Vixly-Edge-Signature') || '').toLowerCase();

  if (!Number.isSafeInteger(time) || Math.abs(Date.now() - time) > MAX_AGE_MS || !/^[0-9a-f]{64}$/.test(signature)) {
    return answer(401, { error: 'signature' });
  }

  const declared = Number(request.headers.get('Content-Length') || '0');

  if (declared > kind.max) {
    return answer(413, { error: 'too_large' });
  }

  const body = await request.text();

  if (body.length > kind.max) {
    return answer(413, { error: 'too_large' });
  }

  if (!equal(await hmacHex(secret, `POST\n${route.path}\n${time}\n${body}`), signature)) {
    return answer(401, { error: 'signature' });
  }

  let data;

  try {
    data = JSON.parse(body);
  } catch {
    return answer(400, { error: 'json' });
  }

  return kind.apply(env, data, time);
}

export function readWindows(data) {
  if (!data || !Array.isArray(data.windows) || data.windows.length > MAX_WINDOWS) {
    return null;
  }

  const list = [];

  for (const item of data.windows) {
    const starts = Date.parse(item && item.startsUtc);
    const ends = Date.parse(item && item.endsUtc);
    const sites = Array.isArray(item && item.sites) ? item.sites.filter(site => SITES.includes(site)) : [];

    if (!item || typeof item.id !== 'string' || !GUID.test(item.id) || !Number.isFinite(starts) || !Number.isFinite(ends) || ends <= starts
      || sites.length === 0 || typeof item.passHash !== 'string' || !HEX64.test(item.passHash)) {
      return null;
    }

    const messages = {};

    for (const language of LANGUAGES) {
      const text = item.messages && item.messages[language];

      if (typeof text === 'string' && text.trim()) {
        messages[language] = text.trim().slice(0, MAX_MESSAGE);
      }
    }

    list.push({ id: item.id.replace(/-/g, '').toLowerCase(), starts, ends, sites, messages, passHash: item.passHash });
  }

  return list;
}

async function storeWindows(env, data, time) {
  const list = readWindows(data);

  if (!list) {
    return answer(400, { error: 'windows' });
  }

  const stored = await env.VIXLY_EDGE.get('windows', { type: 'json' });

  if (stored && Number(stored.time) >= time) {
    return answer(200, { ok: true, stale: true });
  }

  // Finished windows are dropped; KV forgets the record a day after the last window ends.
  const now = Date.now();
  const open = list.filter(window => window.ends > now);
  const last = open.reduce((latest, window) => Math.max(latest, window.ends), now);
  await env.VIXLY_EDGE.put('windows', JSON.stringify({ time, windows: open }), { expirationTtl: Math.max(86400, Math.ceil((last - now) / 1000) + 86400) });
  memory.windows = open;
  memory.windowsAt = now;
  return answer(200, { ok: true, windows: open.length });
}

export function readBrand(data) {
  if (!data || typeof data.slug !== 'string' || !SLUG.test(data.slug)) {
    return null;
  }

  if (data.deleted === true) {
    return { slug: data.slug, deleted: true };
  }

  if (typeof data.name !== 'string' || !data.name.trim() || data.name.length > MAX_NAME) {
    return null;
  }

  const brand = { name: data.name.trim(), colour: null, language: 'en', logo: null, logoType: null };

  if (typeof data.colour === 'string' && COLOUR.test(data.colour)) {
    brand.colour = data.colour;
  }

  if (LANGUAGES.includes(data.language)) {
    brand.language = data.language;
  }

  if (data.logo != null) {
    if (typeof data.logo !== 'string' || !LOGO_TYPES.includes(data.logoType) || !/^[A-Za-z0-9+/]+={0,2}$/.test(data.logo)
      || base64Bytes(data.logo) > MAX_LOGO_BYTES) {
      return null;
    }

    brand.logo = data.logo;
    brand.logoType = data.logoType;
  }

  return { slug: data.slug, brand };
}

function base64Bytes(text) {
  const padding = text.endsWith('==') ? 2 : text.endsWith('=') ? 1 : 0;
  return Math.floor(text.length * 3 / 4) - padding;
}

async function storeBrand(env, data, time) {
  const read = readBrand(data);

  if (!read) {
    return answer(400, { error: 'brand' });
  }

  const key = 'brand:' + read.slug;
  const stored = await env.VIXLY_EDGE.get(key, { type: 'json' });

  if (stored && Number(stored.time) >= time) {
    return answer(200, { ok: true, stale: true });
  }

  if (read.deleted) {
    await env.VIXLY_EDGE.delete(key);
    memory.brands.delete(read.slug);
    return answer(200, { ok: true, deleted: true });
  }

  const record = { time, ...read.brand };
  await env.VIXLY_EDGE.put(key, JSON.stringify(record));
  memory.brands.set(read.slug, { brand: record, at: Date.now() });
  return answer(200, { ok: true });
}

function answer(status, data) {
  return new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' } });
}

// =====================================================
// Crypto
// =====================================================
const encoder = new TextEncoder();

export async function hmacHex(secret, text) {
  const key = await crypto.subtle.importKey('raw', encoder.encode(secret), { name: 'HMAC', hash: 'SHA-256' }, false, ['sign']);
  return hex(await crypto.subtle.sign('HMAC', key, encoder.encode(text)));
}

export async function sha256Hex(text) {
  return hex(await crypto.subtle.digest('SHA-256', encoder.encode(text)));
}

function hex(buffer) {
  return [...new Uint8Array(buffer)].map(byte => byte.toString(16).padStart(2, '0')).join('');
}

// Compares in constant time for strings of the same length.
function equal(a, b) {
  if (typeof a !== 'string' || typeof b !== 'string' || a.length !== b.length) {
    return false;
  }

  let difference = 0;

  for (let index = 0; index < a.length; index++) {
    difference |= a.charCodeAt(index) ^ b.charCodeAt(index);
  }

  return difference === 0;
}
