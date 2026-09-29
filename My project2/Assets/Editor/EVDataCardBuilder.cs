using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// เครื่องมือ Editor สำหรับ "ออกแบบ" การ์ด AR (EVDataCardUI) ให้เห็นเป็น
/// GameObject จริงใน Hierarchy/Scene view ของ Unity แทนที่จะถูกสร้างซ่อน
/// อยู่ตอนรันไทม์อย่างเดียว
///
/// วิธีใช้: เมนูด้านบน > EV AR > Build EV Data Card UI
/// จะได้ GameObject ชื่อ "EVDataCardUI" พร้อมลูกๆ (Canvas/Card/ข้อความ/
/// แถบ SOC/ตารางสถิติ) ครบตามดีไซน์ - แก้สี ขนาด ตำแหน่ง ฟอนต์ได้ใน
/// Inspector ตามปกติเลย
///
/// อย่าลืมกด Ctrl+S เซฟ Scene หลัง Build (หรือหลังแก้ไขอะไรเพิ่ม) เพื่อให้
/// ติดไปกับไฟล์ .unity ไม่หายตอนปิด Editor
///
/// ตอนรันแอปจริง (กด Play หรือ build จริงบนมือถือ) สคริปต์ EVDataCardUI
/// จะ bind เข้ากับ GameObject ชุดนี้อัตโนมัติ (ไม่สร้างซ้ำ) แล้วซ่อนการ์ด
/// ไว้ก่อนจนกว่าจะได้รับข้อมูลจริงจาก Flutter
/// </summary>
public static class EVDataCardBuilder
{
    [MenuItem("EV AR/Build EV Data Card UI (ออกแบบใน Editor)")]
    public static void Build()
    {
        // ถ้ามีของเก่าอยู่แล้ว ลบทิ้งแล้วสร้างใหม่ทั้งหมด (rebuild ซ้ำได้เรื่อยๆ)
        var existing = Object.FindFirstObjectByType<EVDataCardUI>();
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var go = new GameObject("EVDataCardUI");
        Undo.RegisterCreatedObjectUndo(go, "Build EV Data Card UI");

        var cardUI = go.AddComponent<EVDataCardUI>();
        cardUI.BuildUI();

        Selection.activeGameObject = go;

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("[EVDataCardBuilder] สร้าง EV Data Card UI เสร็จแล้ว ดูใน Hierarchy ได้เลย " +
                   "(EVDataCardUI > EVDataCanvas > Card > ...) แก้สี/ขนาด/ตำแหน่งใน Inspector ได้ตามใจ " +
                   "แล้วกด Ctrl+S เพื่อเซฟ Scene");
    }
}
