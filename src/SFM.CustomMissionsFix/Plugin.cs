using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SFM.CustomMissionsFix;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("SFM_custom_mission")]
public class Plugin : BasePlugin
{
	public static new ManualLogSource Log = null!;
	public static string SaveDirectoryPath = "";

	public override void Load()
	{
		Log = base.Log;

		SaveDirectoryPath = Path.Combine(Paths.GameRootPath, "CustomMissions1FixSave");
		if (!Directory.Exists(SaveDirectoryPath))
			Directory.CreateDirectory(SaveDirectoryPath);

		var harmony = new Harmony("SFM.CustomMissionsFix");
		harmony.PatchAll();

		// Because we are loaded after the custom missions plugin we have to call reload so we can catch all the CustomMission constructor calls
		Log.LogInfo("Reloading all custom missions");
		SFM_advanced_custom_mission.Plugin.missionManager.ReloadMissions();
	}
}
