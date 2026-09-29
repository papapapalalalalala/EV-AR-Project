using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI AR แบบ "dark glassmorphism" สำหรับจอมือถือแนวตั้ง (9:16) - รื้อ
/// hierarchy ใหม่ทั้งหมดตามที่ผู้ใช้ระบุ (ยังไม่แตะ FlutterBridge.cs /
/// การรับส่งข้อมูลใดๆ - แก้เฉพาะ UI/Canvas/สไตล์):
///
///   ARScene (root ของสคริปต์นี้)
///   ├── AR Camera             (ของเดิม ไม่ได้สร้าง/แก้ตรงนี้ - อ่านผ่าน Camera.main)
///   ├── WorldSpace_UI_Battery (Canvas โหมด World Space แยกอิสระ - ลอยหน้ากล้องจนกว่าจะผูกกับ marker)
///   │    └── BatteryAnchor → BatteryARPanel   (การ์ดกระจกโปร่งแสงสีเขียว)
///   ├── WorldSpace_UI_Motor   (Canvas โหมด World Space แยกอิสระ - ผูกคนละ offset จาก Battery ได้)
///   │    └── MotorAnchor   → MotorARPanel     (การ์ดกระจกโปร่งแสงสีฟ้า)
///   └── ScreenSpace_UI        (Canvas โหมด Screen Space Overlay เดิม)
///
///   ⚠️ Battery/Motor แยก Canvas กันคนละอันตั้งแต่เวอร์ชันนี้ เพื่อให้ผูกกับ
///   ตำแหน่งจริงบนตัวรถได้ "คนละจุด" จาก marker ใบเดียวกัน (ดู
///   VehicleMarkerTracker.batteryOffset / motorOffset) แทนที่จะลอยติดกัน
///   เป็นคู่ตายตัวแบบเดิม
///        ├── TopBar           (ปุ่มย้อนกลับ, "AR MONITOR", EV001, badge, LIVE)
///        ├── TrackingMessage  (ป้ายเล็กๆ บอกสถานะ HUD)
///        ├── BottomControls   (แถบปุ่มลอย 3 ปุ่ม: AR / DATA / GRAPH - Battery/Motor เปิดจากการแตะการ์ดลอย/จุด Connector แทน)
///        └── DetailOverlay    (พาเนลรายละเอียดเต็มจอ เปิดตอนแตะการ์ด)
///
/// ⚠️ เรื่อง "World Space Canvas / world-anchored UI ไม่ใช่ Screen Space
/// Overlay": โปรเจกต์นี้ยังไม่มีระบบ AR tracking จริง (ไม่มี
/// ARTrackedImageManager/ARPlaneManager/ARRaycastManager เลยสักตัว) ดังนั้น
/// "ปักหมุดกับตัวรถจริง" 100% ยังทำไม่ได้ สิ่งที่ implement จริงคือ
/// WorldSpace_UI เป็น Canvas World Space จริง (ไม่ใช่ Overlay) ที่ทุกเฟรมจะ
/// คำนวณตำแหน่ง/ขนาดให้ "ลอยตรึงหน้ากล้อง" ที่ระยะคงที่ (ดู UpdateWorldHud
/// ใน EVDataCardUI.cs) ได้ฟีล AR HUD ลอยอยู่ในอวกาศ 3 มิติจริง (เป็น
/// geometry จริง ไม่ใช่ quad ติดจอแบบ Overlay) แต่ยังไม่ใช่การ "ล็อกกับ
/// พิกัดจริงบนตัวรถ" ถ้าต้องการแบบนั้นต้องสร้างระบบ AR anchor แยกต่างหาก
///
/// โครงสร้างการสร้าง UI จริงๆ (CreateUIObject/BuildUI/TryBindExisting และ
/// helper ต่างๆ) อยู่ใน EVDataCardUI.Builder.cs (partial class เดียวกัน) -
/// แยกไฟล์เพื่อให้แต่ละไฟล์ไม่ยาวเกินไป
///
/// "ออกแบบได้ใน Editor": เมนู EV AR > Build EV Data Card UI (ดู
/// EVDataCardBuilder.cs) เรียก BuildUI() ตรงๆ ให้เห็นเป็น GameObject จริงใน
/// Hierarchy/Scene ทันที ตอนรันแอปจริง Start() จะลองหา GameObject ที่ถูก
/// สร้าง/เซฟไว้แล้วก่อน (TryBindExisting) ถ้าไม่เจอค่อย fallback มา BuildUI()
/// เอง
/// </summary>
public partial class EVDataCardUI : MonoBehaviour
{
    // ---------- Palette: dark glassmorphism (near-black navy + neon) ----------
    static readonly Color GlassBg = new Color(0.035f, 0.055f, 0.09f, 0.62f);      // การ์ดกระจกโปร่งแสง
    static readonly Color GlassBgSolid = new Color(0.02f, 0.03f, 0.05f, 0.88f);   // ฉากหลัง overlay เต็มจอ
    static readonly Color TopBarBg = new Color(0.02f, 0.035f, 0.06f, 0.55f);

