using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ProgramCommandView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private const float JumpParameterWidth = 74f;
    private const float WaitParameterWidth = 82f;
    private const float ConditionalJumpParameterWidth = 218f;
    private const float EarthFacingSelectorWidth = 112f;
    private const string EarthFacingSideButtonName = "EarthFacingSideButton";
    private const string ConditionButtonName = "ConditionButton";

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image accentImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject parameterRoot;
    [SerializeField] private TMP_InputField targetLineInput;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UIAnimationDriver animationDriver;
    [SerializeField] private LayoutElement parameterLayoutElement;
    [SerializeField] private TMP_Text jumpTargetLabel;
    [SerializeField] private Button earthFacingSideButton;
    [SerializeField] private TMP_Text earthFacingSideButtonText;
    [SerializeField] private Button conditionButton;
    [SerializeField] private TMP_Text conditionButtonText;

    private ProgramCommand command;
    private ProgramPanelController programPanel;
    private ProgramLineView ownerLine;
    private Canvas rootCanvas;
    private RectTransform dragGhostLayer;
    private DragGhostView activeGhost;
    private ProgramLineView pendingDropTarget;
    private bool isDraggingCommand;
    private bool pointerInside;
    private bool tooltipVisible;
    private float stationarySince;
    private Vector2 lastPointerPosition;
    private int maxProgramLines = 13;

    public void Configure(
        RectTransform rect,
        Image background,
        Image accent,
        TMP_Text title,
        TMP_Text description,
        GameObject parameterContainer,
        TMP_InputField targetInput,
        CanvasGroup group,
        UIAnimationDriver driver)
    {
        rectTransform = rect;
        backgroundImage = background;
        accentImage = accent;
        titleText = title;
        descriptionText = description;
        parameterRoot = parameterContainer;
        targetLineInput = targetInput;
        canvasGroup = group;
        animationDriver = driver;
    }

    public void Initialize(ProgramCommand programCommand, int lineCount, UIAnimationDriver driver)
    {
        command = programCommand;
        maxProgramLines = Mathf.Max(1, lineCount);

        if (animationDriver == null)
        {
            animationDriver = driver;
        }

        if (rectTransform == null)
        {
            rectTransform = (RectTransform)transform;
        }

        if (titleText != null)
        {
            titleText.text = command != null ? command.DisplayName : "COMMAND";
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            descriptionText.gameObject.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        EnsureParameterReferences();

        if (backgroundImage != null && command != null)
        {
            Color color = Color.Lerp(new Color(0.34f, 0.36f, 0.38f, 1f), command.AccentColor, 0.28f);
            color.a = 0.58f;
            backgroundImage.color = color;
        }

        if (accentImage != null && command != null)
        {
            accentImage.color = command.AccentColor;
        }

        ConfigureParameterUi(command != null ? command.CommandType : default);

        if (targetLineInput != null)
        {
            targetLineInput.onValueChanged.RemoveListener(OnTargetLineChanged);
            targetLineInput.onEndEdit.RemoveListener(NormalizeTargetLine);
            ConfigureTargetInputMode();
            targetLineInput.text = BuildParameterInputText();
            targetLineInput.onValueChanged.AddListener(OnTargetLineChanged);
            targetLineInput.onEndEdit.AddListener(NormalizeTargetLine);
        }

        if (animationDriver != null)
        {
            animationDriver.PlayCommandAppear(rectTransform, canvasGroup);
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void Update()
    {
        if (!pointerInside || isDraggingCommand || tooltipVisible || command == null || command.Definition == null)
        {
            return;
        }

        if (Time.unscaledTime - stationarySince >= CommandHelpTooltip.DelaySeconds)
        {
            ShowTooltip(lastPointerPosition);
        }
    }

    private void OnDisable()
    {
        pointerInside = false;
        HideTooltip();
    }

    public void BindLineContext(ProgramPanelController panel, ProgramLineView lineView)
    {
        programPanel = panel;
        ownerLine = lineView;
        rootCanvas = GetComponentInParent<Canvas>();
        dragGhostLayer = ResolveDragGhostLayer();
    }

    public void RegisterDropTarget(ProgramLineView targetLine)
    {
        pendingDropTarget = targetLine;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || command == null || ownerLine == null)
        {
            return;
        }

        EnsureDragContext();
        if (rootCanvas == null || dragGhostLayer == null)
        {
            return;
        }

        activeGhost = CreateDragGhost();
        if (activeGhost == null)
        {
            return;
        }

        pendingDropTarget = null;
        isDraggingCommand = true;
        HideTooltip();
        activeGhost.FollowScreenPosition(eventData.position, rootCanvas, true);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.28f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingCommand || activeGhost == null)
        {
            return;
        }

        activeGhost.FollowScreenPosition(eventData.position, rootCanvas, false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingCommand)
        {
            return;
        }

        ProgramPanelController panel = programPanel;
        ProgramLineView sourceLine = ownerLine;
        ProgramLineView targetLine = pendingDropTarget;

        isDraggingCommand = false;
        ResetTooltipTimer(eventData != null ? eventData.position : lastPointerPosition);
        pendingDropTarget = null;
        DestroyActiveGhost();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (panel == null || sourceLine == null)
        {
            return;
        }

        if (targetLine != null)
        {
            panel.MoveCommand(sourceLine, targetLine);
            return;
        }

        panel.RemoveCommandFromLine(sourceLine);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ResetTooltipTimer(eventData != null ? eventData.position : lastPointerPosition);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        Vector2 pointerPosition = eventData != null ? eventData.position : lastPointerPosition;
        if ((pointerPosition - lastPointerPosition).sqrMagnitude <=
            CommandHelpTooltip.PointerMoveThreshold * CommandHelpTooltip.PointerMoveThreshold)
        {
            return;
        }

        ResetTooltipTimer(pointerPosition);
        HideTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        HideTooltip();
    }

    private void OnDestroy()
    {
        if (targetLineInput != null)
        {
            targetLineInput.onValueChanged.RemoveListener(OnTargetLineChanged);
            targetLineInput.onEndEdit.RemoveListener(NormalizeTargetLine);
        }

        if (earthFacingSideButton != null)
        {
            earthFacingSideButton.onClick.RemoveListener(OnEarthFacingSideClicked);
        }

        if (conditionButton != null)
        {
            conditionButton.onClick.RemoveListener(OnConditionClicked);
        }

        DestroyActiveGhost();
        HideTooltip();
    }

    private void OnTargetLineChanged(string value)
    {
        if (command == null)
        {
            return;
        }

        if (command.CommandType == CommandType.Wait)
        {
            if (TryParseSeconds(value, out float seconds))
            {
                command.WaitSeconds = seconds;
            }

            return;
        }

        if (int.TryParse(value, out int targetLine))
        {
            command.TargetLineNumber = Mathf.Clamp(targetLine, 1, maxProgramLines);
        }
    }

    private void NormalizeTargetLine(string value)
    {
        if (command == null || targetLineInput == null)
        {
            return;
        }

        if (command.CommandType == CommandType.Wait)
        {
            command.WaitSeconds = Mathf.Max(0f, command.WaitSeconds);
            targetLineInput.SetTextWithoutNotify(command.WaitSeconds.ToString("0.#", CultureInfo.InvariantCulture));
            return;
        }

        command.TargetLineNumber = Mathf.Clamp(command.TargetLineNumber, 1, maxProgramLines);
        targetLineInput.SetTextWithoutNotify(command.TargetLineNumber.ToString("00"));
    }

    private void ConfigureParameterUi(CommandType commandType)
    {
        if (parameterRoot == null)
        {
            return;
        }

        bool isJump = commandType == CommandType.JumpTo;
        bool isConditionalJump = commandType == CommandType.ConditionalJump;
        bool isWait = commandType == CommandType.Wait;
        bool isRotateToEarth = commandType == CommandType.RotateToEarth;
        bool usesTargetInput = isJump || isConditionalJump || isWait;

        parameterRoot.SetActive(usesTargetInput || isRotateToEarth);
        if (!parameterRoot.activeSelf)
        {
            return;
        }

        if (parameterLayoutElement != null)
        {
            parameterLayoutElement.preferredWidth = isConditionalJump
                ? ConditionalJumpParameterWidth
                : isWait ? WaitParameterWidth
                : isJump ? JumpParameterWidth
                : isRotateToEarth ? EarthFacingSelectorWidth : -1f;
        }

        if (jumpTargetLabel != null)
        {
            jumpTargetLabel.gameObject.SetActive(usesTargetInput);
            jumpTargetLabel.text = isWait ? "SEC" : isConditionalJump ? "IF" : "TO";
        }

        if (targetLineInput != null)
        {
            targetLineInput.gameObject.SetActive(usesTargetInput);
        }

        EnsureEarthFacingSideButton();
        if (earthFacingSideButton != null)
        {
            earthFacingSideButton.gameObject.SetActive(isRotateToEarth);
            earthFacingSideButton.onClick.RemoveListener(OnEarthFacingSideClicked);
            if (isRotateToEarth)
            {
                earthFacingSideButton.onClick.AddListener(OnEarthFacingSideClicked);
                UpdateEarthFacingSideButtonLabel();
            }
        }

        EnsureConditionButton();
        if (conditionButton != null)
        {
            conditionButton.gameObject.SetActive(isConditionalJump);
            conditionButton.onClick.RemoveListener(OnConditionClicked);
            if (isConditionalJump)
            {
                conditionButton.onClick.AddListener(OnConditionClicked);
                UpdateConditionButtonLabel();
            }
        }
    }

    private void EnsureParameterReferences()
    {
        if (parameterRoot == null)
        {
            return;
        }

        if (parameterLayoutElement == null)
        {
            parameterLayoutElement = parameterRoot.GetComponent<LayoutElement>();
        }

        if (jumpTargetLabel == null)
        {
            Transform jumpLabelTransform = parameterRoot.transform.Find("ToLabel");
            if (jumpLabelTransform != null)
            {
                jumpTargetLabel = jumpLabelTransform.GetComponent<TMP_Text>();
            }
        }

        EnsureEarthFacingSideButton();
        EnsureConditionButton();
    }

    private void ConfigureTargetInputMode()
    {
        if (targetLineInput == null || command == null)
        {
            return;
        }

        if (command.CommandType == CommandType.Wait)
        {
            targetLineInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            targetLineInput.characterLimit = 4;
            return;
        }

        targetLineInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        targetLineInput.characterLimit = 2;
    }

    private string BuildParameterInputText()
    {
        if (command == null)
        {
            return "01";
        }

        if (command.CommandType == CommandType.Wait)
        {
            return command.WaitSeconds.ToString("0.#", CultureInfo.InvariantCulture);
        }

        return Mathf.Clamp(command.TargetLineNumber, 1, maxProgramLines).ToString("00");
    }

    private static bool TryParseSeconds(string value, out float seconds)
    {
        string normalizedValue = string.IsNullOrWhiteSpace(value)
            ? "0"
            : value.Replace(',', '.');
        return float.TryParse(
            normalizedValue,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out seconds);
    }

    private void EnsureEarthFacingSideButton()
    {
        if (parameterRoot == null || earthFacingSideButton != null)
        {
            return;
        }

        Transform existingButton = parameterRoot.transform.Find(EarthFacingSideButtonName);
        if (existingButton != null)
        {
            earthFacingSideButton = existingButton.GetComponent<Button>();
            earthFacingSideButtonText = existingButton.GetComponentInChildren<TMP_Text>(true);
            return;
        }

        GameObject buttonObject = new GameObject(
            EarthFacingSideButtonName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(Shadow));
        buttonObject.transform.SetParent(parameterRoot.transform, false);
        buttonObject.SetActive(false);

        LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
        buttonLayout.preferredWidth = EarthFacingSelectorWidth;
        buttonLayout.minWidth = 84f;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.02f, 0.032f, 0.04f, 0.96f);
        buttonImage.raycastTarget = true;

        Shadow buttonShadow = buttonObject.GetComponent<Shadow>();
        buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        buttonShadow.effectDistance = new Vector2(0f, -4f);
        buttonShadow.useGraphicAlpha = true;

        earthFacingSideButton = buttonObject.GetComponent<Button>();

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 0f);
        textRect.offsetMax = new Vector2(-4f, 0f);

        earthFacingSideButtonText = textObject.GetComponent<TMP_Text>();
        earthFacingSideButtonText.fontSize = 11f;
        earthFacingSideButtonText.fontStyle = FontStyles.Bold;
        earthFacingSideButtonText.alignment = TextAlignmentOptions.Center;
        earthFacingSideButtonText.textWrappingMode = TextWrappingModes.NoWrap;
        earthFacingSideButtonText.color = new Color(0.88f, 0.9f, 0.92f, 1f);

        TMP_Text referenceText = targetLineInput != null ? targetLineInput.textComponent : titleText;
        if (referenceText != null)
        {
            earthFacingSideButtonText.font = referenceText.font;
            earthFacingSideButtonText.fontSharedMaterial = referenceText.fontSharedMaterial;
            earthFacingSideButtonText.fontSize = referenceText.fontSize;
        }
    }

    private void EnsureConditionButton()
    {
        if (parameterRoot == null || conditionButton != null)
        {
            return;
        }

        Transform existingButton = parameterRoot.transform.Find(ConditionButtonName);
        if (existingButton != null)
        {
            conditionButton = existingButton.GetComponent<Button>();
            conditionButtonText = existingButton.GetComponentInChildren<TMP_Text>(true);
            return;
        }

        GameObject buttonObject = new GameObject(
            ConditionButtonName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(Shadow));
        buttonObject.transform.SetParent(parameterRoot.transform, false);
        buttonObject.SetActive(false);

        LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
        buttonLayout.preferredWidth = 134f;
        buttonLayout.minWidth = 118f;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.02f, 0.032f, 0.04f, 0.96f);
        buttonImage.raycastTarget = true;

        Shadow buttonShadow = buttonObject.GetComponent<Shadow>();
        buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        buttonShadow.effectDistance = new Vector2(0f, -4f);
        buttonShadow.useGraphicAlpha = true;

        conditionButton = buttonObject.GetComponent<Button>();

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 0f);
        textRect.offsetMax = new Vector2(-4f, 0f);

        conditionButtonText = textObject.GetComponent<TMP_Text>();
        conditionButtonText.fontSize = 11f;
        conditionButtonText.fontStyle = FontStyles.Bold;
        conditionButtonText.alignment = TextAlignmentOptions.Center;
        conditionButtonText.textWrappingMode = TextWrappingModes.NoWrap;
        conditionButtonText.color = new Color(0.88f, 0.9f, 0.92f, 1f);

        TMP_Text referenceText = targetLineInput != null ? targetLineInput.textComponent : titleText;
        if (referenceText != null)
        {
            conditionButtonText.font = referenceText.font;
            conditionButtonText.fontSharedMaterial = referenceText.fontSharedMaterial;
            conditionButtonText.fontSize = referenceText.fontSize;
        }
    }

    private void OnEarthFacingSideClicked()
    {
        if (command == null)
        {
            return;
        }

        command.EarthFacingSide = command.EarthFacingSide == EarthFacingSide.Camera
            ? EarthFacingSide.Antenna
            : EarthFacingSide.Camera;
        UpdateEarthFacingSideButtonLabel();
    }

    private void UpdateEarthFacingSideButtonLabel()
    {
        if (command == null || earthFacingSideButtonText == null)
        {
            return;
        }

        earthFacingSideButtonText.text = command.EarthFacingSide == EarthFacingSide.Camera
            ? "CAMERA"
            : "ANTENNA";
    }

    private void OnConditionClicked()
    {
        if (command == null)
        {
            return;
        }

        int nextValue = ((int)command.Condition + 1) % System.Enum.GetValues(typeof(CommandConditionType)).Length;
        command.Condition = (CommandConditionType)nextValue;
        UpdateConditionButtonLabel();
    }

    private void UpdateConditionButtonLabel()
    {
        if (command == null || conditionButtonText == null)
        {
            return;
        }

        conditionButtonText.text = GetConditionLabel(command.Condition);
    }

    private static string GetConditionLabel(CommandConditionType condition)
    {
        return condition switch
        {
            CommandConditionType.PowerOn => "ПИТАНИЕ",
            CommandConditionType.SunDataReady => "СОЛНЦЕ",
            CommandConditionType.EarthDataReady => "ЗЕМЛЯ",
            CommandConditionType.FacingEarth => "К ЗЕМЛЕ",
            CommandConditionType.FacingSun => "К СОЛНЦУ",
            CommandConditionType.PhotoTaken => "ФОТО",
            CommandConditionType.EarthInFrame => "В КАДРЕ",
            CommandConditionType.DataSent => "ОТПР.",
            CommandConditionType.LastCommandSuccess => "OK",
            CommandConditionType.LastCommandFailed => "ОШИБКА",
            CommandConditionType.Stabilized => "СТАБ.",
            CommandConditionType.BatteryLow => "БАТ<10",
            CommandConditionType.GyrosCalibrated => "ГИРО OK",
            CommandConditionType.CameraCoverOpen => "КРЫШКА",
            CommandConditionType.DataCompressed => "СЖАТО",
            CommandConditionType.CommunicationLinkAvailable => "СВЯЗЬ",
            _ => condition.ToString()
        };
    }

    private void EnsureDragContext()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (animationDriver == null && rootCanvas != null)
        {
            animationDriver = rootCanvas.GetComponent<UIAnimationDriver>();
        }

        if (dragGhostLayer == null)
        {
            dragGhostLayer = ResolveDragGhostLayer();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private RectTransform ResolveDragGhostLayer()
    {
        Canvas canvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        Transform layer = canvas.transform.Find("DragGhostLayer");
        return layer as RectTransform;
    }

    private DragGhostView CreateDragGhost()
    {
        if (dragGhostLayer == null)
        {
            return null;
        }

        GameObject root = new GameObject(
            "GhostCard",
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(DragGhostView));
        root.transform.SetParent(dragGhostLayer, false);
        root.transform.SetAsLastSibling();

        RectTransform ghostRect = root.GetComponent<RectTransform>();
        ghostRect.sizeDelta = rectTransform != null ? rectTransform.rect.size : new Vector2(220f, 46f);

        Image ghostBackground = root.GetComponent<Image>();
        ghostBackground.sprite = backgroundImage != null ? backgroundImage.sprite : null;
        ghostBackground.type = backgroundImage != null ? backgroundImage.type : Image.Type.Sliced;
        ghostBackground.color = command != null ? command.AccentColor : new Color(0.76f, 0.79f, 0.82f, 0.90f);
        ghostBackground.raycastTarget = false;

        CanvasGroup ghostGroup = root.GetComponent<CanvasGroup>();
        ghostGroup.alpha = 0.92f;
        ghostGroup.blocksRaycasts = false;
        ghostGroup.interactable = false;

        GameObject textObject = new GameObject(
            "TitleText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI ghostText = textObject.GetComponent<TextMeshProUGUI>();
        ghostText.text = command != null ? command.DisplayName : "COMMAND";
        ghostText.alignment = TextAlignmentOptions.Center;
        ghostText.fontSize = titleText != null ? titleText.fontSize : 16f;
        ghostText.fontStyle = FontStyles.Bold;
        ghostText.color = new Color(0.02f, 0.04f, 0.05f, 1f);
        ghostText.raycastTarget = false;

        if (titleText != null)
        {
            ghostText.font = titleText.font;
            ghostText.fontSharedMaterial = titleText.fontSharedMaterial;
        }

        DragGhostView ghostView = root.GetComponent<DragGhostView>();
        ghostView.Configure(ghostRect, ghostBackground, ghostText, ghostGroup, animationDriver);
        ghostView.Initialize(command != null ? command.Definition : null);
        return ghostView;
    }

    private void DestroyActiveGhost()
    {
        if (activeGhost == null)
        {
            return;
        }

        activeGhost.StopAnimations();
        Destroy(activeGhost.gameObject);
        activeGhost = null;
    }

    private void ResetTooltipTimer(Vector2 pointerPosition)
    {
        lastPointerPosition = pointerPosition;
        stationarySince = Time.unscaledTime;
    }

    private void ShowTooltip(Vector2 pointerPosition)
    {
        EnsureDragContext();
        if (CommandHelpTooltip.Show(this, rootCanvas, command != null ? command.Definition : null, pointerPosition))
        {
            tooltipVisible = true;
        }
    }

    private void HideTooltip()
    {
        tooltipVisible = false;
        CommandHelpTooltip.Hide(this);
    }
}
