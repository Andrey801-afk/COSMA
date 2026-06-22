using System;
using System.Collections.Generic;

[Serializable]
public class UserMission
{
    public string id;
    public string title;
    public string objective;
    public string description;
    public int rewardScience = 25;
    public string sceneName = "SampleScene";
    public string difficulty = "Базовая";
    public List<MissionConditionData> conditions = new List<MissionConditionData>();
    public string createdAt;

    public void EnsureDefaults()
    {
        if (conditions == null)
            conditions = new List<MissionConditionData>();
        if (string.IsNullOrEmpty(difficulty))
            difficulty = "Базовая";
    }
}