    static readonly Color CyanPrimary = new Color32(0x22, 0xD3, 0xEE, 0xFF);      // #22D3EE - โทนมอเตอร์ / primary
    static readonly Color GreenPrimary = new Color32(0x34, 0xF5, 0x9E, 0xFF);     // #34F59E - โทนแบตเตอรี่ / secondary
    static readonly Color Amber = new Color32(0xFB, 0xBF, 0x24, 0xFF);            // warning (SOC ต่ำ)
    static readonly Color Danger = new Color32(0xF8, 0x71, 0x71, 0xFF);           // abnormal

    static readonly Color TextPrimary = new Color32(0xF3, 0xF8, 0xFF, 0xFF);
    static readonly Color TextSecondary = new Color32(0x92, 0xA6, 0xC4, 0xFF);
    static readonly Color TextMuted = new Color32(0x5C, 0x6B, 0x84, 0xFF);

    const float MotorRpmMax = 8000f;
    const float WorldHudDistance = 1.0f; // ระยะที่ HUD ลอยหน้ากล้อง (เมตร)
    const float CanvasUnitsHigh = 1920f;
    const float CanvasUnitsWide = 1080f;

    enum DetailMode { None, Battery, Motor, Data, Graph }
    DetailMode currentDetail = DetailMode.None;

    // ---- roots (Battery/Motor แยก Canvas อิสระกัน - ผูกกับ marker คนละ
    // offset ได้ ตามที่วัดจริงจากตัวรถ) ----
    Transform worldSpaceBattery;
    Transform worldSpaceMotor;
    Transform screenSpaceUI;

    // ---- ผูกกับ ARAnchor จริงแล้วหรือยัง แยกกันต่อ panel (ตั้งโดย
    // AttachBatteryToAnchor / AttachMotorToAnchor) - true แล้ว LateUpdate()
    // จะหยุด billboard panel นั้นตามกล้อง เพราะ transform จะถูกอัปเดตให้เอง
    // โดย AR Foundation ผ่านการเป็นลูกของ ARAnchor/marker แทน ----
    bool batteryAnchored = false;
    bool motorAnchored = false;

    // ---- ข้อความคำแนะนำใน TrackingMessage ("แตะพื้นเพื่อวาง" / "AR HUD ACTIVE") ----
    TextMeshProUGUI trackingMessageText;

    // ---- top bar ----
    TextMeshProUGUI vehicleIdText;
    Image statusBadgeBg;
    TextMeshProUGUI statusBadgeText;
    Image liveDot;
    Coroutine liveRoutine;

    // ---- world battery panel ----
    Image batteryRingFill;
    TextMeshProUGUI batteryPercentText;
    TextMeshProUGUI batteryVoltageText;
    TextMeshProUGUI batteryTempText;
    Coroutine batteryPulse;

    // ---- world motor panel ----
    Image motorRingFill;
    TextMeshProUGUI motorRpmText;
    TextMeshProUGUI motorPowerText;
    Coroutine motorPulse;

    // ---- detail overlay (generic, 8 แถวพอสำหรับ battery(6)+motor(2)/data(8)) ----
    GameObject detailOverlay;
    TextMeshProUGUI detailTitleText;
    Image detailIconGlow;
    Image detailAccentStroke;
    const int DetailRowCount = 8;
    GameObject[] detailRows = new GameObject[DetailRowCount];
    TextMeshProUGUI[] detailLabels = new TextMeshProUGUI[DetailRowCount];
    TextMeshProUGUI[] detailValues = new TextMeshProUGUI[DetailRowCount];
    Coroutine detailSpinRoutine;

