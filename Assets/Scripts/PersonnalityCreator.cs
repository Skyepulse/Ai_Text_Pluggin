using System;
using System.Collections.Generic;

public class PersonnalityCreator
{
    private const string startPersonality = "Assume the personality of";
    private const string contextPersonality = " with background the following";
    private const string startPrompt = "Answer to the statement given in the user prompt made by";

    //Get set for the personnality
    public List<Tuple<Timing, personnalityType, string>> Personnality
    {
        get { return personnality; }
        set { personnality = value; }
    }   
    private List<Tuple<Timing, personnalityType,  string>> personnality;
    private string[] basicTreats;

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

    public Tuple<string, int> createPrompt(Timing[] times, int numMaxContext, string PlayerName, int tokennum)
    {
        if (basicTreats[0] == null || basicTreats[1] == null || times.Length == 0 || numMaxContext > personnality.Count || tokennum == 0)
        {
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
        fstring += " " + PlayerName + ", " + basicTreats[2] + ". ";
        fstring += $"Please provide a complete response in {tokennum} tokens or fewer.";
        return new Tuple<string, int>(fstring, tokennum);
    }

    public void createPersonnality(string Name, string BaseMainTreat, string RelationToPlayer)
    {
        basicTreats = new string[3];
        basicTreats[0] = Name;
        basicTreats[1] = BaseMainTreat;
        basicTreats[2] = RelationToPlayer;
    }

    public void addEvent(Timing t, personnalityType p, string e){
        if(personnality == null)
        {
            personnality = new List<Tuple<Timing, personnalityType, string>>();
        }
        personnality.Add(new Tuple<Timing, personnalityType, string>(t, p, e));
    }

    public void addEventFromDiscussion(string discussion)
    {
        addEvent(Timing.Present, personnalityType.Discussion, discussion);
    }
    public List<int> GetNDistinctRandoms(int n, int maxExclusive)
    {
        if (maxExclusive < n)
        {
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
