#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuSceneCreator
{
    private const string SceneFolder = "Assets/Scenes";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string MissionsFolder = "Assets/_Project/Missions";

    [MenuItem("COSMA/Create Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        CreateMainMenuScene(true);
    }

    [MenuItem("COSMA/Refresh All UI")]
    public static void RefreshAllUi()
    {
        var activeScene = SceneManager.GetActiveScene();
        string originalScenePath = activeScene.IsValid() ? activeScene.path : string.Empty;

        if (activeScene.IsValid() && activeScene.isDirty && !string.IsNullOrEmpty(originalScenePath))
        {
            if (!EditorSceneManager.SaveScene(activeScene))
            {
                EditorUtility.DisplayDialog("COSMA",
                    "Refresh aborted: failed to save the currently open scene.",
                    "OK");
                return;
            }
        }

        CosmaProgrammingSceneBuilder.BuildSampleScene();
        CreateMainMenuScene(false);

        string sceneToOpen = string.IsNullOrEmpty(originalScenePath)
            ? "Assets/Scenes/SampleScene.unity"
            : originalScenePath;
        EditorSceneManager.OpenScene(sceneToOpen, OpenSceneMode.Single);

        if (sceneToOpen == "Assets/Scenes/SampleScene.unity")
        {
            ProgrammingCommandPaletteAutoSync.SyncCommandPaletteMenu();
        }

        EditorUtility.DisplayDialog("COSMA",
            "Programming UI and main menu refreshed. New command palette is preserved.",
            "OK");
    }

    private static void CreateMainMenuScene(bool showDialog)
    {
        EnsureFolder(SceneFolder);

        var missions = EnsureDefaultMissions();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrapGO = new GameObject("MainMenu");
        var bootstrap = bootstrapGO.AddComponent<MainMenuBootstrap>();

        var so = new SerializedObject(bootstrap);
        var missionsProp = so.FindProperty("_missions");
        missionsProp.arraySize = missions.Length;
        for (int i = 0; i < missions.Length; i++)
            missionsProp.GetArrayElementAtIndex(i).objectReferenceValue = missions[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);

        AddSceneToBuildSettings(ScenePath);
        AddSceneToBuildSettings("Assets/Scenes/SampleScene.unity");

        if (!showDialog)
        {
            return;
        }

        EditorUtility.DisplayDialog("COSMA",
            $"Сцена создана: {ScenePath}\nМиссий: {missions.Length}",
            "OK");

        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(ScenePath));
    }

    [MenuItem("COSMA/Create Default Missions")]
    public static void CreateDefaultMissionsMenu()
    {
        var missions = EnsureDefaultMissions();
        EditorUtility.DisplayDialog("COSMA",
            $"Создано/обновлено миссий: {missions.Length}\nПапка: {MissionsFolder}", "OK");
        if (missions.Length > 0)
            EditorGUIUtility.PingObject(missions[0]);
    }

    // ───────── helpers ─────────

    private static Mission[] EnsureDefaultMissions()
    {
        EnsureFolder(MissionsFolder);

        var defs = new[]
        {
            new MissionDef
            {
                id = "stabilize",
                title = "Стабилизация ориентации",
                objective = "Удержать угловую скорость спутника ниже 0.1 рад/с в течение 10 секунд.",
                description = "Запуск прошёл штатно, но спутник вращается. Используй маховики, чтобы погасить вращение и перейти в стабильный режим.",
                rewardScience = 25
            },
            new MissionDef
            {
                id = "solar_lock",
                title = "Захват солнечного сигнала",
                objective = "Направить датчик на Солнце так, чтобы значение датчика держалось выше 0.8.",
                description = "Солнечные батареи требуют точного наведения. Задача — выйти на оптимальный угол и удерживать его.",
                rewardScience = 50,
                prerequisites = new[] { "stabilize" }
            },
            new MissionDef
            {
                id = "full_orbit",
                title = "Полный оборот",
                objective = "Совершить полный оборот вокруг Земли, не теряя стабилизации.",
                description = "Долгая миссия: пройти 360° по орбите, удерживая ориентацию и связь с Землёй.",
                rewardScience = 75,
                prerequisites = new[] { "solar_lock" }
            },
            new MissionDef
            {
                id = "command_seq",
                title = "Программа на борт",
                objective = "Собрать и выполнить последовательность команд из 5+ блоков.",
                description = "Время автономной работы. Используй редактор команд, чтобы запрограммировать поведение спутника без участия оператора.",
                rewardScience = 100,
                prerequisites = new[] { "full_orbit" }
            },
        };

        var created = new Mission[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            var path = $"{MissionsFolder}/Mission_{def.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<Mission>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<Mission>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.id = def.id;
            asset.title = def.title;
            asset.objective = def.objective;
            asset.description = def.description;
            asset.rewardScience = def.rewardScience;
            asset.sceneName = "SampleScene";
            EditorUtility.SetDirty(asset);
            created[i] = asset;
        }

        // Wire prerequisites (second pass — assets exist now)
        for (int i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            if (def.prerequisites == null || def.prerequisites.Length == 0)
            {
                created[i].prerequisites = new Mission[0];
                continue;
            }
            var arr = new Mission[def.prerequisites.Length];
            for (int j = 0; j < def.prerequisites.Length; j++)
            {
                var pid = def.prerequisites[j];
                arr[j] = AssetDatabase.LoadAssetAtPath<Mission>($"{MissionsFolder}/Mission_{pid}.asset");
            }
            created[i].prerequisites = arr;
            EditorUtility.SetDirty(created[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return created;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    private struct MissionDef
    {
        public string id;
        public string title;
        public string objective;
        public string description;
        public int rewardScience;
        public string[] prerequisites;
    }
}
#endif
