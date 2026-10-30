/* Owner, 2026-10-20: the shipment estimate stays in view and centred between the top bar and the bottom of the window, whatever the scroll.
   The side strip is pinned to the window over its own column (measured on load and on resize); without this script it stays sticky. */
(function () {
    'use strict';
    const side = document.querySelector('.ops-counter__side');
    if (!side) { return; }
    const wide = window.matchMedia('(min-width: 1101px)');
    const bar = document.querySelector('.ops-editor__topbar');

    function place() {
        side.classList.remove('ops-counter__side--pinned');
        side.style.left = side.style.width = side.style.top = '';
        if (!wide.matches) { return; }
        const box = side.getBoundingClientRect();
        side.style.left = box.left + 'px';
        side.style.width = box.width + 'px';
        side.style.top = (bar ? bar.getBoundingClientRect().height : 0) + 'px';
        side.classList.add('ops-counter__side--pinned');
    }

    place();
    window.addEventListener('resize', place);
    wide.addEventListener('change', place);
})();
