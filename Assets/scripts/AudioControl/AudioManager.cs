using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    [SerializeField] AudioSource[] audioSources;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    public void blocksuccess()
    {
        audioSources[0].Play();
    }
}
