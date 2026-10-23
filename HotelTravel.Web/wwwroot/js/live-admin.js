// Admin paneli: canlı bildirişlər (SignalR, API-dəki /hubs/notifications)
(function () {
    // Tərcümə: admin _Layout window.tripioAdminText-i (ingiliscə mətn -> seçilmiş dil) verir
    const texts = window.tripioAdminText || {};
    const tr = s => texts[s] || s;
    // =========================
    // 1. ?highlight=KOD -> həmin sətrə sürüş və vurğula (bildirişə klik edəndə)
    // =========================
    const highlight = new URLSearchParams(location.search).get('highlight');

    if (highlight) {
        window.addEventListener('load', () => {
            const code = highlight.trim().toUpperCase();
            const row = Array.from(document.querySelectorAll('tr, .card, li'))
                .find(el => el.textContent.toUpperCase().includes(code) && !el.querySelector('tr'));

            if (row) {
                row.classList.add('live-highlight');
                row.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        });
    }

    const cfg = window.tripioLive;
    if (!cfg || !cfg.apiBase || !cfg.token || typeof signalR === 'undefined') return;

    const bellBtn = document.getElementById('liveBellBtn');
    const bellIcon = document.getElementById('liveBellIcon');
    const panel = document.getElementById('livePanel');
    const list = document.getElementById('liveList');
    const countEl = document.getElementById('liveCount');
    const statusEl = document.getElementById('liveStatus');
    const toasts = document.getElementById('liveToasts');
    const soundBtn = document.getElementById('liveSoundToggle');
    const popupBtn = document.getElementById('livePopupToggle');

    const icons = { hotel: '🏨', flight: '✈️', taxi: '🚕', car: '🚗', trip: '🧳', cancel: '✖️' };
    let unread = 0;

    // =========================
    // 2. Səs / popup ayarları (brauzerdə yadda qalır)
    // =========================
    function load(key, fallback) {
        try {
            const v = localStorage.getItem(key);
            return v === null ? fallback : v === '1';
        } catch (e) { return fallback; }
    }

    function save(key, value) {
        try { localStorage.setItem(key, value ? '1' : '0'); } catch (e) { }
    }

    const settings = {
        sound: load('tripio-live-sound', true),
        popups: load('tripio-live-popups', true)
    };

    function renderSettings() {
        soundBtn.textContent = settings.sound ? tr('🔊 Sound on') : tr('🔇 Sound off');
        soundBtn.classList.toggle('off', !settings.sound);
        popupBtn.textContent = settings.popups ? tr('💬 Pop-ups on') : tr('🚫 Pop-ups off');
        popupBtn.classList.toggle('off', !settings.popups);
        bellIcon.textContent = settings.sound || settings.popups ? '🔔' : '🔕';
    }

    soundBtn.addEventListener('click', e => {
        e.stopPropagation();
        settings.sound = !settings.sound;
        save('tripio-live-sound', settings.sound);
        renderSettings();
    });

    popupBtn.addEventListener('click', e => {
        e.stopPropagation();
        settings.popups = !settings.popups;
        save('tripio-live-popups', settings.popups);
        renderSettings();
    });

    renderSettings();

    // =========================
    // 3. Panel
    // =========================
    bellBtn.addEventListener('click', () => {
        panel.hidden = !panel.hidden;
        if (!panel.hidden) { unread = 0; renderCount(); }
    });

    document.addEventListener('click', e => {
        if (!panel.hidden && !e.target.closest('#liveBell')) panel.hidden = true;
    });

    function renderCount() {
        countEl.hidden = unread === 0;
        countEl.textContent = unread > 9 ? '9+' : String(unread);
    }

    function setStatus(text, ok) {
        statusEl.textContent = text;
        statusEl.className = 'live-status ' + (ok ? 'ok' : 'off');
    }

    function buildItem(n) {
        const a = document.createElement('a');
        a.className = 'live-item live-' + n.type;
        a.href = n.url || '#';
        a.title = tr('Open');

        const icon = document.createElement('span');
        icon.className = 'live-icon';
        icon.textContent = icons[n.type] || '🔔';

        const body = document.createElement('span');
        body.className = 'live-body';

        const title = document.createElement('b');
        title.textContent = tr(n.title);

        const msg = document.createElement('small');
        msg.textContent = n.message;

        const time = document.createElement('em');
        time.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) +
            (n.code ? ' · ' + n.code : '') + ' · ' + tr('click to open');

        body.append(title, msg, time);
        a.append(icon, body);
        return a;
    }

    function beep() {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.frequency.value = 880;
            gain.gain.setValueAtTime(0.08, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.35);
            osc.connect(gain).connect(ctx.destination);
            osc.start();
            osc.stop(ctx.currentTime + 0.35);
        } catch (e) { /* səs bloklana bilər */ }
    }

    function onNotification(n) {
        const empty = list.querySelector('.live-empty');
        if (empty) empty.remove();

        list.prepend(buildItem(n));
        while (list.children.length > 20) list.lastChild.remove();

        if (panel.hidden) { unread++; renderCount(); }

        if (settings.popups) {
            const toast = buildItem(n);
            toast.classList.add('live-toast');
            toasts.prepend(toast);
            setTimeout(() => toast.classList.add('hide'), 6000);
            setTimeout(() => toast.remove(), 6600);
        }

        bellBtn.classList.remove('ring');
        void bellBtn.offsetWidth;
        bellBtn.classList.add('ring');

        if (settings.sound) beep();
    }

    // =========================
    // 4. SignalR
    // =========================
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(cfg.apiBase + '/hubs/notifications', { accessTokenFactory: () => cfg.token })
        .withAutomaticReconnect()
        .build();

    connection.on('adminNotification', onNotification);
    connection.onreconnecting(() => setStatus(tr('reconnecting…'), false));
    connection.onreconnected(() => setStatus(tr('live'), true));
    connection.onclose(() => setStatus(tr('offline'), false));

    connection.start()
        .then(() => setStatus(tr('live'), true))
        .catch(() => setStatus(tr('offline'), false));
})();
