using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private UIAnimationDriver animationDriver;
    [SerializeField] private RectTransform rectTransform;

    public void Configure(UIAnimationDriver driver, RectTransform rect)
    {
        animationDriver = driver;
        rectTransform = rect;
    }

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = (RectTransform)transform;
        }

        if (animationDriver == null)
        {
            animationDriver = GetComponentInParent<UIAnimationDriver>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        animationDriver?.ScaleTo(rectTransform, Vector3.one * 0.96f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        animationDriver?.ScaleTo(rectTransform, Vector3.one);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        animationDriver?.ScaleTo(rectTransform, Vector3.one);
    }
}
