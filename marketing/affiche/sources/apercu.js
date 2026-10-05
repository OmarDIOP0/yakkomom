// Aperçu à l'écran : l'affiche entière tient dans la fenêtre (mise à l'échelle), sans défilement.
// N'agit pas à l'export : impression (PDF) et captures se font à taille réelle (?export).
(() => {
  if (new URLSearchParams(location.search).has('export')) return;
  const page = document.querySelector('.page');
  const ajuster = () => {
    const l = page.offsetWidth, h = page.offsetHeight, marge = 24;
    const k = Math.min((innerWidth - marge * 2) / l, (innerHeight - marge * 2) / h, 1);
    document.documentElement.classList.add('apercu');
    // Centrée dans la fenêtre, à l'échelle k (la mise en page garde sa taille réelle, masquée)
    page.style.position = 'absolute';
    page.style.left = `${Math.max(marge, (innerWidth - l * k) / 2)}px`;
    page.style.top = `${Math.max(marge, (innerHeight - h * k) / 2)}px`;
    page.style.transform = `scale(${k})`;
  };
  addEventListener('resize', ajuster);
  ajuster();
  if (location.search.includes('guides')) document.body.classList.add('guides');
})();
