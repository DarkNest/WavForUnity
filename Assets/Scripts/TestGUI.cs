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
    }

    public void PlayAudio(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("�Ҳ�����Ƶ�ļ�:" + path);
            return;
        }
        byte[] data = File.ReadAllBytes(path);
        Wav wav = Wav.ReadFromBytes(data);
        audioSource.clip = wav.AudioClip;
        audioSource.loop = false;
        audioSource.Play();

        //�������ļ�
        Wav newWav = Wav.CreateWavFromPCM(wav.channels, wav.sampleRate, wav.bitsPerSample, wav.pcmData);

        string fileName = Path.GetFileNameWithoutExtension(path);
        string fileDic = Path.GetDirectoryName(path);
        string newFile = Path.Combine(fileDic, fileName + "_new.wav");
        Debug.Log("����д���ļ���" + newFile);
        File.WriteAllBytes(newFile, newWav.ToFileBytes());
    }
}
