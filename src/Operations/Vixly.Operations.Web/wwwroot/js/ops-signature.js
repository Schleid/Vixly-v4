/* Signature pad (Slice 5D): the customer signs with a pen, a finger or the mouse on a form marked data-signature-pad.
   Any pad that acts as a pen, touch screen or mouse works. The drawing is sent as a PNG data address in the form's hidden field. */
(() => {
    'use strict';
    document.querySelectorAll('form[data-signature-pad]').forEach(form => {
        const canvas = form.querySelector('canvas');
        const field = form.querySelector('input[name=signature]');
        const pen = canvas.getContext('2d');
        let drawing = false;
        let drawn = false;
        pen.lineWidth = 2.5;
        pen.lineCap = 'round';
        pen.lineJoin = 'round';
        pen.strokeStyle = '#111827';
        const point = event => {
            const box = canvas.getBoundingClientRect();
            return { x: (event.clientX - box.left) * canvas.width / box.width, y: (event.clientY - box.top) * canvas.height / box.height };
        };
        canvas.style.touchAction = 'none';
        canvas.addEventListener('pointerdown', event => {
            drawing = true;
            canvas.setPointerCapture(event.pointerId);
            const at = point(event);
            pen.beginPath();
            pen.moveTo(at.x, at.y);
        });
        canvas.addEventListener('pointermove', event => {
            if (!drawing) return;
            const at = point(event);
            pen.lineTo(at.x, at.y);
            pen.stroke();
            drawn = true;
        });
        const stop = () => { drawing = false; };
        canvas.addEventListener('pointerup', stop);
        canvas.addEventListener('pointercancel', stop);
        form.querySelector('[data-signature-clear]')?.addEventListener('click', () => {
            pen.clearRect(0, 0, canvas.width, canvas.height);
            drawn = false;
        });
        form.addEventListener('submit', event => {
            if (!drawn) {
                event.preventDefault();
                canvas.focus();
                return;
            }
            field.value = canvas.toDataURL('image/png');
        });
    });
})();
