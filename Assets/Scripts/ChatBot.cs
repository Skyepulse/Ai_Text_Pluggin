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
    public ChatBubble chatBubble;
    public PersonnalityCreator personnalityCreator;

    private string OpenAIkey = "My_KEY";
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

    public async Task<Tuple<string, int>> SendPromptToChatAsync(string userPrompt, int tokennum)
    {
        using (var client = new HttpClient())
        {
            client.BaseAddress = new System.Uri("https://api.openai.com/");

            // Set headers for the authorization token
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAIkey);

            // Construct the payload using the chat-based approach for gpt-3.5-turbo
            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
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
    }

  
    private void Start()
    {
        string path = Application.dataPath + "/../apikey.txt";
        if(File.Exists(path))
        {
            OpenAIkey = File.ReadAllText(path);
        }
        else
        {
            Debug.LogError("API key file not found!");
        }
        timer += time_per_character;
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
                    chatBubble.Hide();
                    if (remaining != "") showResponse(remaining);
                }
            } else
            {
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    timer += time_per_character;
                    characterCount++;
                    chatBubble.SetText(responseToDisplay.Substring(0, characterCount));
                }
                if (characterCount == responseToDisplay.Length) timer = time_whole_message;
            }
        }

        //called when pressing space
        if (Input.GetKeyDown(KeyCode.Space) && !_isWaitingForResponse)
        {
            StartCoroutine(sendPromptGeorge(0f));
        }
    }

    private void showResponse(string response)
    {
        chatBubble.Show();
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

    IEnumerator sendPromptGeorge(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Hello");
        chatBubble.StartThinking();
        string name = "George";
        string BaseMainTreat = "A grumpy old man that likes to talk about the good old days";
        string Relationship = "your grandson";
        personnalityCreator.createPersonnality(name, BaseMainTreat, Relationship);

        personnalityCreator.addEvent(PersonnalityCreator.Timing.Past, PersonnalityCreator.personnalityType.Action, "You were a soldier in the war");
        personnalityCreator.addEvent(PersonnalityCreator.Timing.Future, PersonnalityCreator.personnalityType.Opinion, "You are doubtful about the future");
        personnalityCreator.addEvent(PersonnalityCreator.Timing.Present, PersonnalityCreator.personnalityType.Information, "You are a retired teacher");

        int tokennum = 75;
        Tuple<string, int> prompt = personnalityCreator.createPrompt(new PersonnalityCreator.Timing[] { PersonnalityCreator.Timing.Past, PersonnalityCreator.Timing.Present, PersonnalityCreator.Timing.Future }, 3, "Hello grandpa, tell me a story!", "John", tokennum);

        Task.Run(async () =>
        {
            Tuple<string, int> response = await SendPromptToChatAsync(prompt.Item1, prompt.Item2);
            // Enqueue the UI update to run on the main thread.
            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                chatBubble.EndThinking();
                showResponse(response.Item1);
                Debug.Log(response.Item1);
            });
        });
    }
}