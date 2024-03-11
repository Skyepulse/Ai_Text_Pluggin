using UnityEngine;
using System.IO;
using System;

public class VoiceRecorder : MonoBehaviour
{
    private bool isRecording = false;
    private AudioClip recording;
    private string microphoneName;
    private string saveFilePath;
    private string filename = "playerVoice.wav";
    [HideInInspector]
    public bool hasFinishedSaving = false;
    [HideInInspector]
    public string Filename
    {
        get { return filename; }
        set { filename = value; }
    }
    [HideInInspector]
    public string SaveFilePath
    {
        get { return saveFilePath; }
        set { saveFilePath = value; }
    }
    private float startRecordingTime;
    private int defaultrecordingTime = 6;
    private float timer = 0;
    private float adjustVolume = 4.0f;

    private void Start()
    {
        saveFilePath = Application.temporaryCachePath + "/";
    }

    public void StartRecording()
    {
        if(Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found");
            return;
        }
        else if(Microphone.devices.Length > 0)
        {
            microphoneName = Microphone.devices[0];
            recording = Microphone.Start(microphoneName, true, defaultrecordingTime, 44100);
            isRecording = true;
            startRecordingTime = Time.time;
            hasFinishedSaving = false;
            Debug.Log("Recording started...");
        }
    }

    public bool StopRecordingAndSave()
    {
        if(isRecording)
        {
            Microphone.End(microphoneName);
            isRecording = false;
            timer = 0;
            Debug.Log("Recording stopped...");
            float timelength = Time.time - startRecordingTime;

            bool hasSaved = SaveRecording(filename, timelength);
            return hasSaved;
        }
        if(hasFinishedSaving)
        {
            hasFinishedSaving = false;
            return true;
        }
        return false;
    }

    public bool SaveRecording(string filename, float timelength)
    {
        AudioClip recordingTrimmed = TrimAudioClip(recording, timelength);
        bool success = SaveWav.save(saveFilePath + filename, recordingTrimmed, adjustVolume);
        if(success)
        {
            hasFinishedSaving = true;
            Debug.Log("Recording saved...");
        }
        else
        {
            throw new Exception("Failed to save recording");
        }
        return success;
    }

    AudioClip TrimAudioClip(AudioClip clip, float length)
    {
        if(length > defaultrecordingTime)
        {
            return clip;
        }
        int samplesLength = (int)(length * clip.frequency);
        float[] data = new float[samplesLength * clip.channels];
        clip.GetData(data, 0);

        AudioClip trimmedClip = AudioClip.Create(clip.name + "_trimmed", samplesLength, clip.channels, clip.frequency, false);
        trimmedClip.SetData(data, 0);

        return trimmedClip;
    }

    private void Update()
    {
        if (isRecording)
        {
            timer += Time.deltaTime;
            if(timer >= defaultrecordingTime)
            {
                StopRecordingAndSave();
            }
        }
    }
}

public static class SaveWav
{
    const int HEADER_SIZE = 44;

    public static bool save(string filepath, AudioClip clip, float volumeMultiplier = 1.0f)
    {
        if(!filepath.ToLower().EndsWith(".wav"))
        {
            filepath += ".wav";
        }

        using(var fileStream = CreateEmpty(filepath))
        {
            ConvertAndWrite(fileStream, clip, volumeMultiplier);
            WriteHeader(fileStream, clip);
        }

        return true;
    }

    public static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for(int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    public static void ConvertAndWrite(FileStream fileStream, AudioClip clip, float volumeMultiplier)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];

        int rescaleFactor = 32767; // to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            float amplifiedSample = samples[i] * volumeMultiplier; // Apply volume amplification
            // Clamping the values to prevent clipping
            amplifiedSample = Mathf.Clamp(amplifiedSample, -1.0f, 1.0f);
            intData[i] = (short)(amplifiedSample * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    public static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 two = 1;
        UInt16 one = (ushort)channels;
        fileStream.Write(BitConverter.GetBytes(two), 0, 2);
        fileStream.Write(BitConverter.GetBytes(one), 0, 2);

        fileStream.Write(BitConverter.GetBytes(hz), 0, 4);
        fileStream.Write(BitConverter.GetBytes(hz * channels * 2), 0, 4); // byte rate
        UInt16 four = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(four), 0, 2);
        fileStream.Write(BitConverter.GetBytes((UInt16)16), 0, 2); // block align

        Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}
