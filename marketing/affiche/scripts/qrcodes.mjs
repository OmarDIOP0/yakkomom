// QR codes vectoriels aux couleurs de l'app : modules arrondis, yeux personnalisés, logo au centre.
// Correction d'erreur H (30 %) obligatoire avec un logo ; zone de silence de 4 modules.
// Chaque QR est ensuite décodé (jsQR) à plusieurs tailles pour vérifier qu'il se lit.
import fs from 'node:fs';
import path from 'node:path';
import QRCode from 'qrcode';
import jsQR from 'jsqr';
import { PNG } from 'pngjs';
import puppeteer from 'puppeteer-core';
import { SITE, SUPPORTS, COULEURS, EDGE, RACINE as racine } from './config.mjs';
const dossier = path.join(racine, 'qrcodes');
const logo = fs.readFileSync(path.join(racine, '../../Yakkomom/wwwroot/img/logo.svg'), 'utf8')
  .replace(/<svg[^>]*>/, '').replace('</svg>', '');

export function svgQr(texte) {
  const qr = QRCode.create(texte, { errorCorrectionLevel: 'H' });
  const n = qr.modules.size, m = qr.modules.data, marge = 4, total = n + marge * 2;
  const oeil = (x, y) => x < 7 && y < 7 || x >= n - 7 && y < 7 || x < 7 && y >= n - 7;
  // Zone du logo : carré central d'environ 22 % du côté (bien en deçà des 30 % récupérables en niveau H)
  let cote = Math.round(n * 0.22); if (cote % 2 === 0) cote++;
  const debut = (n - cote) / 2;
  const sousLogo = (x, y) => x >= debut - 0.5 && x < debut + cote + 0.5 && y >= debut - 0.5 && y < debut + cote + 0.5;

  // Style « fluide » : modules jointifs (aucun espace, lecture fiable), seuls les coins extérieurs sont arrondis.
  const plein = (x, y) => x >= 0 && y >= 0 && x < n && y < n && m[y * n + x] && !oeil(x, y) && !sousLogo(x, y);
  const r = 0.5;
  const points = [];
  for (let y = 0; y < n; y++) for (let x = 0; x < n; x++) {
    if (!plein(x, y)) continue;
    const X = x + marge, Y = y + marge;
    const h = plein(x, y - 1), b = plein(x, y + 1), g = plein(x - 1, y), d = plein(x + 1, y);
    // Rayon par coin : arrondi seulement si les deux côtés adjacents sont vides
    const [hg, hd, bd, bg] = [!h && !g, !h && !d, !b && !d, !b && !g].map(v => (v ? r : 0));
    points.push(`<path d="M${X + hg},${Y}H${X + 1 - hd}${hd ? `A${hd},${hd} 0 0 1 ${X + 1},${Y + hd}` : ''}V${Y + 1 - bd}${bd ? `A${bd},${bd} 0 0 1 ${X + 1 - bd},${Y + 1}` : ''}H${X + bg}${bg ? `A${bg},${bg} 0 0 1 ${X},${Y + 1 - bg}` : ''}V${Y + hg}${hg ? `A${hg},${hg} 0 0 1 ${X + hg},${Y}` : ''}Z"/>`);
  }
  const yeux = [[0, 0], [n - 7, 0], [0, n - 7]].map(([x, y]) => {
    const ox = x + marge, oy = y + marge;
    return `<path fill="${COULEURS.encre}" fill-rule="evenodd" d="M${ox + 1.6},${oy}h3.8a1.6,1.6 0 0 1 1.6,1.6v3.8a1.6,1.6 0 0 1-1.6,1.6h-3.8a1.6,1.6 0 0 1-1.6-1.6v-3.8a1.6,1.6 0 0 1 1.6-1.6zM${ox + 1.9},${oy + 1}h3.2a0.9,0.9 0 0 1 0.9,0.9v3.2a0.9,0.9 0 0 1-0.9,0.9h-3.2a0.9,0.9 0 0 1-0.9-0.9v-3.2a0.9,0.9 0 0 1 0.9-0.9z"/>`
      + `<rect x="${ox + 2}" y="${oy + 2}" width="3" height="3" rx="0.7" fill="${COULEURS.laterite}"/>`;
  }).join('');
  const lx = debut + marge, pad = 0.35;
  const logoSvg = `<rect x="${lx - pad}" y="${lx - pad}" width="${cote + pad * 2}" height="${cote + pad * 2}" rx="0.9" fill="#FFFFFF"/>`
    + `<svg x="${lx + 0.3}" y="${lx + 0.3}" width="${cote - 0.6}" height="${cote - 0.6}" viewBox="0 0 64 64">${logo}</svg>`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${total} ${total}" shape-rendering="geometricPrecision">`
    + `<title>${texte}</title><rect width="${total}" height="${total}" fill="#FFFFFF"/>`
    // Contour fin de même couleur : supprime les filets clairs d'anticrénelage entre modules voisins
    + `<g fill="${COULEURS.encre}" stroke="${COULEURS.encre}" stroke-width="0.14" stroke-linejoin="round">${points.join('')}</g>${yeux}${logoSvg}</svg>`;
}

async function verifier(page, svg, taille) {
  await page.setViewport({ width: taille, height: taille });
  await page.setContent(`<html><body style="margin:0">${svg.replace('<svg ', `<svg width="${taille}" height="${taille}" `)}</body></html>`);
  const png = PNG.sync.read(await page.screenshot({ type: 'png' }));
  return jsQR(new Uint8ClampedArray(png.data), png.width, png.height)?.data ?? null;
}

if (process.argv[1]?.endsWith('qrcodes.mjs')) {
  fs.mkdirSync(dossier, { recursive: true });
  const navigateur = await puppeteer.launch({ executablePath: EDGE, headless: true });
  const page = await navigateur.newPage();
  let ok = true;
  for (const [nom, s] of Object.entries(SUPPORTS)) {
    const url = SITE + s.chemin;
    const svg = svgQr(url);
    fs.writeFileSync(path.join(dossier, `qr-${nom}.svg`), svg);
    // PNG 2000 px pour les usages hors vectoriel
    await page.setViewport({ width: 2000, height: 2000 });
    await page.setContent(`<html><body style="margin:0">${svg.replace('<svg ', '<svg width="2000" height="2000" ')}</body></html>`);
    fs.writeFileSync(path.join(dossier, `qr-${nom}.png`), await page.screenshot({ type: 'png' }));
    const lectures = [];
    for (const taille of [1200, 400, 180]) lectures.push([taille, await verifier(page, svg, taille)]);
    const bon = lectures.every(([, t]) => t === url);
    ok &&= bon;
    console.log(`${bon ? 'OK ' : 'KO '} ${nom.padEnd(8)} ${url}  ` + lectures.map(([t, v]) => `${t}px:${v === url ? 'lu' : 'ÉCHEC'}`).join(' '));
  }
  await navigateur.close();
  if (!ok) { console.error('Au moins un QR code ne se décode pas.'); process.exit(1); }
}
