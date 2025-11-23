using UnityEngine;

public class Crosshair : MonoBehaviour
{
    public float lineLength = 10f;
    public float lineThickness = 2f;
    public Color crosshairColor = Color.white;

    private Texture2D _lineTex;

    void Awake() {
        _lineTex = new Texture2D(1, 1);
        _lineTex.SetPixel(0, 0, Color.white);
        _lineTex.Apply();
    }

    void OnGUI() {
        // You can early-out here if you only want it while aiming:
        // var anim = FindObjectOfType<Animator>(); etc.
        // but for now: always show the crosshair.

        GUI.color = crosshairColor;

        float x = Screen.width / 2f;
        float y = Screen.height / 2f;

        // Horizontal line
        GUI.DrawTexture(
            new Rect(x - lineLength, y - (lineThickness / 2f), lineLength * 2f, lineThickness),
            _lineTex
        );

        // Vertical line
        GUI.DrawTexture(
            new Rect(x - (lineThickness / 2f), y - lineLength, lineThickness, lineLength * 2f),
            _lineTex
        );

        GUI.color = Color.white;
    }
}
