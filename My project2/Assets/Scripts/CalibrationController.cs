using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Calibration Mode: ให้ผู้ใช้ปรับตำแหน่ง (offset) ของ Battery/Motor สดๆ
/// ในแอประหว่างเห็น AR จริง แล้ว Save ค่าไว้ (PlayerPrefs) โดยไม่ต้องกลับไป
/// แก้ใน Unity Inspector + build ใหม่ทุกครั้ง - ปุ่มเฟือง (มุมขวาบน) ซ่อนโหมด
/// นี้ไว้เป็นค่าเริ่มต้นเสมอ ผู้ใช้ทั่วไปไม่ต้องยุ่งเลยถ้าไม่กดเอง (ตามสเปก
/// "remain optional for normal end users")
///
/// ขั้นตอนใช้งาน: สแกน Marker บนรถ (VehicleMarkerTracker เจอแล้ว/ผูกแล้ว) ->
/// กดเฟือง -> เลือกแท็บ BATTERY หรือ MOTOR -> กด +/- ที่ X/Y/Z จนตำแหน่ง
/// การ์ด/โมเดล 3D ตรงจุดจริงบนรถ (เห็นผลทันทีเพราะเรียก
/// VehicleMarkerTracker.SetBatteryOffset/SetMotorOffset ซึ่ง preview สดผ่าน
/// EVDataCardUI.AttachBatteryToAnchor/AttachMotorToAnchor ที่มีอยู่เดิม) ->
/// กด SAVE (เขียนลง PlayerPrefs) -> CLOSE
///
/// ⚠️ ไม่แตะ/ไม่สร้าง AR Session, XR Origin, ARTrackedImageManager,
/// VehicleMarkerTracker (แก้แค่เพิ่ม SetBatteryOffset/SetMotorOffset ซึ่งเป็น
/// setter เสริม ไม่กระทบ logic tracking เดิม) หรือ FlutterBridge/data
/// contract ใดๆ ทั้งสิ้น - เป็น UI ชั้นบนสุด (sortingOrder 200) ที่แค่เรียก
/// method ที่มีอยู่แล้วซ้ำด้วยค่า offset ใหม่เท่านั้น
///
/// ⚠️ ไม่ได้ใช้ระบบ TryBindExisting/RewireXxx ของ EVDataCardUI เลย - สร้าง
/// UI ของตัวเองสดๆ ทุกครั้งตอนแอปเริ่มทำงาน (ไม่ได้ save ไว้ใน Scene) จึงไม่
/// ติดปัญหา "Button.onClick listener ไม่ persist ข้าม Editor-save/reload"
/// ที่ EVDataCardUI ต้องมี RewireXxx เพื่อแก้ (เพราะไฟล์นั้น build ผ่านเมนู
/// Editor แล้ว save Scene ไว้ - อันนี้ไม่ได้ save เลย)
/// </summary>
[RequireComponent(typeof(VehicleMarkerTracker))]
public class CalibrationController : MonoBehaviour
{
    VehicleMarkerTracker tracker;

    const float Step = 0.02f;
    const string KeyPrefix = "EVAR_";

    enum Target { Battery, Motor }
    Target activeTarget = Target.Battery;

