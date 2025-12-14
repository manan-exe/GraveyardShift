using UnityEngine;

public class CursorManager : MonoBehaviour
{
    void Start() {
        LockCursor();
    }

    void Update() {
        // If game is paused (or gameplay inactive), don't auto-lock on click.
        // Let UI be clickable.
        // If paused OR gameplay not active, keep cursor free (don't re-lock on click)
        if (GameFlowManager.Instance != null)
        {
            if (!GameFlowManager.GameplayActive || GameFlowManager.IsPaused)
            {
                UnlockCursor();
                return;
            }
        }


        // ESC can still unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        // Left click = lock cursor back ONLY during gameplay
        if (Input.GetMouseButtonDown(0))
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                LockCursor();
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