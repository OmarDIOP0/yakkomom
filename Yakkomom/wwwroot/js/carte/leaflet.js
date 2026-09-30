// Chargement de Leaflet à la demande (≈ 45 Ko compressés) : jamais sur les pages sans carte,
// et seulement quand la carte devient visible ou que l'utilisateur la demande.

import { chargerBibliotheque, quandVisible } from '../chargeur.js';

export { quandVisible };

/** @param {HTMLElement} el élément portant data-leaflet-js et data-leaflet-css */
export function chargerLeaflet(el) {
  return chargerBibliotheque(el.dataset.leafletCss, el.dataset.leafletJs, 'L');
}

/** Fonds de carte : plan OpenStreetMap et vue satellite Esri (avec noms de lieux). */
export function creerFonds(L) {
  const plan = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  });
  // Au Sénégal, hors grandes villes, Esri n'a souvent pas d'image au-delà du zoom 17
  // (tuile « Map data not yet available ») : on agrandit la tuile 17 au lieu de l'afficher.
  const images = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    maxZoom: 19,
    maxNativeZoom: 17,
    attribution: 'Images &copy; Esri, Maxar, Earthstar Geographics',
  });
  const noms = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{z}/{y}/{x}', {
    maxZoom: 19,
    maxNativeZoom: 17,
  });
  return { plan, satellite: L.layerGroup([images, noms]) };
}

/** Crédit Leaflet sobre (sans drapeau) + attributions des fonds. */
export function preparerAttribution(carte) {
  carte.attributionControl.setPrefix('<a href="https://leafletjs.com">Leaflet</a>');
}

/** Repère en SVG (pas d'image à télécharger). */
export function iconeRepere(L, couleur = '#B4441F') {
  return L.divIcon({
    className: 'repere-carte',
    html: `<svg viewBox="0 0 32 42" width="32" height="42" aria-hidden="true"><path d="M16 41S3 25.5 3 15.5a13 13 0 0 1 26 0C29 25.5 16 41 16 41z" fill="${couleur}" stroke="#fff" stroke-width="2.5"/><circle cx="16" cy="15.5" r="5" fill="#fff"/></svg>`,
    iconSize: [32, 42],
    iconAnchor: [16, 41],
  });
}

// --- Calculs ---------------------------------------------------------------

/** Surface sur la sphère (m²) : même formule que le serveur (Helpers/Geo.cs). */
export function aireM2(points) {
  if (points.length < 3) return 0;
  const R = 6378137, rad = Math.PI / 180;
  let aire = 0;
  for (let i = 0; i < points.length; i++) {
    const p1 = points[i], p2 = points[(i + 1) % points.length];
    aire += (p2.lng - p1.lng) * rad * (2 + Math.sin(p1.lat * rad) + Math.sin(p2.lat * rad));
  }
  return Math.abs(aire * R * R / 2);
}

export function surfaceLisible(m2) {
  if (m2 >= 10000) return (m2 / 10000).toLocaleString('fr-FR', { maximumFractionDigits: 2 }) + ' ha';
  return Math.round(m2).toLocaleString('fr-FR') + ' m²';
}

/**
 * Lit des coordonnées collées : « 14.5198, -17.0021 », « 14,5198 ; -17,0021 »,
 * « 14°31'11.3"N 17°00'07.6"W », ou un lien Google Maps (…/@14.5198,-17.0021,17z, ?q=…, !3d…!4d…).
 * @returns {{lat:number,lng:number}|null}
 */
export function lireCoordonnees(texte) {
  if (!texte) return null;
  const t = decodeURIComponent(texte.trim());
  const valide = (lat, lng) => (Number.isFinite(lat) && Number.isFinite(lng) && Math.abs(lat) <= 90 && Math.abs(lng) <= 180 ? { lat, lng } : null);

  let m = t.match(/!3d(-?\d+(?:\.\d+)?)!4d(-?\d+(?:\.\d+)?)/);
  if (m) return valide(+m[1], +m[2]);
  m = t.match(/[@=/](-?\d{1,2}\.\d+),\s*(-?\d{1,3}\.\d+)/);
  if (m) return valide(+m[1], +m[2]);

  // Degrés, minutes, secondes
  const dms = [...t.matchAll(/(\d{1,3})\s*°\s*(\d{1,2})\s*['’′]\s*(\d{1,2}(?:[.,]\d+)?)\s*(?:["”″]|'')?\s*([NSEWO])/gi)];
  if (dms.length === 2) {
    const conv = ([, d, mi, s, h]) => (+d + +mi / 60 + parseFloat(s.replace(',', '.')) / 3600) * (/[SWO]/i.test(h) ? -1 : 1);
    const [a, b] = dms.map(conv);
    return /[NS]/i.test(dms[0][4]) ? valide(a, b) : valide(b, a);
  }

  // Deux nombres décimaux (point ou virgule décimale)
  m = t.match(/^(-?\d{1,2}\.\d+)\s*[,; ]\s*(-?\d{1,3}\.\d+)$/) ?? t.match(/^(-?\d{1,2},\d+)\s*[; ]\s*(-?\d{1,3},\d+)$/);
  if (m) return valide(parseFloat(m[1].replace(',', '.')), parseFloat(m[2].replace(',', '.')));
  return null;
}
