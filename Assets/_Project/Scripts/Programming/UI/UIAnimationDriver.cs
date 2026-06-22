#if COSMA_USE_DOTWEEN
using DG.Tweening;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIAnimationDriver : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float fastDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float normalDuration = 0.18f;

    private readonly Dictionary<int, Coroutine> moveRoutines = new();
    private readonly Dictionary<int, Coroutine> scaleRoutines = new();
    private readonly Dictionary<int, Coroutine> colorRoutines = new();
    private readonly Dictionary<int, Coroutine> fadeRoutines = new();

    public void MoveAnchored(RectTransform target, Vector2 anchoredPosition, bool immediate = false)
    {
        if (target == null)
        {
            return;
        }

        if (immediate)
        {
            StopTrackedRoutine(target, moveRoutines);
            TrySetAnchoredPosition(target, anchoredPosition);
            return;
        }

#if COSMA_USE_DOTWEEN
        target.DOAnchorPos(anchoredPosition, fastDuration).SetEase(Ease.OutQuad);
#else
        RestartTrackedRoutine(target, moveRoutines, MoveAnchoredRoutine(target, anchoredPosition, fastDuration));
#endif
    }

    public void ScaleTo(RectTransform target, Vector3 scale, bool immediate = false)
    {
        if (target == null)
        {
            return;
        }

        if (immediate)
        {
            StopTrackedRoutine(target, scaleRoutines);
            TrySetLocalScale(target, scale);
            return;
        }

#if COSMA_USE_DOTWEEN
        target.DOScale(scale, fastDuration).SetEase(Ease.OutQuad);
#else
        RestartTrackedRoutine(target, scaleRoutines, ScaleRoutine(target, scale, fastDuration));
#endif
    }

    public void ColorTo(Image target, Color color, bool immediate = false)
    {
        if (target == null)
        {
            return;
        }

        if (immediate)
        {
            StopTrackedRoutine(target, colorRoutines);
            TrySetColor(target, color);
            return;
        }

#if COSMA_USE_DOTWEEN
        target.DOColor(color, fastDuration).SetEase(Ease.OutQuad);
#else
        RestartTrackedRoutine(target, colorRoutines, ColorRoutine(target, color, fastDuration));
#endif
    }

    public void PlayCommandAppear(RectTransform target, CanvasGroup canvasGroup)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = Vector3.one * 0.82f;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

#if COSMA_USE_DOTWEEN
        target.DOScale(Vector3.one, normalDuration).SetEase(Ease.OutBack);
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(1f, normalDuration).SetEase(Ease.OutQuad);
        }
#else
        RestartTrackedRoutine(target, scaleRoutines, ScaleRoutine(target, Vector3.one, normalDuration));
        if (canvasGroup != null)
        {
            RestartTrackedRoutine(canvasGroup, fadeRoutines, FadeRoutine(canvasGroup, 1f, normalDuration));
        }
#endif
    }

    public void PlayDrop(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = Vector3.one * 1.08f;
#if COSMA_USE_DOTWEEN
        target.DOScale(Vector3.one, normalDuration).SetEase(Ease.OutBack);
#else
        RestartTrackedRoutine(target, scaleRoutines, ScaleRoutine(target, Vector3.one, normalDuration));
#endif
    }

    public void StopAnimations(RectTransform target)
    {
        StopTrackedRoutine(target, moveRoutines);
        StopTrackedRoutine(target, scaleRoutines);
    }

    private void OnDisable()
    {
        StopAllTrackedRoutines();
    }

    private static float Ease(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void RestartTrackedRoutine(Object target, Dictionary<int, Coroutine> registry, IEnumerator routine)
    {
        if (target == null)
        {
            return;
        }

        int key = target.GetInstanceID();
        StopTrackedRoutine(key, registry);
        registry[key] = StartCoroutine(RunTrackedRoutine(key, registry, routine));
    }

    private IEnumerator RunTrackedRoutine(int key, Dictionary<int, Coroutine> registry, IEnumerator routine)
    {
        yield return routine;
        registry.Remove(key);
    }

    private void StopAllTrackedRoutines()
    {
        StopTrackedRoutines(moveRoutines);
        StopTrackedRoutines(scaleRoutines);
        StopTrackedRoutines(colorRoutines);
        StopTrackedRoutines(fadeRoutines);
    }

    private void StopTrackedRoutines(Dictionary<int, Coroutine> registry)
    {
        foreach (Coroutine routine in registry.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }

        registry.Clear();
    }

    private void StopTrackedRoutine(Object target, Dictionary<int, Coroutine> registry)
    {
        if (target == null)
        {
            return;
        }

        StopTrackedRoutine(target.GetInstanceID(), registry);
    }

    private void StopTrackedRoutine(int key, Dictionary<int, Coroutine> registry)
    {
        if (!registry.TryGetValue(key, out Coroutine routine))
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        registry.Remove(key);
    }

    private static bool TrySetAnchoredPosition(RectTransform target, Vector2 value)
    {
        if (target == null)
        {
            return false;
        }

        try
        {
            target.anchoredPosition = value;
            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static bool TrySetLocalScale(RectTransform target, Vector3 value)
    {
        if (target == null)
        {
            return false;
        }

        try
        {
            target.localScale = value;
            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static bool TrySetAlpha(CanvasGroup target, float value)
    {
        if (target == null)
        {
            return false;
        }

        try
        {
            target.alpha = value;
            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static bool TrySetColor(Image target, Color value)
    {
        if (target == null)
        {
            return false;
        }

        try
        {
            target.color = value;
            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static IEnumerator MoveAnchoredRoutine(RectTransform target, Vector2 end, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Vector2 start = target.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            yield return ExecutionPauseController.WaitWhilePaused();

            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            if (target == null)
            {
                yield break;
            }

            if (!TrySetAnchoredPosition(target, Vector2.LerpUnclamped(start, end, Ease(Mathf.Clamp01(elapsed / duration)))))
            {
                yield break;
            }

            yield return null;
        }

        if (target != null)
        {
            TrySetAnchoredPosition(target, end);
        }
    }

    private static IEnumerator ScaleRoutine(RectTransform target, Vector3 end, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 start = target.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            yield return ExecutionPauseController.WaitWhilePaused();

            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            if (target == null)
            {
                yield break;
            }

            if (!TrySetLocalScale(target, Vector3.LerpUnclamped(start, end, Ease(Mathf.Clamp01(elapsed / duration)))))
            {
                yield break;
            }

            yield return null;
        }

        if (target != null)
        {
            TrySetLocalScale(target, end);
        }
    }

    private static IEnumerator FadeRoutine(CanvasGroup target, float end, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        float start = target.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            yield return ExecutionPauseController.WaitWhilePaused();

            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            if (target == null)
            {
                yield break;
            }

            if (!TrySetAlpha(target, Mathf.LerpUnclamped(start, end, Ease(Mathf.Clamp01(elapsed / duration)))))
            {
                yield break;
            }

            yield return null;
        }

        if (target != null)
        {
            TrySetAlpha(target, end);
        }
    }

    private static IEnumerator ColorRoutine(Image target, Color end, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Color start = target.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            yield return ExecutionPauseController.WaitWhilePaused();

            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            if (target == null)
            {
                yield break;
            }

            if (!TrySetColor(target, Color.LerpUnclamped(start, end, Ease(Mathf.Clamp01(elapsed / duration)))))
            {
                yield break;
            }

            yield return null;
        }

        if (target != null)
        {
            TrySetColor(target, end);
        }
    }
}
