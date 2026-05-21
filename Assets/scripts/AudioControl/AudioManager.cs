using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    [SerializeField] AudioSource[] audioSources;
    [SerializeField] AudioClip[] audioClips;
    
    private Dictionary<string, AudioClip> audioLibrary;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        InitializeAudioLibrary();
    }
    
    private void InitializeAudioLibrary()
    {
        audioLibrary = new Dictionary<string, AudioClip>();
        
        foreach (AudioClip clip in audioClips)
        {
            audioLibrary[clip.name] = clip;
        }
    }
    
    public void blocksuccess()
    {
        audioSources[0].Play();
    }
    

    public void playSFX(string name)
    {
        if (audioLibrary.ContainsKey(name))
        {
            audioSources[0].PlayOneShot(audioLibrary[name]);
        }
        else
        {
            Debug.LogWarning($"找不到音效: {name}");
        }
    }
}
