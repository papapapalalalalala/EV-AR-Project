using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ส่วนสร้าง Hierarchy จริง (BuildUI) + bind ของเดิมที่เคย build ไว้แล้ว
/// (TryBindExisting) ของ EVDataCardUI - แยกไฟล์จาก EVDataCardUI.cs (ที่เก็บ
/// logic ข้อมูล/แอนิเมชัน) เพื่อไม่ให้ไฟล์เดียวยาวเกินไป (partial class
/// เดียวกัน)
/// </summary>
public partial class EVDataCardUI
{
    // ==========================================
    // หา GameObject ที่ถูกสร้าง/เซฟไว้แล้ว (จาก EV AR > Build EV Data Card
    // UI ใน Editor หรือจากการรันแอปครั้งก่อนที่เซฟ Scene ไว้) มา bind
    // ==========================================
    bool TryBindExisting()
    {
        var wsuBattery = transform.Find("WorldSpace_UI_Battery");
        var wsuMotor = transform.Find("WorldSpace_UI_Motor");
        var ssu = transform.Find("ScreenSpace_UI");
        if (wsuBattery == null || wsuMotor == null || ssu == null) return false;
        worldSpaceBattery = wsuBattery;
        worldSpaceMotor = wsuMotor;
        screenSpaceUI = ssu;

        var batteryPanel = wsuBattery.Find("BatteryAnchor/BatteryARPanel");
        var motorPanel = wsuMotor.Find("MotorAnchor/MotorARPanel");
        if (batteryPanel == null || motorPanel == null) return false;

        var bGauge = batteryPanel.Find("GaugeRow/Gauge");
        batteryRingFill = bGauge != null ? bGauge.Find("RingFill")?.GetComponent<Image>() : null;
        batteryPercentText = bGauge != null ? bGauge.Find("CenterText/ValueText")?.GetComponent<TextMeshProUGUI>() : null;
        batteryVoltageText = batteryPanel.Find("StatsCol/VoltageRow/Value")?.GetComponent<TextMeshProUGUI>();
        batteryTempText = batteryPanel.Find("StatsCol/TempRow/Value")?.GetComponent<TextMeshProUGUI>();

        var mGauge = motorPanel.Find("GaugeRow/Gauge");
        motorRingFill = mGauge != null ? mGauge.Find("RingFill")?.GetComponent<Image>() : null;
        motorRpmText = mGauge != null ? mGauge.Find("CenterText/ValueText")?.GetComponent<TextMeshProUGUI>() : null;
        motorPowerText = motorPanel.Find("StatsCol/PowerRow/Value")?.GetComponent<TextMeshProUGUI>();

        var topBar = ssu.Find("TopBar");
        vehicleIdText = topBar != null ? topBar.Find("TitleBlock/VehicleIdText")?.GetComponent<TextMeshProUGUI>() : null;
        var badge = topBar != null ? topBar.Find("StatusBadge") : null;
        statusBadgeBg = badge != null ? badge.GetComponent<Image>() : null;
        statusBadgeText = badge != null ? badge.Find("Text")?.GetComponent<TextMeshProUGUI>() : null;
        liveDot = topBar != null ? topBar.Find("LiveGroup/LiveDot")?.GetComponent<Image>() : null;

        trackingMessageText = ssu.Find("TrackingMessage/Text")?.GetComponent<TextMeshProUGUI>();

        var bottom = ssu.Find("BottomControls");
        btnOverviewBg = bottom != null ? bottom.Find("BtnOverview")?.GetComponent<Image>() : null;
        btnDataBg = bottom != null ? bottom.Find("BtnData")?.GetComponent<Image>() : null;
        btnGraphBg = bottom != null ? bottom.Find("BtnGraph")?.GetComponent<Image>() : null;
        RewireBottomButtons(bottom);
        RewirePanelButtons(batteryPanel, motorPanel);

        batteryTagText = wsuBattery.Find("BatteryAnchor/Connector/Pill/Value")?.GetComponent<TextMeshProUGUI>();
        motorTagText = wsuMotor.Find("MotorAnchor/Connector/Pill/Value")?.GetComponent<TextMeshProUGUI>();

        var overlay = ssu.Find("DetailOverlay");
        detailOverlay = overlay != null ? overlay.gameObject : null;
        var card = overlay != null ? overlay.Find("Card") : null;
        detailCardRect = card != null ? card.GetComponent<RectTransform>() : null;
        detailAccentStroke = card != null ? card.Find("Stroke")?.GetComponent<Image>() : null;
        detailTitleText = card != null ? card.Find("Header/Title")?.GetComponent<TextMeshProUGUI>() : null;
        detailIconGlow = card != null ? card.Find("IconGlow")?.GetComponent<Image>() : null;
        RewireCloseButtons(overlay);

        if (card != null)
        {
            var rows = card.Find("Rows");
            for (int i = 0; i < DetailRowCount; i++)
            {
                var row = rows != null ? rows.Find($"Row{i}") : null;
                detailRows[i] = row != null ? row.gameObject : null;
                detailLabels[i] = row != null ? row.Find("Label")?.GetComponent<TextMeshProUGUI>() : null;
                detailValues[i] = row != null ? row.Find("Value")?.GetComponent<TextMeshProUGUI>() : null;
            }

            var graphAreaT = card.Find("GraphArea");
            graphArea = graphAreaT != null ? graphAreaT.gameObject : null;
            graphValueText = graphAreaT != null ? graphAreaT.Find("GraphValue")?.GetComponent<TextMeshProUGUI>() : null;
            var plotFrameT = graphAreaT != null ? graphAreaT.Find("PlotFrame") : null;
            graphPlotArea = plotFrameT != null ? plotFrameT.GetComponent<RectTransform>() : null;
            graphLineParent = plotFrameT != null ? plotFrameT.Find("Line") : null;
            var chipRowT = graphAreaT != null ? graphAreaT.Find("ChipRow") : null;
            if (chipRowT != null)
            {
                for (int i = 0; i < graphChipBg.Length; i++)
                {
                    var chip = chipRowT.Find($"Chip{i}");
                    graphChipBg[i] = chip != null ? chip.GetComponent<Image>() : null;
                }
            }
            RewireGraphChips(chipRowT);
        }

        return vehicleIdText != null && statusBadgeText != null && liveDot != null
            && batteryRingFill != null && batteryPercentText != null && batteryVoltageText != null && batteryTempText != null
            && motorRingFill != null && motorRpmText != null && motorPowerText != null
            && detailOverlay != null && detailTitleText != null && detailIconGlow != null
            && detailRows[0] != null && detailRows[7] != null;
    }

