using System.Collections;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.UI;

public class AudioRecorder : MonoBehaviour
{
    public Button recordBtn;

    [Header("Recording Settings")]
    public string fileName = "recording";
    public int sampleRate = 16000;
    public bool autoSave = true;
    private int minRecordTime = 1;

    private AudioClip recordingClip;
    private bool isRecording = false;

    private string saveDirectory => Application.streamingAssetsPath;
    private int recordingPosition = 0;

    private string deviceName;
    private MemoryStream stream;

    private void Awake()
    {
        UpdateBtn();
        recordBtn.onClick.AddListener(() =>
        {
            if(isRecording)
                StopRecording();
            else
                StartRecording();
            UpdateBtn();
        });
    }

    private void UpdateBtn()
    {        
        Text comp = recordBtn.GetComponentInChildren<Text>();
        comp.text = isRecording ? "��ֹ" : "¼��";
    }


    void Update()
    {
        // ʵʱ����¼��λ��
        if (isRecording && Microphone.IsRecording(deviceName))
        {
            int curPos = Microphone.GetPosition(deviceName);
            if(curPos < recordingPosition)
            {
                //ѭ��
                float[] data = new float[recordingClip.samples - recordingPosition + curPos];
                recordingClip.GetData(data, recordingPosition);
                stream.Write(ConvertToPCM16(data));
            }
            else if(curPos > recordingPosition)
            {
                float[] data = new float[curPos - recordingPosition];
                recordingClip.GetData(data, recordingPosition);
                stream.Write(ConvertToPCM16(data));
            }
            recordingPosition = curPos;
        }
    }

    /// <summary>
    /// ��ʼ¼��
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("�Ѿ���¼����");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("����˷��豸");
            return;
        }

        //Ĭ���豸
        deviceName = Microphone.devices[0];

        // �����µ�AudioClip��ʹ�����ʱ����
        recordingClip = Microphone.Start(deviceName, true, minRecordTime, sampleRate);
        if (recordingClip == null)
        {
            Debug.LogError("�޷�������˷�");
            return;
        }

        isRecording = true;
        recordingPosition = 0;
        stream = new MemoryStream();

        Debug.Log("��ʼ¼��...");
    }

    /// <summary>
    /// ֹͣ¼��������
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("û����¼��");
            return;
        }

        // ֹͣ��˷�
        Microphone.End(deviceName);
        isRecording = false;

        // �Զ�����
        if (autoSave)
        {
            SaveRecording();
        }

        stream.Close();
        stream.Dispose();
        stream = null;
    }

    /// <summary>
    /// ����¼���ļ�
    /// </summary>
    private void SaveRecording()
    {
        if (recordingClip == null)
        {
            Debug.LogError("û��¼�����ݿɱ���");
            return;
        }

        try
        {
            // ת��ΪPCM�ֽ�����
            byte[] pcmData = stream.ToArray();

            // ����WAV�ļ�
            if (!Directory.Exists(saveDirectory))            
                Directory.CreateDirectory(saveDirectory);            
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string finalFileName = $"{fileName}_{timestamp}.wav";
            string fullPath = Path.Combine(saveDirectory, finalFileName);

            Wav wav = Wav.CreateWavFromPCM(
                recordingClip.channels,
                sampleRate,
                16,
                pcmData
            );
            wav.CreateAudioClip();

            // �����ļ�
            File.WriteAllBytes(fullPath, wav.ToFileBytes());

            Debug.Log($"¼���ѱ���: {fullPath}");
            Debug.Log($"�ļ���С: {pcmData.Length / 1024} KB");
            Debug.Log($"��Ƶʱ��: {GetAudioTimeStringWithMilliseconds(wav.AudioClip.length)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"����¼��ʧ��: {e.Message}");
        }
    }

    public string GetAudioTimeStringWithMilliseconds(float lengthInSeconds)
    {
        int minutes = Mathf.FloorToInt(lengthInSeconds / 60f);
        int seconds = Mathf.FloorToInt(lengthInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((lengthInSeconds * 1000) % 1000);
        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }

    /// <summary>
    /// ��float��Ƶ����ת��Ϊ16λPCM
    /// </summary>
    private byte[] ConvertToPCM16(float[] samples)
    {
        byte[] pcmData = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            // ���Ʒ�Χ��ת��Ϊshort
            short sampleValue = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);

            // С���ֽ���
            pcmData[i * 2] = (byte)(sampleValue & 0xFF);
            pcmData[i * 2 + 1] = (byte)((sampleValue >> 8) & 0xFF);
        }

        return pcmData;
    }

    /// <summary>
    /// ������Դ
    /// </summary>
    public void Cleanup()
    {
        if (isRecording)
        {
            StopRecording();
        }

        if (recordingClip != null)
        {
            Destroy(recordingClip);
            recordingClip = null;
        }
    }

    void OnDestroy()
    {
        Cleanup();
    }

    void OnApplicationQuit()
    {
        // Ӧ���˳�ʱ�Զ�ֹͣ¼��
        if (isRecording)
        {
            StopRecording();
        }
    }
}