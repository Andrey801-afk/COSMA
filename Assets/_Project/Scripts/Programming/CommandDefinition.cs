using UnityEngine;

[CreateAssetMenu(fileName = "CommandDefinition", menuName = "COSMA/Command Definition")]
public sealed class CommandDefinition : ScriptableObject
{
    public CommandType Type;
    public string DisplayName;
    [TextArea(2, 4)] public string Description;
    public Sprite Icon;
    public Color AccentColor = new(0.25f, 0.95f, 1f, 1f);
    [Min(1)] public int DefaultTargetLine = 1;
    [Min(0f)] public float DefaultWaitSeconds = 1f;
    public CommandConditionType DefaultCondition = CommandConditionType.PowerOn;
}
