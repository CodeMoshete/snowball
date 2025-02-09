using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class PlayRandomSoundEffect : MonoBehaviour
{
    public List<string> SoundResources;
    private void Start()
    {
        int randomIndex = Random.Range(0, SoundResources.Count);
        string soundResourceName = SoundResources[randomIndex];
        AudioResource soundResource = Resources.Load<AudioResource>(soundResourceName);

        AudioSource audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.resource = soundResource;
        audioSource.Play();
    }
}
