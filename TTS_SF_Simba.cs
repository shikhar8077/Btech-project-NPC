using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class TTS_SF_Simba : MonoBehaviour
{
    // Variables
    [SerializeField] private string SPEECHIFY_API_KEY;

    // Enum to select voice
    private enum SelectVoice
    {
        US_Henry, US_Carly, US_Kyle, US_Kristy, US_Oliver, US_Tasha, US_Joe, US_Lisa,
        US_George, US_Emily, US_Rob, GB_Russell, GB_Benjamin, GB_Michael, AU_KIM, IN_Ankit, IN_Arun,
        GB_Carol, GB_Helen, US_Julie, AU_Linda, US_Mark, US_Nick, NG_Elijah, GB_Beverly, GB_Collin,
        US_Erin, US_Jack, US_Jesse, US_Keenan, US_Lindsey, US_Monica, GB_Phil, GB_Declan, US_Stacy,
        GB_Archie, US_Evelyn, GB_Freddy, GB_Harper, US_Jacob, US_James, US_Mason, US_Victoria
    }

    [SerializeField] 
    private SelectVoice selectVoice;

    private const string TTS_API_URI = "https://api.sws.speechify.com/v1/audio/stream"; // POST URI for streaming API
    private string sfVoice;
    Animator avtAnimator;

    // Start is called before the first execution of Update
    void Start()
    {
        // Set the voice based on the selected enum
        avtAnimator = GetComponent<Animator>();
        sfVoice = selectVoice.ToString().Substring(3).ToLower(); // Ensuring lowercase format
        Debug.Log("You have selected voice: " + sfVoice);
        //Say("Welcome to sahayak");
    }

    // Public method to call TTS
    public void Say(string textInput)
    {
        StartCoroutine(PlayTTS(textInput));
    }

    // Coroutine to handle the TTS request and play the audio
    IEnumerator PlayTTS(string message)
    {
        // Prepare the JSON request data
        TextToSpeechData ttsData = new TextToSpeechData
        {
            input = SimpleCleanText(message),
            voice_id = sfVoice
        };

        string jsonPrompt = JsonUtility.ToJson(ttsData);

        // Web request setup
        UnityWebRequest request = new UnityWebRequest(TTS_API_URI, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPrompt)),
            downloadHandler = new DownloadHandlerAudioClip(TTS_API_URI, AudioType.MPEG)
        };

        // Set headerss
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "audio/mpeg");
        request.SetRequestHeader("Authorization", "Bearer " + SPEECHIFY_API_KEY);

        // Send the request and wait for response
        yield return request.SendWebRequest();

        // Handle the result of the request
        if (request.result == UnityWebRequest.Result.Success)
        {
            avtAnimator.SetBool("isTalking" ,  true);
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            Debug.Log("Audio clip successfully received, playing...");
            GetComponent<AudioSource>().PlayOneShot(clip);
            StartCoroutine(WaitForTalkingFinished());
        }
        else
        {
            // Handle failed request
            Debug.LogError($"TTS API Request failed: {request.error}");
            Debug.LogError($"Response Code: {request.responseCode}");
            Debug.LogError($"Response Body: {request.downloadHandler.text}");
        }
    }

    // Wait until audio has finished playing
    IEnumerator WaitForTalkingFinished()
    {
        while (GetComponent<AudioSource>().isPlaying)
        {
            yield return null;
        }
        avtAnimator.SetBool("isTalking", false);
        Debug.Log("Audio finished playing.");
    }

    // Class for the TTS data
    [System.Serializable]
    public class TextToSpeechData
    {
        public string input;
        public string voice_id;
    }

    // Clean up text by replacing special characters with readable words
    string SimpleCleanText(string msg)
    {
        StringBuilder result = new StringBuilder();

        foreach (char c in msg)
        {
            switch (c)
            {
                case '+': result.Append(" plus "); break;
                case ':': result.Append(", "); break;
                case '*': result.Append(", "); break;
                case '=': result.Append(" equals "); break;
                case '-': result.Append(" "); break;
                case '#': result.Append(" hash "); break;
                case '&': result.Append(" and "); break;
                default: result.Append(c); break;
            }
        }

        string cleanedMessage = result.ToString();
        Debug.Log("Cleaned message: " + cleanedMessage);
        return cleanedMessage;
    }
}
