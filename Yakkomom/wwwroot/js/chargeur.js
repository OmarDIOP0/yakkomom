// Chargement à la demande des bibliothèques lourdes (Leaflet, Pannellum) : une seule fois par page.
const chargements = new Map();

function feuilleDeStyle(url) {
  if (!url || document.querySelector(`link[href="${url}"]`)) return Promise.resolve();
  return new Promise((resoudre, rejeter) => {
    const lien = Object.assign(document.createElement('link'), { rel: 'stylesheet', href: url });
    lien.onload = resoudre;
    lien.onerror = rejeter;
    document.head.append(lien);
  });
}

function script(url) {
  return new Promise((resoudre, rejeter) => {
    const s = Object.assign(document.createElement('script'), { src: url, async: true });
    s.onload = resoudre;
    s.onerror = rejeter;
    document.head.append(s);
  });
}

/** Charge css + js et renvoie l'objet global exposé (ex. window.L, window.pannellum). */
export function chargerBibliotheque(css, js, globale) {
  if (!chargements.has(js)) {
    const p = Promise.all([feuilleDeStyle(css), script(js)])
      .then(() => window[globale])
      .catch((e) => { chargements.delete(js); throw e; });
    chargements.set(js, p);
  }
  return chargements.get(js);
}

/** Exécute `rappel` quand l'élément approche de l'écran (ou tout de suite sans IntersectionObserver). */
export function quandVisible(el, rappel, marge = '200px') {
  if (!('IntersectionObserver' in window)) { rappel(); return; }
  const obs = new IntersectionObserver((entrees) => {
    if (entrees.some((e) => e.isIntersecting)) { obs.disconnect(); rappel(); }
  }, { rootMargin: marge });
  obs.observe(el);
}
