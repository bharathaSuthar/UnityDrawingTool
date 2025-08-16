using System.Collections.Generic;
using UnityEngine;

namespace MyCompany.VectorEditor.Core
{
    public enum ToolMode { Select, Freehand, Line, Rectangle, Circle }

    public class ShapeData
    {
        public ToolMode mode;
        public List<Vector2> points = new List<Vector2>();
        public Color strokeColor = Color.black;
        public Color fillColor = Color.clear;
        public float strokeWidth = 2f;

        public ShapeData Clone()
        {
            return new ShapeData
            {
                mode = this.mode,
                points = new List<Vector2>(this.points),
                strokeColor = this.strokeColor,
                fillColor = this.fillColor,
                strokeWidth = this.strokeWidth
            };
        }
    }
}
