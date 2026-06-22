using System;

[Serializable]
public sealed class MissionObjective
{
    public MissionObjectiveType objectiveType;
    public string displayName;
    public int targetValue;
    [NonSerialized] public bool isCompleted;

    public MissionObjective()
    {
    }

    public MissionObjective(MissionObjectiveType type, string name, int targetValue = 0)
    {
        objectiveType = type;
        displayName = name;
        this.targetValue = targetValue;
    }

    public MissionObjective CloneForRuntime()
    {
        return new MissionObjective(objectiveType, displayName, targetValue)
        {
            isCompleted = false
        };
    }
}
