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

    let previousFocus = null;
    const setNavOpen = (open, restore = true) => {
        root.classList.toggle('vx-shell--nav-open', open);
        const toggle = document.querySelector('[data-vx-nav-toggle]');
        if (toggle) toggle.setAttribute('aria-expanded', String(open));
        const sidebar = document.getElementById('vx-control-sidebar');
        const close = document.querySelector('[data-vx-nav-close]');
        const main = document.getElementById('vx-main-content');
        if (main) main.inert = open;
        if (open) {
            previousFocus = document.activeElement;
            if (sidebar) {
                sidebar.setAttribute('role', 'dialog');
                sidebar.setAttribute('aria-modal', 'true');
            }
            if (close) close.focus();
        } else {
            if (sidebar) {
                sidebar.removeAttribute('role');
                sidebar.removeAttribute('aria-modal');
            }
            if (restore && previousFocus instanceof HTMLElement) previousFocus.focus();
        }
    };

    document.addEventListener('DOMContentLoaded', () => apply(root.getAttribute('data-theme')));
    document.addEventListener('enhancedload', () => {
        apply(root.getAttribute('data-theme'));
        setNavOpen(false, false);
    });

    document.addEventListener('click', event => {
        if (!(event.target instanceof Element)) return;

        if (event.target.closest('[data-vx-theme-toggle]')) {
            const next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
            apply(next);
            try { localStorage.setItem('vx-theme', next); } catch { /* Theme still works when storage is unavailable. */ }
            return;
        }

        if (event.target.closest('[data-vx-nav-toggle]')) {
            const isOpen = root.classList.contains('vx-shell--nav-open');
            setNavOpen(!isOpen);
            return;
        }

        if (event.target.closest('[data-vx-nav-close]') || event.target.closest('[data-vx-nav-backdrop]')) {
            setNavOpen(false);
            return;
        }

        if (event.target.closest('#vx-control-sidebar a') && window.innerWidth <= 1024) {
            setNavOpen(false, false);
            return;
        }
    });

    document.addEventListener('keydown', event => {
        if ((event.ctrlKey || event.metaKey) && event.code === 'KeyK') {
            const search = document.getElementById('vx-tenant-search');
            if (search) { event.preventDefault(); search.focus(); }
            return;
        }

        if (!root.classList.contains('vx-shell--nav-open')) return;

        if (event.key === 'Escape') {
            event.preventDefault();
            setNavOpen(false);
            return;
        }

        if (event.key === 'Tab') {
            const sidebar = document.getElementById('vx-control-sidebar');
            if (!sidebar) return;
            const items = [...sidebar.querySelectorAll('a, button, summary')].filter(item => item.getClientRects().length > 0);
            if (items.length === 0) return;
            const first = items[0], last = items[items.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        }
    });

    document.addEventListener('storage', event => {
        if (event.key === 'vx-theme') apply(event.newValue);
    });

    window.matchMedia('(max-width: 1024px)').addEventListener('change', event => {
        if (!event.matches && root.classList.contains('vx-shell--nav-open')) {
            setNavOpen(false, false);
        }
    });
})();
