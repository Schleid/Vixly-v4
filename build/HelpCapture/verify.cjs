// Browser verification for localized Public V2 help. Uses a fresh, headless profile.
const { chromium } = require('playwright');
const fs = require('node:fs/promises');
const path = require('node:path');
const origin = 'https://localhost:7443';
const proof = process.env.HELP_PROOF_DIRECTORY;

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const checks = [];
  try {
    for (const language of ['en', 'fr', 'es']) {
      for (const width of [1280, 375]) {
        const context = await browser.newContext({ viewport: { width, height: 960 }, locale: language });
        await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort());
        const page = await context.newPage();
        const home = await page.goto(`${origin}/${language}/help`, { waitUntil: 'networkidle' });
        if (home.status() !== 200) throw new Error('Help home did not open');
        const links = await page.locator('main .pub-help-list a').evaluateAll(nodes => [...new Set(nodes.map(node => node.getAttribute('href')))]);
        if (links.length !== 16) throw new Error(`Expected 16 articles, found ${links.length}`);
        for (const link of links) {
          const response = await page.goto(origin + link, { waitUntil: 'networkidle' });
          if (response.status() !== 200) throw new Error(`${link}: HTTP ${response.status()}`);
          const state = await page.locator('article').evaluate(article => ({
            title: article.querySelector('h1').textContent,
            documentWidth: document.documentElement.scrollWidth,
            viewportWidth: window.innerWidth,
            images: [...article.querySelectorAll('img')].map(image => ({ src: image.getAttribute('src'), alt: image.alt, complete: image.complete, width: image.naturalWidth, displayedWidth: image.getBoundingClientRect().width }))
          }));
          if (state.documentWidth > state.viewportWidth) throw new Error(`${link} at ${width}: horizontal overflow`);
          if (!state.images.length || state.images.some(image => !image.complete || !image.width || !image.alt || !image.src.startsWith(`/help/media/v2/${language}-`) || image.displayedWidth > width))
            throw new Error(`${link}: missing, broken or overflowing localized image`);
          checks.push({ language, width, link, images: state.images.length, passed: true });
          if (proof && link.endsWith('/company-details') && ((language === 'en' && width === 1280) || (language === 'fr' && width === 375))) {
            await fs.mkdir(proof, { recursive: true });
            await page.screenshot({ path: path.join(proof, `public-help-v2-${language}-${width}.png`), fullPage: true });
          }
        }
        await context.close();
        console.log(`Verified ${language}, ${width}px: all 16 articles and their localized images`);
      }
    }
  } finally { await browser.close(); }
  await fs.writeFile(path.join(__dirname, 'browser-verification.json'), JSON.stringify({ checked: '2026-10-25', checks }, null, 2) + '\n');
})().catch(error => { console.error(error.message); process.exitCode = 1; });
