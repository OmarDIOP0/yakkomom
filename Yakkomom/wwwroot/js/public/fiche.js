// Fiche terrain : galerie, visionneuse, vidéo à la demande, carte à la demande.
// Rien de lourd ne se charge sans action ou sans que l'élément soit à l'écran (économie de données).
import { chargerLeaflet, quandVisible, creerFonds, iconeRepere, preparerAttribution } from '../carte/leaflet.js';
import { chargerBibliotheque } from '../chargeur.js';

const economie = document.documentElement.classList.contains('economie');

// --- Galerie : compteur « 2 / 8 » ------------------------------------------------
const piste = document.querySelector('[data-piste]');
const compteur = document.querySelector('[data-compteur]');
if (piste && compteur) {
  const total = piste.children.length;
  let attente;
  piste.addEventListener('scroll', () => {
    cancelAnimationFrame(attente);
    attente = requestAnimationFrame(() => {
      const i = Math.round(piste.scrollLeft / piste.clientWidth);
      compteur.textContent = `${Math.min(total, i + 1)} / ${total}`;
    });
  }, { passive: true });
}

// --- Visionneuse plein écran (images 1600 px chargées seulement à l'ouverture) --------
const visionneuse = document.querySelector('[data-visionneuse]');
if (visionneuse && piste) {
  const pisteGrande = visionneuse.querySelector('[data-piste-grande]');
  piste.addEventListener('click', (e) => {
    const bouton = e.target.closest('[data-ouvrir]');
    if (!bouton) return;
    const index = Number(bouton.dataset.ouvrir);
    const images = pisteGrande.querySelectorAll('img');
    // La photo demandée et ses voisines d'abord
    [index, index + 1, index - 1].forEach((i) => { const img = images[i]; if (img && !img.src) img.src = img.dataset.src; });
    visionneuse.showModal();
    pisteGrande.scrollLeft = index * pisteGrande.clientWidth;
    const obs = new IntersectionObserver((entrees) => entrees.forEach((en) => {
      const img = en.target;
      if (en.isIntersecting && !img.src) img.src = img.dataset.src;
    }), { root: pisteGrande, rootMargin: '0px 100%' });
    images.forEach((img) => obs.observe(img));
    visionneuse.addEventListener('close', () => obs.disconnect(), { once: true });
  });
  visionneuse.querySelector('[data-fermer]').addEventListener('click', () => visionneuse.close());
  visionneuse.addEventListener('click', (e) => { if (e.target === visionneuse) visionneuse.close(); });
  visionneuse.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowRight') pisteGrande.scrollBy({ left: pisteGrande.clientWidth, behavior: 'smooth' });
    if (e.key === 'ArrowLeft') pisteGrande.scrollBy({ left: -pisteGrande.clientWidth, behavior: 'smooth' });
  });
}

// --- Vidéo YouTube : lecteur chargé uniquement au clic -------------------------------
for (const bloc of document.querySelectorAll('[data-video]')) {
  bloc.querySelector('[data-lancer-video]').addEventListener('click', () => {
    const iframe = document.createElement('iframe');
    iframe.src = `https://www.youtube-nocookie.com/embed/${encodeURIComponent(bloc.dataset.video)}?autoplay=1&rel=0&playsinline=1`;
    iframe.title = 'Vidéo du terrain';
    iframe.allow = 'autoplay; encrypted-media; picture-in-picture; fullscreen';
    iframe.allowFullscreen = true;
    iframe.loading = 'eager';
    bloc.replaceChildren(iframe);
  }, { once: true });
}

