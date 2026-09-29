using UnityEngine;

/// <summary>
/// สร้างโมเดล 3D แบบ procedural (primitive shape ล้วนๆ ผ่าน
/// GameObject.CreatePrimitive - ไม่ได้ import ไฟล์โมเดล .fbx/.obj จากภายนอก
/// เลย ตามที่ผู้ใช้เลือกไว้ชัดเจน) สำหรับ Battery/Motor แบบ engineering-style
/// กึ่งโปร่งใส (cutaway) - เป็นแค่ "ภาพประกอบ" ลอยอยู่ในกลุ่มเดียวกับการ์ด
/// ข้อมูล นอกตัวรถ (ไม่ใช่ตำแหน่งจริงของชิ้นส่วนข้างในรถ - ดู "External
/// Inspection Point" ใน EVDataCardUI.Builder.cs > BuildWorldSpaceUI/
/// ClusterAnchor) เป็น GameObject เสริมคู่กับการ์ด UI 2D เดิม ไม่ได้แทนที่
/// การ์ด UI 2D แต่อย่างใด
///
/// หน่วยตำแหน่ง/ขนาดทั้งหมดที่นี่คือ "canvas units" เดียวกับ UI ของการ์ด
/// (เพราะ parent เป็น BatteryAnchor/MotorAnchor ที่อยู่ใต้ World Space
/// Canvas) จะถูกย่อเป็นเมตรจริงอัตโนมัติจากสเกล 0.0015 ที่ตั้งใน
/// EVDataCardUI.AttachPanel() เหมือนกับการ์ด UI 2D ทุกประการ ไม่ต้องคำนวณ
/// สเกลแยกเอง
/// </summary>
public static class EVComponent3D
{
    public static GameObject BuildBattery(Transform parent, Vector3 localPosition, Color accent)
    {
        var root = new GameObject("Battery3D");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.identity;

        // เปลือกนอก (shell) - กล่องแบนกว้าง ทรงแพ็คแบตเตอรี่ EV ทั่วไป
        // กึ่งโปร่งใสเพื่อให้เห็น "เซลล์" ด้านในแบบ cutaway
        CreatePrimitiveChild(PrimitiveType.Cube, root.transform, "Shell",
            Vector3.zero, Quaternion.identity, new Vector3(220f, 90f, 140f),
            CreateComponentMaterial(accent, 0.35f, accent * 0.15f));

        // เซลล์ด้านใน (4 แท่งทรงกระบอกตั้ง) - สว่าง/ทึบแสงกว่า shell ชัดเจน
        // ให้ความรู้สึก cutaway มองทะลุเปลือกเข้าไปเห็นส่วนประกอบข้างในได้
        Color cellColor = Color.Lerp(accent, Color.white, 0.25f);
        var cellMat = CreateComponentMaterial(cellColor, 0.9f, cellColor * 0.35f);
        Vector3[] cellOffsets =
        {
            new Vector3(-55f, 0f, -35f),
            new Vector3(55f, 0f, -35f),
            new Vector3(-55f, 0f, 35f),
            new Vector3(55f, 0f, 35f),
        };
        foreach (var offset in cellOffsets)
        {
            CreatePrimitiveChild(PrimitiveType.Cylinder, root.transform, "Cell",
                offset, Quaternion.identity, new Vector3(28f, 55f, 28f), cellMat);
        }

        return root;
    }

