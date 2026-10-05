// Version « une seule page » : un fichier HTML autonome par affiche, sans aucun fichier lié
// (styles, polices, images, QR code et écran du téléphone intégrés). S'ouvre partout, s'envoie par e-mail.
// Usage : node scripts/autonome.mjs [a3 …]
import fs from 'node:fs';
import path from 'node:path';
import { RACINE } from './config.mjs';

const SOURCES = path.join(RACINE, 'sources');
const TYPES = { '.svg': 'image/svg+xml', '.webp': 'image/webp', '.png': 'image/png', '.jpg': 'image/jpeg', '.woff2': 'font/woff2' };
const dataUri = (fichier) => `data:${TYPES[path.extname(fichier)]};base64,${fs.readFileSync(fichier).toString('base64')}`;

/** Remplace url(...) et src="..." relatifs par des data URI, résolus depuis le dossier donné. */
function integrer(texte, dossier) {
  return texte
    .replace(/url\("?(?!data:|https?:)([^")]+\.(?:woff2|svg|webp|png))"?\)/g, (_, f) => `url("${dataUri(path.join(dossier, f))}")`)
    .replace(/(<img[^>]*\ssrc=")(?!data:|https?:)([^"]+)"/g, (_, debut, f) => `${debut}${dataUri(path.join(dossier, f))}"`);
}

/** Écran du téléphone : fiche.html autonome (sprite d'icônes intégré, images en data URI), pour srcdoc. */
function ecranAutonome() {
  const dossier = path.join(SOURCES, 'ecran');
  let html = fs.readFileSync(path.join(dossier, 'fiche.html'), 'utf8');
  // Photos de la galerie hors écran : inutiles dans l'affiche, on ne garde que la première
  let n = 0;
  html = html.replace(/<li[^>]*>\s*<button[^>]*class="galerie__photo"[\s\S]*?<\/li>/g, (li) => (n++ === 0 ? li : ''));
  html = integrer(html, dossier);
  const sprite = fs.readFileSync(path.join(dossier, 'icons.svg'), 'utf8').replace('<svg ', '<svg style="display:none" aria-hidden="true" ');
  html = html.replace(/href="icons\.svg#/g, 'href="#').replace(/<body([^>]*)>/, `<body$1>${sprite}`);
  return html;
}

const demandes = process.argv.slice(2).length ? process.argv.slice(2) : ['a3'];
for (const nom of demandes) {
  let html = fs.readFileSync(path.join(SOURCES, `${nom}.html`), 'utf8');
  const css = integrer(fs.readFileSync(path.join(SOURCES, 'commun.css'), 'utf8'), SOURCES);
  html = html.replace('<link rel="stylesheet" href="commun.css">', `<style>\n${css}\n</style>`);
  for (const [, f] of [...html.matchAll(/<link rel="stylesheet" href="([^"]+)">/g)])
    html = html.replace(`<link rel="stylesheet" href="${f}">`, `<style>\n${integrer(fs.readFileSync(path.join(SOURCES, f), 'utf8'), SOURCES)}\n</style>`);
  html = html.replace('<script src="apercu.js"></script>', `<script>\n${fs.readFileSync(path.join(SOURCES, 'apercu.js'), 'utf8')}\n</script>`);
  // QR code (dossier qrcodes) puis autres images (dossier sources)
  html = html.replace(/src="\.\.\/qrcodes\/([^"]+)"/g, (_, f) => `src="${dataUri(path.join(RACINE, 'qrcodes', f))}"`);
  html = integrer(html, SOURCES);
  const ecran = ecranAutonome().replace(/&/g, '&amp;').replace(/"/g, '&quot;');
  html = html.replace(/<iframe src="ecran\/fiche\.html"/, `<iframe srcdoc="${ecran}"`);
  const sortie = path.join(RACINE, 'exports', 'print', `yakkomom-affiche-${nom}.html`);
  fs.writeFileSync(sortie, html);
  const restes = [...html.matchAll(/(?:src|href)="(?!data:|#|https?:)[^"]+"/g)].map(m => m[0]);
  console.log(`${nom} → exports/print/${path.basename(sortie)} (${Math.round(html.length / 1024)} Ko)` + (restes.length ? `  liens restants : ${restes.join(', ')}` : '  aucun fichier lié'));
}
