// Formulaires admin : amélioration progressive (tout fonctionne sans JavaScript).
const formulaires = document.querySelectorAll('form[data-formulaire-suivi]');

for (const form of formulaires) {
  let modifie = false;
  let envoi = false;

  form.addEventListener('input', () => { modifie = true; });
  form.addEventListener('change', () => { modifie = true; });

  // Pas de double envoi sur une connexion lente
  form.addEventListener('submit', (e) => {
    if (envoi) { e.preventDefault(); return; }
    envoi = true;
    const bouton = e.submitter;
    for (const b of form.querySelectorAll('button[type="submit"]')) b.setAttribute('aria-disabled', 'true');
    if (bouton) {
      // Le bouton cliqué doit rester dans la requête (name="Suite") : on ne le désactive pas réellement.
      bouton.dataset.texte = bouton.textContent;
      bouton.textContent = 'Enregistrement…';
    }
  });

  // Quitter la page avec des modifications non enregistrées
  window.addEventListener('beforeunload', (e) => {
    if (modifie && !envoi) e.preventDefault();
  });
}

// Montants : « 12500000 » → « 12 500 000 » en quittant le champ
for (const champ of document.querySelectorAll('input[data-format-montant]')) {
  champ.addEventListener('blur', () => {
    const chiffres = champ.value.replace(/\D/g, '');
    if (chiffres) champ.value = Number(chiffres).toLocaleString('fr-FR').replace(/ /g, ' ');
  });
}

// Position GPS actuelle (l'admin est sur le terrain)
const boutonGps = document.querySelector('[data-position-gps]');
if (boutonGps && 'geolocation' in navigator) {
  const etat = document.querySelector('[data-position-gps-etat]');
  const lat = document.querySelector('#Latitude');
  const lng = document.querySelector('#Longitude');
  boutonGps.hidden = false;

  boutonGps.addEventListener('click', () => {
    etat.textContent = 'Recherche de la position… (restez à découvert, loin des murs)';
    boutonGps.setAttribute('aria-disabled', 'true');
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        lat.value = pos.coords.latitude.toFixed(6);
        lng.value = pos.coords.longitude.toFixed(6);
        lat.dispatchEvent(new Event('input', { bubbles: true }));
        const precision = Math.round(pos.coords.accuracy);
        etat.textContent = `Position enregistrée dans le formulaire (précision ± ${precision} m).` +
          (precision > 30 ? ' Précision faible : réessayez dans quelques secondes.' : '');
        boutonGps.removeAttribute('aria-disabled');
      },
      async (err) => {
        const { expliquerErreurPosition } = await import('./position.js');
        etat.textContent = await expliquerErreurPosition(err);
        boutonGps.removeAttribute('aria-disabled');
      },
      { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
    );
  });
}
