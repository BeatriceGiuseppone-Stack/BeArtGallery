// "Visualizza nella tua stanza": il visitatore carica una foto della parete
// e vede l'opera appesa. L'opera si sposta, si ridimensiona e si può
// mettere in scala reale indicando una misura nota sulla foto.
// La foto resta nel browser: non viene inviata al server.
document.addEventListener("DOMContentLoaded", function () {

    // Elementi della pagina
    const canvas =
        document.getElementById("roomPreviewCanvas");

    if (!canvas) {
        return;
    }

    const ctx = canvas.getContext("2d");

    const roomPhoto = document.getElementById("roomPhoto");

    const referenceInput = document.getElementById("referenceCm");

    const calibrationButton = document.getElementById("startCalibration");

    const restoreButton = document.getElementById("restoreRealScale");

    const freePreviewButton = document.getElementById("freePreviewMode");

    const realScaleButton = document.getElementById("realScaleMode");

    const freeModeHint = document.getElementById("freeModeHint");

    const realScaleControls = document.getElementById("realScaleControls");

    const status = document.getElementById("roomPreviewStatus");

    const placeholder = document.getElementById("roomPreviewPlaceholder");

    // Immagine e dimensioni reali dell'opera, lette dagli attributi data- del canvas
    const artworkImageUrl = canvas.dataset.artworkImage;

    const artworkWidthCm = parseFloat(canvas.dataset.artworkWidth);

    const artworkHeightCm = parseFloat(canvas.dataset.artworkHeight);

    // Immagini della stanza e dell'opera
    const roomImage = new Image();

    const artworkImage = new Image();

    artworkImage.src = artworkImageUrl;

    // Stato dell'anteprima
    let roomLoaded = false;
    let artworkLoaded = false;

    // Modalità: "free" (libera) o "real" (scala reale)
    let previewMode = "free";

    // Calibrazione: due punti sulla foto e quanti pixel valgono un centimetro
    let calibrationMode = false;

    let calibrationPoints = [];

    let pixelsPerCm = null;

    // Trascinamento e ridimensionamento in corso
    let dragging = false;

    let resizing = false;

    // Distanza tra il mouse e l'angolo dell'opera mentre la si trascina
    let dragOffsetX = 0;

    let dragOffsetY = 0;

    // Posizione e dimensioni dell'opera nel canvas
    const artwork = {
        x: 0,
        y: 0,
        width: 0,
        height: 0,
        realWidth: 0,
        realHeight: 0
    };

    // Quando l'immagine dell'opera è pronta si ridisegna
    artworkImage.onload = function () {
        artworkLoaded = true;
        redraw();
    };

    // Caricamento della foto della stanza scelta dal visitatore
    roomPhoto.addEventListener("change",
        function () {

            const file = this.files?.[0];

            if (!file) {
                return;
            }

            const objectUrl = URL.createObjectURL(file);

            roomImage.onload = function () {

                const maxWidth = 1600;

                const maxHeight = 1100;

                const scale = Math.min(maxWidth / roomImage.naturalWidth, maxHeight / roomImage.naturalHeight, 1);

                canvas.width = Math.round(roomImage.naturalWidth * scale);

                canvas.height = Math.round(roomImage.naturalHeight * scale);

                roomLoaded = true;

                placeholder.classList.add("d-none");

                canvas.classList.remove("d-none");

                calibrationPoints = [];

                pixelsPerCm = null;

                restoreButton.disabled = true;

                applyFreeScale();

                centerArtwork();

                setFreeMode();

                redraw();

                URL.revokeObjectURL(objectUrl);
            };

            roomImage.src = objectUrl;

        });

    // Pulsante "Anteprima libera"
    freePreviewButton.addEventListener("click", function () { setFreeMode(); });

    // Modalità libera: l'opera si ridimensiona a mano
    function setFreeMode() {

        previewMode = "free";

        calibrationMode = false;

        calibrationPoints = [];

        freePreviewButton.classList.add("active");

        realScaleButton.classList.remove("active");

        freePreviewButton.setAttribute("aria-pressed", "true");
        realScaleButton.setAttribute("aria-pressed", "false");

        freeModeHint.classList.remove("d-none");

        realScaleControls.classList.add("d-none");

        if (roomLoaded) {

            if (artwork.width <= 0) {

                applyFreeScale();
                centerArtwork();
            }

            setStatus("Anteprima libera: trascina e ridimensiona l'opera come preferisci.");
        }
        else {

            setStatus("Carica una foto per iniziare.");
        }

        redraw();

    }

    // Pulsante "Scala reale": prima bisogna calibrare
    realScaleButton.addEventListener("click",
        function () {

            previewMode = "real";
            calibrationMode = false;
            calibrationPoints = [];
            realScaleButton.classList.add("active");
            freePreviewButton.classList.remove("active");
            realScaleButton.setAttribute("aria-pressed", "true");
            freePreviewButton.setAttribute("aria-pressed", "false");
            freeModeHint.classList.add("d-none");
            realScaleControls.classList.remove("d-none");

            if (!roomLoaded) {
                setStatus("Carica una foto della stanza per utilizzare la scala reale.");
            }
            else if (!pixelsPerCm) {
                setStatus("Inserisci una misura reale e seleziona i due estremi sulla foto.");
            }
            else {
                setStatus("Scala reale calibrata.");
            }

            redraw();
        });

    // Avvia la calibrazione: il visitatore clicca due punti a distanza nota
    calibrationButton.addEventListener("click", function () {

        if (!roomLoaded) {
            setStatus("Prima carica una foto della stanza.", true);
            return;
        }

        const referenceCm = parseFloat(referenceInput.value);

        if (!referenceCm || referenceCm <= 0) {
            setStatus("Inserisci una misura reale valida.", true);
            return;
        }

        calibrationMode = true;
        calibrationPoints = [];

        setStatus("Clicca il primo estremo dell'elemento di riferimento.");

        redraw();
    });

    // Ripristina dimensioni e posizione iniziali
    restoreButton.addEventListener("click", function () {

        if (!pixelsPerCm) {
            return;
        }

        applyRealScale();
        centerArtwork();
        redraw();

        setStatus("Opera ripristinata alla dimensione reale stimata.");
    });

    // Clic sul canvas: calibrazione, ridimensionamento o trascinamento
    canvas.addEventListener("pointerdown", function (event) {

        const point = getCanvasPoint(event);

        if (previewMode === "real" && calibrationMode) {

            calibrationPoints.push(point);

            if (calibrationPoints.length === 1) {
                setStatus("Ora seleziona il secondo estremo.");
            }

            if (calibrationPoints.length === 2) {
                completeCalibration();
            }

            redraw();
            return;
        }

        if (!artwork.width || !artwork.height) {
            return;
        }

        if (isOnResizeHandle(point.x, point.y)) {
            resizing = true;
            canvas.setPointerCapture(event.pointerId);
            return;
        }

        if (isInsideArtwork(point.x, point.y)) {
            dragging = true;
            dragOffsetX = point.x - artwork.x;
            dragOffsetY = point.y - artwork.y;
            canvas.setPointerCapture(event.pointerId);
        }
    });

    // Movimento del mouse: sposta o ridimensiona l'opera
    canvas.addEventListener("pointermove", function (event) {

        const point = getCanvasPoint(event);

        if (dragging) {
            artwork.x = point.x - dragOffsetX;
            artwork.y = point.y - dragOffsetY;
            keepArtworkInsideCanvas();
            redraw();
        }

        if (resizing) {
            resizeArtwork(point);
        }
    });

    // Fine del trascinamento
    canvas.addEventListener("pointerup", stopInteraction);
    canvas.addEventListener("pointercancel", stopInteraction);

    // Uso da tastiera: frecce per spostare l'opera (con Shift si va più veloci)
    canvas.addEventListener("keydown", function (event) {

        if (!artwork.width || !artwork.height || calibrationMode) {
            return;
        }

        const step = event.shiftKey ? 25 : 5;
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
                resizeArtwork({ x: artwork.x + artwork.width * 1.05 });
                break;
            case "-":
            case "_":
                resizeArtwork({ x: artwork.x + artwork.width * 0.95 });
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

    // Ferma trascinamento e ridimensionamento
    function stopInteraction(event) {

        dragging = false;
        resizing = false;

        if (canvas.hasPointerCapture(event.pointerId)) {
            canvas.releasePointerCapture(event.pointerId);
        }
    }

    // Cambia la larghezza dell'opera mantenendo le proporzioni
    function resizeArtwork(point) {

        const newWidth = Math.max(40, point.x - artwork.x);
        const ratio = artworkHeightCm / artworkWidthCm;

        artwork.width = newWidth;
        artwork.height = newWidth * ratio;

        keepArtworkInsideCanvas();

        if (previewMode === "real" && artwork.realWidth > 0) {
            const percentage = Math.round(artwork.width / artwork.realWidth * 100);
            setStatus(`Scala personalizzata: ${percentage}% della dimensione reale stimata.`);
        }
        else {
            setStatus("Anteprima libera: dimensione dell'opera modificata.");
        }

        redraw();
    }

    // Fine della calibrazione: calcola quanti pixel corrispondono a un centimetro
    function completeCalibration() {

        const referenceCm = parseFloat(referenceInput.value);
        const first = calibrationPoints[0];
        const second = calibrationPoints[1];
        const distance = Math.hypot(second.x - first.x, second.y - first.y);

        if (distance < 10) {
            calibrationPoints = [];
            setStatus("I due punti sono troppo vicini. Ripeti la misurazione.", true);
            return;
        }

        pixelsPerCm = distance / referenceCm;

        applyRealScale();
        centerArtwork();

        calibrationMode = false;
        restoreButton.disabled = false;

        setStatus(`Scala reale calcolata: l'opera misura ${formatNumber(artworkWidthCm)} × ${formatNumber(artworkHeightCm)} cm.`);

        redraw();
    }

    // Dimensioni iniziali in modalità libera
    function applyFreeScale() {

        if (!canvas.width || !canvas.height) {
            return;
        }

        const ratio = artworkHeightCm / artworkWidthCm;
        let width = canvas.width * 0.18;
        let height = width * ratio;
        const maxHeight = canvas.height * 0.4;

        if (height > maxHeight) {
            height = maxHeight;
            width = height / ratio;
        }

        artwork.width = width;
        artwork.height = height;
        artwork.realWidth = 0;
        artwork.realHeight = 0;
    }

    // Dimensioni in scala reale usando i centimetri dell'opera
    function applyRealScale() {

        artwork.realWidth = artworkWidthCm * pixelsPerCm;
        artwork.realHeight = artworkHeightCm * pixelsPerCm;
        artwork.width = artwork.realWidth;
        artwork.height = artwork.realHeight;

        fitArtworkIfTooLarge();
    }

    // Rimpicciolisce l'opera se è più grande della foto
    function fitArtworkIfTooLarge() {

        const maxWidth = canvas.width * 0.9;
        const maxHeight = canvas.height * 0.9;

        if (artwork.width <= maxWidth && artwork.height <= maxHeight) {
            return;
        }

        const scale = Math.min(maxWidth / artwork.width, maxHeight / artwork.height);

        artwork.width *= scale;
        artwork.height *= scale;
    }

    // Mette l'opera al centro della foto
    function centerArtwork() {
        artwork.x = (canvas.width - artwork.width) / 2;
        artwork.y = (canvas.height - artwork.height) / 2;
    }

    // Impedisce che l'opera esca dal canvas
    function keepArtworkInsideCanvas() {
        artwork.x = Math.max(0, Math.min(artwork.x, canvas.width - artwork.width));
        artwork.y = Math.max(0, Math.min(artwork.y, canvas.height - artwork.height));
    }

    // Ridisegna tutto: foto, opera, punti della calibrazione e maniglia
    function redraw() {

        ctx.clearRect(0, 0, canvas.width, canvas.height);

        if (!roomLoaded) {
            return;
        }

        ctx.drawImage(roomImage, 0, 0, canvas.width, canvas.height);

        if (artworkLoaded && artwork.width > 0 && artwork.height > 0) {

            ctx.save();
            ctx.shadowColor = "rgba(0, 0, 0, 0.28)";
            ctx.shadowBlur = 18;
            ctx.shadowOffsetY = 8;
            ctx.drawImage(artworkImage, artwork.x, artwork.y, artwork.width, artwork.height);
            ctx.restore();

            drawResizeHandle();
        }

        drawCalibration();
    }

    // Disegna i punti e la linea della calibrazione
    function drawCalibration() {

        if (!calibrationMode || calibrationPoints.length === 0) {
            return;
        }

        ctx.save();
        ctx.strokeStyle = "#9b7b4f";
        ctx.fillStyle = "#9b7b4f";
        ctx.lineWidth = 4;

        calibrationPoints.forEach(point => {
            ctx.beginPath();
            ctx.arc(point.x, point.y, 8, 0, Math.PI * 2);
            ctx.fill();
        });

        if (calibrationPoints.length === 2) {
            ctx.beginPath();
            ctx.moveTo(calibrationPoints[0].x, calibrationPoints[0].y);
            ctx.lineTo(calibrationPoints[1].x, calibrationPoints[1].y);
            ctx.stroke();
        }

        ctx.restore();
    }

    // Disegna la maniglia per ridimensionare (angolo in basso a destra)
    function drawResizeHandle() {

        const size = getHandleSize();

        ctx.save();
        ctx.fillStyle = "#191918";
        ctx.fillRect(artwork.x + artwork.width - size, artwork.y + artwork.height - size, size, size);
        ctx.strokeStyle = "#ffffff";
        ctx.lineWidth = 2;
        ctx.strokeRect(artwork.x + artwork.width - size, artwork.y + artwork.height - size, size, size);
        ctx.restore();
    }

    // Il punto è sopra l'opera?
    function isInsideArtwork(x, y) {
        return (x >= artwork.x && x <= artwork.x + artwork.width &&
            y >= artwork.y && y <= artwork.y + artwork.height);
    }

    // Il punto è sopra la maniglia?
    function isOnResizeHandle(x, y) {

        const size = getHandleSize();

        return (x >= artwork.x + artwork.width - size && x <= artwork.x + artwork.width &&
            y >= artwork.y + artwork.height - size && y <= artwork.y + artwork.height);
    }

    // Dimensione della maniglia in base alla grandezza del canvas
    function getHandleSize() {
        return Math.max(18, canvas.width * 0.015);
    }

    // Converte la posizione del mouse in coordinate del canvas
    function getCanvasPoint(event) {

        const rect = canvas.getBoundingClientRect();

        return {
            x: (event.clientX - rect.left) * canvas.width / rect.width,
            y: (event.clientY - rect.top) * canvas.height / rect.height
        };
    }

    // Mostra un messaggio di stato sotto l'anteprima
    function setStatus(message, isError = false) {
        status.textContent = message;
        status.classList.toggle("is-error", isError);
    }

    // Numero con una cifra decimale in formato italiano
    function formatNumber(value) {
        return value.toLocaleString("it-IT", { maximumFractionDigits: 1 });
    }

});