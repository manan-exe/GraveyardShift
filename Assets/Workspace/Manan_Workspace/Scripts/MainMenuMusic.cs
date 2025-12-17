using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class MainMenuMusic : MonoBehaviour
{
    //handles main menu background music
    //put this on a random empty game object in main menu
    void Start() {
        //reference to source of audio
        AudioSource a = GetComponent<AudioSource>();
        //want to loop it
        a.loop = true;
        //don't need 3d audio
        a.spatialBlend = 0f;
        //play sound
        a.Play();
    }
}
