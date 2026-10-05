using System.Collections.Generic;

namespace SFM.CustomMissionsManager.Configuration;

public class MissionPack
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<string> Missions { get; set; } = new();
}
