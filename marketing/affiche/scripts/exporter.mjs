// Exports : PDF (vectoriel, fond perdu inclus) + PNG haute définition, à partir des sources HTML.
// Usage : node scripts/exporter.mjs [format…]   (sans argument : tous les formats)
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import puppeteer from 'puppeteer-core';
import { EDGE, RACINE } from './config.mjs';

const MM = 96 / 25.4; // px CSS par millimètre
// largeur/hauteur en mm (pages imprimées, fond perdu compris) ou en px (web, réseaux)
const FORMATS = {
  a3: { source: 'a3.html', mm: [303, 426], dpi: 300, dossier: 'print', nom: 'yakkomom-affiche-a3' },
  a2: { source: 'a2.html', mm: [426, 600], dpi: 300, dossier: 'print', nom: 'yakkomom-affiche-a2' },
  // Échelle 1/10 : PNG de 6150 px de large (≈ 39 dpi à taille réelle, suffisant vu de loin ; le PDF est vectoriel)
  panneau: { source: 'panneau.html', mm: [410, 310], dpi: 381, dossier: 'panneau', nom: 'yakkomom-panneau-4x3m-echelle-1-10' },
  'banniere-web': { source: 'banniere-web.html', px: [1920, 800], dossier: 'web', nom: 'yakkomom-banniere-1920x800', webp: true },
  'banniere-mobile': { source: 'banniere-mobile.html', px: [1080, 1350], dossier: 'web', nom: 'yakkomom-banniere-mobile-1080x1350', webp: true },
  'social-carre': { source: 'social-carre.html', px: [1080, 1080], dossier: 'social', nom: 'yakkomom-carre-1080x1080' },
  'social-story': { source: 'social-story.html', px: [1080, 1920], dossier: 'social', nom: 'yakkomom-story-1080x1920' },
};

/** Emplacement du QR dans l'image exportée (pour le contrôle de lecture). */
async function enregistrerZoneQr(page, fichier, echelle) {
  const r = await page.$eval('.qr', e => { const b = e.getBoundingClientRect(); return [b.x, b.y, b.width, b.height]; });
  fs.writeFileSync(fichier, JSON.stringify(r.map(v => Math.round(v * echelle))));
}

const demandes = process.argv.slice(2);
const navigateur = await puppeteer.launch({ executablePath: EDGE, headless: true, args: ['--allow-file-access-from-files'] });
for (const [cle, f] of Object.entries(FORMATS)) {
  if (demandes.length && !demandes.includes(cle)) continue;
  const page = await navigateur.newPage();
  await page.emulateMediaFeatures([{ name: 'prefers-color-scheme', value: 'light' }]);
  const sortie = path.join(RACINE, 'exports', f.dossier);
  fs.mkdirSync(sortie, { recursive: true });
  const url = pathToFileURL(path.join(RACINE, 'sources', f.source)).href + '?export';

  if (f.mm) {
    const [l, h] = f.mm;
    const echelle = (l / 25.4 * f.dpi) / (l * MM); // ex. 300 dpi
    await page.setViewport({ width: Math.round(l * MM), height: Math.round(h * MM), deviceScaleFactor: echelle });
    await page.goto(url, { waitUntil: 'networkidle0' });
    await page.evaluate(() => document.fonts.ready);
    await page.pdf({ path: path.join(sortie, `${f.nom}.pdf`), width: `${l}mm`, height: `${h}mm`, printBackground: true, preferCSSPageSize: true });
    await page.screenshot({ path: path.join(sortie, `${f.nom}-${f.dpi}dpi.png`), type: 'png' });
    await enregistrerZoneQr(page, path.join(sortie, `${f.nom}-${f.dpi}dpi.qr.json`), echelle);
    // Aperçu léger pour validation à l'écran
    await page.setViewport({ width: Math.round(l * MM), height: Math.round(h * MM), deviceScaleFactor: 1 });
    await page.screenshot({ path: path.join(sortie, `${f.nom}-apercu.png`), type: 'png' });
  }
  if (f.px) {
    const [l, h] = f.px;
    await page.setViewport({ width: l, height: h, deviceScaleFactor: 1 });
    await page.goto(url, { waitUntil: 'networkidle0' });
    await page.evaluate(() => document.fonts.ready);
    await page.screenshot({ path: path.join(sortie, `${f.nom}.png`), type: 'png' });
    await enregistrerZoneQr(page, path.join(sortie, `${f.nom}.qr.json`), 1);
    if (f.webp) await page.screenshot({ path: path.join(sortie, `${f.nom}.webp`), type: 'webp', quality: 90 });
  }
  console.log(`${cle} → exports/${f.dossier}/${f.nom}.*`);
  await page.close();
}
await navigateur.close();
