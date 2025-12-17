using UnityEngine;
using UnityEngine.SceneManagement;

//controls game flow like pause, win, lose, and going to main menu
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    //field that spawners and zombie code checks to see if they should be active or not
    //zombies should not move/attack when game is paused
    //spawner should not spawn while game is paused
    //nothing is active while intro dialogue or win/lose dialogue plays
    public static bool GameplayActive { get; private set; }

    //field for other functions to check if game is paused
    public static bool IsPaused { get; private set; }


    [Header("Scene Names")]
    //tell script what is the name of the scene we will need to go to
    public string mainMenuSceneName = "MainMenu";

    [Header("References")]
    //this is how the game flow manager cmmunicates with the dialogue system to play the relevant
    //  dialogue based on state of game like intro/win/lose
    public DialogueSystem dialogueSystem;
    //references timer to see if we hit win condition
    public GameTimer gameTimer;

    [Header("Player Control To Toggle")]
    //script toggles player controls if the game is paused or unpaused
    public PlayerController playerController;
    public PlayerShooting playerShooting;

    [Header("Pause UI")]
    //ui panel when game is paused
    public GameObject pausePanel;

    //also indicates when paused?
    //i think i accidentally use two variables that do the same thing
    private bool paused;
    //set after the win or lose state so that we transition to main menu
    private bool gameEnded;

    [Header("Audio - Endings")]
    //win audio
    public AudioClip winSfx;
    //volume slider
    [Range(0f, 1f)] public float winVolume = 1f;


    void Awake() {
        Instance = this;
    }


    void Start() {
        //gameplay does not start until intro finishes
        SetGameplayEnabled(false);
        GameplayActive = false;

        //hide pause panel at start
        if (pausePanel != null)
            pausePanel.SetActive(false);

        //find dialogue system
        if (dialogueSystem != null)
        {
            //intro event
            dialogueSystem.IntroFinished += OnIntroFinished;
            //win event
            dialogueSystem.WinFinished += () => { PrepareToLeaveGameplay(); SceneManager.LoadScene(mainMenuSceneName); };
            //lose event
            dialogueSystem.LoseFinished += () => { PrepareToLeaveGameplay(); SceneManager.LoadScene(mainMenuSceneName); };

        }
        else
        {
            //we shouldn't get here but just start the game if dialogue does not exist
            OnIntroFinished();
        }
    }

    void Update() {
        //stop doing stuff if game is done. that means outro sequence finished
        if (gameEnded) return;

        //the "esc" key lets us pause the game
        //"esc" also releases the cursor even if pause menu was not implemented
        //  that funtionality is in another script
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    //runs when intro dialogue finishes
    void OnIntroFinished() {
        //this should not instantly trigger. just an error gaurd because unity loves to crash on me
        if (gameEnded) return;

        //when intro is finished let spawners/zombies run
        //let player move
        SetGameplayEnabled(true);
        GameplayActive = true;

        //start the timer if it exists
        if (gameTimer != null)
            gameTimer.StartTimer();
    }

    //called by game timer script
    public void TriggerWin() {

        //play win sound effect
        if (winSfx != null)
            AudioSource.PlayClipAtPoint(winSfx, Camera.main.transform.position, winVolume);
               
        //makes sure that win sequence doesnt repeat in the same run
        if (gameEnded) return;

        //marks game as complete so sequences does not repeat
        gameEnded = true;

        //reset cursor and allow game time to flow again so that it does not mess up main menu or next run
        PrepareToLeaveGameplay();

        //disable player input
        SetGameplayEnabled(false);

        //variable set to false to stop zombie and spawners
        GameplayActive = false;

        //stop game timer if it exists
        if (gameTimer != null) gameTimer.StopTimer();

        //play win dialogue sequence if it exists
        //otherwise go straight to main menu
        if (dialogueSystem != null) dialogueSystem.StartWinSequence();
        else SceneManager.LoadScene(mainMenuSceneName);
    }

    //called by player health script
    public void TriggerLose() {
        //ignore repeating lose sequence
        if (gameEnded) return;

        //flag game as complete
        gameEnded = true;

        //reset cursor and allow game time to flow again so that it does not mess up main menu or next run
        PrepareToLeaveGameplay();

        //disable player input
        SetGameplayEnabled(false);
        //stop zombie/spanwers
        GameplayActive = false;
        //stop game timer
        if (gameTimer != null) gameTimer.StopTimer();

        //play lose sequence if it exists. else go to main menu
        if (dialogueSystem != null) dialogueSystem.StartLoseSequence();
        else SceneManager.LoadScene(mainMenuSceneName);
    }

    //toggles player controlls
    void SetGameplayEnabled(bool enabled) {
        //toggle movement
        if (playerController != null) playerController.enabled = enabled;
        //toggle shooting system
        if (playerShooting != null) playerShooting.enabled = enabled;

    }

    //pause system. triggers pause screen overlay
    void TogglePause() {
        //if game ended no reason to pause so ignore pause input
        if (gameEnded) return;

        //toggle pause
        paused = !paused;
        //set pause flag for other functions to check
        IsPaused = paused;

        //show or hide pause panel
        if (pausePanel != null)
            pausePanel.SetActive(paused);

        //toggle game timer to stop or run
        Time.timeScale = paused ? 0f : 1f;

        //if paused disable player input
        SetGameplayEnabled(!paused);

        //toggle cursor if pausing or unpausing
        SetCursorPaused(paused);
    }

    //allows other scripts to call setGameplayEnabled
    public void SetGameplayEnabledPublic(bool enabled) {

        SetGameplayEnabled(enabled);
    }

    //toggle cursor lock/hiding
    void SetCursorPaused(bool isPaused) {
        //see cursor when paused. hide when not paused
        Cursor.visible = isPaused;
        //allow cursor to move when paused. lock when not paused
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    //runs when leaving gameplay scene
    void PrepareToLeaveGameplay() {
        //resumes time flow so that it does not mess with the next game
        Time.timeScale = 1f;
        //tells other functions that game is not paused
        paused = false;
        IsPaused = false;
        
        //hide pause panel
        if (pausePanel != null)
            pausePanel.SetActive(false);

        //show/unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    //method used by pause panel to go back to main menu
    public void GoToMainMenu() {
        //do nothing if game ended
        if (gameEnded) return;

        //reset cursor / pause state /timer
        PrepareToLeaveGameplay();
        //go to main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }



}
