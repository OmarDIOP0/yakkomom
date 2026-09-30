// Documents fonciers : envoi avec progression et reprise. Les PDF partent tels quels ;
// les photos de documents trop lourdes sont allégées (2400 px, texte encore bien lisible).
import { compresserImage, tailleLisible } from './compression.js';
import { FileEnvois, nouvelId } from './envois.js';

const form = document.querySelector('[data-form-document]');
if (form) initialiser(form);

function initialiser(form) {
  const bloc = form.closest('[data-documents]');
  const jeton = form.querySelector('input[name="__RequestVerificationToken"]').value;
  const liste = form.querySelector('[data-envois]');
  const entree = form.querySelector('input[type="file"]');
  const lignes = new Map();

  const file = new FileEnvois({
    file: `documents-${bloc.dataset.terrain}`,
    jeton,
    surEtat: (e) => {
      let li = lignes.get(e.id);
      if (!li) {
        li = document.createElement('li');
        li.className = 'envoi';
        li.innerHTML = `<span class="envoi__corps"><span class="envoi__nom"></span>
          <span class="envoi__barre"><span></span></span><span class="envoi__message" aria-live="polite"></span></span>
          <button type="button" class="btn btn--contour btn--petit envoi__relancer" hidden>Réessayer</button>`;
        li.querySelector('.envoi__nom').textContent = e.nom ?? 'Document';
        li.querySelector('.envoi__relancer').addEventListener('click', () => file.relancer(e.id));
        liste.append(li);
        lignes.set(e.id, li);
      }
      li.dataset.etat = e.etat;
      li.querySelector('.envoi__barre span').style.width = `${Math.round((e.progression ?? 0) * 100)}%`;
      li.querySelector('.envoi__relancer').hidden = !(e.etat === 'erreur' && !e.definitif);
      const msg = li.querySelector('.envoi__message');
      if (e.etat === 'envoi') msg.textContent = `Envoi… ${Math.round((e.progression ?? 0) * 100)} %`;
      else if (e.etat === 'reussi') {
        msg.textContent = 'Document ajouté.';
        if (file.nombreEnAttente === 0) setTimeout(() => location.reload(), 700);
      } else msg.textContent = e.message ?? 'En attente…';
    },
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const fichier = entree.files[0];
    if (!fichier) { entree.focus(); return; }

    let blob = fichier;
    let nom = `${fichier.name} · ${tailleLisible(fichier.size)}`;
    if (fichier.type.startsWith('image/') && fichier.size > 1.5 * 1024 * 1024) {
      try {
        const r = await compresserImage(fichier, { largeurMax: 2400, qualite: 0.85, vignette: 0 });
        blob = r.blob;
        nom += ` → ${tailleLisible(blob.size)}`;
      } catch { /* on envoie l'original */ }
    }
    // Conserver le nom d'origine (affiché dans l'admin ; le fichier est stocké sous un nom aléatoire)
    const nomFichier = blob === fichier ? fichier.name : fichier.name.replace(/\.[^.]+$/, '') + (blob.type === 'image/webp' ? '.webp' : '.jpg');
    const donnees = new FormData(form);
    const id = nouvelId();
    await file.ajouter({
      id,
      url: form.action,
      nom,
      champs: { IdEnvoi: id, Type: donnees.get('Type'), Titre: donnees.get('Titre') || null, EstPublic: donnees.get('EstPublic') ? 'true' : 'false' },
      fichiers: { fichier: new File([blob], nomFichier, { type: blob.type }) },
    });
    form.reset();
  });

  file.reprendre();
  window.addEventListener('beforeunload', (e) => { if (file.nombreEnAttente > 0) e.preventDefault(); });
}
