using System;
using System.IO;
using System.Text.Json;
using BepInEx;

namespace SFM.CustomMissionsManager.Configuration;

public class ConfigService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip
	};

	public string ConfigFilePath { get; }
	public ManagerConfig Config { get; private set; }
	public bool IsFirstRun { get; private set; }

	public ConfigService()
	{
		ConfigFilePath = Path.Combine(Paths.ConfigPath, "SFM.CustomMissionsManager.json");
		IsFirstRun = !File.Exists(ConfigFilePath);
		Config = LoadConfig();
	}

	public ManagerConfig LoadConfig()
	{
		try
		{
			if (File.Exists(ConfigFilePath))
			{
				var json = File.ReadAllText(ConfigFilePath);
				var config = JsonSerializer.Deserialize<ManagerConfig>(json, JsonOptions);
				if (config != null)
				{
					if (!config.Profiles.ContainsKey("default"))
						config.Profiles["default"] = new SaveSlotProfile();
					return config;
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[ConfigService] Error loading config from {ConfigFilePath}: {ex.Message}");
		}

		var newConfig = new ManagerConfig();
		newConfig.Profiles["default"] = new SaveSlotProfile();
		return newConfig;
	}

	public void SaveConfig()
	{
		try
		{
			var dir = Path.GetDirectoryName(ConfigFilePath);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			var json = JsonSerializer.Serialize(Config, JsonOptions);
			File.WriteAllText(ConfigFilePath, json);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[ConfigService] Error saving config to {ConfigFilePath}: {ex.Message}");
		}
	}
}
