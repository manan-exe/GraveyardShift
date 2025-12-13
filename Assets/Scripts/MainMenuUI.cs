using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public string gameplaySceneName = "Gameplay";

    public void StartGame() {
        SceneManager.LoadScene(gameplaySceneName);
    }
}
