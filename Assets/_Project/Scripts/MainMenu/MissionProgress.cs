using UnityEngine;

public static class MissionProgress
{
    private const string CompletedPrefix = "cosma_mission_done_";
    private const string LastPlayedKey = "cosma_last_mission";

    public static bool IsCompleted(string missionId)
    {
        if (string.IsNullOrEmpty(missionId)) return false;
        return PlayerPrefs.GetInt(CompletedPrefix + missionId, 0) == 1;
    }

    public static void MarkCompleted(string missionId)
    {
        if (string.IsNullOrEmpty(missionId)) return;
        PlayerPrefs.SetInt(CompletedPrefix + missionId, 1);
        PlayerPrefs.Save();
    }

    public static MissionStatus GetStatus(Mission mission)
    {
        if (mission == null) return MissionStatus.Locked;
        if (IsCompleted(mission.id)) return MissionStatus.Completed;

        if (mission.prerequisites != null)
        {
            foreach (var prereq in mission.prerequisites)
            {
                if (prereq != null && !IsCompleted(prereq.id))
                    return MissionStatus.Locked;
            }
        }
        return MissionStatus.Available;
    }

    public static string LastPlayedId
    {
        get => PlayerPrefs.GetString(LastPlayedKey, string.Empty);
        set { PlayerPrefs.SetString(LastPlayedKey, value); PlayerPrefs.Save(); }
    }

    public static bool HasAnyProgress => !string.IsNullOrEmpty(LastPlayedId);
}
