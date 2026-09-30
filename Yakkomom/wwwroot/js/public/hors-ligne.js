// Page hors ligne : terrains consultés et favoris, lus sur l'appareil.
// Les fiches déjà vues s'ouvrent depuis le cache du service worker.
const lire = (cle) => { try { return JSON.parse(localStorage.getItem(cle)) ?? []; } catch { return []; } };

async function remplir(liste, conteneur, bloc) {
  const pages = await caches?.open('yk-pages').catch(() => null);
  for (const t of liste) {
    const enCache = pages ? !!(await pages.match(t.url, { ignoreVary: true })) : false;
    const li = document.createElement('li');
    const a = document.createElement('a');
    a.href = t.url;
    a.className = 'ligne-hors-ligne' + (enCache ? '' : ' ligne-hors-ligne--indisponible');
    if (t.image) {
      const img = new Image(96, 72);
      img.src = t.image;
      img.alt = '';
      img.style.background = t.couleur || '';
      img.onerror = () => img.replaceWith(Object.assign(document.createElement('span'), { className: 'ligne-hors-ligne__vide' }));
      a.append(img);
    } else a.append(Object.assign(document.createElement('span'), { className: 'ligne-hors-ligne__vide' }));
    const texte = document.createElement('span');
    const titre = Object.assign(document.createElement('strong'), { textContent: t.titre });
    const details = Object.assign(document.createElement('small'), {
      textContent: [t.prix ? `${t.prix} FCFA` : null, t.surface, t.lieu].filter(Boolean).join(' · ') + (enCache ? '' : ' · disponible avec le réseau'),
    });
    texte.append(titre, details);
    a.append(texte);
    li.append(a);
    conteneur.append(li);
  }
  bloc.hidden = liste.length === 0;
}

const vus = lire('yk.vus');
const favoris = lire('yk.favoris');
await remplir(vus, document.querySelector('[data-vus]'), document.querySelector('[data-bloc-vus]'));
await remplir(favoris, document.querySelector('[data-favoris-hl]'), document.querySelector('[data-bloc-favoris]'));
document.querySelector('[data-rien]').hidden = vus.length + favoris.length > 0;

document.querySelector('[data-reessayer]').addEventListener('click', () => location.reload());
window.addEventListener('online', () => location.reload());
