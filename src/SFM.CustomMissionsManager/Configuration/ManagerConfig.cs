using System;
using System.Collections.Generic;

namespace SFM.CustomMissionsManager.Configuration;

public class ManagerConfig
{
	public int Version { get; set; } = 1;
	public bool AutoReloadOnChanges { get; set; } = true;
	public string ToggleHotkey { get; set; } = "F10";
	public List<MissionPack> Packs { get; set; } = new();
	public Dictionary<string, SaveSlotProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	public SaveSlotProfile GetOrCreateProfile(string slotKey)
	{
		if (!Profiles.TryGetValue(slotKey, out var profile))
		{
			// If not found, clone from default profile if available, or create empty
			if (Profiles.TryGetValue("default", out var defaultProfile))
				profile = defaultProfile.Clone();
			else
				profile = new SaveSlotProfile();
			Profiles[slotKey] = profile;
		}

		return profile;
	}
}
