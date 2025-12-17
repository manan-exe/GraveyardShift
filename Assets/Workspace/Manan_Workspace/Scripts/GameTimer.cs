using UnityEngine;
using TMPro;

//important for win condition of the game
//have to survive 2 minutes
public class GameTimer : MonoBehaviour
{
    [Header("Win Condition")]
    [Tooltip("Set to 5 for quick testing.")]
    //120 seconds = 2 min
    public float durationSeconds = 120f;

    [Header("UI")]
    //text where time will be constnatly updated
    public TMP_Text timerText;

    //value of time remaining that will be displayed
    private float timeRemaining;
    //flag to check if the timer is running because game can be paused and when the game
    //  is paused timer should not be running
    private bool running;

    public void StartTimer() {
        //set to default time
        timeRemaining = durationSeconds;
        //set timer as running
        running = true;
        //update text to show time
        UpdateUI();
    }

    //pause timer without reset
    //used for pausing game
    public void StopTimer() {
        running = false;
    }

    void Update() {
        //if timer not running do nothing
        if (!running) return;

        //decrement time
        timeRemaining -= Time.deltaTime;
        //error gaurd so time does not go below 0
        if (timeRemaining < 0f) timeRemaining = 0f;

        //update time every frame
        UpdateUI();

        //if time hits 0 that means you survived the 2 minutes and won
        if (timeRemaining <= 0f)
        {
            //stop timer
            running = false;
            //play win dialogue
            GameFlowManager.Instance.TriggerWin();
        }
    }

    //helper function to constantly update time
    void UpdateUI() {
        //if timer does not exist do nothing. we have a problem if it does not exist
        if (timerText == null) return;

        //time rounded to whole seconds
        int total = Mathf.CeilToInt(timeRemaining);
        //calculate minutes
        int minutes = total / 60;
        //calculate remainder seconds
        int seconds = total % 60;
        //apply proper time to timer text
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
