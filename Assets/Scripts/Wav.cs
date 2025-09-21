using System;
using System.Drawing;
using System.IO;
using System.Text;
using UnityEngine;

public class Wav
{
    private RIFFChunk riffChunk;
    private FormatChunk formatChunk;
    private DataChunk dataChunk;

    private int curPlayIndex = 0;

    public int channels => formatChunk.channels;
    public int sampleRate => formatChunk.sampleRate;
    public int bitsPerSample => formatChunk.bitsPerSample;

    private int dataByte { get { return formatChunk.bitsPerSample / 8; } }

    private int dataLength { get{ return (dataChunk.data.Length) / (dataByte * channels);}}

    public byte[] pcmData => dataChunk.data;


    private AudioClip audioClip;

    public AudioClip AudioClip { get { return audioClip; } }

    private string FileName
    {
        get
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss"); 
            return "temp_" + time;
        }
    }

    private Wav() { }

    /// <summary>
    /// 从.wav文件中加载
    /// </summary>
    /// <param name="data">音频文件数据</param>
    public static Wav ReadFromBytes(byte[] data)
    {
        Wav wav = new Wav();
        wav.ProcessData(data);
        wav.CreateAudioClip();
        return wav;
    }

    /// <summary>
    /// PCM编码转换成.wav
    /// </summary>
    /// <param name="sampleRate"></param>
    /// <param name="channels"></param>
    /// <param name="bitsPerSample"></param>
    /// <param name="pcm"></param>
    /// <returns></returns>
    public static Wav CreateWavFromPCM(int channels, int sampleRate, int bitsPerSample, byte[] pcm)
    {
        Wav wav = new Wav();
        //riff区块
        wav.riffChunk = new RIFFChunk();
        //format区块
        wav.formatChunk = new FormatChunk((short)channels, sampleRate, (short)bitsPerSample); 
        wav.riffChunk.size += wav.formatChunk.size + 8;
        //data区块
        wav.dataChunk = new DataChunk(pcm);
        wav.riffChunk.size += wav.dataChunk.size + 8;
        return wav;
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    public byte[] ToFileBytes()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            riffChunk.WriteBytes(stream);
            formatChunk.WriteBytes(stream);
            dataChunk.WriteBytes(stream);
            return stream.ToArray();
        }
    }


    #region Data Chunk Process
    private void ProcessData(byte[] fileData)
    {
        //RIFF Chunk
        riffChunk = new RIFFChunk(fileData, 0);
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
            if (subChunk.id == "fmt ")
            {
                formatChunk = new FormatChunk(fileData, index);
            }
            if (subChunk.id == "data")
            {
                dataChunk = new DataChunk(fileData, index);
                break;
            }

            int next = index + 8 + subChunk.size;
            index = next;
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
        string fileName = FileName;
        audioClip = AudioClip.Create(fileName, dataLength, channels, sampleRate, false, OnAudioRead, OnAudioSetPosition);
        curPlayIndex = 0;

        //Debug
        Debug.Log(
            $"Create Audio Clip: {fileName}" +
            $"\n\tformat: {formatChunk.id}" +
            $"\n\tchannel: {formatChunk.channels}" +
            $"\n\tsampleByte: {dataByte}" +
            $"\n\tsampleRate {formatChunk.sampleRate}" +
            $"\n\tsample: {dataLength}" +
            $"\n\ttime:{ (int)audioClip.length / 60} min {audioClip.length % 60} s" );
    }

    void OnAudioRead(float[] data)
    {
        int maxValue = (int)Mathf.Pow(2, formatChunk.bitsPerSample - 1);
        int sign = 1 << (formatChunk.bitsPerSample - 1);  //符号位

        for (int i = 0; i < data.Length; i++)
        {
            int dataIndex = curPlayIndex * dataByte;
            if (dataIndex < dataChunk.data.Length - 1)
            {
                int byteData = 0;
                for (int c = 0; c < dataByte; c++)
                {
                    int b = dataChunk.data[dataIndex + c];
                    byteData |= (b << (c * 8));
                }

                //符号位
                bool isNeg = (sign & byteData) != 0;
                //负数
                if (isNeg) byteData &= (~sign);
                float v = byteData / (float)maxValue;
                if (isNeg) v -= 1;
                data[i] = v;
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
        public string type;     //"WAVE"

        public RIFFChunk()
        {
            id = "RIFF";
            size = 0;
            type = "WAVE";
        }

        public RIFFChunk(byte[] file, int off)
        {
            id = Encoding.ASCII.GetString(file, off + 0, 4);
            size = BitConverter.ToInt32(file, off + 4);
            type = Encoding.ASCII.GetString(file, off + 8, 4);
        }

        public void WriteBytes(MemoryStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(id));
            stream.Write(BitConverter.GetBytes(size));
            stream.Write(Encoding.ASCII.GetBytes(type));
        }
    }

    /// <summary>
    /// SubChunk
    /// </summary>
    private class SubChunk
    {
        public string id;               //Chunk 类型
        public int size;                //Chunk 大小(不包含id、size字段的大小)

        public SubChunk() { }

        public SubChunk(byte[] file, int off)
        {
            id = Encoding.ASCII.GetString(file, off + 0, 4);
            size = BitConverter.ToInt32(file, off + 4);
        }

        public virtual void WriteBytes(MemoryStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(id));
            stream.Write(BitConverter.GetBytes(size));
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


        public FormatChunk(short channels, int sampleRate, short bitsPerSample)
        {
            id = "fmt ";
            format = 1;
            size = 16;
            this.channels = channels;
            this.sampleRate = sampleRate;
            byteRate = sampleRate * channels * (bitsPerSample / 8);
            blockAlign = (short)(channels * (bitsPerSample / 8));
            this.bitsPerSample = bitsPerSample;
        }


        public FormatChunk(byte[] file, int off) : base(file, off)
        {
            format = BitConverter.ToInt16(file, off +  8);
            channels = BitConverter.ToInt16(file, off + 10);
            sampleRate = BitConverter.ToInt32(file, off + 12);
            byteRate = BitConverter.ToInt32(file, off + 16);
            blockAlign = BitConverter.ToInt16(file, off + 20);
            bitsPerSample = BitConverter.ToInt16(file, off + 22);
        }

        public override void WriteBytes(MemoryStream stream)
        {
            base.WriteBytes(stream);
            stream.Write(BitConverter.GetBytes(format));
            stream.Write(BitConverter.GetBytes(channels));
            stream.Write(BitConverter.GetBytes(sampleRate));
            stream.Write(BitConverter.GetBytes(byteRate));
            stream.Write(BitConverter.GetBytes(blockAlign));
            stream.Write(BitConverter.GetBytes(bitsPerSample));
        }
    }

    private class DataChunk : SubChunk
    {
        public byte[] data;

        public DataChunk(byte[] pcm)
        {
            id = "data";
            size = pcm.Length;
            data = pcm;
        }

        public DataChunk(byte[] file, int off) : base(file, off) 
        {
            data = new byte[size];
            Array.Copy(file, off + 8, data, 0, size);
        }

        public override void WriteBytes(MemoryStream stream)
        {
            base.WriteBytes(stream);
            stream.Write(data);
        }
    }
    #endregion

    #region Tools
    //(-1, 1)
    private static float BytesToFloat(byte first, byte second)
    {
        short s = (short)((second << 8) | first);
        return s / 32768.0f;
    }
    #endregion
}
