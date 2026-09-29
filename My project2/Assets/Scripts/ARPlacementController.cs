using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ⚠️ ไฟล์นี้ต้องอยู่ใต้โฟลเดอร์ Assets/Scripts/ (runtime script ธรรมดา)
///
/// ระบบ "แตะพื้นเพื่อวาง AR HUD" แบบจริง (ไม่ใช่ mockup) - ตรงกับลำดับที่
/// ผู้ใช้ขอ:
///   ① กล้องเห็นภาพจริง          -> AREnvironmentSetup ขั้นตอน 1 (คนละไฟล์)
///   ② ตรวจจับพื้น               -> AR Plane Manager (ขั้นตอน 2)
///   ③ แตะพื้น -> Panel ลอยในโลก -> สคริปต์นี้ (ARRaycastManager + AttachAnchor)
///   ④ ขยับมือถือ -> Panel อยู่ที่เดิม ⭐
///      -> เกิดขึ้นเองโดยอัตโนมัติ เพราะหลังวางแล้ว Panel (WorldSpace_UI)
///         จะกลายเป็นลูกของ ARAnchor จริง ตัว AR Foundation จะอัปเดตตำแหน่ง/
///         การหมุนของ Anchor ให้เองทุกเฟรมตามผลการ track ของอุปกรณ์ ไม่ต้อง
///         เขียนโค้ดคำนวณตำแหน่งเองอีกต่อไป (ดู EVDataCardUI.AttachToAnchor
///         และ EVDataCardUI.LateUpdate ที่จะหยุด billboard-ตามกล้องทันทีที่
///         isAnchored = true)
///
/// ⚠️ หมายเหตุ: เส้นทางนี้เป็น "ทางเลือกสำรอง" - ไม่ track ตัวรถจริง (ถ้ารถ
/// ถูกเข็นย้ายที่ panel จะไม่ตามรถ) เส้นทางที่ผู้ใช้เลือกใช้เป็นหลักคือ
/// Marker/Image Tracking (ดู VehicleMarkerTracker.cs) ซึ่ง track ตำแหน่งรถ
/// จริงผ่าน marker ที่ติดบนตัวรถ - ไฟล์นี้เก็บไว้เผื่อกรณีไม่สะดวกพก marker
///
/// MVP ตอนนี้ตั้งใจให้ "วางครั้งเดียว" - ยังไม่แยก Anchor คนละอันสำหรับ
/// Battery/Motor
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class ARPlacementController : MonoBehaviour
{
    [Header("ผูกอัตโนมัติจากเมนู EV AR > AR Setup > Alt - Plane Tap-To-Place > 4) (หรือลากเองก็ได้)")]
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;
    public ARPlaneManager planeManager; // ใช้หา ARPlane จาก trackableId เพื่อ AttachAnchor (null ก็ยังทำงานได้ผ่าน fallback)
    public EVDataCardUI dataCardUI;

    [Tooltip("ระยะสูงจากจุดที่แตะ (เมตร) ที่จะให้ Panel ลอยอยู่ - กันไม่ให้ Panel จมพื้น")]
    public float placeHeightOffset = 0.15f;

    [Tooltip("ระยะห่างซ้าย/ขวาจากจุดแตะ (เมตร) ที่จะแยก Battery/Motor panel ออกจากกัน - เส้นทางนี้ไม่รู้ตำแหน่งจริงบนรถ (แค่จุดแตะเดียว) เลยแยกคร่าวๆ ให้ไม่ซ้อนทับกันเฉยๆ")]
    public float sideBySideSpread = 0.2f;

    static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
    bool placed;

    void Reset()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Start()
    {
        if (dataCardUI != null) dataCardUI.SetTrackingMessage("แตะพื้นเพื่อวาง AR HUD");
    }

    void Update()
    {
        if (placed) return;
        if (raycastManager == null) return;

        var tapPos = TryGetTapPosition();
        if (tapPos == null) return;
        if (IsPointerOverUI()) return; // อย่าให้แตะปุ่ม UI (TopBar/BottomControls) โดน raycast พื้นด้วย

        if (raycastManager.Raycast(tapPos.Value, Hits, TrackableType.PlaneWithinPolygon))
        {
            PlaceAt(Hits[0].pose, Hits[0].trackableId);
        }
    }

    Vector2? TryGetTapPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return Mouse.current.position.ReadValue();
        return null;
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return Input.GetTouch(0).position;
        if (Input.GetMouseButtonDown(0))
            return Input.mousePosition;
        return null;
#endif
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void PlaceAt(Pose pose, TrackableId planeId)
    {
        ARAnchor anchor = null;

        if (anchorManager != null && planeManager != null)
        {
            var plane = planeManager.GetPlane(planeId);
            if (plane != null) anchor = anchorManager.AttachAnchor(plane, pose);
        }

        if (anchor == null)
        {
            // fallback: ไม่มี plane/anchor manager ผูกได้ - สร้าง ARAnchor เปล่า
            // ที่ตำแหน่งที่แตะแทน (ยังเป็น ARAnchor จริงที่ AR Foundation จะ
            // พยายาม track ให้ ถ้า provider ของอุปกรณ์รองรับ)
            var go = new GameObject("PlacementAnchor");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            anchor = go.AddComponent<ARAnchor>();
        }

        if (anchor == null)
        {
            Debug.LogError("[ARPlacementController] สร้าง Anchor ไม่สำเร็จ - ลองแตะใหม่อีกครั้ง");
            return;
        }

        placed = true;

        if (dataCardUI != null)
        {
            var up = Vector3.up * placeHeightOffset;
            dataCardUI.AttachBatteryToAnchor(anchor.transform, up + Vector3.left * sideBySideSpread);
            dataCardUI.AttachMotorToAnchor(anchor.transform, up + Vector3.right * sideBySideSpread);
            dataCardUI.SetTrackingMessage("AR HUD ACTIVE");
        }

        Debug.Log("[ARPlacementController] ✅ วาง AR HUD ที่ตำแหน่งจริงแล้ว (Anchor: " + anchor.trackableId + ") - ลองขยับมือถือดู Panel ควรอยู่ตำแหน่งเดิม (ข้อ ④ ⭐)");
    }
}
