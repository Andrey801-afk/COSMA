using System.Collections.Generic;
using UnityEngine;

public sealed class MissionSystem : MonoBehaviour
{
    public static event System.Action<MissionSystem> MissionCompleted;

    [SerializeField] private MissionDefinition missionDefinition;
    [SerializeField] private MissionPanel missionPanel;
    [SerializeField] private SatelliteStateController satelliteStateController;
    [SerializeField] private bool markMissionContextOnCompletion = true;

    private readonly List<MissionObjective> runtimeObjectives = new();
    private MissionDefinition runtimeSource;
    private MissionDefinition contextRuntimeDefinition;
    private string contextRuntimeKey;

    public MissionDefinition Definition => missionDefinition;
    public IReadOnlyList<MissionObjective> Objectives => runtimeObjectives;
    public bool IsCompleted { get; private set; }

    public void Configure(
        MissionDefinition definition,
        MissionPanel panel,
        SatelliteStateController stateController)
    {
        missionDefinition = definition;
        missionPanel = panel;
        satelliteStateController = stateController;
        ResetProgress();
    }

    private void OnEnable()
    {
        MissionHudWidget.EnsureSpawned();
        EnsureReferences();
        ApplyMissionContextOverride();
        if (satelliteStateController != null)
        {
            satelliteStateController.StateChanged += HandleStateChanged;
        }

        EnsureRuntimeObjectives();
        Render();
    }

    private void OnDisable()
    {
        if (satelliteStateController != null)
        {
            satelliteStateController.StateChanged -= HandleStateChanged;
        }
    }

    private void OnDestroy()
    {
        DestroyContextRuntimeDefinition();
    }

    public void SetMission(MissionDefinition definition)
    {
        DestroyContextRuntimeDefinition();
        missionDefinition = definition;
        ResetProgress();
    }

    public void ResetProgress()
    {
        runtimeSource = null;
        runtimeObjectives.Clear();
        IsCompleted = false;
        EnsureRuntimeObjectives();
        Render();
    }

    public void CheckObjectives(SatelliteState state)
    {
        EnsureRuntimeObjectives();
        if (state == null || runtimeObjectives.Count == 0)
        {
            Render();
            return;
        }

        bool changed = false;
        for (int i = 0; i < runtimeObjectives.Count; i++)
        {
            MissionObjective objective = runtimeObjectives[i];
            if (objective == null || objective.isCompleted)
            {
                continue;
            }

            bool completedNow = EvaluateObjective(objective, state);
            if (!completedNow)
            {
                continue;
            }

            objective.isCompleted = true;
            changed = true;
        }

        bool completed = runtimeObjectives.Count > 0 && AreAllObjectivesCompleted();
        if (completed != IsCompleted)
        {
            IsCompleted = completed;
            changed = true;

            if (IsCompleted)
            {
                if (markMissionContextOnCompletion && MissionContext.HasAny)
                {
                    MissionContext.MarkCurrentCompleted();
                }

                MissionCompleted?.Invoke(this);
            }
        }

        if (changed)
        {
            Render();
        }
    }

    private void HandleStateChanged(SatelliteState state, string message)
    {
        CheckObjectives(state);
    }

    private void EnsureReferences()
    {
        if (missionPanel == null)
        {
            missionPanel = GetComponent<MissionPanel>();
        }

        if (satelliteStateController == null)
        {
            satelliteStateController = GetComponent<SatelliteStateController>();
        }

        if (satelliteStateController == null)
        {
            satelliteStateController = FindFirstObjectByType<SatelliteStateController>(FindObjectsInactive.Include);
        }
    }

    private void EnsureRuntimeObjectives()
    {
        if (runtimeSource == missionDefinition && runtimeObjectives.Count > 0)
        {
            return;
        }

        runtimeSource = missionDefinition;
        runtimeObjectives.Clear();

        if (missionDefinition == null || missionDefinition.requiredObjectives == null)
        {
            return;
        }

        for (int i = 0; i < missionDefinition.requiredObjectives.Count; i++)
        {
            MissionObjective source = missionDefinition.requiredObjectives[i];
            if (source == null)
            {
                continue;
            }

            runtimeObjectives.Add(source.CloneForRuntime());
        }
    }

    private void ApplyMissionContextOverride()
    {
        MissionDefinition contextDefinition = ResolveMissionContextDefinition();
        if (contextDefinition == null || missionDefinition == contextDefinition)
        {
            return;
        }

        missionDefinition = contextDefinition;
        runtimeSource = null;
        runtimeObjectives.Clear();
        IsCompleted = false;
    }

