using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuBootstrap : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private string _gameTitle = "COSMA";
    [SerializeField] private string _gameSubtitle = "УЧЕБНАЯ ОРБИТАЛЬНАЯ ПРОГРАММА";
    [SerializeField] private Mission[] _missions;
    [SerializeField] private string _defaultGameScene = "SampleScene";

    [Header("Background")]
    [SerializeField] private Sprite _backgroundSprite;
    [SerializeField] private Color _backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
    [SerializeField, Range(0f, 1f)] private float _vignetteStrength = 0.45f;

    [Header("Title")]
    [SerializeField] private Sprite _titleSprite;
    [SerializeField] private Vector2 _titleSize = new Vector2(520, 200);

    [Header("Palette")]
    [SerializeField] private Color _buttonTextNormal = new Color(0.92f, 0.94f, 0.97f, 1f);
    [SerializeField] private Color _buttonTextHover = new Color(1f, 0.85f, 0.20f, 1f);
    [SerializeField] private Color _buttonTextPressed = new Color(0.85f, 0.65f, 0.10f, 1f);
    [SerializeField] private Color _buttonTextDisabled = new Color(0.45f, 0.48f, 0.55f, 1f);
    [SerializeField] private Color _textPrimary = Color.white;
    [SerializeField] private Color _textSecondary = new Color(0.78f, 0.82f, 0.88f, 1f);
    [SerializeField] private Color _textMuted = new Color(0.55f, 0.58f, 0.65f, 1f);
    [SerializeField] private Color _accent = new Color(1f, 0.65f, 0.18f, 1f);
    [SerializeField] private Color _accentSoft = new Color(1f, 0.85f, 0.20f, 1f);
    [SerializeField] private Color _statusOnline = new Color(0.30f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color _statusInfo = new Color(0.35f, 0.70f, 0.95f, 1f);
    [SerializeField] private Color _statusLocked = new Color(0.55f, 0.58f, 0.65f, 1f);
    [SerializeField] private Color _panelColor = new Color(0.063f, 0.09f, 0.133f, 1f);
    [SerializeField] private Color _cardColor = new Color(0.082f, 0.118f, 0.169f, 1f);
    [SerializeField] private Color _borderColor = new Color(0.149f, 0.196f, 0.267f, 1f);

    [Header("Layout")]
    [SerializeField] private float _leftMargin = 120f;
    [SerializeField] private float _menuButtonHeight = 44f;
    [SerializeField] private int _menuButtonFontSize = 28;

    [Header("Typography")]
    [SerializeField] private TMP_FontAsset _font;

    private Canvas _canvas;
    private RectTransform _root;

    private RectTransform _mainScreen;
    private RectTransform _missionsScreen;
    private RectTransform _settingsScreen;
    private RectTransform _creditsScreen;

    private RectTransform _missionListContent;
    private TextMeshProUGUI _missionsCounter;
    private Image _missionsProgressFill;
    private TextMeshProUGUI _missionsProgressText;
    private TextMeshProUGUI _detailsTitle;
    private TextMeshProUGUI _detailsObjective;
    private TextMeshProUGUI _detailsDescription;
    private TextMeshProUGUI _detailsStatusText;
    private TextMeshProUGUI _detailsRewardText;
    private TextMeshProUGUI _detailsSceneText;
    private RectTransform _detailsChecklist;
    private GameObject _detailsContent;
    private GameObject _detailsEmpty;
    private Button _launchButton;
    private TextMeshProUGUI _launchButtonLabel;
    private Mission _selectedMission;
    private MissionRowView _selectedRow;
    private readonly List<MissionRowView> _rows = new List<MissionRowView>();
    private readonly Dictionary<string, MissionRowView> _userRows = new Dictionary<string, MissionRowView>();

    private Button _continueButton;

    // User missions editor
    private GameObject _userMissionEditor;
    private RectTransform _userMissionsContainer;
    private UserMission _editingUserMission;
    private TMP_InputField _editorTitle;
    private TMP_InputField _editorObjective;
    private TMP_InputField _editorDescription;
    private Slider _editorRewardSlider;
    private TextMeshProUGUI _editorRewardLabel;
    private TextMeshProUGUI _editorSceneLabel;
    private int _editorSceneIndex;
    private System.Collections.Generic.List<string> _availableScenes;
    private GameObject _editorDeleteButton;
    private TextMeshProUGUI _editorDeleteLabel;
    private TextMeshProUGUI _editorTitleLabel;
    private TextMeshProUGUI _editorSummary;
    private TextMeshProUGUI _editorTitleError;
    private TextMeshProUGUI _editorObjectiveError;
    private RectTransform _editorConditionsContainer;
    private GameObject _conditionsModal;
    private TextMeshProUGUI _editorConditionsSummaryText;
    private readonly List<MissionConditionData> _editingConditions = new List<MissionConditionData>();
    private TextMeshProUGUI _editorDifficultyLabel;
    private int _editorDifficultyIndex;
    private readonly string[] _difficultyOptions = { "Базовая", "Средняя", "Сложная" };
    private bool _deleteArmed;

    private void Start()
    {
        EnsureEventSystem();
        ApplyMissionControlPalette();
        EnsureCamera();
        _canvas = EnsureCanvas();
        _root = _canvas.GetComponent<RectTransform>();

        if (_font == null)
            _font = TMP_Settings.defaultFontAsset;

        BuildBackdrop();
        BuildMainScreen();
        BuildMissionsScreen();
        BuildSettingsScreen();
        BuildCreditsScreen();

        ShowMain();
    }

    private void ApplyMissionControlPalette()
    {
        _backgroundColor = FromHex("#070B10", _backgroundColor);
        _panelColor = new Color(0.02f, 0.025f, 0.034f, 0.86f);
        _cardColor = new Color(0.055f, 0.06f, 0.075f, 0.9f);
        _borderColor = new Color(0.28f, 0.30f, 0.34f, 0.45f);
        _textPrimary = FromHex("#F3F6FA", _textPrimary);
        _textSecondary = FromHex("#9BA7B8", _textSecondary);
        _textMuted = FromHex("#9BA7B8", _textMuted);
        _accent = FromHex("#FFB020", _accent);
        _accentSoft = FromHex("#FFB020", _accentSoft);
        _statusInfo = FromHex("#2EA8FF", _statusInfo);
        _statusOnline = FromHex("#3DDC97", _statusOnline);
        _statusLocked = FromHex("#9BA7B8", _statusLocked);
        _buttonTextNormal = _textPrimary;
        _buttonTextHover = _accent;
        _buttonTextPressed = MultiplyAlpha(_accent, 0.78f);
        _buttonTextDisabled = MultiplyAlpha(_textMuted, 0.45f);
    }

    // ───────────── Bootstrap ─────────────

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        go.transform.SetParent(transform, false);
    }

    private void EnsureCamera()
    {
        if (Camera.main != null) return;
        var go = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        var cam = go.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = _backgroundColor;
    }

    private Canvas EnsureCanvas()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas;

        var go = new GameObject("MainMenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // ───────────── Backdrop + vignette ─────────────

    private void BuildBackdrop()
    {
        var bg = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(_root, false);
        Stretch(bg.GetComponent<RectTransform>());
        var bgImg = bg.GetComponent<Image>();
        bgImg.raycastTarget = false;
        if (_backgroundSprite != null)
        {
            bgImg.sprite = _backgroundSprite;
            bgImg.color = Color.white;
            bgImg.preserveAspect = false;
            bgImg.type = Image.Type.Simple;
        }
        else
        {
            bgImg.color = _backgroundColor;
        }

        // Uniform vignette/dim layer for content readability
        var dim = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dim.transform.SetParent(_root, false);
        Stretch(dim.GetComponent<RectTransform>());
        var dimImg = dim.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, _vignetteStrength);
        dimImg.raycastTarget = false;

        // Soft horizontal gradient strip on the left for menu legibility
        var leftDim = new GameObject("LeftFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        leftDim.transform.SetParent(_root, false);
        var lr = leftDim.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0);
        lr.anchorMax = new Vector2(0, 1);
        lr.pivot = new Vector2(0, 0.5f);
        lr.anchoredPosition = Vector2.zero;
        lr.sizeDelta = new Vector2(720, 0);
        var lImg = leftDim.GetComponent<Image>();
        lImg.color = new Color(0f, 0f, 0f, 0.35f);
        lImg.raycastTarget = false;
    }

    // ───────────── Main screen ─────────────

    private void BuildMainScreen()
    {
        _mainScreen = CreateScreen("MainScreen");

        var column = CreateRect("LeftColumn", _mainScreen);
        column.anchorMin = new Vector2(0f, 0.5f);
        column.anchorMax = new Vector2(0f, 0.5f);
        column.pivot = new Vector2(0f, 0.5f);
        column.anchoredPosition = new Vector2(_leftMargin, 40f);
        column.sizeDelta = new Vector2(560, 600);

        var columnVlg = column.gameObject.AddComponent<VerticalLayoutGroup>();
        columnVlg.spacing = 32;
        columnVlg.childAlignment = TextAnchor.UpperLeft;
        columnVlg.childForceExpandWidth = false;
        columnVlg.childForceExpandHeight = false;
        columnVlg.childControlWidth = true;
        columnVlg.childControlHeight = true;

        BuildTitleBlock(column);

        var divider = CreateRect("Divider", column);
        var divLE = divider.gameObject.AddComponent<LayoutElement>();
        divLE.preferredWidth = 320;
        divLE.preferredHeight = 2;
        var divImg = divider.gameObject.AddComponent<Image>();
        divImg.color = _accent;
        divImg.raycastTarget = false;

        var buttonGroup = CreateRect("ButtonGroup", column);
        var btnLE = buttonGroup.gameObject.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 360;

        var vlg = buttonGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        _continueButton = CreateMenuButton(buttonGroup, "ПРОДОЛЖИТЬ", OnContinueClicked);
        CreateMenuButton(buttonGroup, "НАЧАТЬ ОБУЧЕНИЕ", OnNewGameClicked);
        CreateMenuButton(buttonGroup, "МИССИИ", OnMissionsClicked);
        CreateMenuButton(buttonGroup, "НАСТРОЙКИ", () => SwitchTo(_settingsScreen));
        CreateMenuButton(buttonGroup, "АВТОРЫ", () => SwitchTo(_creditsScreen));
        CreateMenuButton(buttonGroup, "ВЫХОД", OnQuitClicked);

        var footer = CreateText(_mainScreen, "Footer", "v0.1 — alpha",
            14, _textMuted, FontStyles.Normal, TextAlignmentOptions.Left);
        var fr = footer.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(0f, 0f);
        fr.pivot = new Vector2(0f, 0f);
        fr.anchoredPosition = new Vector2(_leftMargin, 24);
        fr.sizeDelta = new Vector2(400, 20);
    }

    private void BuildTitleBlock(RectTransform parent)
    {
        if (_titleSprite != null)
        {
            var go = new GameObject("TitleLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = _titleSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = _titleSize.x;
            le.preferredHeight = _titleSize.y;
            return;
        }

        var titleGroup = CreateRect("TitleGroup", parent);
        var titleLE = titleGroup.gameObject.AddComponent<LayoutElement>();
        titleLE.preferredWidth = _titleSize.x;
        titleLE.preferredHeight = _titleSize.y;

        var titleVlg = titleGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        titleVlg.spacing = 4;
        titleVlg.childAlignment = TextAnchor.UpperLeft;
        titleVlg.childForceExpandWidth = true;
        titleVlg.childForceExpandHeight = false;

        var title = CreateText(titleGroup, "Title", _gameTitle,
            104, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        title.characterSpacing = 8f;
        title.GetComponent<LayoutElement>().preferredHeight = 116;

        var subtitle = CreateText(titleGroup, "Subtitle", _gameSubtitle,
            22, _accentSoft, FontStyles.UpperCase, TextAlignmentOptions.Left);
        subtitle.characterSpacing = 12f;
        subtitle.GetComponent<LayoutElement>().preferredHeight = 30;
    }

    // ───────────── Missions screen ─────────────

    private void BuildMissionsScreen()
    {
        _missionsScreen = CreateScreen("MissionsScreen");

        // Header block (top-left)
        var headerGroup = CreateRect("HeaderGroup", _missionsScreen);
        headerGroup.anchorMin = new Vector2(0, 1);
        headerGroup.anchorMax = new Vector2(0, 1);
        headerGroup.pivot = new Vector2(0, 1);
        headerGroup.anchoredPosition = new Vector2(_leftMargin, -60);
        headerGroup.sizeDelta = new Vector2(900, 100);

        var headerVlg = headerGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        headerVlg.spacing = 4;
        headerVlg.childAlignment = TextAnchor.UpperLeft;
        headerVlg.childForceExpandWidth = false;
        headerVlg.childForceExpandHeight = false;
        headerVlg.childControlWidth = true;
        headerVlg.childControlHeight = true;

        var header = CreateText(headerGroup, "ScreenHeader", "МИССИИ",
            56, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        header.characterSpacing = 4f;
        header.GetComponent<LayoutElement>().preferredHeight = 64;

        _missionsCounter = CreateText(headerGroup, "Counter", "",
            17, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
        _missionsCounter.characterSpacing = 8f;
        _missionsCounter.GetComponent<LayoutElement>().preferredHeight = 22;
        BuildMissionsProgress(headerGroup);

        // Body: mission cards and briefing.
        var body = CreateRect("Body", _missionsScreen);
        body.anchorMin = new Vector2(0, 0);
        body.anchorMax = new Vector2(1, 1);
        body.pivot = new Vector2(0.5f, 0.5f);
        body.offsetMin = new Vector2(80, 96);
        body.offsetMax = new Vector2(-80, -175);

        var hlg = body.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 22;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // List column
        var listColumn = CreatePanel(body, "MissionCardsPanel", _panelColor, new RectOffset(18, 18, 18, 18));
        var listLE = listColumn.gameObject.AddComponent<LayoutElement>();
        listLE.preferredWidth = 520;
        listLE.flexibleWidth = 0;

        var listVlg = listColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        listVlg.padding = new RectOffset(18, 18, 18, 18);
        listVlg.spacing = 8;
        listVlg.childForceExpandWidth = true;
        listVlg.childForceExpandHeight = false;
        listVlg.childControlWidth = true;
        listVlg.childControlHeight = true;

        var listHeaderRow = CreateRect("ListHeaderRow", listColumn);
        var listHeaderRowLE = listHeaderRow.gameObject.AddComponent<LayoutElement>();
        listHeaderRowLE.preferredHeight = 30;
        var listHeaderHlg = listHeaderRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        listHeaderHlg.spacing = 8;
        listHeaderHlg.childAlignment = TextAnchor.MiddleLeft;
        listHeaderHlg.childForceExpandWidth = false;
        listHeaderHlg.childForceExpandHeight = true;
        listHeaderHlg.childControlWidth = true;
        listHeaderHlg.childControlHeight = true;

        var listHeader = CreateText(listHeaderRow, "ListHeader", "СПИСОК МИССИЙ",
            17, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
        listHeader.characterSpacing = 8f;
        listHeader.GetComponent<LayoutElement>().flexibleWidth = 1;

        // Thin separator under list header
        var listSep = CreateRect("Sep", listColumn);
        var listSepLE = listSep.gameObject.AddComponent<LayoutElement>();
        listSepLE.preferredHeight = 1;
        var listSepImg = listSep.gameObject.AddComponent<Image>();
        listSepImg.color = new Color(1f, 1f, 1f, 0.10f);
        listSepImg.raycastTarget = false;

        _missionListContent = CreateMissionListScroll(listColumn);
        PopulateMissionList();
        PopulateUserMissions(_missionListContent);
        UpdateMissionsCounter();

        // Details column
        var detailsColumn = CreatePanel(body, "MissionBriefingPanel", _panelColor, new RectOffset(24, 24, 22, 22));
        var detailsLE = detailsColumn.gameObject.AddComponent<LayoutElement>();
        detailsLE.flexibleWidth = 1;

        var detailsVlg = detailsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        detailsVlg.padding = new RectOffset(24, 24, 22, 22);
        detailsVlg.spacing = 14;
        detailsVlg.childForceExpandWidth = true;
        detailsVlg.childForceExpandHeight = false;
        detailsVlg.childControlWidth = true;
        detailsVlg.childControlHeight = true;

        BuildDetailsContent(detailsColumn);
        BuildDetailsEmpty(detailsColumn);

        // Bottom: Back button (text style)
        var backBtn = CreateMenuButton(_missionsScreen, "← НАЗАД", () => SwitchTo(_mainScreen));
        var backRT = backBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0, 0);
        backRT.anchorMax = new Vector2(0, 0);
        backRT.pivot = new Vector2(0, 0);
        backRT.anchoredPosition = new Vector2(_leftMargin, 30);
        backRT.sizeDelta = new Vector2(220, 50);
        var backLE = backBtn.GetComponent<LayoutElement>();
        if (backLE != null) Destroy(backLE);

        BuildUserMissionEditor(_missionsScreen);
        SelectFirstMissionForDetails();
    }

    private void BuildMissionsProgress(RectTransform parent)
    {
        var row = CreateRect("MissionsProgress", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredWidth = 460;
        rowLE.preferredHeight = 18;
        rowLE.flexibleWidth = 0;
        rowLE.flexibleHeight = 0;

        var track = CreateRect("Track", row);
        track.anchorMin = new Vector2(0f, 0.5f);
        track.anchorMax = new Vector2(0f, 0.5f);
        track.pivot = new Vector2(0f, 0.5f);
        track.anchoredPosition = new Vector2(0f, -1f);
        track.sizeDelta = new Vector2(330f, 7f);
        var trackImg = track.gameObject.AddComponent<Image>();
        trackImg.color = new Color(1f, 1f, 1f, 0.10f);
        trackImg.raycastTarget = false;

        var fill = CreateRect("Fill", track);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        _missionsProgressFill = fill.gameObject.AddComponent<Image>();
        _missionsProgressFill.color = _accentSoft;
        _missionsProgressFill.raycastTarget = false;

        _missionsProgressText = CreateText(row, "ProgressText", "",
            13, _textMuted, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        _missionsProgressText.characterSpacing = 3f;
        var textRT = _missionsProgressText.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(0f, 1f);
        textRT.pivot = new Vector2(0f, 0.5f);
        textRT.anchoredPosition = new Vector2(348f, 0f);
        textRT.sizeDelta = new Vector2(170f, 0f);
    }

    private RectTransform CreateMissionListScroll(RectTransform parent)
    {
        var viewport = CreateRect("MissionListViewport", parent);
        var viewportLE = viewport.gameObject.AddComponent<LayoutElement>();
        viewportLE.minHeight = 160f;
        viewportLE.flexibleHeight = 1f;

        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = CreateRect("MissionListContent", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var contentVlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 8;
        contentVlg.childAlignment = TextAnchor.UpperLeft;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 28f;

        return content;
    }

    private void BuildDetailsContent(RectTransform parent)
    {
        var container = CreateRect("DetailsContent", parent);
        _detailsContent = container.gameObject;
        var vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // Status — uppercase muted line above the title
        _detailsStatusText = CreateText(container, "DetailsStatus", "",
            17, _statusInfo, FontStyles.UpperCase, TextAlignmentOptions.Left);
        _detailsStatusText.characterSpacing = 8f;
        _detailsStatusText.GetComponent<LayoutElement>().preferredHeight = 22;

        _detailsTitle = CreateText(container, "DetailsTitle", "—",
            44, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        _detailsTitle.characterSpacing = 2f;
        _detailsTitle.GetComponent<LayoutElement>().preferredHeight = 52;

        // Subtle divider
        var dividerWrap = CreateRect("Divider", container);
        var dividerLE = dividerWrap.gameObject.AddComponent<LayoutElement>();
        dividerLE.preferredHeight = 2;
        var divImg = dividerWrap.gameObject.AddComponent<Image>();
        divImg.color = MultiplyAlpha(_accent, 0.6f);
        divImg.raycastTarget = false;

        // Reward inline
        _detailsRewardText = CreateText(container, "DetailsReward", "",
            17, _accent, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        _detailsRewardText.characterSpacing = 6f;
        _detailsRewardText.GetComponent<LayoutElement>().preferredHeight = 24;

        _detailsSceneText = CreateText(container, "DetailsScene", "",
            15, _textMuted, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        _detailsSceneText.characterSpacing = 4f;
        _detailsSceneText.GetComponent<LayoutElement>().preferredHeight = 20;

        // Section: Objective
        CreateSectionLabel(container, "ЦЕЛЬ МИССИИ", _accentSoft);
        _detailsObjective = CreateText(container, "DetailsObjective", "",
            20, _textPrimary, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var objLE = _detailsObjective.GetComponent<LayoutElement>();
        objLE.preferredHeight = 70;
        objLE.flexibleHeight = 0;

        // Section: Briefing
        CreateSectionLabel(container, "БРИФИНГ", _statusInfo);
        _detailsDescription = CreateText(container, "DetailsDescription", "",
            18, _textSecondary, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var descLE = _detailsDescription.GetComponent<LayoutElement>();
        descLE.preferredHeight = 104;
        descLE.flexibleHeight = 0;

        CreateSectionLabel(container, "КЛЮЧЕВЫЕ ЦЕЛИ", _statusOnline);
        _detailsChecklist = CreateRect("DetailsChecklist", container);
        var checklistLE = _detailsChecklist.gameObject.AddComponent<LayoutElement>();
        checklistLE.preferredHeight = 150;
        checklistLE.flexibleHeight = 0;
        var checklistVlg = _detailsChecklist.gameObject.AddComponent<VerticalLayoutGroup>();
        checklistVlg.spacing = 6;
        checklistVlg.childAlignment = TextAnchor.UpperLeft;
        checklistVlg.childForceExpandWidth = true;
        checklistVlg.childForceExpandHeight = false;
        checklistVlg.childControlWidth = true;
        checklistVlg.childControlHeight = true;

        var spacer = CreateRect("Spacer", container);
        spacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

        // Launch — text-style CTA in accent color
        BuildLaunchButton(container);
    }

    private void BuildDetailsEmpty(RectTransform parent)
    {
        var empty = CreateRect("DetailsEmpty", parent);
        _detailsEmpty = empty.gameObject;
        var vlg = empty.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 60, 0);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var hint = CreateText(empty, "Hint", "Выберите миссию слева",
            24, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
        hint.characterSpacing = 1f;
        hint.GetComponent<LayoutElement>().preferredHeight = 34;

        var sub = CreateText(empty, "Sub", "Брифинг, награда и ключевые цели появятся в центре управления.",
            19, _textSecondary, FontStyles.Normal, TextAlignmentOptions.Center);
        sub.GetComponent<LayoutElement>().preferredHeight = 54;

        var createButton = CreateEditorButton(empty, "+  СОЗДАТЬ СВОЮ МИССИЮ", _accentSoft, ShowEditorForNew);
        var createLE = createButton.gameObject.AddComponent<LayoutElement>();
        createLE.preferredWidth = 340;
        createLE.preferredHeight = 40;
        createLE.flexibleWidth = 0;
    }

    private void BuildLaunchButton(RectTransform parent)
    {
        var go = new GameObject("LaunchButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        // Transparent background — pure click area
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(14, 0);
        labelRT.offsetMax = new Vector2(-14, 0);
        _launchButtonLabel = labelGO.GetComponent<TextMeshProUGUI>();
        _launchButtonLabel.text = ">  ЗАПУСК МИССИИ";
        if (_font != null) _launchButtonLabel.font = _font;
        _launchButtonLabel.fontSize = 30;
        _launchButtonLabel.color = _accent;
        _launchButtonLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        _launchButtonLabel.alignment = TextAlignmentOptions.Left;
        _launchButtonLabel.characterSpacing = 6f;
        _launchButtonLabel.raycastTarget = false;

        _launchButton = go.GetComponent<Button>();
        _launchButton.targetGraphic = _launchButtonLabel;
        var colors = _launchButton.colors;
        colors.normalColor = _accent;
        colors.highlightedColor = _buttonTextHover;
        colors.pressedColor = _buttonTextPressed;
        colors.selectedColor = _buttonTextHover;
        colors.disabledColor = _buttonTextDisabled;
        colors.fadeDuration = 0.1f;
        _launchButton.colors = colors;
        _launchButton.onClick.AddListener(OnLaunchMission);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 56;

        var hover = go.AddComponent<MenuTextShift>();
        hover.target = labelGO.GetComponent<RectTransform>();
        hover.shiftOnHover = 8f;
    }

    // ───────────── Mission rows ─────────────

    private void PopulateMissionList()
    {
        _rows.Clear();
        if (_missions == null) return;

        for (int i = 0; i < _missions.Length; i++)
        {
            var mission = _missions[i];
            if (mission == null) continue;
            CreateMissionRow(_missionListContent, mission);
        }
    }

    private void UpdateMissionsCounter()
    {
        if (_missionsCounter == null || _missions == null) return;
        int total = 0, done = 0, locked = 0;
        for (int i = 0; i < _missions.Length; i++)
        {
            var m = _missions[i];
            if (m == null) continue;
            total++;
            if (MissionProgress.IsCompleted(m.id)) done++;
            if (MissionProgress.GetStatus(m) == MissionStatus.Locked) locked++;
        }

        var userMissions = UserMissionStore.LoadAll();
        int userDone = 0;
        for (int i = 0; i < userMissions.Count; i++)
        {
            if (userMissions[i] != null && UserMissionStore.IsCompleted(userMissions[i].id))
                userDone++;
        }

        _missionsCounter.text = $"{done}/{total} ОСНОВНЫХ  ·  {userDone}/{userMissions.Count} СВОИХ  ·  {locked} ЗАКРЫТО";

        var progress = total <= 0 ? 0f : Mathf.Clamp01((float)done / total);
        if (_missionsProgressFill != null)
        {
            var rt = _missionsProgressFill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(progress, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (_missionsProgressText != null)
            _missionsProgressText.text = $"{Mathf.RoundToInt(progress * 100f)}% ПРОЙДЕНО";
    }

    private void CreateMissionRow(RectTransform parent, Mission mission)
    {
        var go = new GameObject($"MissionRow_{mission.id}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        var bgImage = go.GetComponent<Image>();
        bgImage.color = _cardColor;
        AddBorder(go, MultiplyAlpha(_borderColor, 0.9f));

        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = bgImage;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 78;
        le.minHeight = 78;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 12, 8, 8);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var accentBarGO = new GameObject("AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentBarGO.transform.SetParent(go.transform, false);
        var accentBar = accentBarGO.GetComponent<Image>();
        accentBar.color = MultiplyAlpha(_accentSoft, 0f);
        accentBar.raycastTarget = false;
        var accentLE = accentBarGO.AddComponent<LayoutElement>();
        accentLE.preferredWidth = 4;
        accentLE.flexibleWidth = 0;

        var number = CreateText(go.GetComponent<RectTransform>(), "Number", GetMissionNumber(mission),
            20, _accent, FontStyles.Bold, TextAlignmentOptions.Center);
        number.characterSpacing = 2f;
        number.raycastTarget = false;
        var numberLE = number.GetComponent<LayoutElement>();
        numberLE.preferredWidth = 40;
        numberLE.flexibleWidth = 0;

        var textCol = CreateRect("TextCol", go.GetComponent<RectTransform>());
        var textColLE = textCol.gameObject.AddComponent<LayoutElement>();
        textColLE.flexibleWidth = 1;
        var textVlg = textCol.gameObject.AddComponent<VerticalLayoutGroup>();
        textVlg.spacing = 1;
        textVlg.childAlignment = TextAnchor.MiddleLeft;
        textVlg.childForceExpandWidth = true;
        textVlg.childForceExpandHeight = false;
        textVlg.childControlWidth = true;
        textVlg.childControlHeight = true;

        var title = CreateText(textCol, "Title", mission.title,
            17, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        title.GetComponent<LayoutElement>().preferredHeight = 22;
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.raycastTarget = false;

        var status = MissionProgress.GetStatus(mission);
        Color statusColor = status switch
        {
            MissionStatus.Completed => _statusOnline,
            MissionStatus.Locked => _statusLocked,
            _ => _statusInfo
        };
        string statusText = status switch
        {
            MissionStatus.Completed => "ВЫПОЛНЕНА",
            MissionStatus.Locked => "ЗАБЛОКИРОВАНА",
            _ => "ДОСТУПНА"
        };
        var meta = CreateText(textCol, "Meta", $"ОСНОВНАЯ / {statusText}",
            11, statusColor, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        meta.characterSpacing = 1f;
        meta.GetComponent<LayoutElement>().preferredHeight = 16;
        meta.raycastTarget = false;

        var reward = CreateText(textCol, "Reward", mission.rewardScience > 0 ? $"{mission.rewardScience} SCI" : "без награды",
            11, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
        reward.characterSpacing = 1f;
        reward.GetComponent<LayoutElement>().preferredHeight = 16;
        reward.raycastTarget = false;

        // Chevron right
        var chevGO = new GameObject("Chevron", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        chevGO.transform.SetParent(go.transform, false);
        var chevTmp = chevGO.GetComponent<TextMeshProUGUI>();
        chevTmp.text = "›";
        chevTmp.fontSize = 32;
        chevTmp.color = new Color(1f, 1f, 1f, 0.25f);
        chevTmp.alignment = TextAlignmentOptions.MidlineRight;
        if (_font != null) chevTmp.font = _font;
        chevTmp.raycastTarget = false;
        var chevLE = chevGO.AddComponent<LayoutElement>();
        chevLE.preferredWidth = 24;
        chevLE.flexibleWidth = 0;

        var view = go.AddComponent<MissionRowView>();
        view.background = bgImage;
        view.accentBar = accentBar;
        view.titleText = title;
        view.chevron = chevTmp;
        view.rowNormal = _cardColor;
        view.rowHover = new Color(0.11f, 0.16f, 0.23f, 1f);
        view.rowSelected = new Color(0.23f, 0.16f, 0.05f, 1f);
        view.rowLocked = MultiplyAlpha(_cardColor, 0.55f);
        view.titleNormal = _buttonTextNormal;
        view.titleHover = _buttonTextHover;
        view.titleSelected = _accentSoft;
        view.titleLocked = _textMuted;
        view.accentSelected = _accentSoft;
        view.accentHover = MultiplyAlpha(_accentSoft, 0.55f);
        view.chevronActive = _accentSoft;

        bool locked = status == MissionStatus.Locked;
        view.SetLocked(locked);
        _rows.Add(view);

        btn.onClick.AddListener(() => SelectMission(mission, view));
    }

    private void SelectMission(Mission mission, MissionRowView row)
    {
        _selectedMission = mission;

        if (_selectedRow != null && _selectedRow != row)
            _selectedRow.SetSelected(false);
        _selectedRow = row;
        if (row != null) row.SetSelected(true);

        if (mission == null) { ClearDetails(); return; }

        if (_detailsContent != null) _detailsContent.SetActive(true);
        if (_detailsEmpty != null) _detailsEmpty.SetActive(false);

        _detailsTitle.text = mission.title;
        _detailsObjective.text = string.IsNullOrEmpty(mission.objective) ? "—" : mission.objective;
        _detailsDescription.text = string.IsNullOrEmpty(mission.description) ? "—" : mission.description;

        var status = MissionProgress.GetStatus(mission);
        var statusColor = status switch
        {
            MissionStatus.Completed => _statusOnline,
            MissionStatus.Locked => _statusLocked,
            _ => _statusInfo
        };
        _detailsStatusText.text = status switch
        {
            MissionStatus.Completed => "СТАТУС: ВЫПОЛНЕНА",
            MissionStatus.Locked => "СТАТУС: ЗАБЛОКИРОВАНА",
            _ => "СТАТУС: ДОСТУПНА"
        };
        _detailsStatusText.color = statusColor;
        SetDetailsChecklist(GetBuiltInMissionConditions(mission), status == MissionStatus.Completed);
        if (_detailsSceneText != null)
        {
            var scene = string.IsNullOrEmpty(mission.sceneName) ? _defaultGameScene : mission.sceneName;
            _detailsSceneText.text = $"СЦЕНА: {scene}";
        }

        if (mission.rewardScience > 0)
        {
            _detailsRewardText.gameObject.SetActive(true);
            _detailsRewardText.text = $"SCI  НАГРАДА:  {mission.rewardScience}";
        }
        else
        {
            _detailsRewardText.gameObject.SetActive(false);
        }

        // Restore launch handler for built-in missions (was rebound by user mission selection)
        _launchButton.onClick.RemoveAllListeners();
        _launchButton.onClick.AddListener(OnLaunchMission);
        _launchButton.interactable = status != MissionStatus.Locked;
        if (_launchButtonLabel != null)
        {
            _launchButtonLabel.text = status switch
            {
                MissionStatus.Completed => ">  ПОВТОРИТЬ МИССИЮ",
                MissionStatus.Locked => "×  МИССИЯ ЗАБЛОКИРОВАНА",
                _ => ">  ЗАПУСТИТЬ МИССИЮ"
            };
        }
    }

    private void ClearDetails()
    {
        _selectedMission = null;
        if (_selectedRow != null) { _selectedRow.SetSelected(false); _selectedRow = null; }
        if (_detailsContent != null) _detailsContent.SetActive(false);
        if (_detailsEmpty != null) _detailsEmpty.SetActive(true);
    }

    private void SelectFirstMissionForDetails()
    {
        Mission fallbackMission = null;
        MissionRowView fallbackRow = null;
        int rowIndex = 0;

        if (_missions != null)
        {
            for (int i = 0; i < _missions.Length; i++)
            {
                var mission = _missions[i];
                if (mission == null) continue;

                var row = rowIndex < _rows.Count ? _rows[rowIndex] : null;
                rowIndex++;

                if (fallbackMission == null)
                {
                    fallbackMission = mission;
                    fallbackRow = row;
                }

                if (MissionProgress.GetStatus(mission) != MissionStatus.Locked)
                {
                    SelectMission(mission, row);
                    return;
                }
            }
        }

        if (fallbackMission != null)
            SelectMission(fallbackMission, fallbackRow);
        else
            ClearDetails();
    }

    private void SetDetailsChecklist(List<MissionConditionData> conditions, bool completed)
    {
        if (_detailsChecklist == null) return;
        for (int i = _detailsChecklist.childCount - 1; i >= 0; i--)
            Destroy(_detailsChecklist.GetChild(i).gameObject);

        if (conditions == null || conditions.Count == 0)
            conditions = GetDefaultMissionConditions();

        for (int i = 0; i < conditions.Count; i++)
        {
            var row = CreateRect($"Goal_{i}", _detailsChecklist);
            var rowLE = row.gameObject.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 28;
            rowLE.flexibleHeight = 0;

            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var check = CreateText(row, "Bullet", "•",
                20, completed ? _statusOnline : _textMuted, FontStyles.Bold, TextAlignmentOptions.Center);
            var checkLE = check.GetComponent<LayoutElement>();
            checkLE.preferredWidth = 26;
            checkLE.flexibleWidth = 0;
            check.raycastTarget = false;

            var label = CreateText(row, "Label", GetConditionDisplayName(conditions[i]),
                16, completed ? _statusOnline : _textSecondary, FontStyles.Normal, TextAlignmentOptions.Left);
            label.GetComponent<LayoutElement>().flexibleWidth = 1;
            label.raycastTarget = false;
        }
    }

    // ───────────── User missions ─────────────

    private void PopulateUserMissions(RectTransform parent)
    {
        // Header divider with section label and "+" button
        var sectionRow = CreateRect("UserMissionsHeader", parent);
        var sectionLE = sectionRow.gameObject.AddComponent<LayoutElement>();
        sectionLE.preferredHeight = 32;
        var hlg = sectionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 16, 0);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var label = CreateText(sectionRow, "Label", "СВОИ МИССИИ",
            17, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
        label.characterSpacing = 8f;
        var labelLE = label.GetComponent<LayoutElement>();
        labelLE.preferredHeight = 24;
        labelLE.flexibleWidth = 1;

        // Plus button
        var plusGO = new GameObject("BtnAdd",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        plusGO.transform.SetParent(sectionRow, false);
        plusGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var plusLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        plusLabelGO.transform.SetParent(plusGO.transform, false);
        Stretch(plusLabelGO.GetComponent<RectTransform>());
        var plusTmp = plusLabelGO.GetComponent<TextMeshProUGUI>();
        plusTmp.text = "+ СОЗДАТЬ";
        if (_font != null) plusTmp.font = _font;
        plusTmp.fontSize = 15;
        plusTmp.color = _accentSoft;
        plusTmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        plusTmp.alignment = TextAlignmentOptions.Right;
        plusTmp.characterSpacing = 4f;
        plusTmp.raycastTarget = false;

        var plusBtn = plusGO.GetComponent<Button>();
        plusBtn.targetGraphic = plusTmp;
        var plusColors = plusBtn.colors;
        plusColors.normalColor = _accentSoft;
        plusColors.highlightedColor = Color.white;
        plusColors.pressedColor = _buttonTextPressed;
        plusColors.fadeDuration = 0.1f;
        plusBtn.colors = plusColors;
        plusBtn.onClick.AddListener(() => ShowEditorForNew());

        var plusLE = plusGO.AddComponent<LayoutElement>();
        plusLE.preferredWidth = 140;
        plusLE.flexibleWidth = 0;
        plusLE.preferredHeight = 24;

        // Thin separator
        var sep = CreateRect("UserSep", parent);
        var sepLE = sep.gameObject.AddComponent<LayoutElement>();
        sepLE.preferredHeight = 1;
        var sepImg = sep.gameObject.AddComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.10f);
        sepImg.raycastTarget = false;

        // Container that we'll repopulate on changes
        var container = CreateRect("UserMissionsList", parent);
        _userMissionsContainer = container;
        var vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        RefreshUserMissionRows();
    }

    private void RefreshUserMissionRows()
    {
        if (_userMissionsContainer == null) return;
        _userRows.Clear();
        for (int i = _userMissionsContainer.childCount - 1; i >= 0; i--)
            Destroy(_userMissionsContainer.GetChild(i).gameObject);

        var list = UserMissionStore.LoadAll();
        const int rowSpacing = 6;
        int totalHeight;

        if (list.Count == 0)
        {
            var empty = CreateText(_userMissionsContainer, "Empty",
                "Пусто. Нажми «+ СОЗДАТЬ» чтобы добавить.",
                15, _textMuted, FontStyles.Italic, TextAlignmentOptions.Left);
            var emptyLE = empty.GetComponent<LayoutElement>();
            emptyLE.preferredHeight = 24;
            emptyLE.flexibleHeight = 0;
            totalHeight = 24;
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
                CreateUserMissionRow(_userMissionsContainer, list[i]);
            totalHeight = list.Count * 78 + (list.Count - 1) * rowSpacing;
        }

        // Pin container size so parent VLG (childControlHeight) doesn't stretch row
        var containerLE = _userMissionsContainer.gameObject.GetComponent<LayoutElement>()
                          ?? _userMissionsContainer.gameObject.AddComponent<LayoutElement>();
        containerLE.minHeight = totalHeight;
        containerLE.preferredHeight = totalHeight;
        containerLE.flexibleHeight = 0;
    }

    private void CreateUserMissionRow(RectTransform parent, UserMission mission)
    {
        var go = new GameObject($"UserMissionRow_{mission.id}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        var bgImage = go.GetComponent<Image>();
        bgImage.color = _cardColor;
        AddBorder(go, MultiplyAlpha(_borderColor, 0.9f));

        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = bgImage;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 78;
        le.minHeight = 78;
        le.flexibleHeight = 0;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 10, 8, 8);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        bool completed = UserMissionStore.IsCompleted(mission.id);

        var accentBarGO = new GameObject("AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentBarGO.transform.SetParent(go.transform, false);
        var accentBar = accentBarGO.GetComponent<Image>();
        accentBar.color = MultiplyAlpha(_statusInfo, 0f);
        accentBar.raycastTarget = false;
        var accentLE = accentBarGO.AddComponent<LayoutElement>();
        accentLE.preferredWidth = 4;
        accentLE.flexibleWidth = 0;

        var number = CreateText(go.GetComponent<RectTransform>(), "Number", "U" + GetUserMissionNumber(mission),
            18, _statusInfo, FontStyles.Bold, TextAlignmentOptions.Center);
        number.characterSpacing = 0f;
        number.raycastTarget = false;
        var numberLE = number.GetComponent<LayoutElement>();
        numberLE.preferredWidth = 38;
        numberLE.flexibleWidth = 0;

        var textCol = CreateRect("TextCol", go.GetComponent<RectTransform>());
        var textColLE = textCol.gameObject.AddComponent<LayoutElement>();
        textColLE.flexibleWidth = 1;
        var textVlg = textCol.gameObject.AddComponent<VerticalLayoutGroup>();
        textVlg.spacing = 1;
        textVlg.childAlignment = TextAnchor.MiddleLeft;
        textVlg.childForceExpandWidth = true;
        textVlg.childForceExpandHeight = false;
        textVlg.childControlWidth = true;
        textVlg.childControlHeight = true;

        var title = CreateText(textCol, "Title",
            string.IsNullOrEmpty(mission.title) ? "<без названия>" : mission.title,
            16, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        title.GetComponent<LayoutElement>().preferredHeight = 21;
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.raycastTarget = false;

        var sub = CreateText(textCol, "Sub",
            completed ? "СВОЯ / ВЫПОЛНЕНА" : "СВОЯ / ДОСТУПНА",
            11, completed ? _statusOnline : _statusInfo, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        sub.characterSpacing = 1f;
        sub.GetComponent<LayoutElement>().preferredHeight = 16;
        sub.raycastTarget = false;

        var reward = CreateText(textCol, "Reward", $"{mission.rewardScience} SCI / {GetMissionDifficulty(mission)}",
            11, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
        reward.characterSpacing = 1f;
        reward.GetComponent<LayoutElement>().preferredHeight = 16;
        reward.raycastTarget = false;

        var view = go.AddComponent<MissionRowView>();
        view.background = bgImage;
        view.accentBar = accentBar;
        view.titleText = title;
        view.rowNormal = _cardColor;
        view.rowHover = new Color(0.11f, 0.16f, 0.23f, 1f);
        view.rowSelected = new Color(0.23f, 0.16f, 0.05f, 1f);
        view.titleNormal = _textPrimary;
        view.titleHover = _accentSoft;
        view.titleSelected = _accentSoft;
        view.accentSelected = _statusInfo;
        view.accentHover = MultiplyAlpha(_statusInfo, 0.55f);
        if (!string.IsNullOrEmpty(mission.id))
            _userRows[mission.id] = view;

        CreateMiniIconButton(go.transform, "Edit", "РЕД", _statusInfo, () => ShowEditorForEdit(mission));

        btn.onClick.AddListener(() => SelectUserMission(mission, view));
    }

    private GameObject CreateMiniIconButton(Transform parent, string name, string glyph, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Btn_{name}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = glyph;
        if (_font != null) tmp.font = _font;
        tmp.fontSize = glyph.Length > 1 ? 11 : 14;
        tmp.color = MultiplyAlpha(color, 0.6f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = tmp;
        var c = btn.colors;
        c.normalColor = MultiplyAlpha(color, 0.6f);
        c.highlightedColor = color;
        c.pressedColor = MultiplyAlpha(color, 0.85f);
        c.fadeDuration = 0.1f;
        btn.colors = c;
        btn.onClick.AddListener(onClick);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = glyph.Length > 1 ? 34 : 24;
        le.preferredHeight = 28;
        le.minHeight = 28;
        le.flexibleWidth = 0;
        return go;
    }

    private void SelectUserMission(UserMission mission, MissionRowView row = null)
    {
        if (mission == null) { ClearDetails(); return; }

        if (_selectedRow != null && _selectedRow != row) _selectedRow.SetSelected(false);
        _selectedRow = row;
        if (row != null) row.SetSelected(true);
        _selectedMission = null;

        if (_detailsContent != null) _detailsContent.SetActive(true);
        if (_detailsEmpty != null) _detailsEmpty.SetActive(false);

        _detailsTitle.text = mission.title;
        _detailsObjective.text = string.IsNullOrEmpty(mission.objective) ? "—" : mission.objective;
        _detailsDescription.text = string.IsNullOrEmpty(mission.description) ? "—" : mission.description;

        bool done = UserMissionStore.IsCompleted(mission.id);
        _detailsStatusText.text = done
            ? "СТАТУС: ВЫПОЛНЕНА / ПОЛЬЗОВАТЕЛЬСКАЯ"
            : "СТАТУС: ДОСТУПНА / ПОЛЬЗОВАТЕЛЬСКАЯ";
        _detailsStatusText.color = done ? _statusOnline : _statusInfo;
        SetDetailsChecklist(GetUserMissionConditions(mission), done);
        if (_detailsSceneText != null)
        {
            var scene = string.IsNullOrEmpty(mission.sceneName) ? _defaultGameScene : mission.sceneName;
            _detailsSceneText.text = $"СЦЕНА: {scene}  ·  СЛОЖНОСТЬ: {GetMissionDifficulty(mission)}";
        }

        if (mission.rewardScience > 0)
        {
            _detailsRewardText.gameObject.SetActive(true);
            _detailsRewardText.text = $"SCI  НАГРАДА:  {mission.rewardScience}";
        }
        else
        {
            _detailsRewardText.gameObject.SetActive(false);
        }

        // Repurpose launch button to launch user mission
        _launchButton.onClick.RemoveAllListeners();
        _launchButton.onClick.AddListener(() => LaunchUserMission(mission));
        _launchButton.interactable = true;
        if (_launchButtonLabel != null)
            _launchButtonLabel.text = done ? ">  ПОВТОРИТЬ СВОЮ МИССИЮ" : ">  ЗАПУСТИТЬ СВОЮ МИССИЮ";
    }

    private void LaunchUserMission(UserMission mission)
    {
        var scene = string.IsNullOrEmpty(mission.sceneName) ? _defaultGameScene : mission.sceneName;
        if (!IsSceneInBuild(scene))
        {
            Debug.LogWarning($"User mission '{mission.title}' references missing scene '{scene}', falling back to '{_defaultGameScene}'.");
            scene = _defaultGameScene;
            if (!IsSceneInBuild(scene)) return; // nothing we can do
        }

        MissionContext.StartUserMission(mission);
        SceneManager.LoadScene(scene);
    }

    // ───────────── User mission editor (popup) ─────────────

    private TextMeshProUGUI _editorValidation;
    private Button _editorSaveButton;
    private TextMeshProUGUI _editorSaveLabel;

    private void BuildUserMissionEditor(RectTransform screen)
    {
        var root = new GameObject("UserMissionEditor",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(screen, false);
        Stretch(root.GetComponent<RectTransform>());
        var rootImg = root.GetComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0.88f);
        rootImg.raycastTarget = true;
        _userMissionEditor = root;

        var card = CreateRect("Card", root.GetComponent<RectTransform>());
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(1360, 820);
        var cardImg = card.gameObject.AddComponent<Image>();
        cardImg.color = _panelColor;
        AddBorder(card.gameObject, _borderColor);
        // Block click-through to root dim
        cardImg.raycastTarget = true;

        var cardVlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardVlg.padding = new RectOffset(0, 0, 0, 0);
        cardVlg.spacing = 0;
        cardVlg.childForceExpandWidth = true;
        cardVlg.childForceExpandHeight = false;
        cardVlg.childControlWidth = true;
        cardVlg.childControlHeight = true;

        BuildEditorHeader(card);
        BuildEditorBody(card);
        BuildEditorFooter(card);
        BuildConditionsModal(root.GetComponent<RectTransform>());

        root.SetActive(false);
    }

    private void BuildEditorHeader(RectTransform parent)
    {
        var headerWrap = CreateRect("EditorHeader", parent);
        var headerLE = headerWrap.gameObject.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 96;
        headerLE.minHeight = 96;
        headerLE.flexibleHeight = 0;
        var bg = headerWrap.gameObject.AddComponent<Image>();
        bg.color = _panelColor;

        var hVlg = headerWrap.gameObject.AddComponent<VerticalLayoutGroup>();
        hVlg.padding = new RectOffset(34, 28, 16, 12);
        hVlg.spacing = 5;
        hVlg.childAlignment = TextAnchor.MiddleLeft;
        hVlg.childForceExpandWidth = true;
        hVlg.childControlWidth = true;
        hVlg.childControlHeight = true;

        var topRow = CreateRect("TopRow", headerWrap);
        var topRowLE = topRow.gameObject.AddComponent<LayoutElement>();
        topRowLE.preferredHeight = 38;
        topRowLE.flexibleHeight = 0;
        var topHlg = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        topHlg.spacing = 12;
        topHlg.childAlignment = TextAnchor.MiddleLeft;
        topHlg.childForceExpandHeight = true;
        topHlg.childControlWidth = true;
        topHlg.childControlHeight = true;

        _editorTitleLabel = CreateText(topRow, "EditorTitle", "НОВАЯ МИССИЯ",
            28, _textPrimary, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        _editorTitleLabel.characterSpacing = 6f;
        var titleLE = _editorTitleLabel.GetComponent<LayoutElement>();
        titleLE.flexibleWidth = 1;
        titleLE.preferredHeight = 38;

        var closeButton = CreateMiniIconButton(topRow, "Close", "X", _textMuted, HideEditor);
        var closeLE = closeButton.GetComponent<LayoutElement>();
        closeLE.preferredWidth = 44;
        closeLE.preferredHeight = 34;

        var sub = CreateText(headerWrap, "EditorSub",
            "Опиши, что должен сделать пользователь. После сохранения миссия появится в списке слева.",
            16, _textMuted, FontStyles.Normal, TextAlignmentOptions.Left);
        sub.GetComponent<LayoutElement>().preferredHeight = 22;

        _editorSummary = CreateText(headerWrap, "EditorSummary", "",
            14, _accentSoft, FontStyles.Bold, TextAlignmentOptions.Left);
        _editorSummary.characterSpacing = 2f;
        _editorSummary.GetComponent<LayoutElement>().preferredHeight = 18;

        // Accent bottom border
        var border = CreateRect("Border", parent);
        var borderLE = border.gameObject.AddComponent<LayoutElement>();
        borderLE.preferredHeight = 2;
        borderLE.minHeight = 2;
        borderLE.flexibleHeight = 0;
        border.gameObject.AddComponent<Image>().color = MultiplyAlpha(_accent, 0.7f);
    }

    private void BuildEditorBody(RectTransform parent)
    {
        var bodyWrap = CreateRect("EditorBody", parent);
        var bodyLE = bodyWrap.gameObject.AddComponent<LayoutElement>();
        bodyLE.flexibleHeight = 1;
        bodyLE.minHeight = 420;
        var bodyImg = bodyWrap.gameObject.AddComponent<Image>();
        bodyImg.color = FromHex("#070B10", new Color(0.035f, 0.04f, 0.065f, 1f));
        bodyImg.raycastTarget = false;

        var bodyHlg = bodyWrap.gameObject.AddComponent<HorizontalLayoutGroup>();
        bodyHlg.padding = new RectOffset(34, 34, 22, 18);
        bodyHlg.spacing = 28;
        bodyHlg.childAlignment = TextAnchor.UpperLeft;
        bodyHlg.childForceExpandWidth = false;
        bodyHlg.childForceExpandHeight = true;
        bodyHlg.childControlWidth = true;
        bodyHlg.childControlHeight = true;

        var content = CreateRect("MainFields", bodyWrap);
        var contentLE = content.gameObject.AddComponent<LayoutElement>();
        contentLE.flexibleWidth = 1;
        contentLE.minWidth = 800;
        var contentVlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 8;
        contentVlg.childAlignment = TextAnchor.UpperLeft;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;

        BuildEditorSection(content, "ЗАДАЧА ПОЛЬЗОВАТЕЛЯ", _accentSoft);

        BuildEditorFieldLabel(content, "Название миссии", true);
        _editorTitle = BuildEditorInput(content, "TitleInput", false, 1,
            "Например: Стабилизация ориентации", primary: true);
        _editorTitle.onValueChanged.AddListener(_ => UpdateEditorValidation());
        _editorTitleError = CreateFieldError(content, "TitleError");

        BuildEditorFieldLabel(content, "Что нужно выполнить", true);
        _editorObjective = BuildEditorInput(content, "ObjectiveInput", true, 2,
            "Например: включить питание, повернуться к Земле и сделать снимок.");
        _editorObjective.onValueChanged.AddListener(_ => UpdateEditorValidation());
        _editorObjectiveError = CreateFieldError(content, "ObjectiveError");

        BuildEditorFieldLabel(content, "Брифинг для пользователя", false);
        _editorDescription = BuildEditorInput(content, "DescriptionInput", true, 3,
            "Контекст миссии, подсказки и ограничения. Можно писать в несколько строк.");
        _editorDescription.onValueChanged.AddListener(_ => UpdateEditorValidation());

        BuildEditorConditions(content);

        var side = CreateRect("SideFields", bodyWrap);
        var sideLE = side.gameObject.AddComponent<LayoutElement>();
        sideLE.preferredWidth = 370;
        sideLE.minWidth = 370;
        sideLE.flexibleWidth = 0;
        var sideVlg = side.gameObject.AddComponent<VerticalLayoutGroup>();
        sideVlg.spacing = 6;
        sideVlg.childAlignment = TextAnchor.UpperLeft;
        sideVlg.childForceExpandWidth = true;
        sideVlg.childForceExpandHeight = false;
        sideVlg.childControlWidth = true;
        sideVlg.childControlHeight = true;

        BuildEditorSection(side, "БЫСТРЫЙ СТАРТ", _accent);
        BuildEditorTemplateChips(side);

        BuildEditorSection(side, "НАСТРОЙКИ", _statusInfo);

        BuildEditorFieldLabel(side, "Награда", false);
        BuildEditorRewardField(side);

        BuildEditorFieldLabel(side, "Сцена", false);
        BuildEditorSceneField(side);

        BuildEditorFieldLabel(side, "Сложность", false);
        BuildEditorDifficultyField(side);
    }

    private void BuildEditorTemplateChips(RectTransform parent)
    {
        var row = CreateRect("TemplateChips", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 176;
        rowLE.flexibleHeight = 0;

        var vlg = row.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        CreateTemplateChip(row, "Стабилизация", "наведение на Землю", () => ApplyEditorTemplate(
            "Стабилизация ориентации",
            "Разверни спутник к Земле и удержи стабильную ориентацию до завершения маневра.",
            "Аппарат вышел на рабочий участок орбиты, но ориентация плавает. Собери короткую программу, которая наведет спутник на Землю и не сорвет стабилизацию.",
            25));

        CreateTemplateChip(row, "Солнце", "энергия и панели", () => ApplyEditorTemplate(
            "Солнечная фиксация",
            "Поверни спутник так, чтобы солнечная панель получила устойчивую засветку.",
            "Энергия уходит быстрее расчетного. Используй команды ориентации и проверь, что аппарат успевает поймать Солнце до критического участка.",
            35));

        CreateTemplateChip(row, "Орбита", "длинная проверка", () => ApplyEditorTemplate(
            "Полный виток",
            "Проведи спутник через полный орбитальный цикл без потери контроля.",
            "Это проверка всей цепочки управления: ориентация, ожидание, коррекция и контроль состояния. Миссия подходит как финальный тест программы.",
            50));

        CreateTemplateChip(row, "Своя", "чистая заготовка", () => ApplyEditorTemplate(
            "Своя миссия",
            "",
            "",
            25));
    }

    private void CreateTemplateChip(RectTransform parent, string label, string subtitle, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Template_{label}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 12, 5, 5);
        vlg.spacing = 0;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var title = CreateText(go.GetComponent<RectTransform>(), "Title", label,
            15, _textPrimary, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        title.characterSpacing = 3f;
        title.raycastTarget = false;
        title.GetComponent<LayoutElement>().preferredHeight = 20;

        var sub = CreateText(go.GetComponent<RectTransform>(), "Subtitle", subtitle,
            11, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
        sub.characterSpacing = 2f;
        sub.raycastTarget = false;
        sub.GetComponent<LayoutElement>().preferredHeight = 14;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = title;
        var c = btn.colors;
        c.normalColor = _textPrimary;
        c.highlightedColor = _accent;
        c.pressedColor = MultiplyAlpha(_accent, 0.75f);
        c.fadeDuration = 0.08f;
        btn.colors = c;
        btn.onClick.AddListener(onClick);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 38;
        le.minHeight = 38;
        le.flexibleWidth = 1;
    }

    private void ApplyEditorTemplate(string title, string objective, string description, int rewardScience)
    {
        if (_editorTitle != null) _editorTitle.text = title;
        if (_editorObjective != null) _editorObjective.text = objective;
        if (_editorDescription != null) _editorDescription.text = description;
        SetEditorReward(rewardScience);
        SetEditorConditions(GetConditionsFromText(title + " " + objective + " " + description));
        if (title.Contains("Орб", System.StringComparison.OrdinalIgnoreCase)) SetEditorDifficulty("Сложная");
        else if (title.Contains("Солн", System.StringComparison.OrdinalIgnoreCase)) SetEditorDifficulty("Средняя");
        else SetEditorDifficulty("Базовая");
        UpdateEditorValidation();
    }

    private void BuildEditorFooter(RectTransform parent)
    {
        var footerWrap = CreateRect("EditorFooter", parent);
        var footerLE = footerWrap.gameObject.AddComponent<LayoutElement>();
        footerLE.preferredHeight = 94;
        footerLE.minHeight = 94;
        footerLE.flexibleHeight = 0;
        var bg = footerWrap.gameObject.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.13f, 1f);

        var fVlg = footerWrap.gameObject.AddComponent<VerticalLayoutGroup>();
        fVlg.padding = new RectOffset(40, 40, 14, 18);
        fVlg.spacing = 6;
        fVlg.childForceExpandWidth = true;
        fVlg.childControlWidth = true;
        fVlg.childControlHeight = true;

        // Validation hint line
        _editorValidation = CreateText(footerWrap, "Validation", "",
            14, new Color(0.95f, 0.45f, 0.45f, 1f), FontStyles.Italic, TextAlignmentOptions.Left);
        _editorValidation.GetComponent<LayoutElement>().preferredHeight = 18;

        // Buttons row
        var btnRow = CreateRect("Buttons", footerWrap);
        var btnRowLE = btnRow.gameObject.AddComponent<LayoutElement>();
        btnRowLE.preferredHeight = 50;
        var bhlg = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        bhlg.spacing = 14;
        bhlg.childAlignment = TextAnchor.MiddleLeft;
        bhlg.childForceExpandWidth = false;
        bhlg.childForceExpandHeight = true;
        bhlg.childControlWidth = true;
        bhlg.childControlHeight = true;

        var cancelBtn = CreateEditorButton(btnRow, "ОТМЕНА", _textSecondary, () => HideEditor());
        var cancelLE = cancelBtn.gameObject.AddComponent<LayoutElement>();
        cancelLE.preferredWidth = 160;
        cancelLE.flexibleWidth = 0;

        var clearBtn = CreateEditorButton(btnRow, "ОЧИСТИТЬ", _textMuted, OnClearEditor);
        var clearLE = clearBtn.gameObject.AddComponent<LayoutElement>();
        clearLE.preferredWidth = 170;
        clearLE.flexibleWidth = 0;

        var deleteButton = CreateEditorButton(btnRow, "УДАЛИТЬ",
            new Color(0.95f, 0.45f, 0.45f), OnDeleteEditor);
        _editorDeleteButton = deleteButton.gameObject;
        _editorDeleteLabel = deleteButton.GetComponentInChildren<TextMeshProUGUI>();
        var delLE = _editorDeleteButton.AddComponent<LayoutElement>();
        delLE.preferredWidth = 180;
        delLE.flexibleWidth = 0;

        // Spacer pushes Save to right
        var spacer = CreateRect("Spacer", btnRow);
        var spacerLE = spacer.gameObject.AddComponent<LayoutElement>();
        spacerLE.flexibleWidth = 1;

        _editorSaveButton = CreateEditorButton(btnRow, "СОЗДАТЬ МИССИЮ", _accent, OnSaveEditor);
        _editorSaveLabel = _editorSaveButton.GetComponentInChildren<TextMeshProUGUI>();
        var saveLE = _editorSaveButton.gameObject.AddComponent<LayoutElement>();
        saveLE.preferredWidth = 260;
        saveLE.flexibleWidth = 0;
    }

    private void BuildEditorSection(RectTransform parent, string title, Color color)
    {
        var row = CreateRect($"Section_{title}", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 28;
        rowLE.flexibleHeight = 0;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var label = CreateText(row, "Label", title,
            17, color, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        label.characterSpacing = 4f;
        var labelLE = label.GetComponent<LayoutElement>();
        labelLE.flexibleWidth = 1;
        labelLE.preferredHeight = 24;
    }

    private void BuildEditorFieldLabel(RectTransform parent, string text, bool required)
    {
        var row = CreateRect($"FieldLabel_{text}", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 22;
        rowLE.flexibleHeight = 0;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var label = CreateText(row, "Label", text,
            16, _textSecondary, FontStyles.Normal, TextAlignmentOptions.Left);
        var labelLE = label.GetComponent<LayoutElement>();
        labelLE.flexibleWidth = 0;

        if (required)
        {
            var star = CreateText(row, "Star", "*",
                16, _accent, FontStyles.Bold, TextAlignmentOptions.Left);
            var starLE = star.GetComponent<LayoutElement>();
            starLE.preferredWidth = 12;
            starLE.flexibleWidth = 0;
        }
    }

    private TextMeshProUGUI CreateFieldError(RectTransform parent, string name)
    {
        var error = CreateText(parent, name, "",
            13, FromHex("#FF5A5A", Color.red), FontStyles.Italic, TextAlignmentOptions.Left);
        var le = error.GetComponent<LayoutElement>();
        le.preferredHeight = 16;
        le.flexibleHeight = 0;
        error.gameObject.SetActive(false);
        return error;
    }

    private void BuildEditorConditions(RectTransform parent)
    {
        BuildEditorSection(parent, "УСЛОВИЯ ВЫПОЛНЕНИЯ", _statusOnline);

        var box = CreatePanel(parent, "ConditionsSummaryBox", _cardColor, new RectOffset(14, 14, 12, 12));
        var boxLE = box.gameObject.AddComponent<LayoutElement>();
        boxLE.preferredHeight = 76;
        boxLE.flexibleHeight = 0;

        var hlg = box.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 14, 10, 10);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var summaryCol = CreateRect("Summary", box);
        var summaryLE = summaryCol.gameObject.AddComponent<LayoutElement>();
        summaryLE.flexibleWidth = 1;
        var summaryVlg = summaryCol.gameObject.AddComponent<VerticalLayoutGroup>();
        summaryVlg.spacing = 3;
        summaryVlg.childAlignment = TextAnchor.MiddleLeft;
        summaryVlg.childForceExpandWidth = true;
        summaryVlg.childForceExpandHeight = false;
        summaryVlg.childControlWidth = true;
        summaryVlg.childControlHeight = true;

        var title = CreateText(summaryCol, "Title", "Ключевые состояния миссии",
            15, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        title.GetComponent<LayoutElement>().preferredHeight = 20;

        _editorConditionsSummaryText = CreateText(summaryCol, "SummaryText", "",
            14, _textMuted, FontStyles.Normal, TextAlignmentOptions.Left);
        _editorConditionsSummaryText.GetComponent<LayoutElement>().preferredHeight = 32;

        var configure = CreateEditorButton(box, "НАСТРОИТЬ", _statusInfo, OpenConditionsModal);
        var configureLE = configure.gameObject.AddComponent<LayoutElement>();
        configureLE.preferredWidth = 170;
        configureLE.flexibleWidth = 0;
        configureLE.preferredHeight = 44;
    }

    private void BuildConditionsModal(RectTransform parent)
    {
        var overlay = new GameObject("ConditionsModal",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(parent, false);
        Stretch(overlay.GetComponent<RectTransform>());
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.72f);
        overlayImg.raycastTarget = true;
        _conditionsModal = overlay;

        var card = CreatePanel(overlay.GetComponent<RectTransform>(), "ConditionsCard", _panelColor, new RectOffset(0, 0, 0, 0));
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(1180, 660);
        card.GetComponent<Image>().raycastTarget = true;

        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(34, 34, 28, 28);
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var header = CreateRect("Header", card);
        var headerLE = header.gameObject.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 52;
        var headerHlg = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerHlg.spacing = 12;
        headerHlg.childAlignment = TextAnchor.MiddleLeft;
        headerHlg.childForceExpandWidth = false;
        headerHlg.childForceExpandHeight = true;
        headerHlg.childControlWidth = true;
        headerHlg.childControlHeight = true;

        var title = CreateText(header, "Title", "УСЛОВИЯ ВЫПОЛНЕНИЯ",
            30, _textPrimary, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        title.characterSpacing = 4f;
        title.GetComponent<LayoutElement>().flexibleWidth = 1;

        var close = CreateEditorButton(header, "X", _textMuted, CloseConditionsModal);
        var closeLE = close.gameObject.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 44;
        closeLE.flexibleWidth = 0;

        var help = CreateText(card, "Help",
            "Выбери состояния, которые игра должна проверить для завершения миссии. Числовые условия настраиваются кнопками - и +.",
            15, _textSecondary, FontStyles.Normal, TextAlignmentOptions.Left);
        help.GetComponent<LayoutElement>().preferredHeight = 44;

        var workArea = CreateRect("ConditionsWorkArea", card);
        var workAreaLE = workArea.gameObject.AddComponent<LayoutElement>();
        workAreaLE.preferredHeight = 394;
        workAreaLE.flexibleHeight = 0;
        var workHlg = workArea.gameObject.AddComponent<HorizontalLayoutGroup>();
        workHlg.spacing = 24;
        workHlg.childAlignment = TextAnchor.UpperLeft;
        workHlg.childForceExpandWidth = false;
        workHlg.childForceExpandHeight = true;
        workHlg.childControlWidth = true;
        workHlg.childControlHeight = true;

        var selectedColumn = CreateRect("SelectedConditions", workArea);
        var selectedLE = selectedColumn.gameObject.AddComponent<LayoutElement>();
        selectedLE.flexibleWidth = 1;
        selectedLE.minWidth = 640;
        var selectedVlg = selectedColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        selectedVlg.spacing = 10;
        selectedVlg.childAlignment = TextAnchor.UpperLeft;
        selectedVlg.childForceExpandWidth = true;
        selectedVlg.childForceExpandHeight = false;
        selectedVlg.childControlWidth = true;
        selectedVlg.childControlHeight = true;

        var selectedLabel = CreateText(selectedColumn, "SelectedLabel", "ВЫБРАННЫЕ УСЛОВИЯ",
            15, _textMuted, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        selectedLabel.characterSpacing = 2f;
        selectedLabel.GetComponent<LayoutElement>().preferredHeight = 22;

        _editorConditionsContainer = CreatePanel(selectedColumn, "ConditionRows", _cardColor, new RectOffset(0, 0, 0, 0));
        var rowsLE = _editorConditionsContainer.gameObject.AddComponent<LayoutElement>();
        rowsLE.preferredHeight = 346;
        rowsLE.flexibleHeight = 0;
        var rowsVlg = _editorConditionsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsVlg.padding = new RectOffset(14, 14, 14, 14);
        rowsVlg.spacing = 8;
        rowsVlg.childAlignment = TextAnchor.UpperLeft;
        rowsVlg.childForceExpandWidth = true;
        rowsVlg.childForceExpandHeight = false;
        rowsVlg.childControlWidth = true;
        rowsVlg.childControlHeight = true;

        var paletteColumn = CreatePanel(workArea, "ConditionPalettePanel", _cardColor, new RectOffset(0, 0, 0, 0));
        var paletteColumnLE = paletteColumn.gameObject.AddComponent<LayoutElement>();
        paletteColumnLE.preferredWidth = 380;
        paletteColumnLE.flexibleWidth = 0;
        var paletteVlg = paletteColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        paletteVlg.padding = new RectOffset(16, 16, 16, 16);
        paletteVlg.spacing = 12;
        paletteVlg.childAlignment = TextAnchor.UpperLeft;
        paletteVlg.childForceExpandWidth = true;
        paletteVlg.childForceExpandHeight = false;
        paletteVlg.childControlWidth = true;
        paletteVlg.childControlHeight = true;

        var addLabel = CreateText(paletteColumn, "AddLabel", "ДОБАВИТЬ",
            15, _textMuted, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        addLabel.characterSpacing = 2f;
        addLabel.GetComponent<LayoutElement>().preferredHeight = 22;

        CreateConditionPresetButton(paletteColumn, "Питание включено", MissionConditionType.PowerEnabled);
        CreateConditionPresetButton(paletteColumn, "Повернуться к Земле", MissionConditionType.FacingEarth);
        CreateConditionPresetButton(paletteColumn, "Повернуться к Солнцу", MissionConditionType.FacingSun);
        CreateConditionPresetButton(paletteColumn, "Стабилизировать спутник", MissionConditionType.Stabilized);
        CreateConditionPresetButton(paletteColumn, "Сделать фото", MissionConditionType.PhotoTaken);
        CreateConditionPresetButton(paletteColumn, "Отправить данные", MissionConditionType.DataSent);
        CreateConditionPresetButton(paletteColumn, "Заряд батареи больше %", MissionConditionType.BatteryAbovePercent);
        CreateConditionPresetButton(paletteColumn, "Ждать", MissionConditionType.WaitSeconds);

        /*var firstRow = CreateConditionPaletteRow(palette);
        CreateConditionPresetButton(firstRow, "Питание", MissionConditionType.PowerEnabled);
        CreateConditionPresetButton(firstRow, "К Земле", MissionConditionType.FacingEarth);
        CreateConditionPresetButton(firstRow, "К Солнцу", MissionConditionType.FacingSun);
        CreateConditionPresetButton(firstRow, "Стабилизация", MissionConditionType.Stabilized);

        var secondRow = CreateConditionPaletteRow(palette);
        CreateConditionPresetButton(secondRow, "Фото", MissionConditionType.PhotoTaken);
        CreateConditionPresetButton(secondRow, "Данные", MissionConditionType.DataSent);
        CreateConditionPresetButton(secondRow, "Батарея > %", MissionConditionType.BatteryAbovePercent);
        CreateConditionPresetButton(secondRow, "Ждать", MissionConditionType.WaitSeconds);*/

        var footer = CreateRect("Footer", card);
        var footerLE = footer.gameObject.AddComponent<LayoutElement>();
        footerLE.preferredHeight = 44;
        var footerHlg = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerHlg.childAlignment = TextAnchor.MiddleRight;
        footerHlg.childForceExpandWidth = false;
        footerHlg.childForceExpandHeight = true;
        footerHlg.childControlWidth = true;
        footerHlg.childControlHeight = true;

        var done = CreateEditorButton(footer, "ГОТОВО", _accent, CloseConditionsModal);
        var doneLE = done.gameObject.AddComponent<LayoutElement>();
        doneLE.preferredWidth = 180;
        doneLE.flexibleWidth = 0;

        overlay.SetActive(false);
    }

    private void RefreshEditorConditionRows()
    {
        UpdateEditorConditionsSummary();
        if (_editorConditionsContainer == null) return;
        for (int i = _editorConditionsContainer.childCount - 1; i >= 0; i--)
            Destroy(_editorConditionsContainer.GetChild(i).gameObject);

        if (_editingConditions.Count == 0)
        {
            var empty = CreateText(_editorConditionsContainer, "Empty", "Добавь хотя бы одно ключевое состояние миссии.",
                14, _textMuted, FontStyles.Italic, TextAlignmentOptions.Left);
            empty.GetComponent<LayoutElement>().preferredHeight = 24;
            return;
        }

        for (int i = 0; i < _editingConditions.Count; i++)
            CreateEditorConditionRow(_editorConditionsContainer, i);
    }

    private void UpdateEditorConditionsSummary()
    {
        if (_editorConditionsSummaryText == null) return;

        if (_editingConditions.Count == 0)
        {
            _editorConditionsSummaryText.text = "Условия не выбраны. Нажми «Настроить», чтобы добавить ключевые состояния.";
            _editorConditionsSummaryText.color = FromHex("#FF5A5A", Color.red);
            return;
        }

        _editorConditionsSummaryText.color = _textSecondary;
        int shown = Mathf.Min(_editingConditions.Count, 3);
        var summary = $"{_editingConditions.Count} усл.: ";
        for (int i = 0; i < shown; i++)
        {
            if (i > 0) summary += ", ";
            summary += GetConditionShortName(_editingConditions[i].conditionType);
        }
        if (_editingConditions.Count > shown)
            summary += $", +{_editingConditions.Count - shown}";
        _editorConditionsSummaryText.text = summary;
    }

    private void CreateEditorConditionRow(RectTransform parent, int index)
    {
        var condition = _editingConditions[index];
        var row = CreatePanel(parent, $"Condition_{index}", MultiplyAlpha(_panelColor, 0.72f), new RectOffset(0, 0, 0, 0));
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 46;
        rowLE.flexibleHeight = 0;

        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 10, 0, 0);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var number = CreateText(row, "Number", (index + 1).ToString("00"),
            16, _accent, FontStyles.Bold, TextAlignmentOptions.Left);
        var numberLE = number.GetComponent<LayoutElement>();
        numberLE.preferredWidth = 42;
        numberLE.flexibleWidth = 0;
        number.raycastTarget = false;

        var title = CreateText(row, "Title", GetConditionBaseName(condition.conditionType),
            17, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        var titleLE = title.GetComponent<LayoutElement>();
        titleLE.flexibleWidth = 1;
        title.raycastTarget = false;

        if (ConditionUsesValue(condition.conditionType))
        {
            var minus = CreateEditorButton(row, "-", _textMuted, () =>
            {
                AdjustConditionValue(condition, -5);
                RefreshEditorConditionRows();
                UpdateEditorValidation();
            });
            var minusLE = minus.gameObject.AddComponent<LayoutElement>();
            minusLE.preferredWidth = 42;

            var value = CreateText(row, "Value", GetConditionValueLabel(condition),
                17, _accent, FontStyles.Bold, TextAlignmentOptions.Center);
            var valueLE = value.GetComponent<LayoutElement>();
            valueLE.preferredWidth = 78;
            valueLE.flexibleWidth = 0;
            value.raycastTarget = false;

            var plus = CreateEditorButton(row, "+", _textMuted, () =>
            {
                AdjustConditionValue(condition, 5);
                RefreshEditorConditionRows();
                UpdateEditorValidation();
            });
            var plusLE = plus.gameObject.AddComponent<LayoutElement>();
            plusLE.preferredWidth = 42;
        }

        var remove = CreateEditorButton(row, "X", FromHex("#FF5A5A", Color.red), () =>
        {
            _editingConditions.RemoveAt(index);
            RefreshEditorConditionRows();
            UpdateEditorValidation();
        });
        var removeLE = remove.gameObject.AddComponent<LayoutElement>();
        removeLE.preferredWidth = 42;
    }

    private void AddEditorCondition()
    {
        AddEditorCondition(GetSuggestedConditionType(_editingConditions.Count));
    }

    private void OpenConditionsModal()
    {
        RefreshEditorConditionRows();
        if (_conditionsModal != null)
            _conditionsModal.SetActive(true);
    }

    private void CloseConditionsModal()
    {
        if (_conditionsModal != null)
            _conditionsModal.SetActive(false);
        UpdateEditorValidation();
    }

    private void AddEditorCondition(MissionConditionType type)
    {
        if (HasEditorCondition(type))
            return;

        var condition = new MissionConditionData(type, GetDefaultConditionValue(type));
        NormalizeConditionValue(condition);
        _editingConditions.Add(condition);
        RefreshEditorConditionRows();
        UpdateEditorValidation();
    }

    private bool HasEditorCondition(MissionConditionType type)
    {
        for (int i = 0; i < _editingConditions.Count; i++)
        {
            if (_editingConditions[i].conditionType == type)
                return true;
        }
        return false;
    }

    private int GetDefaultConditionValue(MissionConditionType type)
    {
        return type switch
        {
            MissionConditionType.BatteryAbovePercent => 50,
            MissionConditionType.WaitSeconds => 5,
            _ => 0
        };
    }

    private RectTransform CreateConditionPaletteRow(RectTransform parent)
    {
        var row = CreateRect("PaletteRow", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 30;
        rowLE.flexibleHeight = 0;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        return row;
    }

    private void CreateConditionPresetButton(RectTransform parent, string label, MissionConditionType type)
    {
        var go = new GameObject($"Preset_{type}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = MultiplyAlpha(_statusInfo, 0.08f);
        AddBorder(go, MultiplyAlpha(_statusInfo, 0.28f));

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        if (_font != null) tmp.font = _font;
        tmp.fontSize = 16;
        tmp.color = _textSecondary;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = tmp;
        var c = btn.colors;
        c.normalColor = _textSecondary;
        c.highlightedColor = _accent;
        c.pressedColor = MultiplyAlpha(_accent, 0.75f);
        c.fadeDuration = 0.08f;
        btn.colors = c;
        btn.onClick.AddListener(() => AddEditorCondition(type));

        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredHeight = 38;
        le.minHeight = 38;
    }

    private void BuildEditorDifficultyField(RectTransform parent)
    {
        var go = new GameObject("DifficultyButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = _cardColor;
        AddBorder(go, _borderColor);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        _editorDifficultyLabel = labelGO.GetComponent<TextMeshProUGUI>();
        if (_font != null) _editorDifficultyLabel.font = _font;
        _editorDifficultyLabel.fontSize = 17;
        _editorDifficultyLabel.color = _textPrimary;
        _editorDifficultyLabel.fontStyle = FontStyles.Bold;
        _editorDifficultyLabel.alignment = TextAlignmentOptions.Center;
        _editorDifficultyLabel.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = _editorDifficultyLabel;
        var colors = btn.colors;
        colors.normalColor = _textPrimary;
        colors.highlightedColor = _accent;
        colors.pressedColor = MultiplyAlpha(_accent, 0.75f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(() =>
        {
            _editorDifficultyIndex = (_editorDifficultyIndex + 1) % _difficultyOptions.Length;
            UpdateDifficultyLabel();
            UpdateEditorValidation();
        });

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 42;
        le.flexibleHeight = 0;
        UpdateDifficultyLabel();
    }

    private void BuildEditorSpacer(RectTransform parent, float height)
    {
        var s = CreateRect("Spacer", parent);
        var le = s.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleHeight = 0;
    }

    private TMP_InputField BuildEditorInput(RectTransform parent, string name, bool multiLine, int linesPreferred,
        string placeholder, bool primary = false)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.05f);

        // Text area
        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        var taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(16, 10);
        taRT.offsetMax = new Vector2(-16, -10);

        // Placeholder
        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(textArea.transform, false);
        Stretch(phGO.GetComponent<RectTransform>());
        var phTmp = phGO.GetComponent<TextMeshProUGUI>();
        phTmp.text = placeholder ?? "";
        if (_font != null) phTmp.font = _font;
        phTmp.fontSize = primary ? 21 : 17;
        phTmp.color = MultiplyAlpha(_textMuted, 0.85f);
        phTmp.alignment = multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.enableWordWrapping = true;

        // Text
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(textArea.transform, false);
        Stretch(textGO.GetComponent<RectTransform>());
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = "";
        if (_font != null) tmp.font = _font;
        tmp.fontSize = primary ? 21 : 17;
        tmp.color = _textPrimary;
        tmp.fontStyle = primary ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;

        var input = go.GetComponent<TMP_InputField>();
        input.textViewport = taRT;
        input.textComponent = tmp;
        input.placeholder = phTmp;
        input.lineType = multiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.fontAsset = _font;
        input.pointSize = primary ? 21 : 17;
        input.caretWidth = 2;
        input.customCaretColor = true;
        input.caretColor = _accent;
        input.selectionColor = MultiplyAlpha(_accent, 0.35f);

        var le = go.AddComponent<LayoutElement>();
        int lineH = primary ? 28 : 24;
        le.preferredHeight = multiLine ? (lineH * linesPreferred + 22) : (primary ? 50 : 42);
        le.minHeight = le.preferredHeight;
        le.flexibleHeight = 0;

        return input;
    }

    private void BuildEditorRewardField(RectTransform parent)
    {
        // Slider row
        var sliderRow = CreateRect("RewardSliderRow", parent);
        var rowLE = sliderRow.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 44;
        rowLE.flexibleHeight = 0;
        var hlg = sliderRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var minusButton = CreateEditorButton(sliderRow, "-5", _textMuted, () => AdjustEditorReward(-5));
        var minusLE = minusButton.gameObject.AddComponent<LayoutElement>();
        minusLE.preferredWidth = 44;
        minusLE.preferredHeight = 36;
        minusLE.flexibleWidth = 0;

        _editorRewardSlider = CreateSliderControl(sliderRow, 0.25f);
        _editorRewardSlider.minValue = 0;
        _editorRewardSlider.maxValue = 200;
        _editorRewardSlider.wholeNumbers = true;
        var sliderLE = _editorRewardSlider.gameObject.AddComponent<LayoutElement>();
        sliderLE.preferredHeight = 36;
        sliderLE.flexibleWidth = 1;

        _editorRewardLabel = CreateText(sliderRow, "RewardValue", "25 SCI",
            20, _accent, FontStyles.Bold, TextAlignmentOptions.Right);
        var labelLE = _editorRewardLabel.GetComponent<LayoutElement>();
        labelLE.preferredWidth = 86;
        labelLE.flexibleWidth = 0;

        var plusButton = CreateEditorButton(sliderRow, "+5", _accent, () => AdjustEditorReward(5));
        var plusLE = plusButton.gameObject.AddComponent<LayoutElement>();
        plusLE.preferredWidth = 44;
        plusLE.preferredHeight = 36;
        plusLE.flexibleWidth = 0;

        _editorRewardSlider.onValueChanged.AddListener(v =>
        {
            UpdateEditorRewardLabel();
            UpdateEditorValidation();
        });
        SetEditorReward(25);

        // Quick chips
        var chipsRow = CreateRect("RewardChips", parent);
        var chipsLE = chipsRow.gameObject.AddComponent<LayoutElement>();
        chipsLE.preferredHeight = 28;
        chipsLE.flexibleHeight = 0;
        var chlg = chipsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        chlg.spacing = 14;
        chlg.childAlignment = TextAnchor.MiddleLeft;
        chlg.childForceExpandWidth = false;
        chlg.childControlWidth = true;
        chlg.childControlHeight = true;

        int[] presets = { 10, 25, 50, 100, 200 };
        foreach (var v in presets) CreateRewardChip(chipsRow, v);
    }

    private void CreateRewardChip(RectTransform parent, int value)
    {
        var go = new GameObject($"Chip_{value}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = value.ToString();
        if (_font != null) tmp.font = _font;
        tmp.fontSize = 14;
        tmp.color = _textMuted;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = tmp;
        var c = btn.colors;
        c.normalColor = _textMuted;
        c.highlightedColor = _accent;
        c.pressedColor = MultiplyAlpha(_accent, 0.75f);
        c.fadeDuration = 0.08f;
        btn.colors = c;
        btn.onClick.AddListener(() =>
        {
            SetEditorReward(value);
        });

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 42;
        le.preferredHeight = 24;
        le.minHeight = 24;
        le.flexibleWidth = 0;
    }

    private void AdjustEditorReward(int delta)
    {
        var current = _editorRewardSlider != null ? Mathf.RoundToInt(_editorRewardSlider.value) : 0;
        SetEditorReward(current + delta);
    }

    private void SetEditorReward(int value)
    {
        var clamped = Mathf.Clamp(value, 0, 200);
        if (_editorRewardSlider != null)
            _editorRewardSlider.SetValueWithoutNotify(clamped);
        UpdateEditorRewardLabel();
        UpdateEditorValidation();
    }

    private void UpdateEditorRewardLabel()
    {
        if (_editorRewardLabel == null) return;
        var value = _editorRewardSlider != null ? Mathf.RoundToInt(_editorRewardSlider.value) : 0;
        _editorRewardLabel.text = $"{value} SCI";
    }

    private void BuildEditorSceneField(RectTransform parent)
    {
        _availableScenes = GetAvailableScenes();
        _editorSceneIndex = 0;

        if (_availableScenes.Count <= 1)
        {
            var single = CreateRect("SceneSingle", parent);
            var singleLE = single.gameObject.AddComponent<LayoutElement>();
            singleLE.preferredHeight = 48;
            singleLE.minHeight = 48;
            singleLE.flexibleHeight = 0;
            var singleImg = single.gameObject.AddComponent<Image>();
            singleImg.color = new Color(0f, 0f, 0f, 0f);
            singleImg.raycastTarget = false;

            var singleLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            singleLabelGO.transform.SetParent(single, false);
            var labelRT = singleLabelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(14, 0);
            labelRT.offsetMax = new Vector2(-14, 0);
            _editorSceneLabel = singleLabelGO.GetComponent<TextMeshProUGUI>();
            _editorSceneLabel.text = _availableScenes[0];
            if (_font != null) _editorSceneLabel.font = _font;
            _editorSceneLabel.fontSize = 18;
            _editorSceneLabel.color = _textPrimary;
            _editorSceneLabel.fontStyle = FontStyles.Bold;
            _editorSceneLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _editorSceneLabel.enableWordWrapping = false;
            _editorSceneLabel.overflowMode = TextOverflowModes.Ellipsis;
            _editorSceneLabel.raycastTarget = false;

            var singleSceneHint = CreateText(parent, "SceneHint",
                "единственная сцена в Build Settings",
                12, _textMuted, FontStyles.Italic, TextAlignmentOptions.Left);
            singleSceneHint.GetComponent<LayoutElement>().preferredHeight = 16;
            return;
        }

        var row = CreateRect("SceneRow", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 50;
        rowLE.flexibleHeight = 0;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // Prev arrow
        var prev = CreateSceneArrowButton(row, "<");
        prev.onClick.AddListener(() => CycleScene(-1));

        // Scene name centered
        var center = CreateRect("SceneCenter", row);
        var centerLE = center.gameObject.AddComponent<LayoutElement>();
        centerLE.flexibleWidth = 1;
        var centerImg = center.gameObject.AddComponent<Image>();
        centerImg.color = new Color(0f, 0f, 0f, 0f);
        centerImg.raycastTarget = false;

        var sceneLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        sceneLabelGO.transform.SetParent(center, false);
        Stretch(sceneLabelGO.GetComponent<RectTransform>());
        _editorSceneLabel = sceneLabelGO.GetComponent<TextMeshProUGUI>();
        _editorSceneLabel.text = _availableScenes[0];
        if (_font != null) _editorSceneLabel.font = _font;
        _editorSceneLabel.fontSize = 19;
        _editorSceneLabel.color = _textPrimary;
        _editorSceneLabel.fontStyle = FontStyles.Bold;
        _editorSceneLabel.alignment = TextAlignmentOptions.Center;
        _editorSceneLabel.enableWordWrapping = false;
        _editorSceneLabel.overflowMode = TextOverflowModes.Ellipsis;
        _editorSceneLabel.raycastTarget = false;

        // Next arrow
        var next = CreateSceneArrowButton(row, ">");
        next.onClick.AddListener(() => CycleScene(1));

        // Counter hint below row
        var sceneCountHint = CreateText(parent, "SceneHint",
            $"всего сцен в Build Settings: {_availableScenes.Count}",
            13, _textMuted, FontStyles.Italic, TextAlignmentOptions.Left);
        var hintLE = sceneCountHint.GetComponent<LayoutElement>();
        hintLE.preferredHeight = 18;
        hintLE.flexibleHeight = 0;
    }

    private Button CreateSceneArrowButton(RectTransform parent, string glyph)
    {
        var go = new GameObject($"Arrow_{glyph}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = glyph;
        if (_font != null) tmp.font = _font;
        tmp.fontSize = 24;
        tmp.color = _textPrimary;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = tmp;
        var c = btn.colors;
        c.normalColor = _textPrimary;
        c.highlightedColor = _accent;
        c.pressedColor = MultiplyAlpha(_accent, 0.75f);
        c.fadeDuration = 0.08f;
        btn.colors = c;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 44;
        le.preferredHeight = 44;
        le.flexibleWidth = 0;

        return btn;
    }

    private void CycleScene(int direction)
    {
        if (_availableScenes == null || _availableScenes.Count == 0) return;
        _editorSceneIndex = (_editorSceneIndex + direction + _availableScenes.Count) % _availableScenes.Count;
        if (_editorSceneLabel != null)
            _editorSceneLabel.text = _availableScenes[_editorSceneIndex];
        UpdateEditorValidation();
    }

    private void UpdateEditorValidation()
    {
        if (_editorTitle == null) return;
        if (_deleteArmed) ResetDeleteConfirmation();

        var hasTitle = !string.IsNullOrWhiteSpace(_editorTitle.text);
        var hasObjective = _editorObjective != null && !string.IsNullOrWhiteSpace(_editorObjective.text);
        var hasConditions = _editingConditions.Count > 0;

        if (_editorTitleError != null)
        {
            _editorTitleError.text = hasTitle ? "" : "Введите название миссии.";
            _editorTitleError.gameObject.SetActive(!hasTitle);
        }

        if (_editorObjectiveError != null)
        {
            _editorObjectiveError.text = hasObjective ? "" : "Опишите, какое состояние должен получить пользователь.";
            _editorObjectiveError.gameObject.SetActive(!hasObjective);
        }

        if (_editorValidation != null)
        {
            if (!hasTitle)
            {
                _editorValidation.text = "Поле «Название миссии» обязательно для заполнения.";
                _editorValidation.color = new Color(0.95f, 0.45f, 0.45f, 1f);
            }
            else if (!hasObjective)
            {
                _editorValidation.text = "Поле «Что нужно выполнить» обязательно для заполнения.";
                _editorValidation.color = FromHex("#FF5A5A", Color.red);
            }
            else if (!hasConditions)
            {
                _editorValidation.text = "Добавь хотя бы одно условие выполнения миссии.";
                _editorValidation.color = FromHex("#FF5A5A", Color.red);
            }
            else
            {
                _editorValidation.text = "";
            }
        }

        var canSave = hasTitle && hasObjective && hasConditions;
        if (_editorSaveButton != null) _editorSaveButton.interactable = canSave;
        if (_editorSaveLabel != null)
            _editorSaveLabel.color = canSave ? _accent : MultiplyAlpha(_accent, 0.4f);

        if (_editorSummary != null)
        {
            var mode = _editingUserMission == null ? "Новая миссия" : "Редактирование";
            var scene = GetSelectedEditorScene();
            var reward = _editorRewardSlider != null ? Mathf.RoundToInt(_editorRewardSlider.value) : 0;
            _editorSummary.text = $"{mode} - сцена: {scene} - награда: {reward} SCI";
        }
    }

    private Button CreateEditorButton(RectTransform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Btn_{label}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        if (_font != null) tmp.font = _font;
        tmp.fontSize = 18;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 6f;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = tmp;
        var c = btn.colors;
        c.normalColor = color;
        c.highlightedColor = Color.white;
        c.pressedColor = MultiplyAlpha(color, 0.7f);
        c.fadeDuration = 0.1f;
        btn.colors = c;
        btn.onClick.AddListener(onClick);

        return btn;
    }

    private void ShowEditorForNew()
    {
        _editingUserMission = null;
        ResetDeleteConfirmation();
        _editorTitleLabel.text = "НОВАЯ МИССИЯ";
        if (_editorSaveLabel != null) _editorSaveLabel.text = "СОЗДАТЬ МИССИЮ";
        _editorTitle.text = "";
        _editorObjective.text = "";
        _editorDescription.text = "";
        SetEditorReward(25);
        SetEditorConditions(GetDefaultUserMissionConditions());
        SetEditorDifficulty("Базовая");
        SetEditorScene(_defaultGameScene);
        if (_editorDeleteButton != null) _editorDeleteButton.SetActive(false);
        if (_conditionsModal != null) _conditionsModal.SetActive(false);
        if (_userMissionEditor != null) _userMissionEditor.SetActive(true);
        UpdateEditorValidation();
        FocusEditorTitle();
    }

    private void ShowEditorForEdit(UserMission mission)
    {
        _editingUserMission = mission;
        ResetDeleteConfirmation();
        _editorTitleLabel.text = "РЕДАКТИРОВАНИЕ";
        if (_editorSaveLabel != null) _editorSaveLabel.text = "СОХРАНИТЬ";
        _editorTitle.text = mission.title ?? "";
        _editorObjective.text = mission.objective ?? "";
        _editorDescription.text = mission.description ?? "";
        SetEditorReward(mission.rewardScience);
        SetEditorConditions(GetUserMissionConditions(mission));
        SetEditorDifficulty(GetMissionDifficulty(mission));
        SetEditorScene(string.IsNullOrEmpty(mission.sceneName) ? _defaultGameScene : mission.sceneName);
        if (_editorDeleteButton != null) _editorDeleteButton.SetActive(true);
        if (_conditionsModal != null) _conditionsModal.SetActive(false);
        if (_userMissionEditor != null) _userMissionEditor.SetActive(true);
        UpdateEditorValidation();
        FocusEditorTitle();
    }

    private void SetEditorScene(string sceneName)
    {
        if (_availableScenes == null || _availableScenes.Count == 0)
        {
            _availableScenes = GetAvailableScenes();
        }
        _editorSceneIndex = _availableScenes.IndexOf(sceneName);
        if (_editorSceneIndex < 0) _editorSceneIndex = 0;
        if (_editorSceneLabel != null && _availableScenes.Count > 0)
            _editorSceneLabel.text = _availableScenes[_editorSceneIndex];
        UpdateEditorValidation();
    }

    private string GetSelectedEditorScene()
    {
        if (_availableScenes != null && _availableScenes.Count > 0
            && _editorSceneIndex >= 0 && _editorSceneIndex < _availableScenes.Count)
            return _availableScenes[_editorSceneIndex];
        return _defaultGameScene;
    }

    private void HideEditor()
    {
        if (_conditionsModal != null) _conditionsModal.SetActive(false);
        if (_userMissionEditor != null) _userMissionEditor.SetActive(false);
        _editingUserMission = null;
        ResetDeleteConfirmation();
    }

    private void ResetDeleteConfirmation()
    {
        _deleteArmed = false;
        if (_editorDeleteLabel != null)
            _editorDeleteLabel.text = "УДАЛИТЬ";
    }

    private void FocusEditorTitle()
    {
        if (_editorTitle == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(_editorTitle.gameObject);
        _editorTitle.ActivateInputField();
        _editorTitle.MoveTextEnd(false);
    }

    private void OnClearEditor()
    {
        ResetDeleteConfirmation();
        if (_editorTitle != null) _editorTitle.text = "";
        if (_editorObjective != null) _editorObjective.text = "";
        if (_editorDescription != null) _editorDescription.text = "";
        SetEditorReward(25);
        SetEditorConditions(GetDefaultUserMissionConditions());
        SetEditorDifficulty("Базовая");
        SetEditorScene(_defaultGameScene);
        UpdateEditorValidation();
        FocusEditorTitle();
    }

    private void OnSaveEditor()
    {
        var title = (_editorTitle.text ?? "").Trim();
        var objective = (_editorObjective.text ?? "").Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(objective) || _editingConditions.Count == 0)
        {
            if (string.IsNullOrEmpty(title)) _editorTitle.Select();
            else if (string.IsNullOrEmpty(objective)) _editorObjective.Select();
            return;
        }

        var sceneName = GetSelectedEditorScene();
        UserMission savedMission;

        if (_editingUserMission == null)
        {
            var m = new UserMission
            {
                title = title,
                objective = objective,
                description = _editorDescription.text ?? "",
                rewardScience = Mathf.RoundToInt(_editorRewardSlider.value),
                sceneName = sceneName,
                difficulty = GetEditorDifficulty(),
                conditions = CloneEditorConditions(),
            };
            savedMission = UserMissionStore.Add(m);
        }
        else
        {
            _editingUserMission.title = title;
            _editingUserMission.objective = objective;
            _editingUserMission.description = _editorDescription.text ?? "";
            _editingUserMission.rewardScience = Mathf.RoundToInt(_editorRewardSlider.value);
            _editingUserMission.sceneName = sceneName;
            _editingUserMission.difficulty = GetEditorDifficulty();
            _editingUserMission.conditions = CloneEditorConditions();
            UserMissionStore.Update(_editingUserMission);
            savedMission = _editingUserMission;
        }

        HideEditor();
        RefreshUserMissionRows();
        UpdateMissionsCounter();
        MissionRowView savedRow = null;
        if (savedMission != null && !string.IsNullOrEmpty(savedMission.id))
            _userRows.TryGetValue(savedMission.id, out savedRow);
        SelectUserMission(savedMission, savedRow);
    }

    private void OnDeleteEditor()
    {
        if (_editingUserMission == null) return;

        if (!_deleteArmed)
        {
            _deleteArmed = true;
            if (_editorDeleteLabel != null)
                _editorDeleteLabel.text = "ТОЧНО?";
            if (_editorValidation != null)
                _editorValidation.text = "Нажми «ТОЧНО?» еще раз, чтобы удалить миссию.";
            return;
        }

        UserMissionStore.Delete(_editingUserMission.id);
        HideEditor();
        RefreshUserMissionRows();
        UpdateMissionsCounter();
        ClearDetails();
    }

    private List<MissionConditionData> CloneEditorConditions()
    {
        var result = new List<MissionConditionData>();
        for (int i = 0; i < _editingConditions.Count; i++)
            result.Add(_editingConditions[i].Clone());
        return result;
    }

    private void SetEditorConditions(List<MissionConditionData> conditions)
    {
        _editingConditions.Clear();
        if (conditions != null)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                var copy = conditions[i].Clone();
                NormalizeConditionValue(copy);
                _editingConditions.Add(copy);
            }
        }
        RefreshEditorConditionRows();
    }

    private List<MissionConditionData> GetUserMissionConditions(UserMission mission)
    {
        if (mission == null) return GetDefaultUserMissionConditions();
        mission.EnsureDefaults();
        if (mission.conditions.Count == 0)
            return GetConditionsFromText($"{mission.title} {mission.objective} {mission.description}");
        var result = new List<MissionConditionData>();
        for (int i = 0; i < mission.conditions.Count; i++)
            result.Add(mission.conditions[i].Clone());
        return result;
    }

    private List<MissionConditionData> GetBuiltInMissionConditions(Mission mission)
    {
        if (mission == null) return GetDefaultMissionConditions();
        return GetConditionsFromText($"{mission.title} {mission.objective} {mission.description}");
    }

    private List<MissionConditionData> GetConditionsFromText(string source)
    {
        source = (source ?? "").ToLowerInvariant();
        var result = new List<MissionConditionData>();

        if (source.Contains("питан")) result.Add(new MissionConditionData(MissionConditionType.PowerEnabled));
        if (source.Contains("зем")) result.Add(new MissionConditionData(MissionConditionType.FacingEarth));
        if (source.Contains("солн")) result.Add(new MissionConditionData(MissionConditionType.FacingSun));
        if (source.Contains("стабил")) result.Add(new MissionConditionData(MissionConditionType.Stabilized));
        if (source.Contains("фото") || source.Contains("сним")) result.Add(new MissionConditionData(MissionConditionType.PhotoTaken));
        if (source.Contains("отправ") || source.Contains("данн") || source.Contains("сообщ")) result.Add(new MissionConditionData(MissionConditionType.DataSent));

        if (result.Count == 0)
            return GetDefaultMissionConditions();
        return result;
    }

    private List<MissionConditionData> GetDefaultMissionConditions()
    {
        return new List<MissionConditionData>
        {
            new MissionConditionData(MissionConditionType.PowerEnabled),
            new MissionConditionData(MissionConditionType.Stabilized),
            new MissionConditionData(MissionConditionType.FacingEarth),
            new MissionConditionData(MissionConditionType.PhotoTaken),
            new MissionConditionData(MissionConditionType.DataSent),
        };
    }

    private List<MissionConditionData> GetDefaultUserMissionConditions()
    {
        return new List<MissionConditionData>
        {
            new MissionConditionData(MissionConditionType.PowerEnabled),
            new MissionConditionData(MissionConditionType.Stabilized),
            new MissionConditionData(MissionConditionType.PhotoTaken),
        };
    }

    private string GetConditionDisplayName(MissionConditionData condition)
    {
        if (condition == null) return "условие миссии";
        return condition.conditionType switch
        {
            MissionConditionType.PowerEnabled => "питание включено",
            MissionConditionType.FacingEarth => "повернуться к Земле",
            MissionConditionType.FacingSun => "повернуться к Солнцу",
            MissionConditionType.Stabilized => "спутник стабилизирован",
            MissionConditionType.PhotoTaken => "фото сделано",
            MissionConditionType.DataSent => "данные отправлены",
            MissionConditionType.BatteryAbovePercent => $"заряд батареи больше {Mathf.Clamp(condition.value, 1, 100)}%",
            MissionConditionType.WaitSeconds => $"подождать {Mathf.Clamp(condition.value, 1, 120)} сек.",
            _ => "условие миссии"
        };
    }

    private string GetConditionBaseName(MissionConditionType type)
    {
        return type switch
        {
            MissionConditionType.PowerEnabled => "Питание включено",
            MissionConditionType.FacingEarth => "Повернуться к Земле",
            MissionConditionType.FacingSun => "Повернуться к Солнцу",
            MissionConditionType.Stabilized => "Стабилизировать спутник",
            MissionConditionType.PhotoTaken => "Сделать фото",
            MissionConditionType.DataSent => "Отправить данные",
            MissionConditionType.BatteryAbovePercent => "Заряд батареи больше",
            MissionConditionType.WaitSeconds => "Подождать",
            _ => "Условие миссии"
        };
    }

    private string GetConditionValueLabel(MissionConditionData condition)
    {
        if (condition == null) return "";
        return condition.conditionType switch
        {
            MissionConditionType.BatteryAbovePercent => $"{Mathf.Clamp(condition.value, 1, 100)}%",
            MissionConditionType.WaitSeconds => $"{Mathf.Clamp(condition.value, 1, 120)} сек",
            _ => ""
        };
    }

    private string GetConditionShortName(MissionConditionType type)
    {
        return type switch
        {
            MissionConditionType.PowerEnabled => "питание",
            MissionConditionType.FacingEarth => "к Земле",
            MissionConditionType.FacingSun => "к Солнцу",
            MissionConditionType.Stabilized => "стабилизация",
            MissionConditionType.PhotoTaken => "фото",
            MissionConditionType.DataSent => "данные",
            MissionConditionType.BatteryAbovePercent => "батарея",
            MissionConditionType.WaitSeconds => "Ждать",
            _ => "условие"
        };
    }

    private bool ConditionUsesValue(MissionConditionType type)
    {
        return type == MissionConditionType.BatteryAbovePercent || type == MissionConditionType.WaitSeconds;
    }

    private void NormalizeConditionValue(MissionConditionData condition)
    {
        if (condition == null) return;
        if (condition.conditionType == MissionConditionType.BatteryAbovePercent)
            condition.value = Mathf.Clamp(condition.value <= 0 ? 50 : condition.value, 1, 100);
        else if (condition.conditionType == MissionConditionType.WaitSeconds)
            condition.value = Mathf.Clamp(condition.value <= 0 ? 5 : condition.value, 1, 120);
        else
            condition.value = 0;
    }

    private void AdjustConditionValue(MissionConditionData condition, int delta)
    {
        if (condition == null) return;
        condition.value += delta;
        NormalizeConditionValue(condition);
    }

    private MissionConditionType GetSuggestedConditionType(int index)
    {
        var order = new[]
        {
            MissionConditionType.PowerEnabled,
            MissionConditionType.FacingEarth,
            MissionConditionType.Stabilized,
            MissionConditionType.PhotoTaken,
            MissionConditionType.DataSent,
            MissionConditionType.BatteryAbovePercent,
            MissionConditionType.WaitSeconds,
            MissionConditionType.FacingSun,
        };
        return order[Mathf.Abs(index) % order.Length];
    }

    private void SetEditorDifficulty(string difficulty)
    {
        _editorDifficultyIndex = 0;
        for (int i = 0; i < _difficultyOptions.Length; i++)
        {
            if (_difficultyOptions[i] == difficulty)
            {
                _editorDifficultyIndex = i;
                break;
            }
        }
        UpdateDifficultyLabel();
    }

    private string GetEditorDifficulty()
    {
        if (_editorDifficultyIndex < 0 || _editorDifficultyIndex >= _difficultyOptions.Length)
            return _difficultyOptions[0];
        return _difficultyOptions[_editorDifficultyIndex];
    }

    private string GetMissionDifficulty(UserMission mission)
    {
        if (mission == null || string.IsNullOrEmpty(mission.difficulty))
            return "Базовая";
        return mission.difficulty;
    }

    private void UpdateDifficultyLabel()
    {
        if (_editorDifficultyLabel == null) return;
        _editorDifficultyLabel.text = GetEditorDifficulty();
    }

    private System.Collections.Generic.List<string> GetAvailableScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;
            var n = System.IO.Path.GetFileNameWithoutExtension(path);
            if (n == "MainMenu") continue; // never launch into the menu itself
            if (!list.Contains(n)) list.Add(n);
        }
        if (list.Count == 0) list.Add(_defaultGameScene);
        return list;
    }

    private void BuildSceneSelector(RectTransform parent)
    {
        _availableScenes = GetAvailableScenes();
        _editorSceneIndex = 0;

        var go = new GameObject("SceneSelector",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.06f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 14, 4, 4);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        _editorSceneLabel = labelGO.GetComponent<TextMeshProUGUI>();
        _editorSceneLabel.text = _availableScenes[0];
        if (_font != null) _editorSceneLabel.font = _font;
        _editorSceneLabel.fontSize = 17;
        _editorSceneLabel.color = _textPrimary;
        _editorSceneLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _editorSceneLabel.raycastTarget = false;
        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1;

        var hintGO = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(go.transform, false);
        var hintTmp = hintGO.GetComponent<TextMeshProUGUI>();
        hintTmp.text = $"клик — следующая ({_availableScenes.Count})  ▾";
        if (_font != null) hintTmp.font = _font;
        hintTmp.fontSize = 12;
        hintTmp.color = _textMuted;
        hintTmp.alignment = TextAlignmentOptions.MidlineRight;
        hintTmp.fontStyle = FontStyles.UpperCase;
        hintTmp.characterSpacing = 4f;
        hintTmp.raycastTarget = false;
        var hintLE = hintGO.AddComponent<LayoutElement>();
        hintLE.preferredWidth = 220;
        hintLE.flexibleWidth = 0;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
        c.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        c.fadeDuration = 0.08f;
        btn.colors = c;
        btn.onClick.AddListener(() =>
        {
            if (_availableScenes == null || _availableScenes.Count == 0) return;
            _editorSceneIndex = (_editorSceneIndex + 1) % _availableScenes.Count;
            _editorSceneLabel.text = _availableScenes[_editorSceneIndex];
        });

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 38;
        le.minHeight = 38;
        le.flexibleHeight = 0;
    }

    private bool IsSceneInBuild(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return true;
        }
        return false;
    }

    // ───────────── Settings & Credits screens ─────────────

    private RectTransform BuildScreenWithHeader(string name, string title, string subtitle)
    {
        var screen = CreateScreen(name);

        var headerGroup = CreateRect("HeaderGroup", screen);
        headerGroup.anchorMin = new Vector2(0, 1);
        headerGroup.anchorMax = new Vector2(0, 1);
        headerGroup.pivot = new Vector2(0, 1);
        headerGroup.anchoredPosition = new Vector2(_leftMargin, -60);
        headerGroup.sizeDelta = new Vector2(900, 100);

        var headerVlg = headerGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        headerVlg.spacing = 4;
        headerVlg.childAlignment = TextAnchor.UpperLeft;
        headerVlg.childForceExpandWidth = false;
        headerVlg.childForceExpandHeight = false;

        var header = CreateText(headerGroup, "ScreenHeader", title,
            56, _textPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
        header.characterSpacing = 4f;
        header.GetComponent<LayoutElement>().preferredHeight = 64;

        if (!string.IsNullOrEmpty(subtitle))
        {
            var sub = CreateText(headerGroup, "ScreenSubtitle", subtitle,
                18, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
            sub.characterSpacing = 8f;
            sub.GetComponent<LayoutElement>().preferredHeight = 24;
        }

        // Bottom-left back button
        var backBtn = CreateMenuButton(screen, "← НАЗАД", () => SwitchTo(_mainScreen));
        var backRT = backBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0, 0);
        backRT.anchorMax = new Vector2(0, 0);
        backRT.pivot = new Vector2(0, 0);
        backRT.anchoredPosition = new Vector2(_leftMargin, 30);
        backRT.sizeDelta = new Vector2(220, 50);
        var backLE = backBtn.GetComponent<LayoutElement>();
        if (backLE != null) Destroy(backLE);

        return screen;
    }

    private RectTransform CreateContentColumn(RectTransform screen, float width)
    {
        var body = CreateRect("Body", screen);
        body.anchorMin = new Vector2(0, 1);
        body.anchorMax = new Vector2(0, 1);
        body.pivot = new Vector2(0, 1);
        body.anchoredPosition = new Vector2(_leftMargin, -180);
        body.sizeDelta = new Vector2(width, 720);

        var vlg = body.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        return body;
    }

    private void BuildSettingsScreen()
    {
        _settingsScreen = BuildScreenWithHeader("SettingsScreen", "НАСТРОЙКИ", "АУДИО · ВИДЕО · ИНТЕРФЕЙС");
        var body = CreateContentColumn(_settingsScreen, 720);

        // Audio section
        CreateSectionLabel(body, "АУДИО", _accentSoft);

        var musicVolume = StartupMusicPlayer.GetSavedVolume();
        CreateSliderRow(body, "Музыка", musicVolume, value =>
        {
            StartupMusicPlayer.SetVolume(value);
        });

        // SFX placeholder (disabled)
        CreateSliderRow(body, "Звуки", 0.5f, _ => { }, interactable: false, hint: "Скоро");

        AddVerticalGap(body, 8);

        // Video section
        CreateSectionLabel(body, "ВИДЕО", _statusInfo);

        bool fullScreenInitial = Screen.fullScreen;
        CreateToggleRow(body, "Полноэкранный режим", fullScreenInitial, value =>
        {
            Screen.fullScreen = value;
        });

        AddVerticalGap(body, 8);

        // UI section (info only)
        CreateSectionLabel(body, "ИНТЕРФЕЙС", _statusOnline);
        var langInfo = CreateText(body, "LangInfo", "Язык интерфейса:  Русский",
            19, _textSecondary, FontStyles.Normal, TextAlignmentOptions.Left);
        langInfo.GetComponent<LayoutElement>().preferredHeight = 28;
    }

    private void BuildCreditsScreen()
    {
        _creditsScreen = BuildScreenWithHeader("CreditsScreen", "АВТОРЫ", "COSMA — Учебная орбитальная программа");
        var body = CreateContentColumn(_creditsScreen, 680);

        CreateSectionLabel(body, "ПРОГРАММА", _accentSoft);
        CreateCreditLine(body, "Автор программы", "Андрей Иванов");

        AddVerticalGap(body, 14);

        CreateSectionLabel(body, "ТЕХНОЛОГИИ", _statusInfo);
        CreateCreditLine(body, "Движок", "Unity");

        AddVerticalGap(body, 14);

        CreateSectionLabel(body, "МУЗЫКА", _statusOnline);
        CreateCreditLine(body, "Сборник", "Epic Star Wars Compilation");
    }

    private void AddVerticalGap(RectTransform parent, float height)
    {
        var spacer = CreateRect("Gap", parent);
        var le = spacer.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void CreateCreditLine(RectTransform parent, string role, string name)
    {
        var row = CreateRect($"Credit_{role}", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 30;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var roleText = CreateText(row, "Role", role.ToUpper(),
            16, _textMuted, FontStyles.UpperCase, TextAlignmentOptions.Left);
        roleText.characterSpacing = 6f;
        var roleLE = roleText.GetComponent<LayoutElement>();
        roleLE.preferredWidth = 240;
        roleLE.flexibleWidth = 0;

        var nameText = CreateText(row, "Name", name,
            19, _textPrimary, FontStyles.Normal, TextAlignmentOptions.Left);
        nameText.GetComponent<LayoutElement>().flexibleWidth = 1;
    }

    // ───────────── Settings controls ─────────────

    private void CreateSliderRow(RectTransform parent, string label, float initial,
        System.Action<float> onChanged, bool interactable = true, string hint = null)
    {
        var row = CreateRect($"Row_{label}", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 44;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // Label
        var labelText = CreateText(row, "Label", label,
            19, interactable ? _textPrimary : _textMuted, FontStyles.Normal, TextAlignmentOptions.Left);
        var labelLE = labelText.GetComponent<LayoutElement>();
        labelLE.preferredWidth = 240;
        labelLE.flexibleWidth = 0;

        // Slider
        var slider = CreateSliderControl(row, initial);
        slider.interactable = interactable;
        var sliderLE = slider.gameObject.GetComponent<LayoutElement>();
        if (sliderLE == null) sliderLE = slider.gameObject.AddComponent<LayoutElement>();
        sliderLE.preferredWidth = 320;
        sliderLE.preferredHeight = 36;
        sliderLE.flexibleWidth = 1;

        // Value
        var valueText = CreateText(row, "Value",
            interactable ? $"{Mathf.RoundToInt(initial * 100)}%" : (hint ?? ""),
            17, interactable ? _accent : _textMuted, FontStyles.Bold, TextAlignmentOptions.Right);
        valueText.characterSpacing = 4f;
        var valueLE = valueText.GetComponent<LayoutElement>();
        valueLE.preferredWidth = 90;
        valueLE.flexibleWidth = 0;

        if (interactable)
        {
            slider.onValueChanged.AddListener(v =>
            {
                valueText.text = $"{Mathf.RoundToInt(v * 100)}%";
                onChanged?.Invoke(v);
            });
        }
    }

    private Slider CreateSliderControl(RectTransform parent, float initial)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var goRT = go.GetComponent<RectTransform>();
        goRT.anchorMin = new Vector2(0, 0.5f);
        goRT.anchorMax = new Vector2(1, 0.5f);
        goRT.pivot = new Vector2(0.5f, 0.5f);
        var slider = go.GetComponent<Slider>();

        // Background (track)
        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.5f);
        bgRT.anchorMax = new Vector2(1, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(0, 6);
        bgRT.anchoredPosition = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);

        // Fill area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.5f);
        faRT.anchorMax = new Vector2(1, 0.5f);
        faRT.pivot = new Vector2(0.5f, 0.5f);
        faRT.sizeDelta = new Vector2(-18, 6);
        faRT.anchoredPosition = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.pivot = new Vector2(0.5f, 0.5f);
        fillRT.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = _accent;

        // Handle area
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = new Vector2(0, 0.5f);
        haRT.anchorMax = new Vector2(1, 0.5f);
        haRT.pivot = new Vector2(0.5f, 0.5f);
        haRT.sizeDelta = new Vector2(-18, 0);
        haRT.anchoredPosition = Vector2.zero;

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var hRT = handle.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.5f, 0.5f);
        hRT.anchorMax = new Vector2(0.5f, 0.5f);
        hRT.pivot = new Vector2(0.5f, 0.5f);
        hRT.sizeDelta = new Vector2(18, 18);
        handle.GetComponent<Image>().color = _accent;

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fillRT;
        slider.handleRect = hRT;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initial;

        var colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.fadeDuration = 0.1f;
        slider.colors = colors;

        return slider;
    }

    private void CreateToggleRow(RectTransform parent, string label, bool initial, System.Action<bool> onChanged)
    {
        var row = CreateRect($"Toggle_{label}", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 38;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var labelText = CreateText(row, "Label", label,
            19, _textPrimary, FontStyles.Normal, TextAlignmentOptions.Left);
        var labelLE = labelText.GetComponent<LayoutElement>();
        labelLE.preferredWidth = 240;
        labelLE.flexibleWidth = 0;

        var toggle = CreateToggleControl(row, initial);
        var toggleLE = toggle.gameObject.GetComponent<LayoutElement>();
        if (toggleLE == null) toggleLE = toggle.gameObject.AddComponent<LayoutElement>();
        toggleLE.preferredWidth = 56;
        toggleLE.preferredHeight = 22;
        toggleLE.flexibleWidth = 0;

        var stateText = CreateText(row, "State", initial ? "ВКЛ" : "ВЫКЛ",
            17, initial ? _accent : _textMuted, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Left);
        stateText.characterSpacing = 4f;
        var stateLE = stateText.GetComponent<LayoutElement>();
        stateLE.preferredWidth = 90;
        stateLE.flexibleWidth = 0;

        toggle.onValueChanged.AddListener(v =>
        {
            stateText.text = v ? "ВКЛ" : "ВЫКЛ";
            stateText.color = v ? _accent : _textMuted;
            onChanged?.Invoke(v);
        });
    }

    private Toggle CreateToggleControl(RectTransform parent, bool initial)
    {
        var go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var toggle = go.GetComponent<Toggle>();

        // Track
        var track = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        track.transform.SetParent(go.transform, false);
        var trackRT = track.GetComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0, 0.5f);
        trackRT.anchorMax = new Vector2(1, 0.5f);
        trackRT.pivot = new Vector2(0.5f, 0.5f);
        trackRT.sizeDelta = new Vector2(0, 18);
        trackRT.anchoredPosition = Vector2.zero;
        var trackImg = track.GetComponent<Image>();
        trackImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Knob
        var knob = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        knob.transform.SetParent(track.transform, false);
        var knobRT = knob.GetComponent<RectTransform>();
        knobRT.sizeDelta = new Vector2(16, 16);
        var knobImg = knob.GetComponent<Image>();
        knobImg.color = _accent;

        toggle.isOn = initial;
        toggle.targetGraphic = trackImg;
        toggle.graphic = null; // we render the knob ourselves so Toggle doesn't toggle its alpha

        // Position knob based on state via simple onValueChanged listener
        System.Action<bool> applyKnobPos = isOn =>
        {
            knobRT.anchorMin = new Vector2(isOn ? 1f : 0f, 0.5f);
            knobRT.anchorMax = new Vector2(isOn ? 1f : 0f, 0.5f);
            knobRT.pivot = new Vector2(isOn ? 1f : 0f, 0.5f);
            knobRT.anchoredPosition = new Vector2(isOn ? -1f : 1f, 0f);
            trackImg.color = isOn ? MultiplyAlpha(_accent, 0.35f) : new Color(1f, 1f, 1f, 0.15f);
        };
        applyKnobPos(initial);
        toggle.onValueChanged.AddListener(v => applyKnobPos(v));

        return toggle;
    }

    // ───────────── Navigation ─────────────

    private void ShowMain()
    {
        if (_continueButton != null)
            _continueButton.interactable = MissionProgress.HasAnyProgress;
        SwitchTo(_mainScreen);
    }

    private void SwitchTo(RectTransform screen)
    {
        if (_mainScreen != null) _mainScreen.gameObject.SetActive(screen == _mainScreen);
        if (_missionsScreen != null) _missionsScreen.gameObject.SetActive(screen == _missionsScreen);
        if (_settingsScreen != null) _settingsScreen.gameObject.SetActive(screen == _settingsScreen);
        if (_creditsScreen != null) _creditsScreen.gameObject.SetActive(screen == _creditsScreen);
    }

    // ───────────── Button actions ─────────────

    private void OnContinueClicked()
    {
        if (!MissionProgress.HasAnyProgress) return;
        var lastId = MissionProgress.LastPlayedId;
        var mission = FindMission(lastId);
        if (mission != null) { LaunchMission(mission); return; }

        var user = UserMissionStore.Find(lastId);
        if (user != null) LaunchUserMission(user);
    }

    private void OnNewGameClicked()
    {
        if (_missions != null && _missions.Length > 0 && _missions[0] != null)
            LaunchMission(_missions[0]);
        else
            SceneManager.LoadScene(_defaultGameScene);
    }

    private void OnMissionsClicked() => SwitchTo(_missionsScreen);

    private void OnLaunchMission()
    {
        if (_selectedMission == null) return;
        LaunchMission(_selectedMission);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LaunchMission(Mission mission)
    {
        MissionContext.StartMission(mission, _missions);
        var scene = string.IsNullOrEmpty(mission.sceneName) ? _defaultGameScene : mission.sceneName;
        SceneManager.LoadScene(scene);
    }

    private Mission FindMission(string id)
    {
        if (_missions == null) return null;
        foreach (var m in _missions)
            if (m != null && m.id == id) return m;
        return null;
    }

    // ───────────── UI helpers ─────────────

    private RectTransform CreateScreen(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(_root, false);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.GetComponent<RectTransform>();
        Stretch(rt);
        return rt;
    }

    private RectTransform CreateRect(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go.GetComponent<RectTransform>();
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Color color, RectOffset padding)
    {
        var panel = CreateRect(name, parent);
        var img = panel.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        AddBorder(panel.gameObject, _borderColor);
        return panel;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string text,
        float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (_font != null) tmp.font = _font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;

        go.AddComponent<LayoutElement>();
        return tmp;
    }

    private void CreateSectionLabel(RectTransform parent, string text, Color color)
    {
        var row = CreateRect($"Section_{text}", parent);
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 28;
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var label = CreateText(row, "Label", text, 17, color, FontStyles.UpperCase, TextAlignmentOptions.Left);
        label.characterSpacing = 4f;
        label.GetComponent<LayoutElement>().preferredHeight = 24;
    }

    private Button CreateMenuButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject($"Btn_{label}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        var textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        Stretch(trt);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        if (_font != null) tmp.font = _font;
        tmp.fontSize = _menuButtonFontSize;
        tmp.color = _buttonTextNormal;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.characterSpacing = 4f;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = tmp;
        ApplyMenuButtonColors(btn);
        btn.onClick.AddListener(action);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = _menuButtonHeight;

        // Subtle horizontal shift on hover
        var hover = go.AddComponent<MenuTextShift>();
        hover.target = trt;
        hover.shiftOnHover = 8f;

        return btn;
    }

    private void ApplyMenuButtonColors(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = _buttonTextNormal;
        colors.highlightedColor = _buttonTextHover;
        colors.pressedColor = _buttonTextPressed;
        colors.selectedColor = _buttonTextHover;
        colors.disabledColor = _buttonTextDisabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    private static Color MultiplyAlpha(Color c, float alpha)
        => new Color(c.r, c.g, c.b, alpha);

    private static Color FromHex(string hex, Color fallback)
    {
        return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
    }

    private void AddBorder(GameObject target, Color color)
    {
        var outline = target.GetComponent<Outline>();
        if (outline == null) outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private string GetMissionNumber(Mission mission)
    {
        if (_missions == null || mission == null) return "01";
        for (int i = 0; i < _missions.Length; i++)
        {
            if (_missions[i] == mission)
                return (i + 1).ToString("00");
        }
        return "01";
    }

    private int GetUserMissionNumber(UserMission mission)
    {
        var list = UserMissionStore.LoadAll();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == mission || list[i].id == mission.id)
                return i + 1;
        }
        return 1;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

// ───────────── Helpers (same file for compactness) ─────────────

internal class MenuTextShift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform target;
    public float shiftOnHover = 8f;
    public float duration = 0.12f;

    private Vector2 _start;
    private float _t;
    private bool _hover;

    private void Awake()
    {
        if (target != null) _start = target.offsetMin;
    }

    public void OnPointerEnter(PointerEventData eventData) { _hover = true; }
    public void OnPointerExit(PointerEventData eventData) { _hover = false; }

    private void Update()
    {
        if (target == null) return;
        _t += (_hover ? 1f : -1f) * (Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));
        _t = Mathf.Clamp01(_t);
        var min = target.offsetMin;
        min.x = _start.x + _t * shiftOnHover;
        target.offsetMin = min;
        var max = target.offsetMax;
        max.x = _start.x + _t * shiftOnHover; // shift both edges
        target.offsetMax = max;
    }
}
