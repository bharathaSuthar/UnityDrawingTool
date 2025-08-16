using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using MyCompany.VectorEditor.Core;

namespace MyCompany.VectorEditor.Exporters
{
    public static class SVGExporter
    {
        public static void SaveSVG(string path, int width, int height, List<ShapeData> shapes, Color background)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");

            // --- Background ---
            string bgRGB; float bgOp;
            ToSvgColor(background, out bgRGB, out bgOp);
            sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"{bgRGB}\" fill-opacity=\"{bgOp:0.###}\" />");

            // --- Shapes ---
            foreach (var shape in shapes)
            {
                string strokeRGB; float strokeOp; string fillRGB; float fillOp;
                ToSvgColor(shape.strokeColor, out strokeRGB, out strokeOp);
                ToSvgColor(shape.fillColor, out fillRGB, out fillOp);

                string strokeAttr = $"stroke=\"{strokeRGB}\" stroke-width=\"{Mathf.Max(1f, shape.strokeWidth)}\" stroke-opacity=\"{strokeOp:0.###}\"";
                string fillAttr = (shape.fillColor.a > 0f) ? $"fill=\"{fillRGB}\" fill-opacity=\"{fillOp:0.###}\"" : "fill=\"none\"";

                if (shape.mode == ToolMode.Rectangle && shape.points.Count >= 2)
                {
                    Vector2 s = shape.points[0];
                    Vector2 e = shape.points[1];
                    float x = Mathf.Min(s.x, e.x);
                    float y = Mathf.Min(s.y, e.y);
                    float w = Mathf.Abs(e.x - s.x);
                    float h = Mathf.Abs(e.y - s.y);
                    sb.AppendLine($"  <rect x=\"{x}\" y=\"{height - y - h}\" width=\"{w}\" height=\"{h}\" {strokeAttr} {fillAttr} />");
                }
                else if (shape.mode == ToolMode.Circle && shape.points.Count >= 2)
                {
                    Vector2 c = shape.points[0];
                    Vector2 e = shape.points[1];
                    float r = Vector2.Distance(c, e);
                    sb.AppendLine($"  <circle cx=\"{c.x}\" cy=\"{height - c.y}\" r=\"{r}\" {strokeAttr} {fillAttr} />");
                }
                else if (shape.mode == ToolMode.Line && shape.points.Count >= 2)
                {
                    Vector2 p1 = shape.points[0];
                    Vector2 p2 = shape.points[1];
                    sb.AppendLine($"  <line x1=\"{p1.x}\" y1=\"{height - p1.y}\" x2=\"{p2.x}\" y2=\"{height - p2.y}\" {strokeAttr} fill=\"none\" />");
                }
                else if (shape.mode == ToolMode.Freehand && shape.points.Count >= 2)
                {
                    sb.Append("  <polyline points=\"");
                    foreach (var p in shape.points)
                        sb.Append($"{p.x},{height - p.y} ");
                    sb.AppendLine($"\" {strokeAttr} fill=\"none\" />");
                }
            }

            sb.AppendLine("</svg>");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"SVG saved to {path}");
        }


        private static void ToSvgColor(Color c, out string rgb, out float opacity)
        {
            int r = Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f);
            int g = Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f);
            int b = Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
            rgb = $"rgb({r},{g},{b})";
            opacity = Mathf.Clamp01(c.a);
        }
    }
}
