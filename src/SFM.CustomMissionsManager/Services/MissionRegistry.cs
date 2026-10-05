using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx;
using SFM.CustomMissionsManager.Models;

namespace SFM.CustomMissionsManager.Services;

public class MissionRegistry
{
	private static readonly JsonDocumentOptions RelaxedJsonOptions = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	public string V1Directory { get; }
	public string V2Directory { get; }

	public List<MissionInfo> AllMissions { get; private set; } = new();

	public MissionRegistry()
	{
		V1Directory = Path.Combine(Paths.GameRootPath, "CustomMissions");
		V2Directory = Path.Combine(Paths.GameRootPath, "CustomMissions2");

		EnsureDirectoriesExist();
		Rescan();
	}

	public void EnsureDirectoriesExist()
	{
		try
		{
			if (!Directory.Exists(V1Directory))
				Directory.CreateDirectory(V1Directory);
			if (!Directory.Exists(V2Directory))
				Directory.CreateDirectory(V2Directory);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[MissionRegistry] Failed to ensure directories: {ex.Message}");
		}
	}

	public void Rescan()
	{
		var list = new List<MissionInfo>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (Directory.Exists(V1Directory))
		{
			// Directories in CustomMissions root
			foreach (var dir in Directory.GetDirectories(V1Directory))
			{
				var dirName = Path.GetFileName(dir);
				if (dirName.StartsWith('_'))
					continue;

				var mission = ScanV1Directory(dir, dirName);
				if (mission != null)
				{
					list.Add(mission);
					seen.Add($"v1:{dirName}");
				}
			}

			// Standalone json files in CustomMissions root
			foreach (var file in Directory.GetFiles(V1Directory, "*.json", SearchOption.TopDirectoryOnly))
			{
				var fileName = Path.GetFileName(file);
				if (fileName.StartsWith('_'))
					continue;

				var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
				if (seen.Contains($"v1:{nameNoExt}"))
					continue;

				var mission = ScanV1File(file, nameNoExt);
				if (mission != null)
				{
					list.Add(mission);
					seen.Add($"v1:{nameNoExt}");
				}
			}
		}

		if (Directory.Exists(V2Directory))
		{
			// Directories in CustomMissions2 root
			foreach (var dir in Directory.GetDirectories(V2Directory))
			{
				var dirName = Path.GetFileName(dir);
				if (dirName.StartsWith('_'))
					continue;

				var mission = ScanV2Directory(dir, dirName);
				if (mission != null)
				{
					list.Add(mission);
					seen.Add($"v2:{dirName}");
				}
			}
		}

		AllMissions = list.OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
		Plugin.Log.LogInfo($"[MissionRegistry] Scanned {AllMissions.Count} missions (v1: {AllMissions.Count(m => m.Type == MissionType.V1)}, v2: {AllMissions.Count(m => m.Type == MissionType.V2)})");
	}

	private MissionInfo? ScanV1Directory(string dirPath, string dirName)
	{
		string? title = null;
		string? description = null;
		bool hasJson = false;

		try
		{
			var jsonFiles = Directory.GetFiles(dirPath, "*.json", SearchOption.AllDirectories);
			foreach (var jsonPath in jsonFiles)
			{
				var fileName = Path.GetFileName(jsonPath);
				if (fileName.StartsWith('_') || string.Equals(fileName, "meta.json", StringComparison.OrdinalIgnoreCase))
					continue;

				if (TryExtractV1Meta(jsonPath, out var jsonTitle, out var jsonDesc))
				{
					hasJson = true;
					if (string.IsNullOrEmpty(title) && !string.IsNullOrWhiteSpace(jsonTitle))
						title = jsonTitle;
					if (string.IsNullOrEmpty(description) && !string.IsNullOrWhiteSpace(jsonDesc))
						description = jsonDesc;
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning($"[MissionRegistry] Error scanning v1 directory {dirName}: {ex.Message}");
		}

		if (!hasJson)
			return null;

		return new MissionInfo
		{
			DirectoryName = dirName,
			FullPath = Path.GetFullPath(dirPath),
			Type = MissionType.V1,
			Title = title,
			Description = description
		};
	}

	private MissionInfo? ScanV1File(string filePath, string name)
	{
		if (TryExtractV1Meta(filePath, out var title, out var desc))
		{
			return new MissionInfo
			{
				DirectoryName = name,
				FullPath = Path.GetFullPath(filePath),
				Type = MissionType.V1,
				Title = title,
				Description = desc
			};
		}
		return null;
	}

	private static bool TryExtractV1Meta(string jsonPath, out string? title, out string? desc)
	{
		title = null;
		desc = null;
		try
		{
			var content = File.ReadAllText(jsonPath);
			using var doc = JsonDocument.Parse(content, RelaxedJsonOptions);
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
				return false;

			var root = doc.RootElement;
			if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
				title = titleProp.GetString();

			if (root.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
				desc = descProp.GetString();

			bool hasZones = root.TryGetProperty("zones", out var zones) && zones.ValueKind == JsonValueKind.Array;
			bool hasCheckpoints = root.TryGetProperty("checkpoints", out var checkpoints) && checkpoints.ValueKind == JsonValueKind.Array;
			bool hasSubconditions = root.TryGetProperty("subconditions", out _);
			bool hasListMission = root.TryGetProperty("listmission", out _);

			return hasZones || hasCheckpoints || hasSubconditions || hasListMission || !string.IsNullOrEmpty(title);
		}
		catch
		{
			return false;
		}
	}

	private MissionInfo? ScanV2Directory(string dirPath, string dirName)
	{
		try
		{
			var codeFiles = Directory.GetFiles(dirPath, "*.code", SearchOption.TopDirectoryOnly);
			if (codeFiles.Length == 0)
				return null;

			string? title = null;
			string? description = null;

			var metaPath = Path.Combine(dirPath, "meta.json");
			if (File.Exists(metaPath))
			{
				try
				{
					using var doc = JsonDocument.Parse(File.ReadAllText(metaPath), RelaxedJsonOptions);
					var root = doc.RootElement;
					if (root.TryGetProperty("title", out var titleProp))
					{
						if (titleProp.ValueKind == JsonValueKind.Object && titleProp.TryGetProperty("En", out var enTitle))
							title = enTitle.GetString();
						else if (titleProp.ValueKind == JsonValueKind.String)
							title = titleProp.GetString();
					}

					if (root.TryGetProperty("description", out var descProp))
					{
						if (descProp.ValueKind == JsonValueKind.Object && descProp.TryGetProperty("En", out var enDesc))
							description = enDesc.GetString();
						else if (descProp.ValueKind == JsonValueKind.String)
							description = descProp.GetString();
					}
				}
				catch (Exception ex)
				{
					Plugin.Log.LogWarning($"[MissionRegistry] Error reading meta.json in v2 directory {dirName}: {ex.Message}");
				}
			}

			return new MissionInfo
			{
				DirectoryName = dirName,
				FullPath = Path.GetFullPath(dirPath),
				Type = MissionType.V2,
				Title = title,
				Description = description
			};
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning($"[MissionRegistry] Error scanning v2 directory {dirName}: {ex.Message}");
			return null;
		}
	}

	public MissionInfo? FindMission(string dirName, MissionType type)
	{
		return AllMissions.FirstOrDefault(m => m.Type == type && string.Equals(m.DirectoryName, dirName, StringComparison.OrdinalIgnoreCase));
	}
}