    // ---- bottom bar (3 ปุ่ม: AR / DATA / GRAPH - Battery/Motor ไม่มีปุ่ม
    // เฉพาะในแถบล่างอีกต่อไป เปิด detail ได้จากการแตะการ์ดลอย 2D หรือจุด
    // Connector บนตัวรถแทน ตามสเปกใหม่) ----
    Image btnOverviewBg, btnDataBg, btnGraphBg;

    // ---- ป้าย pill ปลาย Connector ที่ชี้ไปตำแหน่งจริงบนรถ - โชว์ค่าสดๆ
    // ตรงจุดที่ชี้ (ไม่ใช่แค่จุดเฉยๆ) ดู EVDataCardUI.Builder.cs >
    // AddWorldConnector ----
    TextMeshProUGUI batteryTagText;
    TextMeshProUGUI motorTagText;

    // ---- data graph (แท็บ Graph ใน detail overlay) - เก็บประวัติค่าล่าสุด
    // ไว้ในหน่วยความจำเฉยๆ (ไม่ persist ข้ามการเปิดแอปใหม่) แค่พอวาดกราฟ
    // แนวโน้มคร่าวๆ ให้ดูตอนเดโม ไม่ได้แตะ/เปลี่ยน data contract ใดๆ - เลือก
    // metric ที่จะพล็อตได้ (SetGraphMetric) แทนที่จะ fix ตายตัวแค่ SOC+RPM
    // เหมือนเดิม ----
    const int GraphHistoryCount = 40;
    List<float> socHistory = new List<float>();
    List<float> rpmHistory = new List<float>();
    List<float> voltageHistory = new List<float>();
    List<float> tempHistory = new List<float>();
    List<float> powerHistory = new List<float>();
    GameObject graphArea;
    RectTransform detailCardRect;
    RectTransform graphPlotArea;
    Transform graphLineParent;
    enum GraphMetric { SOC, Voltage, Temperature, Rpm, Power }
    GraphMetric selectedGraphMetric = GraphMetric.SOC;
    Image[] graphChipBg = new Image[5];
    TextMeshProUGUI graphValueText;