    private MissionDefinition ResolveMissionContextDefinition()
    {
        if (!MissionContext.HasAny)
        {
            return null;
        }

        string id = MissionContext.CurrentId;
        string key = $"{(MissionContext.IsUserMade ? "user" : "built-in")}:{id}";
        if (contextRuntimeDefinition != null && contextRuntimeKey == key)
        {
            return contextRuntimeDefinition;
        }

        DestroyContextRuntimeDefinition();
        contextRuntimeDefinition = ScriptableObject.CreateInstance<MissionDefinition>();
        contextRuntimeDefinition.hideFlags = HideFlags.HideAndDontSave;
        contextRuntimeKey = key;

        if (missionDefinition != null)
        {
            contextRuntimeDefinition.maxProgramLines = missionDefinition.maxProgramLines;
            if (missionDefinition.availableCommands != null)
            {
                contextRuntimeDefinition.availableCommands = new List<CommandDefinition>(missionDefinition.availableCommands);
            }
        }

        if (MissionContext.CurrentUserMission != null)
        {
            FillFromUserMission(contextRuntimeDefinition, MissionContext.CurrentUserMission);
        }
        else if (MissionContext.Current != null)
        {
            FillFromBuiltInMission(contextRuntimeDefinition, MissionContext.Current);
        }

        return contextRuntimeDefinition;
    }

    private void FillFromBuiltInMission(MissionDefinition target, Mission mission)
    {
        target.missionName = string.IsNullOrWhiteSpace(mission.title) ? "Миссия" : mission.title;
        target.missionDescription = PickDescription(mission.description, mission.objective);
        target.requiredObjectives.Clear();

        AddBuiltInMissionObjectives(mission, target.requiredObjectives);
        if (target.requiredObjectives.Count == 0)
        {
            AddInferredObjectives($"{mission.title} {mission.objective} {mission.description}", target.requiredObjectives);
        }
    }

    private void FillFromUserMission(MissionDefinition target, UserMission mission)
    {
        mission.EnsureDefaults();
        target.missionName = string.IsNullOrWhiteSpace(mission.title) ? "Своя миссия" : mission.title;
        target.missionDescription = PickDescription(mission.description, mission.objective);
        target.requiredObjectives.Clear();

        if (mission.conditions != null)
        {
            for (int i = 0; i < mission.conditions.Count; i++)
            {
                AddConditionObjective(mission.conditions[i], target.requiredObjectives);
            }
        }

        if (target.requiredObjectives.Count == 0)
        {
            AddInferredObjectives($"{mission.title} {mission.objective} {mission.description}", target.requiredObjectives);
        }
    }

