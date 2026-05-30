using TMPro;
using UnityEngine;

public static class RiddleInputTextFitter
{
    const float MaxFontSize = 0.52f;
    const float MinFontSize = 0.14f;
    const float PlaceholderMaxFontSize = 0.48f;

    public static void Configure(TMP_InputField input)
    {
        if (input == null)
            return;

        Apply(input.textComponent as TextMeshProUGUI, MaxFontSize);

        if (input.placeholder is TextMeshProUGUI placeholder)
            Apply(placeholder, PlaceholderMaxFontSize);

        Refresh(input);
    }

    public static void Refresh(TMP_InputField input)
    {
        if (input == null)
            return;

        var text = input.textComponent as TextMeshProUGUI;
        if (text == null)
            return;

        Apply(text, MaxFontSize);
        text.text = input.text;
        text.ForceMeshUpdate(true);
    }

    static void Apply(TextMeshProUGUI label, float maxFontSize)
    {
        if (label == null)
            return;

        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.enableAutoSizing = true;
        label.fontSize = maxFontSize;
        label.fontSizeMax = maxFontSize;
        label.fontSizeMin = MinFontSize;
        label.ForceMeshUpdate(true);
    }
}
