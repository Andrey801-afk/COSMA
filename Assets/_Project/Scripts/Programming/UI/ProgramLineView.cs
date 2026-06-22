using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ProgramLineView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1)] private int lineNumber = 1;
    [SerializeField] private ProgramPanelController programPanel;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private RectTransform commandContainer;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image hoverImage;
    [SerializeField] private UIAnimationDriver animationDriver;

    private ProgramLineData programLine;
    private ProgramCommandView currentCommandView;
    private bool isHovered;
    private bool isExecuting;
    private readonly Color normalColor = new(0.20f, 0.21f, 0.23f, 0.62f);
    private readonly Color hoverColor = new(0.29f, 0.31f, 0.34f, 0.84f);
    private readonly Color executingColor = new(0.56f, 0.61f, 0.67f, 0.92f);

    public int LineNumber => lineNumber;
    public ProgramLineData LineData => programLine;

    public void Configure(
        int number,
        ProgramPanelController panel,
        TMP_Text lineNumberText,
        TMP_Text placeholder,
        RectTransform commandRoot,
        Image background,
        Image hover,
        UIAnimationDriver driver)
    {
        lineNumber = Mathf.Max(1, number);
        programPanel = panel;
        numberText = lineNumberText;
        placeholderText = placeholder;
        commandContainer = commandRoot;
        backgroundImage = background;
        hoverImage = hover;
        animationDriver = driver;
        RefreshLineNumber();
        UpdateVisualState(true);
    }

    public void SetProgramPanel(ProgramPanelController panel)
    {
        programPanel = panel;
    }

    public void BindLine(ProgramLineData line)
    {
        programLine = line;
        if (programLine != null)
        {
            lineNumber = programLine.LineNumber;
        }

        RefreshLineNumber();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ProgramCommandView programCommand = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponentInParent<ProgramCommandView>()
            : null;

        if (programCommand != null)
        {
            programCommand.RegisterDropTarget(this);
            return;
        }

        CommandPoolItemView poolItem = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponentInParent<CommandPoolItemView>()
            : null;

        if (poolItem == null || poolItem.Definition == null || programPanel == null)
        {
            return;
        }

        programPanel.AssignCommandToLine(this, poolItem.Definition);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisualState(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualState(false);
    }

    public void RefreshFromModel(ProgramCommandView prefab, int maxLines, UIAnimationDriver driver)
    {
        ClearCommandView();

        ProgramCommand command = programLine != null ? programLine.Command : null;
        bool hasCommand = command != null;
        if (placeholderText != null)
        {
            placeholderText.gameObject.SetActive(!hasCommand);
            Color placeholderColor = placeholderText.color;
            placeholderColor.a = hasCommand ? 0f : 0.62f;
            placeholderText.color = placeholderColor;
        }

        if (!hasCommand || prefab == null || commandContainer == null)
        {
            UpdateVisualState(false);
            return;
        }

        currentCommandView = Instantiate(prefab, commandContainer);
        currentCommandView.name = $"ProgramCommand_{lineNumber:00}_{command.CommandType}";
        currentCommandView.Initialize(command, maxLines, driver != null ? driver : animationDriver);
        currentCommandView.BindLineContext(programPanel, this);

        RectTransform commandRect = (RectTransform)currentCommandView.transform;
        commandRect.anchorMin = Vector2.zero;
        commandRect.anchorMax = Vector2.one;
        commandRect.offsetMin = Vector2.zero;
        commandRect.offsetMax = Vector2.zero;

        if (driver != null)
        {
            driver.PlayDrop(commandRect);
        }

        UpdateVisualState(false);
    }

    public void ClearCommandVisual()
    {
        ClearCommandView();

        if (placeholderText != null)
        {
            placeholderText.gameObject.SetActive(true);
            Color placeholderColor = placeholderText.color;
            placeholderColor.a = 0.62f;
            placeholderText.color = placeholderColor;
        }

        UpdateVisualState(false);
    }

    public void SetExecuting(bool active)
    {
        isExecuting = active;
        UpdateVisualState(false);
    }

    private void RefreshLineNumber()
    {
        if (numberText != null)
        {
            numberText.text = lineNumber.ToString("00");
        }
    }

    private void ClearCommandView()
    {
        if (currentCommandView != null)
        {
            if (animationDriver != null)
            {
                animationDriver.StopAnimations((RectTransform)currentCommandView.transform);
            }

            Destroy(currentCommandView.gameObject);
            currentCommandView = null;
        }

        if (commandContainer == null)
        {
            return;
        }

        for (int i = commandContainer.childCount - 1; i >= 0; i--)
        {
            RectTransform child = commandContainer.GetChild(i) as RectTransform;
            if (child != null && animationDriver != null)
            {
                animationDriver.StopAnimations(child);
            }

            Destroy(commandContainer.GetChild(i).gameObject);
        }
    }

    private void UpdateVisualState(bool immediate)
    {
        Color target = isExecuting ? executingColor : isHovered ? hoverColor : normalColor;

        if (animationDriver != null)
        {
            animationDriver.ColorTo(backgroundImage, target, immediate);
        }
        else if (backgroundImage != null)
        {
            backgroundImage.color = target;
        }

        if (hoverImage != null)
        {
            Color hover = target;
            hover.a = isHovered || isExecuting ? 0.34f : 0f;
            if (animationDriver != null)
            {
                animationDriver.ColorTo(hoverImage, hover, immediate);
            }
            else
            {
                hoverImage.color = hover;
            }
        }
    }
}
