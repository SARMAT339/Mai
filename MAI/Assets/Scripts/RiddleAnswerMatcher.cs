using System.Text.RegularExpressions;

public static class RiddleAnswerMatcher
{
    static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static bool IsCorrect(string userAnswer, in RiddleEntry riddle)
    {
        if (string.IsNullOrWhiteSpace(userAnswer) || string.IsNullOrWhiteSpace(riddle.answer))
            return false;

        var normalizedUser = Normalize(userAnswer);
        return normalizedUser == Normalize(riddle.answer);
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lower = value.Trim().ToLowerInvariant().Replace('ё', 'е');
        return Whitespace.Replace(lower, " ");
    }
}
