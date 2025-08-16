using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MyCompany.VectorEditor.Core;
using MyCompany.VectorEditor.Exporters;

namespace MyCompany.VectorEditor.UI
{
    public class VectorEditorWindow : EditorWindow
    {
        private Texture2D canvasTex;
        private int texSize = 512;

        private List<ShapeData> shapes = new List<ShapeData>();
        private ShapeData currentShape;
        private ShapeData selectedShape;
        private bool isDrawing = false;
        private bool isMoving = false;
        private Vector2 startPos;

        // UI params
        private Color strokeColor = Color.black;
        private Color fillColor = new Color(0, 0, 0, 0);
        private float strokeWidth = 2f;
        private ToolMode currentMode = ToolMode.Select;

        // Undo/Redo
        private Stack<List<ShapeData>> undoStack = new Stack<List<ShapeData>>();
        private Stack<List<ShapeData>> redoStack = new Stack<List<ShapeData>>();

        [MenuItem("Tools/Vector Editor (Modular)")]
        public static void ShowWindow()
        {
            GetWindow<VectorEditorWindow>("Vector Editor");
        }

        private void OnEnable()
        {
            canvasTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            RedrawAll();
        }

        private void OnGUI()
        {
            GUILayout.Label("Vector Editor (Modular) - Phase 1", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Undo")) UndoAction();
            if (GUILayout.Button("Redo")) RedoAction();
            GUILayout.Space(8);
            if (GUILayout.Button("Clear")) { PushUndo(); shapes.Clear(); selectedShape = null; RedrawAll(); }
            if (GUILayout.Button("Save PNG")) PNGExporter.SavePNG(canvasTex);
            if (GUILayout.Button("Save SVG")) SaveSVGDialog();
            EditorGUILayout.EndHorizontal();

            strokeColor = EditorGUILayout.ColorField("Stroke Color", strokeColor);
            fillColor = EditorGUILayout.ColorField("Fill Color", fillColor);
            strokeWidth = EditorGUILayout.Slider("Stroke Width", strokeWidth, 1f, 20f);

            GUILayout.Label("Tool Mode:");
            currentMode = (ToolMode)GUILayout.Toolbar((int)currentMode, new string[] { "Select", "Freehand", "Line", "Rect", "Circle" });

            if (selectedShape != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Selected Shape", EditorStyles.boldLabel);
                Color newStroke = EditorGUILayout.ColorField("Sel Stroke", selectedShape.strokeColor);
                Color newFill = EditorGUILayout.ColorField("Sel Fill", selectedShape.fillColor);
                float newWidth = EditorGUILayout.Slider("Sel Stroke W", selectedShape.strokeWidth, 1f, 20f);
                if (newStroke != selectedShape.strokeColor || newFill != selectedShape.fillColor || Mathf.Abs(newWidth - selectedShape.strokeWidth) > Mathf.Epsilon)
                {
                    PushUndo();
                    selectedShape.strokeColor = newStroke;
                    selectedShape.fillColor = newFill;
                    selectedShape.strokeWidth = newWidth;
                    RedrawAll();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Bring To Front")) { PushUndo(); shapes.Remove(selectedShape); shapes.Add(selectedShape); RedrawAll(); }
                if (GUILayout.Button("Send To Back")) { PushUndo(); shapes.Remove(selectedShape); shapes.Insert(0, selectedShape); RedrawAll(); }
                if (GUILayout.Button("Delete")) { PushUndo(); shapes.Remove(selectedShape); selectedShape = null; RedrawAll(); }
                EditorGUILayout.EndHorizontal();
            }

            Rect drawArea = GUILayoutUtility.GetRect(texSize, texSize, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(drawArea, canvasTex);

            HandleInput(drawArea);
        }

        private void HandleInput(Rect area)
        {
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            if (!area.Contains(mouse)) return;

            Vector2 local = mouse - new Vector2(area.x, area.y);
            local.y = texSize - local.y; // invert y for texture space

            if (currentMode == ToolMode.Select)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    selectedShape = null;
                    // top-down hit test
                    for (int i = shapes.Count - 1; i >= 0; i--)
                    {
                        if (IsHit(local, shapes[i]))
                        {
                            selectedShape = shapes[i];
                            isMoving = true;
                            startPos = local;
                            Repaint();
                            break;
                        }
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && isMoving && selectedShape != null)
                {
                    Vector2 delta = local - startPos;
                    startPos = local;
                    for (int i = 0; i < selectedShape.points.Count; i++)
                        selectedShape.points[i] += delta;
                    RedrawAll();
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && isMoving)
                {
                    isMoving = false;
                    PushUndo();
                    e.Use();
                }
                return;
            }

            // Drawing modes
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isDrawing = true;
                startPos = local;
                currentShape = new ShapeData
                {
                    mode = currentMode,
                    strokeColor = strokeColor,
                    fillColor = fillColor,
                    strokeWidth = strokeWidth
                };
                if (currentMode == ToolMode.Freehand) currentShape.points.Add(local);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDrawing)
            {
                if (currentMode == ToolMode.Freehand)
                {
                    Vector2 last = currentShape.points[currentShape.points.Count - 1];
                    DrawingUtils.DrawThickLine(canvasTex, last, local, currentShape.strokeColor, currentShape.strokeWidth);
                    currentShape.points.Add(local);
                    canvasTex.Apply();
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp && isDrawing)
            {
                isDrawing = false;
                PushUndo();

                switch (currentMode)
                {
                    case ToolMode.Line:
                        DrawingUtils.DrawThickLine(canvasTex, startPos, local, strokeColor, strokeWidth);
                        currentShape.points.Add(startPos);
                        currentShape.points.Add(local);
                        break;
                    case ToolMode.Rectangle:
                        DrawingUtils.DrawFilledRect(canvasTex, startPos, local, strokeColor, fillColor, strokeWidth);
                        currentShape.points.Add(startPos);
                        currentShape.points.Add(local);
                        break;
                    case ToolMode.Circle:
                        DrawingUtils.DrawFilledCircle(canvasTex, startPos, local, strokeColor, fillColor, strokeWidth);
                        currentShape.points.Add(startPos);
                        currentShape.points.Add(local);
                        break;
                }

                canvasTex.Apply();
                shapes.Add(currentShape);
                currentShape = null;
                e.Use();
            }
        }

        private bool IsHit(Vector2 pos, ShapeData s)
        {
            if (s.mode == ToolMode.Rectangle && s.points.Count >= 2)
            {
                Vector2 min = new Vector2(Mathf.Min(s.points[0].x, s.points[1].x), Mathf.Min(s.points[0].y, s.points[1].y));
                Vector2 max = new Vector2(Mathf.Max(s.points[0].x, s.points[1].x), Mathf.Max(s.points[0].y, s.points[1].y));
                return pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y;
            }
            else if (s.mode == ToolMode.Circle && s.points.Count >= 2)
            {
                float r = Vector2.Distance(s.points[0], s.points[1]);
                return Vector2.Distance(pos, s.points[0]) <= r + s.strokeWidth * 0.5f;
            }
            else if ((s.mode == ToolMode.Line || s.mode == ToolMode.Freehand) && s.points.Count >= 2)
            {
                float threshold = Mathf.Max(2f, s.strokeWidth * 0.6f);
                for (int i = 0; i < s.points.Count - 1; i++)
                    if (DrawingUtils.DistancePointToSegment(pos, s.points[i], s.points[i + 1]) <= threshold) return true;
            }
            return false;
        }

        private void RedrawAll()
        {
            DrawingUtils.ClearTexture(canvasTex);
            foreach (var s in shapes)
            {
                switch (s.mode)
                {
                    case ToolMode.Freehand:
                        for (int i = 0; i < s.points.Count - 1; i++)
                            DrawingUtils.DrawThickLine(canvasTex, s.points[i], s.points[i + 1], s.strokeColor, s.strokeWidth);
                        break;
                    case ToolMode.Line:
                        if (s.points.Count >= 2) DrawingUtils.DrawThickLine(canvasTex, s.points[0], s.points[1], s.strokeColor, s.strokeWidth);
                        break;
                    case ToolMode.Rectangle:
                        if (s.points.Count >= 2) DrawingUtils.DrawFilledRect(canvasTex, s.points[0], s.points[1], s.strokeColor, s.fillColor, s.strokeWidth);
                        break;
                    case ToolMode.Circle:
                        if (s.points.Count >= 2) DrawingUtils.DrawFilledCircle(canvasTex, s.points[0], s.points[1], s.strokeColor, s.fillColor, s.strokeWidth);
                        break;
                }
            }
            canvasTex.Apply();
            Repaint();
        }

        // Undo / Redo helpers (snapshot clones)
        private void PushUndo()
        {
            var snap = new List<ShapeData>(shapes.Count);
            foreach (var s in shapes) snap.Add(s.Clone());
            undoStack.Push(snap);
            redoStack.Clear();
        }

        private void UndoAction()
        {
            if (undoStack.Count == 0) return;
            var cur = new List<ShapeData>(shapes.Count);
            foreach (var s in shapes) cur.Add(s.Clone());
            redoStack.Push(cur);

            shapes = undoStack.Pop();
            selectedShape = null;
            RedrawAll();
        }

        private void RedoAction()
        {
            if (redoStack.Count == 0) return;
            var cur = new List<ShapeData>(shapes.Count);
            foreach (var s in shapes) cur.Add(s.Clone());
            undoStack.Push(cur);

            shapes = redoStack.Pop();
            selectedShape = null;
            RedrawAll();
        }

        private void SaveSVGDialog()
        {
            string path = EditorUtility.SaveFilePanel("Save SVG", "", "drawing.svg", "svg");
            if (string.IsNullOrEmpty(path)) return;
            SVGExporter.SaveSVG(path, texSize, texSize, shapes);
            AssetDatabase.Refresh();
        }
    }
}
