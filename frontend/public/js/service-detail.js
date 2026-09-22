const page = document.querySelector('[data-service-page]');
const slug = page?.dataset.servicePage;

if (page && slug) {
  fetch(`/api/services/${encodeURIComponent(slug)}`, { headers: { Accept: 'application/json' } })
    .then(response => {
      if (!response.ok) throw new Error('No se pudo actualizar el servicio.');
      return response.json();
    })
    .then(service => {
      const serviceTitle = page.querySelector('[data-live-service-title]');
      if (serviceTitle) serviceTitle.textContent = service.title;

      service.packages.forEach(pkg => {
        const card = page.querySelector(`[data-package-card="${CSS.escape(pkg.id)}"]`);
        if (!card) return;
        const formattedPrice = pkg.price.toLocaleString('es-PE', { minimumFractionDigits: 2 });
        const fields = {
          name: card.querySelector('[data-live-package-name]'),
          description: card.querySelector('[data-live-package-description]'),
          price: card.querySelector('[data-live-package-price]'),
          delivery: card.querySelector('[data-live-delivery]'),
          features: card.querySelector('[data-live-features]'),
          checkout: card.querySelector('[data-open-checkout]')
        };

        if (fields.name) fields.name.textContent = pkg.name;
        if (fields.description) fields.description.textContent = pkg.description ?? '';
        if (fields.price) fields.price.textContent = formattedPrice;
        if (fields.delivery) fields.delivery.textContent = `⏱️ Plazo estimado: ~${pkg.deliveryDays} días`;
        if (fields.checkout) {
          fields.checkout.dataset.packageName = pkg.name;
          fields.checkout.dataset.packagePrice = `${pkg.currency} ${formattedPrice} + IGV`;
          const label = fields.checkout.querySelector('span');
          if (label) label.textContent = `Contratar ${pkg.name}`;
        }
        if (fields.features) {
          fields.features.replaceChildren(...pkg.features.map(feature => {
            const item = document.createElement('li');
            item.className = 'flex items-start gap-2 text-xs text-ink/90 font-medium leading-snug';
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
    })
    .catch(() => {
      // El contenido prerenderizado sigue disponible como respaldo.
    });
}
