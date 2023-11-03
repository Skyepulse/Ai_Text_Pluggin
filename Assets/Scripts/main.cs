using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class main : MonoBehaviour
{
    public PersonnalityCreator personnalityCreator;
    public ChatBot chatBot;
    // Start is called before the first frame update
    void Start()
    {
        string name = "George";
        string BaseMainTreat = "A grumpy old man that likes to talk about the good old days";
        string Relationship = "your grandson";
        personnalityCreator.createPersonnality(name, BaseMainTreat, Relationship);

        personnalityCreator.addEvent(PersonnalityCreator.Timing.Past, PersonnalityCreator.personnalityType.Action, "You were a soldier in the war");
        personnalityCreator.addEvent(PersonnalityCreator.Timing.Future, PersonnalityCreator.personnalityType.Opinion, "You are doubtful about the future");
        personnalityCreator.addEvent(PersonnalityCreator.Timing.Present, PersonnalityCreator.personnalityType.Information, "You are a retired teacher");

        int tokennum = 75;
        Tuple<string, int> prompt = personnalityCreator.createPrompt(new PersonnalityCreator.Timing[] { PersonnalityCreator.Timing.Past, PersonnalityCreator.Timing.Present, PersonnalityCreator.Timing.Future }, 3, "Hello grandpa, tell me a story!", "John", tokennum);
        Debug.Log(prompt.Item1 + " " + prompt.Item2.ToString());
        Task.Run(async () =>
        {
            Tuple<string, int> response = await chatBot.SendPromptToChatAsync(prompt.Item1, prompt.Item2);
            Debug.Log(response.Item1);
            // Enqueue the UI update to run on the main thread.
            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                chatBot.responseText.text = response.Item1;
                Debug.Log(response.Item2.ToString());
            });
        });


    }
}
