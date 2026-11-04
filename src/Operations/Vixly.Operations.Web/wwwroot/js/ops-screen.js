/* Customer screen (Slice 5F step 3): asks for the screen's version every 3 seconds and reloads the page when the clerk sent
   something else, the customer paid, or the screen was removed. Nothing else: every choice is a plain form. */
(() => {
    'use strict';
    const shown = document.body.dataset.screenVersion;
    if (!shown) return;
    const ask = async () => {
        try {
            const answer = await fetch('/screen/version', { cache: 'no-store', credentials: 'same-origin' });
            if (answer.status === 401 || answer.status === 404) { location.replace('/screen'); return; }
            if (answer.ok && (await answer.text()) !== shown) { location.replace('/screen'); return; }
        } catch {
            /* Offline for a moment: asked again. */
        }
        setTimeout(ask, 3000);
    };
    setTimeout(ask, 3000);
})();
