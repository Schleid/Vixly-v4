/* Shipment estimate (Slice 5D): while the clerk fills in a form marked data-estimate, its fields are sent to that address
   and the answer replaces the #shipment-estimate panel. Read only; the form itself is sent only by its own button. */
(() => {
    'use strict';
    document.querySelectorAll('form[data-estimate]').forEach(form => {
        let timer = 0;
        let sequence = 0;
        const refresh = () => {
            window.clearTimeout(timer);
            timer = window.setTimeout(async () => {
                const mine = ++sequence;
                try {
                    const response = await fetch(form.dataset.estimate, { method: 'POST', body: new FormData(form), credentials: 'same-origin' });
                    if (!response.ok || mine !== sequence) return;
                    const holder = document.createElement('div');
                    holder.innerHTML = await response.text();
                    const fresh = holder.querySelector('#shipment-estimate');
                    const current = document.getElementById('shipment-estimate');
                    if (fresh && current) current.replaceWith(fresh);
                } catch {
                    /* The estimate is a help: a failed refresh leaves the last one. */
                }
            }, 300);
        };
        form.addEventListener('input', refresh);
        form.addEventListener('change', refresh);
        refresh();
    });
})();
