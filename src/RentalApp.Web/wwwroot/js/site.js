// Modal forms backed by partial views.
//
// A trigger looks like:
//   <button data-modal-url="/Units/Create?propertyId=1"
//           data-refresh-target="#unit-table" data-refresh-url="/Units/Table?propertyId=1">
// or data-refresh="page" to reload the whole page after a successful save.
//
// Protocol with the controller:
//   GET  modal-url        -> 200 + the form partial, shown in #app-modal
//   POST the form action  -> 422 + the same partial with validation messages (re-rendered in place)
//                         -> 204 on success: close the modal and refresh the affected region
(() => {
  const modalEl = document.getElementById('app-modal');
  if (!modalEl) return;
  const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
  const content = modalEl.querySelector('.modal-content');
  let refresh = null;

  const ajaxHeaders = { 'X-Requested-With': 'XMLHttpRequest' };

  function show(html) {
    content.innerHTML = html;
    // Wire up client-side validation for the newly inserted form.
    const form = content.querySelector('form');
    if (form && window.jQuery && jQuery.validator) {
      jQuery(form).removeData('validator').removeData('unobtrusiveValidation');
      jQuery.validator.unobtrusive.parse(form);
    }
    content.querySelector('[autofocus], input:not([type=hidden]), select, textarea')?.focus();
  }

  function showError(message) {
    content.innerHTML =
      '<div class="modal-header"><h5 class="modal-title">Something went wrong</h5>' +
      '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>' +
      '<div class="modal-body"><p class="text-danger mb-0"></p></div>';
    content.querySelector('p').textContent = message;
  }

  async function refreshAfterSuccess() {
    if (!refresh || refresh.mode === 'page') {
      window.location.reload();
      return;
    }
    const target = document.querySelector(refresh.target);
    if (!target) return;
    const res = await fetch(refresh.url, { headers: ajaxHeaders });
    if (res.ok) target.outerHTML = await res.text();
  }

  // Event delegation, so buttons inside refreshed fragments keep working.
  document.addEventListener('click', async (e) => {
    const trigger = e.target.closest('[data-modal-url]');
    if (!trigger) return;
    e.preventDefault();

    refresh = trigger.dataset.refresh === 'page'
      ? { mode: 'page' }
      : { target: trigger.dataset.refreshTarget, url: trigger.dataset.refreshUrl };

    content.innerHTML = '<div class="modal-body text-center py-5"><div class="spinner-border text-primary"></div></div>';
    modal.show();
    const res = await fetch(trigger.dataset.modalUrl, { headers: ajaxHeaders });
    if (res.ok) show(await res.text());
    else showError(res.status === 403 ? 'You are not allowed to do that.' : 'The form could not be loaded.');
  });

  content.addEventListener('submit', async (e) => {
    const form = e.target;
    e.preventDefault();
    if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) return;

    const submitButton = form.querySelector('[type=submit]');
    if (submitButton) submitButton.disabled = true;

    const res = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: ajaxHeaders });
    if (res.status === 422) {
      show(await res.text());
    } else if (res.ok) {
      modal.hide();
      await refreshAfterSuccess();
    } else {
      showError(res.status === 403 ? 'You are not allowed to do that.' : 'The request failed. Reload the page and try again.');
    }
  });
})();
