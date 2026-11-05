/* After a correct code, sends the action that asked for it (owner, 2026-11-04). The form and its token come from the server; nothing is read or stored here. */
(() => {
    'use strict';
    const form = document.querySelector('form[data-stepup-continue]');
    if (form) {
        if (typeof form.requestSubmit === 'function') form.requestSubmit();
        else form.submit();
    }
})();
