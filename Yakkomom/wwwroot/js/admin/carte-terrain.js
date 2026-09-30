// Onglet Localisation : placer le terrain sur la carte et dessiner le contour de la parcelle.
// Sans JavaScript (ou si la carte ne charge pas), la saisie des coordonnées reste possible.
import { chargerLeaflet, quandVisible, creerFonds, iconeRepere, preparerAttribution, aireM2, surfaceLisible, lireCoordonnees } from '../carte/leaflet.js';

const bloc = document.querySelector('[data-carte-edition]');
if (bloc) initialiser(bloc);

function initialiser(bloc) {
  const conteneur = bloc.querySelector('[data-carte]');
  const champLat = document.querySelector('#Latitude');
  const champLng = document.querySelector('#Longitude');
  const champContour = document.querySelector('#ContourGeoJson');
  const affichageSurface = bloc.querySelector('[data-surface]');
  const etat = bloc.querySelector('[data-etat-carte]');
  const boutons = Object.fromEntries([...bloc.querySelectorAll('[data-action]')].map((b) => [b.dataset.action, b]));
  const collage = bloc.querySelector('[data-collage]');

  bloc.hidden = false;
  let demarree = false;
  const demarrer = () => { if (!demarree) { demarree = true; lancer().catch(echec); } };
  quandVisible(conteneur, demarrer);
  boutons.charger?.addEventListener('click', demarrer);

  function echec() {
    demarree = false;
    etat.textContent = 'La carte n’a pas pu se charger (connexion ?). Vous pouvez saisir les coordonnées à la main.';
  }

  async function lancer() {
    etat.textContent = 'Chargement de la carte…';
    const L = await chargerLeaflet(bloc);
    boutons.charger?.remove();
    conteneur.classList.add('est-chargee');

    const fonds = creerFonds(L);
    const carte = L.map(conteneur, { zoomControl: true, attributionControl: true, tapTolerance: 20 });
    preparerAttribution(carte);
    L.control.layers({ Satellite: fonds.satellite, Plan: fonds.plan }, null, { position: 'topright', collapsed: false }).addTo(carte);
    L.control.scale({ imperial: false }).addTo(carte);

    // --- État : repère + contour --------------------------------------------
    let repere = null;
    let sommets = lireContour(champContour.value);
    let mode = 'point';
    const poly = L.polygon([], { color: '#E3A53A', weight: 3, fillColor: '#E3A53A', fillOpacity: 0.18 }).addTo(carte);
    const marqueursSommets = L.layerGroup().addTo(carte);

    const point = lirePoint();
    (point || sommets.length ? fonds.satellite : fonds.plan).addTo(carte);
    if (sommets.length >= 3) carte.fitBounds(L.latLngBounds(sommets), { maxZoom: 18, padding: [30, 30] });
    else if (point) carte.setView(point, 17);
    else carte.setView([14.45, -14.45], 7); // Sénégal entier
    if (point) placerRepere(point, false);
    dessinerContour(false);
    etat.textContent = '';

    // --- Repère ----------------------------------------------------------------
    function placerRepere(ll, ecrire = true) {
      if (!repere) {
        repere = L.marker(ll, { icon: iconeRepere(L), draggable: true, autoPan: true, keyboard: true, title: 'Position du terrain' }).addTo(carte);
        repere.on('dragend', () => ecrirePoint(repere.getLatLng()));
      } else repere.setLatLng(ll);
      if (ecrire) ecrirePoint(ll);
    }

    function ecrirePoint(ll) {
      champLat.value = ll.lat.toFixed(6);
      champLng.value = ll.lng.toFixed(6);
      champLat.dispatchEvent(new Event('input', { bubbles: true })); // formulaire marqué comme modifié
    }

    function lirePoint() {
      const lat = parseFloat(champLat.value.replace(',', '.'));
      const lng = parseFloat(champLng.value.replace(',', '.'));
      return Number.isFinite(lat) && Number.isFinite(lng) ? L.latLng(lat, lng) : null;
    }

    // Saisie manuelle ou bouton GPS du formulaire → le repère suit
    for (const champ of [champLat, champLng]) {
      champ.addEventListener('change', () => { const p = lirePoint(); if (p) { placerRepere(p, false); carte.setView(p, Math.max(carte.getZoom(), 17)); } });
    }
    champLat.addEventListener('input', () => {
      const p = lirePoint();
      if (p && (!repere || !repere.getLatLng().equals(p, 1e-7))) { placerRepere(p, false); carte.setView(p, Math.max(carte.getZoom(), 17)); }
    });

    // --- Contour ---------------------------------------------------------------
    function dessinerContour(ecrire = true) {
      poly.setLatLngs(sommets);
      marqueursSommets.clearLayers();
      sommets.forEach((s, i) => {
        const m = L.marker(s, {
          draggable: true,
          icon: L.divIcon({ className: 'sommet-carte', html: `<span>${i + 1}</span>`, iconSize: [26, 26] }),
          title: `Sommet ${i + 1} (déplaçable)`,
        }).addTo(marqueursSommets);
        m.on('drag', () => { sommets[i] = m.getLatLng(); poly.setLatLngs(sommets); afficherSurface(); });
        m.on('dragend', () => ecrireContour());
      });
      afficherSurface();
      boutons.annuler.disabled = sommets.length === 0;
      boutons.effacer.disabled = sommets.length === 0;
      if (ecrire) ecrireContour();
    }

    function afficherSurface() {
      const m2 = aireM2(sommets);
      affichageSurface.textContent = sommets.length >= 3
        ? `Surface du contour : ≈ ${surfaceLisible(m2)} (${sommets.length} sommets, calcul indicatif)`
        : sommets.length ? `${sommets.length} sommet${sommets.length > 1 ? 's' : ''} : encore ${3 - sommets.length} au minimum.` : '';
    }

    function ecrireContour() {
      champContour.value = sommets.length >= 3
        ? JSON.stringify({ type: 'Polygon', coordinates: [[...sommets, sommets[0]].map((s) => [+s.lng.toFixed(7), +s.lat.toFixed(7)])] })
        : '';
      champContour.dispatchEvent(new Event('input', { bubbles: true }));
    }

    function ajouterSommet(ll) {
      sommets.push(L.latLng(ll));
      dessinerContour();
    }

    // --- Modes et boutons ------------------------------------------------------
    function changerMode(m) {
      mode = m;
      boutons.point.setAttribute('aria-pressed', String(m === 'point'));
      boutons.parcelle.setAttribute('aria-pressed', String(m === 'parcelle'));
      conteneur.classList.toggle('mode-parcelle', m === 'parcelle');
      etat.textContent = m === 'parcelle'
        ? 'Touchez la carte à chaque coin de la parcelle (dans l’ordre). Les numéros se déplacent au doigt.'
        : 'Touchez la carte pour placer le terrain, ou déplacez le repère.';
    }

    carte.on('click', (e) => (mode === 'point' ? placerRepere(e.latlng) : ajouterSommet(e.latlng)));
    boutons.point.addEventListener('click', () => changerMode('point'));
    boutons.parcelle.addEventListener('click', () => changerMode('parcelle'));
    boutons.annuler.addEventListener('click', () => { sommets.pop(); dessinerContour(); });
    boutons.effacer.addEventListener('click', () => {
      if (sommets.length && confirm('Effacer tout le contour de la parcelle ?')) { sommets = []; dessinerContour(); }
    });
    boutons.recentrer?.addEventListener('click', () => {
      if (sommets.length >= 3) carte.fitBounds(L.latLngBounds(sommets), { maxZoom: 18, padding: [30, 30] });
      else if (repere) carte.setView(repere.getLatLng(), 18);
    });

    // Sommet à la position GPS actuelle : l'admin marche de borne en borne.
    if ('geolocation' in navigator && boutons.sommetGps) {
      boutons.sommetGps.hidden = false;
      boutons.sommetGps.addEventListener('click', () => {
        etat.textContent = 'Recherche de votre position…';
        boutons.sommetGps.disabled = true;
        navigator.geolocation.getCurrentPosition((pos) => {
          const ll = L.latLng(pos.coords.latitude, pos.coords.longitude);
          changerMode('parcelle');
          ajouterSommet(ll);
          carte.setView(ll, Math.max(carte.getZoom(), 18));
          const precision = Math.round(pos.coords.accuracy);
          etat.textContent = `Sommet ${sommets.length} ajouté (précision ± ${precision} m).` +
            (precision > 10 ? ' Précision moyenne : patientez quelques secondes sur la borne puis réessayez si besoin.' : ' Allez à la borne suivante.');
          boutons.sommetGps.disabled = false;
        }, () => {
          etat.textContent = 'Position introuvable : activez la localisation du téléphone.';
          boutons.sommetGps.disabled = false;
        }, { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 });
      });
    }

    // Coller des coordonnées ou un lien Google Maps
    collage?.addEventListener('change', () => {
      const c = lireCoordonnees(collage.value);
      if (!c) {
        etat.textContent = /goo\.gl|maps\.app/.test(collage.value)
          ? 'Les liens courts ne sont pas lisibles : ouvrez-le, puis copiez les chiffres de la position (ex. 14.5198, -17.0021).'
          : 'Coordonnées non reconnues. Exemple : 14.5198, -17.0021';
        return;
      }
      placerRepere(L.latLng(c.lat, c.lng));
      carte.setView([c.lat, c.lng], 17);
      collage.value = '';
      etat.textContent = 'Position placée depuis les coordonnées collées.';
    });

    changerMode('point');
    // Le conteneur a pu changer de taille pendant le chargement
    setTimeout(() => carte.invalidateSize(), 200);
  }
}

function lireContour(texte) {
  try {
    const g = JSON.parse(texte);
    const anneau = (g.type === 'Feature' ? g.geometry : g).coordinates[0].map(([lng, lat]) => ({ lat, lng }));
    const premier = anneau[0], dernier = anneau[anneau.length - 1];
    if (anneau.length > 1 && premier.lat === dernier.lat && premier.lng === dernier.lng) anneau.pop();
    return anneau.map((p) => window.L ? window.L.latLng(p.lat, p.lng) : p);
  } catch {
    return [];
  }
}
