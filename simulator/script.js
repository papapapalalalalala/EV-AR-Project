// ==========================================
// EV DATA SIMULATOR
// ==========================================

let isRunning = false;

let countdownTimer = null;

let countdown = 5;

let mode = "normal";

let dataCounter = 0;


// ==========================================
// INITIAL DATA
// ==========================================

let data = {

    soc: 85,

    voltage: 380,

    batteryTemp: 32,

    motorRpm: 3500,

    motorPower: 45

};


// ==========================================
// RANDOM NUMBER
// ==========================================

function randomNumber(min, max) {

    return Math.floor(
        Math.random() *
        (max - min + 1)
    ) + min;

}


// ==========================================
// NORMAL DATA
// ==========================================

function generateNormalData() {

    data.soc -=
        randomNumber(0, 1);


    data.voltage +=
        randomNumber(-3, 3);


    data.batteryTemp +=
        randomNumber(-1, 1);


    data.motorRpm +=
        randomNumber(-200, 200);


    data.motorPower +=
        randomNumber(-3, 3);


    // Keep values normal

    data.soc =
        Math.max(
            20,
            Math.min(100, data.soc)
        );


    data.voltage =
        Math.max(
            330,
            Math.min(420, data.voltage)
        );


    data.batteryTemp =
        Math.max(
            20,
            Math.min(45, data.batteryTemp)
        );


    data.motorRpm =
        Math.max(
            500,
            Math.min(6000, data.motorRpm)
        );


    data.motorPower =
        Math.max(
            5,
            Math.min(80, data.motorPower)
        );

}


// ==========================================
// ABNORMAL DATA
// ==========================================

function generateAbnormalData() {

    let problem =
        randomNumber(1, 5);


    // LOW SOC

    if (problem === 1) {

        data.soc =
            randomNumber(5, 19);

        data.voltage =
            randomNumber(350, 390);

        data.batteryTemp =
            randomNumber(25, 40);

        data.motorRpm =
            randomNumber(2000, 4500);

        data.motorPower =
            randomNumber(20, 60);
    }


    // LOW VOLTAGE

    else if (problem === 2) {

        data.soc =
            randomNumber(40, 80);

        data.voltage =
            randomNumber(280, 329);

        data.batteryTemp =
            randomNumber(25, 40);

        data.motorRpm =
            randomNumber(2000, 4500);

        data.motorPower =
            randomNumber(20, 60);
    }


    // BATTERY OVERHEAT

    else if (problem === 3) {

        data.soc =
            randomNumber(40, 80);

        data.voltage =
            randomNumber(350, 400);

        data.batteryTemp =
            randomNumber(51, 70);

        data.motorRpm =
            randomNumber(2000, 4500);

        data.motorPower =
            randomNumber(20, 60);
    }


    // MOTOR RPM HIGH

    else if (problem === 4) {

        data.soc =
            randomNumber(40, 80);

        data.voltage =
            randomNumber(350, 400);

        data.batteryTemp =
            randomNumber(30, 45);

        data.motorRpm =
            randomNumber(6001, 7500);

        data.motorPower =
            randomNumber(50, 90);
    }


    // MOTOR POWER HIGH

    else {

        data.soc =
            randomNumber(40, 80);

        data.voltage =
            randomNumber(350, 400);

        data.batteryTemp =
            randomNumber(30, 45);

        data.motorRpm =
            randomNumber(3000, 5500);

        data.motorPower =
            randomNumber(81, 100);
    }

}


// ==========================================
// RANDOM MODE
// ==========================================

function generateRandomData() {

    // 70% normal
    // 30% abnormal

    if (Math.random() < 0.7) {

        generateNormalData();

    } else {

        generateAbnormalData();

    }

}


// ==========================================
// CHECK STATUS
// ==========================================

function checkStatus() {

    return {

        soc:
            data.soc < 20,

        voltage:
            data.voltage < 330 ||
            data.voltage > 420,

        batteryTemp:
            data.batteryTemp > 50,

        motorRpm:
            data.motorRpm > 6000,

        motorPower:
            data.motorPower > 80

    };

}


