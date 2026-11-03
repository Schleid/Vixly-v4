const {chromium}=require('playwright');
const fs=require('node:fs/promises');
const path=require('node:path');
const crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
const origin='https://localhost:7444';
const output=path.join(root,'src/Customer/Vixly.Customer.Web/wwwroot/help/media/v2');
const scenes=[
 {id:'mailbox',url:'/customer/',marks:['.customer-portal__code','.customer-portal__mailbox']},
 {id:'announce',url:'/customer/packages/new',marks:['.customer-prealert-form .customer-field-duo:first-of-type','input[name="tracking"]','textarea[name="contents"]','.customer-prealert-form button[type="submit"]']},
 {id:'shipment',url:'/customer/packages/shipments/new',marks:['.customer-destination-fieldset','input[name="parcelCount"]','.customer-recipient-fieldset','.customer-options-grid','.customer-packages-choice-grid','.customer-shipment-form button[type="submit"]']}
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


(async()=>{
 await fs.mkdir(output,{recursive:true});
 const browser=await chromium.launch({channel:'msedge',headless:true});
 const manifest={version:2,captured:'2026-10-27',capture:'browser document only',fixture:'build/CustomerHelpCapture',viewport:{width:1280,height:960},images:[]};
 try{
  for(const language of ['en','fr','es']){
   const context=await browser.newContext({viewport:manifest.viewport,locale:language,timezoneId:'UTC'});
   await context.route('**/*',route=>new URL(route.request().url()).origin===origin?route.continue():route.abort());
   await context.addCookies([{name:'__Secure-vx4-customer',value:'help-demo',url:origin+'/customer/',secure:true},{name:'__Secure-vx4-customer-lang',value:`c=${language}|uic=${language}`,url:origin+'/customer/',secure:true}]);
   const page=await context.newPage();
   for(const scene of scenes){
    const response=await page.goto(origin+scene.url,{waitUntil:'networkidle'});
    if(response.status()!==200)throw new Error(`${language}/${scene.id}: ${response.status()}`);
    if(await page.locator('html').getAttribute('lang')!==language)throw new Error('Wrong language');
    if(scene.id==='announce'){
     await page.locator('input[name="carrier"]').fill('Demo carrier');await page.locator('input[name="store"]').fill('Demo store');
     await page.locator('input[name="tracking"]').fill('DEMO-TRACK-001');await page.locator('textarea[name="contents"]').fill('2 cotton shirts');await page.locator('input[name="value"]').fill('35');
    }
    if(scene.id==='shipment'){await page.locator('input[name="parcelCount"]').fill('1');await page.locator('input[name="preAlerts"]').first().check();}
    const marked=await overlay(page,scene.marks);
    const file=`${language}-${scene.id}.png`;
    const bytes=await page.screenshot({path:path.join(output,file),fullPage:true,animations:'disabled'});
    manifest.images.push({file,language,scene:scene.id,page:scene.url,marked,sha256:crypto.createHash('sha256').update(bytes).digest('hex')});
    console.log('Captured browser page only: '+file);
   }
   await context.close();
  }
 }finally{await browser.close();}
 await fs.writeFile(path.join(output,'manifest.json'),JSON.stringify(manifest,null,2));
})().catch(error=>{console.error(error.message);process.exitCode=1;});