using System.Globalization;
using System.IO;
using System.Reflection;
using HarmonyLib;
using SFM_custom_mission_v2.Script;

namespace SFM.CustomMissionsManager.Patches;

[HarmonyPatch(typeof(Engine), nameof(Engine.Initialize))]
public static class V2EnginePatch
{
	private static readonly FieldInfo? MissionFolderField = typeof(Engine).GetField("MissionFolder", BindingFlags.NonPublic | BindingFlags.Static);

	[HarmonyPrefix]
	public static bool Prefix(string rootfolder)
	{
		if (!Directory.Exists(rootfolder))
			return false;

		MissionFolderField?.SetValue(null, rootfolder);

		string[] directories = Directory.GetDirectories(rootfolder);
		foreach (string text in directories)
		{
			string dirName = Path.GetFileName(text);
			if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
			{
				SFM_custom_mission_v2.Plugin.Error("Mission folder can not be a number");
				continue;
			}

			if (Plugin.StateManager.IsMissionEnabled(dirName))
				Engine.programs.Add(new Program(text));
			else
				Plugin.Log.LogInfo($"[CustomMissionsManager] Skipping disabled v2 mission: '{dirName}'");
		}

		return false; // Skip original unhooked Initialize implementation
	}
}
