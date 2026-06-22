#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class CosmaStartupSceneEnforcer
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    static CosmaStartupSceneEnforcer()
    {
        EditorApplication.delayCall -= ConfigureStartupScene;
        EditorApplication.delayCall += ConfigureStartupScene;
    }

    private static void ConfigureStartupScene()
    {
        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenuScene == null)
        {
            return;
        }

        EditorSceneManager.playModeStartScene = mainMenuScene;
        EnsureBuildSceneOrder();
    }

    private static void EnsureBuildSceneOrder()
    {
        var orderedScenes = new List<EditorBuildSettingsScene>();
        AddSceneIfExists(orderedScenes, MainMenuScenePath);
        AddSceneIfExists(orderedScenes, GameScenePath);

        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < currentScenes.Length; i++)
        {
            EditorBuildSettingsScene scene = currentScenes[i];
            if (scene == null || string.IsNullOrEmpty(scene.path))
            {
                continue;
            }

            if (scene.path == MainMenuScenePath || scene.path == GameScenePath)
            {
                continue;
            }

            orderedScenes.Add(scene);
        }

        EditorBuildSettings.scenes = orderedScenes.ToArray();
    }

    private static void AddSceneIfExists(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        if (scenes == null || string.IsNullOrEmpty(scenePath))
        {
            return;
        }

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset == null)
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    }
}
#endif