    public static GameObject BuildMotor(Transform parent, Vector3 localPosition, Color accent)
    {
        var root = new GameObject("Motor3D");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        // ตั้งแกนมอเตอร์ให้หันหน้าตัดวงกลมเข้าหากล้อง (แกน Cylinder ปกติชี้
        // ตาม local Y - หมุน 90° รอบ X ให้ชี้ตาม local Z แทน)
        root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        CreatePrimitiveChild(PrimitiveType.Cylinder, root.transform, "Housing",
            Vector3.zero, Quaternion.identity, new Vector3(90f, 110f, 90f),
            CreateComponentMaterial(accent, 0.4f, accent * 0.15f));

        // โรเตอร์ด้านใน - ทึบแสงกว่า หมุนตลอดเวลา (MotorSpinAnimator) ให้
        // ความรู้สึก "กำลังทำงาน" แบบ ambient animation เท่านั้น (ไม่ได้ผูก
        // กับความเร็วรอบ motorRpm จริงแต่อย่างใด - แค่ตัวช่วยภาพ)
        var rotor = CreatePrimitiveChild(PrimitiveType.Cylinder, root.transform, "Rotor",
            Vector3.zero, Quaternion.identity, new Vector3(45f, 130f, 45f),
            CreateComponentMaterial(Color.Lerp(accent, Color.white, 0.3f), 0.95f, accent * 0.5f));
        rotor.AddComponent<MotorSpinAnimator>();

        // ครีบระบายความร้อนรอบเรือนนอก (8 แท่ง) - เดคอร์เสริมให้ดูเป็นมอเตอร์
        // จริงมากขึ้น
        var finMat = CreateComponentMaterial(accent * 0.7f, 0.55f, accent * 0.1f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 55f, 0f, Mathf.Sin(angle) * 55f);
            Quaternion rot = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            CreatePrimitiveChild(PrimitiveType.Cube, root.transform, "Fin",
                pos, rot, new Vector3(10f, 120f, 30f), finMat);
        }

        return root;
    }

    static GameObject CreatePrimitiveChild(PrimitiveType type, Transform parent, string name,
        Vector3 localPos, Quaternion localRot, Vector3 localScale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        var col = go.GetComponent<Collider>();
        // ใช้ DestroyImmediate (ไม่ใช่ Destroy) เพราะเมธอดนี้ถูกเรียกจาก
        // BuildUI() ซึ่งอาจรันใน Editor edit-mode ด้วย (เมนู EV AR > Build
        // EV Data Card UI) - Destroy() ปกติใช้ไม่ได้ใน edit-mode (throw
        // "Destroy may not be called from edit mode") ส่วนตอนรันจริงบนมือถือ
        // เมธอดนี้ก็ถูกเรียกแค่ตอน build/start ครั้งเดียวเท่านั้น ไม่ใช่ hot
        // path ที่เรียกซ้ำบ่อยๆ (ต่างจาก ClearGraphLine ใน
        // EVDataCardUI.Builder.cs ที่เรียกซ้ำตอน runtime จริง จึงยังใช้
        // Destroy() ตามเดิม)
        if (col != null) Object.DestroyImmediate(col); // ไม่ต้องมี physics collider - แค่ภาพเฉยๆ
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // สร้าง Material แบบโปร่งใสที่ทำงานได้ทั้งกับ URP (Universal Render
    // Pipeline/Lit - โปรเจกต์นี้ใช้ URP 17.3.0 ยืนยันแล้วจาก
    // ProjectSettings/GraphicsSettings.asset) และ fallback Standard/
    // Sprites-Default เผื่อ shader หลักหาไม่เจอตอน build จริง (กัน pink
    // material) - ตั้งค่า blend mode แบบ low-level ตรงๆ
    // (_SrcBlend/_DstBlend/_ZWrite + keyword) เพราะ property สะดวก _Surface
    // ถูก apply จาก custom Material Inspector GUI เท่านั้น ไม่ได้ผลตอนตั้งค่า
    // จากสคริปต์
    public static Material CreateComponentMaterial(Color baseColor, float alpha, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);

        Color c = baseColor; c.a = alpha;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", emission);
            mat.EnableKeyword("_EMISSION");
        }

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 1 = Transparent (URP convenience prop)
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);

        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }
}

/// <summary>
/// หมุนโรเตอร์มอเตอร์ตลอดเวลาเป็น ambient animation ให้ความรู้สึก "กำลัง
/// ทำงาน" - ไม่ผูกกับ motorRpm จริงใดๆ ทั้งสิ้น (แค่ตัวช่วยภาพ ไม่ใช่
/// data-binding - ไม่กระทบ data contract/FlutterBridge เลย)
/// </summary>
public class MotorSpinAnimator : MonoBehaviour
{
    public float degreesPerSecond = 90f;

    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}
