// Yakkomom — script global, volontairement minuscule (≈ 2 Ko).
// Les fonctionnalités lourdes (carte, 360°, envois…) sont des modules chargés uniquement sur leurs pages.
document.documentElement.classList.add('js');

// --- Favoris (sur l'appareil, sans compte) -----------------------------------
const CLE = 'yk.favoris';

export function lireFavoris() {
  try { return JSON.parse(localStorage.getItem(CLE)) ?? []; } catch { return []; }
}

export function ecrireFavoris(liste) {
  try { localStorage.setItem(CLE, JSON.stringify(liste)); } catch { /* stockage indisponible (navigation privée) */ }
  majCompteur();
  document.dispatchEvent(new CustomEvent('yk:favoris', { detail: liste }));
}

function majBoutons() {
  const refs = new Set(lireFavoris().map((f) => f.reference));
  for (const b of document.querySelectorAll('[data-favori]')) {
    const actif = refs.has(b.dataset.favori);
    b.setAttribute('aria-pressed', String(actif));
    b.setAttribute('aria-label', `${actif ? 'Retirer' : 'Ajouter'} ${b.dataset.favori} ${actif ? 'des' : 'aux'} favoris`);
  }
}

function majCompteur() {
  const n = lireFavoris().length;
  for (const lien of document.querySelectorAll('a[href="/favoris"]')) {
    let pastille = lien.querySelector('.pastille');
    if (!n) { pastille?.remove(); continue; }
    if (!pastille) {
      pastille = document.createElement('span');
      pastille.className = 'pastille';
      lien.append(pastille);
    }
    pastille.textContent = n > 99 ? '99+' : n;
    pastille.setAttribute('aria-label', `${n} favori${n > 1 ? 's' : ''}`);
  }
}

document.addEventListener('click', (e) => {
  const b = e.target.closest('[data-favori]');
  if (!b) return;
  e.preventDefault();
  const d = b.dataset;
  const liste = lireFavoris();
  const i = liste.findIndex((f) => f.reference === d.favori);
  if (i >= 0) liste.splice(i, 1);
  else liste.unshift({
    reference: d.favori, titre: d.titre, url: d.url, image: d.image || null, couleur: d.couleur || null,
    prix: d.prix || null, surface: d.surface || null, lieu: d.lieu || null, ajouteLe: Date.now(),
  });
  ecrireFavoris(liste);
  majBoutons();
  b.animate?.([{ transform: 'scale(1)' }, { transform: 'scale(1.25)' }, { transform: 'scale(1)' }], { duration: 250 });
});

// --- Partage : feuille de partage du téléphone, sinon lien WhatsApp ------------
document.addEventListener('click', async (e) => {
  const lien = e.target.closest('[data-partager]');
  if (!lien || !navigator.share) return; // sans Web Share : le lien wa.me/?text=… s'ouvre normalement
  e.preventDefault();
  try {
    await navigator.share({ title: lien.dataset.titre, text: lien.dataset.texte, url: lien.dataset.url });
  } catch { /* partage annulé */ }
});

majBoutons();
majCompteur();
window.addEventListener('storage', (e) => { if (e.key === CLE) { majBoutons(); majCompteur(); } });

// --- Application installable (PWA) ---------------------------------------------
// Service worker + petits compléments (hors ligne, installation) chargés après l'affichage.
if ('serviceWorker' in navigator && (location.protocol === 'https:' || location.hostname === 'localhost')) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => { /* navigateur ou mode privé sans SW */ });
    import('/js/pwa.js').catch(() => {});
  });
}
