using UnityEngine;
using UnityEngine.UI;

public sealed class CommandPaletteContentSizer : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    public void Configure(RectTransform viewportTransform, RectTransform contentTransform)
    {
        viewport = viewportTransform;
        content = contentTransform;
        Resize();
    }

    private void OnEnable()
    {
        Resize();
    }

    private void LateUpdate()
    {
        Resize();
    }

    private void OnRectTransformDimensionsChange()
    {
        Resize();
    }

    public void Resize()
    {
        if (viewport == null || content == null)
        {
            return;
        }

        Vector2 currentPosition = content.anchoredPosition;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, content.offsetMin.y);
        content.offsetMax = Vector2.zero;

        float viewportHeight = Mathf.Max(1f, viewport.rect.height);
        float preferredHeight = Mathf.Max(1f, CalculatePreferredHeight());
        float targetHeight = Mathf.Max(viewportHeight, preferredHeight);
        bool heightChanged = !Mathf.Approximately(content.rect.height, targetHeight);
        if (heightChanged)
        {
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            LayoutRebuilder.MarkLayoutForRebuild(content);
        }

        float maxScrollY = Mathf.Max(0f, targetHeight - viewportHeight);
        float nextY = Mathf.Clamp(currentPosition.y, 0f, maxScrollY);
        if (!Mathf.Approximately(content.anchoredPosition.y, nextY))
        {
            content.anchoredPosition = new Vector2(currentPosition.x, nextY);
        }
    }

    private float CalculatePreferredHeight()
    {
        float layoutHeight = LayoutUtility.GetPreferredHeight(content);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            return layoutHeight;
        }

        float childrenHeight = layout.padding.top + layout.padding.bottom;
        int visibleChildren = 0;
        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            LayoutElement childLayout = child.GetComponent<LayoutElement>();
            if (childLayout != null && childLayout.ignoreLayout)
            {
                continue;
            }

            float childHeight = LayoutUtility.GetPreferredHeight(child);
            if (childHeight <= 0f)
            {
                childHeight = LayoutUtility.GetMinHeight(child);
            }

            if (childHeight <= 0f)
            {
                childHeight = child.rect.height;
            }

            childrenHeight += Mathf.Max(1f, childHeight);
            visibleChildren++;
        }

        if (visibleChildren > 1)
        {
            childrenHeight += layout.spacing * (visibleChildren - 1);
        }

        return Mathf.Max(layoutHeight, childrenHeight);
    }
}
