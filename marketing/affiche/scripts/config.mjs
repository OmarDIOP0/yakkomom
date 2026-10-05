// Réglages communs : modifiez ici l'adresse du site, puis relancez « npm run qr » et « npm run export ».
import { fileURLToPath } from 'node:url';

/** Dossier marketing/affiche. */
export const RACINE = fileURLToPath(new URL('..', import.meta.url));

export const SITE = 'https://yakkomom.onrender.com';
export const NOM = 'YakkoMoM';

// Une adresse courte par support : l'application redirige vers /terrains avec les paramètres UTM
// (voir Yakkomom/Controllers/CampagnesController.cs). L'adresse imprimée ne change jamais.
export const SUPPORTS = {
  affiche: { chemin: '/affiche', utm: 'utm_source=affiche&utm_medium=print' },
  panneau: { chemin: '/panneau', utm: 'utm_source=panneau&utm_medium=outdoor' },
  web: { chemin: '/web', utm: 'utm_source=site&utm_medium=web' },
  reseaux: { chemin: '/reseaux', utm: 'utm_source=reseaux&utm_medium=social' },
};

export const COULEURS = { papier: '#FBF7F0', encre: '#1C2A21', laterite: '#B4441F', ocre: '#C98A1B', sable: '#F4ECDD' };

export const EDGE = process.env.CHROME_PATH ?? 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
