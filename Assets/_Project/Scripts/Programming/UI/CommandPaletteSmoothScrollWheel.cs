using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public sealed class CommandPaletteSmoothScrollWheel : MonoBehaviour, IScrollHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private bool invertWheel = true;
    [SerializeField, Min(1f)] private float wheelStepPixels = 82f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.11f;

    private RectTransform content;
    private RectTransform viewport;
    private float targetY;
    private float velocityY;
    private bool hasTarget;
    private bool smoothing;

    public void Configure(ScrollRect targetScrollRect, bool invert)
    {
        scrollRect = targetScrollRect;
        invertWheel = invert;
        RefreshReferences();
        SyncTargetToContent();
    }

    private void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        RefreshReferences();
    }

    private void OnEnable()
    {
        SyncTargetToContent();
    }

    private void LateUpdate()
    {
        RefreshReferences();
        if (scrollRect == null || content == null || viewport == null)
        {
            return;
        }

        if (!hasTarget || !smoothing)
        {
            SyncTargetToContent();
            return;
        }

        targetY = ClampY(targetY);
        float currentY = content.anchoredPosition.y;
        float nextY = Mathf.SmoothDamp(
            currentY,
            targetY,
            ref velocityY,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        nextY = ClampY(nextY);
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, nextY);

        if (Mathf.Abs(nextY - targetY) <= 0.1f && Mathf.Abs(velocityY) <= 0.1f)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
            velocityY = 0f;
            smoothing = false;
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        RefreshReferences();
        if (scrollRect == null || content == null || viewport == null)
        {
            return;
        }

        if (!hasTarget)
        {
            SyncTargetToContent();
        }

        float direction = invertWheel ? -1f : 1f;
        targetY = ClampY(targetY + eventData.scrollDelta.y * wheelStepPixels * direction);
        velocityY = 0f;
        smoothing = true;
        scrollRect.velocity = Vector2.zero;
        eventData.Use();
    }

    private void RefreshReferences()
    {
        if (scrollRect == null)
        {
            return;
        }

        content = scrollRect.content;
        viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
    }

    private void SyncTargetToContent()
    {
        RefreshReferences();
        if (content == null)
        {
            hasTarget = false;
            return;
        }

        targetY = ClampY(content.anchoredPosition.y);
        velocityY = 0f;
        smoothing = false;
        hasTarget = true;
    }

    private float ClampY(float y)
    {
        if (content == null || viewport == null)
        {
            return y;
        }

        float maxY = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        return Mathf.Clamp(y, 0f, maxY);
    }
}
