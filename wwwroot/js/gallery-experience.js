// Effetti grafici del sito (tema della galleria).
// Il "faretto" segue il mouse sulle opere: lo script cambia solo
// due variabili CSS (--gx-x e --gx-y), l'effetto è nel CSS.

(function () {
    "use strict";

    // Elementi che hanno il faretto
    const selectors = [
        ".artwork-catalog-image-wrapper",
        ".artist-artwork-image-link",
        ".gx-wall-frame",
        ".gallery-hero-image-wrapper"
    ].join(",");

    // Su touch o con animazioni ridotte il faretto resta fermo
    const canHover = window.matchMedia("(hover: hover)").matches;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (!canHover || reduceMotion) {
        return;
    }

    let frame = null;
    let heroFrame = null;

    // Home: la luce segue il mouse su tutta la prima sezione
    const hero = document.querySelector(".gallery-hero");

    if (hero) {
        hero.addEventListener("pointermove", function (event) {
            if (heroFrame) {
                cancelAnimationFrame(heroFrame);
            }

            // Calcola la posizione del mouse in percentuale
            heroFrame = requestAnimationFrame(function () {
                const box = hero.getBoundingClientRect();
                const x = ((event.clientX - box.left) / box.width) * 100;
                const y = ((event.clientY - box.top) / box.height) * 100;

                hero.style.setProperty("--gx-hx", x.toFixed(1) + "%");
                hero.style.setProperty("--gx-hy", y.toFixed(1) + "%");
            });
        }, { passive: true });
    }

    // Pagina Artisti: l'opera di anteprima segue il mouse in orizzontale
    let previewFrame = null;

    document.addEventListener("pointermove", function (event) {
        const row = event.target.closest(".gx-artist-row.has-preview");

        if (!row) {
            return;
        }

        if (previewFrame) {
            cancelAnimationFrame(previewFrame);
        }

        previewFrame = requestAnimationFrame(function () {
            const box = row.getBoundingClientRect();
            // L'anteprima resta dentro la riga, lontano dal bordo destro
            const x = Math.min(Math.max(event.clientX - box.left, 0), box.width - 300);

            row.style.setProperty("--gx-px", Math.max(x, 0).toFixed(0) + "px");
        });
    }, { passive: true });

    // Faretto sulle opere
    document.addEventListener("pointermove", function (event) {
        const target = event.target.closest(selectors);

        if (!target) {
            return;
        }

        // Al massimo un aggiornamento per fotogramma, per non rallentare il browser
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

// Comparsa degli elementi quando entrano nello schermo
// e numeri della dashboard che salgono fino al valore
(function () {
    "use strict";

    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Niente animazioni se l'utente le ha disattivate
    if (reduceMotion || !("IntersectionObserver" in window)) {
        return;
    }

    // Elementi che compaiono con l'animazione
    const revealSelectors = [
        ".artwork-catalog-card",
        ".artist-card",
        ".artist-artwork-card",
        ".gx-wall-item",
        ".gx-agenda-item",
        ".gx-names li",
        ".gx-artist-group",
        ".gx-event",
        ".gx-ticket",
        ".gx-roster-row",
        ".inquiry-list-item",
        ".inquiry-stat-card",
        ".dashboard-kpi",
        ".dashboard-panel",
        ".favorite-card",
        ".about-info-card"
    ].join(",");

    // Si animano solo gli elementi sotto il bordo dello schermo:
    // quello che si vede all'apertura resta subito visibile.
    // Se la scheda è nascosta l'effetto non serve.
    const foldLine = window.innerHeight * 0.92;

    const items = document.visibilityState === "visible"
        ? Array.from(document.querySelectorAll(revealSelectors)).filter(function (el) { return el.getBoundingClientRect().top > foldLine; })
        : [];

    // Piccolo ritardo tra elementi vicini (massimo 5 passi)
    const counters = new Map();

    items.forEach(function (item) {
        const parent = item.parentElement;
        const index = counters.get(parent) || 0;

        counters.set(parent, index + 1);
        item.style.setProperty("--gx-i", Math.min(index, 5));
        item.classList.add("gx-reveal");
    });

    // Quando un elemento entra nello schermo diventa visibile
    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add("is-visible");
                observer.unobserve(entry.target);
            }
        });
    }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });

    items.forEach(function (item) {
        observer.observe(item);
    });

    // Numeri che salgono fino al valore (solo numeri interi, es. "12")
    const numbers = document.querySelectorAll(".dashboard-kpi strong, .inquiry-stat-card strong");

    const countObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) {
                return;
            }

            const el = entry.target;
            const target = parseInt(el.textContent.trim(), 10);

            countObserver.unobserve(el);

            if (isNaN(target) || target <= 0 || String(target) !== el.textContent.trim() || document.hidden) {
                return;
            }

            // Se la scheda viene nascosta a metà si mostra subito il valore finale
            const finish = function () {
                el.textContent = target;
            };

            document.addEventListener("visibilitychange", finish, { once: true });

            // Animazione di 900 millisecondi che rallenta alla fine
            const duration = 900;
            const start = performance.now();

            function tick(now) {
                const progress = Math.min((now - start) / duration, 1);
                const eased = 1 - Math.pow(1 - progress, 3);

                el.textContent = Math.round(target * eased);

                if (progress < 1) {
                    requestAnimationFrame(tick);
                } else {
                    document.removeEventListener("visibilitychange", finish);
                }
            }

            requestAnimationFrame(tick);
        });
    }, { threshold: 0.4 });

    numbers.forEach(function (el) {
        countObserver.observe(el);
    });
})();