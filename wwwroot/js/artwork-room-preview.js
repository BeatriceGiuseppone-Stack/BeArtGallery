document.addEventListener("DOMContentLoaded", function () {

    const canvas =
        document.getElementById("roomPreviewCanvas");

    if (!canvas) {
        return;
    }


    const ctx =
        canvas.getContext("2d");


    const roomPhoto =
        document.getElementById("roomPhoto");

    const referenceInput =
        document.getElementById("referenceCm");

    const calibrationButton =
        document.getElementById("startCalibration");

    const restoreButton =
        document.getElementById("restoreRealScale");

    const freePreviewButton =
        document.getElementById("freePreviewMode");

    const realScaleButton =
        document.getElementById("realScaleMode");

    const freeModeHint =
        document.getElementById("freeModeHint");

    const realScaleControls =
        document.getElementById("realScaleControls");

    const status =
        document.getElementById("roomPreviewStatus");

    const placeholder =
        document.getElementById("roomPreviewPlaceholder");


    const artworkImageUrl =
        canvas.dataset.artworkImage;

    const artworkWidthCm =
        parseFloat(canvas.dataset.artworkWidth);

    const artworkHeightCm =
        parseFloat(canvas.dataset.artworkHeight);


    const roomImage =
        new Image();

    const artworkImage =
        new Image();


    artworkImage.src =
        artworkImageUrl;


    let roomLoaded = false;
    let artworkLoaded = false;


    let previewMode =
        "free";


    let calibrationMode =
        false;

    let calibrationPoints =
        [];


    let pixelsPerCm =
        null;


    let dragging =
        false;

    let resizing =
        false;


    let dragOffsetX =
        0;

    let dragOffsetY =
        0;


    const artwork = {

        x: 0,
        y: 0,

        width: 0,
        height: 0,

        realWidth: 0,
        realHeight: 0

    };


    /* =================================================
       CARICAMENTO OPERA
       ================================================= */

    artworkImage.onload = function () {

        artworkLoaded =
            true;

        redraw();

    };


    /* =================================================
       CARICAMENTO FOTO
       ================================================= */

    roomPhoto.addEventListener(
        "change",
        function () {

            const file =
                this.files?.[0];


            if (!file) {
                return;
            }


            const objectUrl =
                URL.createObjectURL(file);


            roomImage.onload = function () {

                const maxWidth =
                    1600;

                const maxHeight =
                    1100;


                const scale =
                    Math.min(
                        maxWidth /
                        roomImage.naturalWidth,

                        maxHeight /
                        roomImage.naturalHeight,

                        1
                    );


                canvas.width =
                    Math.round(
                        roomImage.naturalWidth *
                        scale
                    );


                canvas.height =
                    Math.round(
                        roomImage.naturalHeight *
                        scale
                    );


                roomLoaded =
                    true;


                placeholder.classList.add(
                    "d-none"
                );


                canvas.classList.remove(
                    "d-none"
                );


                calibrationPoints =
                    [];

                pixelsPerCm =
                    null;


                restoreButton.disabled =
                    true;


                applyFreeScale();

                centerArtwork();


                setFreeMode();


                redraw();


                URL.revokeObjectURL(
                    objectUrl
                );

            };


            roomImage.src =
                objectUrl;

        });


    /* =================================================
       MODALITÀ LIBERA
       ================================================= */

    freePreviewButton.addEventListener(
        "click",
        function () {

            setFreeMode();

        });


    function setFreeMode() {

        previewMode =
            "free";


        calibrationMode =
            false;

        calibrationPoints =
            [];


        freePreviewButton.classList.add(
            "active"
        );

        realScaleButton.classList.remove(
            "active"
        );

        freePreviewButton.setAttribute("aria-pressed", "true");
        realScaleButton.setAttribute("aria-pressed", "false");


        freeModeHint.classList.remove(
            "d-none"
        );

        realScaleControls.classList.add(
            "d-none"
        );


        if (roomLoaded) {

            if (artwork.width <= 0) {

                applyFreeScale();

                centerArtwork();

            }


            setStatus(
                "Anteprima libera: trascina e ridimensiona l'opera come preferisci."
            );

        }
        else {

            setStatus(
                "Carica una foto per iniziare."
            );

        }


        redraw();

    }


    /* =================================================
       MODALITÀ SCALA REALE
       ================================================= */

    realScaleButton.addEventListener(
        "click",
        function () {

            previewMode =
                "real";


            calibrationMode =
                false;

            calibrationPoints =
                [];


            realScaleButton.classList.add(
                "active"
            );

            freePreviewButton.classList.remove(
                "active"
            );

            realScaleButton.setAttribute("aria-pressed", "true");
            freePreviewButton.setAttribute("aria-pressed", "false");


            freeModeHint.classList.add(
                "d-none"
            );

            realScaleControls.classList.remove(
                "d-none"
            );


            if (!roomLoaded) {

                setStatus(
                    "Carica una foto della stanza per utilizzare la scala reale."
                );

            }
            else if (!pixelsPerCm) {

                setStatus(
                    "Inserisci una misura reale e seleziona i due estremi sulla foto."
                );

            }
            else {

                setStatus(
                    "Scala reale calibrata."
                );

            }


            redraw();

        });


    /* =================================================
       INIZIO CALIBRAZIONE
       ================================================= */

    calibrationButton.addEventListener(
        "click",
        function () {

            if (!roomLoaded) {

                setStatus(
                    "Prima carica una foto della stanza.",
                    true
                );

                return;

            }


            const referenceCm =
                parseFloat(
                    referenceInput.value
                );


            if (!referenceCm ||
                referenceCm <= 0) {

                setStatus(
                    "Inserisci una misura reale valida.",
                    true
                );

                return;

            }


            calibrationMode =
                true;


            calibrationPoints =
                [];


            setStatus(
                "Clicca il primo estremo dell'elemento di riferimento."
            );


            redraw();

        });


    /* =================================================
       RIPRISTINO SCALA REALE
       ================================================= */

    restoreButton.addEventListener(
        "click",
        function () {

            if (!pixelsPerCm) {
                return;
            }


            applyRealScale();

            centerArtwork();

            redraw();


            setStatus(
                "Opera ripristinata alla dimensione reale stimata."
            );

        });


    /* =================================================
       INTERAZIONE CANVAS
       ================================================= */

    canvas.addEventListener(
        "pointerdown",
        function (event) {

            const point =
                getCanvasPoint(event);


            if (previewMode === "real" &&
                calibrationMode) {

                calibrationPoints.push(
                    point
                );


                if (calibrationPoints.length === 1) {

                    setStatus(
                        "Ora seleziona il secondo estremo."
                    );

                }


                if (calibrationPoints.length === 2) {

                    completeCalibration();

                }


                redraw();

                return;

            }


            if (!artwork.width ||
                !artwork.height) {

                return;

            }


            if (isOnResizeHandle(
                point.x,
                point.y)) {

                resizing =
                    true;


                canvas.setPointerCapture(
                    event.pointerId
                );


                return;

            }


            if (isInsideArtwork(
                point.x,
                point.y)) {

                dragging =
                    true;


                dragOffsetX =
                    point.x -
                    artwork.x;


                dragOffsetY =
                    point.y -
                    artwork.y;


                canvas.setPointerCapture(
                    event.pointerId
                );

            }

        });


    canvas.addEventListener(
        "pointermove",
        function (event) {

            const point =
                getCanvasPoint(event);


            if (dragging) {

                artwork.x =
                    point.x -
                    dragOffsetX;


                artwork.y =
                    point.y -
                    dragOffsetY;


                keepArtworkInsideCanvas();

                redraw();

            }


            if (resizing) {

                resizeArtwork(
                    point
                );

            }

        });


    canvas.addEventListener(
        "pointerup",
        stopInteraction);


    canvas.addEventListener(
        "pointercancel",
        stopInteraction);


    /* =================================================
       TASTIERA
       Frecce: spostano l'opera (con Maiusc di più).
       + e -: ingrandiscono o riducono l'opera.
       ================================================= */

    canvas.addEventListener(
        "keydown",
        function (event) {

            if (!artwork.width ||
                !artwork.height ||
                calibrationMode) {
                return;
            }

            const step =
                event.shiftKey ? 25 : 5;

            let handled = true;

            switch (event.key) {

                case "ArrowLeft":
                    artwork.x -= step;
                    break;

                case "ArrowRight":
                    artwork.x += step;
                    break;

                case "ArrowUp":
                    artwork.y -= step;
                    break;

                case "ArrowDown":
                    artwork.y += step;
                    break;

                case "+":
                case "=":
                    resizeArtwork({
                        x: artwork.x + artwork.width * 1.05
                    });
                    break;

                case "-":
                case "_":
                    resizeArtwork({
                        x: artwork.x + artwork.width * 0.95
                    });
                    break;

                default:
                    handled = false;
            }

            if (!handled) {
                return;
            }

            event.preventDefault();

            keepArtworkInsideCanvas();

            redraw();
        });


    function stopInteraction(event) {

        dragging =
            false;

        resizing =
            false;


        if (canvas.hasPointerCapture(
            event.pointerId)) {

            canvas.releasePointerCapture(
                event.pointerId
            );

        }

    }


    /* =================================================
       RIDIMENSIONAMENTO
       ================================================= */

    function resizeArtwork(point) {

        const newWidth =
            Math.max(
                40,
                point.x -
                artwork.x
            );


        const ratio =
            artworkHeightCm /
            artworkWidthCm;


        artwork.width =
            newWidth;


        artwork.height =
            newWidth *
            ratio;


        keepArtworkInsideCanvas();


        if (previewMode === "real" &&
            artwork.realWidth > 0) {

            const percentage =
                Math.round(
                    artwork.width /
                    artwork.realWidth *
                    100
                );


            setStatus(
                `Scala personalizzata: ${percentage}% della dimensione reale stimata.`
            );

        }
        else {

            setStatus(
                "Anteprima libera: dimensione dell'opera modificata."
            );

        }


        redraw();

    }


    /* =================================================
       CALIBRAZIONE
       ================================================= */

    function completeCalibration() {

        const referenceCm =
            parseFloat(
                referenceInput.value
            );


        const first =
            calibrationPoints[0];


        const second =
            calibrationPoints[1];


        const distance =
            Math.hypot(
                second.x -
                first.x,

                second.y -
                first.y
            );


        if (distance < 10) {

            calibrationPoints =
                [];


            setStatus(
                "I due punti sono troppo vicini. Ripeti la misurazione.",
                true
            );


            return;

        }


        pixelsPerCm =
            distance /
            referenceCm;


        applyRealScale();

        centerArtwork();


        calibrationMode =
            false;


        restoreButton.disabled =
            false;


        setStatus(
            `Scala reale calcolata: l'opera misura ${formatNumber(artworkWidthCm)} × ${formatNumber(artworkHeightCm)} cm.`
        );


        redraw();

    }


    /* =================================================
       SCALA LIBERA
       ================================================= */

    function applyFreeScale() {

        if (!canvas.width ||
            !canvas.height) {

            return;

        }


        const ratio =
            artworkHeightCm /
            artworkWidthCm;


        let width =
            canvas.width *
            0.18;


        let height =
            width *
            ratio;


        const maxHeight =
            canvas.height *
            0.4;


        if (height > maxHeight) {

            height =
                maxHeight;


            width =
                height /
                ratio;

        }


        artwork.width =
            width;

        artwork.height =
            height;


        artwork.realWidth =
            0;

        artwork.realHeight =
            0;

    }


    /* =================================================
       SCALA REALE
       ================================================= */

    function applyRealScale() {

        artwork.realWidth =
            artworkWidthCm *
            pixelsPerCm;


        artwork.realHeight =
            artworkHeightCm *
            pixelsPerCm;


        artwork.width =
            artwork.realWidth;


        artwork.height =
            artwork.realHeight;


        fitArtworkIfTooLarge();

    }


    function fitArtworkIfTooLarge() {

        const maxWidth =
            canvas.width *
            0.9;


        const maxHeight =
            canvas.height *
            0.9;


        if (artwork.width <= maxWidth &&
            artwork.height <= maxHeight) {

            return;

        }


        const scale =
            Math.min(
                maxWidth /
                artwork.width,

                maxHeight /
                artwork.height
            );


        artwork.width *=
            scale;


        artwork.height *=
            scale;

    }


    /* =================================================
       POSIZIONAMENTO
       ================================================= */

    function centerArtwork() {

        artwork.x =
            (canvas.width -
                artwork.width) /
            2;


        artwork.y =
            (canvas.height -
                artwork.height) /
            2;

    }


    function keepArtworkInsideCanvas() {

        artwork.x =
            Math.max(
                0,
                Math.min(
                    artwork.x,
                    canvas.width -
                    artwork.width
                )
            );


        artwork.y =
            Math.max(
                0,
                Math.min(
                    artwork.y,
                    canvas.height -
                    artwork.height
                )
            );

    }


    /* =================================================
       DISEGNO
       ================================================= */

    function redraw() {

        ctx.clearRect(
            0,
            0,
            canvas.width,
            canvas.height
        );


        if (!roomLoaded) {
            return;
        }


        ctx.drawImage(
            roomImage,
            0,
            0,
            canvas.width,
            canvas.height
        );


        if (artworkLoaded &&
            artwork.width > 0 &&
            artwork.height > 0) {

            ctx.save();


            ctx.shadowColor =
                "rgba(0, 0, 0, 0.28)";


            ctx.shadowBlur =
                18;


            ctx.shadowOffsetY =
                8;


            ctx.drawImage(
                artworkImage,
                artwork.x,
                artwork.y,
                artwork.width,
                artwork.height
            );


            ctx.restore();


            drawResizeHandle();

        }


        drawCalibration();

    }


    function drawCalibration() {

        if (!calibrationMode ||
            calibrationPoints.length === 0) {

            return;

        }


        ctx.save();


        ctx.strokeStyle =
            "#9b7b4f";


        ctx.fillStyle =
            "#9b7b4f";


        ctx.lineWidth =
            4;


        calibrationPoints.forEach(
            point => {

                ctx.beginPath();


                ctx.arc(
                    point.x,
                    point.y,
                    8,
                    0,
                    Math.PI * 2
                );


                ctx.fill();

            });


        if (calibrationPoints.length === 2) {

            ctx.beginPath();


            ctx.moveTo(
                calibrationPoints[0].x,
                calibrationPoints[0].y
            );


            ctx.lineTo(
                calibrationPoints[1].x,
                calibrationPoints[1].y
            );


            ctx.stroke();

        }


        ctx.restore();

    }


    function drawResizeHandle() {

        const size =
            getHandleSize();


        ctx.save();


        ctx.fillStyle =
            "#191918";


        ctx.fillRect(
            artwork.x +
            artwork.width -
            size,

            artwork.y +
            artwork.height -
            size,

            size,
            size
        );


        ctx.strokeStyle =
            "#ffffff";


        ctx.lineWidth =
            2;


        ctx.strokeRect(
            artwork.x +
            artwork.width -
            size,

            artwork.y +
            artwork.height -
            size,

            size,
            size
        );


        ctx.restore();

    }


    /* =================================================
       HIT TEST
       ================================================= */

    function isInsideArtwork(x, y) {

        return (
            x >= artwork.x &&
            x <= artwork.x +
            artwork.width &&

            y >= artwork.y &&
            y <= artwork.y +
            artwork.height
        );

    }


    function isOnResizeHandle(x, y) {

        const size =
            getHandleSize();


        return (
            x >= artwork.x +
            artwork.width -
            size &&

            x <= artwork.x +
            artwork.width &&

            y >= artwork.y +
            artwork.height -
            size &&

            y <= artwork.y +
            artwork.height
        );

    }


    function getHandleSize() {

        return Math.max(
            18,
            canvas.width *
            0.015
        );

    }


    /* =================================================
       COORDINATE
       ================================================= */

    function getCanvasPoint(event) {

        const rect =
            canvas.getBoundingClientRect();


        return {

            x:
                (event.clientX -
                    rect.left) *
                canvas.width /
                rect.width,

            y:
                (event.clientY -
                    rect.top) *
                canvas.height /
                rect.height

        };

    }


    /* =================================================
       UTILITÀ
       ================================================= */

    function setStatus(
        message,
        isError = false) {

        status.textContent =
            message;


        status.classList.toggle(
            "is-error",
            isError
        );

    }


    function formatNumber(value) {

        return value.toLocaleString(
            "it-IT",
            {
                maximumFractionDigits: 1
            }
        );

    }

});