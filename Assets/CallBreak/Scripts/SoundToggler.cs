using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundToggler : MonoBehaviour
{
    [SerializeField] private GameObject sound_ON;
    [SerializeField] private GameObject sound_OFF;

    void Start()
    {
        sound_ON.SetActive(true);
        sound_OFF.SetActive(false);
    }

    public void ToggleSound()
    {
        if (AudioManager.Instance.IsSoundEnabled)
        {
            sound_ON.SetActive(false);
            sound_OFF.SetActive(true);
            AudioManager.Instance.IsSoundEnabled = false;
        }
        else
        {
            sound_ON.SetActive(true);
            sound_OFF.SetActive(false);
            AudioManager.Instance.IsSoundEnabled = true;
        }
    }
}
