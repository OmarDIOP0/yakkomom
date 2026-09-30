// Page Favoris : affichage depuis le téléphone (fonctionne hors ligne), puis mise à jour
// des statuts et prix auprès du serveur quand la connexion le permet.
import { lireFavoris, ecrireFavoris } from '../site.js';

const grille = document.querySelector('[data-favoris-grille]');
const vide = document.querySelector('[data-favoris-vide]');
const modele = document.querySelector('[data-modele-carte]');
const info = document.querySelector('[data-favoris-maj]');

function afficher(liste) {
  grille.replaceChildren();
  vide.hidden = liste.length > 0;
  for (const f of liste) {
    const carte = modele.content.firstElementChild.cloneNode(true);
    const media = carte.querySelector('.carte-terrain__media');
    media.style.setProperty('--couleur-dominante', f.couleur || 'var(--c-sable-fonce)');
    const img = carte.querySelector('img');
    if (f.image) { img.src = f.image; img.alt = f.titre; } else img.remove();
    carte.querySelector('.carte-terrain__ref').textContent = f.reference;
    if (f.prix) {
      const bloc = carte.querySelector('.carte-terrain__prix');
      bloc.hidden = false;
      bloc.querySelector('[data-prix]').textContent = f.prix;
    }
    if (f.vendu) {
      carte.classList.add('carte-terrain--vendu');
      carte.querySelector('.carte-terrain__bandeau').hidden = false;
    }
    const statut = carte.querySelector('[data-statut]');
    statut.textContent = f.statut ?? '';
    statut.classList.add(f.vendu ? 'statut--vendu' : f.statut === 'Réservé' ? 'statut--reserve' : 'statut--disponible');
    const lien = carte.querySelector('.carte-terrain__titre a');
    lien.href = f.url;
    lien.textContent = f.titre;
    carte.querySelector('.carte-terrain__lieu').textContent = [f.surface, f.lieu].filter(Boolean).join(' · ');
    const bouton = carte.querySelector('.carte-terrain__fav');
    Object.assign(bouton.dataset, { favori: f.reference, titre: f.titre, url: f.url, image: f.image ?? '', prix: f.prix ?? '', surface: f.surface ?? '', lieu: f.lieu ?? '', couleur: f.couleur ?? '' });
    bouton.setAttribute('aria-label', `Retirer ${f.reference} des favoris`);
    grille.append(carte);
  }
}

async function actualiser() {
  const liste = lireFavoris();
  if (!liste.length || !navigator.onLine) return;
  try {
    const rep = await fetch(`/api/terrains/cartes?refs=${encodeURIComponent(liste.map((f) => f.reference).join(','))}`, { headers: { Accept: 'application/json' } });
    if (!rep.ok) return;
    const frais = new Map((await rep.json()).map((c) => [c.reference, c]));
    let retires = 0;
    const maj = liste.flatMap((f) => {
      const c = frais.get(f.reference);
      if (!c) { retires++; return []; } // retiré de la vente
      return [{ ...f, titre: c.titre, url: c.url, prix: c.prix, surface: c.surface, lieu: c.lieu, image: c.image ?? f.image, couleur: c.couleur ?? f.couleur, statut: c.statut, vendu: c.vendu }];
    });
    ecrireFavoris(maj);
    afficher(maj);
    if (retires) {
      info.hidden = false;
      info.textContent = `${retires} terrain${retires > 1 ? 's ont été retirés' : ' a été retiré'} de la vente et de vos favoris.`;
    }
  } catch { /* hors ligne : on garde l'affichage local */ }
}

afficher(lireFavoris());
document.addEventListener('yk:favoris', (e) => afficher(e.detail));
actualiser();
