using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// รับข้อความจากฝั่ง Flutter ผ่าน flutter_embed_unity
/// Flutter เรียกด้วย: sendToUnity('FlutterBridge', 'SetVehicleId', vehicleId);
///                     sendToUnity('FlutterBridge', 'SetEVData', jsonString);
///
/// ต้องมี GameObject ชื่อ "FlutterBridge" (ตรงตัว) อยู่ใน Scene แรกที่ Unity โหลด
/// แล้วแปะ component นี้ไว้ - ชื่อ GameObject ต้องตรงกับ argument แรกของ
/// sendToUnity เพราะ flutter_embed_unity ใช้ UnitySendMessage(objectName, methodName, value)
/// อยู่เบื้องหลัง ซึ่งเป็นกลไกเดียวกับ UnityPlayer.UnitySendMessage บน Android
/// </summary>
public class FlutterBridge : MonoBehaviour
{
    public static FlutterBridge Instance { get; private set; }

    [Tooltip("รถ EV ที่กำลังแสดงผลอยู่ตอนนี้ (ว่าง = ยังไม่ได้รับจาก Flutter)")]
    public string CurrentVehicleId { get; private set; } = "";

    [Tooltip("ข้อมูล EV ล่าสุดที่ได้รับจาก Flutter (SetEVData) - null ถ้ายังไม่เคยได้รับ")]
    public EVDataPayload CurrentData { get; private set; }

    [Serializable]
    public class VehicleIdEvent : UnityEvent<string> { }

    [Tooltip("ยิง event นี้เมื่อได้รับ vehicleId ใหม่จาก Flutter - " +
             "เอาไปผูกกับสคริปต์ที่โหลด/แสดงข้อมูล EV ใน AR scene ได้ใน Inspector")]
    public VehicleIdEvent OnVehicleIdReceived;

    /// <summary>
    /// โครงสร้างข้อมูล EV ที่ส่งมาจาก Flutter เป็น JSON (ดู EVData.toJson()
    /// ฝั่ง Dart ใน frontend/lib/models/ev_data.dart) - ชื่อ field ต้องตรงกัน
    /// ตัวต่อตัวเพราะ JsonUtility ของ Unity map แบบ 1:1 ตามชื่อ
    /// </summary>
    [Serializable]
    public class EVDataPayload
    {
        public string vehicleId;
        public float soc;
        public float voltage;
        public float batteryTemperature;
        public int motorRpm;
        public float motorPower;
        public string status;
    }

    [Serializable]
    public class EVDataEvent : UnityEvent<EVDataPayload> { }

    [Tooltip("ยิง event นี้เมื่อได้รับข้อมูล EV (SOC/Voltage/Temp/RPM/Power/Status) " +
             "ใหม่จาก Flutter - เอาไปผูกกับ UI การ์ดแสดงผลใน AR scene ได้")]
    public EVDataEvent OnEVDataReceived;

    void Awake()
    {
        // กัน GameObject ชื่อซ้ำหรือถูกสร้างซ้ำตอนเปลี่ยน Scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// เรียกจากฝั่ง Flutter: sendToUnity('FlutterBridge', 'SetVehicleId', 'EV001')
    /// ชื่อ method ต้องตรงตัวกับ argument ที่สองของ sendToUnity
    /// </summary>
    public void SetVehicleId(string vehicleId)
    {
        if (string.IsNullOrEmpty(vehicleId))
        {
            Debug.LogWarning("[FlutterBridge] SetVehicleId ได้รับค่าว่าง - ข้าม");
            return;
        }

        Debug.Log($"[FlutterBridge] ได้รับ vehicleId จาก Flutter: {vehicleId}");

        CurrentVehicleId = vehicleId;
        OnVehicleIdReceived?.Invoke(vehicleId);
    }

    /// <summary>
    /// เรียกจากฝั่ง Flutter: sendToUnity('FlutterBridge', 'SetEVData', jsonEncode(data))
    /// data คือค่าที่ EVData.toJson() ฝั่ง Dart สร้างขึ้น ส่งมาทุกครั้งที่ Flutter
    /// poll ข้อมูลใหม่จาก backend (ทุก 5 วิ) เพื่อให้การ์ด AR overlay อัปเดตสด
    /// </summary>
    public void SetEVData(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[FlutterBridge] SetEVData ได้รับค่าว่าง - ข้าม");
            return;
        }

        try
        {
            var payload = JsonUtility.FromJson<EVDataPayload>(json);
            if (payload == null)
            {
                Debug.LogWarning($"[FlutterBridge] SetEVData parse ไม่สำเร็จ: {json}");
                return;
            }

            CurrentData = payload;
            if (!string.IsNullOrEmpty(payload.vehicleId))
            {
                CurrentVehicleId = payload.vehicleId;
            }

            OnEVDataReceived?.Invoke(payload);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FlutterBridge] SetEVData เกิดข้อผิดพลาด: {e.Message}\njson={json}");
        }
    }

    /// <summary>
    /// เรียกใช้จากสคริปต์อื่นฝั่ง Unity เมื่ออยากส่งข้อความกลับไป Flutter
    /// (เช่น แจ้งว่า AR marker เจอแล้ว, โหลด object เสร็จแล้ว ฯลฯ) - ใช้
    /// SendToFlutter.Send(message) จาก package com.learntoflutter.flutter_embed_unity_6000_0
    /// (ติดตั้งผ่าน Package Manager > Install package from git URL) ฝั่ง Flutter
    /// รับข้อความนี้ผ่าน EmbedUnity(onMessageFromUnity: ...) ใน ar_view_page.dart
    /// </summary>
    public void NotifyFlutter(string message)
    {
        Debug.Log($"[FlutterBridge] ส่งกลับ Flutter: {message}");
        // ผูกกับ flutter_embed_unity แล้ว (import ผ่าน Package Manager > Install
        // package from git URL ตอนนี้อยู่ใน Packages/manifest.json) - ฝั่ง Flutter
        // รับผ่าน EmbedUnity widget's onMessageFromUnity callback (ดู
        // ar_view_page.dart) SendToFlutter.Send เป็น static class เปล่าๆ ไม่มี
        // namespace ตามที่ package กำหนดมา
        SendToFlutter.Send(message);
    }
}
