// Valyuta seçimi: səhifədəki "123.45 AZN" qiymətləri seçilmiş valyutada göstərir.
// Ödəniş həmişə AZN-dədir; bu yalnız göstəriş üçündür. Orijinal mətn yadda saxlanılır,
// AZN seçiləndə geri qaytarılır. JS ilə sonradan dəyişən qiymətlər də (MutationObserver) çevrilir.
(function () {
    const cfg = window.tripioLive || {};
    const menu = document.getElementById('currencyMenu');
    const button = document.getElementById('currencyBtn');
    const list = document.getElementById('currencyList');
    const note = document.getElementById('currencyNote');

    if (!menu || !button || !list) return;

    const STORAGE_KEY = 'tripio-currency';
    const RATES_KEY = 'tripio-rates';
    const PRICE = /(\d{1,3}(?:[ ,]\d{3})+(?:\.\d+)?|\d+(?:[.,]\d+)?)\s?AZN\b/g;
    const SKIP = new Set(['SCRIPT', 'STYLE', 'TEXTAREA', 'INPUT', 'SELECT', 'OPTION', 'CODE']);

    const symbols = { AZN: '₼', USD: '$', EUR: '€', TRY: '₺', GEL: '₾', AED: 'AED ' };
    const originals = new WeakMap(); // mətn düyünü -> orijinal AZN mətni
    let rates = { AZN: 1 };
    let current = 'AZN';
    let applying = false;

    function read(key) { try { return localStorage.getItem(key); } catch (e) { return null; } }
    function write(key, value) { try { localStorage.setItem(key, value); } catch (e) { } }

    // "1,234.50" / "1 234,50" / "175,00" -> 1234.5
    function parseAmount(text) {
        let s = text.replace(/\s/g, '');

        if (s.includes('.') && s.includes(',')) s = s.replace(/,/g, '');
        else if (/^\d+,\d{2}$/.test(s)) s = s.replace(',', '.');
        else s = s.replace(/,/g, '');

        return parseFloat(s);
    }

    function format(amount, code, decimals) {
        const value = amount.toLocaleString('en-US', {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals
        });
        return code === 'AED' ? 'AED ' + value : (symbols[code] || '') + value;
    }

    function convertText(original) {
        if (current === 'AZN') return original;

        const rate = rates[current];
        if (!rate) return original;

        return original.replace(PRICE, (match, number) => {
            const amount = parseAmount(number);
            if (isNaN(amount)) return match;

            const decimals = /[.,]\d{2}$/.test(number) ? 2 : 0;
            return format(amount * rate, current, decimals);
        });
    }

    function processNode(node) {
        if (node.nodeType !== Node.TEXT_NODE) return;

        const parent = node.parentElement;
        if (!parent || SKIP.has(parent.tagName) || parent.closest('[data-no-currency]')) return;

        let original = originals.get(node);

        // Mətn JS ilə dəyişibsə (yeni AZN qiyməti), yeni mətn orijinal sayılır
        if (original === undefined || (node.nodeValue !== convertText(original) && node.nodeValue.includes('AZN'))) {
            if (!node.nodeValue.includes('AZN')) return;
            original = node.nodeValue;
            originals.set(node, original);
        }

        const next = convertText(original);
        if (node.nodeValue !== next) node.nodeValue = next;
    }

    function processTree(root) {
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
        const nodes = [];
        while (walker.nextNode()) nodes.push(walker.currentNode);
        nodes.forEach(processNode);
    }

    function applyAll() {
        applying = true;
        processTree(document.body);
        observer.takeRecords();
        applying = false;
    }

    const observer = new MutationObserver(mutations => {
        if (applying) return;
        applying = true;

        for (const m of mutations) {
            if (m.type === 'characterData') processNode(m.target);
            m.addedNodes.forEach(n => n.nodeType === Node.TEXT_NODE ? processNode(n) : processTree(n));
        }

        observer.takeRecords();
        applying = false;
    });

    function select(code) {
        current = rates[code] ? code : 'AZN';
        write(STORAGE_KEY, current);

        button.querySelector('span').textContent = current;
        list.querySelectorAll('[data-code]').forEach(b => b.classList.toggle('active', b.dataset.code === current));
        note.hidden = current === 'AZN';

        applyAll();
    }

    // Menyu
    button.addEventListener('click', e => {
        e.stopPropagation();
        list.hidden = !list.hidden;
    });

    document.addEventListener('click', e => {
        if (!list.hidden && !e.target.closest('#currencyMenu')) list.hidden = true;
    });

    list.querySelectorAll('[data-code]').forEach(b => b.addEventListener('click', () => {
        select(b.dataset.code);
        list.hidden = true;
    }));

    async function loadRates() {
        // Sessiya ərzində bir dəfə yüklə
        try {
            const cached = JSON.parse(sessionStorage.getItem(RATES_KEY) || 'null');
            if (cached && cached.rates) return cached;
        } catch (e) { }

        if (!cfg.apiBase) return null;

        try {
            const response = await fetch(cfg.apiBase + '/api/currency/rates');
            if (!response.ok) return null;

            const data = await response.json();
            try { sessionStorage.setItem(RATES_KEY, JSON.stringify(data)); } catch (e) { }
            return data;
        } catch (e) {
            return null;
        }
    }

    loadRates().then(data => {
        if (data && data.rates) {
            rates = data.rates;
            const updated = document.getElementById('currencyUpdated');
            if (updated) updated.textContent = data.isLive ? 'Live rates' : 'Approximate rates';
        } else {
            list.querySelectorAll('[data-code]').forEach(b => { if (b.dataset.code !== 'AZN') b.disabled = true; });
        }

        observer.observe(document.body, { childList: true, subtree: true, characterData: true });
        select(read(STORAGE_KEY) || 'AZN');
    });
})();
