using System.Collections.Generic;
using UnityEngine;

namespace MyCompany.VectorEditor.Core
{
    public static class DrawingUtils
    {
        // Safe pixel set with simple alpha blend
        public static void SetPixelSafe(Texture2D tex, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) return;
            Color dst = tex.GetPixel(x, y);
            float a = c.a + dst.a * (1f - c.a);
            if (a < 1e-6f) { tex.SetPixel(x, y, Color.clear); return; }
            Color outCol = (c * c.a + dst * dst.a * (1f - c.a)) / a;
            outCol.a = a;
            tex.SetPixel(x, y, outCol);
        }

        // Draw filled disk (used for stroking)
        public static void DrawDisk(Texture2D tex, Vector2 center, float radius, Color col)
        {
            int r = Mathf.CeilToInt(radius);
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            int rr = r * r;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= rr)
                        SetPixelSafe(tex, cx + x, cy + y, col);
                }
            }
        }

        // Draw thick line by stamping disks along line
        public static void DrawThickLine(Texture2D tex, Vector2 p1, Vector2 p2, Color col, float width)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p1, p2)));
            float r = Mathf.Max(0.5f, width * 0.5f);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = Vector2.Lerp(p1, p2, t);
                DrawDisk(tex, p, r, col);
            }
        }

        // Fill rectangle
        public static void DrawFilledRect(Texture2D tex, Vector2 a, Vector2 b, Color stroke, Color fill, float strokeWidth)
        {
            int xMin = Mathf.RoundToInt(Mathf.Min(a.x, b.x));
            int xMax = Mathf.RoundToInt(Mathf.Max(a.x, b.x));
            int yMin = Mathf.RoundToInt(Mathf.Min(a.y, b.y));
            int yMax = Mathf.RoundToInt(Mathf.Max(a.y, b.y));

            if (fill.a > 0f)
            {
                for (int y = yMin; y <= yMax; y++)
                    for (int x = xMin; x <= xMax; x++)
                        SetPixelSafe(tex, x, y, fill);
            }

            // Stroke edges
            DrawThickLine(tex, new Vector2(xMin, yMin), new Vector2(xMax, yMin), stroke, strokeWidth);
            DrawThickLine(tex, new Vector2(xMax, yMin), new Vector2(xMax, yMax), stroke, strokeWidth);
            DrawThickLine(tex, new Vector2(xMax, yMax), new Vector2(xMin, yMax), stroke, strokeWidth);
            DrawThickLine(tex, new Vector2(xMin, yMax), new Vector2(xMin, yMin), stroke, strokeWidth);
        }

        // Fill circle and stroke
        public static void DrawFilledCircle(Texture2D tex, Vector2 center, Vector2 edge, Color stroke, Color fill, float strokeWidth)
        {
            float radius = Vector2.Distance(center, edge);
            int r = Mathf.RoundToInt(radius);

            if (fill.a > 0f)
            {
                for (int y = -r; y <= r; y++)
                {
                    for (int x = -r; x <= r; x++)
                    {
                        if (x * x + y * y <= r * r)
                            SetPixelSafe(tex, (int)center.x + x, (int)center.y + y, fill);
                    }
                }
            }

            int segments = Mathf.Clamp(Mathf.CeilToInt(radius * 6f), 32, 256);
            Vector2 prev = center + new Vector2(Mathf.Cos(0f), Mathf.Sin(0f)) * radius;
            for (int i = 1; i <= segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 next = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
                DrawThickLine(tex, prev, next, stroke, strokeWidth);
                prev = next;
            }
        }

        // Distance from point to segment
        public static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude;
            if (denom == 0f) return Vector2.Distance(p, a);
            float t = Vector2.Dot(p - a, ab) / denom;
            t = Mathf.Clamp01(t);
            return Vector2.Distance(p, a + t * ab);
        }

        // Clear texture
        public static void ClearTexture(Texture2D tex)
        {
            // backward-compatible: clear to transparent
            ClearTexture(tex, Color.clear);
        }

        public static void ClearTexture(Texture2D tex, Color background)
        {
            if (tex == null) return;
            Color[] pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = background;
            tex.SetPixels(pixels);
            tex.Apply();
        }
    }
}
