using System;
using UnityEngine;

[Serializable]
public sealed class ProgramCommand
{
    [SerializeField] private CommandDefinition definition;
    [SerializeField] private CommandType commandType;
    [SerializeField, Min(1)] private int targetLineNumber = 1;
    [SerializeField] private EarthFacingSide earthFacingSide = EarthFacingSide.Camera;
    [SerializeField, Min(0f)] private float waitSeconds = 1f;
    [SerializeField] private CommandConditionType condition = CommandConditionType.PowerOn;

    public CommandDefinition Definition => definition;
    public CommandType CommandType => commandType;

    public int TargetLineNumber
    {
        get => Mathf.Max(1, targetLineNumber);
        set => targetLineNumber = Mathf.Max(1, value);
    }

    public EarthFacingSide EarthFacingSide
    {
        get => earthFacingSide;
        set => earthFacingSide = value;
    }

    public float WaitSeconds
    {
        get => Mathf.Max(0f, waitSeconds);
        set => waitSeconds = Mathf.Max(0f, value);
    }

    public CommandConditionType Condition
    {
        get => condition;
        set => condition = value;
    }

    public string DisplayName => definition != null ? definition.DisplayName : commandType.ToString();
    public string Description => definition != null ? definition.Description : string.Empty;
    public Color AccentColor => definition != null ? definition.AccentColor : Color.white;

    public static ProgramCommand FromDefinition(CommandDefinition source)
    {
        if (source == null)
        {
            return null;
        }

        return new ProgramCommand
        {
            definition = source,
            commandType = source.Type,
            targetLineNumber = Mathf.Max(1, source.DefaultTargetLine),
            earthFacingSide = EarthFacingSide.Camera,
            waitSeconds = Mathf.Max(0.1f, source.DefaultWaitSeconds),
            condition = source.DefaultCondition
        };
    }
}
