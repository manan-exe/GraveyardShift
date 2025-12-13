using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;
    public static bool GameplayActive { get; private set; }


    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";

    [Header("References")]
    public DialogueSystem dialogueSystem;
    public GameTimer gameTimer;

    [Header("Player Control To Toggle")]
    public PlayerController playerController;
    public PlayerShooting playerShooting;

    [Header("Pause UI")]
    public GameObject pausePanel;

    private bool paused;
    private bool gameEnded;

    void Awake() {
        Instance = this;
    }

    void Start() {
        // lock gameplay until intro finishes
        SetGameplayEnabled(false);
        GameplayActive = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (dialogueSystem != null)
        {
            dialogueSystem.IntroFinished += OnIntroFinished;
            dialogueSystem.WinFinished += () => SceneManager.LoadScene(mainMenuSceneName);
            dialogueSystem.LoseFinished += () => SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // if no dialogue system, just start immediately
            OnIntroFinished();
        }
    }

    void Update() {
        if (gameEnded) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    void OnIntroFinished() {
        if (gameEnded) return;

        SetGameplayEnabled(true);
        GameplayActive = true;

        if (gameTimer != null)
            gameTimer.StartTimer();
    }

    public void TriggerWin() {
        if (gameEnded) return;
        gameEnded = true;

        SetGameplayEnabled(false);
        GameplayActive = false;
        if (gameTimer != null) gameTimer.StopTimer();

        if (dialogueSystem != null) dialogueSystem.StartWinSequence();
        else SceneManager.LoadScene(mainMenuSceneName);
    }

    public void TriggerLose() {
        if (gameEnded) return;
        gameEnded = true;

        SetGameplayEnabled(false);
        GameplayActive = false;
        if (gameTimer != null) gameTimer.StopTimer();

        if (dialogueSystem != null) dialogueSystem.StartLoseSequence();
        else SceneManager.LoadScene(mainMenuSceneName);
    }

    void SetGameplayEnabled(bool enabled) {
        if (playerController != null) playerController.enabled = enabled;
        if (playerShooting != null) playerShooting.enabled = enabled;

        // If you have CursorManager, you can also unlock cursor when disabled
        // (leave as-is for now to avoid breaking your setup)
    }

    void TogglePause() {
        if (gameEnded) return;

        paused = !paused;

        if (pausePanel != null)
            pausePanel.SetActive(paused);

        Time.timeScale = paused ? 0f : 1f;

        // Optional: disable inputs while paused
        SetGameplayEnabled(!paused);
    }
    public void SetGameplayEnabledPublic(bool enabled) {
        // call your existing SetGameplayEnabled
        // (or just copy the body)
        SetGameplayEnabled(enabled);
    }

}
