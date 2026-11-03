/* Logos without their blank margins (owner, 2026-10-25). Many logo files are a large square with the mark in a thin
   strip; the website header shows the whole file in 44 pixels, so the mark looked tiny. Before the file is sent,
   this cuts away the transparent or plain-coloured border and keeps a small breathing space. Pictures with
   nothing to cut, and browsers that cannot do it, send the file unchanged. */
(() => {
    'use strict';
    const inputs = document.querySelectorAll('input[type=file][data-trim-margins]');
    if (inputs.length === 0 || !window.DataTransfer || !document.createElement('canvas').getContext) return;

    const near = (data, at, colour) => Math.abs(data[at] - colour[0]) < 24 && Math.abs(data[at + 1] - colour[1]) < 24
        && Math.abs(data[at + 2] - colour[2]) < 24 && Math.abs(data[at + 3] - colour[3]) < 24;

    const blank = (data, at, colour) => data[at + 3] < 16 || near(data, at, colour);

    const trim = (file) => new Promise((resolve) => {
        const image = new Image();
        const url = URL.createObjectURL(file);
        image.onerror = () => { URL.revokeObjectURL(url); resolve(null); };
        image.onload = () => {
            URL.revokeObjectURL(url);
            const width = image.naturalWidth, height = image.naturalHeight;
            if (width < 8 || height < 8) { resolve(null); return; }
            const canvas = document.createElement('canvas');
            canvas.width = width; canvas.height = height;
            const context = canvas.getContext('2d', { willReadFrequently: true });
            context.drawImage(image, 0, 0);
            const data = context.getImageData(0, 0, width, height).data;
            const colour = [data[0], data[1], data[2], data[3]];
            let left = width, top = height, right = -1, bottom = -1;
            for (let y = 0; y < height; y++) {
                for (let x = 0; x < width; x++) {
                    if (!blank(data, (y * width + x) * 4, colour)) {
                        if (x < left) left = x;
                        if (x > right) right = x;
                        if (y < top) top = y;
                        if (y > bottom) bottom = y;
                    }
                }
            }
            if (right < 0) { resolve(null); return; }
            const pad = Math.max(4, Math.round(Math.max(right - left, bottom - top) * 0.04));
            left = Math.max(0, left - pad); top = Math.max(0, top - pad);
            right = Math.min(width - 1, right + pad); bottom = Math.min(height - 1, bottom + pad);
            const keptWidth = right - left + 1, keptHeight = bottom - top + 1;
            if (keptWidth * keptHeight > width * height * 0.9) { resolve(null); return; }
            const out = document.createElement('canvas');
            out.width = keptWidth; out.height = keptHeight;
            out.getContext('2d').drawImage(canvas, left, top, keptWidth, keptHeight, 0, 0, keptWidth, keptHeight);
            out.toBlob((blob) => resolve(blob ? new File([blob], file.name.replace(/\.[^.]+$/, '') + '.png', { type: 'image/png' }) : null), 'image/png');
        };
        image.src = url;
    });

    inputs.forEach((input) => {
        input.addEventListener('change', async () => {
            const file = input.files && input.files[0];
            if (!file || !/^image\/(png|jpeg|webp)$/.test(file.type)) return;
            const form = input.form;
            const button = form && form.querySelector('[type=submit]');
            if (button) button.disabled = true;
            try {
                const trimmed = await trim(file);
                if (trimmed) {
                    const transfer = new DataTransfer();
                    transfer.items.add(trimmed);
                    input.files = transfer.files;
                }
            } catch {
                /* Sent as chosen. */
            } finally {
                if (button) button.disabled = false;
            }
        });
    });
})();