    void RewireBottomButtons(Transform bottom)
    {
        if (bottom == null) return;
        WireClick(bottom.Find("BtnOverview"), () => OpenDetail(DetailMode.None));
        WireClick(bottom.Find("BtnData"), () => OpenDetail(DetailMode.Data));
        WireClick(bottom.Find("BtnGraph"), () => OpenDetail(DetailMode.Graph));
    }

    void RewireGraphChips(Transform chipRow)
    {
        if (chipRow == null) return;
        for (int i = 0; i < graphChipBg.Length; i++)
        {
            int idx = i;
            WireClick(chipRow.Find($"Chip{i}"), () => SetGraphMetric(idx));
        }
    }

    void RewirePanelButtons(Transform batteryPanel, Transform motorPanel)
    {
        WireClick(batteryPanel, () => OpenDetail(DetailMode.Battery));
        WireClick(motorPanel, () => OpenDetail(DetailMode.Motor));
    }

    void RewireCloseButtons(Transform overlay)
    {
        if (overlay == null) return;
        WireClick(overlay.Find("Scrim"), () => CloseDetail());
        WireClick(overlay.Find("Card/Header/CloseBtn"), () => CloseDetail());
        var ssu = screenSpaceUI;
        WireClick(ssu != null ? ssu.Find("TopBar/BackBtn") : null, () => CloseDetail());
    }

