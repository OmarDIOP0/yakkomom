// Filtres de la liste : envoi automatique au changement de tri ou de région.
const form = document.querySelector('[data-filtres]');
if (form) {
  form.querySelector('[data-envoi-auto]')?.addEventListener('change', () => form.requestSubmit());
  form.querySelector('[data-region]')?.addEventListener('change', () => {
    const commune = form.querySelector('[data-commune]');
    if (commune) commune.value = '';
    form.requestSubmit();
  });
  // N'envoyer que les champs remplis : URL plus courte et plus lisible
  form.addEventListener('submit', () => {
    for (const champ of form.querySelectorAll('input, select')) {
      if (!champ.value || ((champ.type === 'checkbox') && !champ.checked)) champ.disabled = true;
    }
    setTimeout(() => form.querySelectorAll(':disabled').forEach((c) => { c.disabled = false; }), 0);
  });
}
