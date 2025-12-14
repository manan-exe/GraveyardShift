using UnityEngine;

public class MainMenuCursor : MonoBehaviour
{
    void Start() {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