// --- Carte : chargée quand elle devient visible (ou au clic en mode économie) ----------
const blocCarte = document.querySelector('[data-carte-publique]');
if (blocCarte) {
  const bouton = blocCarte.querySelector('[data-lancer-carte]');
  let lancee = false;
  const lancer = async () => {
    if (lancee) return;
    lancee = true;
    bouton.querySelector('strong').textContent = 'Chargement de la carte…';
    try {
      const L = await chargerLeaflet(blocCarte);
      bouton.remove();
      blocCarte.classList.add('est-chargee');
      const position = L.latLng(+blocCarte.dataset.lat, +blocCarte.dataset.lng);
      const fonds = creerFonds(L);
      const carte = L.map(blocCarte, { scrollWheelZoom: false, dragging: !L.Browser.mobile, tapTolerance: 20 });
      preparerAttribution(carte);
      fonds.satellite.addTo(carte);
      L.control.layers({ Satellite: fonds.satellite, Plan: fonds.plan }, null, { position: 'topright' }).addTo(carte);
      L.control.scale({ imperial: false }).addTo(carte);
      L.marker(position, { icon: iconeRepere(L), title: 'Emplacement du terrain' }).addTo(carte);

      let contour = null;
      try {
        const g = JSON.parse(blocCarte.dataset.contour || 'null');
        if (g?.coordinates) contour = L.polygon(g.coordinates[0].map(([lng, lat]) => [lat, lng]),
          { color: '#E3A53A', weight: 3, fillColor: '#E3A53A', fillOpacity: 0.2 }).addTo(carte);
      } catch { /* contour absent ou illisible */ }

      if (contour) carte.fitBounds(contour.getBounds(), { maxZoom: 18, padding: [40, 40] });
      else carte.setView(position, 16);
      // Sur mobile : un doigt fait défiler la page, deux doigts déplacent la carte
      if (L.Browser.mobile) carte.on('zoomstart', () => carte.dragging.enable());
    } catch {
      lancee = false;
      bouton.querySelector('strong').textContent = 'Carte indisponible — réessayer';
    }
  };
  bouton.addEventListener('click', lancer);
  if (!economie) quandVisible(blocCarte, lancer, '150px');
}

// --- Bouton WhatsApp flottant : masqué quand les contacts sont déjà à l'écran ----------
const flottant = document.querySelector('[data-wa-flottant]');
const contacts = document.querySelector('#contacter');
if (flottant && contacts && 'IntersectionObserver' in window) {
  new IntersectionObserver(([e]) => flottant.classList.toggle('est-masque', e.isIntersecting), { threshold: 0.2 }).observe(contacts);
}

// --- Visite 360° : jamais chargée automatiquement ---------------------------------------

const blocVisite = document.querySelector('[data-visite]');
if (blocVisite) {
  const lancer = blocVisite.querySelector('[data-lancer-visite]');
  const lecteur = blocVisite.querySelector('[data-lecteur]');
  const outils = blocVisite.querySelector('[data-outils-visite]');
  const boutonHd = blocVisite.querySelector('[data-hd]');
  // Pannellum ajoute des éléments DOM dans la configuration reçue : on repart toujours du JSON d'origine.
  const texteConfig = blocVisite.querySelector('[data-config]').textContent;
  const urlsHd = JSON.parse(blocVisite.querySelector('[data-urls-hd]').textContent);
  let viewer = null;

  function creer(pannellum, cfg) {
    viewer = pannellum.viewer(lecteur, cfg);
    viewer.on('error', () => { outils.hidden = false; });
  }

  lancer.addEventListener('click', async () => {
    lancer.setAttribute('aria-busy', 'true');
    lancer.querySelector('strong').firstChild.textContent = 'Chargement de la visite… ';
    try {
      const pannellum = await chargerBibliotheque(blocVisite.dataset.pannellumCss, blocVisite.dataset.pannellumJs, 'pannellum');
      lancer.hidden = true;
      lecteur.hidden = false;
      outils.hidden = false;
      creer(pannellum, JSON.parse(texteConfig));
      lecteur.focus?.();
    } catch {
      lancer.removeAttribute('aria-busy');
      lancer.querySelector('strong').firstChild.textContent = 'Visite indisponible — réessayer ';
    }
  });

  // Haute définition : même scène, même direction, images HD
  boutonHd?.addEventListener('click', () => {
    if (!viewer) return;
    const scene = viewer.getScene();
    const vue = { yaw: viewer.getYaw(), pitch: viewer.getPitch(), hfov: viewer.getHfov() };
    const cfg = JSON.parse(texteConfig);
    for (const [id, s] of Object.entries(cfg.scenes)) if (urlsHd[id]) s.panorama = urlsHd[id];
    cfg.default.firstScene = scene;
    Object.assign(cfg.scenes[scene], vue);
    viewer.destroy();
    creer(window.pannellum, cfg);
    boutonHd.remove();
  });
}

// --- Terrains récemment consultés (page hors ligne, moment d'installation) ---------------
const favoriFiche = document.querySelector('.galerie [data-favori]');
if (favoriFiche) {
  const d = favoriFiche.dataset;
  try {
    const vus = (JSON.parse(localStorage.getItem('yk.vus')) ?? []).filter((v) => v.reference !== d.favori);
    vus.unshift({ reference: d.favori, titre: d.titre, url: d.url, image: d.image || null, couleur: d.couleur || null,
      prix: d.prix || null, surface: d.surface || null, lieu: d.lieu || null, vuLe: Date.now() });
    localStorage.setItem('yk.vus', JSON.stringify(vus.slice(0, 20)));
  } catch { /* stockage indisponible */ }
}