// ==========================================
// UPDATE STATUS BADGE
// ==========================================

function setStatus(
    id,
    abnormal
) {

    const element =
        document.getElementById(id);


    if (abnormal) {

        element.textContent =
            "ABNORMAL";

        element.className =
            "badge abnormal";

    } else {

        element.textContent =
            "NORMAL";

        element.className =
            "badge normal";

    }

}


// ==========================================
// UPDATE OVERALL STATUS
// ==========================================

function updateOverallStatus(status) {

    const abnormal =

        status.soc ||

        status.voltage ||

        status.batteryTemp ||

        status.motorRpm ||

        status.motorPower;


    const element =
        document.getElementById(
            "overall"
        );


    if (abnormal) {

        element.textContent =
            "⚠ VEHICLE STATUS: ABNORMAL";

        element.className =
            "overall abnormal";

    } else {

        element.textContent =
            "✓ VEHICLE STATUS: NORMAL";

        element.className =
            "overall normal";

    }

}


// ==========================================
// UPDATE CURRENT DATA
// ==========================================

function updateDisplay() {

    document.getElementById(
        "soc"
    ).textContent =
        data.soc;


    document.getElementById(
        "voltage"
    ).textContent =
        data.voltage;


    document.getElementById(
        "batteryTemp"
    ).textContent =
        data.batteryTemp;


    document.getElementById(
        "motorRpm"
    ).textContent =
        data.motorRpm;


    document.getElementById(
        "motorPower"
    ).textContent =
        data.motorPower;


    const status =
        checkStatus();


    setStatus(
        "socStatus",
        status.soc
    );


    setStatus(
        "voltageStatus",
        status.voltage
    );


    setStatus(
        "batteryTempStatus",
        status.batteryTemp
    );


    setStatus(
        "motorRpmStatus",
        status.motorRpm
    );


    setStatus(
        "motorPowerStatus",
        status.motorPower
    );


    updateOverallStatus(status);

}


// ==========================================
// ADD HISTORY
// ==========================================

function addHistory() {

    const table =
        document.getElementById(
            "history"
        );


    const status =
        checkStatus();


    const abnormal =

        status.soc ||

        status.voltage ||

        status.batteryTemp ||

        status.motorRpm ||

        status.motorPower;


    const now =
        new Date();


    const time =
        now.toLocaleTimeString(
            "en-GB"
        );


    const row =
        document.createElement(
            "tr"
        );


    row.innerHTML = `

        <td>${time}</td>

        <td>${data.soc}%</td>

        <td>${data.voltage} V</td>

        <td>${data.batteryTemp} °C</td>

        <td>${data.motorRpm}</td>

        <td>${data.motorPower} kW</td>

        <td class="${
            abnormal
                ? "abnormal-text"
                : "normal-text"
        }">

            ${
                abnormal
                    ? "⚠ ABNORMAL"
                    : "✓ NORMAL"
            }

        </td>

    `;


    table.prepend(row);


    // เก็บ 10 รายการล่าสุด

    while (
        table.children.length > 10
    ) {

        table.removeChild(
            table.lastChild
        );

    }

}

// ==========================================
// SEND DATA TO BACKEND
// ==========================================

async function sendDataToBackend() {

    const status = checkStatus();

    const abnormal =
        status.soc ||
        status.voltage ||
        status.batteryTemp ||
        status.motorRpm ||
        status.motorPower;

    try {

        const response = await fetch(
            "http://localhost:3000/api/ev-data",
            {
                method: "POST",

                headers: {
                    "Content-Type": "application/json"
                },

                body: JSON.stringify({
                    vehicle_id: "EV001",

                    soc: data.soc,

                    voltage: data.voltage,

                    battery_temperature:
                        data.batteryTemp,

                    motor_rpm:
                        data.motorRpm,

                    motor_power:
                        data.motorPower,

                    status:
                        abnormal
                            ? "ABNORMAL"
                            : "NORMAL"
                })
            }
        );

        const result = await response.json();

        console.log(
            "Data sent to PostgreSQL:",
            result
        );

    } catch (error) {

        console.error(
            "Cannot connect to Backend:",
            error
        );

    }

}

