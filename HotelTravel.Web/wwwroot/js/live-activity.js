// Sayt: "Someone just booked ..." canlı mesajları (anonim, şəxsi məlumat yoxdur)
(function () {
    const cfg = window.tripioLive;
    if (!cfg || !cfg.apiBase || typeof signalR === 'undefined') return;

    const box = document.getElementById('liveActivity');
    if (!box) return;

    const icons = { hotel: '🏨', flight: '✈️', taxi: '🚕', car: '🚗', trip: '🧳' };
    let hideTimer = null;
    let lastShown = 0;

    function show(a) {
        // Çox tez-tez göstərməyək
        const now = Date.now();
        if (now - lastShown < 4000) return;
        lastShown = now;

        box.querySelector('.la-icon').textContent = icons[a.type] || '✨';
        box.querySelector('.la-text').textContent = a.message;
        box.classList.add('show');

        clearTimeout(hideTimer);
        hideTimer = setTimeout(() => box.classList.remove('show'), 6000);
    }

    box.querySelector('.la-close').addEventListener('click', () => box.classList.remove('show'));

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(cfg.apiBase + '/hubs/notifications')
        .withAutomaticReconnect()
        .build();

    connection.on('publicActivity', show);
    connection.start().catch(() => { /* API işləmirsə səssizcə keç */ });
})();
