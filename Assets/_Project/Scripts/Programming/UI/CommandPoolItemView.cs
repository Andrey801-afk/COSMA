using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CommandPoolItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private CommandDefinition definition;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform dragGhostLayer;
    [SerializeField] private DragGhostView dragGhostPrefab;
    [SerializeField] private UIAnimationDriver animationDriver;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image accentImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private DragGhostView activeGhost;
    private Color normalColor;
    private readonly Color hoverColor = new(0.12f, 0.20f, 0.24f, 0.92f);
    private bool pointerInside;
    private bool dragging;
    private bool tooltipVisible;
    private float stationarySince;
    private Vector2 lastPointerPosition;

    public CommandDefinition Definition => definition;

    public void SetDefinition(CommandDefinition commandDefinition)
    {
        definition = commandDefinition;
        ApplyDefinition();
    }

    public void Configure(
        CommandDefinition commandDefinition,
        Canvas canvas,
        RectTransform ghostLayer,
        DragGhostView ghostPrefab,
        UIAnimationDriver driver,
        CanvasGroup group,
        Image background,
        Image accent,
        TMP_Text title,
        TMP_Text description)
    {
        definition = commandDefinition;
        rootCanvas = canvas;
        dragGhostLayer = ghostLayer;
        dragGhostPrefab = ghostPrefab;
        animationDriver = driver;
        canvasGroup = group;
        backgroundImage = background;
        accentImage = accent;
        titleText = title;
        descriptionText = description;
        ApplyDefinition();
    }

    private void Awake()
    {
        ApplyDefinition();
    }

    private void Update()
    {
        if (!pointerInside || dragging || tooltipVisible || definition == null)
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
        dragging = false;
        HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureReferences();
        dragging = true;
        HideTooltip();

        if (definition == null || dragGhostPrefab == null || dragGhostLayer == null)
        {
            return;
        }

        activeGhost = Instantiate(dragGhostPrefab, dragGhostLayer);
        activeGhost.name = "GhostCard";
        activeGhost.transform.SetAsLastSibling();
        activeGhost.Initialize(definition);
        activeGhost.FollowScreenPosition(eventData.position, rootCanvas, true);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.62f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activeGhost != null)
        {
            activeGhost.FollowScreenPosition(eventData.position, rootCanvas, true);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        ResetTooltipTimer(eventData != null ? eventData.position : lastPointerPosition);

        if (activeGhost != null)
        {
            activeGhost.StopAnimations();
            Destroy(activeGhost.gameObject);
            activeGhost = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        dragging = false;
        ResetTooltipTimer(eventData != null ? eventData.position : lastPointerPosition);

        if (animationDriver != null)
        {
            animationDriver.ColorTo(backgroundImage, hoverColor);
            animationDriver.ScaleTo((RectTransform)transform, Vector3.one * 1.015f);
        }
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

        if (animationDriver != null)
        {
            animationDriver.ColorTo(backgroundImage, normalColor);
            animationDriver.ScaleTo((RectTransform)transform, Vector3.one);
        }
    }

    private void ApplyDefinition()
    {
        if (backgroundImage != null)
        {
            normalColor = backgroundImage.color;
        }

        if (titleText != null)
        {
            titleText.text = definition != null ? definition.DisplayName : string.Empty;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            descriptionText.gameObject.SetActive(false);
        }

        if (accentImage != null && definition != null)
        {
            accentImage.color = definition.AccentColor;
        }
    }

    private void EnsureReferences()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (animationDriver == null && rootCanvas != null)
        {
            animationDriver = rootCanvas.GetComponent<UIAnimationDriver>();
        }
    }

    private void ResetTooltipTimer(Vector2 pointerPosition)
    {
        lastPointerPosition = pointerPosition;
        stationarySince = Time.unscaledTime;
    }

    private void ShowTooltip(Vector2 pointerPosition)
    {
        EnsureReferences();
        if (CommandHelpTooltip.Show(this, rootCanvas, definition, pointerPosition))
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
