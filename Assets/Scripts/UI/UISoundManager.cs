using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public AudioSource clickSoundSource;

    public void PlayClickSound()
    {
        clickSoundSource.Play();
    }
}
