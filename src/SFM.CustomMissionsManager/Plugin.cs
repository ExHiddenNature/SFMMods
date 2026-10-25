using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UniverseLib;
using UniverseLib.Config;
using UniverseLib.UI;
using SFM.CustomMissionsManager.Configuration;
using SFM.CustomMissionsManager.Services;
using SFM.CustomMissionsManager.UI;

namespace SFM.CustomMissionsManager;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("SFM_custom_mission", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("SFM_custom_mission_v2", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("SFM.CustomMissionsFix", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
	public static new ManualLogSource Log = null!;
	public static ConfigService ConfigService = null!;
	public static MissionRegistry Registry = null!;
	public static MissionReloadService ReloadService = null!;
	public static MissionStateManager StateManager = null!;

	public static UIBase UIBase = null!;
	public static MissionManagerPanel Panel = null!;

	public static KeyCode Hotkey = KeyCode.F10;

	public override void Load()
	{
		Log = base.Log;
		Log.LogInfo($"[SFM Custom Missions Manager] v{MyPluginInfo.PLUGIN_VERSION} initializing...");

		ConfigService = new ConfigService();
		ReloadService = new MissionReloadService();
		StateManager = new MissionStateManager(ConfigService, ReloadService);
		Registry = new MissionRegistry();

		if (ConfigService.IsFirstRun)
		{
			var defaultProfile = ConfigService.Config.GetOrCreateProfile("default");
			foreach (var mission in Registry.AllMissions)
				defaultProfile.ExplicitlyEnabledMissions.Add(mission.DirectoryName);

			ConfigService.SaveConfig();
			Log.LogInfo($"[SFM Custom Missions Manager] First run: enabled {Registry.AllMissions.Count} missions by default in default profile.");
		}

		if (Enum.TryParse<KeyCode>(ConfigService.Config.ToggleHotkey, true, out var parsedKey))
			Hotkey = parsedKey;

		var harmony = new Harmony("SFM.CustomMissionsManager");
		harmony.PatchAll();

		var universeConfig = new UniverseLibConfig
		{
			Force_Unlock_Mouse = false,
			Disable_EventSystem_Override = true,
			Allow_UI_Selection_Outside_UIBase = true
		};

		Universe.Init(1f, OnUniverseLibInitialized, (msg, type) =>
		{
			if (type == LogType.Error || type == LogType.Exception)
				Log.LogError($"[UniverseLib] {msg}");
			else if (type == LogType.Warning)
				Log.LogWarning($"[UniverseLib] {msg}");
		}, universeConfig);

		Log.LogInfo("[SFM Custom Missions Manager] Initialized successfully.");
	}

	private void OnUniverseLibInitialized()
	{
		Log.LogInfo("[SFM Custom Missions Manager] Initializing in-game UniverseLib UI...");
		try
		{
			UIBase = UniversalUI.RegisterUI(MyPluginInfo.PLUGIN_GUID, null);
			Panel = new MissionManagerPanel(UIBase);
			Panel.SetActive(ConfigService.IsFirstRun); // Start visible on first run, otherwise hide on launch
			Log.LogInfo($"[SFM Custom Missions Manager] UI registered. Press {Hotkey} to toggle manager window.");
		}
		catch (Exception ex)
		{
			Log.LogError($"[SFM Custom Missions Manager] Failed to initialize UI: {ex}");
		}
	}

	public static void OnGameUpdate()
	{
		if (UniverseLib.Input.InputManager.Rebinding)
			return;

		ReloadService?.Update();

		if (UniverseLib.Input.InputManager.GetKeyDown(Hotkey))
		{
			if (Panel != null)
				Panel.SetActive(!Panel.Enabled);
		}
	}
}
