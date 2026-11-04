/* Progressive staff-entry presentation only. No credentials are read or stored. */
(() => {
    'use strict';
    const toggle = document.querySelector('[data-password-toggle]');
    const password = document.getElementById('password');
    if (toggle && password) {
        toggle.hidden = false;
        toggle.addEventListener('click', () => {
            const show = password.type === 'password';
            password.type = show ? 'text' : 'password';
            toggle.setAttribute('aria-pressed', String(show));
            toggle.setAttribute('aria-label', show ? toggle.dataset.hideLabel : toggle.dataset.showLabel);
        });
    }
    const logo = document.querySelector('[data-tenant-logo]');
    if (logo) {
        const loaded = () => {
            if (logo.naturalWidth > 0) logo.parentElement.setAttribute('data-logo-loaded', '');
            else logo.remove();
        };
        logo.addEventListener('load', loaded, { once: true });
        logo.addEventListener('error', () => logo.remove(), { once: true });
        if (logo.complete) loaded();
    }
})();
