// =========================================================
// FILE: ops-cashier-tenders.js
//
// PURPOSE:
// The cashier adds the customer's payments one by one; each comes off what is left (owner, 2026-10-26).
//
// RESPONSIBILITIES:
// - Add a line (method, amount, cash handed over or reference) to the list, or remove one
// - Show what is added and what is left; offer the next line for what is left
// - Cash once per payment; the Zelle details only when Zelle is used
// - The card button charges what is left: the row being typed is not sent with it
//
// IMPORTANT NOTES:
// - The form works without this script: the typed row alone pays in full
// - The server checks everything again; this only keeps the cashier from typing twice
// - Lines are sent as method_1, amount_1, given_1, reference_1 ... in their order
// =========================================================
(function () {
    'use strict';
    var form = document.querySelector('form[data-tenders]');
    if (!form) {
        return;
    }
    var owed = cents(form.getAttribute('data-owed'));
    var list = form.querySelector('[data-tenders-list]');
    var adder = form.querySelector('[data-tenders-adder]');
    var method = adder.querySelector('select[name="method_new"]');
    var amount = adder.querySelector('input[name="amount_new"]');
    var given = adder.querySelector('input[name="given_new"]');
    var reference = adder.querySelector('input[name="reference_new"]');
    var addButton = adder.querySelector('[data-tender-add]');
    var error = adder.querySelector('[data-tender-error]');
    var zelle = form.querySelector('[data-zelle]');
    var card = form.querySelector('.ops-cashier__card');
    var max = 5;

    function cents(text) {
        var value = parseFloat(String(text || '').replace(',', '.'));
        return isNaN(value) ? NaN : Math.round(value * 100);
    }
    function money(value) {
        return (value / 100).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
    function plain(value) {
        return (value / 100).toFixed(2);
    }
    function lines() {
        return Array.prototype.slice.call(list.querySelectorAll('[data-tender]'));
    }
    function added() {
        return lines().reduce(function (sum, line) { return sum + cents(line.getAttribute('data-amount')); }, 0);
    }
    function say(text) {
        error.textContent = text || '';
    }
    function hidden(name, value) {
        var input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        return input;
    }
    function renumber() {
        lines().forEach(function (line, index) {
            line.querySelectorAll('input[type="hidden"]').forEach(function (input) {
                input.name = input.name.replace(/_\d+$/, '_' + (index + 1));
            });
        });
    }
    function fields() {
        var isCash = method.value === 'Cash';
        adder.querySelector('[data-tender-given]').hidden = !isCash;
        adder.querySelector('[data-tender-reference]').hidden = isCash;
        var zelleUsed = method.value === 'Zelle' || lines().some(function (line) { return line.getAttribute('data-method') === 'Zelle'; });
        if (zelle) {
            zelle.hidden = !zelleUsed;
        }
    }
    function refresh() {
        var sum = added();
        var left = Math.max(0, owed - sum);
        form.querySelector('[data-tenders-added]').textContent = money(sum);
        form.querySelector('[data-tenders-left]').textContent = money(left);
        var again = form.querySelector('[data-tenders-left-again]');
        if (again) {
            again.textContent = money(left);
        }
        var cashUsed = lines().some(function (line) { return line.getAttribute('data-method') === 'Cash'; });
        Array.prototype.forEach.call(method.options, function (option) {
            option.disabled = option.value === 'Cash' && cashUsed;
        });
        if (method.selectedOptions[0] && method.selectedOptions[0].disabled) {
            var free = Array.prototype.filter.call(method.options, function (option) { return !option.disabled; })[0];
            if (free) {
                method.value = free.value;
            }
        }
        adder.hidden = left === 0;
        amount.value = left === 0 ? '' : plain(left);
        if (card) {
            card.hidden = left === 0;
        }
        fields();
        renumber();
    }
    function add() {
        var left = owed - added();
        var value = cents(amount.value);
        say('');
        if (!(value > 0)) {
            say(form.getAttribute('data-text-amount'));
            return;
        }
        if (value > left) {
            say(form.getAttribute('data-text-more').replace('{amount}', money(left)));
            return;
        }
        var handed = method.value === 'Cash' && given.value.trim() !== '' ? cents(given.value) : null;
        if (handed !== null && (isNaN(handed) || handed < value)) {
            say(form.getAttribute('data-text-given'));
            return;
        }
        if (lines().length >= max) {
            say(form.getAttribute('data-text-amount'));
            return;
        }
        var line = document.createElement('li');
        line.setAttribute('data-tender', '');
        line.setAttribute('data-method', method.value);
        line.setAttribute('data-amount', plain(value));
        var label = document.createElement('span');
        var detail = method.selectedOptions[0].textContent;
        if (method.value === 'Cash' && handed !== null) {
            detail += ' · ' + form.getAttribute('data-text-given-word') + ' ' + money(handed) + ' · ' + form.getAttribute('data-text-change') + ' ' + money(handed - value);
        } else if (method.value !== 'Cash' && reference.value.trim() !== '') {
            detail += ' · ' + reference.value.trim();
        }
        label.textContent = detail;
        var shown = document.createElement('strong');
        shown.textContent = money(value);
        var remove = document.createElement('button');
        remove.type = 'button';
        remove.className = 'vx-button vx-button--ghost';
        remove.setAttribute('data-tender-remove', '');
        remove.setAttribute('aria-label', addButton.getAttribute('data-remove-label') || '×');
        remove.textContent = '×';
        line.appendChild(label);
        line.appendChild(shown);
        line.appendChild(remove);
        line.appendChild(hidden('method_0', method.value));
        line.appendChild(hidden('amount_0', plain(value)));
        line.appendChild(hidden('given_0', method.value === 'Cash' && handed !== null ? plain(handed) : ''));
        line.appendChild(hidden('reference_0', method.value === 'Cash' ? '' : reference.value.trim()));
        list.appendChild(line);
        given.value = '';
        reference.value = '';
        refresh();
        amount.focus();
    }

    addButton.hidden = false;
    addButton.addEventListener('click', add);
    amount.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            add();
        }
    });
    method.addEventListener('change', function () { say(''); fields(); });
    list.addEventListener('click', function (event) {
        var button = event.target.closest('[data-tender-remove]');
        if (button) {
            button.closest('[data-tender]').remove();
            say('');
            refresh();
        }
    });
    form.addEventListener('submit', function (event) {
        var byCard = event.submitter && event.submitter.hasAttribute('data-tenders-card');
        var typed = adder.hidden ? 0 : (cents(amount.value) || 0);
        if (byCard) {
            // The card charges what is left: the row being typed is not a payment.
            amount.value = '';
            return;
        }
        if (added() + typed !== owed) {
            event.preventDefault();
            adder.hidden = false;
            say(form.getAttribute('data-text-left').replace('{amount}', money(Math.max(0, owed - added()))));
        }
    });
    refresh();
}());
