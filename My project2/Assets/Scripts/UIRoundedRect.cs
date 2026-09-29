using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สร้าง Sprite สี่เหลี่ยมมุมโค้ง (rounded rect) ด้วยโค้ดล้วนๆ ตอนรันไทม์
/// ใช้กับ UI.Image (type = Sliced) แทนการต้องเตรียมไฟล์ภาพ .png มุมโค้งเอง
/// - ใช้กับการ์ด AR overlay แบบ minimal ใน EVDataCardUI.cs
/// </summary>
public static class UIRoundedRect
{
    static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

    public static Sprite GetSprite(int radius, int size = 64)
    {
        if (Cache.TryGetValue(radius, out var cached) && cached != null)
        {
            return cached;
        }

        int r = Mathf.Clamp(radius, 1, size / 2);
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 1f;

                // สี่มุม: เช็คระยะห่างจากจุดศูนย์กลางวงกลมมุมนั้นๆ
                Vector2Int corner = new Vector2Int(-1, -1);
                if (x < r && y < r) corner = new Vector2Int(r, r);
                else if (x >= size - r && y < r) corner = new Vector2Int(size - r, r);
                else if (x < r && y >= size - r) corner = new Vector2Int(r, size - r);
                else if (x >= size - r && y >= size - r) corner = new Vector2Int(size - r, size - r);

                if (corner.x >= 0)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), corner);
                    alpha = dist <= r ? 1f : 0f;
                    // anti-alias บางๆ ตรงขอบ
                    if (dist > r - 1f && dist <= r + 1f)
                    {
                        alpha = Mathf.Clamp01(r + 1f - dist) * 0.5f + (dist <= r ? 0.5f : 0f);
                    }
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(r, r, r, r)
        );

        Cache[radius] = sprite;
        return sprite;
    }
}
