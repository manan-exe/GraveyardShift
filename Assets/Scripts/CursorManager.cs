using UnityEngine;

public class CursorManager : MonoBehaviour
{
    void Start() {
        LockCursor();
    }

    void Update() {
        // ESC = unlock the cursor (show it so you can click UI/editor)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        // Left click = lock the cursor back to gameplay
        if (Input.GetMouseButtonDown(0))
        {
            // Only lock if it's currently unlocked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
        }
    }

    void LockCursor() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void UnlockCursor() {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}