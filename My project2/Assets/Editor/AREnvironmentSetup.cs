using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEditor.XR.ARSubsystems;

/// <summary>
/// ⚠️ ไฟล์นี้ต้องอยู่ใต้โฟลเดอร์ Assets/Editor/ (Editor-only script)
///
/// เมนู Editor สำหรับต่อโครงสร้าง AR Foundation "จริง" เข้ากับ Scene ที่มีอยู่
/// แล้ว - ไม่แตะ FlutterBridge.cs หรือ data/API logic ใดๆ เลย ทำงานแบบ
/// idempotent (เช็คก่อนเพิ่มทุกครั้ง กดซ้ำกี่ครั้งก็ปลอดภัย ไม่สร้างซ้ำซ้อน)
///
/// มี 2 เส้นทางให้เลือก (ทำได้ทีละเส้นทาง หรือทำทั้งคู่ก็ได้ ไม่ชนกัน):
///
///   ► Marker Tracking (แนะนำ - ตรงตามที่ผู้ใช้เลือกล่าสุด)
///     ติด Marker/รูปเฉพาะไว้บนตัวรถจริง แล้วใช้ ARTrackedImageManager
///     ตาม Marker นั้น - Panel จะ "ติดกับตำแหน่งรถจริง" เสมอ ต่อให้รถถูก
///     เข็นย้ายที่ก็ยังตามรถไป (ดู VehicleMarkerTracker.cs)
///     เมนู: EV AR > AR Setup > Marker Tracking > ...
///
///   ► Alt: Plane Tap-To-Place (สำรอง/ง่ายกว่า แต่ไม่ track ตัวรถจริง)
///     แตะพื้นที่ตรวจเจอ 1 ครั้งเพื่อวาง Panel ลงในโลก - เหมาะไว้เดโมเร็วๆ
///     หรือกรณีไม่สะดวกพก marker (ดู ARPlacementController.cs)
///     เมนู: EV AR > AR Setup > Alt - Plane Tap-To-Place > ...
///
/// ทั้งสองทางใช้ hook ร่วมกันคือ EVDataCardUI.AttachToAnchor() - ใครวางสำเร็จ
/// ก่อนก็ชนะ (isAnchored = true แล้ว LateUpdate() จะหยุด billboard ตามกล้อง
/// ถาวร) ไม่ต้องกังวลว่าจะชนกันเอง แต่แนะนำให้เปิดใช้ "component" ของเส้นทาง
/// เดียวในซีนจริงตอน build ลงเครื่อง กัน UX สับสน (เช่น ปิด GameObject
/// ARPlacementController ถ้าจะใช้ Marker Tracking เป็นหลัก)
///
/// ขั้นตอนกล้อง (1 - Ensure Camera Feed) ใช้ร่วมกันทั้งสองเส้นทาง เพราะเป็น
/// พื้นฐานที่ต้องมีอยู่แล้วไม่ว่าจะเลือกวิธีไหน
/// </summary>
public static class AREnvironmentSetup
{
    const string OriginName = "XR Origin (AR Rig)";
    const string MarkerLibraryPath = "Assets/AR/VehicleMarkerLibrary.asset";

    // ==========================================
    // ใช้ร่วมกันทั้งสองเส้นทาง
    // ==========================================
    [MenuItem("EV AR/AR Setup/1) Ensure Camera Feed (AR Camera Manager + Background)")]
    public static void EnsureCameraFeed()
    {
        var origin = FindOrigin();
        if (origin == null) return;

        var cam = FindMainCamera(origin);
        if (cam == null)
        {
            Debug.LogError("[AR Setup] หา Main Camera ใต้ \"" + OriginName + "\" ไม่เจอ - เช็ค Hierarchy ด้วยตา (ควรอยู่ที่ Camera Offset/Main Camera)");
            return;
        }

        bool changed = false;
        if (cam.GetComponent<ARCameraManager>() == null)
        {
            Undo.AddComponent<ARCameraManager>(cam.gameObject);
            changed = true;
        }
        if (cam.GetComponent<ARCameraBackground>() == null)
        {
            Undo.AddComponent<ARCameraBackground>(cam.gameObject);
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(cam.gameObject);
            MarkSceneDirty();
            Debug.Log("[AR Setup] ✅ เพิ่ม AR Camera Manager / AR Camera Background ให้ " + cam.gameObject.name + " แล้ว - รันบนมือถือแล้วพื้นหลังควรเห็นภาพกล้องจริง (ไม่ใช่จอดำ)");
        }
        else
        {
            Debug.Log("[AR Setup] ✅ " + cam.gameObject.name + " มี AR Camera Manager + AR Camera Background อยู่แล้ว ไม่ต้องเพิ่ม");
        }
    }

