const modal = document.getElementById('checkout-modal');
const closeBtn = document.getElementById('close-checkout-btn');
const form = document.getElementById('checkout-form');
const packageIdInput = document.getElementById('form-package-id');
const serviceTitleElem = document.getElementById('modal-service-title');
const packageNameElem = document.getElementById('modal-package-name');
const packagePriceElem = document.getElementById('modal-package-price');
const errorBox = document.getElementById('checkout-error-box');
const errorMsg = document.getElementById('checkout-error-msg');
const submitBtn = document.getElementById('submit-checkout-btn');
const btnText = document.getElementById('btn-text');
const btnSpinner = document.getElementById('btn-spinner');
const btnArrow = document.getElementById('btn-arrow');
let modalTrigger = null;

const getFocusableElements = () => modal?.querySelectorAll(
  'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])'
) ?? [];

function openModal(packageId, packageName, packagePrice, serviceTitle) {
  if (!modal) return;
  if (packageIdInput) packageIdInput.value = packageId;
  if (serviceTitleElem) serviceTitleElem.textContent = serviceTitle || 'Servicio Digital';
  if (packageNameElem) packageNameElem.textContent = packageName || 'Paquete';
  if (packagePriceElem) packagePriceElem.textContent = packagePrice || '';
  errorBox?.classList.add('hidden');
  modal.classList.remove('hidden');
  modal.classList.add('flex');
  modal.setAttribute('aria-hidden', 'false');
  document.body.classList.add('overflow-hidden');
  window.setTimeout(() => closeBtn?.focus(), 0);
}

function closeModal() {
  if (!modal) return;
  modal.classList.add('hidden');
  modal.classList.remove('flex');
  modal.setAttribute('aria-hidden', 'true');
  document.body.classList.remove('overflow-hidden');
  form?.reset();
  modalTrigger?.focus();
}

document.addEventListener('click', event => {
  const target = event.target.closest?.('[data-open-checkout]');
  if (!target) return;
  modalTrigger = target;
  openModal(
    target.getAttribute('data-package-id') || '',
    target.getAttribute('data-package-name') || '',
    target.getAttribute('data-package-price') || '',
    target.getAttribute('data-service-title') || ''
  );
});

closeBtn?.addEventListener('click', closeModal);
modal?.addEventListener('click', event => {
  if (event.target === modal) closeModal();
});

document.addEventListener('keydown', event => {
  if (!modal || modal.classList.contains('hidden')) return;
  if (event.key === 'Escape') {
    event.preventDefault();
    closeModal();
    return;
  }
  if (event.key !== 'Tab') return;
  const focusable = Array.from(getFocusableElements());
  if (focusable.length === 0) return;
  const first = focusable[0];
  const last = focusable[focusable.length - 1];
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
});

form?.addEventListener('submit', async event => {
  event.preventDefault();
  if (!form.checkValidity()) {
    form.reportValidity();
    return;
  }

  const packageId = packageIdInput?.value || '';
  if (!packageId) {
    showError('Por favor selecciona un paquete válido.');
    return;
  }

  setLoading(true);
  try {
    const response = await fetch('/api/checkout', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        packageId,
        fullName: document.getElementById('form-name')?.value || '',
        email: document.getElementById('form-email')?.value || '',
        phoneNumber: document.getElementById('form-phone')?.value || null,
        companyName: document.getElementById('form-company')?.value || null
      })
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || 'No se pudo iniciar el checkout.');
    if (!data.checkoutUrl) throw new Error('No se recibió la URL de pago de Mercado Pago.');
    window.location.href = data.checkoutUrl;
  } catch (error) {
    showError(error instanceof Error ? error.message : 'Ocurrió un error al procesar el pago.');
    setLoading(false);
  }
});

function showError(message) {
  if (errorBox && errorMsg) {
    errorMsg.textContent = message;
    errorBox.classList.remove('hidden');
  }
}

function setLoading(isLoading) {
  if (!submitBtn || !btnText || !btnSpinner || !btnArrow) return;
  submitBtn.disabled = isLoading;
  btnText.textContent = isLoading ? 'Generando orden segura...' : 'Continuar a Mercado Pago';
  btnSpinner.classList.toggle('hidden', !isLoading);
  btnArrow.classList.toggle('hidden', isLoading);
}
