const btn = document.getElementById('mobile-menu-btn');
const menu = document.getElementById('mobile-menu');
const links = document.querySelectorAll('.mobile-nav-link');

function closeMenu({ restoreFocus = false } = {}) {
  menu?.classList.add('hidden');
  btn?.setAttribute('aria-expanded', 'false');
  btn?.setAttribute('aria-label', 'Abrir menú');
  if (restoreFocus) btn?.focus();
}

btn?.addEventListener('click', () => {
  menu?.classList.toggle('hidden');
  const isOpen = !menu?.classList.contains('hidden');
  btn.setAttribute('aria-expanded', String(isOpen));
  btn.setAttribute('aria-label', isOpen ? 'Cerrar menú' : 'Abrir menú');
});

links.forEach(link => link.addEventListener('click', () => closeMenu()));
document.addEventListener('keydown', event => {
  if (event.key === 'Escape' && !menu?.classList.contains('hidden')) {
    closeMenu({ restoreFocus: true });
  }
});