    // ==========================================
    // เส้นทางแนะนำ: Marker / Image Tracking
    // ==========================================
    [MenuItem("EV AR/AR Setup/Marker Tracking/1) Ensure AR Tracked Image Manager")]
    public static void EnsureTrackedImageManager()
    {
        var origin = FindOrigin();
        if (origin == null) return;

        var mgr = origin.GetComponent<ARTrackedImageManager>();
        bool created = false;
        if (mgr == null)
        {
            mgr = Undo.AddComponent<ARTrackedImageManager>(origin);
            created = true;
        }

        // สร้าง Reference Image Library เปล่าไว้ให้ (ถ้ายังไม่มี) - ยังไม่มีรูป
        // marker อยู่ข้างใน ต้องเปิดไฟล์นี้ใน Inspector แล้วกด "+" ใส่รูป marker
        // ของจริง + ระบุความกว้างจริงที่พิมพ์ออกมา (หน่วยเมตร) เอง เพราะเป็น
        // ข้อมูลเฉพาะที่ script รู้แทนไม่ได้ (ดูคำแนะนำท้าย log)
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(MarkerLibraryPath);
        if (library == null)
        {
            var dir = Path.GetDirectoryName(MarkerLibraryPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            AssetDatabase.CreateAsset(library, MarkerLibraryPath);
            AssetDatabase.SaveAssets();
        }

        bool changed = created;
        if (mgr.referenceLibrary != library)
        {
            mgr.referenceLibrary = library;
            changed = true;
        }
        if (mgr.requestedMaxNumberOfMovingImages != 1)
        {
            mgr.requestedMaxNumberOfMovingImages = 1;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(mgr);
            MarkSceneDirty();
        }

        Debug.Log("[AR Setup] ✅ AR Tracked Image Manager พร้อมแล้ว บน " + origin.name + " (Library: " + MarkerLibraryPath + ")\n" +
            "⚠️ ยังต้องทำเองใน Unity (ทำครั้งเดียว):\n" +
            "  1. นำเข้ารูป marker ของจริง (โลโก้/ภาพถ่ายที่มีรายละเอียดชัด ไม่ใช่ QR ทั่วไป) เป็น Texture2D ใน Assets\n" +
            "     - เลือกรูปนั้น -> Inspector -> Texture Type = Default, เปิด Read/Write Enabled -> Apply\n" +
            "  2. เปิดไฟล์ " + MarkerLibraryPath + " ใน Inspector -> กด \"+\" -> ลากรูป marker ใส่\n" +
            "  3. ใส่ Physical Size (ความกว้างจริงที่จะพิมพ์ติดรถ หน่วยเมตร เช่น 0.15 = 15 ซม.) ให้ตรงของจริงเป๊ะๆ (มีผลต่อความแม่นยำ)\n" +
            "  4. Build ลงมือถือแล้วเทส");
    }

    [MenuItem("EV AR/AR Setup/Marker Tracking/2) Add Vehicle Marker Tracker To Scene")]
    public static void EnsureMarkerTracker()
    {
        var origin = FindOrigin();
        if (origin == null) return;

        var trackedImageManager = origin.GetComponent<ARTrackedImageManager>();
        if (trackedImageManager == null)
        {
            Debug.LogError("[AR Setup] ยังไม่มี AR Tracked Image Manager บน " + origin.name + " - รันขั้นตอน 1) ก่อน");
            return;
        }

        var dataCardUI = Object.FindFirstObjectByType<EVDataCardUI>();
        if (dataCardUI == null)
        {
            Debug.LogError("[AR Setup] หา EVDataCardUI ในซีนไม่เจอ - เปิดซีนที่มี EVDataCardUI ก่อน (หรือรันเมนู EV AR > Build EV Data Card UI ให้สร้างก่อน)");
            return;
        }

        var controllerGO = GameObject.Find("VehicleMarkerTracker");
        bool created = false;
        if (controllerGO == null)
        {
            controllerGO = new GameObject("VehicleMarkerTracker");
            Undo.RegisterCreatedObjectUndo(controllerGO, "Create VehicleMarkerTracker");
            created = true;
        }

        var controller = controllerGO.GetComponent<VehicleMarkerTracker>();
        if (controller == null) controller = Undo.AddComponent<VehicleMarkerTracker>(controllerGO);

        controller.trackedImageManager = trackedImageManager;
        controller.dataCardUI = dataCardUI;

        EditorUtility.SetDirty(controller);
        MarkSceneDirty();
        Debug.Log((created ? "[AR Setup] ✅ สร้าง " : "[AR Setup] ✅ อัปเดต ") + "VehicleMarkerTracker และผูก reference ให้อัตโนมัติแล้ว - พอ AR Tracked Image Manager เจอ marker บนรถ Panel จะผูกตำแหน่งกับรถทันที");
    }

    [MenuItem("EV AR/AR Setup/Marker Tracking/3) Assign Marker Image (VehicleMarker.png, 0.15m)")]
    public static void AssignMarkerImage()
    {
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(MarkerLibraryPath);
        if (library == null)
        {
            Debug.LogError("[AR Setup] ยังไม่มี " + MarkerLibraryPath + " - รันขั้นตอน 1) ก่อน");
            return;
        }

        const string texturePath = "Assets/AR/VehicleMarker.png";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogError("[AR Setup] หา " + texturePath + " ไม่เจอ - ตรวจสอบว่านำเข้ารูป marker เข้าโปรเจกต์ที่ path นี้แล้วหรือยัง");
            return;
        }

        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            Debug.Log("[AR Setup] ✅ เปิด Read/Write Enabled ให้ " + texturePath + " อัตโนมัติแล้ว");
        }

