/* Owner, 2026-10-20: the shipment estimate stays in view and centred in the window, whatever the scroll.
   The side strip is pinned to the window over its own column (measured on load and on resize); without this script it stays sticky. */
(function () {
    'use strict';
    const side = document.querySelector('.customer-portal__estimate-side');
    if (!side) { return; }
    const wide = window.matchMedia('(min-width: 1001px)');

    function place() {
        side.classList.remove('customer-portal__estimate-side--pinned');
        side.style.left = side.style.width = '';
        if (!wide.matches) { return; }
        const box = side.getBoundingClientRect();
        side.style.left = box.left + 'px';
        side.style.width = box.width + 'px';
        side.classList.add('customer-portal__estimate-side--pinned');
    }

    place();
    window.addEventListener('resize', place);
    wide.addEventListener('change', place);
})();
