using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CommandHelpTooltip
{
    public const float DelaySeconds = 2f;
    public const float PointerMoveThreshold = 3f;

    private const float TooltipWidth = 360f;
    private const float TooltipMargin = 14f;
    private const float TooltipOffsetX = 18f;
    private const float TooltipOffsetY = -10f;

    private static Object activeOwner;
    private static GameObject tooltipRoot;
    private static RectTransform tooltipRect;

    public static bool Show(Object owner, Canvas rootCanvas, CommandDefinition definition, Vector2 pointerPosition)
    {
        if (owner == null || rootCanvas == null || definition == null)
        {
            return false;
        }

        HideAll();
        activeOwner = owner;

        tooltipRoot = new GameObject("CommandHelpTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        tooltipRoot.transform.SetParent(rootCanvas.transform, false);
        tooltipRoot.transform.SetAsLastSibling();

        tooltipRect = (RectTransform)tooltipRoot.transform;
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 1f);
        tooltipRect.sizeDelta = new Vector2(TooltipWidth, 0f);

        Image background = tooltipRoot.GetComponent<Image>();
        background.color = new Color(0.045f, 0.055f, 0.065f, 0.97f);
        background.raycastTarget = false;

        Outline outline = tooltipRoot.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
        outline.effectDistance = new Vector2(1f, -1f);

        CanvasGroup group = tooltipRoot.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;
        group.interactable = false;

        VerticalLayoutGroup layout = tooltipRoot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = tooltipRoot.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = CreateTooltipText(
            tooltipRoot.transform,
            "Title",
            definition.DisplayName,
            15f,
            definition.AccentColor,
            FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI body = CreateTooltipText(
            tooltipRoot.transform,
            "Body",
            BuildText(definition),
            12.5f,
            new Color(0.88f, 0.91f, 0.94f, 1f),
            FontStyles.Normal);
        body.lineSpacing = 6f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        float preferredHeight = Mathf.Max(96f, LayoutUtility.GetPreferredHeight(tooltipRect));
        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TooltipWidth);
        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        Position(rootCanvas, pointerPosition);
        return true;
    }

    public static void Hide(Object owner)
    {
        if (activeOwner != null && activeOwner != owner)
        {
            return;
        }

        HideAll();
    }

    public static void HideAll()
    {
        activeOwner = null;
        tooltipRect = null;

        if (tooltipRoot == null)
        {
            return;
        }

        GameObject root = tooltipRoot;
        tooltipRoot = null;
        if (Application.isPlaying)
        {
            Object.Destroy(root);
        }
        else
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void Position(Canvas rootCanvas, Vector2 pointerPosition)
    {
        if (tooltipRect == null || rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                pointerPosition,
                canvasCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 position = localPoint + new Vector2(TooltipOffsetX, TooltipOffsetY);
        Vector2 size = tooltipRect.rect.size;
        Rect canvasBounds = canvasRect.rect;

        float minX = canvasBounds.xMin + TooltipMargin;
        float maxX = canvasBounds.xMax - size.x - TooltipMargin;
        float minY = canvasBounds.yMin + size.y + TooltipMargin;
        float maxY = canvasBounds.yMax - TooltipMargin;
        position.x = maxX >= minX ? Mathf.Clamp(position.x, minX, maxX) : minX;
        position.y = maxY >= minY ? Mathf.Clamp(position.y, minY, maxY) : maxY;

        tooltipRect.anchoredPosition = position;
    }

    private static TextMeshProUGUI CreateTooltipText(
        Transform parent,
        string objectName,
        string text,
        float fontSize,
        Color color,
        FontStyles style)
    {
        GameObject textObject = new(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;

        LayoutElement element = textObject.AddComponent<LayoutElement>();
        element.preferredWidth = TooltipWidth - 28f;
        return label;
    }

    private static string BuildText(CommandDefinition command)
    {
        if (command == null)
        {
            return string.Empty;
        }

        return command.Type switch
        {
            CommandType.PowerToggle =>
                "Для чего: запустить или отключить электронику спутника.\nЧто делает: переключает питание. При выключении сбрасывает подтверждение связи и проверку кадра.\nУсловия: для включения батарея должна быть выше 0%.",

            CommandType.ReadSunSensors =>
                "Для чего: понять, где находится Солнце.\nЧто делает: считывает солнечные датчики и сохраняет данные для поворота к Солнцу.\nУсловия: питание включено, расход батареи 2%.",

            CommandType.ReadMagnetometer =>
                "Для чего: найти направление на Землю.\nЧто делает: считывает магнитометр и сохраняет данные для поворота к Земле или антенны.\nУсловия: питание включено, расход батареи 2%.",

            CommandType.CalibrateGyroscopes =>
                "Для чего: подготовить точное удержание ориентации.\nЧто делает: калибрует гироскопы и разрешает стабилизацию.\nУсловия: питание включено, расход батареи 3%.",

            CommandType.ChargeBattery =>
                "Для чего: восстановить заряд перед дорогими командами.\nЧто делает: через 1 секунду программа идет дальше, а батарея плавно заряжается до 100% в течение 15 секунд.\nУсловия: питание включено, спутник смотрит к Солнцу.",

            CommandType.DestroyPlanet =>
                "Для чего: специальная финальная команда в стиле Звездных войн.\nЧто делает: наводится на Землю, стреляет ярким неоновым лучом 2 секунды и запускает взрыв.\nУсловия: питание включено, батарея 100%, цель Земли доступна.",

            CommandType.RotateToEarth =>
                "Для чего: направить камеру или антенную сторону к Земле.\nЧто делает: через 1 секунду программа идет дальше, а ориентация удерживается 15 секунд.\nУсловия: питание включено, магнитометр уже считал Землю, расход батареи 8%.",

            CommandType.RotateToSun =>
                "Для чего: направить спутник и панели к Солнцу.\nЧто делает: через 1 секунду программа идет дальше, а наведение удерживается 15 секунд.\nУсловия: питание включено, солнечные датчики уже нашли Солнце, расход батареи 8%.",

            CommandType.StabilizeSatellite =>
                "Для чего: остановить вращение относительно положения к Земле.\nЧто делает: через 1 секунду программа идет дальше, а текущая ориентация удерживается 15 секунд.\nУсловия: питание включено, гироскопы откалиброваны, расход батареи 5%.",

            CommandType.OpenCameraCover =>
                "Для чего: подготовить камеру к проверке кадра и съемке.\nЧто делает: открывает крышку камеры.\nУсловия: питание включено, расход батареи 3%.",

            CommandType.CloseCameraCover =>
                "Для чего: защитить камеру после съемки.\nЧто делает: закрывает крышку и сбрасывает признак Земли в кадре.\nУсловия: питание включено.",

            CommandType.CheckEarthInFrame =>
                "Для чего: убедиться, что камера действительно смотрит на Землю.\nЧто делает: проверяет кадр. Если крышка закрыта, результатом считается черный кадр.\nУсловия: питание включено.",

            CommandType.TakeEarthPhoto =>
                "Для чего: сделать снимок с камеры спутника.\nЧто делает: фотографирует Землю, горизонт, звездное небо или черный экран при закрытой крышке.\nУсловия: питание включено, расход батареи 12%.",

            CommandType.CompressPhoto =>
                "Для чего: подготовить снимок к передаче.\nЧто делает: сжимает уже сделанное фото и помечает данные готовыми к отправке.\nУсловия: питание включено, снимок уже сделан, расход батареи 3%.",

            CommandType.RotateAntennaToEarth =>
                "Для чего: направить связь на Землю.\nЧто делает: поворачивает антенну к Земле для передачи данных.\nУсловия: питание включено, магнитометр уже считал Землю, расход батареи 8%.",

            CommandType.CheckCommunicationLink =>
                "Для чего: проверить, есть ли рабочий канал связи.\nЧто делает: подтверждает, что антенна наведена на Землю и сигнал может пройти.\nУсловия: питание включено, расход батареи 2%.",

            CommandType.SendMessageToEarth =>
                "Для чего: отправить сообщение или подготовленные данные на Землю.\nЧто делает: выпускает луч связи из антенны и помечает данные отправленными.\nУсловия: питание включено, канал связи подтвержден, фото перед отправкой должно быть сжато, расход батареи 10%.",

            CommandType.Wait =>
                "Для чего: сделать паузу между действиями.\nЧто делает: ждет указанное количество секунд, затем переходит к следующей строке.\nУсловия: специальных условий нет.",

            CommandType.JumpTo =>
                "Для чего: повторять часть программы или менять порядок выполнения.\nЧто делает: переносит выполнение на указанную строку.\nУсловия: номер строки должен быть внутри программы.",

            CommandType.ConditionalJump =>
                "Для чего: сделать ветвление по состоянию спутника.\nЧто делает: проверяет выбранное условие и переходит к строке только если оно истинно.\nУсловия: зависят от выбранного условия IF.",

            _ =>
                string.IsNullOrWhiteSpace(command.Description)
                    ? "Справка для этой команды пока не заполнена."
                    : command.Description
        };
    }
}
