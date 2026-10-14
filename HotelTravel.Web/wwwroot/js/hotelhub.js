// =========================================================
// HOTELHUB - site-wide scripts
// =========================================================

// ---------------------------------------------------------
// WISHLIST (ürək düyməsi)
// .hh-save düyməsi olan hər yerdə işləyir
// ---------------------------------------------------------
(function () {
    const body = document.body;

    function setSaved(button, saved) {
        button.classList.toggle("saved", saved);
        button.setAttribute("aria-pressed", saved ? "true" : "false");

        const icon = button.querySelector("i");
        if (icon) {
            icon.classList.toggle("bi-heart-fill", saved);
            icon.classList.toggle("bi-heart", !saved);
        }

        const text = button.querySelector(".hh-save-text");
        if (text) {
            text.textContent = saved ? "Saved" : "Save";
        }
    }

    function goToLogin() {
        // Axtarış nəticələri POST səhifəsidir, ora qayıtmaq olmur
        let returnUrl = window.location.pathname + window.location.search;
        if (window.location.pathname.toLowerCase().endsWith("/home/search")) {
            returnUrl = "/";
        }

        window.location.href =
            body.dataset.loginUrl + "?returnUrl=" + encodeURIComponent(returnUrl);
    }

    document.addEventListener("click", async function (event) {
        const button = event.target.closest(".hh-save");
        if (!button) return;

        event.preventDefault();
        event.stopPropagation();

        if (body.dataset.auth !== "1") {
            goToLogin();
            return;
        }

        const tokenInput = document.querySelector(
            '#hhAntiforgery input[name="__RequestVerificationToken"]');

        const roomId = button.dataset.roomId;

        button.disabled = true;

        try {
            const response = await fetch(body.dataset.wishlistUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded",
                    "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                },
                body: "roomId=" + encodeURIComponent(roomId)
            });

            if (!response.ok) {
                throw new Error("Request failed");
            }

            const data = await response.json();

            // Eyni otağın səhifədəki bütün ürəkləri yenilənsin
            document
                .querySelectorAll('.hh-save[data-room-id="' + roomId + '"]')
                .forEach(b => setSaved(b, data.saved));

            // "Saved" səhifəsində sildikdə kartı gizlət
            if (!data.saved && button.dataset.removeCard === "1") {
                const card = button.closest(".hh-room-wrap");
                if (card) {
                    card.remove();
                }

                const grid = document.getElementById("savedGrid");
                if (grid && grid.children.length === 0) {
                    grid.remove();
                    document.getElementById("savedEmpty")?.classList.remove("d-none");
                }
            }
        } catch (error) {
            alert("Could not update your saved rooms. Please try again.");
        } finally {
            button.disabled = false;
        }
    });
})();
