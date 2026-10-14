const modal = document.getElementById('checkout-modal');
const closeBtn = document.getElementById('close-checkout-btn');
const form = document.getElementById('checkout-form');
const packageIdInput = document.getElementById('form-package-id');
const serviceTitleElem = document.getElementById('modal-service-title');
const packageNameElem = document.getElementById('modal-package-name');
const packagePriceElem = document.getElementById('modal-package-price');
const basePriceElem = document.getElementById('modal-base-price');
const igvPriceElem = document.getElementById('modal-igv-price');
const totalPriceElem = document.getElementById('modal-total-price');
const errorBox = document.getElementById('checkout-error-box');
const errorMsg = document.getElementById('checkout-error-msg');
const errorList = document.getElementById('checkout-error-list');
const submitBtn = document.getElementById('submit-checkout-btn');
const btnText = document.getElementById('btn-text');
const btnSpinner = document.getElementById('btn-spinner');
const btnArrow = document.getElementById('btn-arrow');
let modalTrigger = null;

const getFocusableElements = () => modal?.querySelectorAll(
  'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])'
) ?? [];

function clearFieldErrors() {
  form?.querySelectorAll('input').forEach(input => {
    input.classList.remove('border-red-600', 'bg-red-50');
  });
}

function openModal(packageId, packageName, packagePrice, serviceTitle, rawPriceStr, currencyStr) {
  if (!modal) return;
  if (packageIdInput) packageIdInput.value = packageId;
  if (serviceTitleElem) serviceTitleElem.textContent = serviceTitle || 'Servicio Digital';
  if (packageNameElem) packageNameElem.textContent = packageName || 'Paquete';
  if (packagePriceElem) packagePriceElem.textContent = packagePrice || '';

  // Calculate Base + IGV (18%) + Total
  let currency = currencyStr || 'PEN';
  let basePrice = parseFloat(rawPriceStr);
  if (isNaN(basePrice) && packagePrice) {
    const matched = packagePrice.match(/([0-9]+[.,]?[0-9]*)/);
    if (matched) {
      basePrice = parseFloat(matched[0].replace(',', ''));
    }
  }

  if (!isNaN(basePrice) && basePrice > 0) {
    const igv = Math.round(basePrice * 0.18 * 100) / 100;
    const total = Math.round(basePrice * 1.18 * 100) / 100;
    const currLabel = currency === 'PEN' ? 'S/.' : currency;

    if (basePriceElem) {
      basePriceElem.textContent = `${currLabel} ${basePrice.toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
    if (igvPriceElem) {
      igvPriceElem.textContent = `${currLabel} ${igv.toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
    if (totalPriceElem) {
      totalPriceElem.textContent = `${currLabel} ${total.toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
  }

  clearFieldErrors();
  hideError();
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
  clearFieldErrors();
  hideError();
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
    target.getAttribute('data-service-title') || '',
    target.getAttribute('data-raw-price') || '',
    target.getAttribute('data-currency') || 'PEN'
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
  clearFieldErrors();
  hideError();

  if (!form.checkValidity()) {
    form.reportValidity();
    return;
  }

  const packageId = packageIdInput?.value || '';
  if (!packageId) {
    showError('Por favor selecciona un paquete válido.');
    return;
  }

  const fullNameInput = document.getElementById('form-name');
  const emailInput = document.getElementById('form-email');
  const phoneInput = document.getElementById('form-phone');
  const companyInput = document.getElementById('form-company');

  setLoading(true);
  try {
    const response = await fetch('/api/checkout', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        packageId,
        fullName: fullNameInput?.value?.trim() || '',
        email: emailInput?.value?.trim() || '',
        phoneNumber: phoneInput?.value?.trim() || null,
        companyName: companyInput?.value?.trim() || null
      })
    });

    const data = await response.json();
    if (!response.ok) {
      if (data.errors && typeof data.errors === 'object') {
        const errorListItems = [];
        for (const [field, messages] of Object.entries(data.errors)) {
          const fieldLower = field.toLowerCase();
          if (fieldLower.includes('email') && emailInput) {
            emailInput.classList.add('border-red-600', 'bg-red-50');
          } else if ((fieldLower.includes('name') || fieldLower.includes('fullname')) && fullNameInput) {
            fullNameInput.classList.add('border-red-600', 'bg-red-50');
          } else if (fieldLower.includes('phone') && phoneInput) {
            phoneInput.classList.add('border-red-600', 'bg-red-50');
          }
          if (Array.isArray(messages)) {
            errorListItems.push(...messages);
          } else if (typeof messages === 'string') {
            errorListItems.push(messages);
          }
        }
        showErrors(data.message || 'Por favor corrige los siguientes errores:', errorListItems);
        return;
      }
      throw new Error(data.message || 'No se pudo iniciar el checkout.');
    }

    if (!data.checkoutUrl) {
      throw new Error('No se recibió la URL de pago de Mercado Pago.');
    }

    window.location.href = data.checkoutUrl;
  } catch (error) {
    showError(error instanceof Error ? error.message : 'Ocurrió un error al procesar el pago.');
  } finally {
    setLoading(false);
  }
});

function showError(message) {
  if (errorBox && errorMsg) {
    errorMsg.textContent = message;
    if (errorList) {
      errorList.innerHTML = '';
      errorList.classList.add('hidden');
    }
    errorBox.classList.remove('hidden');
  }
}

function showErrors(mainMessage, details) {
  if (!errorBox || !errorMsg) return;
  errorMsg.textContent = mainMessage;
  if (errorList && Array.isArray(details) && details.length > 0) {
    errorList.innerHTML = '';
    details.forEach(msg => {
      const li = document.createElement('li');
      li.textContent = msg;
      errorList.appendChild(li);
    });
    errorList.classList.remove('hidden');
  }
  errorBox.classList.remove('hidden');
}

function hideError() {
  if (errorBox) {
    errorBox.classList.add('hidden');
    if (errorList) errorList.innerHTML = '';
  }
}

function setLoading(isLoading) {
  if (!submitBtn || !btnText || !btnSpinner || !btnArrow) return;
  submitBtn.disabled = isLoading;
  btnText.textContent = isLoading ? 'Generando orden segura...' : 'Continuar a Mercado Pago';
  btnSpinner.classList.toggle('hidden', !isLoading);
  btnArrow.classList.toggle('hidden', isLoading);
}
