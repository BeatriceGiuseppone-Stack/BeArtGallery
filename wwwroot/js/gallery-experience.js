// BeArtGallery — tema "Luce di sala"
// Fa seguire al faretto la posizione del cursore sulle opere.
// Aggiorna solo due variabili CSS (--gx-x, --gx-y): tutto l'effetto
// visivo è descritto in gallery-experience.css.

(function () {
    "use strict";

    const selectors = [
        ".artwork-catalog-image-wrapper",
        ".artist-artwork-image-link",
        ".gallery-hero-image-wrapper"
    ].join(",");

    // Su dispositivi touch o con movimento ridotto il faretto resta fermo.
    const canHover = window.matchMedia("(hover: hover)").matches;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (!canHover || reduceMotion) {
        return;
    }

    let frame = null;

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
