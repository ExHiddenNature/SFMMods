using System;
using System.Collections.Generic;

namespace SFM.CustomMissionsManager.Configuration;

public class SaveSlotProfile
{
	public HashSet<string> ActivePacks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> ExplicitlyEnabledMissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	public SaveSlotProfile Clone()
	{
		return new SaveSlotProfile
		{
			ActivePacks = new HashSet<string>(ActivePacks, StringComparer.OrdinalIgnoreCase),
			ExplicitlyEnabledMissions = new HashSet<string>(ExplicitlyEnabledMissions, StringComparer.OrdinalIgnoreCase)
		};
	}
}
