// Barres fixées en bas (navigation, bouton WhatsApp, Enregistrer) : masquées pendant la saisie au clavier.
// Sur mobile, le clavier virtuel les fait sinon remonter et sauter au milieu de l'écran.
const tactile = matchMedia('(pointer: coarse)').matches;
const champTexte = (el) => el?.matches?.('input:not([type=checkbox]):not([type=radio]):not([type=range]):not([type=file]):not([type=button]):not([type=submit]), textarea, select, [contenteditable="true"]');

if (tactile) {
  const racine = document.documentElement;
  document.addEventListener('focusin', (e) => { if (champTexte(e.target)) racine.classList.add('clavier-ouvert'); });
  document.addEventListener('focusout', () => {
    // Laisse le temps au focus de passer d'un champ à l'autre sans faire clignoter la barre
    setTimeout(() => { if (!champTexte(document.activeElement)) racine.classList.remove('clavier-ouvert'); }, 120);
  });
}
