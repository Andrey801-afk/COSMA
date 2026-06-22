using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProgrammingCommandPaletteAutoSync
{
    private const string CommandsFolder = "Assets/_Project/Commands";
    private const string CommandPoolContainerName = "CommandPoolItemsContainer";
    private const string CommandPoolViewportName = "CommandPoolScrollViewport";

    static ProgrammingCommandPaletteAutoSync()
    {
        EditorApplication.delayCall += SyncOpenScenePalette;
        EditorSceneManager.sceneOpened += HandleSceneOpened;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("COSMA/Sync Command Palette")]
    public static void SyncCommandPaletteMenu()
    {
        SyncOpenScenePalette();
    }

    private static void HandleSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += SyncOpenScenePalette;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += SyncOpenScenePalette;
        }
    }

    private static void SyncOpenScenePalette()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject containerObject = GameObject.Find(CommandPoolContainerName);
        if (containerObject == null)
        {
            return;
        }

        List<CommandDefinition> definitions = LoadCommandDefinitions();
        if (definitions.Count == 0)
        {
            return;
        }

        EnsureScrollableContainer(containerObject);

        CommandPoolItemView templateView = containerObject.GetComponentInChildren<CommandPoolItemView>(true);
        if (templateView == null)
        {
            return;
        }

        bool changed = false;
        foreach (CommandDefinition definition in definitions)
        {
            if (definition == null)
            {
                continue;
            }

            string itemName = $"CommandPoolItem_{definition.Type}";
            GameObject existingItem = GameObject.Find(itemName);
            if (existingItem != null)
            {
                RefreshDefinition(existingItem, definition);
                continue;
            }

            GameObject clonedItem = Object.Instantiate(templateView.gameObject, containerObject.transform);
            clonedItem.name = itemName;
            clonedItem.transform.SetAsLastSibling();

            CommandPoolItemView clonedView = clonedItem.GetComponent<CommandPoolItemView>();
            SerializedObject serializedView = new SerializedObject(clonedView);
            serializedView.FindProperty("definition").objectReferenceValue = definition;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            RefreshDefinition(clonedItem, definition);
            EditorUtility.SetDirty(clonedItem);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(containerObject.scene);
        }
    }

    private static void EnsureScrollableContainer(GameObject containerObject)
    {
        if (containerObject == null)
        {
            return;
        }

        RectTransform content = containerObject.transform as RectTransform;
        RectTransform oldParent = content != null ? content.parent as RectTransform : null;
        if (content == null || oldParent == null)
        {
            return;
        }

        if (oldParent.name == CommandPoolViewportName)
        {
            EnsureViewportComponents(oldParent, content);
            EnsureContentLayout(content);
            return;
        }

        int siblingIndex = content.GetSiblingIndex();
        GameObject viewportObject = new GameObject(CommandPoolViewportName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(viewportObject, "Create command pool viewport");
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
            Object.DestroyImmediate(oldLayout);
        }
        else
        {
            viewportLayout.minHeight = 160f;
            viewportLayout.flexibleHeight = 1f;
        }

        content.SetParent(viewport, false);
        EnsureViewportComponents(viewport, content);
        EnsureContentLayout(content);
        EditorUtility.SetDirty(viewportObject);
        EditorUtility.SetDirty(containerObject);
        EditorSceneManager.MarkSceneDirty(containerObject.scene);
    }

    private static void EnsureViewportComponents(RectTransform viewport, RectTransform content)
    {
        LayoutElement viewportElement = viewport.GetComponent<LayoutElement>();
        if (viewportElement == null)
        {
            viewportElement = viewport.gameObject.AddComponent<LayoutElement>();
        }
        viewportElement.minHeight = 160f;
        viewportElement.flexibleHeight = 1f;

        Image image = viewport.GetComponent<Image>();
        if (image == null)
        {
            image = viewport.gameObject.AddComponent<Image>();
        }

        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        ScrollRect scrollRect = viewport.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.elasticity = 0.08f;
        scrollRect.scrollSensitivity = 0f;
        scrollRect.verticalScrollbar = EnsureScrollbar(viewport);
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        CommandPaletteContentSizer contentSizer = viewport.GetComponent<CommandPaletteContentSizer>();
        if (contentSizer == null)
        {
            contentSizer = viewport.gameObject.AddComponent<CommandPaletteContentSizer>();
        }
        contentSizer.Configure(viewport, content);

        CommandPaletteSmoothScrollWheel smoothWheel = viewport.GetComponent<CommandPaletteSmoothScrollWheel>();
        if (smoothWheel == null)
        {
            smoothWheel = viewport.gameObject.AddComponent<CommandPaletteSmoothScrollWheel>();
        }

        smoothWheel.Configure(scrollRect, true);
    }

    private static void EnsureContentLayout(RectTransform content)
    {
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = Vector2.zero;
        content.offsetMax = new Vector2(-10f, 0f);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CommandPoolItemView[] itemViews = content.GetComponentsInChildren<CommandPoolItemView>(true);
        for (int i = 0; i < itemViews.Length; i++)
        {
            LayoutElement itemElement = itemViews[i].GetComponent<LayoutElement>();
            if (itemElement == null)
            {
                itemElement = itemViews[i].gameObject.AddComponent<LayoutElement>();
            }

            itemElement.minHeight = Mathf.Max(58f, itemElement.minHeight);
            itemElement.preferredHeight = Mathf.Max(58f, itemElement.preferredHeight);
            itemElement.flexibleHeight = 0f;
        }

        EditorUtility.SetDirty(content);
    }

    private static Scrollbar EnsureScrollbar(RectTransform viewport)
    {
        Transform existing = viewport.Find("CommandPoolScrollbar");
        GameObject root = existing != null ? existing.gameObject : new GameObject("CommandPoolScrollbar", typeof(RectTransform));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(root, "Create command pool scrollbar");
            root.transform.SetParent(viewport, false);
        }

        SetStretch(root, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-7f, 4f), new Vector2(-1f, -4f));

        Image background = root.GetComponent<Image>();
        if (background == null)
        {
            background = root.AddComponent<Image>();
        }
        background.color = new Color(0.08f, 0.09f, 0.10f, 0.48f);

        Scrollbar scrollbar = root.GetComponent<Scrollbar>();
        if (scrollbar == null)
        {
            scrollbar = root.AddComponent<Scrollbar>();
        }
        scrollbar.direction = Scrollbar.Direction.TopToBottom;

        Transform sliding = root.transform.Find("Sliding Area");
        GameObject slidingArea = sliding != null ? sliding.gameObject : new GameObject("Sliding Area", typeof(RectTransform));
        if (sliding == null)
        {
            slidingArea.transform.SetParent(root.transform, false);
        }
        SetStretch(slidingArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Transform handleTransform = slidingArea.transform.Find("Handle");
        GameObject handle = handleTransform != null ? handleTransform.gameObject : new GameObject("Handle", typeof(RectTransform));
        if (handleTransform == null)
        {
            handle.transform.SetParent(slidingArea.transform, false);
        }
        SetStretch(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image handleImage = handle.GetComponent<Image>();
        if (handleImage == null)
        {
            handleImage = handle.AddComponent<Image>();
        }
        handleImage.color = new Color(0.82f, 0.70f, 0.54f, 0.82f);

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = (RectTransform)handle.transform;
        EditorUtility.SetDirty(root);
        return scrollbar;
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

    private static List<CommandDefinition> LoadCommandDefinitions()
    {
        List<CommandDefinition> definitions = new List<CommandDefinition>();
        string[] guids = AssetDatabase.FindAssets("t:CommandDefinition", new[] { CommandsFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CommandDefinition definition = AssetDatabase.LoadAssetAtPath<CommandDefinition>(path);
            if (definition != null && !IsLegacyAliasAsset(path, definition))
            {
                definitions.Add(definition);
            }
        }

        definitions.Sort((a, b) => a.Type.CompareTo(b.Type));
        return definitions;
    }

    private static bool IsLegacyAliasAsset(string path, CommandDefinition definition)
    {
        string assetName = Path.GetFileNameWithoutExtension(path);
        return (definition.Type == CommandType.RotateToEarth && assetName != nameof(CommandType.RotateToEarth)) ||
               (definition.Type == CommandType.RotateToSun && assetName != nameof(CommandType.RotateToSun));
    }

    private static void RefreshDefinition(GameObject itemObject, CommandDefinition definition)
    {
        if (itemObject == null || definition == null)
        {
            return;
        }

        CommandPoolItemView view = itemObject.GetComponent<CommandPoolItemView>();
        if (view == null)
        {
            return;
        }

        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("definition").objectReferenceValue = definition;
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        view.SetDefinition(definition);

        EditorUtility.SetDirty(itemObject);
        EditorSceneManager.MarkSceneDirty(itemObject.scene);
    }
}
