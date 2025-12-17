using UnityEngine;

public class MainMenuCursor : MonoBehaviour
{
    //handles cursor on the main menu.
    //had some issues with the cursor because i want to lock it during the game since the game
    //  is a third person shooter and you do not want to see your cursor
    //however the gameplay loop messes things up
    //cursor needs to exist on main menu
    //cursor gets hidden during gameplay
    //cursor needs to come back if you pause
    //cursor comes back if you go back to main menu from gameplay whether that was from pause,winning, or losing
    void Start() {
        //resumes passage of time in game
        //needed because pause freezes time
        //also win and lose and intro dialogue freezes time
        Time.timeScale = 1f;

        //reenable cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
