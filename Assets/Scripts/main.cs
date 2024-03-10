using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class main : MonoBehaviour
{
    [SerializeField]
    public GameObject george;

    private void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChatBot chatBot = george.GetComponent<ChatBot>();
            chatBot.sendUserPrompt("Hello grandad how are you? I've been meaning to ask you what your role was during the second world war. What do you think of AI nowadays?");
        }
        */

        if(Input.GetKeyDown(KeyCode.E))
        {
            ChatBot chatBot = george.GetComponent<ChatBot>();
            chatBot.startRecording();
        }

        if(Input.GetKeyUp(KeyCode.E))
        {
            ChatBot chatBot = george.GetComponent<ChatBot>();
            chatBot.stopRecordingAndSend();
        }
    }

}
