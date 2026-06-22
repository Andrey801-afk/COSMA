using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unified.UniversalBlur.Runtime;

public static class CosmaProgrammingSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string CommandsFolder = "Assets/_Project/Commands";
    private const string MissionDefinitionsFolder = "Assets/_Project/MissionDefinitions";
    private const string PrefabsFolder = "Assets/_Project/Prefabs";
    private const string PoolItemPrefabPath = PrefabsFolder + "/CommandPoolItem.prefab";
    private const string ProgramLinePrefabPath = PrefabsFolder + "/ProgramLine.prefab";
    private const string ProgramCommandPrefabPath = PrefabsFolder + "/ProgramCommand.prefab";
    private const string DragGhostPrefabPath = PrefabsFolder + "/DragGhost.prefab";
    private const string SoftPanelSpritePath = "Assets/_Project/Textures/UI_SoftPanel.png";
    private const string SoftPillSpritePath = "Assets/_Project/Textures/UI_SoftPill.png";
    private const string BlurMaterialPath = "Assets/_Project/Unified-Universal-Blur-main/Materials/UniversalBlurUI.mat";

    private static readonly Color PanelColor = new(0.19f, 0.20f, 0.22f, 0.52f);
    private static readonly Color PanelColorStrong = new(0.16f, 0.17f, 0.19f, 0.64f);
    private static readonly Color LineColor = new(0.24f, 0.25f, 0.27f, 0.46f);
    private static readonly Color TextColor = new(0.88f, 0.90f, 0.92f, 1f);
    private static readonly Color MutedTextColor = new(0.66f, 0.69f, 0.72f, 1f);
    private static readonly Color Cyan = new(0.56f, 0.70f, 0.76f, 1f);
    private static readonly Color Amber = new(0.82f, 0.70f, 0.54f, 1f);
    private static readonly Color Green = new(0.62f, 0.76f, 0.64f, 1f);
    private static readonly Color Magenta = new(0.70f, 0.62f, 0.76f, 1f);
    private static Sprite softPanelSprite;
    private static Sprite softPillSprite;
    private static Material blurMaterial;

    [MenuItem("COSMA/Rebuild Programming UI")]
    public static void BuildSampleScene()
    {
        Directory.CreateDirectory(CommandsFolder);
        Directory.CreateDirectory(MissionDefinitionsFolder);
        Directory.CreateDirectory(PrefabsFolder);
        Directory.CreateDirectory("Assets/_Project/Textures");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureSoftSprites();
        EnsureUniversalBlurFeature();
        blurMaterial = AssetDatabase.LoadAssetAtPath<Material>(BlurMaterialPath);
        CommandDefinition[] definitions = EnsureCommandDefinitions();
        MissionDefinition[] missionDefinitions = EnsureMissionDefinitions(definitions);
        MissionDefinition activeMissionDefinition = missionDefinitions.Length > 0 ? missionDefinitions[0] : null;
        CommandDefinition[] commandPoolDefinitions = ResolveMissionCommandPool(activeMissionDefinition, definitions);
        int maxProgramLines = ResolveMissionLineCount(activeMissionDefinition);

        GameObject dragGhostPrefabObject = EnsureDragGhostPrefab();
        GameObject programCommandPrefabObject = EnsureProgramCommandPrefab();
        GameObject programLinePrefabObject = EnsureProgramLinePrefab();
        GameObject poolItemPrefabObject = EnsurePoolItemPrefab(dragGhostPrefabObject.GetComponent<DragGhostView>());

        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();
        UIAnimationDriver animationDriver = EnsureComponent<UIAnimationDriver>(canvas.gameObject);

        RemoveTargetCanvasChildren(canvas.transform);
        MoveLegacyCanvasChildren(canvas.transform);

        GameObject missionPanel = CreatePanel(canvas.transform, "MissionPanel", PanelColorStrong);
        SetFixed(missionPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(430f, 360f));

        GameObject rightRoot = CreateUiObject("RightProgrammingRoot", canvas.transform);
        SetStretch(rightRoot, new Vector2(0.62f, 0.12f), new Vector2(0.985f, 0.94f), Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup rightLayout = rightRoot.AddComponent<HorizontalLayoutGroup>();
        rightLayout.spacing = 12f;
        rightLayout.childAlignment = TextAnchor.UpperCenter;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = true;

        GameObject bottomLeftPanel = CreatePanel(canvas.transform, "BottomLeftControlsPanel", new Color(0.025f, 0.038f, 0.052f, 0.72f));
        bottomLeftPanel.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.21f, 0.46f);
        SetFixed(bottomLeftPanel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(320f, 74f));

        GameObject bottomRightPanel = CreatePanel(canvas.transform, "BottomRightActionsPanel", PanelColorStrong);
        SetFixed(bottomRightPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(480f, 74f));

        GameObject messagePanel = CreatePanel(canvas.transform, "MessagePanel", new Color(0.055f, 0.052f, 0.035f, 0.88f));
        messagePanel.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.19f, 0.54f);
        SetFixed(messagePanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(540f, 74f));

        GameObject popupLayer = CreateUiObject("PopupLayer", canvas.transform);
        SetStretch(popupLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CanvasGroup popupGroup = popupLayer.AddComponent<CanvasGroup>();
        popupGroup.alpha = 1f;
        popupGroup.blocksRaycasts = false;
        popupGroup.interactable = false;

        GameObject dragGhostLayer = CreateUiObject("DragGhostLayer", canvas.transform);
        SetStretch(dragGhostLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CanvasGroup ghostGroup = dragGhostLayer.AddComponent<CanvasGroup>();
        ghostGroup.alpha = 1f;
        ghostGroup.blocksRaycasts = false;
        ghostGroup.interactable = false;

        TMP_Text messageText = BuildMessagePanel(messagePanel.transform);
        MissionSystem missionSystem;
        SatelliteStateController satelliteState = BuildMissionPanel(missionPanel.transform, messageText, activeMissionDefinition, out missionSystem);

        Button runButton, pauseButton;
        BuildBottomControlsPanel(bottomLeftPanel.transform, animationDriver, out runButton, out pauseButton);

        GameObject commandPoolPanel = BuildCommandPoolPanel(rightRoot.transform, commandPoolDefinitions, poolItemPrefabObject, dragGhostPrefabObject.GetComponent<DragGhostView>(), canvas, (RectTransform)dragGhostLayer.transform, animationDriver);

        GameObject programPanel = BuildProgramPanel(rightRoot.transform, programLinePrefabObject, programCommandPrefabObject.GetComponent<ProgramCommandView>(), animationDriver, messageText, maxProgramLines);

        ProgramPanelController programPanelController = programPanel.GetComponent<ProgramPanelController>();
        Button clearButton, undoButton, restartButton;
        BuildBottomActionsPanel(bottomRightPanel.transform, programPanelController, animationDriver, out clearButton, out undoButton, out restartButton);

        ProgramExecutor executor = bottomLeftPanel.AddComponent<ProgramExecutor>();
        ConfigureExecutor(executor, programPanelController, satelliteState, missionSystem, messageText, runButton, pauseButton, restartButton);
        UnityEventTools.AddPersistentListener(runButton.onClick, executor.RunProgram);
        UnityEventTools.AddPersistentListener(pauseButton.onClick, executor.TogglePause);
        UnityEventTools.AddPersistentListener(restartButton.onClick, executor.RestartProgramFromBeginning);
        UnityEventTools.AddPersistentListener(clearButton.onClick, programPanelController.ClearProgram);
        UnityEventTools.AddPersistentListener(undoButton.onClick, programPanelController.UndoLastCommand);
        EditorUtility.SetDirty(runButton);
        EditorUtility.SetDirty(pauseButton);
        EditorUtility.SetDirty(restartButton);
        EditorUtility.SetDirty(clearButton);
        EditorUtility.SetDirty(undoButton);
        EditorUtility.SetDirty(executor);

        ConfigureSatelliteController();
        CleanupLegacyBottomLeftControls(bottomLeftPanel.transform);

        commandPoolPanel.transform.SetSiblingIndex(0);
        programPanel.transform.SetSiblingIndex(1);
        popupLayer.transform.SetAsLastSibling();
        dragGhostLayer.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static TMP_Text BuildMessagePanel(Transform parent)
    {
        AddPanelTitle(parent, "DOWNLINK");
        TMP_Text text = CreateText(parent, "MessageText", "Перетащи команду из пула в строку программы.", 19f, TextColor, TextAlignmentOptions.MidlineLeft);
        SetStretch(text.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-16f, -28f));
        return text;
    }

    private static SatelliteStateController BuildMissionPanel(
        Transform parent,
        TMP_Text messageText,
        MissionDefinition missionDefinition,
        out MissionSystem missionSystem)
    {
        TMP_Text missionTitle = CreateText(parent, "MissionTitleText", "МИССИЯ", 21f, Amber, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        SetStretch(missionTitle.gameObject, Vector2.zero, Vector2.one, new Vector2(18f, 308f), new Vector2(-18f, -18f));

        TMP_Text missionDescription = CreateText(parent, "MissionDescriptionText", string.Empty, 13f, MutedTextColor, TextAlignmentOptions.TopLeft);
        missionDescription.overflowMode = TextOverflowModes.Truncate;
        SetStretch(missionDescription.gameObject, Vector2.zero, Vector2.one, new Vector2(18f, 256f), new Vector2(-18f, -62f));

        TMP_Text completedText = CreateText(parent, "MissionCompletedText", "КЛЮЧЕВЫЕ ЗАДАЧИ", 13f, Amber, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        SetStretch(completedText.gameObject, Vector2.zero, Vector2.one, new Vector2(18f, 216f), new Vector2(-18f, -122f));

        TMP_Text objective = CreateText(parent, "ObjectiveText", string.Empty, 13.6f, TextColor, TextAlignmentOptions.TopLeft);
        objective.richText = true;
        objective.lineSpacing = -4f;
        objective.textWrappingMode = TextWrappingModes.NoWrap;
        objective.overflowMode = TextOverflowModes.Truncate;
        SetStretch(objective.gameObject, Vector2.zero, Vector2.one, new Vector2(18f, 128f), new Vector2(-18f, -150f));

        TMP_Text stateText = CreateText(parent, "SatelliteStateText", string.Empty, 13f, TextColor, TextAlignmentOptions.TopLeft);
        stateText.richText = true;
        stateText.lineSpacing = -7f;
        stateText.textWrappingMode = TextWrappingModes.NoWrap;
        stateText.overflowMode = TextOverflowModes.Truncate;
        SetStretch(stateText.gameObject, Vector2.zero, Vector2.one, new Vector2(18f, 20f), new Vector2(-18f, -246f));

        SatelliteStateView stateView = parent.gameObject.AddComponent<SatelliteStateView>();
        stateView.Configure(stateText, messageText);
        SatelliteStateController stateController = parent.gameObject.AddComponent<SatelliteStateController>();
        stateController.Configure(stateView, messageText);
        MissionPanel panel = parent.gameObject.AddComponent<MissionPanel>();
        panel.Configure(missionTitle, missionDescription, objective, completedText);
        missionSystem = parent.gameObject.AddComponent<MissionSystem>();
        missionSystem.Configure(missionDefinition, panel, stateController);
        return stateController;
    }

    private static void BuildBottomControlsPanel(Transform parent, UIAnimationDriver animationDriver, out Button runButton, out Button pauseButton)
    {
        HorizontalLayoutGroup layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        runButton = CreateActionButton(parent, "RunButton", "RUN", Green, animationDriver);
        pauseButton = CreateActionButton(parent, "PauseButton", "PAUSE", Amber, animationDriver);
    }

    private static GameObject BuildCommandPoolPanel(Transform parent, CommandDefinition[] definitions, GameObject itemPrefab, DragGhostView dragGhostPrefab, Canvas canvas, RectTransform dragGhostLayer, UIAnimationDriver animationDriver)
    {
        GameObject panel = CreatePanel(parent, "CommandPoolPanel", PanelColor);
        LayoutElement layoutElement = panel.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 245f;
        layoutElement.flexibleWidth = 0f;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 12);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddHeader(panel.transform, "COMMANDS", "templates");
        BuildCommandCategoryTabs(panel.transform);

        GameObject viewport = CreateUiObject("CommandPoolScrollViewport", panel.transform);
        LayoutElement viewportElement = viewport.AddComponent<LayoutElement>();
        viewportElement.flexibleHeight = 1f;
        viewportElement.minHeight = 160f;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();

        GameObject itemsContainer = CreateUiObject("CommandPoolItemsContainer", viewport.transform);
        RectTransform itemsRect = (RectTransform)itemsContainer.transform;
        itemsRect.anchorMin = new Vector2(0f, 1f);
        itemsRect.anchorMax = new Vector2(1f, 1f);
        itemsRect.pivot = new Vector2(0.5f, 1f);
        itemsRect.anchoredPosition = Vector2.zero;
        itemsRect.offsetMin = Vector2.zero;
        itemsRect.offsetMax = Vector2.zero;
        VerticalLayoutGroup itemsLayout = itemsContainer.AddComponent<VerticalLayoutGroup>();
        itemsLayout.spacing = 7f;
        itemsLayout.childAlignment = TextAnchor.UpperCenter;
        itemsLayout.childControlWidth = true;
        itemsLayout.childControlHeight = true;
        itemsLayout.childForceExpandWidth = true;
        itemsLayout.childForceExpandHeight = false;

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = itemsRect;
        scrollRect.viewport = (RectTransform)viewport.transform;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.elasticity = 0.08f;
        scrollRect.scrollSensitivity = 0f;
        scrollRect.verticalScrollbar = null;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        CommandPaletteContentSizer contentSizer = viewport.AddComponent<CommandPaletteContentSizer>();
        contentSizer.Configure((RectTransform)viewport.transform, itemsRect);
        CommandPaletteSmoothScrollWheel smoothWheel = viewport.AddComponent<CommandPaletteSmoothScrollWheel>();
        smoothWheel.Configure(scrollRect, true);

        foreach (CommandDefinition definition in definitions)
        {
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab, itemsContainer.transform);
            item.name = $"CommandPoolItem_{definition.Type}";
            CommandPoolItemView view = item.GetComponent<CommandPoolItemView>();
            view.Configure(definition, canvas, dragGhostLayer, dragGhostPrefab, animationDriver, item.GetComponent<CanvasGroup>(), item.GetComponent<Image>(), item.transform.Find("AccentBar").GetComponent<Image>(), item.transform.Find("TextStack/TitleText").GetComponent<TMP_Text>(), item.transform.Find("TextStack/DescriptionText").GetComponent<TMP_Text>());
            EditorUtility.SetDirty(view);
        }

        return panel;
    }

    private static void BuildCommandCategoryTabs(Transform parent)
    {
        GameObject tabs = CreateUiObject("CommandCategoryTabs", parent);
        LayoutElement element = tabs.AddComponent<LayoutElement>();
        element.minHeight = 52f;
        element.preferredHeight = 52f;

        GridLayoutGroup grid = tabs.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(67f, 24f);
        grid.spacing = new Vector2(4f, 4f);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        CreateCommandCategoryTab(tabs.transform, "CommandCategoryTab_All", "ВСЕ");
        CreateCommandCategoryTab(tabs.transform, "CommandCategoryTab_Systems", "СИСТЕМЫ");
        CreateCommandCategoryTab(tabs.transform, "CommandCategoryTab_Attitude", "ОРИЕНТ");
        CreateCommandCategoryTab(tabs.transform, "CommandCategoryTab_Camera", "КАМЕРА");
        CreateCommandCategoryTab(tabs.transform, "CommandCategoryTab_Link", "СВЯЗЬ");
        CreateCommandCategoryTab(tabs.transform, "CommandCategoryTab_Logic", "ЛОГИКА");
    }

    private static void CreateCommandCategoryTab(Transform parent, string name, string label)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.20f, 0.21f, 0.23f, 0.62f));
        Image image = root.GetComponent<Image>();
        image.color = new Color(0.26f, 0.27f, 0.30f, 0.70f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText(root.transform, "Text", label, 9.5f, TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        SetStretch(text.gameObject, Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(-2f, 0f));
    }

    private static Scrollbar BuildCommandPoolScrollbar(Transform viewport)
    {
        GameObject root = CreateUiObject("CommandPoolScrollbar", viewport);
        SetStretch(root, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-7f, 4f), new Vector2(-1f, -4f));

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.08f, 0.09f, 0.10f, 0.48f);
        Scrollbar scrollbar = root.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.TopToBottom;

        GameObject slidingArea = CreateUiObject("Sliding Area", root.transform);
        SetStretch(slidingArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject handle = CreateUiObject("Handle", slidingArea.transform);
        SetStretch(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.82f, 0.70f, 0.54f, 0.82f);

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = (RectTransform)handle.transform;
        return scrollbar;
    }

    private static GameObject BuildProgramPanel(
        Transform parent,
        GameObject linePrefab,
        ProgramCommandView programCommandPrefab,
        UIAnimationDriver animationDriver,
        TMP_Text messageText,
        int lineCount)
    {
        GameObject panel = CreatePanel(parent, "ProgramPanel", PanelColorStrong);
        LayoutElement layoutElement = panel.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 438f;
        layoutElement.flexibleWidth = 1f;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        lineCount = Mathf.Max(1, lineCount);
        AddHeader(panel.transform, "PROGRAM", $"{lineCount} lines");

        GameObject linesContainer = CreatePanel(panel.transform, "ProgramLinesContainer", new Color(0.015f, 0.023f, 0.032f, 0.64f));
        LayoutElement linesElement = linesContainer.AddComponent<LayoutElement>();
        linesElement.flexibleHeight = 1f;
        linesElement.minHeight = Mathf.Max(180f, lineCount * 48f);
        VerticalLayoutGroup linesLayout = linesContainer.AddComponent<VerticalLayoutGroup>();
        linesLayout.padding = new RectOffset(8, 8, 8, 8);
        linesLayout.spacing = 6f;
        linesLayout.childAlignment = TextAnchor.UpperCenter;
        linesLayout.childControlWidth = true;
        linesLayout.childControlHeight = true;
        linesLayout.childForceExpandWidth = true;
        linesLayout.childForceExpandHeight = false;

        ProgramModel model = panel.AddComponent<ProgramModel>();
        ProgramPanelController controller = panel.AddComponent<ProgramPanelController>();
        List<ProgramLineView> lineViews = new(lineCount);

        for (int i = 0; i < lineCount; i++)
        {
            GameObject line = (GameObject)PrefabUtility.InstantiatePrefab(linePrefab, linesContainer.transform);
            line.name = $"ProgramLine_{i + 1:00}";
            ProgramLineView view = line.GetComponent<ProgramLineView>();
            view.Configure(i + 1, controller, line.transform.Find("LineNumberText").GetComponent<TMP_Text>(), line.transform.Find("PlaceholderText").GetComponent<TMP_Text>(), (RectTransform)line.transform.Find("CommandContainer"), line.GetComponent<Image>(), line.transform.Find("HoverImage").GetComponent<Image>(), animationDriver);
            lineViews.Add(view);
            EditorUtility.SetDirty(view);
        }

        model.EnsureLineCount(lineViews.Count);
        controller.Configure(model, programCommandPrefab, animationDriver, messageText, lineViews);
        EditorUtility.SetDirty(model);
        EditorUtility.SetDirty(controller);
        return panel;
    }

    private static void BuildBottomActionsPanel(Transform parent, ProgramPanelController programPanel, UIAnimationDriver animationDriver, out Button clearButton, out Button undoButton, out Button restartButton)
    {
        HorizontalLayoutGroup layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        undoButton = CreateActionButton(parent, "UndoButton", "UNDO", Amber, animationDriver);
        restartButton = CreateActionButton(parent, "RestartButton", "RESTART", Cyan, animationDriver);
        clearButton = CreateActionButton(parent, "ClearButton", "CLEAR", Magenta, animationDriver);
    }

    private static void ConfigureExecutor(
        ProgramExecutor executor,
        ProgramPanelController programPanelController,
        SatelliteStateController satelliteStateController,
        MissionSystem missionSystem,
        TMP_Text messageText,
        Button runButton,
        Button pauseButton,
        Button restartButton)
    {
        SatelliteController satelliteController = FindPrimarySatelliteController();
        ProgramModel programModel = programPanelController != null ? programPanelController.Model : null;

        executor.Configure(
            programModel,
            programPanelController,
            satelliteStateController,
            messageText,
            runButton,
            null,
            pauseButton,
            null,
            missionSystem);
        executor.ConfigureSceneBindings(satelliteController, 30f);

        SerializedObject executorObject = new SerializedObject(executor);
        executorObject.FindProperty("programModel").objectReferenceValue = programModel;
        executorObject.FindProperty("programPanelView").objectReferenceValue = programPanelController;
        executorObject.FindProperty("satelliteStateController").objectReferenceValue = satelliteStateController;
        executorObject.FindProperty("missionSystem").objectReferenceValue = missionSystem;
        executorObject.FindProperty("satelliteController").objectReferenceValue = satelliteController;
        executorObject.FindProperty("statusText").objectReferenceValue = messageText;
        executorObject.FindProperty("runButton").objectReferenceValue = runButton;
        executorObject.FindProperty("stepButton").objectReferenceValue = null;
        executorObject.FindProperty("pauseButton").objectReferenceValue = pauseButton;
        executorObject.FindProperty("resetButton").objectReferenceValue = null;
        SerializedProperty restartButtonProperty = executorObject.FindProperty("restartButton");
        if (restartButtonProperty != null)
        {
            restartButtonProperty.objectReferenceValue = restartButton;
        }

        SerializedProperty emptyLineSkipDelay = executorObject.FindProperty("emptyLineSkipDelay");
        if (emptyLineSkipDelay != null)
        {
            emptyLineSkipDelay.floatValue = 0.1f;
        }

        SerializedProperty rotationSpeedDegreesPerSecond = executorObject.FindProperty("rotationSpeedDegreesPerSecond");
        if (rotationSpeedDegreesPerSecond != null)
        {
            rotationSpeedDegreesPerSecond.floatValue = 30f;
        }

        SerializedProperty attitudeCommandDurationSeconds = executorObject.FindProperty("attitudeCommandDurationSeconds");
        if (attitudeCommandDurationSeconds != null)
        {
            attitudeCommandDurationSeconds.floatValue = 15f;
        }

        SerializedProperty attitudeCommandAdvanceDelaySeconds = executorObject.FindProperty("attitudeCommandAdvanceDelaySeconds");
        if (attitudeCommandAdvanceDelaySeconds != null)
        {
            attitudeCommandAdvanceDelaySeconds.floatValue = 1f;
        }

        if (satelliteController != null)
        {
            SerializedObject satelliteControllerObject = new SerializedObject(satelliteController);
            SerializedProperty defaultRotationSpeed = satelliteControllerObject.FindProperty("defaultRotationSpeedDegreesPerSecond");
            if (defaultRotationSpeed != null)
            {
                defaultRotationSpeed.floatValue = 30f;
            }

            SerializedProperty rotationCompletionAngle = satelliteControllerObject.FindProperty("rotationCompletionAngleDegrees");
            if (rotationCompletionAngle != null)
            {
                rotationCompletionAngle.floatValue = 0.25f;
            }

            SerializedProperty earthAimLocalAxis = satelliteControllerObject.FindProperty("earthAimLocalAxis");
            if (earthAimLocalAxis != null)
            {
                earthAimLocalAxis.vector3Value = Vector3.up;
            }

            SerializedProperty sunAimLocalAxis = satelliteControllerObject.FindProperty("sunAimLocalAxis");
            if (sunAimLocalAxis != null)
            {
                sunAimLocalAxis.vector3Value = Vector3.forward;
            }

            SerializedProperty antennaAimLocalAxis = satelliteControllerObject.FindProperty("antennaAimLocalAxis");
            if (antennaAimLocalAxis != null)
            {
                antennaAimLocalAxis.vector3Value = Vector3.forward;
            }

            satelliteControllerObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(satelliteController);
        }

        executorObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CommandDefinition[] EnsureCommandDefinitions()
    {
        return new[]
        {
            CreateOrUpdateCommand("PowerToggle", CommandType.PowerToggle, "Питание ВКЛ/ВЫКЛ", "Переключить питание спутника.", Green, 1),
            CreateOrUpdateCommand("ReadSunSensors", CommandType.ReadSunSensors, "Считать солнечные датчики", "Получить данные освещенности.", Amber, 1),
            CreateOrUpdateCommand("ReadMagnetometer", CommandType.ReadMagnetometer, "Считать магнитометр", "Получить данные магнитного поля.", Cyan, 1),
            CreateOrUpdateCommand("RotateToEarth", CommandType.RotateToEarth, "Повернуть к Земле", "Наводить спутник на Землю камерой или антенной стороной в течение 15 секунд.", new Color(0.35f, 0.62f, 1f, 1f), 1),
            CreateOrUpdateCommand("RotateToSun", CommandType.RotateToSun, "Повернуть к Солнцу", "Наводить спутник и панели на Солнце в течение 15 секунд.", new Color(1f, 0.52f, 0.22f, 1f), 1),
            CreateOrUpdateCommand("JumpTo", CommandType.JumpTo, "Перейти к строке", "Перейти к указанной строке.", Magenta, 1),
            CreateOrUpdateCommand("TakeEarthPhoto", CommandType.TakeEarthPhoto, "Сделать фото", "Снять кадр с камеры: Землю, звездное небо или черный экран при закрытой крышке.", new Color(0.25f, 1f, 0.78f, 1f), 1),
            CreateOrUpdateCommand("RotateAntennaToEarth", CommandType.RotateAntennaToEarth, "Повернуть антенну к Земле", "Навести антенну на Землю для передачи данных.", new Color(0.42f, 0.86f, 1f, 1f), 1),
            CreateOrUpdateCommand("SendMessageToEarth", CommandType.SendMessageToEarth, "Отправить сообщение", "Передать сигнал тонким лучом туда, куда сейчас направлена антенна.", new Color(0.3f, 0.92f, 1f, 1f), 1),
            CreateOrUpdateCommand("StabilizeSatellite", CommandType.StabilizeSatellite, "Стабилизировать спутник", "Гасить вращение и удерживать текущую ориентацию спутника относительно Земли 15 секунд.", Green, 1),
            CreateOrUpdateCommand("CheckEarthInFrame", CommandType.CheckEarthInFrame, "Проверить Землю в кадре", "Проверить, попадает ли Земля в поле камеры спутника.", Cyan, 1),
            CreateOrUpdateCommand("Wait", CommandType.Wait, "Ждать", "Подождать указанное количество секунд.", Amber, 1, 1f),
            CreateOrUpdateCommand("ConditionalJump", CommandType.ConditionalJump, "Если условие -> строка", "Если условие истинно, перейти к указанной строке.", Magenta, 1, 1f, CommandConditionType.PowerOn),
            CreateOrUpdateCommand("CalibrateGyroscopes", CommandType.CalibrateGyroscopes, "Калибровать гироскопы", "Подготовить точную ориентацию спутника.", new Color(0.53f, 0.78f, 1f, 1f), 1),
            CreateOrUpdateCommand("ChargeBattery", CommandType.ChargeBattery, "Зарядить батарею", "Зарядить аккумулятор, если панели смотрят на Солнце.", new Color(1f, 0.78f, 0.22f, 1f), 1),
            CreateOrUpdateCommand("OpenCameraCover", CommandType.OpenCameraCover, "Открыть крышку камеры", "Подготовить оптический канал перед проверкой кадра и съемкой.", new Color(0.58f, 0.95f, 0.75f, 1f), 1),
            CreateOrUpdateCommand("CloseCameraCover", CommandType.CloseCameraCover, "Закрыть крышку камеры", "Защитить камеру после съемки.", new Color(0.62f, 0.67f, 0.74f, 1f), 1),
            CreateOrUpdateCommand("CompressPhoto", CommandType.CompressPhoto, "Сжать снимок", "Подготовить снятые данные к передаче на Землю.", new Color(0.86f, 0.62f, 1f, 1f), 1),
            CreateOrUpdateCommand("CheckCommunicationLink", CommandType.CheckCommunicationLink, "Проверить канал связи", "Проверить, проходит ли сигнал через антенну на Землю.", new Color(0.34f, 0.88f, 1f, 1f), 1)
        };
    }

    private static MissionDefinition[] EnsureMissionDefinitions(CommandDefinition[] availableCommands)
    {
        return new[]
        {
            CreateOrUpdateMissionDefinition(
                "Mission_FirstPhoto",
                "Первый снимок",
                "Собери минимальную программу для первого снимка Земли.",
                availableCommands,
                new[]
                {
                    new MissionObjective(MissionObjectiveType.PowerEnabled, "Включить питание"),
                    new MissionObjective(MissionObjectiveType.EarthDataCollected, "Считать магнитометр"),
                    new MissionObjective(MissionObjectiveType.SatelliteFacingEarth, "Повернуться к Земле"),
                    new MissionObjective(MissionObjectiveType.CameraCoverOpen, "Открыть крышку камеры"),
                    new MissionObjective(MissionObjectiveType.EarthInFrame, "Проверить Землю в кадре"),
                    new MissionObjective(MissionObjectiveType.PhotoTaken, "Сделать фото")
                }),
            CreateOrUpdateMissionDefinition(
                "Mission_PhotoAndTransmit",
                "Снимок и передача",
                "Сделай снимок, проверь кадр и отправь данные на Землю.",
                availableCommands,
                new[]
                {
                    new MissionObjective(MissionObjectiveType.PowerEnabled, "Включить питание"),
                    new MissionObjective(MissionObjectiveType.EarthDataCollected, "Считать магнитометр"),
                    new MissionObjective(MissionObjectiveType.SatelliteFacingEarth, "Повернуться к Земле"),
                    new MissionObjective(MissionObjectiveType.CameraCoverOpen, "Открыть крышку камеры"),
                    new MissionObjective(MissionObjectiveType.EarthInFrame, "Проверить Землю в кадре"),
                    new MissionObjective(MissionObjectiveType.PhotoTaken, "Сделать фото"),
                    new MissionObjective(MissionObjectiveType.DataCompressed, "Сжать снимок"),
                    new MissionObjective(MissionObjectiveType.CommunicationLinkAvailable, "Проверить канал связи"),
                    new MissionObjective(MissionObjectiveType.DataSent, "Отправить сообщение")
                }),
            CreateOrUpdateMissionDefinition(
                "Mission_SafePhoto",
                "Безопасная съемка",
                "Полная цепочка: питание, ориентация, стабилизация, снимок и передача.",
                availableCommands,
                new[]
                {
                    new MissionObjective(MissionObjectiveType.PowerEnabled, "Включить питание"),
                    new MissionObjective(MissionObjectiveType.EarthDataCollected, "Считать магнитометр"),
                    new MissionObjective(MissionObjectiveType.SatelliteFacingEarth, "Повернуться к Земле"),
                    new MissionObjective(MissionObjectiveType.GyrosCalibrated, "Калибровать гироскопы"),
                    new MissionObjective(MissionObjectiveType.SatelliteStabilized, "Стабилизировать спутник"),
                    new MissionObjective(MissionObjectiveType.CameraCoverOpen, "Открыть крышку камеры"),
                    new MissionObjective(MissionObjectiveType.EarthInFrame, "Проверить Землю в кадре"),
                    new MissionObjective(MissionObjectiveType.PhotoTaken, "Сделать фото"),
                    new MissionObjective(MissionObjectiveType.DataCompressed, "Сжать снимок"),
                    new MissionObjective(MissionObjectiveType.CommunicationLinkAvailable, "Проверить канал связи"),
                    new MissionObjective(MissionObjectiveType.DataSent, "Отправить сообщение")
                })
        };
    }

    private static CommandDefinition[] ResolveMissionCommandPool(MissionDefinition missionDefinition, CommandDefinition[] fallbackDefinitions)
    {
        if (missionDefinition == null || missionDefinition.availableCommands == null || missionDefinition.availableCommands.Count == 0)
        {
            return fallbackDefinitions;
        }

        List<CommandDefinition> commands = new List<CommandDefinition>();
        for (int i = 0; i < missionDefinition.availableCommands.Count; i++)
        {
            CommandDefinition command = missionDefinition.availableCommands[i];
            if (command != null && !commands.Contains(command))
            {
                commands.Add(command);
            }
        }

        return commands.Count > 0 ? commands.ToArray() : fallbackDefinitions;
    }

    private static int ResolveMissionLineCount(MissionDefinition missionDefinition)
    {
        return missionDefinition != null
            ? Mathf.Max(1, missionDefinition.maxProgramLines)
            : 13;
    }

    private static MissionDefinition CreateOrUpdateMissionDefinition(
        string assetName,
        string missionName,
        string missionDescription,
        CommandDefinition[] availableCommands,
        MissionObjective[] requiredObjectives)
    {
        string path = $"{MissionDefinitionsFolder}/{assetName}.asset";
        MissionDefinition definition = AssetDatabase.LoadAssetAtPath<MissionDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<MissionDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        definition.missionName = missionName;
        definition.missionDescription = missionDescription;
        definition.maxProgramLines = 13;

        definition.availableCommands.Clear();
        if (availableCommands != null)
        {
            definition.availableCommands.AddRange(availableCommands);
        }

        definition.requiredObjectives.Clear();
        if (requiredObjectives != null)
        {
            definition.requiredObjectives.AddRange(requiredObjectives);
        }

        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static CommandDefinition CreateOrUpdateCommand(
        string assetName,
        CommandType type,
        string displayName,
        string description,
        Color accent,
        int defaultTargetLine,
        float defaultWaitSeconds = 1f,
        CommandConditionType defaultCondition = CommandConditionType.PowerOn)
    {
        string path = $"{CommandsFolder}/{assetName}.asset";
        CommandDefinition definition = AssetDatabase.LoadAssetAtPath<CommandDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<CommandDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        definition.Type = type;
        definition.DisplayName = displayName;
        definition.Description = description;
        definition.AccentColor = accent;
        definition.DefaultTargetLine = defaultTargetLine;
        definition.DefaultWaitSeconds = defaultWaitSeconds;
        definition.DefaultCondition = defaultCondition;
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static GameObject EnsureDragGhostPrefab()
    {
        GameObject root = CreateUiObject("DragGhost", null);
        SetPrefabSize(root, new Vector2(230f, 54f));
        Image background = root.AddComponent<Image>();
        background.sprite = softPillSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.76f, 0.79f, 0.82f, 0.90f);
        background.raycastTarget = false;
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        TMP_Text title = CreateText(root.transform, "TitleText", "COMMAND", 17f, new Color(0.02f, 0.04f, 0.05f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetStretch(title.gameObject, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));

        DragGhostView ghostView = root.AddComponent<DragGhostView>();
        ghostView.Configure((RectTransform)root.transform, background, title, group, null);
        return SavePrefab(root, DragGhostPrefabPath);
    }

    private static GameObject EnsurePoolItemPrefab(DragGhostView dragGhostPrefab)
    {
        GameObject root = CreateUiObject("CommandPoolItem", null);
        SetPrefabSize(root, new Vector2(220f, 58f));
        Image background = root.AddComponent<Image>();
        background.sprite = softPanelSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.24f, 0.25f, 0.27f, 0.58f);
        background.raycastTarget = true;
        AddFrostedLayer(root.transform);
        Shadow poolShadow = root.AddComponent<Shadow>();
        poolShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        poolShadow.effectDistance = new Vector2(0f, -2f);
        CanvasGroup group = root.AddComponent<CanvasGroup>();

        HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 10, 7, 7);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        LayoutElement element = root.AddComponent<LayoutElement>();
        element.minHeight = 58f;
        element.preferredHeight = 58f;
        element.flexibleHeight = 0f;

        GameObject accent = CreatePanel(root.transform, "AccentBar", Cyan);
        accent.GetComponent<Image>().raycastTarget = false;
        LayoutElement accentElement = accent.AddComponent<LayoutElement>();
        accentElement.minWidth = 5f;
        accentElement.preferredWidth = 5f;

        GameObject textStack = CreateUiObject("TextStack", root.transform);
        VerticalLayoutGroup stackLayout = textStack.AddComponent<VerticalLayoutGroup>();
        stackLayout.spacing = 1f;
        stackLayout.childControlWidth = true;
        stackLayout.childControlHeight = true;
        stackLayout.childForceExpandWidth = true;
        stackLayout.childForceExpandHeight = false;
        textStack.AddComponent<LayoutElement>().flexibleWidth = 1f;

        TMP_Text title = CreateText(textStack.transform, "TitleText", "Command", 14f, TextColor, TextAlignmentOptions.Left, FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        TMP_Text description = CreateText(textStack.transform, "DescriptionText", "Template", 10.5f, MutedTextColor, TextAlignmentOptions.Left);
        description.textWrappingMode = TextWrappingModes.NoWrap;

        CommandPoolItemView view = root.AddComponent<CommandPoolItemView>();
        view.Configure(null, null, null, dragGhostPrefab, null, group, background, accent.GetComponent<Image>(), title, description);
        return SavePrefab(root, PoolItemPrefabPath);
    }

    private static GameObject EnsureProgramLinePrefab()
    {
        GameObject root = CreateUiObject("ProgramLine", null);
        SetPrefabSize(root, new Vector2(410f, 42f));
        Image background = root.AddComponent<Image>();
        background.sprite = softPanelSprite;
        background.type = Image.Type.Sliced;
        background.color = LineColor;
        background.raycastTarget = true;
        AddFrostedLayer(root.transform);
        Shadow lineShadow = root.AddComponent<Shadow>();
        lineShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
        lineShadow.effectDistance = new Vector2(0f, -1.5f);

        HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        LayoutElement element = root.AddComponent<LayoutElement>();
        element.minHeight = 42f;
        element.preferredHeight = 42f;

        TMP_Text lineNumber = CreateText(root.transform, "LineNumberText", "01", 15f, Amber, TextAlignmentOptions.Center, FontStyles.Bold);
        lineNumber.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;

        TMP_Text placeholder = CreateText(root.transform, "PlaceholderText", "drop command", 14f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
        placeholder.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;

        GameObject commandContainer = CreateUiObject("CommandContainer", root.transform);
        commandContainer.AddComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject hover = CreatePanel(root.transform, "HoverImage", new Color(0.18f, 0.92f, 1f, 0f));
        hover.GetComponent<Image>().raycastTarget = false;
        hover.AddComponent<LayoutElement>().ignoreLayout = true;
        SetStretch(hover, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        hover.transform.SetAsFirstSibling();

        ProgramLineView view = root.AddComponent<ProgramLineView>();
        view.Configure(1, null, lineNumber, placeholder, (RectTransform)commandContainer.transform, background, hover.GetComponent<Image>(), null);
        return SavePrefab(root, ProgramLinePrefabPath);
    }

    private static GameObject EnsureProgramCommandPrefab()
    {
        GameObject root = CreateUiObject("ProgramCommand", null);
        SetPrefabSize(root, new Vector2(260f, 34f));
        Image background = root.AddComponent<Image>();
        background.sprite = softPanelSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.36f, 0.38f, 0.40f, 0.58f);
        background.raycastTarget = true;
        AddFrostedLayer(root.transform);
        Shadow commandShadow = root.AddComponent<Shadow>();
        commandShadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
        commandShadow.effectDistance = new Vector2(0f, -2f);
        CanvasGroup group = root.AddComponent<CanvasGroup>();

        HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 8, 4, 4);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject accent = CreatePanel(root.transform, "AccentBar", Cyan);
        accent.GetComponent<Image>().raycastTarget = false;
        LayoutElement accentElement = accent.AddComponent<LayoutElement>();
        accentElement.preferredWidth = 4f;
        accentElement.minWidth = 4f;

        GameObject textStack = CreateUiObject("TextStack", root.transform);
        VerticalLayoutGroup stackLayout = textStack.AddComponent<VerticalLayoutGroup>();
        stackLayout.spacing = 0f;
        stackLayout.childControlWidth = true;
        stackLayout.childControlHeight = true;
        stackLayout.childForceExpandWidth = true;
        stackLayout.childForceExpandHeight = false;
        textStack.AddComponent<LayoutElement>().flexibleWidth = 1f;

        TMP_Text title = CreateText(textStack.transform, "TitleText", "Command", 13f, TextColor, TextAlignmentOptions.Left, FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        TMP_Text description = CreateText(textStack.transform, "DescriptionText", "Instance", 9.5f, MutedTextColor, TextAlignmentOptions.Left);
        description.textWrappingMode = TextWrappingModes.NoWrap;

        GameObject parameterRoot = CreateUiObject("ParameterRoot", root.transform);
        HorizontalLayoutGroup parameterLayout = parameterRoot.AddComponent<HorizontalLayoutGroup>();
        parameterLayout.spacing = 4f;
        parameterLayout.childAlignment = TextAnchor.MiddleCenter;
        parameterLayout.childControlWidth = true;
        parameterLayout.childControlHeight = true;
        parameterLayout.childForceExpandWidth = false;
        parameterLayout.childForceExpandHeight = true;
        parameterRoot.AddComponent<LayoutElement>().preferredWidth = 74f;

        TMP_Text toLabel = CreateText(parameterRoot.transform, "ToLabel", "TO", 10f, Amber, TextAlignmentOptions.Center, FontStyles.Bold);
        toLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 22f;
        TMP_InputField input = CreateLineInput(parameterRoot.transform, "TargetLineInput");

        ProgramCommandView view = root.AddComponent<ProgramCommandView>();
        view.Configure((RectTransform)root.transform, background, accent.GetComponent<Image>(), title, description, parameterRoot, input, group, null);
        return SavePrefab(root, ProgramCommandPrefabPath);
    }

    private static TMP_InputField CreateLineInput(Transform parent, string name)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.02f, 0.032f, 0.04f, 0.96f));
        root.AddComponent<LayoutElement>().preferredWidth = 42f;
        TMP_InputField input = root.AddComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 2;

        TMP_Text text = CreateText(root.transform, "Text", "01", 13f, TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        SetStretch(text.gameObject, Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(-2f, 0f));
        TMP_Text placeholder = CreateText(root.transform, "Placeholder", "01", 13f, MutedTextColor, TextAlignmentOptions.Center);
        SetStretch(placeholder.gameObject, Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(-2f, 0f));
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private static void AddHeader(Transform parent, string title, string subtitle)
    {
        GameObject header = CreateUiObject("Header", parent);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = true;
        LayoutElement headerElement = header.AddComponent<LayoutElement>();
        headerElement.minHeight = 34f;
        headerElement.preferredHeight = 34f;

        TMP_Text titleText = CreateText(header.transform, "TitleText", title, 18f, TextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        TMP_Text subtitleText = CreateText(header.transform, "SubtitleText", subtitle.ToUpperInvariant(), 11f, Amber, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        subtitleText.gameObject.AddComponent<LayoutElement>().preferredWidth = 92f;
    }

    private static void AddPanelTitle(Transform parent, string title)
    {
        TMP_Text text = CreateText(parent, "PanelTitle", title, 15f, Cyan, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        SetStretch(text.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 12f), new Vector2(-16f, -12f));
    }

    private static void CreateReadout(Transform parent, string label, string value, Color accent)
    {
        GameObject readout = CreatePanel(parent, $"Readout_{label}", new Color(0.052f, 0.072f, 0.082f, 0.90f));
        VerticalLayoutGroup layout = readout.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 6, 6);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        TMP_Text labelText = CreateText(readout.transform, "LabelText", label, 11f, MutedTextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        TMP_Text valueText = CreateText(readout.transform, "ValueText", value, 16f, accent, TextAlignmentOptions.Center, FontStyles.Bold);
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static Button CreateActionButton(Transform parent, string name, string label, Color accent, UIAnimationDriver animationDriver)
    {
        GameObject root = CreatePanel(parent, name, new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.94f));
        Image image = root.GetComponent<Image>();
        image.color = new Color(0.28f, 0.29f, 0.31f, 0.58f);
        Button button = root.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.36f, 0.37f, 0.39f, 0.72f);
        colors.pressedColor = new Color(0.46f, 0.47f, 0.49f, 0.82f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        LayoutElement element = root.AddComponent<LayoutElement>();
        element.minWidth = 108f;
        element.preferredWidth = 130f;
        element.minHeight = 46f;

        TMP_Text text = CreateText(root.transform, "Text", label, 16f, accent, TextAlignmentOptions.Center, FontStyles.Bold);
        SetStretch(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        AnimatedButton animatedButton = root.AddComponent<AnimatedButton>();
        animatedButton.Configure(animationDriver, (RectTransform)root.transform);
        return button;
    }

    private static void EnsureSoftSprites()
    {
        softPanelSprite = EnsureRoundedSprite(SoftPanelSpritePath, 96, 96, 18);
        softPillSprite = EnsureRoundedSprite(SoftPillSpritePath, 96, 42, 21);
    }

    private static void EnsureUniversalBlurFeature()
    {
        EnsureUniversalBlurFeature("Assets/Settings/PC_Renderer.asset");
        EnsureUniversalBlurFeature("Assets/Settings/Mobile_Renderer.asset");
    }

    private static void EnsureUniversalBlurFeature(string rendererDataPath)
    {
        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererDataPath);
        if (rendererData == null)
        {
            return;
        }

        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature is UniversalBlurFeature)
            {
                return;
            }
        }

        UniversalBlurFeature blurFeature = ScriptableObject.CreateInstance<UniversalBlurFeature>();
        blurFeature.name = "Unified Blur";
        blurFeature.SetActive(true);

        SerializedObject blurObject = new(blurFeature);
        blurObject.FindProperty("iterations").intValue = 3;
        blurObject.FindProperty("downsample").floatValue = 2.5f;
        blurObject.FindProperty("enableMipMaps").boolValue = true;
        blurObject.FindProperty("scale").floatValue = 0.85f;
        blurObject.FindProperty("offset").floatValue = 0.85f;
        blurObject.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.AddObjectToAsset(blurFeature, rendererData);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(blurFeature, out _, out long localId);

        SerializedObject rendererObject = new(rendererData);
        SerializedProperty features = rendererObject.FindProperty("m_RendererFeatures");
        SerializedProperty featureMap = rendererObject.FindProperty("m_RendererFeatureMap");
        int index = features.arraySize;
        features.InsertArrayElementAtIndex(index);
        features.GetArrayElementAtIndex(index).objectReferenceValue = blurFeature;
        featureMap.InsertArrayElementAtIndex(index);
        featureMap.GetArrayElementAtIndex(index).longValue = localId;
        rendererObject.ApplyModifiedPropertiesWithoutUndo();

        rendererData.SetDirty();
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssetIfDirty(rendererData);
    }

    private static Sprite EnsureRoundedSprite(string path, int width, int height, int radius)
    {
        if (!File.Exists(path))
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            Color transparent = new(1f, 1f, 1f, 0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, IsInsideRoundedRect(x, y, width, height, radius) ? Color.white : transparent);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        int left = radius;
        int right = width - radius - 1;
        int bottom = radius;
        int top = height - radius - 1;

        if (x >= left && x <= right)
        {
            return true;
        }

        if (y >= bottom && y <= top)
        {
            return true;
        }

        int cx = x < left ? left : right;
        int cy = y < bottom ? bottom : top;
        int dx = x - cx;
        int dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool ShouldAddFrostedLayer(string name)
    {
        return !name.Contains("AccentBar") &&
               !name.Contains("HoverImage") &&
               !name.Contains("TargetLineInput");
    }

    private static bool ShouldAddPanelDecorators(string name)
    {
        return !name.Contains("AccentBar") &&
               !name.Contains("HoverImage");
    }

    private static void AddFrostedLayer(Transform parent)
    {
        GameObject highlight = CreateUiObject("FrostedHighlight", parent);
        highlight.transform.SetAsFirstSibling();
        LayoutElement layoutElement = highlight.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
        Image image = highlight.AddComponent<Image>();
        image.sprite = softPanelSprite;
        image.type = Image.Type.Sliced;
        image.color = blurMaterial == null ? new Color(1f, 1f, 1f, 0.045f) : new Color(1f, 1f, 1f, 0.16f);
        image.material = blurMaterial;
        image.raycastTarget = false;
        SetStretch(highlight, Vector2.zero, Vector2.one, new Vector2(1.5f, 1.5f), new Vector2(-1.5f, -1.5f));
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
        }

        canvas.name = "Canvas";
        canvas.gameObject.SetActive(true);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureComponent<GraphicRaycaster>(canvas.gameObject);
        SetLayerRecursive(canvas.gameObject);
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SetLayerRecursive(eventSystem);
    }

    private static void RemoveTargetCanvasChildren(Transform canvas)
    {
        foreach (string targetName in TargetCanvasChildNames())
        {
            Transform child = canvas.Find(targetName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void MoveLegacyCanvasChildren(Transform canvas)
    {
        HashSet<string> targetNames = new(TargetCanvasChildNames());
        List<Transform> legacyChildren = new();

        foreach (Transform child in canvas)
        {
            if (!targetNames.Contains(child.name))
            {
                legacyChildren.Add(child);
            }
        }

        if (legacyChildren.Count == 0)
        {
            return;
        }

        GameObject legacyRoot = FindSceneObject("LegacyCanvasUI_Disabled");
        if (legacyRoot == null)
        {
            legacyRoot = new GameObject("LegacyCanvasUI_Disabled");
        }

        legacyRoot.SetActive(true);
        foreach (Transform child in legacyChildren)
        {
            child.SetParent(legacyRoot.transform, true);
        }

        legacyRoot.SetActive(false);
        EditorUtility.SetDirty(legacyRoot);
    }

    private static void CleanupLegacyBottomLeftControls(Transform activeBottomLeftPanel)
    {
        GameObject legacyRoot = FindSceneObject("LegacyCanvasUI_Disabled");
        if (legacyRoot != null)
        {
            List<GameObject> legacyObjectsToDestroy = new();
            foreach (Transform child in legacyRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == null)
                {
                    continue;
                }

                if (IsBottomLeftControlName(child.name) ||
                    child.name == "BottomActionPanel" ||
                    child.name == "BottomControlPanel")
                {
                    legacyObjectsToDestroy.Add(child.gameObject);
                }
            }

            for (int i = 0; i < legacyObjectsToDestroy.Count; i++)
            {
                Object.DestroyImmediate(legacyObjectsToDestroy[i]);
            }
        }

        List<GameObject> obsoleteButtons = new();
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform == null || !transform.gameObject.scene.IsValid())
            {
                continue;
            }

            if ((transform.name != "StopButton" && transform.name != "StepButton") ||
                (activeBottomLeftPanel != null && transform.IsChildOf(activeBottomLeftPanel)))
            {
                continue;
            }

            obsoleteButtons.Add(transform.gameObject);
        }

        for (int i = 0; i < obsoleteButtons.Count; i++)
        {
            Object.DestroyImmediate(obsoleteButtons[i]);
        }

        if (legacyRoot != null && legacyRoot.transform.childCount == 0)
        {
            Object.DestroyImmediate(legacyRoot);
        }
    }

    private static bool IsBottomLeftControlName(string objectName)
    {
        return objectName == "StopButton" ||
               objectName == "StepButton" ||
               objectName == "RunButton" ||
               objectName == "PlayButton" ||
               objectName == "PauseButton";
    }

    private static string[] TargetCanvasChildNames()
    {
        return new[]
        {
            "MissionPanel",
            "RightProgrammingRoot",
            "BottomLeftControlsPanel",
            "BottomRightActionsPanel",
            "MessagePanel",
            "PopupLayer",
            "DragGhostLayer"
        };
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name == objectName && transform.gameObject.scene.IsValid())
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject root = CreateUiObject(name, parent);
        Image image = root.AddComponent<Image>();
        image.sprite = softPanelSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = true;

        if (ShouldAddPanelDecorators(name))
        {
            Shadow shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -4f);

            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.86f, 0.90f, 0.16f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        if (ShouldAddFrostedLayer(name))
        {
            AddFrostedLayer(root.transform);
        }

        return root;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject root = new(name, typeof(RectTransform));
        if (parent != null)
        {
            root.transform.SetParent(parent, false);
        }

        SetLayerRecursive(root);
        return root;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        GameObject root = CreateUiObject(name, parent);
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void SetFixed(GameObject root, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
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

    private static void SetPrefabSize(GameObject root, Vector2 size)
    {
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
    }

    private static T EnsureComponent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        SetLayerRecursive(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    [MenuItem("COSMA/Setup Scene References")]
    public static void SetupSceneReferences()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureSatelliteController();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[COSMA] Scene references configured and saved.");
    }

    private static void ConfigureSatelliteController()
    {
        SatelliteController controller = FindPrimarySatelliteController();
        if (controller == null)
        {
            Debug.LogWarning("[COSMA] SatelliteController not found in scene.");
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        bool changed = false;

        SerializedProperty earthProp = so.FindProperty("earthTarget");
        if (earthProp == null) earthProp = so.FindProperty("<earthTarget>k__BackingField");
        if (earthProp != null && earthProp.objectReferenceValue == null)
        {
            Transform earthTarget = FindEarthTarget();
            if (earthTarget != null)
            {
                earthProp.objectReferenceValue = earthTarget;
                Debug.Log($"[COSMA] SatelliteController.earthTarget -> '{earthTarget.name}'");
                changed = true;
            }
        }

        SerializedProperty sunProp = so.FindProperty("sunTarget");
        if (sunProp == null) sunProp = so.FindProperty("<sunTarget>k__BackingField");
        if (sunProp != null && sunProp.objectReferenceValue == null)
        {
            Transform sunTarget = FindSunTarget();
            if (sunTarget != null)
            {
                sunProp.objectReferenceValue = sunTarget;
                Debug.Log($"[COSMA] SatelliteController.sunTarget -> '{sunTarget.name}'");
                changed = true;
            }
        }

        SerializedProperty defaultRotationSpeed = so.FindProperty("defaultRotationSpeedDegreesPerSecond");
        if (defaultRotationSpeed != null && !Mathf.Approximately(defaultRotationSpeed.floatValue, 30f))
        {
            defaultRotationSpeed.floatValue = 30f;
            changed = true;
        }

        SerializedProperty rotationCompletionAngle = so.FindProperty("rotationCompletionAngleDegrees");
        if (rotationCompletionAngle != null && !Mathf.Approximately(rotationCompletionAngle.floatValue, 0.25f))
        {
            rotationCompletionAngle.floatValue = 0.25f;
            changed = true;
        }

        SerializedProperty earthAimLocalAxis = so.FindProperty("earthAimLocalAxis");
        if (earthAimLocalAxis != null && earthAimLocalAxis.vector3Value != Vector3.up)
        {
            earthAimLocalAxis.vector3Value = Vector3.up;
            changed = true;
        }

        SerializedProperty sunAimLocalAxis = so.FindProperty("sunAimLocalAxis");
        if (sunAimLocalAxis != null && sunAimLocalAxis.vector3Value != Vector3.forward)
        {
            sunAimLocalAxis.vector3Value = Vector3.forward;
            changed = true;
        }

        SerializedProperty antennaAimLocalAxis = so.FindProperty("antennaAimLocalAxis");
        if (antennaAimLocalAxis != null && antennaAimLocalAxis.vector3Value != Vector3.forward)
        {
            antennaAimLocalAxis.vector3Value = Vector3.forward;
            changed = true;
        }

        if (changed)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }
        else
        {
            Debug.Log("[COSMA] All SatelliteController references already set.");
        }
    }

    private static Transform FindEarthTarget()
    {
        GameObject explicitTarget = FindSceneObject("EarthTarget");
        if (explicitTarget != null) return explicitTarget.transform;

        GameObject earth = FindSceneObject("Earth");
        if (earth != null) return earth.transform;

        OrbitEarth orbitEarth = Object.FindFirstObjectByType<OrbitEarth>(FindObjectsInactive.Include);
        if (orbitEarth != null) return orbitEarth.transform;

        return null;
    }

    private static Transform FindSunTarget()
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name == "Sun" && t.gameObject.scene.IsValid() && t.gameObject.activeInHierarchy)
            {
                return t;
            }
        }

        Sun sunComponent = Object.FindFirstObjectByType<Sun>(FindObjectsInactive.Include);
        if (sunComponent != null) return sunComponent.transform;

        GameObject sunObject = FindSceneObject("Sun");
        if (sunObject != null) return sunObject.transform;

        return null;
    }

    private static SatelliteController FindPrimarySatelliteController()
    {
        GameObject namedSatellite = FindSceneObject("Satellite");
        if (namedSatellite != null && namedSatellite.TryGetComponent(out SatelliteController namedController))
        {
            return namedController;
        }

        SatelliteController[] candidates = Object.FindObjectsByType<SatelliteController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SatelliteController bestCandidate = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            SatelliteController candidate = candidates[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            int score = 0;
            if (candidate.name == "Satellite")
            {
                score += 8;
            }

            if (candidate.GetComponent<Rigidbody>() != null)
            {
                score += 4;
            }

            if (candidate.EarthTarget != null)
            {
                score += 2;
            }

            if (candidate.SunTarget != null)
            {
                score += 2;
            }

            if (bestCandidate == null || score > bestScore)
            {
                bestCandidate = candidate;
                bestScore = score;
            }
        }

        return bestCandidate;
    }

    private static void SetLayerRecursive(GameObject root)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            root.layer = uiLayer;
        }

        foreach (Transform child in root.transform)
        {
            SetLayerRecursive(child.gameObject);
        }
    }
}
