using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // for loading main menu later

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance;

    [Header("Global Toggle")]
    [Tooltip("Turn this off to skip ALL dialogue (useful while testing).")]
    public bool dialogueEnabled = true;

    [Header("Auto Intro")]
    [Tooltip("Play the intro dialogue as soon as the scene starts.")]
    public bool playIntroOnStart = true;

    [Header("UI References")]
    public GameObject dialoguePanel; // whole box
    public Image portraitImage;      // left side portrait
    public TMP_Text dialogueText;    // right side text

    [Header("Typewriter Settings")]
    public float typeSpeed = 0.03f;

    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;  // optional (not used visually yet, but good to keep)
        public Sprite portrait;
        [TextArea(3, 5)]
        public string text;
    }

    [Header("Intro Sequence")]
    public Sprite oldManPortrait;
    public Sprite playerPortrait;

    // You can edit these in the Inspector if you want,
    // but we also populate them with defaults in Awake().
    public DialogueLine[] introLines;
    public DialogueLine[] winLines;
    public DialogueLine[] loseLines;

    // --- Runtime state ---
    DialogueLine[] currentLines;
    int currentIndex;
    bool isTyping;
    Coroutine typingRoutine;
    Action onDialogueComplete;
    bool isRunning;

    void Awake() {
        // Simple singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // If arrays are empty in the inspector, fill with default lines.
        InitializeDefaultSequencesIfEmpty();
    }

    void Start() {
        if (playIntroOnStart)
        {
            StartIntroSequence();
        }
    }

    void Update() {
        if (!isRunning) return;
        if (dialoguePanel == null || !dialoguePanel.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // First space: skip typewriter, show full line
                SkipTypewriter();
            }
            else
            {
                // Next space: advance to next dialogue line
                AdvanceLine();
            }
        }
    }

    // =========================
    // PUBLIC API
    // =========================

    public void StartIntroSequence() {
        StartDialogue(introLines, OnIntroComplete);
    }

    public void StartWinSequence() {
        StartDialogue(winLines, OnWinComplete);
    }

    public void StartLoseSequence() {
        StartDialogue(loseLines, OnLoseComplete);
    }

    public void StartDialogue(DialogueLine[] lines, Action onComplete = null) {
        if (!dialogueEnabled)
        {
            // Skip dialogue entirely during testing
            onComplete?.Invoke();
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        onDialogueComplete = onComplete;
        isRunning = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowCurrentLine();
    }

    // =========================
    // INTERNAL LOGIC
    // =========================

    void ShowCurrentLine() {
        if (currentLines == null || currentIndex < 0 || currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentLines[currentIndex];

        if (portraitImage != null)
            portraitImage.sprite = line.portrait;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        dialogueText.text = "";
        typingRoutine = StartCoroutine(TypeLine(line.text));
    }

    IEnumerator TypeLine(string line) {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    void SkipTypewriter() {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        if (currentLines != null && currentIndex >= 0 && currentIndex < currentLines.Length)
        {
            dialogueText.text = currentLines[currentIndex].text;
        }

        isTyping = false;
    }

    void AdvanceLine() {
        currentIndex++;

        if (currentLines == null || currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void EndDialogue() {
        isRunning = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
    }

    // =========================
    // DEFAULT SEQUENCES
    // =========================

    void InitializeDefaultSequencesIfEmpty() {
        // Intro
        if (introLines == null || introLines.Length == 0)
        {
            introLines = new DialogueLine[]
            {
                new DialogueLine {
                    speakerName = "Old Man",
                    portrait = oldManPortrait,
                    text = "Good Heavens! This cannot be..."
                },
                new DialogueLine {
                    speakerName = "Player",
                    portrait = playerPortrait,
                    text = "What is it Old Man?"
                },
                new DialogueLine {
                    speakerName = "Old Man",
                    portrait = oldManPortrait,
                    text = "Heh... Well... The train has broken down..."
                },
                new DialogueLine {
                    speakerName = "Player",
                    portrait = playerPortrait,
                    text = "What?! Now?! We are in undead territory"
                },
                new DialogueLine {
                    speakerName = "Old Man",
                    portrait = oldManPortrait,
                    text = "I know this better than anyone young man..."
                },
                new DialogueLine {
                    speakerName = "Old Man",
                    portrait = oldManPortrait,
                    text = "Give me 2 minutes. If you can draw the undead away for just 2 minutes I can finish the repairs"
                },
                new DialogueLine {
                    speakerName = "Player",
                    portrait = playerPortrait,
                    text = "Your 2 minutes... I got it covered!"
                },
            };
        }

        // Lose
        if (loseLines == null || loseLines.Length == 0)
        {
            loseLines = new DialogueLine[]
            {
                new DialogueLine {
                    speakerName = "Player",
                    portrait = playerPortrait,
                    text = "Old Man! I can't keep this up"
                },
                new DialogueLine {
                    speakerName = "Old Man",
                    portrait = oldManPortrait,
                    text = "Blast! ...It's hopeless"
                },
            };
        }

        // Win
        if (winLines == null || winLines.Length == 0)
        {
            winLines = new DialogueLine[]
            {
                new DialogueLine {
                    speakerName = "Old Man",
                    portrait = oldManPortrait,
                    text = "Hurry boy! Climb aboard!"
                },
                new DialogueLine {
                    speakerName = "Player",
                    portrait = playerPortrait,
                    text = "You got it!"
                },
            };
        }
    }

    // =========================
    // CALLBACKS AFTER SEQUENCES
    // =========================

    void OnIntroComplete() {
        // TODO: start your gameplay here
        // e.g. enable player movement, start timer, etc.
        Debug.Log("Intro finished. Start gameplay.");
    }

    void OnWinComplete() {
        // TODO: once you have a main menu scene, load it here.
        // SceneManager.LoadScene("MainMenu");
        Debug.Log("Win dialogue finished. Go to main menu.");
    }

    void OnLoseComplete() {
        // TODO: once you have a main menu scene, load it here.
        // SceneManager.LoadScene("MainMenu");
        Debug.Log("Lose dialogue finished. Go to main menu.");
    }
}
