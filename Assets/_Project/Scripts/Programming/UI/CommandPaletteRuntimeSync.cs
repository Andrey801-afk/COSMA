using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CommandPaletteRuntimeSync
{
    private const string CommandPoolContainerName = "CommandPoolItemsContainer";
    private const string CommandPoolViewportName = "CommandPoolScrollViewport";
    private const string CommandPoolScrollbarName = "CommandPoolScrollbar";
    private const string CategoryTabsName = "CommandCategoryTabs";

    private static readonly Color NormalTabColor = new(0.26f, 0.27f, 0.30f, 0.70f);
    private static readonly Color SelectedTabColor = new(0.34f, 0.46f, 0.52f, 0.92f);
    private static readonly Color NormalTextColor = new(0.84f, 0.87f, 0.90f, 1f);
    private static readonly Color SelectedTextColor = new(1f, 0.78f, 0.22f, 1f);

    private static readonly Dictionary<CommandType, CommandDefinition> RuntimeDefinitions = new();

    private static readonly TabSpec[] Tabs =
    {
        new(CommandPaletteCategory.All, "CommandCategoryTab_All", "ВСЕ"),
        new(CommandPaletteCategory.Systems, "CommandCategoryTab_Systems", "СИСТЕМЫ"),
        new(CommandPaletteCategory.Attitude, "CommandCategoryTab_Attitude", "ОРИЕНТ"),
        new(CommandPaletteCategory.Camera, "CommandCategoryTab_Camera", "КАМЕРА"),
        new(CommandPaletteCategory.Link, "CommandCategoryTab_Link", "СВЯЗЬ"),
        new(CommandPaletteCategory.Logic, "CommandCategoryTab_Logic", "ЛОГИКА")
    };

    private static readonly CommandDescriptor[] Catalog =
    {
        new(CommandType.PowerToggle, "Питание ВКЛ/ВЫКЛ", "Переключить питание спутника.", new Color(0.62f, 0.76f, 0.64f, 1f), CommandPaletteCategory.Systems),
        new(CommandType.ReadSunSensors, "Считать солнечные датчики", "Получить данные освещенности.", new Color(0.82f, 0.70f, 0.54f, 1f), CommandPaletteCategory.Systems),
        new(CommandType.ReadMagnetometer, "Считать магнитометр", "Получить данные магнитного поля.", new Color(0.56f, 0.70f, 0.76f, 1f), CommandPaletteCategory.Systems),
        new(CommandType.CalibrateGyroscopes, "Калибровать гироскопы", "Подготовить точную ориентацию спутника.", new Color(0.53f, 0.78f, 1f, 1f), CommandPaletteCategory.Systems),
        new(CommandType.ChargeBattery, "Зарядить батарею", "Запустить плавную зарядку до 100% на 15 секунд, если панели смотрят на Солнце.", new Color(1f, 0.78f, 0.22f, 1f), CommandPaletteCategory.Systems),
        new(CommandType.DestroyPlanet, "Уничтожить планету", "Навести спутник на Землю и выпустить 2-секундный неоновый импульс. Требуется заряд батареи 100%.", new Color(1f, 0.18f, 0.08f, 1f), CommandPaletteCategory.Systems),
        new(CommandType.RotateToEarth, "Повернуть к Земле", "Наводить спутник на Землю камерой или антенной стороной в течение 15 секунд.", new Color(0.35f, 0.62f, 1f, 1f), CommandPaletteCategory.Attitude),
        new(CommandType.RotateToSun, "Повернуть к Солнцу", "Наводить спутник и панели на Солнце в течение 15 секунд.", new Color(1f, 0.52f, 0.22f, 1f), CommandPaletteCategory.Attitude),
        new(CommandType.StabilizeSatellite, "Стабилизировать спутник", "Гасить вращение и удерживать текущую ориентацию спутника относительно Земли 15 секунд.", new Color(0.62f, 0.76f, 0.64f, 1f), CommandPaletteCategory.Attitude),
        new(CommandType.OpenCameraCover, "Открыть крышку камеры", "Подготовить оптический канал перед проверкой кадра и съемкой.", new Color(0.58f, 0.95f, 0.75f, 1f), CommandPaletteCategory.Camera),
        new(CommandType.CloseCameraCover, "Закрыть крышку камеры", "Защитить камеру после съемки.", new Color(0.62f, 0.67f, 0.74f, 1f), CommandPaletteCategory.Camera),
        new(CommandType.CheckEarthInFrame, "Проверить Землю в кадре", "Проверить, попадает ли Земля в поле камеры спутника.", new Color(0.56f, 0.70f, 0.76f, 1f), CommandPaletteCategory.Camera),
        new(CommandType.TakeEarthPhoto, "Сделать фото", "Снять кадр с камеры: Землю, звездное небо или черный экран при закрытой крышке.", new Color(0.25f, 1f, 0.78f, 1f), CommandPaletteCategory.Camera),
        new(CommandType.CompressPhoto, "Сжать снимок", "Подготовить снятые данные к передаче на Землю.", new Color(0.86f, 0.62f, 1f, 1f), CommandPaletteCategory.Camera),
        new(CommandType.RotateAntennaToEarth, "Повернуть антенну к Земле", "Навести антенну на Землю для передачи данных.", new Color(0.42f, 0.86f, 1f, 1f), CommandPaletteCategory.Link),
        new(CommandType.CheckCommunicationLink, "Проверить канал связи", "Проверить, проходит ли сигнал через антенну на Землю.", new Color(0.34f, 0.88f, 1f, 1f), CommandPaletteCategory.Link),
        new(CommandType.SendMessageToEarth, "Отправить сообщение", "Передать сигнал тонким лучом туда, куда сейчас направлена антенна.", new Color(0.30f, 0.92f, 1f, 1f), CommandPaletteCategory.Link),
        new(CommandType.Wait, "Ждать", "Подождать указанное количество секунд.", new Color(0.82f, 0.70f, 0.54f, 1f), CommandPaletteCategory.Logic),
        new(CommandType.JumpTo, "Перейти к строке", "Перейти к указанной строке.", new Color(0.70f, 0.62f, 0.76f, 1f), CommandPaletteCategory.Logic),
        new(CommandType.ConditionalJump, "Если условие -> строка", "Если условие истинно, перейти к указанной строке.", new Color(0.70f, 0.62f, 0.76f, 1f), CommandPaletteCategory.Logic, 1f, CommandConditionType.PowerOn)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SyncAfterSceneLoad()
    {
        SchedulePaletteSync();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHooks()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SchedulePaletteSync();
    }

    public static void SyncOpenPalettes()
    {
        GameObject container = GameObject.Find(CommandPoolContainerName);
        if (container != null)
        {
            EnsurePalette(container);
        }
    }

    private static void SchedulePaletteSync()
    {
        if (!Application.isPlaying)
        {
            SyncOpenPalettes();
            return;
        }

        CommandPaletteRuntimeSyncRunner.Schedule();
    }

    public static void EnsurePalette(GameObject containerObject)
    {
        if (containerObject == null)
        {
            return;
        }

        RectTransform viewport = EnsureScrollableContainer(containerObject);
        EnsurePanelLayout(containerObject, viewport);
        CommandPoolItemView templateView = containerObject.GetComponentInChildren<CommandPoolItemView>(true);
        if (templateView == null)
        {
            return;
        }

        Dictionary<CommandType, CommandPoolItemView> existingViews = CollectExistingViews(containerObject);
        int siblingIndex = 0;
        for (int i = 0; i < Catalog.Length; i++)
        {
            CommandDescriptor descriptor = Catalog[i];
            CommandDefinition definition = GetRuntimeDefinition(descriptor);
            CommandPoolItemView view;

            if (!existingViews.TryGetValue(descriptor.Type, out view) || view == null)
            {
                GameObject item = Object.Instantiate(templateView.gameObject, containerObject.transform, false);
                item.name = $"CommandPoolItem_{descriptor.Type}";
                view = item.GetComponent<CommandPoolItemView>();
                existingViews[descriptor.Type] = view;
            }

            view.SetDefinition(definition);
            EnsureItemLayout(view);
            view.gameObject.name = $"CommandPoolItem_{descriptor.Type}";
            view.gameObject.SetActive(true);
            view.transform.SetSiblingIndex(siblingIndex++);
        }

        Dictionary<CommandPaletteCategory, Button> tabButtons = EnsureCategoryTabs(containerObject, viewport);
        EnsurePanelLayout(containerObject, viewport);
        ApplyCategory(containerObject, tabButtons, CommandPaletteCategory.All);
    }

    private static Dictionary<CommandType, CommandPoolItemView> CollectExistingViews(GameObject containerObject)
    {
        Dictionary<CommandType, CommandPoolItemView> byType = new();
        CommandPoolItemView[] views = containerObject.GetComponentsInChildren<CommandPoolItemView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            CommandPoolItemView view = views[i];
            if (view == null || view.Definition == null)
            {
                continue;
            }

            CommandType type = view.Definition.Type;
            if (byType.ContainsKey(type))
            {
                view.SetDefinition(null);
                view.gameObject.SetActive(false);
                continue;
            }

            byType.Add(type, view);
        }

        return byType;
    }

    private static RectTransform EnsureScrollableContainer(GameObject containerObject)
    {
        RectTransform content = containerObject.transform as RectTransform;
        RectTransform oldParent = content != null ? content.parent as RectTransform : null;
        if (content == null || oldParent == null)
        {
            return null;
        }

        if (oldParent.name == CommandPoolViewportName)
        {
            EnsureViewportComponents(oldParent, content);
            EnsureContentLayout(content);
            return oldParent;
        }

        int siblingIndex = content.GetSiblingIndex();
        GameObject viewportObject = new(CommandPoolViewportName, typeof(RectTransform));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(oldParent, false);
        viewport.SetSiblingIndex(siblingIndex);

        LayoutElement oldLayout = containerObject.GetComponent<LayoutElement>();
        LayoutElement viewportLayout = viewportObject.AddComponent<LayoutElement>();
        if (oldLayout != null)
        {
            viewportLayout.minWidth = oldLayout.minWidth;
            viewportLayout.minHeight = Mathf.Max(160f, oldLayout.minHeight);
            viewportLayout.preferredWidth = oldLayout.preferredWidth;
            viewportLayout.preferredHeight = oldLayout.preferredHeight;
            viewportLayout.flexibleWidth = oldLayout.flexibleWidth;
            viewportLayout.flexibleHeight = Mathf.Max(1f, oldLayout.flexibleHeight);
            Object.Destroy(oldLayout);
        }
        else
        {
            viewportLayout.minHeight = 160f;
            viewportLayout.flexibleHeight = 1f;
        }

        content.SetParent(viewport, false);
        EnsureViewportComponents(viewport, content);
        EnsureContentLayout(content);
        return viewport;
    }

    private static void EnsureViewportComponents(RectTransform viewport, RectTransform content)
    {
        LayoutElement viewportElement = GetOrAdd<LayoutElement>(viewport.gameObject);
        viewportElement.minHeight = 160f;
        viewportElement.flexibleHeight = 1f;

        Image image = GetOrAdd<Image>(viewport.gameObject);
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        GetOrAdd<RectMask2D>(viewport.gameObject);

        ScrollRect scrollRect = GetOrAdd<ScrollRect>(viewport.gameObject);
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.elasticity = 0.08f;
        scrollRect.scrollSensitivity = 0f;
        scrollRect.verticalScrollbar = null;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        HideScrollbar(viewport);

        CommandPaletteContentSizer contentSizer = GetOrAdd<CommandPaletteContentSizer>(viewport.gameObject);
        contentSizer.Configure(viewport, content);

        CommandPaletteSmoothScrollWheel smoothWheel = GetOrAdd<CommandPaletteSmoothScrollWheel>(viewport.gameObject);
        smoothWheel.Configure(scrollRect, true);
    }

    private static void EnsureContentLayout(RectTransform content)
    {
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void EnsurePanelLayout(GameObject containerObject, RectTransform viewport)
    {
        Transform panel = viewport != null && viewport.parent != null
            ? viewport.parent
            : containerObject.transform.parent;

        if (panel == null)
        {
            return;
        }

        if (panel.parent != null && panel.parent.TryGetComponent(out HorizontalLayoutGroup rootLayout))
        {
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = true;
        }

        VerticalLayoutGroup panelLayout = panel.GetComponent<VerticalLayoutGroup>();
        if (panelLayout != null)
        {
            panelLayout.padding = new RectOffset(12, 12, 8, 12);
            panelLayout.spacing = 6f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
        }

        EnsureIgnoredLayout(panel.Find("FrostedHighlight"));

        Transform header = panel.Find("Header");
        if (header != null)
        {
            LayoutElement headerElement = GetOrAdd<LayoutElement>(header.gameObject);
            headerElement.minHeight = 34f;
            headerElement.preferredHeight = 34f;
            headerElement.flexibleHeight = 0f;
        }

        if (viewport != null)
        {
            LayoutElement viewportElement = GetOrAdd<LayoutElement>(viewport.gameObject);
            viewportElement.minHeight = 160f;
            viewportElement.preferredHeight = -1f;
            viewportElement.flexibleHeight = 1f;
        }
    }

    private static void EnsureIgnoredLayout(Transform transform)
    {
        if (transform == null)
        {
            return;
        }

        LayoutElement element = GetOrAdd<LayoutElement>(transform.gameObject);
        element.ignoreLayout = true;
    }

    private static void EnsureItemLayout(CommandPoolItemView view)
    {
        if (view == null)
        {
            return;
        }

        LayoutElement element = GetOrAdd<LayoutElement>(view.gameObject);
        element.minHeight = 48f;
        element.preferredHeight = 48f;
        element.flexibleHeight = 0f;
    }

    private static Scrollbar EnsureScrollbar(RectTransform viewport)
    {
        Transform existing = viewport.Find(CommandPoolScrollbarName);
        GameObject root = existing != null ? existing.gameObject : new GameObject(CommandPoolScrollbarName, typeof(RectTransform));
        if (existing == null)
        {
            root.transform.SetParent(viewport, false);
        }

        SetStretch(root, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-7f, 4f), new Vector2(-1f, -4f));
        Image background = GetOrAdd<Image>(root);
        background.color = new Color(0.08f, 0.09f, 0.10f, 0.48f);

        Scrollbar scrollbar = GetOrAdd<Scrollbar>(root);
        scrollbar.direction = Scrollbar.Direction.TopToBottom;

        Transform sliding = root.transform.Find("Sliding Area");
        GameObject slidingArea = sliding != null ? sliding.gameObject : new GameObject("Sliding Area", typeof(RectTransform));
        if (sliding == null)
        {
            slidingArea.transform.SetParent(root.transform, false);
        }

        SetStretch(slidingArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Transform existingHandle = slidingArea.transform.Find("Handle");
        GameObject handle = existingHandle != null ? existingHandle.gameObject : new GameObject("Handle", typeof(RectTransform));
        if (existingHandle == null)
        {
            handle.transform.SetParent(slidingArea.transform, false);
        }

        SetStretch(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image handleImage = GetOrAdd<Image>(handle);
        handleImage.color = new Color(0.82f, 0.70f, 0.54f, 0.82f);

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = (RectTransform)handle.transform;
        return scrollbar;
    }

    private static void HideScrollbar(RectTransform viewport)
    {
        if (viewport == null)
        {
            return;
        }

        Transform existing = viewport.Find(CommandPoolScrollbarName);
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }

    private static Dictionary<CommandPaletteCategory, Button> EnsureCategoryTabs(GameObject containerObject, RectTransform viewport)
    {
        Dictionary<CommandPaletteCategory, Button> buttons = new();
        Transform panel = viewport != null && viewport.parent != null
            ? viewport.parent
            : containerObject.transform.parent;

        if (panel == null)
        {
            return buttons;
        }

        Transform existing = panel.Find(CategoryTabsName);
        GameObject tabsRoot = existing != null ? existing.gameObject : new GameObject(CategoryTabsName, typeof(RectTransform));
        if (existing == null)
        {
            tabsRoot.transform.SetParent(panel, false);
        }

        LayoutElement element = GetOrAdd<LayoutElement>(tabsRoot);
        element.minHeight = 52f;
        element.preferredHeight = 52f;
        element.flexibleHeight = 0f;

        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(tabsRoot);
        grid.cellSize = new Vector2(67f, 24f);
        grid.spacing = new Vector2(4f, 4f);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        int headerIndex = panel.Find("Header") != null ? panel.Find("Header").GetSiblingIndex() : -1;
        tabsRoot.transform.SetSiblingIndex(Mathf.Max(0, headerIndex + 1));

        for (int i = 0; i < Tabs.Length; i++)
        {
            TabSpec spec = Tabs[i];
            Button button = EnsureCategoryButton(tabsRoot.transform, spec);
            CommandPaletteCategory capturedCategory = spec.Category;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ApplyCategory(containerObject, buttons, capturedCategory));
            buttons[spec.Category] = button;
        }

        return buttons;
    }

    private static Button EnsureCategoryButton(Transform parent, TabSpec spec)
    {
        Transform existing = parent.Find(spec.ObjectName);
        GameObject root = existing != null ? existing.gameObject : new GameObject(spec.ObjectName, typeof(RectTransform));
        if (existing == null)
        {
            root.transform.SetParent(parent, false);
        }

        Image image = GetOrAdd<Image>(root);
        image.color = NormalTabColor;

        Button button = GetOrAdd<Button>(root);
        button.targetGraphic = image;

        Transform textTransform = root.transform.Find("Text");
        GameObject textObject = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform));
        if (textTransform == null)
        {
            textObject.transform.SetParent(root.transform, false);
        }

        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(textObject);
        text.text = spec.Label;
        text.fontSize = 9.5f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = NormalTextColor;
        text.raycastTarget = false;
        SetStretch(textObject, Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(-2f, 0f));

        return button;
    }

    private static void ApplyCategory(
        GameObject containerObject,
        Dictionary<CommandPaletteCategory, Button> tabButtons,
        CommandPaletteCategory activeCategory)
    {
        CommandPoolItemView[] views = containerObject.GetComponentsInChildren<CommandPoolItemView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            CommandPoolItemView view = views[i];
            if (view == null || view.Definition == null)
            {
                continue;
            }

            EnsureItemLayout(view);
            view.gameObject.SetActive(ShouldShow(view.Definition.Type, activeCategory));
        }

        foreach (KeyValuePair<CommandPaletteCategory, Button> pair in tabButtons)
        {
            bool selected = pair.Key == activeCategory;
            Image image = pair.Value.targetGraphic as Image;
            if (image == null)
            {
                image = pair.Value.GetComponent<Image>();
            }

            if (image != null)
            {
                image.color = selected ? SelectedTabColor : NormalTabColor;
            }

            TMP_Text text = pair.Value.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.color = selected ? SelectedTextColor : NormalTextColor;
            }
        }

        RectTransform content = containerObject.transform as RectTransform;
        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            if (content.parent is RectTransform viewport)
            {
                CommandPaletteContentSizer contentSizer = viewport.GetComponent<CommandPaletteContentSizer>();
                if (contentSizer != null)
                {
                    contentSizer.Resize();
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
                if (viewport.parent is RectTransform panel)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
                }
            }
        }
    }

    private static bool ShouldShow(CommandType type, CommandPaletteCategory category)
    {
        if (category == CommandPaletteCategory.All)
        {
            return true;
        }

        for (int i = 0; i < Catalog.Length; i++)
        {
            if (Catalog[i].Type == type)
            {
                return Catalog[i].Category == category;
            }
        }

        return false;
    }

    private static CommandDefinition GetRuntimeDefinition(CommandDescriptor descriptor)
    {
        if (RuntimeDefinitions.TryGetValue(descriptor.Type, out CommandDefinition definition) && definition != null)
        {
            return definition;
        }

        definition = ScriptableObject.CreateInstance<CommandDefinition>();
        definition.name = $"RuntimeCommandDefinition_{descriptor.Type}";
        definition.hideFlags = HideFlags.DontSave;
        definition.Type = descriptor.Type;
        definition.DisplayName = descriptor.DisplayName;
        definition.Description = descriptor.Description;
        definition.AccentColor = descriptor.AccentColor;
        definition.DefaultTargetLine = 1;
        definition.DefaultWaitSeconds = descriptor.DefaultWaitSeconds;
        definition.DefaultCondition = descriptor.DefaultCondition;
        RuntimeDefinitions[descriptor.Type] = definition;
        return definition;
    }

    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static void SetStretch(GameObject root, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private enum CommandPaletteCategory
    {
        All,
        Systems,
        Attitude,
        Camera,
        Link,
        Logic
    }

    private readonly struct TabSpec
    {
        public readonly CommandPaletteCategory Category;
        public readonly string ObjectName;
        public readonly string Label;

        public TabSpec(CommandPaletteCategory category, string objectName, string label)
        {
            Category = category;
            ObjectName = objectName;
            Label = label;
        }
    }

    private readonly struct CommandDescriptor
    {
        public readonly CommandType Type;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly Color AccentColor;
        public readonly CommandPaletteCategory Category;
        public readonly float DefaultWaitSeconds;
        public readonly CommandConditionType DefaultCondition;

        public CommandDescriptor(
            CommandType type,
            string displayName,
            string description,
            Color accentColor,
            CommandPaletteCategory category,
            float defaultWaitSeconds = 1f,
            CommandConditionType defaultCondition = CommandConditionType.PowerOn)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
            AccentColor = accentColor;
            Category = category;
            DefaultWaitSeconds = defaultWaitSeconds;
            DefaultCondition = defaultCondition;
        }
    }
}
