using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class UserMissionStore
{
    private const string CompletedPrefix = "cosma_user_mission_done_";
    private const string FileName = "user_missions.json";

    [Serializable]
    private class Container
    {
        public List<UserMission> missions = new List<UserMission>();
    }

    private static Container _cache;
    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static List<UserMission> LoadAll()
    {
        if (_cache != null) return _cache.missions;

        if (!File.Exists(FilePath))
        {
            _cache = new Container();
            return _cache.missions;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            _cache = JsonUtility.FromJson<Container>(json) ?? new Container();
            if (_cache.missions == null) _cache.missions = new List<UserMission>();
            Normalize(_cache.missions);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"UserMissionStore: failed to load — {e.Message}");
            _cache = new Container();
        }
        return _cache.missions;
    }

    public static void SaveAll()
    {
        if (_cache == null) _cache = new Container();
        try
        {
            var json = JsonUtility.ToJson(_cache, true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"UserMissionStore: failed to save — {e.Message}");
        }
    }

    public static UserMission Add(UserMission mission)
    {
        LoadAll();
        mission.EnsureDefaults();
        if (string.IsNullOrEmpty(mission.id))
            mission.id = "um_" + DateTime.UtcNow.Ticks;
        if (string.IsNullOrEmpty(mission.createdAt))
            mission.createdAt = DateTime.UtcNow.ToString("o");
        _cache.missions.Add(mission);
        SaveAll();
        return mission;
    }

    public static void Update(UserMission mission)
    {
        LoadAll();
        mission.EnsureDefaults();
        var idx = _cache.missions.FindIndex(m => m.id == mission.id);
        if (idx >= 0) _cache.missions[idx] = mission;
        SaveAll();
    }

    public static void Delete(string id)
    {
        LoadAll();
        _cache.missions.RemoveAll(m => m.id == id);
        PlayerPrefs.DeleteKey(CompletedPrefix + id);
        PlayerPrefs.Save();
        SaveAll();
    }

    public static UserMission Find(string id)
    {
        LoadAll();
        return _cache.missions.Find(m => m.id == id);
    }

    public static bool IsCompleted(string id) =>
        !string.IsNullOrEmpty(id) && PlayerPrefs.GetInt(CompletedPrefix + id, 0) == 1;

    public static void MarkCompleted(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        PlayerPrefs.SetInt(CompletedPrefix + id, 1);
        PlayerPrefs.Save();
    }

    private static void Normalize(List<UserMission> missions)
    {
        if (missions == null) return;
        for (int i = 0; i < missions.Count; i++)
            missions[i]?.EnsureDefaults();
    }
}
