// An orientation only: never submit a package or shipment form.
(() => {
    const panel = document.querySelector('[data-customer-tour]');
    if (!(panel instanceof HTMLDialogElement)) return;
    const target = document.querySelector(panel.dataset.tourTarget) || document.querySelector('#mailbox-title');
    document.body.classList.add('customer-tour-active');
    if (target) {
        target.classList.add('customer-tour-highlight');
    }
    if (typeof panel.showModal === 'function') {
        if (panel.open) panel.close();
        panel.showModal();
    }
    panel.addEventListener('keydown', event => {
        if (event.key !== 'Tab') return;
        const controls = [...panel.querySelectorAll('a[href],button:not(:disabled)')].filter(item => item.getClientRects().length);
        const first = controls[0];
        const last = controls[controls.length - 1];
        if (!first) return;
        if (event.shiftKey && (document.activeElement === first || !panel.contains(document.activeElement))) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && (document.activeElement === last || !panel.contains(document.activeElement))) {
            event.preventDefault();
            first.focus();
        }
    });
    // Escape means Skip, with the same antiforgery-protected account save.
    panel.addEventListener('cancel', event => {
        event.preventDefault();
        const form = panel.querySelector('[data-tour-dismiss]');
        const skip = panel.querySelector('[data-tour-skip]');
        if (form && skip) form.requestSubmit(skip);
    });
    const next = panel.querySelector('.customer-tour__actions > a') || panel.querySelector('button');
    if (next) next.focus({ preventScroll: true });
    const positionTarget = () => {
        if (!target) return;
        const box = target.getBoundingClientRect();
        const available = panel.getBoundingClientRect().top - 16;
        const top = Math.max(16, Math.min(96, available - box.height - 16));
        window.scrollBy({ top: box.top - top, behavior: 'instant' });
    };
    requestAnimationFrame(positionTarget);
    window.addEventListener('resize', positionTarget);
})();
