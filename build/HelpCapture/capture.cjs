// Public help V2: capture only web-page pixels in an isolated headless browser.
// Never connect to the user's browser, profile, desktop or open windows.
const { chromium } = require('playwright');
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');

const root = path.resolve(__dirname, '../..');
const origin = 'https://localhost:7443';
const output = path.join(root, 'src/Public/Vixly.Public.Web/wwwroot/help/media/v2');
const inspectOnly = process.argv.includes('--inspect');
const languages = process.argv.includes('--en-only') ? ['en'] : ['en', 'fr', 'es'];
const onlyScene = process.argv.find(value => value.startsWith('--scene='))?.split('=')[1];
const scenes = [
  { id: 'overview', url: lang => `/${lang}`, marks: ['.pub-nav', '.pub-cta'] },
  { id: 'plans', url: lang => `/${lang}/plans`, marks: ['.pub-plan:first-child .pub-plan__facts', '.pub-plan:first-child a.vx-button'] },
  { id: 'company', url: lang => `/${lang}/register`, marks: ['form[method="post"] fieldset:nth-of-type(1)', 'form[method="post"] fieldset:nth-of-type(2)', 'form[method="post"] fieldset:nth-of-type(3)', '#reg-slug', 'input[name="Form.AcceptedTerms"]', 'form[method="post"] button[type="submit"]'] },
  { id: 'email', url: lang => `/__capture/${lang}/email`, marks: ['form button[type="submit"]', '#reg-new-email'] },
  { id: 'verify', url: lang => `/__capture/${lang}/verify`, marks: ['main form button[type="submit"]'] },
  { id: 'plan-choice', url: lang => `/__capture/${lang}/plan`, marks: ['.pub-plan-radio-group', '#reg-term', '#reg-licences', '#reg-promo', 'main form button[type="submit"]'] },
  { id: 'first-invoice', url: lang => `/__capture/${lang}/pay`, marks: ['main .vx-card', '#pay-card', 'main form button[type="submit"]'] },
  { id: 'workspace-ready', url: lang => `/__capture/${lang}/done`, marks: ['main h2', 'main .pub-lead'] },
  { id: 'invoice', url: lang => `/pay/help-demo?lang=${lang}`, marks: ['main .vx-card', '#pay-card', 'main form button[type="submit"]'] },
  { id: 'save-card', url: lang => `/cards/help-save-card?lang=${lang}`, marks: ['#pay-card', 'main form button[type="submit"]'] },
  { id: 'zelle', url: lang => `/pay/help-demo?lang=${lang}`, region: 'section[aria-labelledby="pay-zelle"]', marks: ['section[aria-labelledby="pay-zelle"] dl', '#zelle-amount', '#zelle-sent', '#zelle-sender', '#zelle-reference', 'section[aria-labelledby="pay-zelle"] button[type="submit"]'] },
  { id: 'legal', url: lang => `/${lang}/legal`, marks: ['main .vx-card a', 'main .pub-list'] },
  { id: 'contact', url: lang => `/${lang}/contact`, marks: ['#contact-company', '#contact-email', '#contact-message', 'main form button[type="submit"]'] },
  { id: 'offer', url: lang => `/${lang}/register?offer=help-offer`, marks: ['.pub-offer .vx-facts', '.pub-offer__email', '#reg-oemail', 'main form button[type="submit"]'] }
];

