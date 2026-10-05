using System;
using System.IO;
using HarmonyLib;
using SFM_advanced_custom_mission;

namespace SFM.CustomMissionsManager.Patches;

[HarmonyPatch(typeof(CustomMissionManager), nameof(CustomMissionManager.LoadMission))]
public static class V1CustomMissionPatch
{
	[HarmonyPrefix]
	public static bool Prefix(string filename)
	{
		var missionName = GetMissionDirectoryName(filename);
		if (!Plugin.StateManager.IsMissionEnabled(missionName))
		{
			Plugin.Log.LogInfo($"[CustomMissionsManager] Skipping disabled v1 mission: '{missionName}'");
			return false;
		}

		return true;
	}

	public static string GetMissionDirectoryName(string filename)
	{
		var normalized = filename.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
		try
		{
			var v1Dir = Plugin.Registry.V1Directory;
			var relative = Path.GetRelativePath(v1Dir, normalized);
			if (!relative.StartsWith("..") && !Path.IsPathRooted(relative))
			{
				var parts = relative.Split(Path.DirectorySeparatorChar);
				if (parts.Length > 1)
					return parts[0];
				return Path.GetFileNameWithoutExtension(parts[0]);
			}
		}
		catch
		{
		}

		var parentDir = Path.GetDirectoryName(normalized);
		if (!string.IsNullOrEmpty(parentDir))
		{
			var dirName = Path.GetFileName(parentDir);
			if (!string.Equals(dirName, "CustomMissions", StringComparison.OrdinalIgnoreCase))
				return dirName;
		}

		return Path.GetFileNameWithoutExtension(normalized);
	}
}
