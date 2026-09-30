// Onglet Visite 360° : envoi des panoramas (préparés sur le téléphone) et éditeur Pannellum
// pour régler la vue de départ et placer les points de passage au viseur central.
import { preparerPanorama, tailleLisible } from './compression.js';
import { FileEnvois, nouvelId } from './envois.js';
import { chargerBibliotheque } from '../chargeur.js';

const jeton = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

// =============================================================================
// Envoi des panoramas
// =============================================================================
const zone = document.querySelector('[data-panoramas]');
if (zone) {
  const liste = zone.querySelector('[data-envois]');
  const entree = zone.querySelector('input[type="file"]');
  const lignes = new Map();
  let preparations = 0;
  let reussis = 0;

  const file = new FileEnvois({
    file: `panoramas-${zone.dataset.terrain}`,
    jeton,
    surEtat: (e) => {
      const li = ligne(e);
      li.dataset.etat = e.etat;
      li.querySelector('.envoi__barre span').style.width = `${Math.round((e.progression ?? 0) * 100)}%`;
      li.querySelector('.envoi__relancer').hidden = !(e.etat === 'erreur' && !e.definitif);
      const msg = li.querySelector('.envoi__message');
      if (e.etat === 'envoi') msg.textContent = `Envoi… ${Math.round((e.progression ?? 0) * 100)} %`;
      else if (e.etat === 'reussi') { msg.textContent = 'Panorama ajouté.'; reussis++; terminer(); }
      else msg.textContent = e.message ?? 'En attente…';
    },
  });

  function ligne(e) {
    let li = lignes.get(e.id);
    if (li) return li;
    li = document.createElement('li');
    li.className = 'envoi';
    li.innerHTML = `<span class="envoi__corps"><span class="envoi__nom"></span><span class="envoi__barre"><span></span></span>
      <span class="envoi__message" aria-live="polite"></span></span>
      <button type="button" class="btn btn--contour btn--petit envoi__relancer" hidden>Réessayer</button>`;
    li.querySelector('.envoi__nom').textContent = e.nom ?? 'Panorama';
    li.querySelector('.envoi__relancer').addEventListener('click', () => file.relancer(e.id));
    liste.append(li);
    lignes.set(e.id, li);
    return li;
  }

  function terminer() {
    if (file.nombreEnAttente === 0 && preparations === 0 && reussis > 0) setTimeout(() => location.reload(), 700);
  }

  entree.addEventListener('change', async () => {
    const fichiers = [...entree.files];
    entree.value = '';
    preparations += fichiers.length;
    for (const f of fichiers) {
      const id = nouvelId();
      const li = ligne({ id, nom: f.name });
      li.querySelector('.envoi__message').textContent = 'Préparation (HD, version légère, vignette)…';
      try {
        const p = await preparerPanorama(f);
        li.remove();
        lignes.delete(id);
        await file.ajouter({
          id,
          url: zone.dataset.urlEnvoi,
          nom: `${f.name} · ${tailleLisible(f.size)} → HD ${tailleLisible(p.hd.size)} + léger ${tailleLisible(p.bd.size)}`,
          champs: { IdEnvoi: id, Titre: f.name.replace(/\.[^.]+$/, '').slice(0, 60) },
          fichiers: { hd: p.hd, bd: p.bd, vignette: p.vignette },
        });
      } catch (err) {
        li.dataset.etat = 'erreur';
        li.querySelector('.envoi__message').textContent = err.message === 'pas-panorama'
          ? 'Ce n’est pas un panorama : utilisez le mode « Panorama » ou « Photo sphère » de l’appareil photo (image au moins 2 fois plus large que haute).'
          : 'Image illisible ou trop grande pour cet appareil.';
      } finally {
        preparations--;
      }
    }
    terminer();
  });

  file.reprendre();
  window.addEventListener('beforeunload', (e) => { if (file.nombreEnAttente > 0) e.preventDefault(); });
}

