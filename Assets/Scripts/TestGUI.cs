using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TestGUI : MonoBehaviour
{
    private Button testBtn;
    private AudioSource audioSource;

    private void Awake()
    {
        testBtn = transform.Find("TestBtn").GetComponent<Button>();
        audioSource = transform.GetComponent<AudioSource>();

        testBtn.onClick.AddListener(PlayAudio);
    }

    private void PlayAudio()
    {
        byte[] data = File.ReadAllBytes(Application.dataPath + "/Audio/test.wav");
        Wav wav = new Wav(data);
        wav.ProcessData();
        wav.CreateAudioClip();
        audioSource.clip = wav.AudioClip;
        audioSource.loop = false;
        audioSource.Play();
    }
}