async function overlay(page, selectors) {
  const targets = [];
  for (const selector of selectors) {
    const element = page.locator(selector).first();
    await element.waitFor({ state: 'visible' });
    const box = await element.boundingBox();
    if (!box) throw new Error(`No visible target: ${selector}`);
    targets.push({ selector, ...box });
  }
  // The overlay annotates real controls. It never replaces text or changes form state.
  await page.evaluate(targets => {
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    const width = document.documentElement.scrollWidth;
    const height = document.documentElement.scrollHeight;
    svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
    svg.setAttribute('width', width);
    svg.setAttribute('height', height);
    svg.style.cssText = 'position:absolute;left:0;top:0;pointer-events:none;z-index:2147483647';
    svg.setAttribute('aria-hidden', 'true');
    let drawing = '<defs><marker id="help-arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0,0 L6,3 L0,6" fill="#be123c"/></marker></defs>';
    targets.forEach((box, index) => {
      const x = box.x + window.scrollX;
      const y = box.y + window.scrollY;
      const fromLeft = x >= 64;
      const cx = fromLeft ? x - 34 : x + 24;
      const cy = fromLeft ? y + Math.min(box.height / 2, 40) : Math.max(22, y - 27);
      const tx = fromLeft ? x - 6 : cx;
      const ty = fromLeft ? cy : y - 6;
      drawing += `<rect x="${x-5}" y="${y-5}" width="${box.width+10}" height="${box.height+10}" rx="7" fill="none" stroke="#be123c" stroke-width="3"/>`;
      drawing += `<path d="M${cx},${cy} L${tx},${ty}" stroke="#be123c" stroke-width="3" fill="none" marker-end="url(#help-arrow)"/>`;
      drawing += `<circle cx="${cx}" cy="${cy}" r="16" fill="#be123c" stroke="white" stroke-width="2"/><text x="${cx}" y="${cy+5}" fill="white" text-anchor="middle" font-size="15" font-weight="700" font-family="Arial,sans-serif">${index+1}</text>`;
    });
    svg.innerHTML = drawing;
    document.body.append(svg);
  }, targets);
  return targets.map(({ selector }) => selector);
}

(async () => {
  await fs.mkdir(output, { recursive: true });
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const manifest = { version: 2, captured: '2026-10-25', capture: 'headless browser document only', fixture: 'build/HelpCapture', viewport: { width: 1280, height: 960 }, images: [] };
  if (onlyScene) manifest.images = JSON.parse(await fs.readFile(path.join(output, 'manifest.json'), 'utf8')).images;
  try {
    for (const language of languages) {
      const context = await browser.newContext({ viewport: manifest.viewport, deviceScaleFactor: 1, locale: language, timezoneId: 'UTC' });
      await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort());
      const page = await context.newPage();
      for (const scene of scenes.filter(item => !onlyScene || item.id === onlyScene)) {
        await page.setViewportSize(manifest.viewport);
        const response = await page.goto(origin + scene.url(language), { waitUntil: 'networkidle' });
        if (response.status() !== 200) throw new Error(`${language}/${scene.id}: HTTP ${response.status()}`);
        await page.evaluate(() => document.fonts.ready);
        if (await page.locator('html').getAttribute('lang') !== language) throw new Error(`${language}/${scene.id}: wrong page language`);
        if (await page.locator('[role="alert"]').count()) throw new Error(`${language}/${scene.id}: application error shown`);
        if (inspectOnly) {
          console.log(JSON.stringify({ language, scene: scene.id, title: await page.title(), inputs: await page.locator('main input, main select, main button, main fieldset').evaluateAll(nodes => nodes.map(node => ({ tag: node.tagName, id: node.id, name: node.name, type: node.type, class: node.className, text: node.tagName === 'BUTTON' ? node.textContent.trim() : undefined }))) }));
          if (scene.id === 'company') await page.screenshot({ path: path.join(output, `${language}-preview.png`), fullPage: true });
          continue;
        }
        if (scene.region) {
          const height = await page.evaluate(() => document.documentElement.scrollHeight);
          await page.setViewportSize({ width: manifest.viewport.width, height: Math.max(manifest.viewport.height, height) });
        }
        const marked = await overlay(page, scene.marks);
        const file = `${language}-${scene.id}.png`;
        let clip;
        if (scene.region) {
          const bounds = await page.locator(scene.region).boundingBox();
          const size = await page.evaluate(() => ({ width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight }));
          const x = Math.max(0, bounds.x - 64);
          const y = Math.max(0, bounds.y - 20);
          clip = { x, y, width: Math.min(size.width - x, bounds.width + 96), height: Math.min(size.height - y, bounds.height + 40) };
        }
        const bytes = await page.screenshot({ path: path.join(output, file), ...(clip ? { clip } : { fullPage: true }), animations: 'disabled' });
        manifest.images = manifest.images.filter(item => item.file !== file);
        manifest.images.push({ file, language, scene: scene.id, page: scene.url(language).replace(/\?.*$/, ''), viewport: page.viewportSize(), marked, sha256: crypto.createHash('sha256').update(bytes).digest('hex') });
        console.log(`Captured page only: ${language}/${scene.id}`);
      }
      await context.close();
    }
  } finally { await browser.close(); }
  if (!inspectOnly) await fs.writeFile(path.join(output, 'manifest.json'), JSON.stringify(manifest, null, 2) + '\n');
})().catch(error => { console.error(error.message); process.exitCode = 1; });
