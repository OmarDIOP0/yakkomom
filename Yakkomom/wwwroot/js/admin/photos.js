// Onglet Photos : sélection / glisser-déposer, compression, envoi avec progression,
// reprise après coupure, réorganisation par glisser-déposer, légendes enregistrées automatiquement.
import { compresserImage, tailleLisible } from './compression.js';
import { FileEnvois, nouvelId } from './envois.js';

const zone = document.querySelector('[data-photos]');
if (zone) initialiser(zone);

function initialiser(zone) {
  const jeton = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
  const urlEnvoi = zone.dataset.urlEnvoi;
  const urlOrdre = zone.dataset.urlOrdre;
  const grille = zone.querySelector('[data-grille]');
  const listeEnvois = zone.querySelector('[data-envois]');
  const depot = zone.querySelector('[data-depot]');
  const entrees = zone.querySelectorAll('input[type="file"][data-selection]');
  const annonce = zone.querySelector('[data-annonce]');
  let aActualiser = false;
  let compressionsEnCours = 0;

  zone.querySelector('[data-sans-js]')?.remove();
  zone.classList.add('photos--js');

  // --- File d'envoi -------------------------------------------------------
  const lignes = new Map();
  const file = new FileEnvois({
    file: zone.dataset.file ?? `photos-${zone.dataset.terrain}`,
    jeton,
    surEtat: (e) => afficherEtat(e),
  });

  function ligneEnvoi(e) {
    let li = lignes.get(e.id);
    if (!li) {
      li = document.createElement('li');
      li.className = 'envoi';
      li.innerHTML = `
        <span class="envoi__apercu"></span>
        <span class="envoi__corps">
          <span class="envoi__nom"></span>
          <span class="envoi__barre"><span></span></span>
          <span class="envoi__message" aria-live="polite"></span>
        </span>
        <button type="button" class="btn btn--contour btn--petit envoi__relancer" hidden>Réessayer</button>`;
      const apercu = e.fichiers?.vignette ?? e.fichiers?.fichier;
      if (apercu) {
        const img = document.createElement('img');
        img.alt = '';
        img.src = URL.createObjectURL(apercu);
        img.onload = () => URL.revokeObjectURL(img.src);
        li.querySelector('.envoi__apercu').append(img);
      }
      li.querySelector('.envoi__nom').textContent = e.nom ?? 'Photo';
      li.querySelector('.envoi__relancer').addEventListener('click', () => file.relancer(e.id));
      listeEnvois.append(li);
      lignes.set(e.id, li);
    }
    return li;
  }

  function afficherEtat(e) {
    const li = ligneEnvoi(e);
    li.dataset.etat = e.etat;
    li.querySelector('.envoi__barre span').style.width = `${Math.round((e.progression ?? 0) * 100)}%`;
    const message = li.querySelector('.envoi__message');
    const relancer = li.querySelector('.envoi__relancer');
    relancer.hidden = !(e.etat === 'erreur' && !e.definitif);

    switch (e.etat) {
      case 'attente': message.textContent = e.message ?? 'En attente…'; break;
      case 'hors-ligne': message.textContent = e.message; break;
      case 'envoi': message.textContent = `Envoi… ${Math.round((e.progression ?? 0) * 100)} %`; break;
      case 'erreur': message.textContent = e.message; break;
      case 'reussi':
        message.textContent = 'Envoyée';
        ajouterCarte(e.reponse);
        aActualiser = true;
        setTimeout(() => { li.remove(); lignes.delete(e.id); terminerSiVide(); }, 1200);
        break;
    }
  }

  /** Aperçu immédiat de la photo reçue ; la page est rechargée quand tout est envoyé (boutons complets). */
  function ajouterCarte(photo) {
    if (!photo || grille.querySelector(`[data-id="${photo.id}"]`)) return;
    grille.querySelector('[data-vide]')?.remove();
    const li = document.createElement('li');
    li.className = 'photo photo--nouvelle';
    li.dataset.id = photo.id;
    li.innerHTML = `<div class="photo__media" style="background:${photo.couleurDominante ?? 'var(--c-sable-fonce)'}">
      <img src="${photo.urlVignette}" alt="" width="${photo.largeur}" height="${photo.hauteur}" loading="lazy">
      ${photo.estCouverture ? '<span class="photo__couverture">Couverture</span>' : ''}</div>`;
    grille.append(li);
  }

  function terminerSiVide() {
    // Ne pas recharger tant qu'une photo est encore en compression (pas encore dans la file).
    if (file.nombreEnAttente > 0 || compressionsEnCours > 0 || !aActualiser) return;
    annonce.textContent = 'Toutes les photos sont envoyées.';
    // Recharger pour afficher les boutons (couverture, ordre, légende) — sauf si l'admin est en train d'écrire.
    if (!document.activeElement?.matches('input, textarea')) setTimeout(() => location.reload(), 600);
  }

  // --- Sélection des fichiers --------------------------------------------
  async function traiter(fichiers) {
    const images = [...fichiers].filter((f) => f.type.startsWith('image/') || /\.(heic|heif)$/i.test(f.name));
    compressionsEnCours += images.length;
    for (const f of images) {
      const id = nouvelId();
      afficherEtat({ id, nom: f.name, etat: 'attente', message: 'Compression…' });
      try {
        const r = await compresserImage(f, { largeurMax: 1600, qualite: 0.8, vignette: 480 });
        lignes.get(id)?.remove();
        lignes.delete(id);
        await file.ajouter({
          id,
          url: urlEnvoi,
          nom: `${f.name} · ${tailleLisible(f.size)} → ${tailleLisible(r.blob.size)}`,
          champs: { IdEnvoi: id, CouleurDominante: r.couleur },
          fichiers: { fichier: r.blob, vignette: r.vignette },
        });
      } catch {
        afficherEtat({ id, nom: f.name, etat: 'erreur', definitif: true,
          message: 'Image illisible sur cet appareil. Sur iPhone, choisissez le format « Le plus compatible » (JPEG) dans Réglages > Appareil photo > Formats.' });
      } finally {
        compressionsEnCours--;
      }
    }
    terminerSiVide();
  }

  for (const entree of entrees) {
    entree.addEventListener('change', () => { traiter(entree.files); entree.value = ''; });
  }

  // Glisser-déposer de fichiers (ordinateur)
  ['dragenter', 'dragover'].forEach((t) => depot.addEventListener(t, (e) => {
    if (!e.dataTransfer?.types.includes('Files')) return;
    e.preventDefault();
    depot.classList.add('est-survole');
  }));
  ['dragleave', 'drop'].forEach((t) => depot.addEventListener(t, () => depot.classList.remove('est-survole')));
  depot.addEventListener('drop', (e) => {
    if (!e.dataTransfer?.files.length) return;
    e.preventDefault();
    traiter(e.dataTransfer.files);
  });

  file.reprendre();
  window.addEventListener('beforeunload', (e) => { if (file.nombreEnAttente > 0) e.preventDefault(); });

  // --- Réorganisation par glisser-déposer (souris et tactile) --------------
  let deplace = null;
  let ordreInitial = '';

  grille.addEventListener('pointerdown', (e) => {
    const poignee = e.target.closest('[data-poignee]');
    if (!poignee) return;
    e.preventDefault();
    deplace = poignee.closest('li[data-id]');
    ordreInitial = ordreActuel().join(',');
    deplace.classList.add('est-deplace');
    try { poignee.setPointerCapture(e.pointerId); } catch { /* pointeur déjà relâché */ }
  });

  grille.addEventListener('pointermove', (e) => {
    if (!deplace) return;
    const cible = document.elementFromPoint(e.clientX, e.clientY)?.closest('li[data-id]');
    if (cible && cible !== deplace && cible.parentElement === grille) {
      const r = cible.getBoundingClientRect();
      const apres = e.clientY > r.bottom - r.height / 3 || (e.clientY > r.top + r.height / 3 && e.clientX > r.left + r.width / 2);
      grille.insertBefore(deplace, apres ? cible.nextSibling : cible);
    }
    // Défilement automatique près des bords de l'écran
    if (e.clientY < 80) window.scrollBy(0, -12);
    else if (e.clientY > innerHeight - 140) window.scrollBy(0, 12);
  });

  const finDeplacement = async () => {
    if (!deplace) return;
    deplace.classList.remove('est-deplace');
    deplace = null;
    const ordre = ordreActuel();
    if (ordre.join(',') === ordreInitial) return;
    annonce.textContent = 'Enregistrement de l’ordre…';
    try {
      const rep = await fetch(urlOrdre, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json', RequestVerificationToken: jeton },
        body: JSON.stringify({ ids: ordre }),
      });
      if (!rep.ok) throw new Error();
      annonce.textContent = 'Ordre enregistré.';
      numeroter();
    } catch {
      annonce.textContent = 'L’ordre n’a pas pu être enregistré. La page va se recharger.';
      setTimeout(() => location.reload(), 1500);
    }
  };
  grille.addEventListener('pointerup', finDeplacement);
  grille.addEventListener('pointercancel', finDeplacement);

  function ordreActuel() {
    return [...grille.querySelectorAll('li[data-id]')].map((li) => Number(li.dataset.id));
  }
  function numeroter() {
    grille.querySelectorAll('li[data-id] [data-numero]').forEach((n, i) => { n.textContent = i + 1; });
  }

  // --- Légendes : enregistrement automatique en quittant le champ ----------
  grille.addEventListener('change', async (e) => {
    const champ = e.target.closest('input[name="legende"]');
    if (!champ) return;
    const form = champ.form;
    const etat = form.querySelector('[data-etat-legende]');
    try {
      const rep = await fetch(form.action, {
        method: 'POST',
        headers: { Accept: 'application/json', RequestVerificationToken: jeton },
        body: new FormData(form),
      });
      if (!rep.ok) throw new Error();
      etat.textContent = 'Enregistrée';
    } catch {
      etat.textContent = 'Non enregistrée — réessayez';
    }
  });
  grille.addEventListener('submit', (e) => {
    // Avec JavaScript, la légende s'enregistre seule : Entrée ne recharge pas la page.
    if (e.target.matches('[data-form-legende]')) {
      e.preventDefault();
      e.target.querySelector('input[name="legende"]').dispatchEvent(new Event('change', { bubbles: true }));
    }
  });
}
