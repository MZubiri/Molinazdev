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

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

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
        let card = panel.querySelector(`[data-package-card="${CSS.escape(pkg.id)}"]`);
        const formattedPrice = pkg.price.toLocaleString('es-PE', { minimumFractionDigits: 2 });
        const currencyLabel = pkg.currency === 'PEN' ? 'S/.' : pkg.currency;

        if (!card) {
          const grid = panel.querySelector('.grid');
          if (grid) {
            card = document.createElement('div');
            card.setAttribute('data-package-card', pkg.id);
            card.className = 'card-brutal p-6 sm:p-7 flex flex-col justify-between relative bg-white';
            card.innerHTML = `
              <div>
                <h4 data-live-package-name class="text-xl sm:text-2xl font-extrabold text-zinc-900 font-display">${escapeHtml(pkg.name)}</h4>
                <p data-live-package-description class="text-xs text-zinc-600 mt-2 min-h-[36px] font-medium leading-relaxed">${escapeHtml(pkg.description || '')}</p>
                <div class="mt-5 pt-5 border-t-2 border-zinc-100">
                  <div class="flex items-baseline gap-1.5">
                    <span class="text-xs font-bold text-zinc-500">${currencyLabel}</span>
                    <span data-live-package-price class="text-2xl sm:text-3xl font-extrabold text-zinc-900 font-display tracking-tight">${formattedPrice}</span>
                    <span class="text-xs font-extrabold text-indigo-700">+ IGV</span>
                  </div>
                  <div class="inline-flex items-center gap-1 mt-2 text-[11px] font-bold text-zinc-700 bg-zinc-100 px-2.5 py-1 rounded-full border border-zinc-200">
                    <span data-live-delivery>⏱️ Plazo: ~${pkg.deliveryDays} días hábiles</span>
                  </div>
                </div>
                <div class="mt-6 space-y-2.5">
                  <span class="text-[11px] font-extrabold uppercase tracking-wider text-zinc-500 block">Qué incluye:</span>
                  <ul data-live-features class="space-y-2"></ul>
                </div>
              </div>
              <div class="mt-6 pt-5 border-t-2 border-zinc-100">
                <button
                  type="button"
                  data-open-checkout
                  data-package-id="${escapeHtml(pkg.id)}"
                  data-package-name="${escapeHtml(pkg.name)}"
                  data-package-price="${pkg.currency} ${formattedPrice} + IGV"
                  data-raw-price="${pkg.price}"
                  data-currency="${pkg.currency}"
                  data-service-title="${escapeHtml(service.title)}"
                  class="w-full py-3.5 px-4 text-xs uppercase tracking-wider font-extrabold flex items-center justify-center gap-2 btn-primary"
                >
                  <span>Contratar ${escapeHtml(pkg.name)}</span>
                  <span class="font-bold">↗</span>
                </button>
                <p class="text-[10px] text-zinc-500 text-center mt-2 font-medium">🔒 Pago 100% Protegido</p>
              </div>
            `;
            grid.appendChild(card);
          }
        }

        if (!card) return;

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
          checkout.dataset.rawPrice = pkg.price;
          checkout.dataset.currency = pkg.currency;
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
