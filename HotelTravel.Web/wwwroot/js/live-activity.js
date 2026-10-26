// Sayt: "Someone just booked ..." canlı mesajları (anonim, şəxsi məlumat yoxdur)
(function () {
    const cfg = window.tripioLive;
    if (!cfg || !cfg.apiBase || typeof signalR === 'undefined') return;

    const box = document.getElementById('liveActivity');
    const guestBox = document.getElementById('guestToast');
    if (!box && !guestBox) return;

    const icons = { hotel: '🏨', flight: '✈️', taxi: '🚕', car: '🚗', trip: '🧳' };
    let hideTimer = null;
    let lastShown = 0;

    // Mesajı seçilmiş dildə qururuq (şablonlar _Layout-dakı window.tripioText-dədir)
    function localize(a) {
        const t = window.tripioText || {};
        const template = a.city ? t['act_' + a.type] : t['act_' + a.type + '_any'];
        return template ? template.replace('{0}', a.city || '') : a.message;
    }

    function show(a) {
        // Çox tez-tez göstərməyək
        const now = Date.now();
        if (now - lastShown < 4000) return;
        lastShown = now;

        box.querySelector('.la-icon').textContent = icons[a.type] || '✨';
        box.querySelector('.la-text').textContent = localize(a);
        box.classList.add('show');

        clearTimeout(hideTimer);
        hideTimer = setTimeout(() => box.classList.remove('show'), 6000);
    }

    if (box) {
        box.querySelector('.la-close').addEventListener('click', () => box.classList.remove('show'));
    }

    // ===== Qonağın öz bildirişi (təsdiq / rədd / ləğv / xatırlatma) =====
    const guestIcons = {
        requested: '📨', confirmed: '✅', rejected: '❌', cancelled: '⚠️',
        cancelled_by_you: '🗑️', cancelled_by_you_fee: '🧾', flight_cancelled: '🗑️', flight_cancelled_fee: '🧾',
        hotel_reminder: '🏨', hotel_tomorrow: '🧳', flight_reminder: '✈️', flight_soon: '⏰'
    };
    const guestTone = { rejected: 'bad', cancelled: 'bad', cancelled_by_you_fee: 'bad', flight_cancelled_fee: 'bad', hotel_reminder: 'info', hotel_tomorrow: 'info', flight_reminder: 'info', flight_soon: 'info' };

    // Otel rezervasiyası (BK-...) -> birbaşa həmin rezervasiyanın səhifəsi
    function urlFor(n) {
        if (n.code && /^BK-/i.test(n.code))
            return '/Bookings/Confirmation?confirmationCode=' + encodeURIComponent(n.code);
        return n.url || '/Bookings/MyBookings';
    }

    function guestText(n) {
        const t = (window.tripioText || {})['g_' + n.kind];
        if (!t) return n.message;
        return t.replace('{0}', n.place || '').replace('{1}', n.code || '').replace('{2}', n.when || '');
    }

    function showGuest(n) {
        if (!guestBox) return;

        guestBox.classList.remove('bad', 'info');
        if (guestTone[n.kind]) guestBox.classList.add(guestTone[n.kind]);

        guestBox.querySelector('.gt-icon').textContent = guestIcons[n.kind] || '🔔';
        guestBox.querySelector('.gt-text span').textContent = guestText(n);
        guestBox.querySelector('.gt-text a').href = urlFor(n);
        guestBox.classList.add('show');

        // Səs: qısa "ding" (brauzer icazə verirsə)
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.frequency.value = 880;
            gain.gain.setValueAtTime(0.08, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.4);
            osc.connect(gain).connect(ctx.destination);
            osc.start();
            osc.stop(ctx.currentTime + 0.4);
        } catch (e) { /* səs olmasa da olar */ }
    }

    if (guestBox) {
        guestBox.querySelector('.gt-close').addEventListener('click', () => guestBox.classList.remove('show'));
    }

    // ===== 🔔 Bildiriş paneli (bazadan yüklənir, yeniləri canlı əlavə olunur) =====
    const notifBtn = document.getElementById('notifBtn');
    const notifPanel = document.getElementById('notifPanel');
    const notifList = document.getElementById('notifList');
    const notifCount = document.getElementById('notifCount');
    let unread = 0;

    function api(path, method) {
        return fetch(cfg.apiBase + path, {
            method: method || 'GET',
            headers: { 'Authorization': 'Bearer ' + cfg.token }
        });
    }

    function renderCount() {
        if (!notifCount) return;
        notifCount.hidden = unread === 0;
        notifCount.textContent = unread > 9 ? '9+' : String(unread);
    }

    function timeAgo(iso) {
        const d = iso ? new Date(iso.endsWith('Z') ? iso : iso + 'Z') : new Date();
        return d.toLocaleString([], { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
    }

    function buildItem(n) {
        const a = document.createElement('a');
        a.className = 'hh-notif-item' + (n.isRead ? '' : ' unread');
        a.href = urlFor(n);

        const icon = document.createElement('span');
        icon.className = 'hh-notif-icon';
        icon.textContent = guestIcons[n.kind] || '🔔';

        const body = document.createElement('span');
        body.className = 'hh-notif-body';

        const text = document.createElement('span');
        text.textContent = guestText(n);

        const meta = document.createElement('em');
        meta.textContent = timeAgo(n.createdAt) + (n.code ? ' · ' + n.code : '');

        body.append(text, meta);
        a.append(icon, body);
        return a;
    }

    function addToList(n, atTop) {
        if (!notifList) return;
        const empty = notifList.querySelector('.hh-notif-empty');
        if (empty) empty.remove();

        const item = buildItem(n);
        if (atTop) notifList.prepend(item); else notifList.append(item);
        while (notifList.children.length > 30) notifList.lastChild.remove();
    }

    // Eyni bildirişi hər səhifədə təkrar göstərməmək üçün
    function sessionStorageHas(key) {
        try { return sessionStorage.getItem(key) === '1'; } catch (e) { return false; }
    }

    function sessionStorageSet(key) {
        try { sessionStorage.setItem(key, '1'); } catch (e) { }
    }

    async function loadNotifications() {
        if (!notifList || !cfg.token) return;
        try {
            const res = await api('/api/notifications/mine?take=20');
            if (!res.ok) return;
            const items = await res.json();
            if (items.length) notifList.innerHTML = '';
            items.forEach(n => addToList(n, false));
            unread = items.filter(n => !n.isRead).length;
            renderCount();

            // Səhifə yenilənəndə (məs. "Ləğv et" düyməsindən sonra) canlı mesaj itməsin:
            // son 2 dəqiqədə gələn oxunmamış bildirişi pəncərədə göstər
            const fresh = items.find(n => !n.isRead && n.createdAt &&
                Date.now() - new Date(n.createdAt.endsWith('Z') ? n.createdAt : n.createdAt + 'Z').getTime() < 120000);

            if (fresh && !sessionStorageHas('tripio-toast-' + fresh.id)) {
                sessionStorageSet('tripio-toast-' + fresh.id);
                showGuest(fresh);
            }
        } catch (e) { /* API işləmirsə səssizcə keç */ }
    }

    if (notifBtn && notifPanel) {
        notifBtn.addEventListener('click', e => {
            e.stopPropagation();
            notifPanel.hidden = !notifPanel.hidden;

            // Açılanda hamısı oxundu
            if (!notifPanel.hidden && unread > 0) {
                unread = 0;
                renderCount();
                api('/api/notifications/read-all', 'POST').catch(() => { });
                setTimeout(() => notifList.querySelectorAll('.unread').forEach(el => el.classList.remove('unread')), 2500);
            }
        });

        document.addEventListener('click', e => {
            if (!notifPanel.hidden && !e.target.closest('#notifMenu')) notifPanel.hidden = true;
        });

        loadNotifications();
    }

    function onGuestNotification(n) {
        if (n.id) sessionStorageSet('tripio-toast-' + n.id);
        showGuest(n);
        addToList(n, true);

        if (!notifPanel || notifPanel.hidden) { unread++; renderCount(); }

        if (notifBtn) {
            notifBtn.classList.remove('ring');
            void notifBtn.offsetWidth;
            notifBtn.classList.add('ring');
        }
    }

    const options = cfg.token ? { accessTokenFactory: () => cfg.token } : {};

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(cfg.apiBase + '/hubs/notifications', options)
        .withAutomaticReconnect()
        .build();

    if (box) connection.on('publicActivity', show);
    connection.on('guestNotification', onGuestNotification);
    connection.start().catch(() => { /* API işləmirsə səssizcə keç */ });
})();
