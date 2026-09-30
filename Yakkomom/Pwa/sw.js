/* Yakkomom — service worker (écrit à la main, sans dépendance).
 * Le serveur ajoute en tête : const VERSION = '…'; const PRECACHE = [ … ];
 *
 * Stratégies :
 *  - fichiers statiques à empreinte (/css, /js, /fonts, /img, /lib) : cache d'abord (ils ne changent jamais) ;
 *  - photos (/media, Cloudinary) : cache d'abord, 150 images au plus ;
 *  - pages : réseau d'abord, MAIS si le serveur ne répond pas en 3,5 s (réveil de Render, 3G lente),
 *    la version enregistrée s'affiche aussitôt ; la page est mise à jour en arrière-plan et l'utilisateur
 *    est prévenu. Hors ligne sans version enregistrée : page « hors ligne » ;
 *  - jamais en cache : admin, API, redirections WhatsApp, documents fonciers, préférences.
 */

const CACHE_STATIQUE = `yk-statique-${VERSION}`;
const CACHE_PAGES = 'yk-pages';
const CACHE_IMAGES = 'yk-images';
const PAGE_HORS_LIGNE = '/hors-ligne';
const DELAI_PAGE_MS = 3500;
const MAX_PAGES = 60;
const MAX_IMAGES = 150;
const JAMAIS = ['/admin', '/api/', '/wa/', '/documents/', '/preferences/', '/sw.js', '/erreur/'];

self.addEventListener('install', (event) => {
  event.waitUntil((async () => {
    const cache = await caches.open(CACHE_STATIQUE);
    // Un fichier manquant ne doit pas empêcher l'installation (addAll échouerait en bloc).
    await Promise.all(PRECACHE.map((url) => cache.add(new Request(url, { cache: 'reload' })).catch(() => {})));
    const pages = await caches.open(CACHE_PAGES);
    await pages.add(new Request(PAGE_HORS_LIGNE, { cache: 'reload' })).catch(() => {});
    await self.skipWaiting();
  })());
});

self.addEventListener('activate', (event) => {
  event.waitUntil((async () => {
    for (const nom of await caches.keys()) {
      if (nom.startsWith('yk-statique-') && nom !== CACHE_STATIQUE) await caches.delete(nom);
    }
    if (self.registration.navigationPreload) await self.registration.navigationPreload.enable().catch(() => {});
    await self.clients.claim();
  })());
});

self.addEventListener('fetch', (event) => {
  const requete = event.request;
  if (requete.method !== 'GET') return;
  const url = new URL(requete.url);

  if (url.origin === self.location.origin) {
    if (JAMAIS.some((p) => url.pathname.startsWith(p))) return;
    if (requete.mode === 'navigate') { event.respondWith(page(event)); return; }
    if (/^\/(css|js|fonts|img|lib)\//.test(url.pathname)) { event.respondWith(cacheDabord(requete, CACHE_STATIQUE)); return; }
    if (url.pathname.startsWith('/media/')) { event.respondWith(cacheDabord(requete, CACHE_IMAGES, MAX_IMAGES)); return; }
    return;
  }
  if (url.hostname === 'res.cloudinary.com' && requete.destination === 'image') {
    event.respondWith(cacheDabord(requete, CACHE_IMAGES, MAX_IMAGES));
  }
  // Tuiles de carte, YouTube, etc. : comportement normal du navigateur.
});

async function cacheDabord(requete, nomCache, max) {
  const cache = await caches.open(nomCache);
  const trouve = await cache.match(requete);
  if (trouve) return trouve;
  const reponse = await fetch(requete);
  if (reponse.ok && (reponse.type === 'basic' || reponse.type === 'cors')) {
    await cache.put(requete, reponse.clone());
    if (max) limiter(nomCache, max);
  }
  return reponse;
}

async function page(event) {
  const cache = await caches.open(CACHE_PAGES);
  const enregistree = await cache.match(event.request, { ignoreVary: true });
  let versionEnregistreeServie = false;

  const reseau = (async () => {
    const reponse = (await event.preloadResponse) || (await fetch(event.request));
    const cacheable = reponse.ok && reponse.type === 'basic' && !(reponse.headers.get('Cache-Control') ?? '').includes('no-store');
    if (cacheable) {
      await cache.put(event.request, reponse.clone());
      limiter(CACHE_PAGES, MAX_PAGES);
      if (versionEnregistreeServie) prevenir(event.resultingClientId || event.clientId);
    }
    if (!reponse.ok && enregistree && reponse.status >= 500) throw new Error('serveur');
    return reponse;
  })();

  if (!enregistree) {
    try { return await reseau; } catch { return (await cache.match(PAGE_HORS_LIGNE)) ?? Response.error(); }
  }

  event.waitUntil(reseau.catch(() => {}));
  const delai = new Promise((resoudre) => setTimeout(() => resoudre(null), DELAI_PAGE_MS));
  const rapide = await Promise.race([reseau.catch(() => null), delai]);
  if (rapide) return rapide;

  versionEnregistreeServie = true;
  return marquer(enregistree);
}

/** Signale à la page qu'elle vient du cache (lu via Performance / Server-Timing). */
async function marquer(reponse) {
  const entetes = new Headers(reponse.headers);
  entetes.set('Server-Timing', 'yk-cache;desc="version enregistree"');
  return new Response(await reponse.blob(), { status: reponse.status, statusText: reponse.statusText, headers: entetes });
}

async function prevenir(clientId) {
  // Laisser à la page le temps de s'initialiser avant de lui écrire
  await new Promise((r) => setTimeout(r, 500));
  const client = clientId ? await self.clients.get(clientId) : null;
  client?.postMessage({ type: 'yk:page-a-jour' });
}

async function limiter(nomCache, max) {
  const cache = await caches.open(nomCache);
  const cles = await cache.keys();
  for (let i = 0; i < cles.length - max; i++) {
    if (new URL(cles[i].url).pathname === PAGE_HORS_LIGNE) continue;
    await cache.delete(cles[i]);
  }
}