    private static string PickDescription(string description, string objective)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        return string.IsNullOrWhiteSpace(objective) ? string.Empty : objective;
    }

    private static void AddBuiltInMissionObjectives(Mission mission, List<MissionObjective> objectives)
    {
        string id = mission.id ?? string.Empty;
        switch (id.ToLowerInvariant())
        {
            case "stabilize":
                AddUniqueObjective(objectives, MissionObjectiveType.PowerEnabled, "Включить питание");
                AddUniqueObjective(objectives, MissionObjectiveType.GyrosCalibrated, "Калибровать гироскопы");
                AddUniqueObjective(objectives, MissionObjectiveType.SatelliteStabilized, "Стабилизировать спутник");
                break;

            case "solar_lock":
                AddUniqueObjective(objectives, MissionObjectiveType.PowerEnabled, "Включить питание");
                AddUniqueObjective(objectives, MissionObjectiveType.SunDataCollected, "Считать солнечный датчик");
                AddUniqueObjective(objectives, MissionObjectiveType.SatelliteFacingSun, "Повернуться к Солнцу");
                break;

            case "full_orbit":
                AddUniqueObjective(objectives, MissionObjectiveType.PowerEnabled, "Включить питание");
                AddUniqueObjective(objectives, MissionObjectiveType.EarthDataCollected, "Считать магнитометр");
                AddUniqueObjective(objectives, MissionObjectiveType.GyrosCalibrated, "Калибровать гироскопы");
                AddUniqueObjective(objectives, MissionObjectiveType.SatelliteStabilized, "Стабилизировать спутник");
                AddUniqueObjective(objectives, MissionObjectiveType.CommunicationLinkAvailable, "Подготовить связь с Землей");
                break;

            case "command_seq":
                AddUniqueObjective(objectives, MissionObjectiveType.PowerEnabled, "Включить питание");
                AddUniqueObjective(objectives, MissionObjectiveType.EarthDataCollected, "Считать магнитометр");
                AddUniqueObjective(objectives, MissionObjectiveType.SatelliteFacingEarth, "Повернуться к Земле");
                AddUniqueObjective(objectives, MissionObjectiveType.CameraCoverOpen, "Открыть крышку камеры");
                AddUniqueObjective(objectives, MissionObjectiveType.PhotoTaken, "Сделать снимок");
                AddUniqueObjective(objectives, MissionObjectiveType.DataSent, "Отправить сообщение");
                break;
        }
    }

    private static void AddInferredObjectives(string source, List<MissionObjective> objectives)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        string text = source.ToLowerInvariant();
        if (text.Contains("питан"))
        {
            AddUniqueObjective(objectives, MissionObjectiveType.PowerEnabled, "Включить питание");
        }

        if (text.Contains("солн"))
        {
            AddUniqueObjective(objectives, MissionObjectiveType.SatelliteFacingSun, "Повернуться к Солнцу");
        }

        if (text.Contains("зем"))
        {
            AddUniqueObjective(objectives, MissionObjectiveType.SatelliteFacingEarth, "Повернуться к Земле");
        }

        if (text.Contains("стабил"))
        {
            AddUniqueObjective(objectives, MissionObjectiveType.SatelliteStabilized, "Стабилизировать спутник");
        }

        if (text.Contains("фото") || text.Contains("сним"))
        {
            AddUniqueObjective(objectives, MissionObjectiveType.PhotoTaken, "Сделать снимок");
        }

        if (text.Contains("отправ") || text.Contains("данн") || text.Contains("сообщ") || text.Contains("связ"))
        {
            AddUniqueObjective(objectives, MissionObjectiveType.DataSent, "Отправить сообщение");
        }
    }

    private static void AddConditionObjective(MissionConditionData condition, List<MissionObjective> objectives)
    {
        if (condition == null)
        {
            return;
        }

        switch (condition.conditionType)
        {
            case MissionConditionType.PowerEnabled:
                AddUniqueObjective(objectives, MissionObjectiveType.PowerEnabled, "Включить питание");
                break;
            case MissionConditionType.FacingEarth:
                AddUniqueObjective(objectives, MissionObjectiveType.SatelliteFacingEarth, "Повернуться к Земле");
                break;
            case MissionConditionType.FacingSun:
                AddUniqueObjective(objectives, MissionObjectiveType.SatelliteFacingSun, "Повернуться к Солнцу");
                break;
            case MissionConditionType.Stabilized:
                AddUniqueObjective(objectives, MissionObjectiveType.SatelliteStabilized, "Стабилизировать спутник");
                break;
            case MissionConditionType.PhotoTaken:
                AddUniqueObjective(objectives, MissionObjectiveType.PhotoTaken, "Сделать снимок");
                break;
            case MissionConditionType.DataSent:
                AddUniqueObjective(objectives, MissionObjectiveType.DataSent, "Отправить сообщение");
                break;
            case MissionConditionType.BatteryAbovePercent:
                int batteryTarget = condition.value > 0 ? Mathf.Clamp(condition.value, 1, 100) : 95;
                string batteryLabel = condition.value > 0
                    ? $"Зарядить батарею до {batteryTarget}%"
                    : "Зарядить батарею";
                AddUniqueObjective(objectives, MissionObjectiveType.BatteryCharged, batteryLabel, batteryTarget);
                break;
        }
    }

    private static void AddUniqueObjective(
        List<MissionObjective> objectives,
        MissionObjectiveType objectiveType,
        string displayName,
        int targetValue = 0)
    {
        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjective objective = objectives[i];
            if (objective != null && objective.objectiveType == objectiveType)
            {
                if (targetValue > objective.targetValue)
                {
                    objective.displayName = displayName;
                    objective.targetValue = targetValue;
                }

                return;
            }
        }

        objectives.Add(new MissionObjective(objectiveType, displayName, targetValue));
    }

    private void DestroyContextRuntimeDefinition()
    {
        if (contextRuntimeDefinition == null)
        {
            contextRuntimeKey = null;
            return;
        }

        MissionDefinition definition = contextRuntimeDefinition;
        contextRuntimeDefinition = null;
        contextRuntimeKey = null;

        if (Application.isPlaying)
        {
            Destroy(definition);
        }
        else
        {
            DestroyImmediate(definition);
        }
    }

    private bool AreAllObjectivesCompleted()
    {
        for (int i = 0; i < runtimeObjectives.Count; i++)
        {
            if (runtimeObjectives[i] == null || !runtimeObjectives[i].isCompleted)
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateObjective(MissionObjective objective, SatelliteState state)
    {
        if (objective == null)
        {
            return false;
        }

        return objective.objectiveType switch
        {
            MissionObjectiveType.PowerEnabled => state.powerOn,
            MissionObjectiveType.SunDataCollected => state.hasSunData,
            MissionObjectiveType.EarthDataCollected => state.hasEarthData,
            MissionObjectiveType.SatelliteFacingEarth => state.FacingEarth,
            MissionObjectiveType.SatelliteFacingSun => state.FacingSun,
            MissionObjectiveType.SatelliteStabilized => state.isStabilized,
            MissionObjectiveType.PhotoTaken => state.photoTaken,
            MissionObjectiveType.EarthInFrame => state.earthInFrame,
            MissionObjectiveType.DataSent => state.dataSent,
            MissionObjectiveType.BatteryCharged => state.batteryCharge >= (objective.targetValue > 0 ? objective.targetValue : 95f),
            MissionObjectiveType.GyrosCalibrated => state.gyrosCalibrated,
            MissionObjectiveType.CameraCoverOpen => state.cameraCoverOpen,
            MissionObjectiveType.DataCompressed => state.dataCompressed,
            MissionObjectiveType.CommunicationLinkAvailable => state.communicationLinkAvailable,
            _ => false
        };
    }

    private void Render()
    {
        missionPanel?.Render(missionDefinition, runtimeObjectives, IsCompleted);
    }
}
