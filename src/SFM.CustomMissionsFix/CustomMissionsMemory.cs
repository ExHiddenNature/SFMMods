
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System;
using SFMACM = SFM_advanced_custom_mission;
using SFM.CustomMissionsFix;
using System.IO;

public static class CustomMissionsMemory
{
	public class Json
	{
		public int Clears { get; set; }
	};

	public class Info
	{
		public int CurrentCheckpoint = 0;
		public string Filename = "";
		public int Clears = 0;
		public bool CurrentInGameCompleted = false;
	};

	public static Dictionary<SFMACM.CustomMission, Info> Missions = new();
	public static int CurrentSaveSlot = -1;

	public static bool TryGetFileName(SFMACM.CustomMission mission, [NotNullWhen(true)] out string? filename)
	{
		if (!Missions.TryGetValue(mission, out var info))
		{
			filename = null;
			return false;
		}

		filename = info.Filename;
		return true;
	}

	public static bool TryGetSavePathForMission(int slot, SFMACM.CustomMission mission, [NotNullWhen(true)] out string? path)
	{
		if (!TryGetFileName(mission, out var filename))
		{
			path = null;
			return false;
		}

		if (!filename.EndsWith(".json"))
			filename += ".json";

		path = Path.Join(Plugin.SaveDirectoryPath, $"slot_{slot}", filename);
		return true;
	}

	public static SFMACM.CustomMissionCheckpoint GetMissionCurrentCheckpoint(SFMACM.CustomMission mission, int currentCheckpointNum)
	{
		var currentCheckpoint = currentCheckpointNum >= 0 && currentCheckpointNum < mission.missionData.checkpoints.Count ? mission.missionData.checkpoints[currentCheckpointNum] : null;
		if (currentCheckpoint == null)
			currentCheckpoint = mission.missionData.checkpoints[mission.missionData.checkpoints.Count - 1];
		return currentCheckpoint;
	}

	public static bool TryGetMissionCurrentCheckpoint(SFMACM.CustomMission mission, [NotNullWhen(true)] out SFMACM.CustomMissionCheckpoint? currentCheckpoint)
	{
		if (!Missions.TryGetValue(mission, out var info))
		{
			currentCheckpoint = null;
			return false;
		}

		currentCheckpoint = GetMissionCurrentCheckpoint(mission, info.CurrentCheckpoint);
		return true;
	}

	public static bool TryGetMissionName(SFMACM.CustomMission mission, [NotNullWhen(true)] out string? name)
	{
		if (!TryGetMissionCurrentCheckpoint(mission, out var currentCheckpoint))
		{
			name = null;
			return false;
		}

		var description = mission.travelphase ? currentCheckpoint.travelcondition.GetDescription() : currentCheckpoint.condition.GetDescription();
		name = (mission.missionData.addtitleinlist ? (mission.missionData.title + ":\n\r") : "") + description;
		return true;
	}

	public static void SaveAllMissionProgress(int slot)
	{
		Plugin.Log.LogInfo($"Saving all custom mission progress for slot {slot}");

		foreach (var item in Missions)
		{
			try
			{
				if (!TryGetSavePathForMission(slot, item.Key, out var path))
				{
					LogInfo(item.Key, $"Failed to get save path for slot {slot}");
					continue;
				}

				if (!Missions.TryGetValue(item.Key, out var info))
				{
					LogInfo(item.Key, $"Failed to get info for mission for slot {slot}");
					continue;
				}

				LogInfo(item.Key, $"Saving to '{path}'");

				var dirPath = Path.GetDirectoryName(path);
				if (dirPath != null && !Directory.Exists(dirPath))
					Directory.CreateDirectory(dirPath);

				var json = new Json
				{
					Clears = info.Clears
				};
				File.WriteAllText(path, JsonSerializer.Serialize(json));
			}
			catch (Exception ex)
			{
				LogError(item.Key, ex.Message);
				LogError(item.Key, ex.StackTrace ?? "");
			}
		}
	}

	public static void LoadAllMissionProgress(int slot)
	{
		Plugin.Log.LogInfo($"Loading all custom mission progress for slot {slot}");

		foreach (var item in Missions)
		{
			try
			{
				if (!TryGetSavePathForMission(slot, item.Key, out var path))
				{
					LogInfo(item.Key, $"Failed to get save path for slot {slot}");
					continue;
				}

				if (!Missions.TryGetValue(item.Key, out var info))
				{
					LogInfo(item.Key, $"Failed to get info for mission for slot {slot}");
					continue;
				}

				LogInfo(item.Key, $"Loading from '{path}'");
				if (!File.Exists(path))
				{
					LogInfo(item.Key, "File does not exist");
					continue;
				}

				var text = File.ReadAllText(path);
				var json = JsonSerializer.Deserialize<Json>(text);
				if (json == null)
				{
					LogError(item.Key, "Failed to parse JSON");
					continue;
				}

				info.Clears = json.Clears;
				LogInfo(item.Key, $"Successfully loaded saved data for slot {slot}");
			}
			catch (Exception ex)
			{
				LogError(item.Key, ex.Message);
				LogError(item.Key, ex.StackTrace ?? "");
			}
		}
	}

	public static void LogInfo(SFMACM.CustomMission mission, string text)
	{
		// FIXME: Some missions have an empty name?
		if (!TryGetMissionName(mission, out var name))
			name = mission.missionData?.title ?? $"<Unknown Mission {mission}>";
		Plugin.Log.LogInfo($"[{name}] {text}");
	}

	public static void LogError(SFMACM.CustomMission mission, string text)
	{
		if (!TryGetMissionName(mission, out var name))
			name = mission.missionData?.title ?? $"<Unknown Mission {mission}>";
		Plugin.Log.LogError($"[{name}] {text}");
	}
};
