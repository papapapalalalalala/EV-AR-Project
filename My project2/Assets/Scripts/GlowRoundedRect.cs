using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สร้าง sprite "กรอบเส้นเรืองแสง" (stroke-only rounded rect, ตรงกลางโปร่งใส)
/// และ sprite "แสงฟุ้งนุ่มๆ" (soft radial glow blob) แบบ procedural ล้วนๆ
/// เหมือน UIRoundedRect/GaugeSprite/IconSprites - ไม่ต้องมีไฟล์ภาพ ไม่ต้อง
/// เขียน shader หรือเปิด Post Processing Bloom (โปรเจกต์นี้ยังไม่ได้ตั้งค่า
/// URP Bloom ไว้) ก็ได้ลุค "เรืองแสง" ของธีม dark-glassmorphism
///
/// วิธีใช้ร่วมกัน (ดู EVDataCardUI.cs > BuildGlassPanel):
///   1) Glow  : sprite ฟุ้งใหญ่ วางเป็นชั้นล่างสุด สีเดียวกับขอบ อัลฟ่าต่ำๆ
///   2) Fill  : พื้นการ์ดสีเข้มโปร่งแสง (ใช้ UIRoundedRect.GetSprite ปกติ)
///   3) Stroke: เส้นขอบบางๆ สีสว่าง วางบนสุด
/// ซ้อน 3 ชั้นนี้แล้วตาจะเห็นเป็นกรอบเรืองแสงรอบกระจกโปร่งแสง
/// </summary>
public static class GlowRoundedRect
{
    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    const float AA = 1.4f;

    public static Sprite GetStroke(int radius, int thickness, int size = 128)
    {
        string key = $"stroke_{radius}_{thickness}_{size}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = NewTex(size);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = SdfRoundedBox(x + 0.5f, y + 0.5f, size, radius);
                float outerA = 1f - Mathf.Clamp01(d / AA);
                float innerA = Mathf.Clamp01((d + thickness) / AA);
                px[y * size + x] = new Color(1f, 1f, 1f, outerA * innerA);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        Cache[key] = sprite;
        return sprite;
    }

    public static Sprite GetGlow(int size = 128)
    {
        string key = $"glow_{size}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = NewTex(size);
        var px = new Color32[size * size];
        var center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxR;
                float a = Mathf.Pow(1f - Mathf.Clamp01(d), 2.2f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = sprite;
        return sprite;
    }

    static Texture2D NewTex(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    // signed distance จากพิกเซล (px,py) ถึงขอบสี่เหลี่ยมมุมโค้งขนาด size x
    // size รัศมีมุม radius (บวก = อยู่นอกกรอบ, ลบ = อยู่ในกรอบ) - สูตร SDF
    // rounded-box มาตรฐาน (Inigo Quilez)
    static float SdfRoundedBox(float px, float py, float size, float radius)
    {
        float half = size * 0.5f;
        float qx = Mathf.Abs(px - half) - (half - radius);
        float qy = Mathf.Abs(py - half) - (half - radius);
        float outsideX = Mathf.Max(qx, 0f);
        float outsideY = Mathf.Max(qy, 0f);
        float outsideDist = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
        float insideDist = Mathf.Min(Mathf.Max(qx, qy), 0f);
        return outsideDist + insideDist - radius;
    }
}
