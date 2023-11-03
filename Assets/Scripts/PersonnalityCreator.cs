using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonnalityCreator : MonoBehaviour
{
    private string startPersonality = "Assume the personality of";
    private string contextPersonality = " with background the following";
    private string startPrompt = "Answer to the following statement given by";

    public List<Tuple<Timing, personnalityType,  string>> personnality = new List<Tuple<Timing, personnalityType, string>>();
    public string[] basicTreats = new string[3];

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

    public Tuple<string, int> createPrompt(Timing[] times, int numMaxContext, string playerStatement, string PlayerName, int tokennum)
    {
        if (basicTreats[0] == null || basicTreats[1] == null || times.Length == 0 || numMaxContext > personnality.Count || tokennum == 0)
        {
            Debug.LogError("PersonnalityCreator: basicTreats not set");
            return null;
        }
        string fstring = "";
        fstring += startPersonality;
        fstring += " " + basicTreats[0] + ", " + basicTreats[1];
        fstring += contextPersonality + " " + numMaxContext.ToString() + " things: ";
        List<int> randoms = GetNDistinctRandoms(numMaxContext, this.personnality.Count);
        foreach (int i in randoms)
        {
            switch(personnality[i].Item1)
            {
                case Timing.Past:
                    fstring += "In the past, ";
                    break;
                case Timing.Present:
                    fstring += "In the present, ";
                    break;
                case Timing.Future:
                    fstring += "In the future, ";
                    break;
            }

            switch (personnality[i].Item2)
            {
                case personnalityType.None:
                    fstring += "nothing happened.";
                    break;
                case personnalityType.Action:
                    fstring += "an action happened, ";
                    break;
                case personnalityType.Discussion:
                    fstring += "a discussion happened, ";
                    break;
                case personnalityType.Emotion:
                    fstring += "an emotion was felt by you, ";
                    break;
                case personnalityType.Information:
                    fstring += " ";
                    break;
                case personnalityType.Opinion:
                    fstring += "you believe ";
                    break;
                case personnalityType.Question:
                    fstring += "a question is/was asked, ";
                    break;
                case personnalityType.Reaction:
                    fstring += "a reaction: ";
                    break;
            }   

            fstring += personnality[i].Item3 +"; ";
        }
        fstring += startPrompt;
        fstring += " " + PlayerName + ", " + basicTreats[2] + ": ";
        fstring += playerStatement + ".";
        fstring += $"Please provide a complete response in {tokennum} tokens or fewer.";
        return new Tuple<string, int>(fstring, tokennum);
    }

    public void createPersonnality(string Name, string BaseMainTreat, string RelationToPlayer)
    {
        basicTreats[0] = Name;
        basicTreats[1] = BaseMainTreat;
        basicTreats[2] = RelationToPlayer;
    }

    public void addEvent(Timing t, personnalityType p, string e){
        personnality.Add(new Tuple<Timing, personnalityType, string>(t, p, e));
    }
    public List<int> GetNDistinctRandoms(int n, int maxExclusive)
    {
        if (maxExclusive < n)
        {
            Debug.LogError("The upper limit must be at least 3 for this method to work.");
            return null;
        }

        HashSet<int> resultSet = new HashSet<int>();

        while (resultSet.Count < n)
        {
            resultSet.Add(UnityEngine.Random.Range(0, maxExclusive));
        }

        return new List<int>(resultSet);
    }
}
