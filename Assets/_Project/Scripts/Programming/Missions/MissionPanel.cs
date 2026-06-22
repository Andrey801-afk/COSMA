using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class MissionPanel : MonoBehaviour
{
    private const float SidePadding = 18f;
    private const float TitleTop = 18f;
    private const float TitleHeight = 34f;
    private const float DescriptionTop = 62f;
    private const float DescriptionHeight = 42f;
    private const float HeaderTop = 122f;
    private const float HeaderHeight = 22f;
    private const float ObjectivesTop = 150f;
    private const float ObjectivesHeight = 82f;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text objectivesText;
    [SerializeField] private TMP_Text completedText;

    public void Configure(
        TMP_Text missionTitle,
        TMP_Text missionDescription,
        TMP_Text objectiveList,
        TMP_Text completionLabel)
    {
        titleText = missionTitle;
        descriptionText = missionDescription;
        objectivesText = objectiveList;
        completedText = completionLabel;
        ApplyLayout();
    }

    public void Render(MissionDefinition definition, IReadOnlyList<MissionObjective> objectives, bool missionCompleted)
    {
        ApplyLayout();

        if (titleText != null)
        {
            titleText.text = definition != null && !string.IsNullOrWhiteSpace(definition.missionName)
                ? definition.missionName
                : "МИССИЯ";
        }

        if (descriptionText != null)
        {
            descriptionText.text = definition != null ? definition.missionDescription : string.Empty;
        }

        if (objectivesText != null)
        {
            objectivesText.text = BuildObjectivesText(objectives);
        }

        if (completedText != null)
        {
            completedText.text = missionCompleted ? "МИССИЯ ВЫПОЛНЕНА" : "КЛЮЧЕВЫЕ ЗАДАЧИ";
            completedText.color = missionCompleted
                ? new Color(0.60f, 0.95f, 0.58f, 1f)
                : new Color(0.82f, 0.70f, 0.54f, 1f);
        }
    }

    private static string BuildObjectivesText(IReadOnlyList<MissionObjective> objectives)
    {
        if (objectives == null || objectives.Count == 0)
        {
            return "<color=#89909A>[ ]</color> Цели не заданы";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjective objective = objectives[i];
            if (objective == null)
            {
                continue;
            }

            string marker = objective.isCompleted ? "<color=#8FE28A>[x]</color>" : "<color=#89909A>[ ]</color>";
            string label = !string.IsNullOrWhiteSpace(objective.displayName)
                ? objective.displayName
                : objective.objectiveType.ToString();

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(marker).Append(' ').Append(label);
        }

        return builder.ToString();
    }

    private void ApplyLayout()
    {
        DisableDuplicateTitleLayers();

        ConfigureText(titleText, 21f, TextOverflowModes.Ellipsis, TextWrappingModes.NoWrap, 0f);
        ConfigureText(descriptionText, 13f, TextOverflowModes.Truncate, TextWrappingModes.Normal, 0f);
        ConfigureText(completedText, 13f, TextOverflowModes.Ellipsis, TextWrappingModes.NoWrap, 1f);
        ConfigureText(objectivesText, 13.6f, TextOverflowModes.Truncate, TextWrappingModes.NoWrap, -4f);

        SetTopBand(titleText, TitleTop, TitleHeight);
        SetTopBand(descriptionText, DescriptionTop, DescriptionHeight);
        SetTopBand(completedText, HeaderTop, HeaderHeight);
        SetTopBand(objectivesText, ObjectivesTop, ObjectivesHeight);
    }

    private void DisableDuplicateTitleLayers()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
        }

        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || label == titleText)
            {
                continue;
            }

            if (label.name.StartsWith("PanelTitle") || label.name.StartsWith("MissionTitleText"))
            {
                label.gameObject.SetActive(false);
            }
        }
    }

    private static void ConfigureText(
        TMP_Text text,
        float fontSize,
        TextOverflowModes overflowMode,
        TextWrappingModes wrappingMode,
        float lineSpacing)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.overflowMode = overflowMode;
        text.textWrappingMode = wrappingMode;
        text.lineSpacing = lineSpacing;
        text.characterSpacing = 0f;
        text.margin = Vector4.zero;
    }

    private static void SetTopBand(TMP_Text text, float top, float height)
    {
        if (text == null || text.rectTransform == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        RectTransform parent = rect.parent as RectTransform;
        float panelHeight = parent != null && parent.rect.height > 1f ? parent.rect.height : 360f;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(SidePadding, Mathf.Max(0f, panelHeight - top - height));
        rect.offsetMax = new Vector2(-SidePadding, -top);
    }
}
