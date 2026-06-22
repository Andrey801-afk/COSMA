using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProgramRestartButtonRuntimeSync
{
    private const string BottomRightActionsPanelName = "BottomRightActionsPanel";
    private const string RestartButtonName = "RestartButton";
    private const string RestartLabel = "RESTART";

    private static readonly Color RestartAccent = new(0.56f, 0.70f, 0.76f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureRestartButton();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureRestartButton();
    }

    public static void EnsureRestartButton()
    {
        ProgramExecutor executor = Object.FindFirstObjectByType<ProgramExecutor>(FindObjectsInactive.Include);
        if (executor == null)
        {
            return;
        }

        GameObject panelObject = GameObject.Find(BottomRightActionsPanelName);
        if (panelObject == null)
        {
            return;
        }

        Button restartButton = EnsureButton(panelObject.transform);
        executor.BindRestartButton(restartButton);
    }

    private static Button EnsureButton(Transform parent)
    {
        Transform existing = parent.Find(RestartButtonName);
        GameObject root = existing != null
            ? existing.gameObject
            : new GameObject(RestartButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

        if (existing == null)
        {
            root.transform.SetParent(parent, false);
        }

        CopySiblingImageStyle(root, parent);
        Button button = GetOrAdd<Button>(root);
        Image image = root.GetComponent<Image>();
        button.targetGraphic = image;
        ApplyButtonColors(button, image);

        LayoutElement element = GetOrAdd<LayoutElement>(root);
        element.minWidth = 108f;
        element.preferredWidth = 130f;
        element.minHeight = 46f;

        TMP_Text label = EnsureLabel(root, parent);
        label.text = RestartLabel;
        label.color = RestartAccent;
        label.fontSize = 16f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;

        AnimatedButton animatedButton = GetOrAdd<AnimatedButton>(root);
        animatedButton.Configure(Object.FindFirstObjectByType<UIAnimationDriver>(FindObjectsInactive.Include), (RectTransform)root.transform);

        Transform clearButton = parent.Find("ClearButton");
        if (clearButton != null && root.transform.GetSiblingIndex() > clearButton.GetSiblingIndex())
        {
            root.transform.SetSiblingIndex(clearButton.GetSiblingIndex());
        }

        if (parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        return button;
    }

    private static void CopySiblingImageStyle(GameObject root, Transform parent)
    {
        Image image = GetOrAdd<Image>(root);
        Image template = parent.Find("UndoButton") != null
            ? parent.Find("UndoButton").GetComponent<Image>()
            : null;

        if (template != null)
        {
            image.sprite = template.sprite;
            image.type = template.type;
            image.material = template.material;
            image.color = template.color;
            image.raycastTarget = template.raycastTarget;
            return;
        }

        image.color = new Color(0.28f, 0.29f, 0.31f, 0.58f);
        image.raycastTarget = true;
    }

    private static void ApplyButtonColors(Button button, Image image)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = image != null ? image.color : new Color(0.28f, 0.29f, 0.31f, 0.58f);
        colors.highlightedColor = new Color(0.36f, 0.37f, 0.39f, 0.72f);
        colors.pressedColor = new Color(0.46f, 0.47f, 0.49f, 0.82f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private static TMP_Text EnsureLabel(GameObject root, Transform parent)
    {
        Transform existing = root.transform.Find("Text");
        GameObject labelObject = existing != null
            ? existing.gameObject
            : new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

        if (existing == null)
        {
            labelObject.transform.SetParent(root.transform, false);
        }

        RectTransform rect = (RectTransform)labelObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = GetOrAdd<TextMeshProUGUI>(labelObject);
        TMP_Text template = parent.Find("UndoButton") != null
            ? parent.Find("UndoButton").GetComponentInChildren<TMP_Text>(true)
            : null;

        if (template != null)
        {
            text.font = template.font;
            text.raycastTarget = template.raycastTarget;
        }

        return text;
    }

    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }
}
