// Explication claire quand la position GPS ne peut pas être lue (admin, sur le terrain).

const iOS = /iphone|ipad|ipod/i.test(navigator.userAgent);
const android = /android/i.test(navigator.userAgent);
const integre = /FBAN|FBAV|Instagram|WhatsApp|; wv\)/i.test(navigator.userAgent);

/** Message adapté à l'erreur de géolocalisation et à l'appareil. */
export async function expliquerErreurPosition(err) {
  if (!window.isSecureContext) return 'La position n\'est disponible qu\'en https:// : ouvrez le site par son adresse sécurisée.';
  if (integre) return 'Le navigateur intégré (WhatsApp, Facebook…) bloque la position : ouvrez ce lien dans Chrome ou Safari (menu ⋮ → « Ouvrir dans le navigateur »).';

  if (err.code === err.PERMISSION_DENIED) {
    let etat = null;
    try { etat = (await navigator.permissions?.query({ name: 'geolocation' }))?.state; } catch { /* non pris en charge */ }

    if (etat === 'denied' || etat === null) {
      // Bloqué pour ce site dans le navigateur
      if (iOS) return 'Position bloquée. Sur iPhone : Réglages → Confidentialité et sécurité → Service de localisation → activé, puis « Sites web Safari » → « Lorsque l\'app est active ». Ensuite Réglages → Safari → Position → « Demander ». Rechargez la page.';
      if (android) return 'Position bloquée pour ce site. Touchez l\'icône à gauche de l\'adresse → Autorisations (ou Paramètres du site) → Position → Autoriser, puis rechargez la page.';
      return 'Position bloquée pour ce site. Cliquez sur l\'icône à gauche de l\'adresse → Position → Autoriser, puis rechargez la page.';
    }
    if (etat === 'prompt') return 'La demande d\'autorisation a été fermée sans réponse : appuyez de nouveau sur le bouton et choisissez « Autoriser ».';
    // Le site est autorisé mais le système refuse au navigateur
    if (android) return 'Le téléphone refuse la position à Chrome : Paramètres → Applications → Chrome → Autorisations → Position → Autoriser. Activez aussi la localisation du téléphone.';
    if (iOS) return 'L\'iPhone refuse la position à Safari : Réglages → Confidentialité et sécurité → Service de localisation → « Sites web Safari » → « Lorsque l\'app est active ».';
    return 'L\'ordinateur refuse la position au navigateur : sous Windows, Paramètres → Confidentialité et sécurité → Localisation → activez les services de localisation et l\'accès pour les applications de bureau.';
  }
  if (err.code === err.TIMEOUT) return 'Le GPS met trop de temps à répondre : mettez-vous à découvert et réessayez.';
  return 'Position introuvable : activez la localisation (GPS) du téléphone, puis réessayez.';
}
