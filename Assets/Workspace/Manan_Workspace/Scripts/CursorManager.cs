using UnityEngine;

//handles locking cursor during gameplay so it is not in the way
public class CursorManager : MonoBehaviour
{

    void Start() {
        //immediately lock cursor
        LockCursor();
    }

    void Update() {
        //if gameflow manager exists. (it should or else there is a problem)
        if (GameFlowManager.Instance != null)
        {
            //if gameplay is not active because it is paused
            if (!GameFlowManager.GameplayActive || GameFlowManager.IsPaused)
            {
                //make cursor visible so you can hit return to main menu button
                UnlockCursor();
                return;
            }
        }


        //esc unlocks cursor. this was mainly for early stages where pause menu didn't exist but
        //  im leaving this in here
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            UnlockCursor();
        }

        //checks if left click is pressed
        if (Input.GetMouseButtonDown(0))
        {
            //if cursor isnt already locked then lock it
            if (Cursor.lockState != CursorLockMode.Locked)
                LockCursor();
        }
    }

    //helper function to lock cursor
    void LockCursor() {
        //make it invisible
        Cursor.visible = false;
        //make it not move so it doesnt move somewhere while invisble and mess something up
        Cursor.lockState = CursorLockMode.Locked;
    }

    //helper function to release cursor
    void UnlockCursor() {
        //make it visible
        Cursor.visible = true;
        //allow it to move
        Cursor.lockState = CursorLockMode.None;
    }
}