using System;
using System.Text;
using UnityEngine;

public class Wav
{
    private byte[] fileData;
    private RIFFChunk riffChunk;
    private FormatChunk formatChunk;
    private DataChunk dataChunk;

    private int dataStartIndex;
    private int curPlayIndex = 0;

    private AudioClip audioClip;

    public AudioClip AudioClip { get { return audioClip; } }

    private string FileName
    {
        get
        {
            string time = System.DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss");
            return "temp_" + time;
        }
    }

    public Wav(byte[] data)
    {
        fileData = data;
    }

    #region Data Chunk Process
    public void ProcessData()
    {
        //RIFF Chunk
        riffChunk = new RIFFChunk(fileData);
        if (riffChunk.id != "RIFF")
        {
            Debug.LogError("File is not RIFF file:" + riffChunk.id);
            return;
        }
        if (riffChunk.type != "WAVE")
        {
            Debug.LogError("File is not WAVE file:" + riffChunk.type);
            return;
        }

        //Read Chunks
        int index = 12;
        do
        {
            SubChunk subChunk = new SubChunk(fileData, index);
            if (subChunk.id == "JUNK")
            {
                //JUNK Chunk，Skip
            }
            if (subChunk.id == "fmt ")
            {
                formatChunk = new FormatChunk(fileData, index);
            }
            if (subChunk.id == "data")
            {
                dataChunk = new DataChunk(fileData, index);
                dataStartIndex = index + 8;
                break;
            }
            index = subChunk.next;
        } while (index < fileData.Length);
    }
    #endregion

    #region Create AudioClip
    public void CreateAudioClip()
    {
        if (riffChunk == null || formatChunk == null)
        {
            Debug.LogError("No wav data to create AudioClip");
            return;
        }

        //Creat Audio Clip
        int channels = formatChunk.channels;
        int dataLength = (fileData.Length - dataStartIndex) / (2 * channels);
        int sampleRate = formatChunk.sampleRate;
        string fileName = FileName;
        audioClip = AudioClip.Create(fileName, dataLength, channels, sampleRate, false, OnAudioRead, OnAudioSetPosition);
        curPlayIndex = 0;

        //Debug
        Debug.Log(
            $"Create Audio Clip: {fileName}" +
            $"\n\tformat: {formatChunk.id}" +
            $"\n\tchannel: {formatChunk.channels}" +
            $"\n\tsampleRate {formatChunk.sampleRate}" +
            $"\n\ttotal: {fileData.Length}" +
            $"\n\tsample: {dataLength}" +
            $"\n\tstartIndex: {dataStartIndex}");
    }

    void OnAudioRead(float[] data)
    {
        for(int i = 0; i < data.Length; i++)
        {
            int dataIndex = dataStartIndex + curPlayIndex * 2;
            if (dataIndex < fileData.Length - 1)
            {
                data[i] = BytesToFloat01(fileData[dataIndex], fileData[dataIndex + 1]);
                curPlayIndex++;
            }
            else
            {
                data[i] = 0;
            }
        }
    }

    void OnAudioSetPosition(int newIndex)
    {
        curPlayIndex = newIndex;
    }
    #endregion

    #region Chunks
    /// <summary>
    /// RIFF头部Chunk
    /// </summary>
    private class RIFFChunk
    {
        public string id;       //"RIFF"
        public int size;        //size:（fileSize - id - size）
        public string type;     //"AVI" or "WAV"

        public RIFFChunk(byte[] data, int off = 0)
        {
            id      = ToASCIIString(data, off + 0);
            size    = ToInt32(data, off + 4, false);
            type    = ToASCIIString(data, off + 8);
        }
    }

    /// <summary>
    /// SubChunk
    /// </summary>
    private class SubChunk
    {
        public string id;               //Chunk 类型
        public int size;                //Chunk 大小
        public int next;                //下一位索引

        public SubChunk(byte[] data, int off)
        {
            id = ToASCIIString(data, off + 0);
            size = ToInt32(data, off + 4, false);
            next = off + 8 + size;
        }
    }

    /// <summary>
    /// "fmt " Chunk
    /// </summary>
    private class FormatChunk : SubChunk
    {
        public short format;            //音频格式：PCM = 1
        public short channels;          //声道数
        public int sampleRate;          //采样率
        public int byteRate;            //每秒字节数
        public short blockAlign;        //区块对齐
        public short bitsPerSample;     //采样位数

        public FormatChunk(byte[] data, int off = 12) : base(data, off)
        {
            format          = ToInt16(data, off +  8, false);
            channels        = ToInt16(data, off + 10, false);
            sampleRate      = ToInt32(data, off + 12, false);
            byteRate        = ToInt32(data, off + 16, false);
            blockAlign      = ToInt16(data, off + 20, false);
            bitsPerSample   = ToInt16(data, off + 22, false);
        }
    }

    private class DataChunk : SubChunk
    {
        public DataChunk(byte[] data, int off = 36) : base(data, off) { }
    }
    #endregion

    #region Tools
    private static float BytesToFloat01(byte first, byte second)
    {
        short s = (short)((second << 8) | first);
        return s / 32768.0f;
    }

    public static string ToASCIIString(byte[] data, int index, int cnt = 4)
    {
        return ASCIIEncoding.ASCII.GetString(data, index, cnt);
    }

    private static short ToInt16(byte[] data, int index, bool reverse = false)
    {
        short v;
        if (reverse)
        {
            v = (short)((data[index + 1]) | (data[index]) << 8);
        }
        else
        {
            v = BitConverter.ToInt16(data, index);
        }
        return v;
    }


    private static int ToInt32(byte[] data, int index, bool reverse = false)
    {
        int v;
        if (reverse)
        {
            v =   (data[index + 3])
                | (data[index + 2]  << 8)
                | (data[index + 1]  << 16)
                | (data[index]      << 24);
        }
        else
        {
            v = BitConverter.ToInt32(data, index);
        }
        return v;
    }
    #endregion
}
