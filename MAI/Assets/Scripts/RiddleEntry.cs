using System;

[Serializable]
public struct RiddleEntry
{
    public string question;
    public string answer;

    public RiddleEntry(string question, string answer)
    {
        this.question = question;
        this.answer = answer;
    }
}