// ==========================================
// GENERATE DATA
// ==========================================

function generateData() {

    if (mode === "normal") {
        generateNormalData();
    }

    else if (mode === "abnormal") {
        generateAbnormalData();
    }

    else {
        generateRandomData();
    }

    updateDisplay();
    addHistory();

    // ส่งข้อมูลไป Backend
    sendDataToBackend();
}


// ==========================================
// COUNTDOWN
// ==========================================

function startCountdown() {

    countdown = 5;

    document.getElementById(
        "countdown"
    ).textContent =
        countdown;


    clearInterval(
        countdownTimer
    );


    countdownTimer =
        setInterval(
            function () {

                countdown--;


                document.getElementById(
                    "countdown"
                ).textContent =
                    countdown;


                if (countdown <= 0) {

                    generateData();

                    countdown = 5;

                }

            },
            1000
        );

}


// ==========================================
// START / STOP
// ==========================================

document
    .getElementById("toggleBtn")
    .addEventListener(
        "click",
        function () {


            // ======================
            // START
            // ======================

            if (!isRunning) {

                isRunning = true;


                // Generate immediately

                generateData();


                // Countdown drives the next generateData() call
                // (single synced timer, no more drift)

                startCountdown();


                this.textContent =
                    "■ STOP";

                this.className =
                    "stop";


                document.getElementById(
                    "runningText"
                ).textContent =
                    "RUNNING";


                document.getElementById(
                    "dot"
                ).className =
                    "dot running";


                document.getElementById(
                    "runningWrap"
                ).className =
                    "running active";


                document.getElementById(
                    "countdownSection"
                ).classList.remove(
                    "idle"
                );

            }


            // ======================
            // STOP
            // ======================

            else {

                isRunning = false;


                clearInterval(
                    countdownTimer
                );


                this.textContent =
                    "▶ START";

                this.className =
                    "start";


                document.getElementById(
                    "runningText"
                ).textContent =
                    "STOPPED";


                document.getElementById(
                    "dot"
                ).className =
                    "dot";


                document.getElementById(
                    "runningWrap"
                ).className =
                    "running";


                document.getElementById(
                    "countdown"
                ).textContent =
                    "–";


                document.getElementById(
                    "countdownSection"
                ).classList.add(
                    "idle"
                );

            }

        }
    );


// ==========================================
// MODE BUTTONS
// ==========================================

function selectMode(
    selectedMode,
    buttonId
) {

    mode =
        selectedMode;


    document
        .querySelectorAll(".mode")
        .forEach(
            button => {

                button.classList.remove(
                    "active"
                );

            }
        );


    document
        .getElementById(buttonId)
        .classList.add(
            "active"
        );

}


// NORMAL

document
    .getElementById("normalBtn")
    .addEventListener(
        "click",
        function () {

            selectMode(
                "normal",
                "normalBtn"
            );


            data = {

                soc: 85,

                voltage: 380,

                batteryTemp: 32,

                motorRpm: 3500,

                motorPower: 45

            };


            updateDisplay();

        }
    );


// ABNORMAL

document
    .getElementById("abnormalBtn")
    .addEventListener(
        "click",
        function () {

            selectMode(
                "abnormal",
                "abnormalBtn"
            );


            generateAbnormalData();

            updateDisplay();

            addHistory();

        }
    );


// RANDOM

document
    .getElementById("randomBtn")
    .addEventListener(
        "click",
        function () {

            selectMode(
                "random",
                "randomBtn"
            );


            generateRandomData();

            updateDisplay();

            addHistory();

        }
    );


// ==========================================
// INITIAL DISPLAY
// ==========================================

updateDisplay();

// Preload the starting reading into the history table
// so "Latest Data" never looks broken/empty on first load

addHistory();