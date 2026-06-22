using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionHudWidget : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string CurrentMissionCaption = "\u0422\u0415\u041a\u0423\u0429\u0410\u042f \u041c\u0418\u0421\u0421\u0418\u042f";
    private const string UserMadeMissionCaption = "\u0422\u0415\u041a\u0423\u0429\u0410\u042f \u041c\u0418\u0421\u0421\u0418\u042f \u00b7 \u0421\u041e\u0417\u0414\u0410\u041d\u0410 \u041f\u041e\u041b\u042c\u0417\u041e\u0412\u0410\u0422\u0415\u041b\u0415\u041c";
    private const string ReturnToMenuLabel = "\u0412 \u041c\u0415\u041d\u042e";

    private static Sprite roundedPanelSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        TrySpawn();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void EnsureSpawned()
    {
        TrySpawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MainMenuSceneName) return;
        TrySpawn();
    }

    private static void TrySpawn()
    {
        if (SceneManager.GetActiveScene().name == MainMenuSceneName) return;
        if (FindFirstObjectByType<MissionHudWidget>() != null) return;
        bool hasMissionSystem = FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include) != null;
        if (!MissionContext.HasAny && !hasMissionSystem) return;

        var go = new GameObject("MissionHudWidget");
        go.AddComponent<MissionHudWidget>();
    }

    private TextMeshProUGUI _titleText;
    private GameObject _completionOverlay;
    private float _timeScaleBeforeCompletion = 1f;
    private bool _completionOpen;
    private bool _completionPausedTimeScale;
    private MissionSystem _pendingCompletionMission;

    private void OnEnable()
    {
        MissionSystem.MissionCompleted += HandleMissionCompleted;
        ProgramExecutor.ExecutionStopped += HandleProgramExecutionStopped;
    }

    private void OnDisable()
    {
        MissionSystem.MissionCompleted -= HandleMissionCompleted;
        ProgramExecutor.ExecutionStopped -= HandleProgramExecutionStopped;
        RestoreTimeScale();
    }

    private void Start()
    {
        BuildUI();
        var missionSystem = FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
        if (missionSystem != null && missionSystem.IsCompleted)
        {
            HandleMissionCompleted(missionSystem);
        }
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("MissionHudCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // Top-center panel
        var panel = new GameObject("Panel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasRT, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(-110, -16);
        rt.sizeDelta = new Vector2(620, 76);
        var panelImage = panel.GetComponent<Image>();
        panelImage.sprite = GetRoundedPanelSprite();
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 10, 8);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Caption
        var caption = CreateText(panel.transform, "Caption",
            MissionContext.IsUserMade ? UserMadeMissionCaption : CurrentMissionCaption,
            13, new Color(1f, 0.85f, 0.20f), FontStyles.UpperCase, TextAlignmentOptions.Center);
        caption.characterSpacing = 8f;

        // Title
        var missionSystem = FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
        _titleText = CreateText(panel.transform, "Title",
            GetCurrentMissionTitle(missionSystem),
            20, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);

        CreateTopRightMenuButton(canvasRT);
        BuildCompletionOverlay(canvasRT);
    }

    private void CreateTopRightMenuButton(RectTransform canvasRT)
    {
        var go = new GameObject("Btn_ReturnToMenu",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        go.transform.SetParent(canvasRT, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-18f, -18f);
        rt.sizeDelta = new Vector2(118f, 34f);

        var img = go.GetComponent<Image>();
        img.sprite = GetRoundedPanelSprite();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.04f, 0.06f, 0.10f, 0.78f);
        img.raycastTarget = true;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());

        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = ReturnToMenuLabel;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 13;
        tmp.color = new Color(0.92f, 0.94f, 0.97f, 1f);
        tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 3f;
        tmp.raycastTarget = false;

        var button = go.GetComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(OnReturnToMenu);
    }

    private void BuildCompletionOverlay(RectTransform canvasRT)
    {
        _completionOverlay = new GameObject("MissionCompleteOverlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _completionOverlay.transform.SetParent(canvasRT, false);
        Stretch(_completionOverlay.GetComponent<RectTransform>());
        var overlayImg = _completionOverlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.82f);
        overlayImg.raycastTarget = true;

        var card = new GameObject("Card",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        card.transform.SetParent(_completionOverlay.transform, false);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta = new Vector2(920, 620);
        card.GetComponent<Image>().color = new Color(0.035f, 0.04f, 0.055f, 0.98f);
        var outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.65f, 0.12f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(46, 46, 36, 34);
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var status = CreateText(card.transform, "Status", "МИССИЯ ВЫПОЛНЕНА",
            22, new Color(0.24f, 0.86f, 0.59f), FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        status.characterSpacing = 5f;
        status.GetComponent<LayoutElement>().preferredHeight = 30;

        var title = CreateText(card.transform, "CompleteTitle", "",
            40, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
        title.GetComponent<LayoutElement>().preferredHeight = 54;

        var line = new GameObject("AccentLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.transform.SetParent(card.transform, false);
        line.GetComponent<Image>().color = new Color(1f, 0.65f, 0.12f, 0.9f);
        var lineLE = line.AddComponent<LayoutElement>();
        lineLE.preferredHeight = 2;

        var stats = CreateText(card.transform, "Stats", "",
            18, new Color(0.78f, 0.82f, 0.88f, 1f), FontStyles.Bold, TextAlignmentOptions.Left);
        stats.GetComponent<LayoutElement>().preferredHeight = 68;

        var objectiveHeader = CreateText(card.transform, "ObjectiveHeader", "ЗАКРЫТЫЕ ЦЕЛИ",
            17, new Color(1f, 0.69f, 0.13f), FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        objectiveHeader.characterSpacing = 3f;
        objectiveHeader.GetComponent<LayoutElement>().preferredHeight = 24;

        var objectives = CreateText(card.transform, "Objectives", "",
            17, new Color(0.90f, 0.93f, 0.96f, 1f), FontStyles.Normal, TextAlignmentOptions.TopLeft);
        objectives.GetComponent<LayoutElement>().preferredHeight = 170;

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1;

        var buttons = new GameObject("Buttons", typeof(RectTransform));
        buttons.transform.SetParent(card.transform, false);
        var buttonsLE = buttons.AddComponent<LayoutElement>();
        buttonsLE.preferredHeight = 52;
        var hlg = buttons.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 22;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        CreateActionButton(buttons.transform, "ПРОДОЛЖИТЬ",
            new Color(0.78f, 0.82f, 0.88f, 1f), OnContinueAfterCompletion);
        CreateActionButton(buttons.transform, "ПОВТОРИТЬ",
            new Color(0.18f, 0.66f, 1f, 1f), OnRestartMission);
        CreateActionButton(buttons.transform, "В МЕНЮ",
            new Color(1f, 0.65f, 0.12f, 1f), OnReturnToMenu);

        _completionOverlay.SetActive(false);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text,
        int size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size + 6;
        return tmp;
    }

    private void CreateActionButton(Transform parent, string label, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Btn_{label}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        var rt = labelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 16;
        tmp.color = textColor;
        tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 4f;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = tmp;
        var colors = btn.colors;
        colors.normalColor = textColor;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 200;
        le.preferredHeight = 32;
    }

    private void HandleMissionCompleted(MissionSystem missionSystem)
    {
        if (_completionOpen || _completionOverlay == null)
        {
            return;
        }

        if (ShouldDelayCompletionOverlay())
        {
            _pendingCompletionMission = missionSystem;
            return;
        }

        ShowCompletionOverlay(missionSystem);
    }

    private void HandleProgramExecutionStopped(ProgramExecutor executor)
    {
        if (_pendingCompletionMission == null || _completionOpen || _completionOverlay == null)
        {
            return;
        }

        MissionSystem missionSystem = _pendingCompletionMission;
        _pendingCompletionMission = null;
        ShowCompletionOverlay(missionSystem);
    }

    private bool ShouldDelayCompletionOverlay()
    {
        ProgramExecutor executor = FindFirstObjectByType<ProgramExecutor>(FindObjectsInactive.Include);
        return executor != null &&
               executor.HasExecutionStarted &&
               executor.HasRemainingProgramLines &&
               !executor.IsProgramComplete;
    }

    private void ShowCompletionOverlay(MissionSystem missionSystem)
    {
        _completionOpen = true;
        _timeScaleBeforeCompletion = Time.timeScale;
        _completionPausedTimeScale = false;

        SetChildText(_completionOverlay.transform, "CompleteTitle", GetCurrentMissionTitle(missionSystem));
        SetChildText(_completionOverlay.transform, "Stats", BuildCompletionStats(missionSystem));
        SetChildText(_completionOverlay.transform, "Objectives", BuildCompletionObjectives(missionSystem));
        _completionOverlay.SetActive(true);
    }

    private string BuildCompletionStats(MissionSystem missionSystem)
    {
        int completed = 0;
        int total = 0;
        if (missionSystem != null && missionSystem.Objectives != null)
        {
            total = missionSystem.Objectives.Count;
            for (int i = 0; i < missionSystem.Objectives.Count; i++)
            {
                if (missionSystem.Objectives[i] != null && missionSystem.Objectives[i].isCompleted)
                {
                    completed++;
                }
            }
        }

        int reward = GetCurrentRewardScience();
        string type = MissionContext.HasAny
            ? (MissionContext.IsUserMade ? "пользовательская" : "основная")
            : "сценарная";
        string rewardLine = reward > 0 ? $"\nНаграда: {reward} SCI" : "";
        return $"Тип: {type}\nЦели: {completed} / {total}{rewardLine}";
    }

    private string BuildCompletionObjectives(MissionSystem missionSystem)
    {
        IReadOnlyList<MissionObjective> objectives = missionSystem != null ? missionSystem.Objectives : null;
        if (objectives == null || objectives.Count == 0)
        {
            return "• Ключевые задачи выполнены";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjective objective = objectives[i];
            if (objective == null)
            {
                continue;
            }

            string label = !string.IsNullOrWhiteSpace(objective.displayName)
                ? objective.displayName
                : objective.objectiveType.ToString();
            builder.Append("• ").Append(label).AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private int GetCurrentRewardScience()
    {
        if (MissionContext.Current != null)
        {
            return MissionContext.Current.rewardScience;
        }

        if (MissionContext.CurrentUserMission != null)
        {
            return MissionContext.CurrentUserMission.rewardScience;
        }

        return 0;
    }

    private string GetCurrentMissionTitle(MissionSystem missionSystem)
    {
        if (!string.IsNullOrWhiteSpace(MissionContext.CurrentTitle))
        {
            return MissionContext.CurrentTitle;
        }

        MissionDefinition definition = missionSystem != null ? missionSystem.Definition : null;
        if (definition != null && !string.IsNullOrWhiteSpace(definition.missionName))
        {
            return definition.missionName;
        }

        return "Миссия";
    }

    private void SetChildText(Transform root, string childName, string text)
    {
        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == childName)
            {
                texts[i].text = text;
                return;
            }
        }
    }

    private void OnContinueAfterCompletion()
    {
        Mission nextMission = MissionContext.GetNextAvailableMission();
        if (nextMission != null)
        {
            RestoreTimeScale(forceNormal: true);
            MissionContext.StartMission(nextMission);
            string nextScene = string.IsNullOrEmpty(nextMission.sceneName)
                ? SceneManager.GetActiveScene().name
                : nextMission.sceneName;
            SceneManager.LoadScene(nextScene);
            return;
        }

        if (MissionContext.HasAny)
        {
            ReturnToMenu();
            return;
        }

        if (_completionOverlay != null)
        {
            _completionOverlay.SetActive(false);
        }

        RestoreTimeScale();
        _pendingCompletionMission = null;
        _completionOpen = false;
    }

    private void OnRestartMission()
    {
        RestoreTimeScale(forceNormal: true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnReturnToMenu() => ReturnToMenu();

    private void ReturnToMenu()
    {
        RestoreTimeScale(forceNormal: true);
        MissionContext.Clear();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void RestoreTimeScale(bool forceNormal = false)
    {
        if (forceNormal)
        {
            _completionPausedTimeScale = false;
            Time.timeScale = 1f;
            return;
        }

        if (_completionOpen && _completionPausedTimeScale)
        {
            Time.timeScale = _timeScaleBeforeCompletion <= 0f ? 1f : _timeScaleBeforeCompletion;
            _completionPausedTimeScale = false;
        }
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite GetRoundedPanelSprite()
    {
        if (roundedPanelSprite != null)
        {
            return roundedPanelSprite;
        }

        const int size = 96;
        const int radius = 18;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color visible = Color.white;
        Color hidden = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, IsInsideRoundedRect(x + 0.5f, y + 0.5f, size, size, radius) ? visible : hidden);
            }
        }

        texture.Apply();
        roundedPanelSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        roundedPanelSprite.name = "MissionHudRoundedPanel";
        return roundedPanelSprite;
    }

    private static bool IsInsideRoundedRect(float x, float y, float width, float height, float radius)
    {
        float nearestX = Mathf.Clamp(x, radius, width - radius);
        float nearestY = Mathf.Clamp(y, radius, height - radius);
        float deltaX = x - nearestX;
        float deltaY = y - nearestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }
}
