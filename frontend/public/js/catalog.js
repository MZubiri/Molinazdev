const tabs = document.querySelectorAll('.category-tab');
const panels = document.querySelectorAll('.service-panel');

tabs.forEach(tab => {
  tab.addEventListener('click', () => {
    const slug = tab.getAttribute('data-service-tab');
    if (!slug) return;

    tabs.forEach(item => {
      item.classList.remove('bg-zinc-900', 'text-white', 'shadow-brutal-sm', '-translate-y-0.5');
      item.classList.add('bg-white', 'text-zinc-800');
    });
    tab.classList.remove('bg-white', 'text-zinc-800');
    tab.classList.add('bg-zinc-900', 'text-white', 'shadow-brutal-sm', '-translate-y-0.5');

    panels.forEach(panel => {
      const isActive = panel.id === `service-panel-${slug}`;
      panel.classList.toggle('hidden', !isActive);
      panel.classList.toggle('block', isActive);
    });
  });
});

fetch('/api/catalog', { headers: { Accept: 'application/json' } })
  .then(response => {
    if (!response.ok) throw new Error('No se pudo actualizar el catálogo.');
    return response.json();
  })
  .then(services => {
    services.forEach(service => {
      const panel = document.querySelector(`[data-catalog-service="${CSS.escape(service.slug)}"]`);
      if (!panel) return;
      const serviceTitle = panel.querySelector('[data-live-service-title]');
      if (serviceTitle) serviceTitle.textContent = service.title;

      service.packages.forEach(pkg => {
        const card = panel.querySelector(`[data-package-card="${CSS.escape(pkg.id)}"]`);
        if (!card) return;
        const formattedPrice = pkg.price.toLocaleString('es-PE', { minimumFractionDigits: 2 });
        const name = card.querySelector('[data-live-package-name]');
        const description = card.querySelector('[data-live-package-description]');
        const price = card.querySelector('[data-live-package-price]');
        const delivery = card.querySelector('[data-live-delivery]');
        const features = card.querySelector('[data-live-features]');
        const checkout = card.querySelector('[data-open-checkout]');

        if (name) name.textContent = pkg.name;
        if (description) description.textContent = pkg.description ?? '';
        if (price) price.textContent = formattedPrice;
        if (delivery) delivery.textContent = `⏱️ Plazo: ~${pkg.deliveryDays} días hábiles`;
        if (checkout) {
          checkout.dataset.packageName = pkg.name;
          checkout.dataset.packagePrice = `${pkg.currency} ${formattedPrice} + IGV`;
          checkout.dataset.serviceTitle = service.title;
          const label = checkout.querySelector('span');
          if (label) label.textContent = `Contratar ${pkg.name}`;
        }
        if (features) {
          features.replaceChildren(...pkg.features.map(feature => {
            const item = document.createElement('li');
            item.className = 'flex items-start gap-2 text-xs text-zinc-800 font-medium leading-snug';
            const marker = document.createElement('span');
            marker.className = 'text-emerald-600 font-extrabold text-sm flex-shrink-0';
            marker.textContent = '✓';
            const text = document.createElement('span');
            text.textContent = feature;
            item.append(marker, text);
            return item;
          }));
        }
      });
    });
  })
  .catch(() => {
    // El catálogo prerenderizado sigue disponible como respaldo.
  });
