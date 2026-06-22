#if COSMA_USE_DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExecutionPauseController
{
    private sealed class LegacyAnimationStateSnapshot
    {
        public string StateName;
        public float Speed;
    }

    private sealed class LegacyAnimationSnapshot
    {
        public Animation Animation;
        public readonly List<LegacyAnimationStateSnapshot> States = new();
    }

    private static bool isPaused;
    private static float cachedTimeScale = 1f;
    private static readonly Dictionary<Animator, float> pausedAnimators = new();
    private static readonly List<LegacyAnimationSnapshot> pausedLegacyAnimations = new();

    public static bool IsPaused => isPaused;
    public static event Action<bool> PauseStateChanged;

    public static void PauseExecution()
    {
        if (isPaused)
        {
            return;
        }

        if (Time.timeScale > 0.0001f)
        {
            cachedTimeScale = Time.timeScale;
        }

        isPaused = true;
        Time.timeScale = 0f;
        CaptureAndPauseAnimators();
        CaptureAndPauseLegacyAnimations();
#if COSMA_USE_DOTWEEN
        DOTween.PauseAll();
#endif
        PauseStateChanged?.Invoke(true);
    }

    public static void ResumeExecution()
    {
        if (!isPaused)
        {
            if (Time.timeScale <= 0f)
            {
                Time.timeScale = cachedTimeScale > 0.0001f ? cachedTimeScale : 1f;
            }

            return;
        }

        isPaused = false;
        Time.timeScale = cachedTimeScale > 0.0001f ? cachedTimeScale : 1f;
        RestoreLegacyAnimations();
        RestoreAnimators();
#if COSMA_USE_DOTWEEN
        DOTween.PlayAll();
#endif
        PauseStateChanged?.Invoke(false);
    }

    public static void ResetExecutionPauseState()
    {
        bool wasPaused = isPaused;
        isPaused = false;
        Time.timeScale = cachedTimeScale > 0.0001f ? cachedTimeScale : 1f;
        RestoreLegacyAnimations();
        RestoreAnimators();
#if COSMA_USE_DOTWEEN
        DOTween.PlayAll();
#endif
        if (wasPaused)
        {
            PauseStateChanged?.Invoke(false);
        }
    }

    public static IEnumerator WaitWhilePaused()
    {
        while (isPaused)
        {
            yield return null;
        }
    }

    private static void CaptureAndPauseAnimators()
    {
        pausedAnimators.Clear();

        Animator[] animators = UnityEngine.Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
            {
                continue;
            }

            pausedAnimators[animator] = animator.speed;
            animator.speed = 0f;
        }
    }

    private static void RestoreAnimators()
    {
        foreach (KeyValuePair<Animator, float> entry in pausedAnimators)
        {
            if (entry.Key == null)
            {
                continue;
            }

            entry.Key.speed = entry.Value;
        }

        pausedAnimators.Clear();
    }

    private static void CaptureAndPauseLegacyAnimations()
    {
        pausedLegacyAnimations.Clear();

        Animation[] animations = UnityEngine.Object.FindObjectsByType<Animation>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < animations.Length; i++)
        {
            Animation animation = animations[i];
            if (animation == null)
            {
                continue;
            }

            LegacyAnimationSnapshot snapshot = null;
            foreach (AnimationState state in animation)
            {
                if (state == null)
                {
                    continue;
                }

                snapshot ??= new LegacyAnimationSnapshot
                {
                    Animation = animation
                };

                snapshot.States.Add(new LegacyAnimationStateSnapshot
                {
                    StateName = state.name,
                    Speed = state.speed
                });

                state.speed = 0f;
            }

            if (snapshot != null && snapshot.States.Count > 0)
            {
                pausedLegacyAnimations.Add(snapshot);
            }
        }
    }

    private static void RestoreLegacyAnimations()
    {
        for (int i = 0; i < pausedLegacyAnimations.Count; i++)
        {
            LegacyAnimationSnapshot snapshot = pausedLegacyAnimations[i];
            if (snapshot == null || snapshot.Animation == null)
            {
                continue;
            }

            for (int j = 0; j < snapshot.States.Count; j++)
            {
                LegacyAnimationStateSnapshot stateSnapshot = snapshot.States[j];
                if (stateSnapshot == null || string.IsNullOrEmpty(stateSnapshot.StateName))
                {
                    continue;
                }

                AnimationState state = snapshot.Animation[stateSnapshot.StateName];
                if (state != null)
                {
                    state.speed = stateSnapshot.Speed;
                }
            }
        }

        pausedLegacyAnimations.Clear();
    }
}
