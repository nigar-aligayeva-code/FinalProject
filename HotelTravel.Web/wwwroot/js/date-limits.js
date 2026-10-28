// Tarix sahələri: il 4 rəqəmdən uzun yazılmasın (məs. 10/28/275750).
// max verilməyən hər <input type="date">-ə 2099-12-31 qoyulur - brauzer ili 4 rəqəmlə məhdudlaşdırır
// və bundan böyük tarixlə formu göndərmir.
(function () {
    "use strict";

    var MAX = "2099-12-31";
    var MIN = "2000-01-01";

    function limit(root) {
        (root || document).querySelectorAll('input[type="date"]').forEach(function (input) {
            if (!input.max) input.max = MAX;
            if (!input.min) input.min = MIN;
        });
    }

    // Ay/gün düzdür, amma il səhv yazılıbsa (≥ 2100) dərhal xəbərdarlıq
    document.addEventListener("change", function (e) {
        var input = e.target;
        if (!input || input.type !== "date" || !input.value) return;

        input.setCustomValidity("");
        if (!input.checkValidity()) input.reportValidity();
    });

    if (document.readyState === "loading")
        document.addEventListener("DOMContentLoaded", function () { limit(); });
    else
        limit();
})();
