using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class SatellitePhotoPreviewController : MonoBehaviour
{
    private const string DefaultCanvasName = "Canvas";
    private const string PreviewPanelName = "PhotoPreviewPanel";
    private const string PreviewImageName = "PreviewImage";
    private const string FlashOverlayName = "PhotoFlashOverlay";

    [SerializeField] private RectTransform previewPanel;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private CanvasGroup previewCanvasGroup;
    [SerializeField] private Image previewBackground;
    [SerializeField] private Image flashOverlay;
    [SerializeField] private Vector2 panelSize = new Vector2(360f, 420f);
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(28f, 0f);
    [SerializeField] private Vector4 imagePadding = new Vector4(18f, 52f, 18f, 18f);
    [SerializeField] private Color panelBackgroundColor = new Color(0.97f, 0.97f, 0.95f, 0.985f);
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float flashPeakAlpha = 0.85f;
    [SerializeField, Min(0.01f)] private float flashFadeInDuration = 0.045f;
    [SerializeField, Min(0.01f)] private float flashFadeOutDuration = 0.18f;
    [SerializeField, Range(0.75f, 1f)] private float previewIntroStartScale = 0.92f;
    [SerializeField, Min(0.01f)] private float previewIntroDuration = 0.16f;
    [SerializeField, Range(0.5f, 1.5f)] private float photoSaturation = 0.93f;
    [SerializeField, Range(0.5f, 1.5f)] private float photoContrast = 1.08f;
    [SerializeField, Range(0f, 0.5f)] private float vignetteStrength = 0.18f;
    [SerializeField, Range(0f, 0.15f)] private float grainStrength = 0.025f;
    [SerializeField] private bool preserveExistingPanelLayout = true;
    [SerializeField] private bool preserveExistingImageLayout = true;

    private bool applyDefaultPanelLayout;
    private Texture2D generatedPreviewTexture;

    private void Awake()
    {
        EnsureBuilt();
        HidePreview(false);
    }

    public static SatellitePhotoPreviewController FindOrCreate()
    {
        SatellitePhotoPreviewController[] existingControllers = Object.FindObjectsByType<SatellitePhotoPreviewController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < existingControllers.Length; i++)
        {
            SatellitePhotoPreviewController controller = existingControllers[i];
            if (controller == null)
            {
                continue;
            }

            controller.EnsureBuilt();
            return controller;
        }

        Canvas rootCanvas = ResolveCanvas();
        if (rootCanvas == null)
        {
            return null;
        }

        GameObject panelObject = FindPanelObject(rootCanvas.transform);
        bool panelWasCreated = false;
        if (panelObject == null)
        {
            panelObject = new GameObject(
                PreviewPanelName,
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            panelObject.transform.SetParent(rootCanvas.transform, false);
            panelWasCreated = true;
        }

        SatellitePhotoPreviewController createdController =
            panelObject.GetComponent<SatellitePhotoPreviewController>();
        if (createdController == null)
        {
            createdController = panelObject.AddComponent<SatellitePhotoPreviewController>();
        }

        if (panelWasCreated)
        {
            createdController.MarkPanelAsAutoCreated();
        }

        createdController.EnsureBuilt();
        createdController.HidePreview(false);
        return createdController;
    }

    public IEnumerator ShowPreview(RenderTexture texture, float durationSeconds)
    {
        EnsureBuilt();
        if (previewPanel == null || previewImage == null || texture == null)
        {
            yield break;
        }

        ReplacePreviewTexture(BuildPhotoPreviewTexture(texture));

        Vector3 targetScale = previewPanel.localScale;
        Vector3 startScale = targetScale * previewIntroStartScale;

        previewCanvasGroup.alpha = 0f;
        previewPanel.localScale = startScale;
        SetVisible(true);

        yield return PlayFlash();
        yield return PlayPreviewIntro(startScale, targetScale);

        float elapsed = 0f;
        durationSeconds = Mathf.Max(0f, durationSeconds);
        while (elapsed < durationSeconds)
        {
            yield return ExecutionPauseController.WaitWhilePaused();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        HidePreview();
    }

    public void HidePreview(bool clearTexture = true)
    {
        EnsureBuilt();
        if (previewImage != null && clearTexture)
        {
            previewImage.texture = null;
        }

        if (clearTexture)
        {
            ReleaseGeneratedPreviewTexture();
        }

        SetFlashAlpha(0f);
        SetVisible(false);
    }

    private void EnsureBuilt()
    {
        if (previewPanel == null)
        {
            previewPanel = transform as RectTransform;
        }

        if (previewPanel == null)
        {
            return;
        }

        if (previewCanvasGroup == null)
        {
            previewCanvasGroup = GetComponent<CanvasGroup>();
            if (previewCanvasGroup == null)
            {
                previewCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (previewBackground == null)
        {
            previewBackground = GetComponent<Image>();
            if (previewBackground == null)
            {
                previewBackground = gameObject.AddComponent<Image>();
            }
        }

        previewBackground.color = panelBackgroundColor;
        previewBackground.raycastTarget = false;

        Shadow panelShadow = GetComponent<Shadow>();
        if (panelShadow == null)
        {
            panelShadow = gameObject.AddComponent<Shadow>();
        }

        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        panelShadow.effectDistance = new Vector2(0f, -12f);
        panelShadow.useGraphicAlpha = false;

        if (applyDefaultPanelLayout || !preserveExistingPanelLayout)
        {
            previewPanel.anchorMin = new Vector2(0f, 0.5f);
            previewPanel.anchorMax = new Vector2(0f, 0.5f);
            previewPanel.pivot = new Vector2(0f, 0.5f);
            previewPanel.sizeDelta = panelSize;
            previewPanel.anchoredPosition = panelAnchoredPosition;
            previewPanel.localScale = Vector3.one;
            previewPanel.localRotation = Quaternion.identity;
        }

        bool previewImageWasCreated = false;
        if (previewImage == null)
        {
            Transform existingPreviewImage = previewPanel.Find(PreviewImageName);
            if (existingPreviewImage != null)
            {
                previewImage = existingPreviewImage.GetComponent<RawImage>();
            }
        }

        if (previewImage == null)
        {
            GameObject previewImageObject = new GameObject(
                PreviewImageName,
                typeof(RectTransform),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            previewImageObject.transform.SetParent(previewPanel, false);
            previewImage = previewImageObject.GetComponent<RawImage>();
            previewImageWasCreated = true;
        }

        RectTransform previewImageRect = previewImage.rectTransform;
        if (previewImageWasCreated || !preserveExistingImageLayout)
        {
            previewImageRect.anchorMin = Vector2.zero;
            previewImageRect.anchorMax = Vector2.one;
            previewImageRect.pivot = new Vector2(0.5f, 0.5f);
            previewImageRect.offsetMin = new Vector2(imagePadding.x, imagePadding.y);
            previewImageRect.offsetMax = new Vector2(-imagePadding.z, -imagePadding.w);
            previewImageRect.localScale = Vector3.one;
            previewImageRect.localRotation = Quaternion.identity;
        }

        previewImage.color = Color.white;
        previewImage.raycastTarget = false;

        AspectRatioFitter aspectRatioFitter = previewImage.GetComponent<AspectRatioFitter>();
        if (aspectRatioFitter == null)
        {
            aspectRatioFitter = previewImage.gameObject.AddComponent<AspectRatioFitter>();
        }

        aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectRatioFitter.aspectRatio = 1f;

        EnsureFlashOverlay();
    }

    private void SetVisible(bool visible)
    {
        if (previewCanvasGroup == null)
        {
            return;
        }

        if (!visible)
        {
            previewCanvasGroup.alpha = 0f;
        }

        previewCanvasGroup.blocksRaycasts = visible;
        previewCanvasGroup.interactable = visible;
    }

    private void MarkPanelAsAutoCreated()
    {
        applyDefaultPanelLayout = true;
    }

    private void EnsureFlashOverlay()
    {
        if (flashOverlay == null)
        {
            Canvas rootCanvas = previewPanel != null ? previewPanel.GetComponentInParent<Canvas>() : null;
            if (rootCanvas == null)
            {
                rootCanvas = ResolveCanvas();
            }

            if (rootCanvas == null)
            {
                return;
            }

            Transform existingOverlay = rootCanvas.transform.Find(FlashOverlayName);
            if (existingOverlay != null)
            {
                flashOverlay = existingOverlay.GetComponent<Image>();
            }

            if (flashOverlay == null)
            {
                GameObject overlayObject = new GameObject(
                    FlashOverlayName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                overlayObject.transform.SetParent(rootCanvas.transform, false);
                flashOverlay = overlayObject.GetComponent<Image>();
            }
        }

        if (flashOverlay == null)
        {
            return;
        }

        RectTransform flashRect = flashOverlay.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.pivot = new Vector2(0.5f, 0.5f);
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flashRect.localScale = Vector3.one;
        flashRect.localRotation = Quaternion.identity;

        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        flashOverlay.raycastTarget = false;
        flashOverlay.transform.SetAsLastSibling();
    }

    private IEnumerator PlayFlash()
    {
        if (flashOverlay == null || flashPeakAlpha <= 0f)
        {
            yield break;
        }

        yield return FadeFlashAlpha(0f, flashPeakAlpha, flashFadeInDuration);
        yield return FadeFlashAlpha(flashPeakAlpha, 0f, flashFadeOutDuration);
    }

    private IEnumerator PlayPreviewIntro(Vector3 startScale, Vector3 targetScale)
    {
        if (previewCanvasGroup == null || previewPanel == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, previewIntroDuration);
        while (elapsed < duration)
        {
            yield return ExecutionPauseController.WaitWhilePaused();

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            previewCanvasGroup.alpha = eased;
            previewPanel.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }

        previewCanvasGroup.alpha = 1f;
        previewPanel.localScale = targetScale;
    }

    private IEnumerator FadeFlashAlpha(float from, float to, float duration)
    {
        if (flashOverlay == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return ExecutionPauseController.WaitWhilePaused();

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFlashAlpha(Mathf.LerpUnclamped(from, to, t));
            yield return null;
        }

        SetFlashAlpha(to);
    }

    private void SetFlashAlpha(float alpha)
    {
        if (flashOverlay == null)
        {
            return;
        }

        Color color = flashOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        flashOverlay.color = color;
    }

    private Texture2D BuildPhotoPreviewTexture(RenderTexture source)
    {
        if (source == null)
        {
            return null;
        }

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = source;

        Texture2D photoTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        photoTexture.name = "SatellitePhotoPreviewTexture";
        photoTexture.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0, false);
        photoTexture.Apply(false, false);
        RenderTexture.active = previousActive;

        if (!IsNearlyBlack(photoTexture))
        {
            ApplyPhotoLook(photoTexture);
        }

        return photoTexture;
    }

    private static bool IsNearlyBlack(Texture2D texture)
    {
        if (texture == null)
        {
            return true;
        }

        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 color = pixels[i];
            if (color.r > 3 || color.g > 3 || color.b > 3)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyPhotoLook(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;
        float maxDimension = Mathf.Max(1f, Mathf.Max(width, height));

        for (int y = 0; y < height; y++)
        {
            float ny = ((y + 0.5f) / height) * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color color = pixels[index];

                float luminance = color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
                color.r = Mathf.Lerp(luminance, color.r, photoSaturation);
                color.g = Mathf.Lerp(luminance, color.g, photoSaturation);
                color.b = Mathf.Lerp(luminance, color.b, photoSaturation);

                color.r = ((color.r - 0.5f) * photoContrast) + 0.5f;
                color.g = ((color.g - 0.5f) * photoContrast) + 0.5f;
                color.b = ((color.b - 0.5f) * photoContrast) + 0.5f;

                float nx = ((x + 0.5f) / width) * 2f - 1f;
                float radialDistance = Mathf.Sqrt(nx * nx + ny * ny);
                float vignette = 1f - vignetteStrength * Mathf.SmoothStep(0.25f, 1f, radialDistance);

                float grainNoise = Mathf.PerlinNoise(
                    (x + 13.37f) / (maxDimension * 0.12f),
                    (y + 57.19f) / (maxDimension * 0.12f));
                float grain = (grainNoise - 0.5f) * grainStrength;

                color.r = Mathf.Clamp01((color.r + grain) * vignette * 1.01f);
                color.g = Mathf.Clamp01((color.g + grain * 0.9f) * vignette);
                color.b = Mathf.Clamp01((color.b + grain * 1.15f) * vignette * 1.03f);

                pixels[index] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
    }

    private void ReplacePreviewTexture(Texture2D newTexture)
    {
        ReleaseGeneratedPreviewTexture();
        generatedPreviewTexture = newTexture;

        if (previewImage != null)
        {
            previewImage.texture = generatedPreviewTexture;
        }
    }

    private void ReleaseGeneratedPreviewTexture()
    {
        if (generatedPreviewTexture == null)
        {
            return;
        }

        Destroy(generatedPreviewTexture);
        generatedPreviewTexture = null;
    }

    private void OnDestroy()
    {
        ReleaseGeneratedPreviewTexture();
    }

    private static Canvas ResolveCanvas()
    {
        GameObject namedCanvas = GameObject.Find(DefaultCanvasName);
        if (namedCanvas != null && namedCanvas.TryGetComponent(out Canvas canvas))
        {
            return canvas;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas current = canvases[i];
            if (current == null)
            {
                continue;
            }

            if (current.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return current;
            }
        }

        GameObject createdCanvas = new GameObject(
            DefaultCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas createdRootCanvas = createdCanvas.GetComponent<Canvas>();
        createdRootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = createdCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return createdRootCanvas;
    }

    private static GameObject FindPanelObject(Transform canvasTransform)
    {
        if (canvasTransform == null)
        {
            return null;
        }

        Transform existingPanel = canvasTransform.Find(PreviewPanelName);
        return existingPanel != null ? existingPanel.gameObject : null;
    }
}
