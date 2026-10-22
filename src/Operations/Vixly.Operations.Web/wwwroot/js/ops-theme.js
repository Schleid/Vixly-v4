/* CSP-safe Operations theme presentation. No inline handlers, network calls or account state. */
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
    document.addEventListener('DOMContentLoaded', () => apply(root.getAttribute('data-theme')));
    document.addEventListener('click', event => {
        if (!(event.target instanceof Element) || !event.target.closest('[data-vx-theme-toggle]')) return;
        const next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        apply(next);
        try { localStorage.setItem('vx-theme', next); } catch { /* Theme still works when storage is unavailable. */ }
    });
    document.addEventListener('storage', event => {
        if (event.key === 'vx-theme') apply(event.newValue);
    });
    document.addEventListener('enhancedload', () => apply(root.getAttribute('data-theme')));
    document.addEventListener('keydown', event => {
        if ((event.ctrlKey || event.metaKey) && event.code === 'KeyK') {
            const search = document.getElementById('ops-quick-search');
            if (search) { event.preventDefault(); search.focus(); }
        }
    });
})();