// =============================================================================
// Éditeur : vue de départ et points de passage
// =============================================================================
const editeur = document.querySelector('[data-editeur-360]');
if (editeur) {
  const base = editeur.dataset.urlBase;
  const config = JSON.parse(editeur.querySelector('[data-config]').textContent);
  const viseur = editeur.querySelector('[data-viseur]');
  const choixScene = editeur.querySelector('[data-scene]');
  const listePoints = editeur.querySelector('[data-points]');
  const etat = editeur.querySelector('[data-etat]');
  const formPoint = editeur.querySelector('[data-form-point]');
  const cible = formPoint.querySelector('[name="cible"]');
  const texte = formPoint.querySelector('[name="texte"]');
  const lancer = editeur.querySelector('[data-lancer-editeur]');
  let viewer = null;

  const idPanorama = (scene) => Number(scene.slice(1));
  const sceneCourante = () => viewer?.getScene();

  async function envoyer(url, corps) {
    const rep = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json', RequestVerificationToken: jeton },
      body: JSON.stringify(corps ?? {}),
    });
    const donnees = rep.status === 204 ? null : await rep.json().catch(() => null);
    if (!rep.ok) throw new Error(donnees?.erreur ?? `Erreur ${rep.status}`);
    return donnees;
  }

  function afficherPoints() {
    const scene = sceneCourante();
    // Pannellum tient lui-même la liste à jour (addHotSpot / removeHotSpot) : on la lit, sans la dupliquer.
    const points = viewer.getConfig().hotSpots ?? [];
    listePoints.replaceChildren(...points.map((h) => {
      const li = document.createElement('li');
      const nom = document.createElement('span');
      nom.textContent = `${h.type === 'scene' ? 'Passage' : 'Info'} · ${h.text}`;
      const suppr = Object.assign(document.createElement('button'), { type: 'button', className: 'btn btn--discret btn--petit', textContent: 'Retirer' });
      suppr.addEventListener('click', async () => {
        try {
          await envoyer(`${base}/hotspots/${h.id}/supprimer`);
          viewer.removeHotSpot(h.id, scene);
          afficherPoints();
        } catch (e) { etat.textContent = e.message; }
      });
      li.append(nom, suppr);
      return li;
    }));
    if (!points.length) listePoints.innerHTML = '<li class="muet">Aucun point dans cette vue.</li>';
    // Destinations possibles : toutes les autres scènes
    cible.replaceChildren(...Object.entries(config.scenes).filter(([id]) => id !== scene)
      .map(([id, s]) => Object.assign(document.createElement('option'), { value: id, textContent: s.title })));
    choixScene.value = scene;
  }

  async function demarrer() {
    if (viewer) return;
    lancer.disabled = true;
    etat.textContent = 'Chargement de la visite…';
    try {
      const pannellum = await chargerBibliotheque(editeur.dataset.pannellumCss, editeur.dataset.pannellumJs, 'pannellum');
      lancer.remove();
      viseur.hidden = false;
      viewer = pannellum.viewer(viseur.querySelector('[data-viewer]'), { ...config, default: { ...config.default, hotSpotDebug: false } });
      viewer.on('scenechange', () => setTimeout(afficherPoints, 50));
      viewer.on('load', () => { afficherPoints(); etat.textContent = 'Tournez la vue pour placer le viseur sur l’endroit voulu.'; });
      editeur.querySelector('[data-outils]').hidden = false;
    } catch {
      lancer.disabled = false;
      etat.textContent = 'Chargement impossible (connexion ?). Réessayez.';
    }
  }

  lancer.addEventListener('click', demarrer);

  choixScene.addEventListener('change', () => viewer?.loadScene(choixScene.value));

  editeur.querySelector('[data-action="vue"]').addEventListener('click', async () => {
    const scene = sceneCourante();
    const vue = { yaw: viewer.getYaw(), pitch: viewer.getPitch(), hfov: viewer.getHfov() };
    try {
      await envoyer(`${base}/panoramas/${idPanorama(scene)}/vue`, vue);
      Object.assign(config.scenes[scene], vue);
      etat.textContent = `Vue de départ enregistrée pour « ${config.scenes[scene].title} ».`;
    } catch (e) { etat.textContent = e.message; }
  });

  formPoint.addEventListener('submit', async (e) => {
    e.preventDefault();
    const scene = sceneCourante();
    const type = e.submitter?.value === 'info' ? 'info' : 'scene';
    if (type === 'scene' && !cible.value) { etat.textContent = 'Ajoutez d’abord un autre panorama pour créer un passage.'; return; }
    if (type === 'info' && !texte.value.trim()) { texte.focus(); etat.textContent = 'Saisissez le texte de l’information.'; return; }
    const point = { pitch: viewer.getPitch(), yaw: viewer.getYaw(), type: type === 'scene' ? 'Navigation' : 'Information',
      cibleId: type === 'scene' ? idPanorama(cible.value) : null, texte: texte.value.trim() || null };
    try {
      const { id } = await envoyer(`${base}/panoramas/${idPanorama(scene)}/hotspots`, point);
      const hotspot = type === 'scene'
        ? { id, pitch: point.pitch, yaw: point.yaw, type: 'scene', sceneId: cible.value, text: point.texte ?? `Vers : ${config.scenes[cible.value].title}` }
        : { id, pitch: point.pitch, yaw: point.yaw, type: 'info', text: point.texte };
      viewer.addHotSpot(hotspot, scene);
      texte.value = '';
      afficherPoints();
      etat.textContent = 'Point ajouté.';
    } catch (err) { etat.textContent = err.message; }
  });
}
