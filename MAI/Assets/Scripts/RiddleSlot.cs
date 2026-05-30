using System;
using TMPro;
using UnityEngine;

[Serializable]
public class RiddleSlot
{
    public TextMeshProUGUI riddleText;
    public TMP_InputField answerInput;
    public SpriteRenderer rewardPhoto;

    [NonSerialized] public RiddleEntry currentRiddle;
    [NonSerialized] public bool isSolved;

    public void ClearAnswer()
    {
        if (answerInput == null)
            return;

        answerInput.text = string.Empty;
        answerInput.interactable = true;
    }

    public void ResetForNewRiddle()
    {
        isSolved = false;
        ShowRiddleUi();
        ClearAnswer();
        RiddlePhotoReveal.Hide(rewardPhoto);
    }

    public void ShowRiddleUi()
    {
        if (riddleText != null)
            riddleText.gameObject.SetActive(true);

        if (answerInput != null)
            answerInput.gameObject.SetActive(true);
    }

    public void HideRiddleUi()
    {
        if (riddleText != null)
            riddleText.gameObject.SetActive(false);

        if (answerInput != null)
            answerInput.gameObject.SetActive(false);
    }
}
