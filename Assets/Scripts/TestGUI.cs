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
            Debug.LogError("找不到音频文件:" + path);
            return;
        }
        byte[] data = File.ReadAllBytes(path);
        Wav wav = Wav.ReadFromBytes(data);
        audioSource.clip = wav.AudioClip;
        audioSource.loop = false;
        audioSource.Play();

        //创建新文件
        Wav newWav = Wav.CreateWavFromPCM(wav.channels, wav.sampleRate, wav.bitsPerSample, wav.pcmData);

        string fileName = Path.GetFileNameWithoutExtension(path);
        string fileDic = Path.GetDirectoryName(path);
        string newFile = Path.Combine(fileDic, fileName + "_new.wav");
        Debug.Log("重新写入文件：" + newFile);
        File.WriteAllBytes(newFile, newWav.ToFileBytes());
    }
}
