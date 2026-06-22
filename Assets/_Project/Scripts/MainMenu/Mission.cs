using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "COSMA/Mission")]
public class Mission : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string title;

    [Header("Briefing")]
    [TextArea(3, 6)] public string description;
    [TextArea(2, 4)] public string objective;

    [Header("Reward")]
    public int rewardScience;

    [Header("Gating")]
    public Mission[] prerequisites;

    [Header("Launch")]
    public string sceneName = "SampleScene";
}
