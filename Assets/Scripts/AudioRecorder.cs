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
        comp.text = isRecording ? "终止" : "录制";
    }


    void Update()
    {
        // 实时更新录音位置
        if (isRecording && Microphone.IsRecording(deviceName))
        {
            int curPos = Microphone.GetPosition(deviceName);
            if(curPos < recordingPosition)
            {
                //循环
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
    /// 开始录音
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("已经在录音中");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("无麦克风设备");
            return;
        }

        //默认设备
        deviceName = Microphone.devices[0];

        // 创建新的AudioClip（使用最大时长）
        recordingClip = Microphone.Start(deviceName, true, minRecordTime, sampleRate);
        if (recordingClip == null)
        {
            Debug.LogError("无法启动麦克风");
            return;
        }

        isRecording = true;
        recordingPosition = 0;
        stream = new MemoryStream();

        Debug.Log("开始录音...");
    }

    /// <summary>
    /// 停止录音并保存
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("没有在录音");
            return;
        }

        // 停止麦克风
        Microphone.End(deviceName);
        isRecording = false;

        // 自动保存
        if (autoSave)
        {
            SaveRecording();
        }

        stream.Close();
        stream.Dispose();
        stream = null;
    }

    /// <summary>
    /// 保存录音文件
    /// </summary>
    private void SaveRecording()
    {
        if (recordingClip == null)
        {
            Debug.LogError("没有录音数据可保存");
            return;
        }

        try
        {
            // 转换为PCM字节数据
            byte[] pcmData = stream.ToArray();

            // 创建WAV文件
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

            // 保存文件
            File.WriteAllBytes(fullPath, wav.ToFileBytes());

            Debug.Log($"录音已保存: {fullPath}");
            Debug.Log($"文件大小: {pcmData.Length / 1024} KB");
            Debug.Log($"音频时长: {GetAudioTimeStringWithMilliseconds(wav.AudioClip.length)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存录音失败: {e.Message}");
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
    /// 将float音频数据转换为16位PCM
    /// </summary>
    private byte[] ConvertToPCM16(float[] samples)
    {
        byte[] pcmData = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            // 限制范围并转换为short
            short sampleValue = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);

            // 小端字节序
            pcmData[i * 2] = (byte)(sampleValue & 0xFF);
            pcmData[i * 2 + 1] = (byte)((sampleValue >> 8) & 0xFF);
        }

        return pcmData;
    }

    /// <summary>
    /// 清理资源
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
        // 应用退出时自动停止录音
        if (isRecording)
        {
            StopRecording();
        }
    }
}