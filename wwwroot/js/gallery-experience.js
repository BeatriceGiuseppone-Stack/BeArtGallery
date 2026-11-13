// BeArtGallery — tema "Luce di sala"
// Fa seguire al faretto la posizione del cursore sulle opere.
// Aggiorna solo due variabili CSS (--gx-x, --gx-y): tutto l'effetto
// visivo è descritto in gallery-experience.css.

(function () {
    "use strict";

    const selectors = [
        ".artwork-catalog-image-wrapper",
        ".artist-artwork-image-link",
        ".gx-wall-frame",
        ".gallery-hero-image-wrapper"
    ].join(",");

    // Su dispositivi touch o con movimento ridotto il faretto resta fermo.
    const canHover = window.matchMedia("(hover: hover)").matches;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (!canHover || reduceMotion) {
        return;
    }

    let frame = null;
    let heroFrame = null;

    // La luce della home segue il cursore su tutta la prima sezione.
    const hero = document.querySelector(".gallery-hero");

    if (hero) {
        hero.addEventListener("pointermove", function (event) {
            if (heroFrame) {
                cancelAnimationFrame(heroFrame);
            }

            heroFrame = requestAnimationFrame(function () {
                const box = hero.getBoundingClientRect();
                const x = ((event.clientX - box.left) / box.width) * 100;
                const y = ((event.clientY - box.top) / box.height) * 100;

                hero.style.setProperty("--gx-hx", x.toFixed(1) + "%");
                hero.style.setProperty("--gx-hy", y.toFixed(1) + "%");
            });
        }, { passive: true });
    }

    document.addEventListener("pointermove", function (event) {
        const target = event.target.closest(selectors);

        if (!target) {
            return;
        }

        // Un aggiornamento per fotogramma, per non appesantire il browser.
        if (frame) {
            cancelAnimationFrame(frame);
        }

        frame = requestAnimationFrame(function () {
            const box = target.getBoundingClientRect();
            const x = ((event.clientX - box.left) / box.width) * 100;
            const y = ((event.clientY - box.top) / box.height) * 100;

            target.style.setProperty("--gx-x", x.toFixed(1) + "%");
            target.style.setProperty("--gx-y", y.toFixed(1) + "%");
        });
    }, { passive: true });
})();
