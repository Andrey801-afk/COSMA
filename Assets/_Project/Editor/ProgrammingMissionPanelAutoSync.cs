using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProgrammingMissionPanelAutoSync
{
    private const string DefaultMissionPath = "Assets/_Project/MissionDefinitions/Mission_FirstPhoto.asset";
    private const string MissionPanelObjectName = "MissionPanel";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    static ProgrammingMissionPanelAutoSync()
    {
        EditorApplication.delayCall += SyncOpenSceneMissionPanel;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += SyncOpenSceneMissionPanel;
    }

    [MenuItem("COSMA/Sync Programming Mission Panel")]
    public static void SyncSampleSceneMissionPanel()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        SyncOpenSceneMissionPanel();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void SyncOpenSceneMissionPanel()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        MissionDefinition missionDefinition = AssetDatabase.LoadAssetAtPath<MissionDefinition>(DefaultMissionPath);
        if (missionDefinition == null)
        {
            return;
        }

        GameObject missionPanelObject = GameObject.Find(MissionPanelObjectName);
        if (missionPanelObject == null)
        {
            return;
        }

        ExpandPanelIfNeeded(missionPanelObject);

        TMP_Text missionTitle = EnsureText(
            missionPanelObject.transform,
            "PanelTitle",
            "МИССИЯ",
            21f,
            new Color(0.82f, 0.70f, 0.54f, 1f),
            TextAlignmentOptions.TopLeft,
            FontStyles.Bold,
            new Vector2(16f, 314f),
            new Vector2(-16f, -16f));
        TMP_Text missionDescription = EnsureText(
            missionPanelObject.transform,
            "MissionDescriptionText",
            string.Empty,
            13f,
            new Color(0.66f, 0.69f, 0.72f, 1f),
            TextAlignmentOptions.TopLeft,
            FontStyles.Normal,
            new Vector2(16f, 270f),
            new Vector2(-16f, -54f));
        TMP_Text missionComplete = EnsureText(
            missionPanelObject.transform,
            "MissionCompletedText",
            "КЛЮЧЕВЫЕ ЗАДАЧИ",
            13f,
            new Color(0.82f, 0.70f, 0.54f, 1f),
            TextAlignmentOptions.TopLeft,
            FontStyles.Bold,
            new Vector2(16f, 238f),
            new Vector2(-16f, -94f));
        TMP_Text objectives = EnsureText(
            missionPanelObject.transform,
            "ObjectiveText",
            string.Empty,
            14f,
            new Color(0.88f, 0.90f, 0.92f, 1f),
            TextAlignmentOptions.TopLeft,
            FontStyles.Normal,
            new Vector2(16f, 132f),
            new Vector2(-16f, -126f));
        TMP_Text satelliteStateText = EnsureText(
            missionPanelObject.transform,
            "SatelliteStateText",
            string.Empty,
            13f,
            new Color(0.88f, 0.90f, 0.92f, 1f),
            TextAlignmentOptions.TopLeft,
            FontStyles.Normal,
            new Vector2(16f, 16f),
            new Vector2(-16f, -220f));
        satelliteStateText.richText = true;
        satelliteStateText.lineSpacing = -6f;
        satelliteStateText.textWrappingMode = TextWrappingModes.NoWrap;
        satelliteStateText.overflowMode = TextOverflowModes.Truncate;

        MissionPanel missionPanel = missionPanelObject.GetComponent<MissionPanel>();
        if (missionPanel == null)
        {
            missionPanel = missionPanelObject.AddComponent<MissionPanel>();
        }

        missionPanel.Configure(missionTitle, missionDescription, objectives, missionComplete);

        SatelliteStateController stateController = missionPanelObject.GetComponent<SatelliteStateController>();
        if (stateController == null)
        {
            stateController = Object.FindFirstObjectByType<SatelliteStateController>(FindObjectsInactive.Include);
        }

        MissionSystem missionSystem = missionPanelObject.GetComponent<MissionSystem>();
        if (missionSystem == null)
        {
            missionSystem = missionPanelObject.AddComponent<MissionSystem>();
        }

        missionSystem.Configure(missionDefinition, missionPanel, stateController);
        StoreMissionSystemReferences(missionSystem, missionDefinition, missionPanel, stateController);
        BindExecutor(missionSystem);

        missionPanel.Render(missionDefinition, missionSystem.Objectives, missionSystem.IsCompleted);
        EditorUtility.SetDirty(missionPanelObject);
        EditorSceneManager.MarkSceneDirty(missionPanelObject.scene);
    }

    private static void ExpandPanelIfNeeded(GameObject missionPanelObject)
    {
        RectTransform rect = missionPanelObject.transform as RectTransform;
        if (rect == null || rect.sizeDelta.y >= 360f)
        {
            return;
        }

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 360f);
        EditorUtility.SetDirty(rect);
    }

    private static TMP_Text EnsureText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles style,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        Transform existing = parent.Find(name);
        GameObject root = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));

        if (existing == null)
        {
            root.transform.SetParent(parent, false);
        }

        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TMP_Text text = root.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        EditorUtility.SetDirty(root);
        return text;
    }

    private static void StoreMissionSystemReferences(
        MissionSystem missionSystem,
        MissionDefinition missionDefinition,
        MissionPanel missionPanel,
        SatelliteStateController stateController)
    {
        if (missionSystem == null)
        {
            return;
        }

        SerializedObject serializedSystem = new SerializedObject(missionSystem);
        serializedSystem.FindProperty("missionDefinition").objectReferenceValue = missionDefinition;
        serializedSystem.FindProperty("missionPanel").objectReferenceValue = missionPanel;
        serializedSystem.FindProperty("satelliteStateController").objectReferenceValue = stateController;
        serializedSystem.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(missionSystem);
    }

    private static void BindExecutor(MissionSystem missionSystem)
    {
        if (missionSystem == null)
        {
            return;
        }

        ProgramExecutor executor = Object.FindFirstObjectByType<ProgramExecutor>(FindObjectsInactive.Include);
        if (executor == null)
        {
            return;
        }

        SerializedObject serializedExecutor = new SerializedObject(executor);
        serializedExecutor.FindProperty("missionSystem").objectReferenceValue = missionSystem;
        serializedExecutor.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(executor);
    }
}
