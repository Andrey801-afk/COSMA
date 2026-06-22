#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MissionBuilderWindow : EditorWindow
{
    private const string MissionsFolder = "Assets/_Project/Missions";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    private readonly List<Mission> _missions = new List<Mission>();
    private Mission _selected;
    private SerializedObject _selectedSO;
    private Vector2 _listScroll;
    private Vector2 _editorScroll;
    private string _searchFilter = string.Empty;
    private bool _showOnlyIssues;

    [MenuItem("COSMA/Mission Builder")]
    public static void Open()
    {
        var window = GetWindow<MissionBuilderWindow>("Mission Builder");
        window.minSize = new Vector2(1020, 620);
        window.Show();
    }

    private void OnEnable() => RefreshMissions();
    private void OnFocus() => RefreshMissions();

    private void RefreshMissions()
    {
        _missions.Clear();
        var guids = AssetDatabase.FindAssets("t:Mission");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mission = AssetDatabase.LoadAssetAtPath<Mission>(path);
            if (mission != null) _missions.Add(mission);
        }

        _missions.Sort((a, b) =>
            string.Compare(a.id ?? string.Empty, b.id ?? string.Empty, System.StringComparison.OrdinalIgnoreCase));

        if (_selected != null && !_missions.Contains(_selected))
            Select(null);
        else if (_selected != null)
            _selectedSO = new SerializedObject(_selected);

        Repaint();
    }

    private void Select(Mission mission)
    {
        _selected = mission;
        _selectedSO = mission != null ? new SerializedObject(mission) : null;
        GUI.FocusControl(null);
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawLeftPane();
        DrawDivider();
        DrawRightPane();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("COSMA Mission Builder", EditorStyles.boldLabel, GUILayout.Width(180));
        GUILayout.Label($"{_missions.Count} миссий", EditorStyles.miniLabel, GUILayout.Width(80));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(90)))
            RefreshMissions();

        if (GUILayout.Button("Синхронизировать с MainMenu", EditorStyles.toolbarButton, GUILayout.Width(220)))
            SyncToScene();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPane()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(330));

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        _searchFilter = EditorGUILayout.TextField("Поиск", _searchFilter);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_searchFilter)))
        {
            if (GUILayout.Button("X", GUILayout.Width(26)))
                _searchFilter = string.Empty;
        }
        EditorGUILayout.EndHorizontal();

        _showOnlyIssues = EditorGUILayout.ToggleLeft("Показать только миссии с замечаниями", _showOnlyIssues);
        EditorGUILayout.Space(4);

        int visibleCount = 0;
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        for (int i = 0; i < _missions.Count; i++)
        {
            var mission = _missions[i];
            if (mission == null || !MissionPassesFilters(mission)) continue;
            visibleCount++;
            DrawMissionRow(mission);
        }

        if (visibleCount == 0)
            EditorGUILayout.HelpBox("По текущему фильтру ничего не найдено.", MessageType.Info);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Новая миссия", GUILayout.Height(28))) CreateNew();
        using (new EditorGUI.DisabledScope(_selected == null))
        {
            if (GUILayout.Button("Дублировать", GUILayout.Width(118), GUILayout.Height(28)))
                DuplicateSelected();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);

        EditorGUILayout.EndVertical();
    }

    private bool MissionPassesFilters(Mission mission)
    {
        if (_showOnlyIssues && GetWarnings(mission).Count == 0)
            return false;

        if (string.IsNullOrEmpty(_searchFilter))
            return true;

        return Contains(mission.title, _searchFilter)
            || Contains(mission.id, _searchFilter)
            || Contains(mission.sceneName, _searchFilter);
    }

    private static bool Contains(string source, string value) =>
        !string.IsNullOrEmpty(source)
        && source.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;

    private void DrawMissionRow(Mission mission)
    {
        bool isSelected = _selected == mission;
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = isSelected ? new Color(0.55f, 0.75f, 1f, 1f) : oldColor;

        var rowRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = oldColor;

        EditorGUILayout.BeginHorizontal();

        var status = MissionProgress.GetStatus(mission);
        var dotRect = GUILayoutUtility.GetRect(11, 11, GUILayout.Width(11), GUILayout.Height(20));
        dotRect.y += 4;
        dotRect.height = 11;
        EditorGUI.DrawRect(dotRect, GetStatusColor(status));

        EditorGUILayout.BeginVertical();
        var title = string.IsNullOrEmpty(mission.title) ? "<без названия>" : mission.title;
        EditorGUILayout.LabelField(title, isSelected ? EditorStyles.boldLabel : EditorStyles.label);
        EditorGUILayout.LabelField(BuildMissionMeta(mission), EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        var warnings = GetWarnings(mission);
        if (warnings.Count > 0)
        {
            var oldContentColor = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.72f, 0.32f);
            GUILayout.Label($"! {warnings.Count}", EditorStyles.boldLabel, GUILayout.Width(34));
            GUI.contentColor = oldContentColor;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
        {
            Select(mission);
            Event.current.Use();
        }
    }

    private static string BuildMissionMeta(Mission mission)
    {
        var id = string.IsNullOrEmpty(mission.id) ? "нет id" : mission.id;
        var scene = string.IsNullOrEmpty(mission.sceneName) ? "сцена не указана" : mission.sceneName;
        var prereq = mission.prerequisites == null ? 0 : mission.prerequisites.Length;
        return $"{id}  ·  {scene}  ·  условий: {prereq}";
    }

    private static Color GetStatusColor(MissionStatus status)
    {
        return status switch
        {
            MissionStatus.Completed => new Color(0.30f, 0.85f, 0.45f),
            MissionStatus.Locked => new Color(0.55f, 0.58f, 0.65f),
            _ => new Color(0.35f, 0.70f, 0.95f),
        };
    }

    private static string GetStatusLabel(MissionStatus status)
    {
        return status switch
        {
            MissionStatus.Completed => "Выполнена",
            MissionStatus.Locked => "Закрыта условиями",
            _ => "Доступна",
        };
    }

    private void DrawDivider()
    {
        var rect = GUILayoutUtility.GetRect(1, 0, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
    }

    private void DrawRightPane()
    {
        EditorGUILayout.BeginVertical();

        if (_selected == null || _selectedSO == null)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("Выберите миссию слева или создайте новую. После выбора здесь появятся поля, проверки и условия открытия.", MessageType.Info);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            return;
        }

        _selectedSO.Update();

        DrawSelectedHeader();

        _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);
        DrawIdentitySection();
        DrawBriefingSection();
        DrawRewardSection();
        DrawPrerequisitesSection();
        DrawSceneSection();

        if (_selectedSO.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(_selected);
            RefreshMissions();
        }

        DrawValidation();
        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();

        DrawRightPaneFooter();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(string.IsNullOrEmpty(_selected.title) ? "<без названия>" : _selected.title, EditorStyles.largeLabel);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(_selected), EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        var status = MissionProgress.GetStatus(_selected);
        var oldColor = GUI.contentColor;
        GUI.contentColor = GetStatusColor(status);
        GUILayout.Label(GetStatusLabel(status), EditorStyles.boldLabel, GUILayout.Width(130));
        GUI.contentColor = oldColor;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawIdentitySection()
    {
        BeginSection("Идентификация");

        var id = _selectedSO.FindProperty("id");
        var title = _selectedSO.FindProperty("title");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(id, new GUIContent("ID", "Уникальный ключ прогресса. Лучше использовать snake_case."));
        if (GUILayout.Button("Сгенерировать", GUILayout.Width(130)))
            id.stringValue = CreateUniqueId(MakeSlug(title.stringValue), _selected);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(title, new GUIContent("Название"));

        EndSection();
    }

    private void DrawBriefingSection()
    {
        BeginSection("Брифинг");

        var objective = _selectedSO.FindProperty("objective");
        EditorGUILayout.LabelField("Цель миссии", EditorStyles.miniBoldLabel);
        objective.stringValue = EditorGUILayout.TextArea(objective.stringValue ?? string.Empty, EditorStyles.textArea, GUILayout.MinHeight(54));

        EditorGUILayout.Space(4);

        var description = _selectedSO.FindProperty("description");
        EditorGUILayout.LabelField("Описание / брифинг", EditorStyles.miniBoldLabel);
        description.stringValue = EditorGUILayout.TextArea(description.stringValue ?? string.Empty, EditorStyles.textArea, GUILayout.MinHeight(104));

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{(objective.stringValue ?? string.Empty).Length} / {(description.stringValue ?? string.Empty).Length} символов", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EndSection();
    }

    private void DrawRewardSection()
    {
        BeginSection("Награда");

        var reward = _selectedSO.FindProperty("rewardScience");
        reward.intValue = EditorGUILayout.IntSlider(new GUIContent("Очки науки (SCI)"), reward.intValue, 0, 200);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Быстро:", GUILayout.Width(52));
        int[] presets = { 0, 10, 25, 50, 100, 200 };
        foreach (var value in presets)
        {
            if (GUILayout.Button(value.ToString(), GUILayout.Width(44)))
                reward.intValue = value;
        }
        EditorGUILayout.EndHorizontal();

        EndSection();
    }

    private void DrawPrerequisitesSection()
    {
        BeginSection("Условия открытия");

        var prereqProp = _selectedSO.FindProperty("prerequisites");
        EditorGUILayout.HelpBox("Отметьте миссии, которые должны быть выполнены перед открытием текущей. Ручной массив больше трогать не нужно.", MessageType.None);

        int available = 0;
        for (int i = 0; i < _missions.Count; i++)
        {
            var mission = _missions[i];
            if (mission == null || mission == _selected) continue;
            available++;

            bool has = HasPrerequisite(prereqProp, mission);
            var label = $"{(string.IsNullOrEmpty(mission.title) ? mission.id : mission.title)}  ({mission.id})";
            bool next = EditorGUILayout.ToggleLeft(label, has);
            if (next != has)
            {
                if (next) AddPrerequisite(prereqProp, mission);
                else RemovePrerequisite(prereqProp, mission);
            }
        }

        if (available == 0)
            EditorGUILayout.LabelField("Других миссий пока нет.", EditorStyles.miniLabel);

        using (new EditorGUI.DisabledScope(prereqProp.arraySize == 0))
        {
            if (GUILayout.Button("Очистить условия", GUILayout.Width(140)))
                prereqProp.ClearArray();
        }

        EndSection();
    }

    private void DrawSceneSection()
    {
        BeginSection("Запуск");

        var sceneProp = _selectedSO.FindProperty("sceneName");
        var sceneNames = GetBuildSceneNames();

        if (sceneNames.Count == 0)
        {
            EditorGUILayout.HelpBox("В Build Settings нет игровых сцен.", MessageType.Warning);
            sceneProp.stringValue = EditorGUILayout.TextField("Имя сцены", sceneProp.stringValue ?? string.Empty);
            EndSection();
            return;
        }

        int currentIndex = sceneNames.IndexOf(sceneProp.stringValue);
        if (currentIndex < 0)
        {
            sceneNames.Insert(0, string.IsNullOrEmpty(sceneProp.stringValue) ? "<не выбрано>" : sceneProp.stringValue);
            currentIndex = 0;
        }

        int newIndex = EditorGUILayout.Popup("Сцена", currentIndex, sceneNames.ToArray());
        if (newIndex != currentIndex && newIndex >= 0 && newIndex < sceneNames.Count)
            sceneProp.stringValue = sceneNames[newIndex] == "<не выбрано>" ? string.Empty : sceneNames[newIndex];

        sceneProp.stringValue = EditorGUILayout.TextField("Точное имя", sceneProp.stringValue ?? string.Empty);

        EndSection();
    }

    private void DrawValidation()
    {
        var warnings = GetWarnings(_selected);
        if (warnings.Count == 0)
        {
            EditorGUILayout.HelpBox("Миссия выглядит готовой: id уникален, название и сцена заполнены, условия не конфликтуют.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Замечания", EditorStyles.boldLabel);
        foreach (var warning in warnings)
            EditorGUILayout.HelpBox(warning, MessageType.Warning);
    }

    private List<string> GetWarnings(Mission mission)
    {
        var warnings = new List<string>();
        if (mission == null) return warnings;

        if (string.IsNullOrWhiteSpace(mission.id))
            warnings.Add("ID пустой: прогресс не сохранится корректно.");
        else if (mission.id.Contains(" "))
            warnings.Add("ID содержит пробел. Лучше использовать snake_case.");

        for (int i = 0; i < _missions.Count; i++)
        {
            var other = _missions[i];
            if (other == null || other == mission) continue;
            if (!string.IsNullOrEmpty(other.id) && other.id == mission.id)
            {
                warnings.Add($"ID '{mission.id}' уже используется в другой миссии.");
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(mission.title))
            warnings.Add("Название пустое.");

        if (string.IsNullOrWhiteSpace(mission.objective))
            warnings.Add("Цель миссии пустая: пользователю будет непонятно, что делать.");

        if (string.IsNullOrWhiteSpace(mission.sceneName))
            warnings.Add("Сцена не указана: миссия не запустится.");
        else if (!GetBuildSceneNames().Contains(mission.sceneName))
            warnings.Add($"Сцена '{mission.sceneName}' не добавлена в Build Settings.");

        if (mission.prerequisites != null)
        {
            for (int i = 0; i < mission.prerequisites.Length; i++)
            {
                if (mission.prerequisites[i] == mission)
                {
                    warnings.Add("Миссия не может быть собственным условием открытия.");
                    break;
                }
            }
        }

        return warnings;
    }

    private void DrawRightPaneFooter()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Показать в Project", EditorStyles.toolbarButton, GUILayout.Width(130)))
        {
            EditorGUIUtility.PingObject(_selected);
            Selection.activeObject = _selected;
        }

        if (GUILayout.Button("Сохранить assets", EditorStyles.toolbarButton, GUILayout.Width(120)))
            AssetDatabase.SaveAssets();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Сбросить прогресс", EditorStyles.toolbarButton, GUILayout.Width(130)))
            ResetProgressForSelected();

        var oldColor = GUI.color;
        GUI.color = new Color(1f, 0.62f, 0.62f);
        if (GUILayout.Button("Удалить", EditorStyles.toolbarButton, GUILayout.Width(90)))
            DeleteSelected();
        GUI.color = oldColor;

        EditorGUILayout.EndHorizontal();
    }

    private static void BeginSection(string title)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private static void EndSection()
    {
        EditorGUILayout.EndVertical();
    }

    private static bool HasPrerequisite(SerializedProperty prereqProp, Mission mission)
    {
        for (int i = 0; i < prereqProp.arraySize; i++)
        {
            if (prereqProp.GetArrayElementAtIndex(i).objectReferenceValue == mission)
                return true;
        }
        return false;
    }

    private static void AddPrerequisite(SerializedProperty prereqProp, Mission mission)
    {
        int index = prereqProp.arraySize;
        prereqProp.InsertArrayElementAtIndex(index);
        prereqProp.GetArrayElementAtIndex(index).objectReferenceValue = mission;
    }

    private static void RemovePrerequisite(SerializedProperty prereqProp, Mission mission)
    {
        for (int i = prereqProp.arraySize - 1; i >= 0; i--)
        {
            if (prereqProp.GetArrayElementAtIndex(i).objectReferenceValue == mission)
                prereqProp.DeleteArrayElementAtIndex(i);
        }
    }

    private void CreateNew()
    {
        EnsureFolder(MissionsFolder);

        string id = CreateUniqueId("mission_new", null);
        var asset = ScriptableObject.CreateInstance<Mission>();
        asset.id = id;
        asset.title = "Новая миссия";
        asset.objective = "Опиши цель миссии.";
        asset.description = "Подробный брифинг.";
        asset.rewardScience = 25;
        asset.sceneName = GetBuildSceneNames().Count > 0 ? GetBuildSceneNames()[0] : "SampleScene";
        asset.prerequisites = new Mission[0];

        var path = AssetDatabase.GenerateUniqueAssetPath($"{MissionsFolder}/Mission_{id}.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        RefreshMissions();
        Select(asset);
    }

    private void DuplicateSelected()
    {
        if (_selected == null) return;

        var srcPath = AssetDatabase.GetAssetPath(_selected);
        var dstPath = AssetDatabase.GenerateUniqueAssetPath(srcPath);
        if (!AssetDatabase.CopyAsset(srcPath, dstPath))
        {
            EditorUtility.DisplayDialog("Ошибка", "Не удалось скопировать миссию.", "OK");
            return;
        }

        var copy = AssetDatabase.LoadAssetAtPath<Mission>(dstPath);
        if (copy != null)
        {
            copy.id = CreateUniqueId($"{_selected.id}_copy", copy);
            copy.title = $"{(_selected.title ?? "Миссия")} (копия)";
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();
        }

        RefreshMissions();
        Select(copy);
    }

    private void DeleteSelected()
    {
        if (_selected == null) return;
        if (!EditorUtility.DisplayDialog("Удалить миссию",
            $"Удалить '{_selected.title}'?\nЭто действие нельзя отменить через окно конструктора.",
            "Удалить", "Отмена"))
            return;

        var path = AssetDatabase.GetAssetPath(_selected);
        AssetDatabase.DeleteAsset(path);
        Select(null);
        RefreshMissions();
    }

    private void ResetProgressForSelected()
    {
        if (_selected == null) return;
        if (!EditorUtility.DisplayDialog("Сброс прогресса",
            $"Снять отметку выполнения для миссии '{_selected.title}'?", "Сбросить", "Отмена"))
            return;

        if (!string.IsNullOrEmpty(_selected.id))
        {
            PlayerPrefs.DeleteKey("cosma_mission_done_" + _selected.id);
            PlayerPrefs.Save();
        }

        Repaint();
    }

    private void SyncToScene()
    {
        if (!File.Exists(MainMenuScenePath))
        {
            EditorUtility.DisplayDialog("Сцена не найдена",
                $"Сцена {MainMenuScenePath} не существует.\nСоздай ее через COSMA -> Create Main Menu Scene.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var bootstrap = FindFirstObjectByType<MainMenuBootstrap>();
        if (bootstrap == null)
        {
            EditorUtility.DisplayDialog("Не найден MainMenuBootstrap",
                "В сцене MainMenu нет объекта с компонентом MainMenuBootstrap.",
                "OK");
            return;
        }

        var so = new SerializedObject(bootstrap);
        var missionsProp = so.FindProperty("_missions");
        missionsProp.arraySize = _missions.Count;
        for (int i = 0; i < _missions.Count; i++)
            missionsProp.GetArrayElementAtIndex(i).objectReferenceValue = _missions[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Синхронизация",
            $"Записано миссий в MainMenu: {_missions.Count}", "OK");
    }

    private static List<string> GetBuildSceneNames()
    {
        var list = new List<string>();
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i] == null || string.IsNullOrEmpty(scenes[i].path)) continue;
            var name = Path.GetFileNameWithoutExtension(scenes[i].path);
            if (name == "MainMenu") continue;
            if (!list.Contains(name)) list.Add(name);
        }
        return list;
    }

    private string CreateUniqueId(string baseId, Mission ignore)
    {
        baseId = string.IsNullOrWhiteSpace(baseId) ? "mission" : baseId;
        string id = baseId;
        int suffix = 1;
        while (_missions.Exists(m => m != null && m != ignore && m.id == id))
            id = $"{baseId}_{suffix++}";
        return id;
    }

    private static string MakeSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "mission";

        var sb = new StringBuilder(value.Length);
        bool lastWasSeparator = false;

        foreach (char raw in value.Trim())
        {
            string chunk = Transliterate(raw);
            if (string.IsNullOrEmpty(chunk))
            {
                if (!lastWasSeparator && sb.Length > 0)
                {
                    sb.Append('_');
                    lastWasSeparator = true;
                }
                continue;
            }

            sb.Append(chunk);
            lastWasSeparator = false;
        }

        var result = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(result) ? "mission" : result;
    }

    private static string Transliterate(char value)
    {
        char c = char.ToLowerInvariant(value);
        if (c >= 'a' && c <= 'z') return c.ToString();
        if (c >= '0' && c <= '9') return c.ToString();

        return c switch
        {
            'а' => "a",
            'б' => "b",
            'в' => "v",
            'г' => "g",
            'д' => "d",
            'е' => "e",
            'ё' => "e",
            'ж' => "zh",
            'з' => "z",
            'и' => "i",
            'й' => "y",
            'к' => "k",
            'л' => "l",
            'м' => "m",
            'н' => "n",
            'о' => "o",
            'п' => "p",
            'р' => "r",
            'с' => "s",
            'т' => "t",
            'у' => "u",
            'ф' => "f",
            'х' => "h",
            'ц' => "ts",
            'ч' => "ch",
            'ш' => "sh",
            'щ' => "sch",
            'ы' => "y",
            'э' => "e",
            'ю' => "yu",
            'я' => "ya",
            _ => string.Empty,
        };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
