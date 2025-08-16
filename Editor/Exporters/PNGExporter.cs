using System.IO;
using UnityEditor;
using UnityEngine;

namespace MyCompany.VectorEditor.Exporters
{
    public static class PNGExporter
    {
        public static void SavePNG(Texture2D tex, string defaultName = "drawing.png")
        {
            string path = EditorUtility.SaveFilePanel("Save PNG", "", defaultName, "png");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log($"PNG saved to {path}");
        }
    }
}
