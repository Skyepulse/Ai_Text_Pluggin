using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChatBot : MonoBehaviour
{
    //Shown in the inspector
    public enum AiType
    {
        GPT3_5_turbo,
        Mistral,
        Mistral_Local_LLM
    }
    public enum textToSpeechType
    {
        openAI_tts,
        elevenLabs_tts
    }

    [SerializeField]
    private AiType AiApiChoice;
    public AiType ApiChoice
    {
        get
        {
            return AiApiChoice;
        }
        set
        {
            AiApiChoice = value;
        }
    }

    [SerializeField]
    private int PORT = 1234;

    [SerializeField]
    private bool textToSpeech;
    [SerializeField]
    private textToSpeechType textToSpeechChoice;
    [SerializeField]
    private string VoiceId;



    [SerializeField]
    private string BotName;
    [SerializeField] 
    private string YourName;
    [SerializeField]
    private string BaseMainTreat;
    [SerializeField]
    private string RelationshipToPlayer;

    
    [System.Serializable]
    public class Choice
    {
        public PersonnalityCreator.Timing Time;
        public PersonnalityCreator.personnalityType TypeOfEvent;
        public string ShortExplanation;
    }

    [SerializeField]
    private List<Choice> personnality;

    [SerializeField]
    private Camera followingCamera;
    
    
    private string prefabPath = "ChatBubble";
    private GameObject chatBubblePrefab;
    private ChatBubble chatBubbleScript;
    private PersonnalityCreator personnalityCreator;

    private string OpenAikey = "My_KEY";
    private string mistralKey = "My_KEY";
    private string elevenLabsKey = "My_KEY";
    private bool _isWaitingForResponse = false;

    private float time_per_character = 0.05f;
    private float time_whole_message = 1.5f;
    private float timer;
    private int maxNumCharacters = 100;
    private int characterCount = 0;
    private string response = "";
    private string responseToDisplay = "";

    private AudioSource audioSource;

    public class OpenAIResponse
    {
        public class Choice
        {
            public Message message { get; set; }
        }

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        public class Usage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
        }

        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
    }

    public async Task<Tuple<string, int>> SendPromptToChatAsync(string systemPrompt, string userPrompt, int tokennum)
    {
        if(AiApiChoice == AiType.GPT3_5_turbo)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("https://api.openai.com/");

                // Set headers for the authorization token
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAikey);

                // Construct the payload using the chat-based approach for gpt-3.5-turbo
                var payload = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                    temperature = 0.7,
                    max_tokens = tokennum
                };
                var jsonPayload = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                // Make the API call
                _isWaitingForResponse = true;
                HttpResponseMessage response = await client.PostAsync("v1/chat/completions", jsonPayload);

                // Read the response and return the result
                string jsonResponse = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    OpenAIResponse apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(jsonResponse);
                    string messageContent = apiResponse.choices[0].message.content.Trim();
                    int tokensUsed = apiResponse.usage.completion_tokens; // Get the token count
                    _isWaitingForResponse = false;
                    return Tuple.Create(messageContent, tokensUsed); // returning the assistant's message content
                }
                else
                {
                    _isWaitingForResponse = false;
                    throw new HttpRequestException($"Failed to retrieve data. Status code: {response.StatusCode}. Response: {jsonResponse}");
                }
            }
        } else if(AiApiChoice == AiType.Mistral)
        {
            //Mistral does not have a role system, so we group the two prompts together for the user prompt
            string mistralPrompt = "sys_prompt: " + systemPrompt + " prompt: " + userPrompt;
            using(var client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("https://api.mistral.ai/");

                // Set headers for the authorization token
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mistralKey);

                // Construct the payload using the chat-based approach for Mistral chat completion
                var payload = new
                {
                    model = "mistral-large-latest",
                    messages = new[]
                    {
                    new { role = "user", content = mistralPrompt }
                },
                    temperature = 0.7,
                    max_tokens = tokennum
                };
                var jsonPayload = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                // Make the API call
                _isWaitingForResponse = true;
                HttpResponseMessage response = await client.PostAsync("/v1/chat/completions", jsonPayload);

                // Read the response and return the result
                string jsonResponse = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    OpenAIResponse apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(jsonResponse);
                    string messageContent = apiResponse.choices[0].message.content.Trim();
                    Debug.Log("Mistral response: " + messageContent);                    
                    int tokensUsed = apiResponse.usage.completion_tokens; // Get the token count
                    _isWaitingForResponse = false;
                    return Tuple.Create(messageContent, tokensUsed); // returning the assistant's message content
                }
                else
                {
                    _isWaitingForResponse = false;
                    throw new HttpRequestException($"Failed to retrieve data. Status code: {response.StatusCode}. Response: {jsonResponse}");
                }
            }
        }
        else if(AiApiChoice == AiType.Mistral_Local_LLM)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("http://localhost:" + PORT.ToString());
                // Set headers for the authorization token
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Construct the payload using the chat-based approach for Mistral chat completion
                var payload = new
                {
                    messages = new[]
                    {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }

                },
                    temperature = 0.7,
                    max_tokens = tokennum
                };
                var jsonPayload = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                // Make the API call
                _isWaitingForResponse = true;
                HttpResponseMessage response = await client.PostAsync("/v1/chat/completions", jsonPayload);

                // Read the response and return the result
                string jsonResponse = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    OpenAIResponse apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(jsonResponse);
                    string messageContent = apiResponse.choices[0].message.content.Trim();
                    Debug.Log("Mistral local LLM response: " + messageContent);
                    int tokensUsed = apiResponse.usage.completion_tokens; // Get the token count
                    _isWaitingForResponse = false;
                    return Tuple.Create(messageContent, tokensUsed); // returning the assistant's message content
                }
                else
                {
                    _isWaitingForResponse = false;
                    throw new HttpRequestException($"Failed to retrieve data. Status code: {response.StatusCode}. Response: {jsonResponse}");
                }
            }
        }
        else
        {
            throw new Exception("No AI API selected");
        }   
    }

    public async Task<byte[]> GenerateSpeech(string message)
    {
        if(textToSpeechChoice == textToSpeechType.openAI_tts)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("https://api.openai.com/");

                // Set headers for the authorization token
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAikey);

                var payload = new
                {
                    model = "tts-1",
                    voice = "onyx",
                    input = message,
                    response_format = "pcm",
                    speed = 1.0
                };
                var jsonPayload = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("v1/audio/speech", jsonPayload);
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                if (response.IsSuccessStatusCode)
                {
                    
                    return audioBytes;
                }
                else
                {
                    throw new HttpRequestException($"Failed to retrieve data. Status code: {response.StatusCode}.");
                }
            }
        } else if(textToSpeechChoice == textToSpeechType.elevenLabs_tts)
        {
            if(VoiceId == "")
            {
                throw new Exception("No voice id selected for ElevenLabs TTS");
            }
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("https://api.elevenlabs.io/");

                // Set headers for the authorization token
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("xi-api-key", elevenLabsKey);

                var payload = new
                {
                    text = message,
                    voice_settings = new
                    {
                        similarity_boost = 0.5,
                        stability = 0.5
                    }
                };
                var url = $"v1/text-to-speech/{VoiceId}?output_format=pcm_24000";

                var jsonPayload = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(url, jsonPayload);
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    Debug.Log("Response: " + response);
                });
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                if (response.IsSuccessStatusCode)
                {
                    return audioBytes;
                }
                else
                {
                    throw new HttpRequestException($"Failed to retrieve data. Status code: {response.StatusCode}.");
                }
            }
        }
        else
        {
            throw new Exception("No textToSpeech API selected");
        }
    }

    private static AudioClip bytesToWav(byte[] pcmBytes, int sampleRate = 24000, int channels = 1)
    {
        int samples = pcmBytes.Length / 2; // 2 bytes per sample for 16-bit audio
        float[] audioData = new float[samples];

        // Convert byte array to float array
        for (int i = 0; i < samples; i++)
        {
            short sample = (short)((pcmBytes[i * 2 + 1] << 8) | pcmBytes[i * 2]);
            audioData[i] = sample / 32768.0f; // Normalize the 16-bit sample to [-1.0, 1.0]
        }

        // Create AudioClip
        AudioClip audioClip = AudioClip.Create("LoadedPCMClip", samples, channels, sampleRate, false);
        audioClip.SetData(audioData, 0);

        return audioClip;
    }

    private void Start()
    {
        string path1 = UnityEngine.Application.dataPath + "/../apikey.txt";
        
        if(File.Exists(path1))
        {
            OpenAikey = File.ReadAllText(path1);
        }
        else
        {
            Debug.LogError("API key file not found for OpenAi!");
        }

        string path2 = UnityEngine.Application.dataPath + "/../apikey2.txt";
        if(File.Exists(path2))
        {
               mistralKey = File.ReadAllText(path2);
        }
        else
        {
            Debug.LogError("API key file not found for Mistral!");
        }

        string path3 = UnityEngine.Application.dataPath + "/../apikey3.txt";
        if(File.Exists(path3))
        {
            elevenLabsKey = File.ReadAllText(path3);
        }
        else
        {
            Debug.LogError("API key file not found for ElevenLabs!");
        }
        timer += time_per_character;

        //Create audio source for text to speech
        audioSource = gameObject.GetComponent<AudioSource>();
        if(audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        //We instanciate a new personnality creator
        personnalityCreator = new PersonnalityCreator();
        personnalityCreator.createPersonnality(BotName, BaseMainTreat, RelationshipToPlayer);

        //We add the events to the personnality
        for(int i = 0; i < personnality.Count; i++)
        {
            personnalityCreator.addEvent(personnality[i].Time, personnality[i].TypeOfEvent, personnality[i].ShortExplanation);
        }

        //Instanciate a chat bubble from prefab as a child of the player named "ChatBubble"
        chatBubblePrefab = Resources.Load<GameObject>(prefabPath);
        if(chatBubblePrefab != null)
        {
            GameObject chatBubbleObject = Instantiate(chatBubblePrefab, this.transform.position, Quaternion.identity, this.transform);
            chatBubbleScript = chatBubbleObject.GetComponent<ChatBubble>();
            if(chatBubbleScript != null)
            {
                chatBubbleScript.Follower = this.gameObject;
                Canvas bubbleCanvas = chatBubbleObject.GetComponentInChildren<Canvas>();
                bubbleCanvas.worldCamera = followingCamera;
            }
            else
            {
                Debug.LogError("ChatBubble script not found!");
            }
        }
        else
        {
            Debug.LogError("ChatBubble prefab not found!");
        }

        Python_CScript.instance.chatBots.Add(BotName, this);
    }

    private void Update()
    {
        if (_isWaitingForResponse)
        {

        }
        
        if(responseToDisplay != "") // We have something left to write
        {
            if (characterCount == responseToDisplay.Length) //We finished displaying the whole message
            {
                timer -= Time.deltaTime;
                if(timer < 0)
                {
                    //We change the text to be the whole message minus what we just displayed, which is responseToDisplay
                    string remaining = response.Substring(characterCount);
                    responseToDisplay = "";
                    characterCount = 0;
                    chatBubbleScript.Hide();
                    if (remaining != "") showResponse(remaining);
                }
            } else
            {
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    timer += time_per_character;
                    characterCount++;
                    chatBubbleScript.SetText(responseToDisplay.Substring(0, characterCount));
                }
                if (characterCount == responseToDisplay.Length) timer = time_whole_message;
            }
        }

        if(Input.GetKeyDown(KeyCode.Z))
        {
            PlaySpeech();
        }
    }

    private void showResponse(string response)
    {
        chatBubbleScript.Show();
        timer = time_per_character;
        this.response = response;
        int numCharacters = response.Length;
        if(numCharacters > maxNumCharacters)
        {
            //We check if we are cutting something in two, if so we cut at the last space before the maxNumCharacters
            int lastSpace = response.Substring(0, maxNumCharacters).LastIndexOf(" ");
            string toShow = response.Substring(0, lastSpace);
            responseToDisplay = toShow;
        }
        else
        {
            string toShow = response;
            responseToDisplay = toShow;
        }
    }

    public void sendUserPrompt(string userPrompt)
    {
        StartCoroutine(sendPrompt(0.0f, userPrompt));
    }

    public void addDiscussionEvent(string discussion)
    {
        Debug.Log("Before adding length of list is: " + personnalityCreator.Personnality.Count.ToString());
        personnalityCreator.addEventFromDiscussion(discussion);
        Debug.Log("After adding length of list is: " + personnalityCreator.Personnality.Count.ToString());
    }

    IEnumerator sendPrompt(float delay, string userPrompt)
    {
        yield return new WaitForSeconds(delay);
        chatBubbleScript.StartThinking();
        

        int tokennum = 75;
        Tuple<string, int> prompt = personnalityCreator.createPrompt(new PersonnalityCreator.Timing[] { PersonnalityCreator.Timing.Past, PersonnalityCreator.Timing.Present, PersonnalityCreator.Timing.Future }, 3, YourName, tokennum);

        Task.Run(async () =>
        {
            try
            {
                var timeout = TimeSpan.FromSeconds(30);
                var task = SendPromptToChatAsync(prompt.Item1, userPrompt, prompt.Item2);
                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                {
                    Tuple<string, int> response = await task;
                    MainThreadDispatcher.ExecuteOnMainThread(() =>
                    {
                        if (AiApiChoice == AiType.Mistral || AiApiChoice == AiType.Mistral_Local_LLM)
                        {
                            response = Tuple.Create(cutMessage(response.Item1), response.Item2);
                        }
                        if (textToSpeech)
                        {
                            Debug.Log("Generating speech for " + BotName);
                            Task.Run(async () =>
                            {
                                try
                                {
                                    var speechTimeout = TimeSpan.FromSeconds(30);
                                    var speechTask = GenerateSpeech(response.Item1);
                                    if (await Task.WhenAny(speechTask, Task.Delay(speechTimeout)) == speechTask)
                                    {
                                        byte[] audioClipbytes = await speechTask;
                                        MainThreadDispatcher.ExecuteOnMainThread(() =>
                                        {
                                            Debug.Log("Generated speech for " + BotName + ". Transforming to audioclip...");
                                            AudioClip audioClip = bytesToWav(audioClipbytes);
                                            Debug.Log("Generated speech for " + BotName + ". Playing if press z...");
                                            audioSource.clip = audioClip;
                                            PlaySpeech();
                                            chatBubbleScript.EndThinking();
                                            showResponse(response.Item1);
                                            Python_CScript.instance.SendDataToClient(userPrompt, response.Item1, BotName);
                                        });
                                    }
                                    else
                                    {
                                        MainThreadDispatcher.ExecuteOnMainThread(() =>
                                        {
                                            chatBubbleScript.EndThinking();
                                            Debug.LogError("Generating speech timed out for " + BotName);
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MainThreadDispatcher.ExecuteOnMainThread(() =>
                                    {
                                        chatBubbleScript.EndThinking();
                                        Debug.LogError("Error generating speech: " + ex.Message);
                                    });
                                }
                            });
                        }
                        else { 
                            chatBubbleScript.EndThinking();
                            showResponse(response.Item1);
                            Python_CScript.instance.SendDataToClient(userPrompt, response.Item1, BotName);
                        }
                    });
                }
                else
                {
                    MainThreadDispatcher.ExecuteOnMainThread(() =>
                    {
                        chatBubbleScript.EndThinking();
                        Debug.LogError("Sending prompt timed out for " + BotName);
                    });
                }
            }
            catch (Exception ex)
            {
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    chatBubbleScript.EndThinking();
                    Debug.LogError("Error sending prompt to " + BotName +$": {ex.Message}");
                });
            }
        });
    }

    private void PlaySpeech()
    {
        if(audioSource.clip != null)
        {
            audioSource.Play();
        } else
        {
            Debug.LogError("No audio clip to play!");
        }
    }

    //Some API chatbots do not properly finish their sentences with token limits, so we need to cut the message to the last ponctuation
    string cutMessage(string message)
    {
        List<string> ponctuation = new List<string>{ ".", "!", "?", ";" }; //We consider these as ponctuation
        int lastPonctuation = -1;
        for(int i = message.Length - 1; i >= 0; i--)
        {
            if (ponctuation.Contains(message[i].ToString()))
            {
                lastPonctuation = i;
                break;
            }
        }
        if(lastPonctuation == -1)
        {
            return message;
        }
        else
        {
            return message.Substring(0, lastPonctuation + 1);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChatBot))]
