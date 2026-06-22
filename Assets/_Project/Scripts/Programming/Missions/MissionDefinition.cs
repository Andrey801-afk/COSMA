using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionDefinition", menuName = "COSMA/Mission Definition")]
public sealed class MissionDefinition : ScriptableObject
{
    public string missionName = "New Mission";
    [TextArea(2, 5)] public string missionDescription;
    public List<MissionObjective> requiredObjectives = new();
    public List<CommandDefinition> availableCommands = new();
    [Min(1)] public int maxProgramLines = 13;
}
