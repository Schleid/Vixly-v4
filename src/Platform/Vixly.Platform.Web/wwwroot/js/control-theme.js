/* CSP-safe Control presentation. No inline handlers, network calls or account state. */
(() => {
    'use strict';
    const root = document.documentElement;
    const readPreference = () => {
        try { return localStorage.getItem('vx-theme'); } catch { return null; }
    };
    const apply = theme => {
        const dark = theme === 'dark';
        root.setAttribute('data-theme', dark ? 'dark' : 'light');
        root.style.colorScheme = dark ? 'dark' : 'light';
        document.querySelectorAll('[data-vx-theme-toggle]').forEach(button => {
            button.setAttribute('aria-pressed', String(dark));
        });
    };
    apply(readPreference());

    let setNavOpen = null;
    const initNav = () => {
        const sidebar = document.getElementById('vx-control-sidebar');
        const toggle = document.querySelector('[data-vx-nav-toggle]');
        const close = document.querySelector('[data-vx-nav-close]');
        const backdrop = document.querySelector('[data-vx-nav-backdrop]');
        const main = document.getElementById('vx-main-content');
        if (!sidebar || !toggle || !close || !backdrop || !main) return;
        root.classList.add('vx-shell--enhanced');
        toggle.hidden = false;
        close.hidden = false;
        let previousFocus = null;
        setNavOpen = (open, restore = true) => {
            root.classList.toggle('vx-shell--nav-open', open);
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
        toggle.addEventListener('click', () => setNavOpen(toggle.getAttribute('aria-expanded') !== 'true'));
        close.addEventListener('click', () => setNavOpen(false));
        backdrop.addEventListener('click', () => setNavOpen(false));
        sidebar.addEventListener('click', event => {
            if (event.target instanceof Element && event.target.closest('a') && window.innerWidth <= 1024) {
                setNavOpen(false, false);
            }
        });
        document.addEventListener('keydown', event => {
            if (toggle.getAttribute('aria-expanded') !== 'true') return;
            if (event.key === 'Escape') { event.preventDefault(); setNavOpen(false); }
            if (event.key === 'Tab') {
                const items = [...sidebar.querySelectorAll('a, button, summary')].filter(item => item.getClientRects().length > 0);
                const first = items[0], last = items[items.length - 1];
                if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
                else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
            }
        });
        window.matchMedia('(max-width: 1024px)').addEventListener('change', () => setNavOpen(false, false));
    };

    document.addEventListener('DOMContentLoaded', () => {
        apply(root.getAttribute('data-theme'));
        initNav();
    });
    document.addEventListener('enhancedload', () => {
        apply(root.getAttribute('data-theme'));
        if (setNavOpen) setNavOpen(false, false);
        else initNav();
    });
    document.addEventListener('click', event => {
        if (!(event.target instanceof Element) || !event.target.closest('[data-vx-theme-toggle]')) return;
        const next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        apply(next);
        try { localStorage.setItem('vx-theme', next); } catch { /* Theme still works when storage is unavailable. */ }
    });
    document.addEventListener('storage', event => {
        if (event.key === 'vx-theme') apply(event.newValue);
    });
    document.addEventListener('keydown', event => {
        if ((event.ctrlKey || event.metaKey) && event.code === 'KeyK') {
            const search = document.getElementById('vx-tenant-search');
            if (search) { event.preventDefault(); search.focus(); }
        }
    });
})();
