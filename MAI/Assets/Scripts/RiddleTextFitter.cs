using TMPro;
using UnityEngine;

public static class RiddleTextFitter
{
    const float DefaultWidth = 3.5f;
    const float MaxFontSize = 0.45f;
    const float MinFontSize = 0.14f;
    const float LineHeight = 0.34f;
    const float BaseHeight = 0.75f;
    const float MaxHeight = 3.4f;
    static readonly Color RiddleTextColor = new(0.92f, 0.08f, 0.08f, 1f);

    public static void Apply(TextMeshProUGUI label, string text)
    {
        if (label == null)
            return;

        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        var height = Mathf.Clamp(BaseHeight + lineCount * LineHeight, 1f, MaxHeight);

        var rect = label.rectTransform;
        rect.sizeDelta = new Vector2(DefaultWidth, height);

        label.text = text;
        label.color = RiddleTextColor;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.enableAutoSizing = true;
        label.fontSize = MaxFontSize;
        label.fontSizeMax = MaxFontSize;
        label.fontSizeMin = MinFontSize;

        var charCount = text?.Length ?? 0;
        if (charCount > 140)
            label.fontSizeMax = MaxFontSize * 0.82f;
        else if (charCount > 100)
            label.fontSizeMax = MaxFontSize * 0.9f;

        label.ForceMeshUpdate(true);
    }
}
