document.addEventListener(
    "DOMContentLoaded",
    function () {

        const dataElement =
            document.getElementById(
                "dashboardData"
            );


        if (!dataElement) {
            return;
        }


        const data =
            JSON.parse(
                dataElement.textContent
            );


        createBookingsChart(
            data.bookings
        );


        createInquiriesChart(
            data.inquiries
        );

    });


function createBookingsChart(data) {

    const canvas =
        document.getElementById(
            "bookingsChart"
        );


    if (!canvas) {
        return;
    }


    describeChart(canvas, "Prenotazioni negli ultimi 6 mesi", data);


    new Chart(
        canvas,
        {
            type: "line",

            data: {

                labels:
                    data.map(
                        item =>
                            item.Label
                    ),

                datasets: [
                    {
                        label:
                            "Prenotazioni",

                        data:
                            data.map(
                                item =>
                                    item.Value
                            ),

                        borderColor:
                            "#9b7b4f",

                        backgroundColor:
                            "rgba(155, 123, 79, 0.10)",

                        borderWidth:
                            2,

                        tension:
                            0.35,

                        fill:
                            true,

                        pointRadius:
                            4,

                        pointBackgroundColor:
                            "#9b7b4f"
                    }
                ]

            },


            options: {

                responsive:
                    true,

                maintainAspectRatio:
                    false,

                plugins: {

                    legend: {
                        display: false
                    }

                },

                scales: {

                    y: {

                        beginAtZero:
                            true,

                        ticks: {

                            precision:
                                0

                        },

                        grid: {

                            color:
                                "rgba(0,0,0,0.06)"

                        }

                    },


                    x: {

                        grid: {

                            display:
                                false

                        }

                    }

                }

            }

        });

}


function createInquiriesChart(data) {

    const canvas =
        document.getElementById(
            "inquiriesChart"
        );


    if (!canvas) {
        return;
    }


    describeChart(canvas, "Richieste di informazioni", [
        { Label: "Nuove", Value: data.newCount },
        { Label: "In lavorazione", Value: data.inProgressCount },
        { Label: "Chiuse", Value: data.closedCount }
    ]);


    new Chart(
        canvas,
        {
            type:
                "doughnut",

            data: {

                labels: [
                    "Nuove",
                    "In lavorazione",
                    "Chiuse"
                ],

                datasets: [
                    {
                        data: [
                            data.newCount,
                            data.inProgressCount,
                            data.closedCount
                        ],

                        backgroundColor: [
                            "#9b7b4f",
                            "#191918",
                            "#ded9d1"
                        ],

                        borderWidth:
                            0
                    }
                ]

            },


            options: {

                responsive:
                    true,

                maintainAspectRatio:
                    false,

                cutout:
                    "72%",

                plugins: {

                    legend: {
                        display: false
                    }

                }

            }

        });

}


// Descrive il grafico a parole per i lettori di schermo:
// il canvas da solo non è leggibile.
function describeChart(canvas, title, data) {

    const values =
        data
            .map(item => item.Label + ": " + item.Value)
            .join(", ");

    canvas.setAttribute("role", "img");

    canvas.setAttribute(
        "aria-label",
        title + ". " + values + ".");
}
