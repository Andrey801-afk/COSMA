using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DragGhostView : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UIAnimationDriver animationDriver;

    public void Configure(RectTransform rect, Image background, TMP_Text title, CanvasGroup group, UIAnimationDriver driver)
    {
        rectTransform = rect;
        backgroundImage = background;
        titleText = title;
        canvasGroup = group;
        animationDriver = driver;
    }

    public void Initialize(CommandDefinition definition)
    {
        if (rectTransform == null)
        {
            rectTransform = (RectTransform)transform;
        }

        if (titleText != null)
        {
            titleText.text = definition != null ? definition.DisplayName : "COMMAND";
        }

        if (backgroundImage != null && definition != null)
        {
            Color color = definition.AccentColor;
            color.a = 0.9f;
            backgroundImage.color = color;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.92f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void FollowScreenPosition(Vector2 screenPosition, Canvas canvas, bool immediate)
    {
        if (rectTransform == null || rectTransform.parent == null || canvas == null)
        {
            return;
        }

        if (animationDriver == null)
        {
            animationDriver = GetComponentInParent<UIAnimationDriver>();
        }

        var parent = (RectTransform)rectTransform.parent;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, camera, out Vector2 localPoint))
        {
            return;
        }

        if (animationDriver != null)
        {
            animationDriver.MoveAnchored(rectTransform, localPoint, immediate);
        }
        else
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void StopAnimations()
    {
        if (animationDriver == null)
        {
            animationDriver = GetComponentInParent<UIAnimationDriver>();
        }

        if (animationDriver != null && rectTransform != null)
        {
            animationDriver.StopAnimations(rectTransform);
        }
    }
}
