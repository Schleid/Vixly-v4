/* Progressive mobile navigation only; no session, tenant or business data is stored. */
(() => {
    'use strict';
    const sidebar = document.getElementById('ops-navigation');
    const toggle = document.querySelector('[data-nav-toggle]');
    const close = document.querySelector('[data-nav-close]');
    const backdrop = document.querySelector('[data-nav-backdrop]');
    const main = document.getElementById('main-content');
    if (!sidebar || !toggle || !close || !backdrop || !main) return;
    const mobile = window.matchMedia('(max-width: 1000px)');
    let previousFocus;
    document.body.classList.add('ops-editor--enhanced');
    toggle.hidden = false;
    close.hidden = false;
    const setOpen = (open, restore = true) => {
        document.body.classList.toggle('ops-editor--nav-open', open);
        toggle.setAttribute('aria-expanded', String(open));
        backdrop.hidden = !open;
        main.inert = open;
        if (open) {
            previousFocus = document.activeElement;
            sidebar.setAttribute('role', 'dialog');
            sidebar.setAttribute('aria-modal', 'true');
            close.focus();
        } else {
            sidebar.removeAttribute('role');
            sidebar.removeAttribute('aria-modal');
            if (restore && previousFocus instanceof HTMLElement) previousFocus.focus();
        }
    };
    toggle.addEventListener('click', () => setOpen(toggle.getAttribute('aria-expanded') !== 'true'));
    close.addEventListener('click', () => setOpen(false));
    backdrop.addEventListener('click', () => setOpen(false));
    document.addEventListener('keydown', event => {
        if (toggle.getAttribute('aria-expanded') !== 'true') return;
        if (event.key === 'Escape') { event.preventDefault(); setOpen(false); }
        if (event.key === 'Tab') {
            const items = [...sidebar.querySelectorAll('a, button, summary')].filter(item => item.getClientRects().length > 0);
            const first = items[0], last = items[items.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });
    mobile.addEventListener('change', () => setOpen(false, false));
})();
