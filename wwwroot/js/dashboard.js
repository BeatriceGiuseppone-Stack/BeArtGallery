// Grafici della dashboard con la libreria Chart.js
document.addEventListener("DOMContentLoaded", function () {

    // Legge i dati che la pagina ha scritto in formato JSON
    const dataElement = document.getElementById("dashboardData");

    if (!dataElement) {
        return;
    }

    const data = JSON.parse(dataElement.textContent);

    createBookingsChart(data.bookings);
    createInquiriesChart(data.inquiries);
});

// Grafico a linee delle prenotazioni negli ultimi 6 mesi
function createBookingsChart(data) {

    const canvas = document.getElementById("bookingsChart");

    if (!canvas) {
        return;
    }

    describeChart(canvas, "Prenotazioni negli ultimi 6 mesi", data);

    // Crea il grafico con i colori della galleria
    new Chart(canvas, {
        type: "line",
        data: {
            labels: data.map(item => item.Label),
            datasets: [{
                label: "Prenotazioni",
                data: data.map(item => item.Value),
                borderColor: "#8c1d36",
                backgroundColor: "rgba(140, 29, 54, 0.10)",
                borderWidth: 2,
                tension: 0.35,
                fill: true,
                pointRadius: 4,
                pointBackgroundColor: "#8c1d36"
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { precision: 0 },
                    grid: { color: "rgba(0,0,0,0.06)" }
                },
                x: {
                    grid: { display: false }
                }
            }
        }
    });
}

// Grafico a ciambella delle richieste per stato
function createInquiriesChart(data) {

    const canvas = document.getElementById("inquiriesChart");

    if (!canvas) {
        return;
    }

    describeChart(canvas, "Richieste di informazioni", [
        { Label: "Nuove", Value: data.newCount },
        { Label: "In lavorazione", Value: data.inProgressCount },
        { Label: "Chiuse", Value: data.closedCount }
    ]);

    // Crea il grafico
    new Chart(canvas, {
        type: "doughnut",
        data: {
            labels: ["Nuove", "In lavorazione", "Chiuse"],
            datasets: [{
                data: [data.newCount, data.inProgressCount, data.closedCount],
                backgroundColor: ["#8c1d36", "#b8872e", "#e6dcd3"],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: "72%",
            plugins: {
                legend: { display: false }
            }
        }
    });
}

// Descrive il grafico a parole per i lettori di schermo,
// perché il disegno nel canvas da solo non si può leggere
function describeChart(canvas, title, data) {

    const values = data.map(item => item.Label + ": " + item.Value).join(", ");

    canvas.setAttribute("role", "img");
    canvas.setAttribute("aria-label", title + ". " + values + ".");
}