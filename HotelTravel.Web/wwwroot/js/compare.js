// Otaq müqayisəsi: seçilmiş otaqlar brauzerdə saxlanılır (ən çox 3),
// aşağıda kiçik panel göstərilir, "Compare" düyməsi /Rooms/Compare?ids=... açır.
(function () {
    const KEY = 'tripio-compare';
    const MAX = 3;
    const root = document.body.dataset.compareUrl || '/Rooms/Compare';

    // Tərcümələr _Layout-dan gəlir (window.tripioText); yoxdursa ingiliscə
    const text = Object.assign({
        compare: 'Compare',
        added: 'Added to compare',
        addRoom: 'Add room',
        clear: 'Clear',
        max: 'You can compare up to 3 rooms. Remove one first.'
    }, window.tripioText || {});

    function read() {
        try { return JSON.parse(localStorage.getItem(KEY)) || []; } catch (e) { return []; }
    }
    function write(items) {
        try { localStorage.setItem(KEY, JSON.stringify(items)); } catch (e) { }
    }

    let items = read();

    // ---------- Panel ----------
    const bar = document.createElement('div');
    bar.className = 'cmp-bar';
    bar.setAttribute('aria-live', 'polite');
    document.body.appendChild(bar);

    function esc(s) {
        return String(s || '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    function url() { return root + '?ids=' + items.map(i => i.id).join(','); }

    function renderBar() {
        const onComparePage = location.pathname.toLowerCase().indexOf('/rooms/compare') === 0;

        if (!items.length || onComparePage) {
            bar.classList.remove('show');
            return;
        }

        const slots = [];
        for (let i = 0; i < MAX; i++) {
            const it = items[i];
            slots.push(it
                ? `<div class="cmp-slot" title="${esc(it.name)}">
                       ${it.img ? `<img src="${esc(it.img)}" alt="">` : '<i class="bi bi-image"></i>'}
                       <span>${esc(it.name)}</span>
                       <button type="button" data-compare-remove="${it.id}" aria-label="Remove"><i class="bi bi-x"></i></button>
                   </div>`
                : `<div class="cmp-slot empty"><i class="bi bi-plus"></i><span>${esc(text.addRoom)}</span></div>`);
        }

        bar.innerHTML = `
            <div class="cmp-slots">${slots.join('')}</div>
            <div class="cmp-actions">
                <button type="button" class="cmp-clear" data-compare-clear>${esc(text.clear)}</button>
                <a class="hh-btn hh-btn-primary ${items.length < 2 ? 'disabled' : ''}" href="${url()}">
                    ${esc(text.compare)} (${items.length})
                </a>
            </div>`;
        bar.classList.add('show');
    }

    function renderButtons() {
        document.querySelectorAll('[data-compare-id]').forEach(btn => {
            const on = items.some(i => i.id === +btn.dataset.compareId);
            btn.classList.toggle('on', on);
            btn.setAttribute('aria-pressed', on ? 'true' : 'false');
            const label = btn.querySelector('.cmp-toggle-text');
            if (label) label.textContent = on ? text.added : text.compare;
        });
    }

    function render() { renderBar(); renderButtons(); }

    function toast(text) {
        const t = document.createElement('div');
        t.className = 'cmp-toast';
        t.textContent = text;
        document.body.appendChild(t);
        setTimeout(() => t.remove(), 2600);
    }

    // ---------- Klik hadisələri ----------
    document.addEventListener('click', e => {
        const toggle = e.target.closest('[data-compare-id]');
        if (toggle) {
            e.preventDefault();
            const id = +toggle.dataset.compareId;

            if (items.some(i => i.id === id)) {
                items = items.filter(i => i.id !== id);
            } else if (items.length >= MAX) {
                toast(text.max);
                return;
            } else {
                items.push({ id: id, name: toggle.dataset.compareName, img: toggle.dataset.compareImg });
            }

            write(items);
            render();
            return;
        }

        const remove = e.target.closest('[data-compare-remove]');
        if (remove) {
            const id = +remove.dataset.compareRemove;
            items = items.filter(i => i.id !== id);
            write(items);

            // Müqayisə səhifəsindəyiksə, səhifəni yeni siyahı ilə açırıq
            if (location.pathname.toLowerCase().indexOf('/rooms/compare') === 0) location.href = url();
            else render();
            return;
        }

        if (e.target.closest('[data-compare-clear]')) {
            items = [];
            write(items);
            render();
        }
    });

    // Başqa tabda dəyişəndə bu tab da yenilənsin
    window.addEventListener('storage', ev => {
        if (ev.key === KEY) { items = read(); render(); }
    });

    window.tripioCompare = {
        sync: function (serverItems) {
            items = (serverItems || []).slice(0, MAX);
            write(items);
            render();
        }
    };

    render();
})();
