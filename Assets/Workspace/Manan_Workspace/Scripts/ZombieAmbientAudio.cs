using System.Collections;
using UnityEngine;
using UnityEngineInternal;

//handles zombie noise audio logic
public class ZombieAmbientAudio : MonoBehaviour
{
    [Header("Audio")]
    //source where audio comes from
    public AudioSource source;
    //actual audio clip.
    //why is this an array?
    //i am just going to leave it alone
    public AudioClip[] clips;


    [Header("Timing")]
    //minimum and maximum delay between zombie noises
    //we want it random and not on a continunous predictable loop
    //that will get annoying
    public float minDelay = 3f;
    public float maxDelay = 9f;
    //slider to control zombie noise frequency
    [Range(0f, 1f)] public float playChance = 0.8f; // chance each cycle plays a sound

    [Header("Variation")]
    //volume adjustment in unity inspector
    [Range(0f, 1f)] public float volume = 0.8f;
    //max and min volume
    public float pitchMin = 0.95f;
    public float pitchMax = 1.08f;


    void Awake()
    {
        //try to auto grab audio source if it wasnt manually assigned
        if (source == null) source = GetComponent<AudioSource>();
    }

    //runs when object is enabled. so zombie noise does not play during intro. or when game is paused
    void OnEnable()
    {
        StartCoroutine(Loop());
    }

    //helper function to loop audio
    IEnumerator Loop()
    {
        //offset so zombie is not constantly making noise
        yield return new WaitForSeconds(Random.Range(0.2f, 1.2f));

        //loop if audio component is enabled
        while (true)
        {
            //checks to make sure game is in a state where zombie noise can play
            //should not play when game is paused or when intro dialogue is palying
            if (GameFlowManager.GameplayActive && !GameFlowManager.IsPaused)
            {
                //makes sure we have audio clip in so that there are no errors
                if (source != null && clips != null && clips.Length > 0)
                {
                    //makes sure audio is not already playing
                    if (!source.isPlaying && Random.value <= playChance)
                    {
                        //i guess we were going to randomize zombie clips here
                        //guess it didn't happen
                        var clip = clips[Random.Range(0, clips.Length)];
                        //volume randomization
                        source.pitch = Random.Range(pitchMin, pitchMax);
                        //play audio
                        source.PlayOneShot(clip, volume);
                    }
                }
            }
            //wait before audio plays again
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }
}
