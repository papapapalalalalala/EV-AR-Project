using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// ⚠️ ไฟล์นี้ต้องอยู่ใต้โฟลเดอร์ Assets/Scripts/ (runtime script ธรรมดา)
///
/// ระบบ Marker/Image Tracking - ทางเลือกที่ผู้ใช้ต้องการใช้เป็นหลักแทนการ
/// "แตะพื้น" (ดู ARPlacementController.cs ซึ่งยังเก็บไว้เป็นทางเลือกสำรอง):
///
///   ติด Marker (รูป/QR เฉพาะสำหรับ AR - คนละอันกับ QR ที่ Flutter ใช้สแกน
///   ระบุรถ) ไว้บนตัวรถจริงจุดเดียว แล้วให้ ARTrackedImageManager ของ AR
///   Foundation ตามตำแหน่ง/การหมุนของ Marker นั้นแบบ real-time
///
///   Marker (ARTrackedImage.transform) - ใบเดียว ติดจุดเดียวบนรถ
///     ├── WorldSpace_UI_Battery (offset batteryOffset จาก marker)
///     └── WorldSpace_UI_Motor   (offset motorOffset จาก marker)
///
///   เพราะ AR Foundation จะอัปเดต transform ของ ARTrackedImage ให้เองทุกเฟรม
///   ตามผล tracking ของกล้อง ดังนั้นแค่ทำให้ Panel ทั้งสองเป็นลูกของมัน (คนละ
///   offset) Battery/Motor panel ก็จะ "ติดกับตำแหน่งรถจริง" ไปเองโดยอัตโนมัติ
///   - เดินรอบรถ/ขยับมือถือแล้ว panel ยังอยู่ตำแหน่งเดิมบนรถ (ข้อ ⭐ ที่
///   ผู้ใช้ต้องการ) โดยไม่ต้องเขียน logic คำนวณตำแหน่งเองเลย
///
///   ⚠️ วิธีนี้ (marker ใบเดียว + offset 2 ค่า) ใช้ได้เพราะตัวรถเป็นโครง
///   แข็ง (rigid body) - ระยะจริงจาก marker ไปแบตเตอรี่ และจาก marker ไป
///   มอเตอร์ คงที่เสมอไม่ว่ารถจะขยับ/หมุนยังไง "ต้องวัดระยะจริงจากรถ" แล้ว
///   ใส่ค่าใน batteryOffset / motorOffset ด้านล่างเอง (หน่วยเมตร) ค่า default
///   ที่ให้มาเป็นแค่ตัวอย่าง ไม่ตรงกับรถจริงแน่นอน (เหมือนกลไกเดียวกับ
///   ARPlacementController แค่ที่มาของ "anchor" เปลี่ยนจาก AR Plane Raycast
///   มาเป็น ARTrackedImage แทน - ใช้ hook เดียวกันคือ
///   EVDataCardUI.AttachBatteryToAnchor / AttachMotorToAnchor)
///
/// หมายเหตุสำคัญเรื่อง "Marker": ควรเป็นภาพที่มีรายละเอียด/คอนทราสต์สูง
/// ไม่สมมาตร (เช่น โลโก้ ภาพถ่าย ลวดลายเฉพาะ) ไม่ใช่ QR Code ทั่วไป เพราะ
/// QR Code มีลวดลายซ้ำๆ กัน (finder pattern 3 มุม) ทำให้ ARTrackedImageManager
/// จับจุดอ้างอิง (feature points) ได้ไม่ดี แนะนำพิมพ์ภาพขนาดจริงอย่างน้อย
/// ~10x10 ซม. ขึ้นไป ผิวด้าน (กันแสงสะท้อน) แล้วติดจุดเดียวนิ่งๆ บนตัวรถ
/// (เช่น บริเวณประตู/ฝากระโปรง) - ส่วนการตั้งค่า Reference Image Library
/// (เพิ่มรูป + ระบุความกว้างจริงหน่วยเมตร) ต้องทำใน Unity Inspector เอง
/// เพราะเป็นรูปเฉพาะของแต่ละคนและต้องวัดขนาดจริงที่พิมพ์ออกมา (ดูคอมเมนต์
/// ใน AREnvironmentSetup.cs ขั้นตอน Marker Tracking สำหรับวิธีตั้งค่า
/// ทีละคลิก)
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class VehicleMarkerTracker : MonoBehaviour
{
    [Header("ผูกอัตโนมัติจากเมนู EV AR > AR Setup > Marker Tracking > 2) (หรือลากเองก็ได้)")]
    public ARTrackedImageManager trackedImageManager;
    public EVDataCardUI dataCardUI;

    [Tooltip("ชื่อรูปใน Reference Image Library ที่ต้องการให้ตรงเป๊ะ - เว้นว่างไว้ = รับ marker แรกที่เจอ (พอสำหรับกรณีมี marker เดียวในไลบรารี)")]
    public string expectedImageName = "";

    [Header("จุด \"External Inspection Point\" บนผิวรถ (หน่วยเมตร จาก marker) - กำหนดไว้ล่วงหน้า (predefined) ไม่ใช่พิกัดจริงของชิ้นส่วนข้างในรถ")]
    [Tooltip("ระยะ+ทิศจาก marker ไปจุดที่ต้องการให้ชี้ว่า \"ดูแบตเตอรี่ตรงนี้\" (x=ขวา, y=บน, z=หน้า ตามทิศที่ marker หันหน้าไป) หน่วยเมตร - เป็นจุดสมมติบนผิวรถ ไม่ต้องวัดตำแหน่งแบตเตอรี่จริงข้างใน ปรับให้พอเหมาะกับจุดที่อยากให้ช่างมองก็พอ (ระยะห่างจาก motorOffset จะถูกขยายเพิ่มอัตโนมัติตอนแสดงผล ดู ClusterAnchor ใน EVDataCardUI.Builder.cs จึงไม่ต้องตั้งให้ห่างกันมากในนี้)")]
    public Vector3 batteryOffset = new Vector3(-0.15f, 0.05f, 0.1f);

    [Tooltip("ระยะ+ทิศจาก marker ไปจุดที่ต้องการให้ชี้ว่า \"ดูมอเตอร์ตรงนี้\" (x=ขวา, y=บน, z=หน้า ตามทิศที่ marker หันหน้าไป) หน่วยเมตร - เป็นจุดสมมติบนผิวรถ ไม่ต้องวัดตำแหน่งมอเตอร์จริงข้างใน")]
    public Vector3 motorOffset = new Vector3(0.15f, -0.05f, -0.1f);

    bool attached;

    // ⚠️ ใช้โดย CalibrationController.cs เท่านั้น (โหมด Calibration ในแอป) - เก็บ
    // reference ของ marker ล่าสุดที่ track เจอไว้ เพื่อให้ปรับ
    // batteryOffset/motorOffset สดๆ ระหว่างทดสอบ/ตั้งค่าได้ โดยไม่ต้องกลับไปแก้ใน
    // Unity Inspector + build ใหม่ทุกครั้ง - ไม่กระทบ logic tracking เดิมเลย (แค่
    // cache transform ตัวเดียวกับที่ AttachXToAnchor ใช้อยู่แล้ว)
    public Transform CurrentVehicleAnchor { get; private set; }

    void Reset()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        if (trackedImageManager == null) trackedImageManager = GetComponent<ARTrackedImageManager>();
        if (trackedImageManager != null) trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        if (dataCardUI != null && !attached) dataCardUI.SetTrackingMessage("กำลังหา Marker บนรถ...");
    }

    void OnDisable()
    {
        if (trackedImageManager != null) trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        if (attached) return; // ผูกครั้งเดียวพอ - หลังจากนี้ transform ของ marker จะลาก panel ไปเองทุกเฟรม ไม่ต้องเรียกซ้ำ

        foreach (var trackedImage in args.added)
        {
            TryAttach(trackedImage);
            if (attached) return;
        }
        foreach (var trackedImage in args.updated)
        {
            TryAttach(trackedImage);
            if (attached) return;
        }
    }

    void TryAttach(ARTrackedImage trackedImage)
    {
        if (dataCardUI == null) return;
        if (trackedImage.trackingState != TrackingState.Tracking) return;
        if (!string.IsNullOrEmpty(expectedImageName) && trackedImage.referenceImage.name != expectedImageName) return;

        CurrentVehicleAnchor = trackedImage.transform;
        dataCardUI.AttachBatteryToAnchor(trackedImage.transform, batteryOffset);
        dataCardUI.AttachMotorToAnchor(trackedImage.transform, motorOffset);
        attached = true;
        dataCardUI.SetTrackingMessage("AR HUD ACTIVE");
        Debug.Log("[VehicleMarkerTracker] ✅ เจอ Marker (\"" + trackedImage.referenceImage.name + "\") แล้ว - Panel ทั้ง Battery และ Motor ผูกกับตำแหน่งรถจริงแล้ว (คนละ offset) ลองเดินรอบรถ/ขยับมือถือดู ทั้งสอง Panel ควรตามรถไป (ไม่ใช่ตามกล้อง) ถ้าตำแหน่งยังไม่ตรงจุดจริง ให้กลับมาแก้ batteryOffset/motorOffset ใน Inspector");
    }

    // ==========================================
    // ใช้โดย CalibrationController.cs (Calibration Mode ในแอป)
    // เท่านั้น - ปรับ offset สดๆ ระหว่างทดสอบ + preview ตำแหน่งใหม่ทันที
    // ถ้า marker ถูก track อยู่แล้ว (ไม่ต้องรอ track ใหม่) ไม่ได้แตะ
    // logic การ track เดิมแม้แต่บรรทัดเดียว - เป็นแค่ setter ที่เรียก
    // AttachXToAnchor ซ้ำด้วยค่า offset ใหม่เท่านั้น
    // ==========================================
    public void SetBatteryOffset(Vector3 offset)
    {
        batteryOffset = offset;
        if (attached && CurrentVehicleAnchor != null && dataCardUI != null)
            dataCardUI.AttachBatteryToAnchor(CurrentVehicleAnchor, batteryOffset);
    }

    public void SetMotorOffset(Vector3 offset)
    {
        motorOffset = offset;
        if (attached && CurrentVehicleAnchor != null && dataCardUI != null)
            dataCardUI.AttachMotorToAnchor(CurrentVehicleAnchor, motorOffset);
    }
}
