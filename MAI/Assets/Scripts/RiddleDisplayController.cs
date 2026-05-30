using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RiddleDisplayController : MonoBehaviour
{
    const int DefaultRiddlesToShow = 3;
    const string RestartButtonName = "RestartButton";

    static readonly string[] RiddleTextNames =
    {
        "Text (TMP)",
        "Text (TMP) (1)",
        "Text (TMP) (2)",
    };

    static readonly string[] InputNames =
    {
        "InputField (TMP)",
        "InputField (TMP) (1)",
        "InputField (TMP) (2)",
    };

    static readonly Vector2[] InputPositions =
    {
        new(-4.552f, -2.316f),
        new(0f, -2.316f),
        new(4.552f, -2.316f),
    };

    static readonly Vector2 RestartButtonPosition = new(0f, -4.1f);
    static readonly Vector2 RestartButtonSize = new(6f, 1.1f);

    [SerializeField] RiddleSlot[] slots;
    [SerializeField] int riddlesToShow = DefaultRiddlesToShow;
    [SerializeField] string photoNamePrefix = "Picsart_";
    [SerializeField] Button restartButton;

    readonly List<InputSubscription> _inputSubscriptions = new();

    struct InputSubscription
    {
        public TMP_InputField input;
        public UnityEngine.Events.UnityAction<string> onEndEdit;
        public UnityEngine.Events.UnityAction<string> onValueChanged;
    }

    void Awake()
    {
        if (NeedsAutoWire())
            AutoWireSlots();
        else
        {
            WireRewardPhotosIfNeeded();
            EnsureInputFields();
        }

        EnsureRestartButton();
        HideAllRewardPhotos();
        SetRestartButtonVisible(false);
    }

    void Start()
    {
        ShowRandomRiddles();
    }

    void OnDestroy()
    {
        UnregisterInputListeners();
    }

    bool NeedsAutoWire()
    {
        if (slots == null || slots.Length == 0)
            return true;

        foreach (var slot in slots)
        {
            if (slot == null || slot.riddleText == null)
                return true;
        }

        return false;
    }

    void AutoWireSlots()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("RiddleDisplayController: Canvas not found.");
            return;
        }

        slots = new RiddleSlot[RiddleTextNames.Length];

        for (var i = 0; i < RiddleTextNames.Length; i++)
        {
            var textTransform = canvas.transform.Find(RiddleTextNames[i]);
            if (textTransform == null)
            {
                Debug.LogError($"RiddleDisplayController: UI element \"{RiddleTextNames[i]}\" not found.");
                continue;
            }

            slots[i] = new RiddleSlot
            {
                riddleText = textTransform.GetComponent<TextMeshProUGUI>(),
            };
        }

        EnsureInputFields();
        WireRewardPhotosIfNeeded();
    }

    void WireRewardPhotosIfNeeded()
    {
        if (slots == null || slots.Length == 0)
            return;

        if (slots.All(slot => slot != null && slot.rewardPhoto != null))
            return;

        var photos = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(renderer => renderer.gameObject.name.StartsWith(photoNamePrefix))
            .OrderBy(renderer => renderer.transform.position.x)
            .Take(slots.Length)
            .ToArray();

        if (photos.Length < slots.Length)
        {
            Debug.LogWarning(
                $"RiddleDisplayController: expected {slots.Length} photos with prefix \"{photoNamePrefix}\", found {photos.Length}.");
        }

        for (var i = 0; i < slots.Length && i < photos.Length; i++)
        {
            if (slots[i] == null)
                continue;

            if (slots[i].rewardPhoto == null)
                slots[i].rewardPhoto = photos[i];
        }
    }

    void EnsureInputFields()
    {
        if (slots == null || slots.Length == 0)
            return;

        var canvas = slots[0].riddleText != null
            ? slots[0].riddleText.transform.parent
            : GameObject.Find("Canvas")?.transform;

        if (canvas == null)
        {
            Debug.LogError("RiddleDisplayController: cannot resolve Canvas transform.");
            return;
        }

        var template = FindInputTemplate(canvas);
        if (template == null)
        {
            Debug.LogError("RiddleDisplayController: TMP_InputField template not found on Canvas.");
            return;
        }

        UnregisterInputListeners();

        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].answerInput = GetOrCreateInput(canvas, template, i);
            PositionInput(slots[i].answerInput, i);
            ConfigureInput(slots[i].answerInput);
            RegisterInputListener(i);
        }
    }

    void RegisterInputListener(int slotIndex)
    {
        var input = slots[slotIndex].answerInput;
        if (input == null)
            return;

        UnityEngine.Events.UnityAction<string> onEndEdit = text => TrySubmitAnswer(slotIndex, text);
        UnityEngine.Events.UnityAction<string> onValueChanged = _ => RiddleInputTextFitter.Refresh(input);

        input.onEndEdit.AddListener(onEndEdit);
        input.onValueChanged.AddListener(onValueChanged);

        _inputSubscriptions.Add(new InputSubscription
        {
            input = input,
            onEndEdit = onEndEdit,
            onValueChanged = onValueChanged,
        });
    }

    void UnregisterInputListeners()
    {
        foreach (var subscription in _inputSubscriptions)
        {
            if (subscription.input == null)
                continue;

            subscription.input.onEndEdit.RemoveListener(subscription.onEndEdit);
            subscription.input.onValueChanged.RemoveListener(subscription.onValueChanged);
        }

        _inputSubscriptions.Clear();
    }

    void HideAllRewardPhotos()
    {
        if (slots == null)
            return;

        WireRewardPhotosIfNeeded();

        foreach (var slot in slots)
            RiddlePhotoReveal.Hide(slot?.rewardPhoto);
    }

    void EnsureRestartButton()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
            return;
        }

        var canvas = slots != null && slots.Length > 0 && slots[0].riddleText != null
            ? slots[0].riddleText.transform.parent
            : GameObject.Find("Canvas")?.transform;

        if (canvas == null)
        {
            Debug.LogError("RiddleDisplayController: cannot create restart button without Canvas.");
            return;
        }

        var existing = canvas.Find(RestartButtonName);
        if (existing != null)
        {
            restartButton = existing.GetComponent<Button>();
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
                return;
            }
        }

        restartButton = CreateRestartButton(canvas);
        restartButton.onClick.AddListener(RestartGame);
    }

    Button CreateRestartButton(Transform canvas)
    {
        var buttonObject = new GameObject(RestartButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas, false);
        buttonObject.layer = canvas.gameObject.layer;

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = RestartButtonPosition;
        rect.sizeDelta = RestartButtonSize;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.42f, 0.2f, 0.92f);

        var button = buttonObject.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.24f, 0.52f, 0.26f, 1f);
        colors.pressedColor = new Color(0.12f, 0.32f, 0.14f, 1f);
        button.colors = colors;

        var labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        labelObject.layer = buttonObject.layer;

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Начать заново";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 0.42f;
        label.color = Color.white;
        label.raycastTarget = false;

        var fontSource = slots != null && slots.Length > 0 ? slots[0].riddleText : null;
        if (fontSource == null)
            fontSource = canvas.GetComponentInChildren<TextMeshProUGUI>(true);

        if (fontSource != null)
        {
            label.font = fontSource.font;
            label.fontSharedMaterial = fontSource.fontSharedMaterial;
        }

        return button;
    }

    void SetRestartButtonVisible(bool visible)
    {
        if (restartButton != null)
            restartButton.gameObject.SetActive(visible);
    }

    static TMP_InputField FindInputTemplate(Transform canvas)
    {
        foreach (var name in InputNames)
        {
            var child = canvas.Find(name);
            if (child != null)
                return child.GetComponent<TMP_InputField>();
        }

        return canvas.GetComponentInChildren<TMP_InputField>(true);
    }

    static TMP_InputField GetOrCreateInput(Transform canvas, TMP_InputField template, int index)
    {
        var existing = canvas.Find(InputNames[index]);
        if (existing != null)
            return existing.GetComponent<TMP_InputField>();

        var instance = Instantiate(template.gameObject, canvas);
        instance.name = InputNames[index];
        return instance.GetComponent<TMP_InputField>();
    }

    static void ConfigureInput(TMP_InputField input)
    {
        if (input == null)
            return;

        if (input.placeholder is TextMeshProUGUI placeholder)
            placeholder.text = "Ваш ответ...";

        RiddleInputTextFitter.Configure(input);
    }

    static void PositionInput(TMP_InputField input, int index)
    {
        if (input == null || index < 0 || index >= InputPositions.Length)
            return;

        var rect = input.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = InputPositions[index];
        rect.sizeDelta = new Vector2(4f, 1f);
    }

    public void ShowRandomRiddles()
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("RiddleDisplayController: riddle slots are not assigned.");
            return;
        }

        SetRestartButtonVisible(false);
        WireRewardPhotosIfNeeded();

        var slotCount = Mathf.Min(riddlesToShow, slots.Length);
        var pool = RiddleBank.All;

        if (pool.Count < slotCount)
        {
            Debug.LogError(
                $"RiddleDisplayController: need at least {slotCount} riddles in the bank, but only {pool.Count} defined.");
            return;
        }

        var picked = PickRandomIndices(pool.Count, slotCount);

        for (var i = 0; i < slotCount; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].ResetForNewRiddle();
            slots[i].currentRiddle = pool[picked[i]];

            if (slots[i].riddleText != null)
                RiddleTextFitter.Apply(slots[i].riddleText, slots[i].currentRiddle.question);

            if (slots[i].answerInput != null)
                RiddleInputTextFitter.Configure(slots[i].answerInput);
        }

        for (var i = slotCount; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].ResetForNewRiddle();
            slots[i].currentRiddle = default;

            if (slots[i].riddleText != null)
                RiddleTextFitter.Apply(slots[i].riddleText, string.Empty);

            if (slots[i].answerInput != null)
                RiddleInputTextFitter.Configure(slots[i].answerInput);
        }
    }

    public void RestartGame()
    {
        ShowRandomRiddles();
    }

    void TrySubmitAnswer(int slotIndex, string rawAnswer)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        var slot = slots[slotIndex];
        if (slot == null || slot.isSolved)
            return;

        if (!RiddleAnswerMatcher.IsCorrect(rawAnswer, slot.currentRiddle))
            return;

        slot.isSolved = true;
        RiddlePhotoReveal.Show(slot.rewardPhoto);
        slot.HideRiddleUi();

        if (AreAllActiveRiddlesSolved())
            SetRestartButtonVisible(true);
    }

    bool AreAllActiveRiddlesSolved()
    {
        var activeCount = Mathf.Min(riddlesToShow, slots.Length);

        for (var i = 0; i < activeCount; i++)
        {
            if (slots[i] == null || !slots[i].isSolved)
                return false;
        }

        return activeCount > 0;
    }

    static List<int> PickRandomIndices(int poolSize, int count)
    {
        var indices = new List<int>(poolSize);
        for (var i = 0; i < poolSize; i++)
            indices.Add(i);

        for (var i = indices.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices.GetRange(0, count);
    }
}
