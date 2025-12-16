using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MainMenuMusic : MonoBehaviour
{
    //save
    void Start() {
        AudioSource a = GetComponent<AudioSource>();
        a.loop = true;
        a.spatialBlend = 0f; // 2D
        a.Play();
    }
}
