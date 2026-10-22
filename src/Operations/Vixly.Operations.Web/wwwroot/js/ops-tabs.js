/* Progressive section tabs. All sections remain readable without JavaScript.
   Selection lives in the URL fragment; forms, authorization and submissions are unchanged. */
(() => {
    'use strict';
    document.querySelectorAll('[data-section-tabs]').forEach(group => {
        const panels = [...group.children].filter(panel =>
            panel.matches('section,form') && (panel.dataset.tabLabel || panel.querySelector('h2')));
        if (panels.length < 2) return;
        const bar = document.createElement('div');
        bar.className = 'ops-section-tabs';
        bar.setAttribute('role', 'tablist');
        bar.setAttribute('aria-label', group.dataset.tabsLabel);
        const buttons = panels.map((panel, index) => {
            const button = document.createElement('button');
            const heading = panel.querySelector('h2');
            const id = heading?.id ? `${heading.id}-panel` : `${group.dataset.sectionTabs}-section-${index + 1}`;
            panel.id ||= id;
            button.type = 'button';
            button.id = `${id}-tab`;
            button.textContent = panel.dataset.tabLabel || panel.querySelector('h2').textContent;
            button.setAttribute('role', 'tab');
            button.setAttribute('aria-controls', panel.id);
            panel.setAttribute('role', 'tabpanel');
            panel.setAttribute('aria-labelledby', button.id);
            panel.tabIndex = 0;
            bar.append(button);
            return button;
        });
        const select = (index, updateUrl = false) => {
            panels.forEach((panel, position) => {
                panel.hidden = position !== index;
                buttons[position].setAttribute('aria-selected', String(position === index));
                buttons[position].tabIndex = position === index ? 0 : -1;
            });
            if (updateUrl) history.replaceState(null, '', `${location.pathname}${location.search}#${panels[index].id}`);
        };
        buttons.forEach((button, index) => {
            button.addEventListener('click', () => select(index, true));
            button.addEventListener('keydown', event => {
                let next;
                if (event.key === 'ArrowRight') next = (index + 1) % panels.length;
                if (event.key === 'ArrowLeft') next = (index + panels.length - 1) % panels.length;
                if (event.key === 'Home') next = 0;
                if (event.key === 'End') next = panels.length - 1;
                if (next === undefined) return;
                event.preventDefault();
                select(next, true);
                buttons[next].focus();
            });
        });
        // Native validation must expose an invalid field even when its tab is hidden.
        group.addEventListener('invalid', event => {
            const index = panels.findIndex(panel => panel.contains(event.target));
            if (index >= 0) select(index);
        }, true);
        const fromHash = () => panels.findIndex(panel => `#${panel.id}` === location.hash ||
            [...panel.querySelectorAll('[id]')].some(child => `#${child.id}` === location.hash));
        const initial = fromHash();
        const preferred = panels.findIndex(panel => panel.hasAttribute('data-tab-default'));
        select(initial >= 0 ? initial : Math.max(0, preferred));
        window.addEventListener('hashchange', () => { const index = fromHash(); if (index >= 0) select(index); });
        group.insertBefore(bar, panels[0]);
        group.classList.add('ops-tabs-enhanced');
    });
})();
