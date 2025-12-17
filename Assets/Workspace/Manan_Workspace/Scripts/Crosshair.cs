using UnityEngine;

//simple script for crosshair to show where you are aiming
public class Crosshair : MonoBehaviour
{
    //customizable fields. easier to fine tune in unity inspector than in the code
    //length of crosshair since it is a "+" shaped crosshair
    public float lineLength = 10f;
    //how thick those lines are
    public float lineThickness = 2f;
    //color of crosshair
    public Color crosshairColor = Color.white;

    //used to draw crosshair lines on screen
    private Texture2D _lineTex;

    void Awake() {
        //draws 1x1 texture
        _lineTex = new Texture2D(1, 1);
        //set crosshair color to white?
        _lineTex.SetPixel(0, 0, Color.white);
        //apply settings
        _lineTex.Apply();
    }

    void OnGUI() {
        //set it as configured crosshair color
        GUI.color = crosshairColor;

        //calculate center of screen
        float x = Screen.width / 2f;
        float y = Screen.height / 2f;

        //draw horizontal line
        GUI.DrawTexture(
            new Rect(x - lineLength, y - (lineThickness / 2f), lineLength * 2f, lineThickness),
            _lineTex
        );

        //draw vertical line
        GUI.DrawTexture(
            new Rect(x - (lineThickness / 2f), y - lineLength, lineThickness, lineLength * 2f),
            _lineTex
        );

        //set color again
        GUI.color = Color.white;
    }
}