    GameObject panelRoot;
    TextMeshProUGUI readoutText;
    Image batteryTabBg, motorTabBg;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var existingTracker = FindFirstObjectByType<VehicleMarkerTracker>();
        if (existingTracker == null) return; // ไม่มีระบบ marker tracking ก็ไม่ต้องมี Calibration Mode
        if (existingTracker.GetComponent<CalibrationController>() != null) return;
        existingTracker.gameObject.AddComponent<CalibrationController>();
    }

    void Awake()
    {
        tracker = GetComponent<VehicleMarkerTracker>();
        LoadSavedOffsets();
        BuildUI();
    }

    // โหลดค่าที่เคย Save ไว้จาก Calibration Mode ครั้งก่อน (PlayerPrefs) มา
    // เขียนทับค่า default ที่ตั้งไว้ใน Inspector ของ VehicleMarkerTracker -
    // ทำใน Awake() ซึ่งมาก่อน VehicleMarkerTracker.OnEnable() เสมอ (ตาม
    // ลำดับ Unity execution order ปกติ) เพื่อให้ตำแหน่งที่ Calibrate ไว้แล้ว
    // มีผลตั้งแต่ครั้งแรกที่เจอ marker
    void LoadSavedOffsets()
    {
        if (PlayerPrefs.HasKey(KeyPrefix + "BatteryOffset_X"))
        {
            tracker.batteryOffset = new Vector3(
                PlayerPrefs.GetFloat(KeyPrefix + "BatteryOffset_X"),
                PlayerPrefs.GetFloat(KeyPrefix + "BatteryOffset_Y"),
                PlayerPrefs.GetFloat(KeyPrefix + "BatteryOffset_Z"));
        }
        if (PlayerPrefs.HasKey(KeyPrefix + "MotorOffset_X"))
        {
            tracker.motorOffset = new Vector3(
                PlayerPrefs.GetFloat(KeyPrefix + "MotorOffset_X"),
                PlayerPrefs.GetFloat(KeyPrefix + "MotorOffset_Y"),
                PlayerPrefs.GetFloat(KeyPrefix + "MotorOffset_Z"));
        }
    }

    void SaveOffsets()
    {
        var b = tracker.batteryOffset;
        PlayerPrefs.SetFloat(KeyPrefix + "BatteryOffset_X", b.x);
        PlayerPrefs.SetFloat(KeyPrefix + "BatteryOffset_Y", b.y);
        PlayerPrefs.SetFloat(KeyPrefix + "BatteryOffset_Z", b.z);

        var m = tracker.motorOffset;
        PlayerPrefs.SetFloat(KeyPrefix + "MotorOffset_X", m.x);
        PlayerPrefs.SetFloat(KeyPrefix + "MotorOffset_Y", m.y);
        PlayerPrefs.SetFloat(KeyPrefix + "MotorOffset_Z", m.z);
        PlayerPrefs.Save();
    }

    // ==========================================
    // UI: ปุ่มเฟือง (มุมขวาบน) เปิด/ปิดแผง Calibration - ปิดอยู่เสมอเป็นค่า
    // เริ่มต้น
    // ==========================================
    void BuildUI()
    {
        var canvasGO = new GameObject("CalibrationCanvas");
        canvasGO.transform.SetParent(transform, false); // ลูกของ VehicleMarkerTracker GameObject เฉยๆ ไม่ใช่ AR object ใดๆ
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // ทับ ScreenSpace_UI (sortingOrder 100) ของ EVDataCardUI ได้เสมอ
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var toggleBtn = CreateButton(canvasGO.transform, "ToggleBtn", "⚙", 24);
        var toggleRect = toggleBtn.GetComponent<RectTransform>();
        toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1f, 1f);
        toggleRect.pivot = new Vector2(1f, 1f);
        toggleRect.sizeDelta = new Vector2(56, 56);
        toggleRect.anchoredPosition = new Vector2(-16, -50);
        toggleBtn.GetComponent<Image>().color = new Color(0.02f, 0.035f, 0.06f, 0.7f);
        toggleBtn.GetComponent<Button>().onClick.AddListener(TogglePanel);

        BuildPanel(canvasGO.transform);
        panelRoot.SetActive(false);
    }

    void BuildPanel(Transform parent)
    {
        panelRoot = new GameObject("CalibrationPanel", typeof(RectTransform));
        panelRoot.transform.SetParent(parent, false);
        var rect = panelRoot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(320, 540);
        rect.anchoredPosition = new Vector2(-16, -114);

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.035f, 0.06f, 0.94f);

        var layout = panelRoot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 10;
        layout.childControlWidth = true; layout.childForceExpandWidth = true;
        layout.childControlHeight = true; layout.childForceExpandHeight = false;

        var titleTxt = CreateLabel(panelRoot.transform, "CALIBRATION MODE", 15, Color.white);
        AddPreferredHeight(titleTxt.gameObject, 22);

        var tabRow = new GameObject("TabRow", typeof(RectTransform));
        tabRow.transform.SetParent(panelRoot.transform, false);
        var tabH = tabRow.AddComponent<HorizontalLayoutGroup>();
        tabH.spacing = 8;
        tabH.childControlWidth = true; tabH.childForceExpandWidth = true;
        tabH.childControlHeight = true;
        AddPreferredHeight(tabRow, 44);

        var batteryTabGO = CreateButton(tabRow.transform, "BatteryTab", "BATTERY", 13);
        batteryTabBg = batteryTabGO.GetComponent<Image>();
        batteryTabGO.GetComponent<Button>().onClick.AddListener(() => SetTarget(Target.Battery));

        var motorTabGO = CreateButton(tabRow.transform, "MotorTab", "MOTOR", 13);
        motorTabBg = motorTabGO.GetComponent<Image>();
        motorTabGO.GetComponent<Button>().onClick.AddListener(() => SetTarget(Target.Motor));

        readoutText = CreateLabel(panelRoot.transform, "-", 14, new Color(0.6f, 0.75f, 0.9f));
        readoutText.alignment = TextAlignmentOptions.Left;
        AddPreferredHeight(readoutText.gameObject, 60);

        AddAxisRow(panelRoot.transform, "X", 0);
        AddAxisRow(panelRoot.transform, "Y", 1);
        AddAxisRow(panelRoot.transform, "Z", 2);

        var actionRow = new GameObject("ActionRow", typeof(RectTransform));
        actionRow.transform.SetParent(panelRoot.transform, false);
        var actH = actionRow.AddComponent<HorizontalLayoutGroup>();
        actH.spacing = 8;
        actH.childControlWidth = true; actH.childForceExpandWidth = true;
        actH.childControlHeight = true;
        AddPreferredHeight(actionRow, 48);

        var saveBtn = CreateButton(actionRow.transform, "SaveBtn", "SAVE", 14);
        saveBtn.GetComponent<Image>().color = new Color(0.13f, 0.83f, 0.62f, 0.85f);
        saveBtn.GetComponent<Button>().onClick.AddListener(() => { SaveOffsets(); RefreshReadout(); });

        var closeBtn = CreateButton(actionRow.transform, "CloseBtn", "CLOSE", 14);
        closeBtn.GetComponent<Button>().onClick.AddListener(TogglePanel);

        SetTarget(Target.Battery);
    }

    void AddAxisRow(Transform parent, string axisLabel, int axisIndex)
    {
        var row = new GameObject("Axis" + axisLabel, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 6;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childForceExpandWidth = false;
        h.childControlHeight = true;
        AddPreferredHeight(row, 44);

        var label = CreateLabel(row.transform, axisLabel, 15, Color.white);
        label.alignment = TextAlignmentOptions.Center;
        var labelLE = label.gameObject.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 26;

        var minusBtn = CreateButton(row.transform, "Minus", "-", 20);
        minusBtn.GetComponent<LayoutElement>().preferredWidth = 64;
        minusBtn.GetComponent<Button>().onClick.AddListener(() => Nudge(axisIndex, -Step));

        var plusBtn = CreateButton(row.transform, "Plus", "+", 20);
        plusBtn.GetComponent<LayoutElement>().preferredWidth = 64;
        plusBtn.GetComponent<Button>().onClick.AddListener(() => Nudge(axisIndex, Step));
    }

    void SetTarget(Target target)
    {
        activeTarget = target;
        batteryTabBg.color = target == Target.Battery ? new Color(0.2f, 0.96f, 0.62f, 0.4f) : new Color(1f, 1f, 1f, 0.06f);
        motorTabBg.color = target == Target.Motor ? new Color(0.13f, 0.83f, 0.93f, 0.4f) : new Color(1f, 1f, 1f, 0.06f);
        RefreshReadout();
    }

    void Nudge(int axisIndex, float delta)
    {
        if (activeTarget == Target.Battery)
        {
            var v = tracker.batteryOffset;
            v[axisIndex] += delta;
            tracker.SetBatteryOffset(v);
        }
        else
        {
            var v = tracker.motorOffset;
            v[axisIndex] += delta;
            tracker.SetMotorOffset(v);
        }
        RefreshReadout();
    }

    void RefreshReadout()
    {
        if (readoutText == null) return;
        Vector3 v = activeTarget == Target.Battery ? tracker.batteryOffset : tracker.motorOffset;
        string name = activeTarget == Target.Battery ? "BATTERY" : "MOTOR";
        readoutText.text = name + " offset (m)\nX " + v.x.ToString("0.00") + "  Y " + v.y.ToString("0.00") + "  Z " + v.z.ToString("0.00");
    }

    void TogglePanel()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
        if (panelRoot.activeSelf) RefreshReadout();
    }

    // ==========================================
    // primitive helpers (ชุดเล็กๆ แยกอิสระจาก EVDataCardUI.Builder.cs โดย
    // ตั้งใจ - Calibration UI เป็นระบบแยกอิสระ ไม่ผูกกับ partial class นั้น)
    // ==========================================
    GameObject CreateButton(Transform parent, string name, string label, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.08f);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        go.AddComponent<LayoutElement>();

        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRect = txtGO.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
        var text = txtGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;

        return go;
    }

    TextMeshProUGUI CreateLabel(Transform parent, string label, float fontSize, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.Left;
        txt.fontStyle = FontStyles.Bold;
        return txt;
    }

    void AddPreferredHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
    }
}
