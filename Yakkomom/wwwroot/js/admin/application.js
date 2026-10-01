// Application « Yakkomom Admin » : installation sur l'écran d'accueil de l'équipe.
// Le service worker est le même que celui du site ; il ne met jamais l'admin en cache (données toujours à jour).

import '../clavier.js';

const autonome = matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
const iOS = /iphone|ipad|ipod/i.test(navigator.userAgent);

if ('serviceWorker' in navigator && (location.protocol === 'https:' || location.hostname === 'localhost')) {
  navigator.serviceWorker.register('/sw.js').catch(() => { /* mode privé, navigateur ancien */ });
}

let invitation = null;
window.addEventListener('beforeinstallprompt', (e) => {
  e.preventDefault(); // l'installation se fait par nos boutons, pas par une bannière au milieu du travail
  invitation = e;
});

function message(texte) {
  document.querySelector('.message-pwa')?.remove();
  const el = document.createElement('div');
  el.className = 'message-pwa';
  el.setAttribute('role', 'status');
  const span = document.createElement('span');
  span.textContent = texte;
  const fermer = document.createElement('button');
  fermer.type = 'button';
  fermer.className = 'message-pwa__fermer';
  fermer.setAttribute('aria-label', 'Fermer');
  fermer.textContent = '×';
  fermer.addEventListener('click', () => el.remove());
  el.append(span, fermer);
  document.body.append(el);
}

const boutons = [...document.querySelectorAll('[data-installer-admin]')];
const cacher = () => boutons.forEach((b) => { (b.closest('[data-installer-conteneur]') ?? b).hidden = true; });

if (autonome) {
  cacher();
} else {
  for (const bouton of boutons) {
    (bouton.closest('[data-installer-conteneur]') ?? bouton).hidden = false;
    bouton.addEventListener('click', async () => {
      if (invitation) {
        invitation.prompt();
        const { outcome } = await invitation.userChoice;
        invitation = null;
        if (outcome === 'accepted') cacher();
        return;
      }
      message(iOS
        ? 'Dans Safari : bouton Partager (carré avec une flèche), puis « Sur l\'écran d\'accueil ». L\'icône « Yakkomom Admin » s\'ouvrira directement sur l\'administration.'
        : 'Menu du navigateur (⋮ en haut à droite), puis « Installer l\'application » ou « Ajouter à l\'écran d\'accueil ».');
    });
  }
  window.addEventListener('appinstalled', cacher);
}
