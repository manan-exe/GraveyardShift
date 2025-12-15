using System.Collections;
using UnityEngine;

public class ZombieAmbientAudio : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource source;
    public AudioClip[] clips;

    [Header("Timing")]
    public float minDelay = 3f;
    public float maxDelay = 9f;
    [Range(0f, 1f)] public float playChance = 0.8f; // chance each cycle plays a sound

    [Header("Variation")]
    [Range(0f, 1f)] public float volume = 0.8f;
    public float pitchMin = 0.95f;
    public float pitchMax = 1.08f;

    void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        // small offset so a wave of zombies doesn't all play at once
        yield return new WaitForSeconds(Random.Range(0.2f, 1.2f));

        while (true)
        {
            // only do this during gameplay (optional but recommended)
            if (GameFlowManager.GameplayActive && !GameFlowManager.IsPaused)
            {
                if (source != null && clips != null && clips.Length > 0)
                {
                    if (!source.isPlaying && Random.value <= playChance)
                    {
                        var clip = clips[Random.Range(0, clips.Length)];
                        source.pitch = Random.Range(pitchMin, pitchMax);
                        source.PlayOneShot(clip, volume);
                    }
                }
            }

            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }
}
