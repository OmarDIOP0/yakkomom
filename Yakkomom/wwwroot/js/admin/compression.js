// Compression d'images dans le navigateur, AVANT l'envoi (l'admin est souvent en 3G).
// Une photo de téléphone de 4 à 8 Mo devient un WebP d'environ 200 à 400 Ko.

/** Le navigateur sait-il produire du WebP ? (Safari < 16 renvoie du PNG à la place) */
let webpPossible;
async function formatSortie() {
  if (webpPossible === undefined) {
    const c = document.createElement('canvas');
    c.width = c.height = 2;
    const blob = await new Promise((r) => c.toBlob(r, 'image/webp', 0.8));
    webpPossible = blob?.type === 'image/webp';
  }
  return webpPossible ? 'image/webp' : 'image/jpeg';
}

/** Décode l'image en respectant l'orientation EXIF (photos prises en portrait). */
async function decoder(fichier) {
  if ('createImageBitmap' in window) {
    try {
      return await createImageBitmap(fichier, { imageOrientation: 'from-image' });
    } catch { /* format non décodable ici : repli sur <img> */ }
  }
  const url = URL.createObjectURL(fichier);
  try {
    const img = new Image();
    img.decoding = 'async';
    img.src = url;
    await img.decode();
    return img;
  } finally {
    URL.revokeObjectURL(url);
  }
}

function dessiner(source, largeurMax) {
  const l = source.width, h = source.height;
  const ratio = Math.min(1, largeurMax / Math.max(l, h));
  const canvas = document.createElement('canvas');
  canvas.width = Math.round(l * ratio);
  canvas.height = Math.round(h * ratio);
  const ctx = canvas.getContext('2d', { alpha: false });
  ctx.imageSmoothingQuality = 'high';
  ctx.drawImage(source, 0, 0, canvas.width, canvas.height);
  return canvas;
}

function versBlob(canvas, type, qualite) {
  return new Promise((resoudre, rejeter) =>
    canvas.toBlob((b) => (b ? resoudre(b) : rejeter(new Error('Compression impossible'))), type, qualite));
}

/** Couleur moyenne (#RRGGBB), affichée pendant le chargement de la photo. */
function couleurDominante(source) {
  const c = document.createElement('canvas');
  c.width = c.height = 1;
  const ctx = c.getContext('2d', { willReadFrequently: true });
  ctx.drawImage(source, 0, 0, 1, 1);
  const [r, g, b] = ctx.getImageData(0, 0, 1, 1).data;
  return '#' + [r, g, b].map((v) => v.toString(16).padStart(2, '0')).join('');
}

/**
 * @param {File} fichier
 * @param {{ largeurMax?: number, qualite?: number, vignette?: number }} options
 * @returns {Promise<{ blob: Blob, largeur: number, hauteur: number, couleur: string, vignette?: Blob }>}
 */
export async function compresserImage(fichier, { largeurMax = 1600, qualite = 0.8, vignette = 480 } = {}) {
  const source = await decoder(fichier);
  try {
    const type = await formatSortie();
    const grand = dessiner(source, largeurMax);
    const blob = await versBlob(grand, type, type === 'image/webp' ? qualite : qualite + 0.02);
    const resultat = { blob, largeur: grand.width, hauteur: grand.height, couleur: couleurDominante(grand) };
    if (vignette) resultat.vignette = await versBlob(dessiner(grand, vignette), type, 0.72);
    return resultat;
  } finally {
    source.close?.();
  }
}

export function tailleLisible(octets) {
  if (octets >= 1024 * 1024) return (octets / 1024 / 1024).toFixed(1).replace('.', ',') + ' Mo';
  return Math.max(1, Math.round(octets / 1024)) + ' Ko';
}

/**
 * Panorama équirectangulaire (photo sphère ou « mode panorama » du téléphone) :
 * HD ≤ 4096 px (limite de texture sûre sur mobile), version légère 2048 px, vignette 640×320 recadrée au centre.
 * @returns {Promise<{ hd: Blob, bd: Blob, vignette: Blob, largeur: number, hauteur: number, ratio: number }>}
 */
export async function preparerPanorama(fichier) {
  const source = await decoder(fichier);
  try {
    const ratio = source.width / source.height;
    if (ratio < 1.9) throw Object.assign(new Error('pas-panorama'), { ratio });
    const type = await formatSortie();

    const hdCanvas = dessiner(source, Math.min(4096, source.width));
    const hd = await versBlob(hdCanvas, type, 0.82);
    const bd = await versBlob(dessiner(hdCanvas, Math.min(2048, hdCanvas.width)), type, 0.74);

    // Vignette : zone centrale au format 2:1 (lisible même pour un panorama très allongé)
    const v = document.createElement('canvas');
    v.width = 640; v.height = 320;
    const largeurZone = Math.min(hdCanvas.width, hdCanvas.height * 2);
    const x = (hdCanvas.width - largeurZone) / 2;
    const ctx = v.getContext('2d', { alpha: false });
    ctx.imageSmoothingQuality = 'high';
    ctx.drawImage(hdCanvas, x, 0, largeurZone, largeurZone / 2, 0, 0, 640, 320);
    const vignette = await versBlob(v, type, 0.7);

    return { hd, bd, vignette, largeur: hdCanvas.width, hauteur: hdCanvas.height, ratio };
  } finally {
    source.close?.();
  }
}
