namespace SFM.CustomMissionsManager.Models;

public class MissionInfo
{
	public string DirectoryName { get; set; } = string.Empty;
	public string FullPath { get; set; } = string.Empty;
	public MissionType Type { get; set; } = MissionType.Unknown;
	public string? Title { get; set; }
	public string? Description { get; set; }

	public string DisplayName => !string.IsNullOrWhiteSpace(Title) ? Title : DirectoryName;
	public string Subtitle => !string.IsNullOrWhiteSpace(Title) && !string.Equals(Title, DirectoryName, System.StringComparison.OrdinalIgnoreCase) ? DirectoryName : string.Empty;
}
