using UnityEngine;
public class CanvasSettings
{
    public int width;
    public int height;
    public Color backgroundColor;

    public CanvasSettings(int w, int h, Color bg)
    {
        width = w;
        height = h;
        backgroundColor = bg;
    }
}