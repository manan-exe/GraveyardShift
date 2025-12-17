using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // for loading main menu later

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance;

    //this is supposed to let you skip dialogue but it is broken
    [Header("Global Toggle")]
    [Tooltip("Turn this off to skip ALL dialogue (useful while testing).")]
    public bool dialogueEnabled = true;


    [Header("Auto Intro")]
    [Tooltip("Play the intro dialogue as soon as the scene starts.")]
    public bool playIntroOnStart = true;

    [Header("UI References")]
    //this is the entire transparent box
    public GameObject dialoguePanel;
    //shows the current speaker on the left
    public Image portraitImage;
    //shows dialogue from the character
    public TMP_Text dialogueText;

    //for typing effect where text slowly displays instead of instant
    [Header("Typewriter Settings")]
    public float typeSpeed = 0.03f;

    [System.Serializable]
    public struct DialogueLine
    {
        //ended up not showing speaker name. leaving it here though
        public string speakerName;
        //shows current speaker
        public Sprite portrait;
        [TextArea(3, 5)]
        //dialogue being displayed
        public string text;
    }


    [Header("Intro Sequence")]
    //entries to wire in portraits in unity inspector
    public Sprite oldManPortrait;
    public Sprite playerPortrait;

    //lines of dialogue. but i am using the hardcoded defaults i made
    public DialogueLine[] introLines;
    public DialogueLine[] winLines;
    public DialogueLine[] loseLines;

    //current sequence being played. could be intro sequence, win sequence, or lose sequence
    DialogueLine[] currentLines;
    //current line from the array of lines
    int currentIndex;
    //true if text still being written
    bool isTyping;
    //reference to typewriter helper function
    Coroutine typingRoutine;
    //actions after done displaying dialogue sequence
    Action onDialogueComplete;
    //true when dialogue sequence in progress
    bool isRunning;

    //events for if you start game or win game or lose game
    public event Action IntroFinished;
    public event Action WinFinished;
    public event Action LoseFinished;


    void Awake() {
        //makes sure something already does not exist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //hide dialogue pane initially
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        //use default hardcoded dialogue if we do not change it
        InitializeDefaultSequencesIfEmpty();
    }

    void Start() {
        //start intro when game starts
        if (playIntroOnStart)
        {
            StartIntroSequence();
        }
    }


    void Update() {
        //if dialogue is not currently being typed then nothing to do
        if (!isRunning) return;
        //if script cant access dialogue panel then nothing to do.
        //if this is true then there is a problem
        if (dialoguePanel == null || !dialoguePanel.activeSelf) return;

        //handles what happens when you click to advance text
        if (Input.GetMouseButtonDown(0))
        {
            //if the dialogue is still being displayed character by character skip the 
            //  typewriter effect and just display the full thing
            if (isTyping)
            {
                SkipTypewriter();
            }
            //if text is fully displayed then go to next dialogue line
            else
            {
                AdvanceLine();
            }
        }
    }

    //called by other functions

    //start intro dialogue
    public void StartIntroSequence() {
        StartDialogue(introLines, OnIntroComplete);
    }

    //start win dialogue
    public void StartWinSequence() {
        StartDialogue(winLines, OnWinComplete);
    }

    //start lose dialogue
    public void StartLoseSequence() {
        StartDialogue(loseLines, OnLoseComplete);
    }

    //run a dialogue sequence helper function
    public void StartDialogue(DialogueLine[] lines, Action onComplete = null) {
        //skip dialogue if we disabled it. this is broken though
        if (!dialogueEnabled)
        {
            onComplete?.Invoke();
            return;
        }

        //if for some reason there are no lines then just skip dialogue
        //this shouldnt happen because we have hardcoded default lines
        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        //store lines
        currentLines = lines;
        //start lines at beginning
        currentIndex = 0;
        //what to do when dialogue is done
        onDialogueComplete = onComplete;
        //flag to show text is being typed
        isRunning = true;

        //show dialogue panel if it exists
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        //show dialogue lines
        ShowCurrentLine();
    }


    //helper functions

    //displays text character by character
    void ShowCurrentLine() {
        //error handling if there are no lines or we finished the lines
        //if either is the case then end dialogue
        //avoids index out of bounds errors
        if (currentLines == null || currentIndex < 0 || currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        //grab current line
        DialogueLine line = currentLines[currentIndex];

        //set portrait being displayed to the portrait of the speaker
        if (portraitImage != null)
            portraitImage.sprite = line.portrait;

        //stop any typing routine that isnt already stopped
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        //clear any displayed text before you start displaying new lines
        dialogueText.text = "";
        typingRoutine = StartCoroutine(TypeLine(line.text));
    }

    //the actual function that displays dialogue character by character
    IEnumerator TypeLine(string line) {
        //flag for other functions to show that script is typing the text out
        isTyping = true;
        dialogueText.text = "";

        //loop over each character and display one by one
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        //once down set typing flag to done
        isTyping = false;
    }

    //handles if the player wants to fast forward dialogue.
    //if dialogue is being typed and the player hits left click then the dialogue instantly
    //  displays the current line
    void SkipTypewriter() {
        //stop running anything currently being typed to prevent jumbled text
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        //if there exists a line then fully display it
        //avoid index out of bounds
        if (currentLines != null && currentIndex >= 0 && currentIndex < currentLines.Length)
        {
            dialogueText.text = currentLines[currentIndex].text;
        }

        //tell other functions we are done typing
        isTyping = false;
    }

    //go to next dialogue
    //if we are on the last line then end dialogue
    void AdvanceLine() {
        //go to next index in the array for a new line
        currentIndex++;

        //if there are no more lines then stop doing dialogue and do next action
        //  that might be starting gameplay or going back to main menu
        if (currentLines == null || currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        //if there are more lines then go to next line
        ShowCurrentLine();
    }

    //handles when all of the lines have been displayed
    void EndDialogue() {
        //mark dialogue as done for other functions to see
        isRunning = false;

        //hide dialogue panel if it is being shown
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
    }

   //hard coded dialogue -----------------------------------------------------------
   //its just dialogue i am not going to comment this part since it is a repeating structure
    void InitializeDefaultSequencesIfEmpty() {
        //intro dialogue
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

        //lose dialogue
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

        //win dialogue
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

    //what to do after a specific set of dialogue finishes
    //tell game flow manager to start gameplay
    void OnIntroComplete() {
        Debug.Log("Intro finished. Start gameplay.");
        IntroFinished?.Invoke();
    }

    //tell game flow manager to go to main menu because we won
    void OnWinComplete() {
        Debug.Log("Win dialogue finished. Go to main menu.");
        WinFinished?.Invoke();
    }

    //tell game flow manager to go to main menu because we lost
    void OnLoseComplete() {
        Debug.Log("Lose dialogue finished. Go to main menu.");
        LoseFinished?.Invoke();
    }

}
