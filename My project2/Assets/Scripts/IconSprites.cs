using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สร้างไอคอนเล็กๆ (bolt/thermometer/gauge/car) แบบ procedural ด้วยโค้ด
/// ล้วนๆ (วาดใส่ Texture2D ทีละพิกเซล) เหมือนกับ UIRoundedRect - เพราะไม่มี
/// ไฟล์รูปไอคอนให้ใช้ ใช้ทำ badge สีเขียวอ่อนใน EVDataCardUI ให้การ์ดดู
/// มีมิติ ไม่ใช่แค่ตัวหนังสือล้วนๆ
/// </summary>
public static class IconSprites
{
    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite GetIcon(string kind, int size = 64)
    {
        string key = kind + "_" + size;
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float v = (y + 0.5f) / size;
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float a = SampleIcon(kind, u, v);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = sprite;
        return sprite;
    }

    static float SampleIcon(string kind, float u, float v)
    {
        switch (kind)
        {
            case "bolt": return Bolt(u, v);
            case "thermometer": return Thermometer(u, v);
            case "gauge": return Gauge(u, v);
            case "car": return Car(u, v);
            case "battery": return Battery(u, v);
            default: return 0f;
        }
    }

    static float Bolt(float u, float v)
    {
        Vector2[] poly =
        {
            new Vector2(0.58f, 0.98f),
            new Vector2(0.22f, 0.42f),
            new Vector2(0.46f, 0.42f),
            new Vector2(0.40f, 0.02f),
            new Vector2(0.80f, 0.58f),
            new Vector2(0.54f, 0.58f),
        };
        return PointInPoly(poly, u, v) ? 1f : 0f;
    }

    static float Thermometer(float u, float v)
    {
        float stem = RectAA(u, v, 0.42f, 0.34f, 0.58f, 0.80f);
        float bulb = CircleAA(u, v, 0.5f, 0.26f, 0.17f);
        return Mathf.Max(stem, bulb);
    }

    static float Gauge(float u, float v)
    {
        Vector2 c = new Vector2(0.5f, 0.40f);
        float d = Vector2.Distance(new Vector2(u, v), c);
        float arc = (v >= c.y - 0.02f) ? RingAA(d, 0.28f, 0.40f) : 0f;
        Vector2 tip = c + new Vector2(0.22f, 0.20f);
        float needle = SegmentAA(u, v, c, tip, 0.035f);
        float hub = CircleAA(u, v, c.x, c.y, 0.05f);
        return Mathf.Max(arc, Mathf.Max(needle, hub));
    }

    static float Car(float u, float v)
    {
        float body = RectAA(u, v, 0.08f, 0.36f, 0.92f, 0.58f);
        float cabin = RectAA(u, v, 0.27f, 0.54f, 0.73f, 0.74f);
        float wheel1 = CircleAA(u, v, 0.27f, 0.30f, 0.11f);
        float wheel2 = CircleAA(u, v, 0.73f, 0.30f, 0.11f);
        return Mathf.Max(Mathf.Max(body, cabin), Mathf.Max(wheel1, wheel2));
    }

    // ไอคอนแบตเตอรี่: ตัวถังสี่เหลี่ยม + ขั้วเล็กด้านบน แล้วเจาะรูปสายฟ้า
    // ตรงกลางเป็น "รู" (alpha=0) ให้เห็นพื้นหลัง badge ทะลุผ่าน - ได้ลุค
    // แบตกำลังชาร์จแบบ 2 โทนสีโดยไม่ต้องวาดหลายเลเยอร์
    static float Battery(float u, float v)
    {
        float body = RectAA(u, v, 0.26f, 0.08f, 0.74f, 0.88f);
        float nub = RectAA(u, v, 0.42f, 0.88f, 0.58f, 0.97f);
        float shape = Mathf.Max(body, nub);

        Vector2[] boltPoly =
        {
            new Vector2(0.58f, 0.80f),
            new Vector2(0.42f, 0.52f),
            new Vector2(0.51f, 0.52f),
            new Vector2(0.46f, 0.18f),
            new Vector2(0.63f, 0.48f),
            new Vector2(0.53f, 0.48f),
        };
        bool boltHole = PointInPoly(boltPoly, u, v);
        return boltHole ? 0f : shape;
    }

    // ---------- primitive helpers (soft-edge ~1px AA) ----------
    const float AA = 0.014f;

    static float CircleAA(float u, float v, float cx, float cy, float r)
    {
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(cx, cy));
        return 1f - Mathf.Clamp01((d - r) / AA);
    }

    static float RingAA(float d, float rInner, float rOuter)
    {
        float outerA = 1f - Mathf.Clamp01((d - rOuter) / AA);
        float innerA = 1f - Mathf.Clamp01((rInner - d) / AA);
        return Mathf.Min(outerA, innerA);
    }

    static float RectAA(float u, float v, float x0, float y0, float x1, float y1)
    {
        float ux = Mathf.Max(x0 - u, u - x1, 0f);
        float uy = Mathf.Max(y0 - v, v - y1, 0f);
        float outsideDist = Mathf.Sqrt(ux * ux + uy * uy);
        return 1f - Mathf.Clamp01(outsideDist / AA);
    }

    static float SegmentAA(float u, float v, Vector2 a, Vector2 b, float thickness)
    {
        Vector2 p = new Vector2(u, v);
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        Vector2 proj = a + ab * t;
        float d = Vector2.Distance(p, proj);
        return 1f - Mathf.Clamp01((d - thickness) / AA);
    }

    static bool PointInPoly(Vector2[] poly, float u, float v)
    {
        bool inside = false;
        int n = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 pi = poly[i], pj = poly[j];
            if (((pi.y > v) != (pj.y > v)) &&
                (u < (pj.x - pi.x) * (v - pi.y) / (pj.y - pi.y) + pi.x))
            {
                inside = !inside;
            }
        }
        return inside;
    }
}
