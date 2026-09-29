using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สร้าง sprite วงแหวน (ring/donut) แบบ procedural สำหรับทำเกจวงกลม SOC -
/// ใช้คู่กับ Image.Type.Filled (Radial360) ของ Unity UI เอง (ไม่ต้องเขียน
/// shader เพิ่ม) แค่เปลี่ยน fillAmount ก็ได้เกจเคลื่อนไหวแบบเรียลไทม์แล้ว
/// </summary>
public static class GaugeSprite
{
    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite GetRing(float innerRatio, int size = 160)
    {
        string key = "ring_" + innerRatio.ToString("0.000") + "_" + size;
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float outerR = size * 0.5f - 1.5f;
        float innerR = outerR * innerRatio;
        var center = new Vector2(size * 0.5f, size * 0.5f);

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float outerA = 1f - Mathf.Clamp01((d - outerR) / 1.5f);
                float innerA = 1f - Mathf.Clamp01((innerR - d) / 1.5f);
                float a = Mathf.Min(outerA, innerA);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = sprite;
        return sprite;
    }
}
