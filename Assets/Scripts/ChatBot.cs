using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System;
using UnityEngine.Experimental.Rendering;
using System.Collections;

public class ChatBot : MonoBehaviour
{
    //Shown in the inspector
    public enum AiType
    {
        GPT3_5_turbo,
        Mistral
    }

    [SerializeField]
    private AiType AiApiChoice;

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

    private string key = "My_KEY";
    private bool _isWaitingForResponse = false;

    private float time_per_character = 0.05f;
    private float time_whole_message = 1.5f;
    private float timer;
    private int maxNumCharacters = 100;
    private int characterCount = 0;
    private string response = "";
    private string responseToDisplay = "";

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
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

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
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

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

  
    private void Start()
    {
        string path;
        if(AiApiChoice == AiType.GPT3_5_turbo)
        {
            path = Application.dataPath + "/../apikey.txt";
        }
        else if(AiApiChoice == AiType.Mistral)
        {
            path = Application.dataPath + "/../apikey2.txt";
        } else
        {
            path = Application.dataPath + "/../apikey.txt";
        }
        if(File.Exists(path))
        {
            key = File.ReadAllText(path);
        }
        else
        {
            Debug.LogError("API key file not found!");
        }
        timer += time_per_character;

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
}