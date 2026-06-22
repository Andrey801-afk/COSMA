public static class MissionContext
{
    public static Mission Current;
    public static UserMission CurrentUserMission;
    public static Mission[] MissionSequence;

    public static bool HasAny =>
        Current != null || CurrentUserMission != null;

    public static string CurrentId =>
        Current != null ? Current.id :
        CurrentUserMission != null ? CurrentUserMission.id : null;

    public static string CurrentTitle =>
        Current != null ? Current.title :
        CurrentUserMission != null ? CurrentUserMission.title : "";

    public static string CurrentObjective =>
        Current != null ? Current.objective :
        CurrentUserMission != null ? CurrentUserMission.objective : "";

    public static bool IsUserMade => CurrentUserMission != null;

    public static void Clear()
    {
        Current = null;
        CurrentUserMission = null;
        MissionSequence = null;
    }

    public static void StartMission(Mission mission, Mission[] sequence = null)
    {
        Mission[] nextSequence = sequence ?? MissionSequence;
        Current = mission;
        CurrentUserMission = null;
        MissionSequence = nextSequence;

        if (mission != null)
        {
            MissionProgress.LastPlayedId = mission.id;
        }
    }

    public static void StartUserMission(UserMission mission)
    {
        Current = null;
        CurrentUserMission = mission;
        MissionSequence = null;

        if (mission != null)
        {
            MissionProgress.LastPlayedId = mission.id;
        }
    }

    public static Mission GetNextAvailableMission()
    {
        if (Current == null || MissionSequence == null || MissionSequence.Length == 0)
        {
            return null;
        }

        int currentIndex = -1;
        for (int i = 0; i < MissionSequence.Length; i++)
        {
            Mission mission = MissionSequence[i];
            if (mission == Current || (mission != null && mission.id == Current.id))
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
        {
            return null;
        }

        for (int i = currentIndex + 1; i < MissionSequence.Length; i++)
        {
            Mission mission = MissionSequence[i];
            if (mission != null &&
                !MissionProgress.IsCompleted(mission.id) &&
                MissionProgress.GetStatus(mission) == MissionStatus.Available)
            {
                return mission;
            }
        }

        return null;
    }

    public static void MarkCurrentCompleted()
    {
        if (Current != null) MissionProgress.MarkCompleted(Current.id);
        if (CurrentUserMission != null) UserMissionStore.MarkCompleted(CurrentUserMission.id);
    }
}