    static void WireClick(Transform t, UnityEngine.Events.UnityAction action)
    {
        if (t == null) return;
        var btn = t.GetComponent<Button>();
        if (btn == null) btn = t.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    // ==========================================
    // สร้าง UI ทั้งหมดด้วยโค้ด - เรียกจาก EVDataCardBuilder.cs (เมนู EV AR >
    // Build...) ตอนออกแบบใน Editor และจาก Start() ตอนรันจริงถ้ายังไม่เคย
    // build ไว้ใน Scene เลย (fallback)
    // ==========================================
    public void BuildUI()
    {
        BuildWorldSpaceUI();
        BuildScreenSpaceUI();
    }

    // เดิม Battery/Motor เคยอยู่ใน Canvas เดียวกันชื่อ "WorldSpace_UI" (ลอย
    // เป็นคู่ตายตัว) - ถ้าเจอ GameObject ชื่อเดิมค้างอยู่ (จาก Scene ที่เซฟ
    // ไว้ก่อนหน้า) ให้ลบทิ้งก่อนสร้างโครงสร้างใหม่ กันไม่ให้มี GameObject
    // ซ้ำซ้อน/ลอยไม่ได้ใช้งานค้างใน Hierarchy
    void RemoveLegacyCombinedWorldUI()
    {
        var legacy = transform.Find("WorldSpace_UI");
        if (legacy != null) DestroyImmediate(legacy.gameObject);
    }

    void BuildWorldSpaceUI()
    {
        RemoveLegacyCombinedWorldUI();

        // Battery กับ Motor แยก Canvas World Space อิสระกันคนละอัน (คนละ
        // GameObject) เพื่อให้ผูก (AttachBatteryToAnchor / AttachMotorToAnchor)
        // กับ offset จริงบนตัวรถได้คนละตำแหน่งจาก marker ใบเดียวกัน - ก่อนผูก
        // (ยัง isAnchored=false) ทั้งคู่จะลอยหน้ากล้องซ้อนกันไปก่อนเฉยๆ
        var batteryGO = new GameObject("WorldSpace_UI_Battery");
        batteryGO.transform.SetParent(transform, false);
        // ตำแหน่ง/สเกลเริ่มต้นนี้แค่กันไม่ให้ Battery/Motor ซ้อนทับกันตอนดูใน
        // Editor เฉยๆ (ยังไม่ได้ attach กับ marker) - ค่าจริงตอนรันบนมือถือจะถูก
        // เขียนทับด้วย AttachBatteryToAnchor(batteryOffset) จาก VehicleMarkerTracker
        // เสมอ ไม่กระทบ behavior ตอน runtime
        batteryGO.transform.localPosition = new Vector3(-19f, 0.15f, 0.5f);
        batteryGO.transform.localScale = Vector3.one * 0.06f;
        worldSpaceBattery = batteryGO.transform;
        SetupWorldCanvas(batteryGO);

        var batteryAnchor = CreateAnchor(batteryGO.transform, "BatteryAnchor", new Vector2(0, 60));
        var batteryPanel = BuildWorldPanel(batteryAnchor.transform, "BatteryARPanel", "BATTERY", GreenPrimary,
            out batteryRingFill, out batteryPercentText, "SOC",
            "VoltageRow", "Voltage", out batteryVoltageText,
            "TempRow", "Temp", out batteryTempText);
        // การ์ด/โมเดล 3D ของ Battery ทั้งกลุ่มลอยออกไปทาง "นอกตัวรถ" ฝั่งซ้าย
        // (outSign=-1) ให้ห่างจากจุด Inspection Point (BatteryAnchor local
        // origin = จุดบนผิวรถจริง) และห่างจากกลุ่มของ Motor (ฝั่งขวา) แน่นอน
        // ไม่ทับกันไม่ว่า offset จริงจาก marker จะเป็นเท่าไหร่ก็ตาม (ดู
        // คอมเมนต์ยาวที่ ClusterAnchor ด้านล่าง)
        batteryPanel.GetComponent<RectTransform>().anchoredPosition = CardAnchorPos(-1f);
        AddWorldConnector(batteryAnchor.transform, GreenPrimary, -1f, "BATTERY", out batteryTagText);
        // โมเดล 3D (primitive สร้างด้วยโค้ดล้วนๆ ดู EVComponent3D.cs) เป็นแค่
        // "ภาพประกอบ" ลอยอยู่ในกลุ่มเดียวกับการ์ด ไม่ใช่ตำแหน่งจริงของ
        // แบตเตอรี่ในตัวรถ (ดูคอมเมนต์หัวไฟล์เรื่อง External Inspection
        // Point) - วางที่ ClusterAnchor เดียวกับที่เส้น Connector ชี้ไป แค่
        // ถอยเข้าไปในจอ (local Z) อีกนิดให้ดูมีมิติ
        var batteryCluster = ClusterAnchor(-1f);
        // ปิดโมเดล 3D ไว้ก่อนตามที่ขอ (เอาออกชั่วคราว) - เปิดกลับมาแค่ uncomment บรรทัดนี้
        // EVComponent3D.BuildBattery(batteryAnchor.transform, new Vector3(batteryCluster.x, batteryCluster.y, 160f), GreenPrimary);
        WireClick(batteryPanel.transform, () => OpenDetail(DetailMode.Battery));

        var motorGO = new GameObject("WorldSpace_UI_Motor");
        motorGO.transform.SetParent(transform, false);
        // เช่นเดียวกับ battery ด้านบน - แค่กันซ้อนกันตอนดูใน Editor เท่านั้น
        motorGO.transform.localPosition = new Vector3(19f, 0.15f, 0.5f);
        motorGO.transform.localScale = Vector3.one * 0.06f;
        worldSpaceMotor = motorGO.transform;
        SetupWorldCanvas(motorGO);

        var motorAnchor = CreateAnchor(motorGO.transform, "MotorAnchor", new Vector2(0, 60));
        var motorPanel = BuildWorldPanel(motorAnchor.transform, "MotorARPanel", "MOTOR", CyanPrimary,
            out motorRingFill, out motorRpmText, "RPM",
            "PowerRow", "Power", out motorPowerText,
            null, null, out _);
        // กลุ่มของ Motor ลอยออกไปทาง "นอกตัวรถ" ฝั่งขวา (outSign=+1) - ตรงข้าม
        // กับ Battery เสมอ กันไม่ให้สองกลุ่มมาซ้อนทับกัน (ดูคอมเมนต์ฝั่ง
        // Battery ด้านบน)
        motorPanel.GetComponent<RectTransform>().anchoredPosition = CardAnchorPos(+1f);
        AddWorldConnector(motorAnchor.transform, CyanPrimary, +1f, "MOTOR", out motorTagText);
        var motorCluster = ClusterAnchor(+1f);
        // ปิดโมเดล 3D ไว้ก่อนตามที่ขอ (เอาออกชั่วคราว) - เปิดกลับมาแค่ uncomment บรรทัดนี้
        // EVComponent3D.BuildMotor(motorAnchor.transform, new Vector3(motorCluster.x, motorCluster.y, 160f), CyanPrimary);
        WireClick(motorPanel.transform, () => OpenDetail(DetailMode.Motor));
    }

    // ==========================================
    // "External Inspection Point" - แนวคิดสำคัญ (อย่าแก้โดยไม่อ่านตรงนี้):
    // ระบบนี้ไม่ได้ตรวจจับตำแหน่งจริงของแบตเตอรี่/มอเตอร์ที่ซ่อนอยู่ในตัวรถ
    // (กล้องมองทะลุเข้าไปในรถไม่ได้ และไม่ได้ใช้ AI object detection ใดๆ)
    // สิ่งที่ BatteryAnchor/MotorAnchor แทนคือ "จุดบนผิวรถด้านนอกที่กำหนดไว้
    // ล่วงหน้า" (predefined) สำหรับให้ช่างมองไปที่จุดนั้น - ไม่ใช่พิกัดจริง
    // ของชิ้นส่วนข้างใน (ดูคอมเมนต์หัวไฟล์ EVDataCardUI.cs เพิ่มเติม)
    //
    // InspectionDot = จุดจุดเดียวบนผิวรถ (anchor local origin ตรงๆ)
    // ClusterAnchor(outSign) = จุดที่กลุ่ม "เส้นชี้ + โมเดล 3D" ลอยออกไปอยู่
    //   นอกตัวรถ (outSign: -1 = ฝั่งซ้ายสำหรับ Battery, +1 = ฝั่งขวาสำหรับ
    //   Motor) - ห่างจาก InspectionDot แน่นอนเสมอ ไม่ทับตัวรถ ไม่ทับกันเอง
    // CardAnchorPos(outSign) = จุดที่การ์ดข้อมูล (ตัวเลข/เกจ) ลอยอยู่ - อยู่
    //   เหนือ ClusterAnchor ขึ้นไปอีก เว้นระยะพอที่โมเดล 3D กับการ์ดจะไม่ทับกัน
    // ==========================================
    static Vector2 InspectionDot => Vector2.zero;
    static Vector2 ClusterAnchor(float outSign) => new Vector2(outSign * 210f, 260f);
    static Vector2 CardAnchorPos(float outSign) => new Vector2(outSign * 280f, 150f);

    void SetupWorldCanvas(GameObject go)
    {
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(CanvasUnitsWide, CanvasUnitsHigh);
        rect.pivot = new Vector2(0.5f, 0.5f);
        go.AddComponent<GraphicRaycaster>();
    }

    GameObject CreateAnchor(Transform parent, string name, Vector2 pos)
    {
        var go = CreateUIObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = pos;
        return go;
    }

    // การ์ดกระจกโปร่งแสงลอยหน้ากล้อง (compact, กว้างคงที่ 380 units - ขยายจาก 300
    // เดิมเพื่อรองรับตัวเลขฟอนต์ใหญ่ตามที่ต้องการให้อ่านง่ายบนมือถือจริง) - มี
    // เกจวงกลม + ตัวเลขใหญ่ตรงกลาง + แถวค่าย่อย 1-2 แถว เท่านั้น (ข้อมูล
    // น้อยที่สุดตามสเปก "Minimal information only") แตะทั้งการ์ดเพื่อเปิด
    // detail overlay (ผูก Button ที่ผู้เรียกใช้)
    GameObject BuildWorldPanel(Transform parent, string name, string title, Color accent,
        out Image ringFill, out TextMeshProUGUI valueText, string gaugeCaption,
        string stat1Name, string stat1Label, out TextMeshProUGUI stat1Value,
        string stat2Name, string stat2Label, out TextMeshProUGUI stat2Value)
    {
        const float w = 520f;
        var panel = BuildGlassPanel(parent, name, w, accent, out var stroke, out var glow);
        panel.AddComponent<Button>().transition = Selectable.Transition.None;

        var vLayout = panel.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(22, 22, 18, 20);
        vLayout.spacing = 14;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandHeight = false;
        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var titleText = CreateText("Title", panel.transform, 16, accent, FontStyles.Bold, TextAlignmentOptions.Left);
        titleText.text = title;
        titleText.characterSpacing = 3;
        var titleLE = titleText.gameObject.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 22;

        BuildCompactGauge(panel.transform, accent, gaugeCaption, 170, out ringFill, out valueText);

        var statsCol = CreateUIObject("StatsCol", panel.transform);
        var sLayout = statsCol.AddComponent<VerticalLayoutGroup>();
        sLayout.spacing = 10;
        sLayout.childControlWidth = true;
        sLayout.childControlHeight = false;
        sLayout.childForceExpandWidth = true;

        stat1Value = AddCompactStat(statsCol.transform, stat1Name, stat1Label);
        stat2Value = string.IsNullOrEmpty(stat2Name) ? null : AddCompactStat(statsCol.transform, stat2Name, stat2Label);

        return panel;
    }

    // ชั้นกระจกโปร่งแสง: glow (ฟุ้งใหญ่ อัลฟ่าต่ำ) -> fill (พื้นเข้ม
    // โปร่งแสง) -> stroke (เส้นขอบเรืองแสงบาง) ซ้อนกัน 3 ชั้น
    GameObject BuildGlassPanel(Transform parent, string name, float width, Color accent, out Image stroke, out Image glow)
    {
        var panel = CreateUIObject(name, parent);
        var rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 0);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        var glowGO = CreateUIObject("Glow", panel.transform);
        var glowRect = glowGO.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero; glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-14, -14); glowRect.offsetMax = new Vector2(14, 14);
        glow = glowGO.AddComponent<Image>();
        glow.sprite = GlowRoundedRect.GetGlow(128);
        glow.color = new Color(accent.r, accent.g, accent.b, 0.22f);
        glow.raycastTarget = false;
        glowGO.AddComponent<LayoutElement>().ignoreLayout = true; // อย่าให้ VerticalLayoutGroup ของ panel มาคุมขนาด

