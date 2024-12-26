using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Interaction.Toolkit;

public class STT_HF_OpenAI : MonoBehaviour
{
    [SerializeField]
    private string HF_INF_API_KEY;

    private const string STT_API_URI = "https://api-inference.huggingface.co/models/openai/whisper-large-v3-turbo";

    [SerializeField]
    private LLM_Groq llmGroq;

    private MemoryStream stream;

    public void SelectEventHandler(SelectEnterEventArgs eventArgs)
    {
        StartSpeaking();
    }

    public void SelectExitEventHandler(SelectExitEventArgs eventArgs)
    {
        Microphone.End(null);
    }
    public void StartSpeaking()
    {
        stream = new MemoryStream();
        AudioSource aud = GetComponent<AudioSource>();

        Debug.Log("Start Recording");
        aud.clip = Microphone.Start(null, false, 30, 11025);

        StartCoroutine(RecordAudio(aud.clip));
    }

    private IEnumerator RecordAudio(AudioClip clip)
    {
        while (Microphone.IsRecording(null))
        {
            yield return null;
        }

        AudioSource aud = GetComponent<AudioSource>();
        ConvertClipToWav(aud.clip);
        StartCoroutine(STT());
    }

    private IEnumerator STT()
    {
        // Create the request for the Hugging Face API
        UnityWebRequest request = new UnityWebRequest(STT_API_URI, "POST");

        // Audio must be converted to WAV before this is called!
        request.uploadHandler = new UploadHandlerRaw(stream.GetBuffer());
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + HF_INF_API_KEY);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            SpeechToTextData sttResponse = JsonUtility.FromJson<SpeechToTextData>(responseText);
            Debug.Log(sttResponse.text);

            if (llmGroq)
            {
                llmGroq.TextToLLM(sttResponse.text);
            }
        }
        else
        {
            Debug.LogError("API request failed: " + request.error);
        }
    }

    private Stream ConvertClipToWav(AudioClip clip)
    {
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);

        // Cleanup and prepare a fresh stream
        if (stream != null)
        {
            stream.Dispose();
        }

        stream = new MemoryStream();

        ushort bitsPerSample = 16;
        const string chunkID = "RIFF";
        const string format = "WAVE";
        const string subChunk1ID = "fmt ";
        uint subChunk1Size = 16;
        ushort audioFormat = 1;
        ushort numChannels = (ushort)clip.channels;
        uint sampleRate = (uint)clip.frequency;
        uint byteRate = (uint)(sampleRate * clip.channels * bitsPerSample / 8);
        ushort blockAlign = (ushort)(numChannels * bitsPerSample / 8);
        const string subChunk2ID = "data";
        uint subChunk2Size = (uint)(data.Length * clip.channels * bitsPerSample / 8);
        uint chunkSize = 36 + subChunk2Size;

        WriteString(stream, chunkID);
        WriteUInt(stream, chunkSize);
        WriteString(stream, format);
        WriteString(stream, subChunk1ID);
        WriteUInt(stream, subChunk1Size);
        WriteShort(stream, audioFormat);
        WriteShort(stream, numChannels);
        WriteUInt(stream, sampleRate);
        WriteUInt(stream, byteRate);
        WriteShort(stream, blockAlign);
        WriteShort(stream, bitsPerSample);
        WriteString(stream, subChunk2ID);
        WriteUInt(stream, subChunk2Size);

        foreach (var sample in data)
        {
            // De-normalize the samples to 16 bits.
            short deNormalizedSample = (short)(sample * short.MaxValue);
            WriteShort(stream, (ushort)deNormalizedSample);
        }

        return stream;
    }

    // Helper functions to send data into the stream
    private void WriteUInt(Stream stream, uint data)
    {
        stream.WriteByte((byte)(data & 0xFF));
        stream.WriteByte((byte)((data >> 8) & 0xFF));
        stream.WriteByte((byte)((data >> 16) & 0xFF));
        stream.WriteByte((byte)((data >> 24) & 0xFF));
    }

    private void WriteShort(Stream stream, ushort data)
    {
        stream.WriteByte((byte)(data & 0xFF));
        stream.WriteByte((byte)((data >> 8) & 0xFF));
    }

    private void WriteString(Stream stream, string value)
    {
        foreach (var character in value)
        {
            stream.WriteByte((byte)character);
        }
    }

    [System.Serializable]
    private class SpeechToTextData
    {
        public string text;
    }
}
