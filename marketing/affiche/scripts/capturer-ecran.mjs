// Fige le vrai écran « fiche terrain » de l'application (HTML généré par l'app + sa feuille de style)
// dans sources/ecran/fiche.html, pour l'afficher dans le téléphone de l'affiche (reste vectoriel en PDF).
// Prérequis : l'application tourne en local (dotnet run) avec les terrains de démonstration.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer-core';
import { EDGE, RACINE } from './config.mjs';

const BASE = process.env.YK_LOCAL ?? 'http://localhost:5009';
const PAGE = process.env.YK_FICHE ?? '/terrains/yk-0020-terrain-500m2-popenguine-ndayane';
const dossier = path.join(RACINE, 'sources', 'ecran');
fs.mkdirSync(dossier, { recursive: true });

const navigateur = await puppeteer.launch({ executablePath: EDGE, headless: true });
const page = await navigateur.newPage();
await page.emulateMediaFeatures([{ name: 'prefers-color-scheme', value: 'light' }]);
await page.emulate({ viewport: { width: 390, height: 844, isMobile: true, hasTouch: true, deviceScaleFactor: 3 },
  userAgent: 'Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Mobile Safari/537.36' });
await page.goto(BASE + PAGE, { waitUntil: 'networkidle0' });

const { html, styles, images, fichiers } = await page.evaluate(() => {
  const d = document.documentElement;
  d.setAttribute('data-theme', 'light');
  d.querySelectorAll('.galerie__legende, script, link[rel=manifest], link[rel=preload], link[rel=modulepreload], link[rel=icon], link[rel=apple-touch-icon], .message-pwa, .installation, .hors-ligne').forEach(e => e.remove());
  // Titre d'exemple : sans la mention technique « (démo) »
  d.querySelectorAll('h1, title').forEach(e => { e.textContent = e.textContent.replace(/\s*\(démo\)/i, ''); });
  const styles = [...document.querySelectorAll('link[rel=stylesheet]')].map(l => { const u = l.href; l.remove(); return u; });
  const images = [];
  document.querySelectorAll('img').forEach((img, i) => {
    if (!img.currentSrc) { img.remove(); return; }
    const nom = `image-${i}` + (img.currentSrc.match(/\.(webp|png|jpe?g|svg)(\?|$)/i)?.[0].replace('?', '') ?? '.img');
    images.push([img.currentSrc, nom]);
    img.src = nom; img.removeAttribute('srcset'); img.removeAttribute('sizes'); img.loading = 'eager';
  });
  const fichiers = new Set();
  document.querySelectorAll('use').forEach(u => {
    const href = u.getAttribute('href'); const [f, id] = href.split('#');
    fichiers.add(f); u.setAttribute('href', 'icons.svg#' + id);
  });
  // Polices déclarées dans le <head> (@font-face) : copiées à côté
  document.querySelectorAll('style').forEach(s => {
    s.textContent = s.textContent.replace(/url\("?(\/fonts\/[^")]+)"?\)/g, (_, u) => { fichiers.add(u); return `url("${u.split('/').pop()}")`; });
  });
  return { html: '<!doctype html>\n' + d.outerHTML, styles, images, fichiers: [...fichiers] };
});

const css = (await Promise.all(styles.map(async u => (await fetch(u)).text()))).join('\n')
  .replace(/url\("?(\/fonts\/[^")]+)"?\)/g, (_, u) => { fichiers.push(u); return `url("${u.split('/').pop()}")`; });
for (const [u, nom] of images) fs.writeFileSync(path.join(dossier, nom), Buffer.from(await (await fetch(u)).arrayBuffer()));
for (const f of new Set(fichiers)) {
  const nom = f.includes('icons') ? 'icons.svg' : f.split('/').pop();
  fs.writeFileSync(path.join(dossier, nom), Buffer.from(await (await fetch(BASE + f)).arrayBuffer()));
}
fs.writeFileSync(path.join(dossier, 'fiche.html'), html.replace('</head>', `<style>\n${css}\n</style>\n</head>`));
console.log(`Écran figé : ${images.length} image(s), ${new Set(fichiers).size} fichier(s), ${Math.round(html.length / 1024)} Ko de HTML.`);
await navigateur.close();