        var fillImg = panel.AddComponent<Image>();
        fillImg.color = GlassBg;
        fillImg.sprite = UIRoundedRect.GetSprite(20, 40);
        fillImg.type = Image.Type.Sliced;

        var strokeGO = CreateUIObject("Stroke", panel.transform);
        StretchFull(strokeGO.GetComponent<RectTransform>());
        stroke = strokeGO.AddComponent<Image>();
        stroke.sprite = GlowRoundedRect.GetStroke(20, 2, 128);
        stroke.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        stroke.type = Image.Type.Sliced;
        stroke.raycastTarget = false;
        strokeGO.AddComponent<LayoutElement>().ignoreLayout = true;

        return panel;
    }

    // เกจวงกลมเล็ก (ใช้ในการ์ดลอย compact) - ตัวเลขใหญ่กลาง + caption เล็ก
    void BuildCompactGauge(Transform parent, Color ringColor, string caption, float size, out Image ringFill, out TextMeshProUGUI valueText)
    {
        var row = CreateUIObject("GaugeRow", parent);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = size;

        var gauge = CreateUIObject("Gauge", row.transform);
        var gaugeLE = gauge.AddComponent<LayoutElement>();
        gaugeLE.preferredWidth = size;
        gaugeLE.preferredHeight = size;

        var ringBG = CreateUIObject("RingBG", gauge.transform);
        StretchFull(ringBG.GetComponent<RectTransform>());
        var ringBGImg = ringBG.AddComponent<Image>();
        ringBGImg.sprite = GaugeSprite.GetRing(0.80f, 160);
        ringBGImg.color = new Color(1f, 1f, 1f, 0.08f);

        var ringFillGO = CreateUIObject("RingFill", gauge.transform);
        StretchFull(ringFillGO.GetComponent<RectTransform>());
        ringFill = ringFillGO.AddComponent<Image>();
        ringFill.sprite = GaugeSprite.GetRing(0.80f, 160);
        ringFill.color = ringColor;
        ringFill.type = Image.Type.Filled;
        ringFill.fillMethod = Image.FillMethod.Radial360;
        ringFill.fillOrigin = (int)Image.Origin360.Top;
        ringFill.fillClockwise = true;
        ringFill.fillAmount = 0f;

        var centerText = CreateUIObject("CenterText", gauge.transform);
        StretchFull(centerText.GetComponent<RectTransform>());
        var centerLayout = centerText.AddComponent<VerticalLayoutGroup>();
        centerLayout.childAlignment = TextAnchor.MiddleCenter;
        centerLayout.childControlHeight = false;
        centerLayout.childForceExpandWidth = true;

        valueText = CreateText("ValueText", centerText.transform, 50, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
        valueText.text = "0";
        var valLE = valueText.gameObject.AddComponent<LayoutElement>();
        valLE.preferredHeight = 58;

        var capText = CreateText("Caption", centerText.transform, 15, TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
        capText.text = caption;
        capText.characterSpacing = 2;
        var capLE = capText.gameObject.AddComponent<LayoutElement>();
        capLE.preferredHeight = 20;
    }

    // แถวค่าย่อยแบบ minimal (label เล็ก + value ใหญ่ อยู่บรรทัดเดียวกัน) -
    // ใช้ในการ์ดลอย compact เท่านั้น (ไม่มี icon badge ตาม "sparse icon use")
    TextMeshProUGUI AddCompactStat(Transform parent, string goName, string label)
    {
        var row = CreateUIObject(goName, parent);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 46;

        var labelText = CreateText("Label", row.transform, 19, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Left);
        labelText.text = label;
        var lle = labelText.gameObject.AddComponent<LayoutElement>();
        lle.preferredWidth = 118;

        var valueText = CreateText("Value", row.transform, 32, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Right);
        valueText.text = "-";
        var vle = valueText.gameObject.AddComponent<LayoutElement>();
        vle.flexibleWidth = 1;

        return valueText;
    }

    // เส้น+จุดเรืองแสง ชี้จาก InspectionDot (จุดบนผิวรถ) ออกไปยัง
    // ClusterAnchor (กลุ่มโมเดล 3D + การ์ด ที่ลอยอยู่นอกตัวรถ) - outSign:
    // -1 = กลุ่มลอยไปทางซ้าย (Battery), +1 = กลุ่มลอยไปทางขวา (Motor)
    void AddWorldConnector(Transform anchor, Color accent, float outSign, string tagCaption, out TextMeshProUGUI tagValueText)
    {
        var connector = CreateUIObject("Connector", anchor);
        var connRect = connector.GetComponent<RectTransform>();
        connRect.anchorMin = connRect.anchorMax = connRect.pivot = new Vector2(0.5f, 0.5f);
        connRect.sizeDelta = Vector2.zero;
        connRect.anchoredPosition = Vector2.zero;

        // เส้นชี้จาก "จุดบนผิวรถ" (InspectionDot) ออกไปยังกลุ่มโมเดล 3D/การ์ด
        // ที่ลอยอยู่นอกตัวรถ (ClusterAnchor) - ดูคอมเมนต์ยาวที่นิยามทั้งสอง
        // helper นี้ด้านบน (เหนือ BuildWorldSpaceUI)
        Vector2 from = InspectionDot;
        Vector2 to = ClusterAnchor(outSign);

        // ปิดเส้น Line เชื่อม (เอาออกชั่วคราวตามที่ขอ) - เปิดกลับมาแค่ uncomment บล็อกนี้
        // var line = CreateUIObject("Line", connector.transform);
        // var lineRect = line.GetComponent<RectTransform>();
        // lineRect.anchorMin = lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        // Vector2 delta = to - from;
        // float len = delta.magnitude;
        // float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        // lineRect.pivot = new Vector2(0f, 0.5f);
        // lineRect.sizeDelta = new Vector2(len, 2f);
        // lineRect.anchoredPosition = from;
        // lineRect.localRotation = Quaternion.Euler(0, 0, ang);
        // var lineImg = line.AddComponent<Image>();
        // lineImg.color = new Color(accent.r, accent.g, accent.b, 0.55f);
        // lineImg.raycastTarget = false;

        var dot = CreateUIObject("Dot", connector.transform);
        var dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = new Vector2(12, 12);
        dotRect.anchoredPosition = to;
        var dotImg = dot.AddComponent<Image>();
        dotImg.sprite = UIRoundedRect.GetSprite(6, 12);
        dotImg.color = accent;
        dotImg.raycastTarget = false;

        // ป้าย pill ปลายจุด - โชว์ชื่อ + ค่าสดจริงลอยข้างกลุ่มโมเดล 3D (ไม่ใช่
        // แค่จุดเฉยๆ ตามสเปก "ทำค่าที่ชี้ให้หน่อย") - กลุ่มฝั่งซ้าย (outSign=-1)
        // ป้ายกางออกทางซ้ายต่อ, กลุ่มฝั่งขวา (outSign=+1) กางออกทางขวาต่อ กัน
        // ป้ายทะลุขอบจอ
        var pill = CreateUIObject("Pill", connector.transform);
        var pillRect = pill.GetComponent<RectTransform>();
        pillRect.anchorMin = pillRect.anchorMax = new Vector2(0.5f, 0.5f);
        pillRect.pivot = new Vector2(outSign >= 0 ? 1f : 0f, 0.5f);
        pillRect.sizeDelta = new Vector2(150, 56);
        pillRect.anchoredPosition = to + new Vector2(outSign * 20, 0);

        var pillImg = pill.AddComponent<Image>();
        pillImg.color = new Color(0.02f, 0.035f, 0.06f, 0.85f);
        pillImg.sprite = UIRoundedRect.GetSprite(14, 28);
        pillImg.type = Image.Type.Sliced;
        pillImg.raycastTarget = false;

        var pillStrokeGO = CreateUIObject("Stroke", pill.transform);
        StretchFull(pillStrokeGO.GetComponent<RectTransform>());
        var pillStrokeImg = pillStrokeGO.AddComponent<Image>();
        pillStrokeImg.sprite = GlowRoundedRect.GetStroke(14, 2, 128);
        pillStrokeImg.color = new Color(accent.r, accent.g, accent.b, 0.7f);
        pillStrokeImg.type = Image.Type.Sliced;
        pillStrokeImg.raycastTarget = false;

        var pillLayout = pill.AddComponent<VerticalLayoutGroup>();
        pillLayout.padding = new RectOffset(14, 14, 7, 7);
        pillLayout.spacing = 1;
        pillLayout.childAlignment = TextAnchor.MiddleCenter;
        pillLayout.childControlWidth = true; pillLayout.childForceExpandWidth = true;
        pillLayout.childControlHeight = true; pillLayout.childForceExpandHeight = false;

        var capText = CreateText("Caption", pill.transform, 11, accent, FontStyles.Bold, TextAlignmentOptions.Center);
        capText.text = tagCaption;
        capText.characterSpacing = 1;
        var capLE = capText.gameObject.AddComponent<LayoutElement>();
        capLE.preferredHeight = 16;

        tagValueText = CreateText("Value", pill.transform, 22, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
        tagValueText.text = "--";
        var valLE = tagValueText.gameObject.AddComponent<LayoutElement>();
        valLE.preferredHeight = 26;
    }

    // ==========================================
    // Screen Space UI: TopBar / TrackingMessage / BottomControls / DetailOverlay
    // ==========================================
    void BuildScreenSpaceUI()
    {
        var ssuGO = new GameObject("ScreenSpace_UI");
        ssuGO.transform.SetParent(transform, false);
        screenSpaceUI = ssuGO.transform;

        var canvas = ssuGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = ssuGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasUnitsWide, CanvasUnitsHigh);
        // match ด้วยความสูงอย่างเดียว (1f) ให้สเกลตรงกับ WorldSpace_UI_Battery/Motor
        // ที่คำนวณสเกลจาก vertical FOV ล้วนๆ ใน EVDataCardUI.LateUpdate() -
        // ของเดิม (0.5f = blend กว้าง+สูง) ทำให้บนจอ landscape (กว้างกว่าอัตรา
        // ส่วน reference 1080x1920 มาก) ScreenSpace_UI ดูใหญ่กว่า WorldSpace
        // อย่างชัดเจน (เอาที่ขอ "ScreenSpace_UI มันใหญ่จังอ่ะ")
        scaler.matchWidthOrHeight = 1f;
        ssuGO.AddComponent<GraphicRaycaster>();

        BuildTopBar(ssuGO.transform);
        BuildTrackingMessage(ssuGO.transform);
        BuildBottomControls(ssuGO.transform);
        BuildDetailOverlay(ssuGO.transform);

        // ผูก onClick ให้ Scrim/CloseBtn/BackBtn (ปิด detail overlay) - ใช้
        // ฟังก์ชันเดียวกับตอน rebind ของเดิม กันลืมผูกตอน build ครั้งแรก
        RewireCloseButtons(ssuGO.transform.Find("DetailOverlay"));
    }

    void BuildTopBar(Transform parent)
    {
        var topBar = CreateUIObject("TopBar", parent);
        var rect = topBar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0, 130);
        rect.anchoredPosition = Vector2.zero;

        var bg = topBar.AddComponent<Image>();
        bg.color = TopBarBg;

        var h = topBar.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(20, 20, 46, 14);
        h.spacing = 12;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        // ลบ BackBtn ออกตามที่ขอ (ไม่เอาแล้ว) - RewireCloseButtons() หา
        // "TopBar/BackBtn" ไม่เจอก็แค่ no-op เฉยๆ (WireClick มี null-guard)
        var titleBlock = CreateUIObject("TitleBlock", topBar.transform);
        var tbLE = titleBlock.AddComponent<LayoutElement>();
        tbLE.flexibleWidth = 1;
        var tbLayout = titleBlock.AddComponent<VerticalLayoutGroup>();
        tbLayout.childAlignment = TextAnchor.MiddleLeft;
        tbLayout.childControlHeight = false;
        tbLayout.childForceExpandWidth = true;
        tbLayout.spacing = 2;

        var arMonitor = CreateText("ARMonitor", titleBlock.transform, 15, TextSecondary, FontStyles.Bold, TextAlignmentOptions.Left);
        arMonitor.text = "AR MONITOR";
        arMonitor.characterSpacing = 3;
        var amLE = arMonitor.gameObject.AddComponent<LayoutElement>();
        amLE.preferredHeight = 20;

        vehicleIdText = CreateText("VehicleIdText", titleBlock.transform, 30, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        vehicleIdText.text = "EV001";
        var vidLE = vehicleIdText.gameObject.AddComponent<LayoutElement>();
        vidLE.preferredHeight = 38;

        var badge = CreateUIObject("StatusBadge", topBar.transform);
        var badgeLE = badge.AddComponent<LayoutElement>();
        badgeLE.preferredWidth = 136; badgeLE.preferredHeight = 44;
        statusBadgeBg = badge.AddComponent<Image>();
        statusBadgeBg.color = new Color(GreenPrimary.r, GreenPrimary.g, GreenPrimary.b, 0.16f);
        statusBadgeBg.sprite = UIRoundedRect.GetSprite(20, 44);
        statusBadgeBg.type = Image.Type.Sliced;
        statusBadgeText = CreateText("Text", badge.transform, 16, GreenPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
        statusBadgeText.text = "NORMAL";
        StretchFull(statusBadgeText.rectTransform);

        var liveGroup = CreateUIObject("LiveGroup", topBar.transform);
        var lg = liveGroup.AddComponent<HorizontalLayoutGroup>();
        lg.spacing = 5; lg.childAlignment = TextAnchor.MiddleCenter;
        lg.childControlWidth = true; lg.childControlHeight = true;
        lg.childForceExpandWidth = false; lg.childForceExpandHeight = false;
        var lgLE = liveGroup.AddComponent<LayoutElement>();
        lgLE.preferredWidth = 56; lgLE.preferredHeight = 20;

        var dotGO = CreateUIObject("LiveDot", liveGroup.transform);
        var dotLE = dotGO.AddComponent<LayoutElement>();
        dotLE.preferredWidth = 8; dotLE.preferredHeight = 8;
        liveDot = dotGO.AddComponent<Image>();
        liveDot.sprite = UIRoundedRect.GetSprite(4, 8);
        liveDot.color = GreenPrimary; // เริ่มต้นสีเขียว - LiveDotRoutine() จะกระพริบอัลฟ่าเองใน Update

        var liveTxt = CreateText("LiveText", liveGroup.transform, 11, TextSecondary, FontStyles.Bold, TextAlignmentOptions.Left);
        liveTxt.text = "LIVE";
        liveTxt.characterSpacing = 2;
        var ltLE = liveTxt.gameObject.AddComponent<LayoutElement>();
        ltLE.preferredWidth = 40; ltLE.preferredHeight = 20;
    }

    void BuildTrackingMessage(Transform parent)
    {
        var msg = CreateUIObject("TrackingMessage", parent);
        var rect = msg.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(320, 48);
        rect.anchoredPosition = new Vector2(0, -150);

        var bg = msg.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.05f);
        bg.sprite = UIRoundedRect.GetSprite(22, 48);
        bg.type = Image.Type.Sliced;

        var txt = CreateText("Text", msg.transform, 15, TextMuted, FontStyles.Bold, TextAlignmentOptions.Center);
        txt.text = "AR HUD ACTIVE";
        txt.characterSpacing = 2;
        StretchFull(txt.rectTransform);
        trackingMessageText = txt;
    }

    void BuildBottomControls(Transform parent)
    {
        var bar = CreateUIObject("BottomControls", parent);
        var rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        // สำคัญ: BottomControls ยึด anchor แบบจุดเดียว (ไม่ stretch แนวนอน)
        // จึงต้องกำหนดความกว้างจริงผ่าน sizeDelta.x ตรงๆ (เดิมเคยเผลอใส่ 0
        // แล้วหวังพึ่ง LayoutElement.preferredWidth ด้านล่างแทน แต่
        // LayoutElement มีผลก็ต่อเมื่ออยู่ใต้ parent ที่มี LayoutGroup เท่านั้น -
        // ScreenSpace_UI ไม่มี LayoutGroup เลยทำให้แถบล่างกว้าง 0 มองไม่เห็นเลย)
        rect.sizeDelta = new Vector2(440, 66);
        rect.anchoredPosition = new Vector2(0, 34);

        var bg = bar.AddComponent<Image>();
        bg.color = TopBarBg;
        bg.sprite = UIRoundedRect.GetSprite(30, 60);
        bg.type = Image.Type.Sliced;

        var le = bar.AddComponent<LayoutElement>();
        le.preferredWidth = 440;

        var h = bar.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(8, 8, 8, 8);
        h.spacing = 6;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childForceExpandWidth = true;
        h.childControlHeight = true;

        // 3 ปุ่มเท่านั้นตามสเปกใหม่: AR / DATA / GRAPH (ไม่มี Battery/Motor
        // ในแถบล่างอีกต่อไป - เปิด detail ของ Battery/Motor ได้จากการแตะ
        // การ์ดลอย 2D หรือจุด Connector บนตัวรถแทน)
        btnOverviewBg = AddBottomButton(bar.transform, "BtnOverview", "AR", () => OpenDetail(DetailMode.None));
        btnDataBg = AddBottomButton(bar.transform, "BtnData", "DATA", () => OpenDetail(DetailMode.Data));
        btnGraphBg = AddBottomButton(bar.transform, "BtnGraph", "GRAPH", () => OpenDetail(DetailMode.Graph));
        SetBtnActive(btnOverviewBg, true);
    }

    Image AddBottomButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        var btn = CreateUIObject(name, parent);
        var img = btn.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.04f);
        img.sprite = UIRoundedRect.GetSprite(18, 44);
        img.type = Image.Type.Sliced;
        var button = btn.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(onClick);

        var txt = CreateText("Label", btn.transform, 11, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
        txt.text = label;
        StretchFull(txt.rectTransform);
        return img;
    }

    void BuildDetailOverlay(Transform parent)
    {
        var overlay = CreateUIObject("DetailOverlay", parent);
        StretchFull(overlay.GetComponent<RectTransform>());
        detailOverlay = overlay;

        var scrim = CreateUIObject("Scrim", overlay.transform);
        StretchFull(scrim.GetComponent<RectTransform>());
        var scrimImg = scrim.AddComponent<Image>();
        scrimImg.color = GlassBgSolid;
        scrim.AddComponent<Button>().transition = Selectable.Transition.None;

        var card = CreateUIObject("Card", overlay.transform);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(620, 0);
        detailCardRect = cardRect;

        var glowGO = CreateUIObject("Glow", card.transform);
        var glowRect = glowGO.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero; glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-20, -20); glowRect.offsetMax = new Vector2(20, 20);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.sprite = GlowRoundedRect.GetGlow(128);
        glowImg.color = new Color(CyanPrimary.r, CyanPrimary.g, CyanPrimary.b, 0.18f);
        glowImg.raycastTarget = false;
        glowGO.AddComponent<LayoutElement>().ignoreLayout = true;

        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.03f, 0.045f, 0.075f, 0.94f);
        cardImg.sprite = UIRoundedRect.GetSprite(28, 56);
        cardImg.type = Image.Type.Sliced;

        var strokeGO = CreateUIObject("Stroke", card.transform);
        StretchFull(strokeGO.GetComponent<RectTransform>());
        detailAccentStroke = strokeGO.AddComponent<Image>();
        detailAccentStroke.sprite = GlowRoundedRect.GetStroke(28, 2, 128);
        detailAccentStroke.color = CyanPrimary;
        detailAccentStroke.type = Image.Type.Sliced;
        detailAccentStroke.raycastTarget = false;
        strokeGO.AddComponent<LayoutElement>().ignoreLayout = true;

        var fitter = card.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vLayout = card.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(26, 26, 22, 26);
        vLayout.spacing = 16;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandHeight = false;

        var header = CreateUIObject("Header", card.transform);
        var hLayout = header.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = true; hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = false; hLayout.childForceExpandHeight = false;
        var headerLE = header.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 34;

        detailTitleText = CreateText("Title", header.transform, 20, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        detailTitleText.text = "BATTERY DETAIL";
        detailTitleText.characterSpacing = 2;
        var titleLE = detailTitleText.gameObject.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1; titleLE.preferredHeight = 34;

        var closeBtn = CreateUIObject("CloseBtn", header.transform);
        var closeLE = closeBtn.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 30; closeLE.preferredHeight = 30;
        var closeImg = closeBtn.AddComponent<Image>();
        closeImg.color = new Color(1f, 1f, 1f, 0.06f);
        closeImg.sprite = UIRoundedRect.GetSprite(15, 30);
        closeImg.type = Image.Type.Sliced;
        var closeTxt = CreateText("X", closeBtn.transform, 14, TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);
        closeTxt.text = "✕";
        StretchFull(closeTxt.rectTransform);
        closeBtn.AddComponent<Button>().transition = Selectable.Transition.None;

        var iconGlowGO = CreateUIObject("IconGlow", card.transform);
        var iglLE = iconGlowGO.AddComponent<LayoutElement>();
        iglLE.preferredWidth = 130; iglLE.preferredHeight = 130;
        var iglRow = iconGlowGO.AddComponent<HorizontalLayoutGroup>();
        iglRow.childAlignment = TextAnchor.MiddleCenter;
        detailIconGlow = iconGlowGO.AddComponent<Image>();
        detailIconGlow.sprite = GaugeSprite.GetRing(0.62f, 160);
        detailIconGlow.color = new Color(CyanPrimary.r, CyanPrimary.g, CyanPrimary.b, 0.35f);

        var rows = CreateUIObject("Rows", card.transform);
        var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 20;
        rowsLayout.childControlHeight = false;
        rowsLayout.childForceExpandWidth = true;

        for (int i = 0; i < DetailRowCount; i++)
        {
            // แถวแบบ "label บรรทัดบน (เล็ก สีรอง) + value บรรทัดล่าง (ใหญ่
            // ตัวหนา สีหลัก)" แทนแบบเดิมที่วาง label/value ข้างกันในแถวเดียว
            // ด้วยความกว้าง value คงที่ 160 หน่วย - แบบเดิมพอ label ยาว (เช่น
            // "INTERNAL RESISTANCE") บนจอจริงจะชนกับ value ได้เพราะอิงค่า
            // pixel ตายตัว ตอนนี้ใช้ VerticalLayoutGroup + childForceExpandWidth
            // แทน ทั้ง 2 บรรทัดเลยกว้างเต็มแถวเสมอ ไม่มีทางชนกันไม่ว่าความกว้าง
            // จอจะเท่าไหร่ก็ตาม
            var row = CreateUIObject($"Row{i}", rows.transform);
            var rowV = row.AddComponent<VerticalLayoutGroup>();
            rowV.childAlignment = TextAnchor.UpperLeft;
            rowV.spacing = 4;
            rowV.childControlWidth = true; rowV.childForceExpandWidth = true;
            rowV.childControlHeight = true; rowV.childForceExpandHeight = false;
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 76;

            var label = CreateText("Label", row.transform, 19, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Left);
            label.characterSpacing = 1;
            var lLE = label.gameObject.AddComponent<LayoutElement>();
            lLE.preferredHeight = 24;

            var value = CreateText("Value", row.transform, 34, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
            var vLE = value.gameObject.AddComponent<LayoutElement>();
            vLE.preferredHeight = 42;

            detailRows[i] = row;
            detailLabels[i] = label;
            detailValues[i] = value;
        }

        BuildGraphArea(card.transform);

        overlay.SetActive(false);
    }

    // ==========================================
    // Graph tab: เลือก metric ได้ (chip 5 ปุ่ม) + วาดกราฟเส้นด้วย Image
    // สี่เหลี่ยมบางๆ หมุนต่อกันเป็นทอน (เทคนิคเดียวกับ AddWorldConnector)
    // เพราะ Unity UI ไม่มี LineRenderer สำหรับ Canvas โดยตรง - ตัว logic
    // เลือก metric/normalize ค่า อยู่ใน EVDataCardUI.cs (RefreshGraph/
    // SetGraphMetric) ส่วนนี้มีแค่ construction/draw เท่านั้น
    // ==========================================
    void BuildGraphArea(Transform cardParent)
    {
        graphArea = new GameObject("GraphArea", typeof(RectTransform));
        graphArea.transform.SetParent(cardParent, false);
        var vLayout = graphArea.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 14;
        vLayout.childControlWidth = true; vLayout.childForceExpandWidth = true;
        vLayout.childControlHeight = true; vLayout.childForceExpandHeight = false;

        var chipRow = CreateUIObject("ChipRow", graphArea.transform);
        var chipH = chipRow.AddComponent<HorizontalLayoutGroup>();
        chipH.spacing = 6;
        chipH.childControlWidth = true; chipH.childForceExpandWidth = true;
        chipH.childControlHeight = true;
        var chipRowLE = chipRow.AddComponent<LayoutElement>();
        chipRowLE.preferredHeight = 44;

        string[] chipLabels = { "SOC", "VOLT", "TEMP", "RPM", "POWER" };
        for (int i = 0; i < chipLabels.Length; i++)
        {
            int idx = i;
            var chip = CreateUIObject($"Chip{i}", chipRow.transform);
            var chipImg = chip.AddComponent<Image>();
            chipImg.color = (i == 0)
                ? new Color(CyanPrimary.r, CyanPrimary.g, CyanPrimary.b, 0.28f)
                : new Color(1f, 1f, 1f, 0.05f);
            chipImg.sprite = UIRoundedRect.GetSprite(12, 24);
            chipImg.type = Image.Type.Sliced;
            var chipBtn = chip.AddComponent<Button>();
            chipBtn.transition = Selectable.Transition.None;
            chipBtn.onClick.AddListener(() => SetGraphMetric(idx));
            var chipTxt = CreateText("Label", chip.transform, 12, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
            chipTxt.text = chipLabels[i];
            StretchFull(chipTxt.rectTransform);
            graphChipBg[i] = chipImg;
        }

        graphValueText = CreateText("GraphValue", graphArea.transform, 40, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        graphValueText.text = "--";
        var gvLE = graphValueText.gameObject.AddComponent<LayoutElement>();
        gvLE.preferredHeight = 50;

        var plotFrame = CreateUIObject("PlotFrame", graphArea.transform);
        var plotImg = plotFrame.AddComponent<Image>();
        plotImg.color = new Color(1f, 1f, 1f, 0.04f);
        plotImg.sprite = UIRoundedRect.GetSprite(14, 28);
        plotImg.type = Image.Type.Sliced;
        var plotLE = plotFrame.AddComponent<LayoutElement>();
        plotLE.preferredHeight = 260;
        graphPlotArea = plotFrame.GetComponent<RectTransform>();

        var lineParentGO = CreateUIObject("Line", plotFrame.transform);
        StretchFull(lineParentGO.GetComponent<RectTransform>());
        graphLineParent = lineParentGO.transform;
    }

    void DrawLineGraph(Transform lineParent, System.Collections.Generic.List<float> normalizedValues, Color color, Vector2 plotSize)
    {
        ClearGraphLine(lineParent);
        if (normalizedValues == null || normalizedValues.Count < 2) return;

        int count = normalizedValues.Count;
        var points = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float x = (i / (float)(count - 1)) * plotSize.x - plotSize.x * 0.5f;
            float y = normalizedValues[i] * plotSize.y - plotSize.y * 0.5f;
            points[i] = new Vector2(x, y);
        }

        for (int i = 0; i < count - 1; i++)
        {
            var segGO = CreateUIObject($"Seg{i}", lineParent);
            var segRect = segGO.GetComponent<RectTransform>();
            segRect.anchorMin = segRect.anchorMax = new Vector2(0.5f, 0.5f);
            segRect.pivot = new Vector2(0f, 0.5f);
            Vector2 delta = points[i + 1] - points[i];
            float len = delta.magnitude;
            float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            segRect.sizeDelta = new Vector2(len, 3f);
            segRect.anchoredPosition = points[i];
            segRect.localRotation = Quaternion.Euler(0, 0, ang);
            var segImg = segGO.AddComponent<Image>();
            segImg.color = color;
            segImg.raycastTarget = false;
        }

        var dotGO = CreateUIObject("LastDot", lineParent);
        var dotRect = dotGO.GetComponent<RectTransform>();
        dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = new Vector2(10, 10);
        dotRect.anchoredPosition = points[count - 1];
        var dotImg = dotGO.AddComponent<Image>();
        dotImg.sprite = UIRoundedRect.GetSprite(5, 10);
        dotImg.color = color;
        dotImg.raycastTarget = false;
    }

    // ล้างเส้นกราฟเก่าก่อนวาดใหม่ทุกครั้ง - ใช้ Destroy() (ไม่ใช่
    // DestroyImmediate) เพราะเมธอดนี้ถูกเรียกซ้ำบ่อยตอน runtime จริง (ทุกครั้ง
    // ที่มีข้อมูลใหม่เข้ามาระหว่างเปิด tab Graph อยู่) DestroyImmediate ควร
    // ใช้เฉพาะตอน Editor-time/Start() ครั้งเดียวเท่านั้น (ดู
    // RemoveLegacyCombinedWorldUI ด้านบน)
    void ClearGraphLine(Transform lineParent)
    {
        if (lineParent == null) return;
        for (int i = lineParent.childCount - 1; i >= 0; i--)
            Destroy(lineParent.GetChild(i).gameObject);
    }

    // ==========================================
    // primitive helpers
    // ==========================================
    static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = CreateUIObject(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.fontSize = size;
        t.enableAutoSizing = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
