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

public class ChatBot : MonoBehaviour
{
    public TMP_Text responseText;

    private string OpenAIkey = "My_KEY";

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
            HttpResponseMessage response = await client.PostAsync("v1/chat/completions", jsonPayload);

            // Read the response and return the result
            string jsonResponse = await response.Content.ReadAsStringAsync();   
            if (response.IsSuccessStatusCode)
            {
                OpenAIResponse apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(jsonResponse);
                string messageContent = apiResponse.choices[0].message.content.Trim();
                int tokensUsed = apiResponse.usage.completion_tokens; // Get the token count
                return Tuple.Create(messageContent, tokensUsed); // returning the assistant's message content
            }
            else
            {
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
    }
}