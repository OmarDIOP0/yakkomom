// Compléments PWA : indicateur hors ligne, version enregistrée, bannière d'installation.
const $ = (html) => { const t = document.createElement('template'); t.innerHTML = html.trim(); return t.content.firstElementChild; };
const lire = (cle, defaut) => { try { return JSON.parse(localStorage.getItem(cle)) ?? defaut; } catch { return defaut; } };
const ecrire = (cle, valeur) => { try { localStorage.setItem(cle, JSON.stringify(valeur)); } catch { /* ignoré */ } };
// Nom configuré dans l'admin (repris de l'en-tête de la page)
const nomSite = document.querySelector('meta[name="apple-mobile-web-app-title"]')?.content || 'Yakkomom';
const autonome = matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;

// --- Petits messages en bas d'écran ---------------------------------------------------
function message(texte, action) {
  document.querySelector('.message-pwa')?.remove();
  const el = $(`<div class="message-pwa" role="status"><span></span></div>`);
  el.querySelector('span').textContent = texte;
  if (action) {
    const b = $(`<button type="button" class="btn btn--petit btn--accent"></button>`);
    b.textContent = action.libelle;
    b.addEventListener('click', action.faire);
    el.append(b);
  }
  const fermer = $(`<button type="button" class="message-pwa__fermer" aria-label="Fermer">×</button>`);
  fermer.addEventListener('click', () => el.remove());
  el.append(fermer);
  document.body.append(el);
  return el;
}

// --- Hors ligne ------------------------------------------------------------------------
const indicateur = $(`<div class="hors-ligne" role="status" hidden><span class="hors-ligne__point" aria-hidden="true"></span>Vous êtes hors ligne</div>`);
document.body.append(indicateur);
const majConnexion = () => { indicateur.hidden = navigator.onLine; };
window.addEventListener('online', majConnexion);
window.addEventListener('offline', majConnexion);
majConnexion();

// --- Page servie depuis la version enregistrée (réveil du serveur, réseau lent) -------------
const nav = performance.getEntriesByType('navigation')[0];
if (nav?.serverTiming?.some((t) => t.name === 'yk-cache')) {
  message(navigator.onLine ? 'Version enregistrée affichée. Mise à jour en cours…' : 'Version enregistrée (hors ligne).');
}
navigator.serviceWorker?.addEventListener('message', (e) => {
  if (e.data?.type === 'yk:page-a-jour') message('Une version plus récente de cette page est prête.', { libelle: 'Actualiser', faire: () => location.reload() });
});

// --- Bannière d'installation, au bon moment ---------------------------------------------------
const visites = lire('yk.visites', 0) + (sessionStorage.getItem('yk.session') ? 0 : 1);
sessionStorage.setItem('yk.session', '1');
ecrire('yk.visites', visites);
const refusJusqua = lire('yk.installation-refusee', 0);
const fichesVues = lire('yk.vus', []).length;
const bonMoment = () => !autonome && Date.now() > refusJusqua && (visites >= 2 || fichesVues >= 2);
const iOS = /iphone|ipad|ipod/i.test(navigator.userAgent) && !window.MSStream;

function refuser(el) {
  ecrire('yk.installation-refusee', Date.now() + 30 * 24 * 3600 * 1000);
  el.remove();
}

let invitation = null;
window.addEventListener('beforeinstallprompt', (e) => {
  e.preventDefault(); // pas de bannière du navigateur à l'arrivée : on choisit le moment
  invitation = e;
  if (bonMoment()) setTimeout(proposer, 15000);
});
window.addEventListener('appinstalled', () => { document.querySelector('.installation')?.remove(); ecrire('yk.installation-refusee', Date.now() + 3650 * 24 * 3600 * 1000); });

function proposer() {
  if (!invitation || document.querySelector('.installation')) return;
  const el = $(`<aside class="installation" aria-label="Installer l'application">
    <img src="${document.querySelector('link[rel=apple-touch-icon]')?.href ?? ''}" alt="" width="48" height="48">
    <div><strong>Installer ${nomSite}</strong><span>Accès direct depuis l'écran d'accueil, même avec une connexion faible.</span></div>
    <button type="button" class="btn btn--accent btn--petit" data-installer>Installer</button>
    <button type="button" class="message-pwa__fermer" aria-label="Plus tard" data-plus-tard>×</button></aside>`);
  el.querySelector('[data-installer]').addEventListener('click', async () => {
    el.remove();
    invitation.prompt();
    const { outcome } = await invitation.userChoice;
    if (outcome !== 'accepted') ecrire('yk.installation-refusee', Date.now() + 30 * 24 * 3600 * 1000);
    invitation = null;
  });
  el.querySelector('[data-plus-tard]').addEventListener('click', () => refuser(el));
  document.body.append(el);
}

// iPhone : pas d'invitation automatique possible, on explique le geste (Safari uniquement)
if (iOS && bonMoment() && /safari/i.test(navigator.userAgent) && !/crios|fxios/i.test(navigator.userAgent)) {
  setTimeout(() => {
    const el = $(`<aside class="installation" aria-label="Installer l'application">
      <img src="${document.querySelector('link[rel=apple-touch-icon]')?.href ?? ''}" alt="" width="48" height="48">
      <div><strong>Ajoutez ${nomSite} à votre écran d'accueil</strong>
      <span>Touchez <svg class="ic ic--partage-ios" viewBox="0 0 24 24" aria-label="Partager"><path d="M12 3v12M8 7l4-4 4 4M5 11v9h14v-9"/></svg> puis « Sur l'écran d'accueil ».</span></div>
      <button type="button" class="message-pwa__fermer" aria-label="Plus tard" data-plus-tard>×</button></aside>`);
    el.querySelector('[data-plus-tard]').addEventListener('click', () => refuser(el));
    document.body.append(el);
  }, 15000);
}

// --- Bouton permanent « Installer l'application » (pied de page) ------------------------------
// La bannière ci-dessus attend la 2e visite ; ce bouton permet d'installer à tout moment.
const boutonInstaller = document.querySelector('[data-installer-app]');
if (boutonInstaller && !autonome) {
  boutonInstaller.hidden = false;
  boutonInstaller.addEventListener('click', async () => {
    if (invitation) {
      invitation.prompt();
      const { outcome } = await invitation.userChoice;
      invitation = null;
      if (outcome === 'accepted') boutonInstaller.hidden = true;
      return;
    }
    // Pas d'invitation disponible : on explique le geste propre au navigateur.
    message(iOS
      ? 'Dans Safari, touchez le bouton Partager (carré avec une flèche), puis « Sur l\'écran d\'accueil ».'
      : 'Ouvrez le menu du navigateur (⋮ en haut à droite), puis « Installer l\'application » ou « Ajouter à l\'écran d\'accueil ».');
  });
  window.addEventListener('appinstalled', () => { boutonInstaller.hidden = true; });
}
