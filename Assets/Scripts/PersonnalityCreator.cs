using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonnalityCreator : MonoBehaviour
{
    private string startPersonality = "Assume the personality of";
    private string contextPersonality = "with background the following:";
    private string endPoint = ".";
    private string startPrompt = "Answer to the following statement given by";

    public Tuple<Timing, personnalityType,  string[]>[] personnality;

    public enum Timing
    {
        Past,
        Present,
        Future,
    }

    public enum personnalityType
    {
        None,
        Action,
        Discussion,
        Emotion,
        Information,
        Opinion,
        Question,
        Reaction,
    }
}
