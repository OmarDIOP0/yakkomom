// Vérifie que le QR code de chaque export se décode (jsQR) et renvoie la bonne adresse.
import fs from 'node:fs';
import path from 'node:path';
import jsQR from 'jsqr';
import { PNG } from 'pngjs';
import { RACINE, SITE, SUPPORTS } from './config.mjs';

const attendus = { print: 'affiche', panneau: 'panneau', web: 'web', social: 'reseaux' };
const lire = (p) => jsQR(new Uint8ClampedArray(p.data), p.width, p.height)?.data ?? null;

function recadrer(src, x, y, w, h) {
  x = Math.max(0, x); y = Math.max(0, y); w = Math.min(w, src.width - x); h = Math.min(h, src.height - y);
  const p = new PNG({ width: w, height: h });
  for (let j = 0; j < h; j++) src.data.copy(p.data, j * w * 4, ((y + j) * src.width + x) * 4, ((y + j) * src.width + x + w) * 4);
  return p;
}

/** Réduction par moyenne de blocs (comme l'échantillonnage d'un appareil photo). */
function reduire(src, largeurMax) {
  const k = Math.max(1, Math.ceil(src.width / largeurMax));
  if (k === 1) return src;
  const w = Math.floor(src.width / k), h = Math.floor(src.height / k), p = new PNG({ width: w, height: h });
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) for (let c = 0; c < 4; c++) {
    let t = 0;
    for (let j = 0; j < k; j++) for (let i = 0; i < k; i++) t += src.data[((y * k + j) * src.width + x * k + i) * 4 + c];
    p.data[(y * w + x) * 4 + c] = t / (k * k);
  }
  return p;
}

let ok = true;
for (const [dossier, support] of Object.entries(attendus)) {
  const d = path.join(RACINE, 'exports', dossier);
  if (!fs.existsSync(d)) continue;
  for (const f of fs.readdirSync(d).filter(f => f.endsWith('.png'))) {
    const png = PNG.sync.read(fs.readFileSync(path.join(d, f)));
    const url = SITE + SUPPORTS[support].chemin;
    const essais = [];
    // 1. Téléphone qui vise le QR : zone recadrée (marge de 10 %), pleine résolution
    const zone = path.join(d, f.replace(/\.png$/, '.qr.json'));
    if (fs.existsSync(zone)) {
      const [x, y, w, h] = JSON.parse(fs.readFileSync(zone, 'utf8'));
      const m = Math.round(w * 0.1);
      essais.push(['QR cadré', lire(recadrer(png, x - m, y - m, w + 2 * m, h + 2 * m))]);
    }
    // 2. Photo de l'ensemble : image entière, réduite à 1600 px de large au plus
    essais.push(["vue d'ensemble", lire(reduire(png, 1600))]);
    const bon = essais.every(([, lu]) => lu === url);
    ok &&= bon;
    console.log(`${bon ? 'OK ' : 'KO '} ${dossier}/${f} (${png.width}×${png.height}) ` + essais.map(([n, lu]) => `${n} : ${lu === url ? 'lu' : lu ?? 'illisible'}`).join(' · '));
  }
}
if (!ok) process.exit(1);
