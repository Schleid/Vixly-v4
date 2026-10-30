/* Price lists (Slice 4B): "Add a band" adds one more row, as many as the company needs (up to 100); the base shows only for "base plus per unit". No data is stored. */
(() => {
    'use strict';
    const MaxBands = 100;
    const sync = row => {
        const kind = row.querySelector('select[name^="band_kind_"]');
        const base = row.querySelector('input[name^="band_base_"]');
        if (kind && base) base.hidden = kind.value !== 'BasePlusPerUnit';
    };
    document.addEventListener('change', event => {
        const row = event.target.closest('tr[data-band]');
        if (row && event.target.matches('select')) sync(row);
    });
    document.addEventListener('click', event => {
        const button = event.target.closest('[data-add-band]');
        if (!button) return;
        const body = document.getElementById(button.dataset.addBand);
        const rows = body ? body.querySelectorAll('tr[data-band]') : [];
        if (rows.length === 0 || rows.length >= MaxBands) return;
        const next = rows[rows.length - 1].cloneNode(true);
        const number = rows.length;
        next.querySelectorAll('input, select').forEach(field => {
            field.name = field.name.replace(/_\d+$/, '_' + number);
            field.setAttribute('aria-label', (field.getAttribute('aria-label') || '').replace(/\d+$/, String(number + 1)));
            if (field instanceof HTMLSelectElement) field.selectedIndex = 0; else field.value = '';
        });
        body.appendChild(next);
        sync(next);
        next.querySelector('input')?.focus();
        if (rows.length + 1 >= MaxBands) button.hidden = true;
    });
    document.querySelectorAll('tr[data-band]').forEach(sync);
    document.querySelectorAll('[data-add-band]').forEach(button => { button.hidden = false; });
})();