    // ---- last known data (สำหรับ refresh detail overlay ตอนเปิด) ----
    FlutterBridge.EVDataPayload lastData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<EVDataCardUI>() != null) return;
        var go = new GameObject("EVDataCardUI");
        go.AddComponent<EVDataCardUI>();
        DontDestroyOnLoad(go);
    }

    void Start()
    {
        if (!TryBindExisting())
        {
            BuildUI();
        }

        HideCard();
        CloseDetail();

        liveRoutine = StartCoroutine(LiveDotRoutine());

        if (FlutterBridge.Instance != null) Subscribe();
        else StartCoroutine(WaitForBridge());
    }

    IEnumerator WaitForBridge()
    {
        while (FlutterBridge.Instance == null) yield return null;
        Subscribe();
    }

    void Subscribe()
    {
        FlutterBridge.Instance.OnVehicleIdReceived.AddListener(OnVehicleId);
        FlutterBridge.Instance.OnEVDataReceived.AddListener(OnEVData);
    }

    void OnVehicleId(string vehicleId)
    {
        vehicleIdText.text = vehicleId;
        ShowCard();
    }

    void OnEVData(FlutterBridge.EVDataPayload data)
    {
        lastData = data;
        vehicleIdText.text = data.vehicleId;

        float socClamped = Mathf.Clamp01(data.soc / 100f);
        batteryPercentText.text = $"{data.soc:0}%";
        batteryRingFill.fillAmount = socClamped;
        batteryVoltageText.text = $"{data.voltage:0} V";
        batteryTempText.text = $"{data.batteryTemperature:0}°C";

        float rpmClamped = Mathf.Clamp01(data.motorRpm / MotorRpmMax);
        motorRpmText.text = $"{data.motorRpm:N0}";
        motorRingFill.fillAmount = rpmClamped;
        motorPowerText.text = $"{data.motorPower:0} kW";

        socHistory.Add(data.soc);
        if (socHistory.Count > GraphHistoryCount) socHistory.RemoveAt(0);
        rpmHistory.Add(data.motorRpm);
        if (rpmHistory.Count > GraphHistoryCount) rpmHistory.RemoveAt(0);
        voltageHistory.Add(data.voltage);
        if (voltageHistory.Count > GraphHistoryCount) voltageHistory.RemoveAt(0);
        tempHistory.Add(data.batteryTemperature);
        if (tempHistory.Count > GraphHistoryCount) tempHistory.RemoveAt(0);
        powerHistory.Add(data.motorPower);
        if (powerHistory.Count > GraphHistoryCount) powerHistory.RemoveAt(0);
        if (currentDetail == DetailMode.Graph) RefreshGraph();

        if (batteryTagText != null) batteryTagText.text = $"{data.soc:0}%";
        if (motorTagText != null) motorTagText.text = $"{data.motorRpm:N0}";

        bool normal = data.status == "NORMAL";
        bool socLow = data.soc < 20f;
        Color statusColor = normal ? (socLow ? Amber : GreenPrimary) : Danger;
        string statusLabel = string.IsNullOrEmpty(data.status) ? "-" : data.status;

        statusBadgeText.text = statusLabel;
        statusBadgeText.color = statusColor;
        statusBadgeBg.color = new Color(statusColor.r, statusColor.g, statusColor.b, 0.16f);

        batteryRingFill.color = (normal && !socLow) ? GreenPrimary : (socLow ? Amber : Danger);

        ShowCard();
        Pulse(ref batteryPulse, batteryRingFill.transform.parent);
        Pulse(ref motorPulse, motorRingFill.transform.parent);

        if (currentDetail != DetailMode.None) RefreshDetailValues();
    }

    void ShowCard()
    {
        worldSpaceBattery.gameObject.SetActive(true);
        worldSpaceMotor.gameObject.SetActive(true);
    }

    void HideCard()
    {
        worldSpaceBattery.gameObject.SetActive(false);
        worldSpaceMotor.gameObject.SetActive(false);
    }

    // ==========================================
    // ตรึง WorldSpace_UI ให้ลอยหน้ากล้อง AR เสมอ (billboard) และสเกลให้พอดี
    // กับขอบจอจริง (คำนวณจาก FOV/ระยะ) เพื่อให้ % ความกว้างการ์ดที่ตั้งไว้
    // (300/1080 = 27.8% ของ CanvasUnitsWide) ตรงกับ % ความกว้างจอจริงเป๊ะๆ
    // ไม่ว่ามือถือเครื่องไหน - อ่าน Camera.main อย่างเดียว ไม่ได้แตะ/สร้าง
    // กล้องหรือ tracking component ใดๆ เอง
    //
    // ⚠️ นี่คือโหมด "ก่อนแตะวาง" เท่านั้น (พรีวิว/reticle-like) พอผู้ใช้แตะพื้น
    // แล้วและ ARPlacementController เรียก AttachToAnchor() สำเร็จ (isAnchored
    // = true) เมธอดนี้จะหยุดทำงานทันที เพราะ worldSpaceUI กลายเป็นลูกของ
    // ARAnchor จริงแล้ว - ตำแหน่ง/การหมุนจะถูก AR Foundation อัปเดตให้เองจาก
    // การ track ของอุปกรณ์ (ข้อ ④ ที่ผู้ใช้ต้องการ: ขยับมือถือ -> Panel อยู่
    // ตำแหน่งเดิม)
    // ==========================================
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        float worldViewHeight = 2f * WorldHudDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float scale = worldViewHeight / CanvasUnitsHigh;

        if (worldSpaceBattery != null && !batteryAnchored)
        {
            worldSpaceBattery.position = cam.transform.position + cam.transform.forward * WorldHudDistance;
            worldSpaceBattery.rotation = cam.transform.rotation;
            worldSpaceBattery.localScale = Vector3.one * scale;
        }

        if (worldSpaceMotor != null && !motorAnchored)
        {
            worldSpaceMotor.position = cam.transform.position + cam.transform.forward * WorldHudDistance;
            worldSpaceMotor.rotation = cam.transform.rotation;
            worldSpaceMotor.localScale = Vector3.one * scale;
        }
    }

    // ==========================================
    // Hook กลางที่ใช้ร่วมกันโดยทุกตัวควบคุมการวางตำแหน่ง AR:
    //   - VehicleMarkerTracker  (แนะนำ - ผูกกับ ARTrackedImage/marker บนรถ)
    //   - ARPlacementController (สำรอง - ผูกกับ ARAnchor จากการแตะพื้น)
    // ย้าย WorldSpace_UI ไปเป็นลูกของ anchor ที่ส่งเข้ามา แล้วหยุด billboard
    // ตามกล้องถาวร (isAnchored = true) - ไม่แตะ data/API logic ใดๆ ทั้งสิ้น
    // ==========================================
    public void AttachBatteryToAnchor(Transform anchor, Vector3 localOffset)
        => AttachPanel(worldSpaceBattery, anchor, localOffset, ref batteryAnchored);

    public void AttachMotorToAnchor(Transform anchor, Vector3 localOffset)
        => AttachPanel(worldSpaceMotor, anchor, localOffset, ref motorAnchored);

    void AttachPanel(Transform panelRoot, Transform anchor, Vector3 localOffset, ref bool anchoredFlag)
    {
        if (panelRoot == null || anchor == null) return;

        panelRoot.SetParent(anchor, false);
        panelRoot.localPosition = localOffset;
        panelRoot.localRotation = Quaternion.identity;
        // สเกลเริ่มต้นสำหรับ AR บนมือถือ (หน่วย Canvas 1080x1920 ต้องย่อเป็น
        // สเกลระดับเมตรจริง) - ค่าตั้งต้นตามที่ผู้ใช้แนะนำ ปรับได้ทีหลังจาก
        // ทดสอบบนอุปกรณ์จริง (ไม่ควรอิงจาก Scene View เพียงอย่างเดียว)
        panelRoot.localScale = Vector3.one * 0.0015f;

        anchoredFlag = true;
    }

    // แสดงข้อความแนะนำใน TrackingMessage เช่น "แตะพื้นเพื่อวาง AR HUD" ก่อน
    // วาง แล้วเปลี่ยนเป็น "AR HUD ACTIVE" หลังวางสำเร็จ (เรียกจาก
    // ARPlacementController)
    public void SetTrackingMessage(string text)
    {
        if (trackingMessageText != null) trackingMessageText.text = text;
    }

    // เด้งเบาๆ ที่เกจเมื่อมีข้อมูลใหม่ - ให้รู้สึกเรียลไทม์
    void Pulse(ref Coroutine slot, Transform target)
    {
        if (target == null) return;
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(PulseRoutine(target));
    }

    IEnumerator PulseRoutine(Transform target)
    {
        float dur = 0.26f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin((t / dur) * Mathf.PI);
            target.localScale = Vector3.one * (1f + 0.08f * k);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    // จุด LIVE กระพริบเบาๆ ตลอดเวลา (ambient animation ไม่ผูกกับข้อมูลจริง)
    IEnumerator LiveDotRoutine()
    {
        while (true)
        {
            float t = Time.time * 2.2f;
            float a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t));
            if (liveDot != null)
            {
                var c = liveDot.color;
                liveDot.color = new Color(c.r, c.g, c.b, a);
            }
            yield return null;
        }
    }

    // ==========================================
    // Detail overlay: เปิด/ปิด + เติมค่าแต่ละโหมด (เรียกจาก onClick ที่ผูกไว้
    // ใน EVDataCardUI.Builder.cs)
    // ==========================================
    void OpenDetail(DetailMode mode)
    {
        currentDetail = mode;
        detailOverlay.SetActive(mode != DetailMode.None);
        SetActiveBottomButton(mode);
        if (mode == DetailMode.None) return;

        bool isGraph = mode == DetailMode.Graph;
        if (graphArea != null) graphArea.SetActive(isGraph);
        if (detailIconGlow != null) detailIconGlow.gameObject.SetActive(!isGraph);

        switch (mode)
        {
            case DetailMode.Battery:
                detailTitleText.text = "BATTERY DETAIL";
                detailAccentStroke.color = GreenPrimary;
                detailIconGlow.color = new Color(GreenPrimary.r, GreenPrimary.g, GreenPrimary.b, 0.35f);
                SetRow(0, "SOC", true);
                SetRow(1, "VOLTAGE", true);
                SetRow(2, "TEMPERATURE", true);
                SetRow(3, "CURRENT", true);
                SetRow(4, "CYCLE COUNT", true);
                SetRow(5, "INTERNAL RESISTANCE", true);
                SetRow(6, "", false);
                SetRow(7, "", false);
                break;
            case DetailMode.Motor:
                detailTitleText.text = "MOTOR DETAIL";
                detailAccentStroke.color = CyanPrimary;
                detailIconGlow.color = new Color(CyanPrimary.r, CyanPrimary.g, CyanPrimary.b, 0.35f);
                SetRow(0, "RPM", true);
                SetRow(1, "POWER", true);
                for (int i = 2; i < DetailRowCount; i++) SetRow(i, "", false);
                break;
            case DetailMode.Data:
                detailTitleText.text = "ALL DATA";
                detailAccentStroke.color = TextSecondary;
                detailIconGlow.color = new Color(CyanPrimary.r, CyanPrimary.g, CyanPrimary.b, 0.2f);
                SetRow(0, "SOC", true);
                SetRow(1, "VOLTAGE", true);
                SetRow(2, "TEMPERATURE", true);
                SetRow(3, "CURRENT", true);
                SetRow(4, "CYCLE COUNT", true);
                SetRow(5, "INTERNAL RESISTANCE", true);
                SetRow(6, "RPM", true);
                SetRow(7, "POWER", true);
                break;
            case DetailMode.Graph:
                detailTitleText.text = "DATA GRAPH";
                detailAccentStroke.color = CyanPrimary;
                for (int i = 0; i < DetailRowCount; i++) SetRow(i, "", false);
                break;
        }

        RefreshDetailValues();
        if (isGraph) RefreshGraph();

        if (detailSpinRoutine != null) { StopCoroutine(detailSpinRoutine); detailSpinRoutine = null; }
        if (!isGraph) detailSpinRoutine = StartCoroutine(SpinRoutine(detailIconGlow.transform));
    }

    void SetRow(int i, string label, bool visible)
    {
        detailRows[i].SetActive(visible);
        if (visible) detailLabels[i].text = label;
    }

    // ⚠️ Current / Cycle Count / Internal Resistance ยังไม่มีอยู่ใน
    // FlutterBridge.EVDataPayload (data contract เดิมมีแค่
    // soc/voltage/batteryTemperature/motorRpm/motorPower/status) - ตามที่
    // สั่งไว้ว่าห้ามแตะโค้ด data/API เลยแสดงเป็น "N/A" ไปก่อน ถ้าต้องการค่า
    // จริงต้องเพิ่ม field ฝั่ง Flutter/FlutterBridge ก่อน (คนละงานกับ UI นี้)
    void RefreshDetailValues()
    {
        if (lastData == null) return;
        switch (currentDetail)
        {
            case DetailMode.Battery:
                detailValues[0].text = $"{lastData.soc:0}%";
                detailValues[1].text = $"{lastData.voltage:0} V";
                detailValues[2].text = $"{lastData.batteryTemperature:0}°C";
                detailValues[3].text = "N/A";
                detailValues[4].text = "N/A";
                detailValues[5].text = "N/A";
                break;
            case DetailMode.Motor:
                detailValues[0].text = $"{lastData.motorRpm:N0}";
                detailValues[1].text = $"{lastData.motorPower:0} kW";
                break;
            case DetailMode.Data:
                detailValues[0].text = $"{lastData.soc:0}%";
                detailValues[1].text = $"{lastData.voltage:0} V";
                detailValues[2].text = $"{lastData.batteryTemperature:0}°C";
                detailValues[3].text = "N/A";
                detailValues[4].text = "N/A";
                detailValues[5].text = "N/A";
                detailValues[6].text = $"{lastData.motorRpm:N0}";
                detailValues[7].text = $"{lastData.motorPower:0} kW";
                break;
        }
    }

    // อัปเดตกราฟเส้นแนวโน้มของ metric ที่เลือกอยู่ (selectedGraphMetric) ใน
    // tab Graph จากประวัติที่เก็บไว้ - เรียกตอนเปิด tab Graph, ตอนสลับ
    // metric (SetGraphMetric) และทุกครั้งที่มีข้อมูลใหม่เข้ามาระหว่างเปิด
    // tab นี้อยู่
    void RefreshGraph()
    {
        if (graphLineParent == null || graphPlotArea == null) return;

        // บังคับให้ Layout คำนวณขนาดพื้นที่กราฟให้เสร็จก่อนอ่านค่า .rect -
        // เพราะ GraphArea อาจเพิ่งถูก SetActive(true) เฟรมนี้เอง (ตอน
        // inactive มันไม่ถูกจัดสรรพื้นที่โดย VerticalLayoutGroup ของ Card)
        Canvas.ForceUpdateCanvases();
        if (detailCardRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(detailCardRect);

        Vector2 plotSize = graphPlotArea.rect.size;
        if (plotSize.x < 10f || plotSize.y < 10f) plotSize = new Vector2(560, 260);

        List<float> raw;
        Color lineColor;
        switch (selectedGraphMetric)
        {
            case GraphMetric.Voltage: raw = voltageHistory; lineColor = Amber; break;
            case GraphMetric.Temperature: raw = tempHistory; lineColor = Danger; break;
            case GraphMetric.Rpm: raw = rpmHistory; lineColor = CyanPrimary; break;
            case GraphMetric.Power: raw = powerHistory; lineColor = CyanPrimary; break;
            default: raw = socHistory; lineColor = GreenPrimary; break;
        }

        var norm = new List<float>();
        float maxVal = GraphMetricMax(selectedGraphMetric);
        foreach (var v in raw) norm.Add(Mathf.Clamp01(v / maxVal));
        DrawLineGraph(graphLineParent, norm, lineColor, plotSize);

        if (graphValueText != null)
        {
            float latest = raw.Count > 0 ? raw[raw.Count - 1] : 0f;
            graphValueText.text = FormatGraphValue(selectedGraphMetric, latest);
        }
    }

    // ค่าสูงสุดโดยประมาณของแต่ละ metric - ใช้ normalize เส้นกราฟให้อยู่ในกรอบ
    // plot เท่านั้น ไม่กระทบตัวเลขจริงที่แสดง (graphValueText/detailValues
    // ยังใช้ค่าจาก data ตรงๆ เสมอ)
    float GraphMetricMax(GraphMetric metric)
    {
        switch (metric)
        {
            case GraphMetric.Voltage: return 500f;
            case GraphMetric.Temperature: return 80f;
            case GraphMetric.Rpm: return MotorRpmMax;
            case GraphMetric.Power: return 150f;
            default: return 100f; // SOC %
        }
    }

    string FormatGraphValue(GraphMetric metric, float v)
    {
        switch (metric)
        {
            case GraphMetric.Voltage: return $"{v:0} V";
            case GraphMetric.Temperature: return $"{v:0}°C";
            case GraphMetric.Rpm: return $"{v:N0} RPM";
            case GraphMetric.Power: return $"{v:0} kW";
            default: return $"{v:0}%";
        }
    }

    // เรียกจากปุ่ม chip เลือก metric ใน tab Graph (ผูกไว้ใน
    // EVDataCardUI.Builder.cs > BuildGraphArea/RewireGraphChips) - สลับ
    // metric ที่กำลังพล็อตแล้ว refresh เส้นกราฟทันที
    public void SetGraphMetric(int metricIndex)
    {
        if (metricIndex < 0 || metricIndex >= graphChipBg.Length) return;
        selectedGraphMetric = (GraphMetric)metricIndex;
        for (int i = 0; i < graphChipBg.Length; i++) SetBtnActive(graphChipBg[i], i == metricIndex);
        RefreshGraph();
    }

    void CloseDetail()
    {
        currentDetail = DetailMode.None;
        if (detailOverlay != null) detailOverlay.SetActive(false);
        if (detailSpinRoutine != null) { StopCoroutine(detailSpinRoutine); detailSpinRoutine = null; }
        SetActiveBottomButton(DetailMode.None);
    }

    IEnumerator SpinRoutine(Transform t)
    {
        while (true)
        {
            t.Rotate(0f, 0f, 22f * Time.deltaTime);
            yield return null;
        }
    }

    // 3 ปุ่มเท่านั้น: AR (ครอบคลุม None/Battery/Motor - เพราะ Battery/Motor
    // detail ตอนนี้เปิดจากการแตะการ์ดลอย/จุด Connector ไม่ใช่ปุ่มแถบล่างแล้ว)
    // / DATA / GRAPH
    void SetActiveBottomButton(DetailMode mode)
    {
        bool inAr = mode == DetailMode.None || mode == DetailMode.Battery || mode == DetailMode.Motor;
        SetBtnActive(btnOverviewBg, inAr);
        SetBtnActive(btnDataBg, mode == DetailMode.Data);
        SetBtnActive(btnGraphBg, mode == DetailMode.Graph);
    }

    void SetBtnActive(Image bg, bool active)
    {
        if (bg == null) return;
        bg.color = active ? new Color(CyanPrimary.r, CyanPrimary.g, CyanPrimary.b, 0.22f)
                           : new Color(1f, 1f, 1f, 0.04f);
    }
}
