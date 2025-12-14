using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Win Condition")]
    [Tooltip("Set to 5 for quick testing.")]
    public float durationSeconds = 120f;

    [Header("UI")]
    public TMP_Text timerText;

    private float timeRemaining;
    private bool running;

    public void StartTimer() {
        timeRemaining = durationSeconds;
        running = true;
        UpdateUI();
    }

    public void StopTimer() {
        running = false;
    }

    void Update() {
        if (!running) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f) timeRemaining = 0f;

        UpdateUI();

        if (timeRemaining <= 0f)
        {
            running = false;
            GameFlowManager.Instance.TriggerWin();
        }
    }

    void UpdateUI() {
        if (timerText == null) return;

        int total = Mathf.CeilToInt(timeRemaining);
        int minutes = total / 60;
        int seconds = total % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
