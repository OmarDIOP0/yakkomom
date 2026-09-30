// Menus Région > Département > Commune liés.
// Sans JavaScript, les trois listes complètes restent utilisables (le serveur vérifie la cohérence).
for (const bloc of document.querySelectorAll('[data-cascade-localites]')) {
  const region = bloc.querySelector('[data-niveau="region"]');
  const departement = bloc.querySelector('[data-niveau="departement"]');
  const commune = bloc.querySelector('[data-niveau="commune"]');

  // Copie de toutes les options d'origine (Safari iOS ne sait pas masquer une <option>)
  const optionsDep = [...departement.options].slice(1).map((o) => o.cloneNode(true));
  const groupesCommunes = [...commune.querySelectorAll('optgroup')].map((g) => g.cloneNode(true));
  const parentDe = new Map(optionsDep.map((o) => [o.value, o.dataset.parent]));

  function remplirDepartements() {
    const choisi = departement.value;
    departement.length = 1;
    for (const o of optionsDep) {
      if (!region.value || o.dataset.parent === region.value) departement.append(o.cloneNode(true));
    }
    departement.value = [...departement.options].some((o) => o.value === choisi) ? choisi : '';
  }

  function remplirCommunes() {
    const choisi = commune.value;
    commune.length = 1;
    commune.querySelectorAll('optgroup').forEach((g) => g.remove());
    for (const g of groupesCommunes) {
      const dep = g.dataset.parent;
      const visible = departement.value ? dep === departement.value : !region.value || parentDe.get(dep) === region.value;
      if (visible) commune.append(g.cloneNode(true));
    }
    commune.value = [...commune.options].some((o) => o.value === choisi) ? choisi : '';
  }

  region.addEventListener('change', () => { remplirDepartements(); remplirCommunes(); });
  departement.addEventListener('change', () => {
    if (departement.value) region.value = parentDe.get(departement.value) ?? region.value;
    remplirCommunes();
  });
  commune.addEventListener('change', () => {
    const opt = commune.selectedOptions[0];
    if (opt?.dataset.parent) {
      region.value = parentDe.get(opt.dataset.parent) ?? '';
      remplirDepartements();
      departement.value = opt.dataset.parent;
    }
  });

  remplirDepartements();
  remplirCommunes();
}
