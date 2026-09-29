const express = require("express");
const cors = require("cors");
const { Pool } = require("pg");

const app = express();
const PORT = 3000;

// Middleware
app.use(cors());
app.use(express.json());

// =====================================
// PostgreSQL
// =====================================

const pool = new Pool({
    user: "postgres",
    host: "localhost",
    database: "ev_project",
    password: "123456",
    port: 5432
});
app.get("/ev/:vehicleId", async (req, res) => {
  try {
    const { vehicleId } = req.params;

    const result = await pool.query(
      `
      SELECT *
      FROM ev_data
      WHERE vehicle_id = $1
      ORDER BY id DESC
      LIMIT 1
      `,
      [vehicleId]
    );

    if (result.rows.length === 0) {
      return res.status(404).json({
        success: false,
        message: "ไม่พบข้อมูลรถ"
      });
    }

    res.json({
      success: true,
      data: result.rows[0]
    });

  } catch (error) {
    console.error(error);

    res.status(500).json({
      success: false,
      message: "Database error"
    });
  }
});

// =====================================
// Test Backend
// =====================================

app.get("/", (req, res) => {
    res.json({
        message: "EV Backend is running"
    });
});

// =====================================
// รับข้อมูลจาก EV Simulator
// =====================================

app.post("/api/ev-data", async (req, res) => {

    try {

        const {
            vehicle_id,
            soc,
            voltage,
            battery_temperature,
            motor_rpm,
            motor_power,
            status
        } = req.body;

        const result = await pool.query(
            `
            INSERT INTO ev_data
            (
                vehicle_id,
                soc,
                voltage,
                battery_temperature,
                motor_rpm,
                motor_power,
                status
            )
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            RETURNING *;
            `,
            [
                vehicle_id,
                soc,
                voltage,
                battery_temperature,
                motor_rpm,
                motor_power,
                status
            ]
        );

        res.json({
            success: true,
            data: result.rows[0]
        });

    } catch (error) {

        console.error(error);

        res.status(500).json({
            success: false,
            message: "Database error"
        });

    }

});

// =====================================
// ดึงข้อมูลล่าสุดของรถ
// =====================================

app.get("/api/ev-data/:vehicle_id", async (req, res) => {

    try {

        const { vehicle_id } = req.params;

        const result = await pool.query(
            `
            SELECT *
            FROM ev_data
            WHERE vehicle_id = $1
            ORDER BY id DESC
            LIMIT 1;
            `,
            [vehicle_id]
        );

        if (result.rows.length === 0) {

            return res.status(404).json({
                success: false,
                message: "Vehicle not found"
            });

        }

        res.json({
            success: true,
            data: result.rows[0]
        });

    } catch (error) {

        console.error(error);

        res.status(500).json({
            success: false,
            message: "Database error"
        });

    }

});

// =====================================
// เชื่อม Flutter (สแกน QR) เข้ากับ Unity (AR display)
//
// Flutter ยิง POST /api/scan ทันทีที่สแกนได้ vehicle_id
// Unity poll GET /api/scan/latest ทุกๆ รอบ เพื่อรู้ว่าตอนนี้ควร
// แสดงข้อมูลของรถคันไหนอยู่ (แทนที่จะ fix เป็น "EV001" ตายตัว)
//
// เก็บไว้ใน memory เฉยๆ (ไม่ต้องมีตารางใน DB) เพราะเป็นแค่สถานะ
// "ตอนนี้กำลังดูรถคันไหนอยู่" ไม่ใช่ข้อมูล telemetry ที่ต้องเก็บถาวร
// =====================================

let lastScannedVehicle = null; // { vehicle_id, scanned_at }

app.post("/api/scan", (req, res) => {

    const { vehicle_id } = req.body;

    if (!vehicle_id) {
        return res.status(400).json({
            success: false,
            message: "ต้องระบุ vehicle_id"
        });
    }

    lastScannedVehicle = {
        vehicle_id,
        scanned_at: new Date().toISOString()
    };

    console.log(`Scanned: ${vehicle_id}`);

    res.json({
        success: true,
        data: lastScannedVehicle
    });

});

app.get("/api/scan/latest", (req, res) => {

    if (!lastScannedVehicle) {
        return res.status(404).json({
            success: false,
            message: "ยังไม่มีการสแกน"
        });
    }

    res.json({
        success: true,
        data: lastScannedVehicle
    });

});

// =====================================
// Start Server
// =====================================

app.listen(PORT, () => {

    console.log(
        `EV Backend running at http://localhost:${PORT}`
    );

});