public class ChatBotEditor : Editor
{
    SerializedProperty AiApiChoiceProp;
    SerializedProperty portProp;
    SerializedProperty textToSpeech;
    SerializedProperty textToSpeechType;
    SerializedProperty voiceId;
    SerializedProperty botNameProp;
    SerializedProperty yourNameProp;
    SerializedProperty baseMainTreatProp;
    SerializedProperty relationshipToPlayerProp;
    SerializedProperty personnalityProp;
    SerializedProperty followingCameraProp;

    private void OnEnable()
    {
        // Find properties
        AiApiChoiceProp = serializedObject.FindProperty("AiApiChoice");
        portProp = serializedObject.FindProperty("PORT");
        textToSpeech = serializedObject.FindProperty("textToSpeech");
        textToSpeechType = serializedObject.FindProperty("textToSpeechChoice");
        voiceId = serializedObject.FindProperty("VoiceId");
        botNameProp = serializedObject.FindProperty("BotName");
        yourNameProp = serializedObject.FindProperty("YourName");
        baseMainTreatProp = serializedObject.FindProperty("BaseMainTreat");
        relationshipToPlayerProp = serializedObject.FindProperty("RelationshipToPlayer");
        personnalityProp = serializedObject.FindProperty("personnality");
        followingCameraProp = serializedObject.FindProperty("followingCamera");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // Prepare object for modifications

        // Manually render each property
        EditorGUILayout.PropertyField(AiApiChoiceProp, new GUIContent("AI API Choice"));

        // Conditionally display PORT based on AiApiChoice
        if (AiApiChoiceProp.enumValueIndex == (int)ChatBot.AiType.Mistral_Local_LLM)
        {
            EditorGUILayout.PropertyField(portProp, new GUIContent("PORT"));
        }
        EditorGUILayout.PropertyField(textToSpeech, new GUIContent("Text To Speech"));
        if (textToSpeech.boolValue)
        {
            EditorGUILayout.PropertyField(textToSpeechType, new GUIContent("Text To Speech Choice"));
            if(textToSpeechType.enumValueIndex == (int)ChatBot.textToSpeechType.elevenLabs_tts)
            {
                EditorGUILayout.PropertyField(voiceId, new GUIContent("Voice Id"));
            }
        }

        // Display other fields
        EditorGUILayout.PropertyField(botNameProp, new GUIContent("Bot Name"));
        EditorGUILayout.PropertyField(yourNameProp, new GUIContent("Your Name"));
        EditorGUILayout.PropertyField(baseMainTreatProp, new GUIContent("Base Main Treat"));
        EditorGUILayout.PropertyField(relationshipToPlayerProp, new GUIContent("Relationship To Player"));
        EditorGUILayout.PropertyField(personnalityProp, new GUIContent("Personality Choices"));
        EditorGUILayout.PropertyField(followingCameraProp, new GUIContent("Following Camera"));

        serializedObject.ApplyModifiedProperties(); // Apply changes to the serialized object
    }
}
#endif