        int index = 0;
        if (library.count == 0)
        {
            library.Add();
            index = 0;
        }

        library.SetTexture(index, texture, true);
        library.SetName(index, "EV001");
        library.SetSpecifySize(index, true);
        library.SetSize(index, new Vector2(0.15f, 0.15f));

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("[AR Setup] ✅ ใส่รูป marker (VehicleMarker.png) ลง library แล้ว ชื่อ EV001 ขนาด 0.15 x 0.15 ม. (15ซม.) - ถ้าพิมพ์ขนาดอื่น แก้ Physical Size ทีหลังได้ใน Inspector ของ " + MarkerLibraryPath);
    }

    [MenuItem("EV AR/AR Setup/Marker Tracking/Run All (1 → 2, ไม่รวมขั้นตอนกล้อง)")]
    public static void RunAllMarkerTracking()
    {
        EnsureCameraFeed();
        EnsureTrackedImageManager();
        EnsureMarkerTracker();
        AssetDatabase.SaveAssets();
        Debug.Log("[AR Setup] ==== Marker Tracking: ต่อโครงสร้างเสร็จแล้ว - เหลือแค่ใส่รูป marker จริงในไลบรารี (ดู log ขั้นตอน 1 ด้านบน) แล้วเซฟซีน (Ctrl+S) ก่อน Build ====");
    }

    // ==========================================
    // เส้นทางสำรอง: Plane Tap-To-Place (เดิม)
    // ==========================================
    [MenuItem("EV AR/AR Setup/Alt - Plane Tap-To-Place/2) Ensure Plane Detection (AR Plane Manager)")]
    public static void EnsurePlaneDetection()
    {
        var origin = FindOrigin();
        if (origin == null) return;

        bool changed = false;
        if (origin.GetComponent<ARPlaneManager>() == null)
        {
            var pm = Undo.AddComponent<ARPlaneManager>(origin);
            pm.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(origin);
            MarkSceneDirty();
            Debug.Log("[AR Setup] ✅ เพิ่ม AR Plane Manager ให้ " + origin.name + " แล้ว (ตรวจจับพื้นแนวนอน) - ยังไม่ได้ตั้ง planePrefab จึงยังไม่เห็นตาราง grid พื้นบนจอ (ไม่กระทบการแตะวาง Panel)");
        }
        else
        {
            Debug.Log("[AR Setup] ✅ " + origin.name + " มี AR Plane Manager อยู่แล้ว ไม่ต้องเพิ่ม");
        }
    }

    [MenuItem("EV AR/AR Setup/Alt - Plane Tap-To-Place/3) Ensure Raycast + Anchor Manager")]
    public static void EnsureRaycastAndAnchor()
    {
        var origin = FindOrigin();
        if (origin == null) return;

        bool changed = false;
        if (origin.GetComponent<ARRaycastManager>() == null)
        {
            Undo.AddComponent<ARRaycastManager>(origin);
            changed = true;
        }
        if (origin.GetComponent<ARAnchorManager>() == null)
        {
            Undo.AddComponent<ARAnchorManager>(origin);
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(origin);
            MarkSceneDirty();
            Debug.Log("[AR Setup] ✅ เพิ่ม AR Raycast Manager / AR Anchor Manager ให้ " + origin.name + " แล้ว");
        }
        else
        {
            Debug.Log("[AR Setup] ✅ " + origin.name + " มี AR Raycast Manager + AR Anchor Manager อยู่แล้ว ไม่ต้องเพิ่ม");
        }
    }

    [MenuItem("EV AR/AR Setup/Alt - Plane Tap-To-Place/4) Add Tap-To-Place Controller To Scene")]
    public static void EnsurePlacementController()
    {
        var origin = FindOrigin();
        if (origin == null) return;

        var raycastManager = origin.GetComponent<ARRaycastManager>();
        var anchorManager = origin.GetComponent<ARAnchorManager>();
        var planeManager = origin.GetComponent<ARPlaneManager>();
        if (raycastManager == null || anchorManager == null)
        {
            Debug.LogError("[AR Setup] ยังไม่มี AR Raycast Manager / AR Anchor Manager บน " + origin.name + " - รันขั้นตอน 3) ก่อน");
            return;
        }

        var dataCardUI = Object.FindFirstObjectByType<EVDataCardUI>();
        if (dataCardUI == null)
        {
            Debug.LogError("[AR Setup] หา EVDataCardUI ในซีนไม่เจอ - เปิดซีนที่มี EVDataCardUI ก่อน (หรือรันเมนู EV AR > Build EV Data Card UI ให้สร้างก่อน)");
            return;
        }

        var controllerGO = GameObject.Find("ARPlacementController");
        bool created = false;
        if (controllerGO == null)
        {
            controllerGO = new GameObject("ARPlacementController");
            Undo.RegisterCreatedObjectUndo(controllerGO, "Create ARPlacementController");
            created = true;
        }

        var controller = controllerGO.GetComponent<ARPlacementController>();
        if (controller == null) controller = Undo.AddComponent<ARPlacementController>(controllerGO);

        controller.raycastManager = raycastManager;
        controller.anchorManager = anchorManager;
        controller.planeManager = planeManager;
        controller.dataCardUI = dataCardUI;

        EditorUtility.SetDirty(controller);
        MarkSceneDirty();
        Debug.Log((created ? "[AR Setup] ✅ สร้าง " : "[AR Setup] ✅ อัปเดต ") + "ARPlacementController และผูก reference ให้อัตโนมัติแล้ว - แตะพื้น (บนมือถือจริง หรือคลิกซ้ายใน Play Mode ถ้าเปิด XR Simulation) เพื่อวาง AR HUD");
    }

    [MenuItem("EV AR/AR Setup/Alt - Plane Tap-To-Place/Run All (1 → 4)")]
    public static void RunAllPlaneTapToPlace()
    {
        EnsureCameraFeed();
        EnsurePlaneDetection();
        EnsureRaycastAndAnchor();
        EnsurePlacementController();
        AssetDatabase.SaveAssets();
        Debug.Log("[AR Setup] ==== Plane Tap-To-Place: เสร็จครบ - เซฟซีนด้วย Ctrl+S แล้ว Build ลงมือถือเพื่อทดสอบ: กล้องเห็นภาพจริง → แตะพื้น → Panel ลอย → ขยับมือถือ → Panel อยู่ตำแหน่งเดิม ====");
    }

    // ==========================================
    // helpers
    // ==========================================
    static GameObject FindOrigin()
    {
        var origin = GameObject.Find(OriginName);
        if (origin == null)
        {
            Debug.LogError($"[AR Setup] หา GameObject ชื่อ \"{OriginName}\" ในซีนที่เปิดอยู่ไม่เจอ - เปิดซีนหลัก (SampleScene) ก่อนรันเมนูนี้");
        }
        return origin;
    }

    static Transform FindMainCamera(GameObject origin)
    {
        var t = origin.transform.Find("Camera Offset/Main Camera");
        if (t != null) return t;
        var cam = origin.GetComponentInChildren<Camera>(true);
        return cam != null ? cam.transform : null;
    }

    static void MarkSceneDirty()
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
