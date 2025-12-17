using UnityEngine;
using UnityEngine.SceneManagement;

//handles main menu functionality. nothing much here
public class MainMenuUI : MonoBehaviour
{
    //field to enter in the name for the scene that contains the gameplay stuff
    public string gameplaySceneName = "Gameplay";

    //handles the button press to start the game
    public void StartGame() {
        SceneManager.LoadScene(gameplaySceneName);
    }
}
