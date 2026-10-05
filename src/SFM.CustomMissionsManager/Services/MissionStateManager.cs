using System;
using System.Collections.Generic;
using System.Linq;
using SFM.CustomMissionsManager.Configuration;

namespace SFM.CustomMissionsManager.Services;

public class MissionStateManager
{
	private readonly ConfigService _configService;
	private readonly MissionReloadService _reloadService;

	public int CurrentSlot { get; private set; } = -1;
	public string ActiveSlotKey => CurrentSlot >= 0 ? $"slot_{CurrentSlot}" : "default";

	// The slot profile currently selected for editing in the UI
	private string _selectedSlotKey = "default";
	public string SelectedSlotKey
	{
		get => _selectedSlotKey;
		set
		{
			if (!string.Equals(_selectedSlotKey, value, StringComparison.OrdinalIgnoreCase))
			{
				_selectedSlotKey = value;
				OnStateChanged?.Invoke();
			}
		}
	}

	public event Action? OnStateChanged;

	public MissionStateManager(ConfigService configService, MissionReloadService reloadService)
	{
		_configService = configService;
		_reloadService = reloadService;
	}

	public ManagerConfig Config => _configService.Config;

	public SaveSlotProfile GetActiveProfile() => Config.GetOrCreateProfile(ActiveSlotKey);

	public SaveSlotProfile GetSelectedProfile() => Config.GetOrCreateProfile(SelectedSlotKey);

	public bool IsMissionEnabled(string missionName, string? slotKey = null)
	{
		var profile = Config.GetOrCreateProfile(slotKey ?? ActiveSlotKey);

		if (profile.ExplicitlyEnabledMissions.Contains(missionName))
			return true;

		foreach (var packName in profile.ActivePacks)
		{
			var pack = Config.Packs.FirstOrDefault(p => string.Equals(p.Name, packName, StringComparison.OrdinalIgnoreCase));
			if (pack != null && pack.Missions.Any(m => string.Equals(m, missionName, StringComparison.OrdinalIgnoreCase)))
				return true;
		}

		return false;
	}

	public bool IsMissionInActivePack(string missionName, string? slotKey = null)
	{
		var profile = Config.GetOrCreateProfile(slotKey ?? ActiveSlotKey);
		foreach (var packName in profile.ActivePacks)
		{
			var pack = Config.Packs.FirstOrDefault(p => string.Equals(p.Name, packName, StringComparison.OrdinalIgnoreCase));
			if (pack != null && pack.Missions.Any(m => string.Equals(m, missionName, StringComparison.OrdinalIgnoreCase)))
				return true;
		}
		return false;
	}

	public bool IsPackActive(string packName, string? slotKey = null)
	{
		var profile = Config.GetOrCreateProfile(slotKey ?? ActiveSlotKey);
		return profile.ActivePacks.Contains(packName);
	}

	public void SetMissionEnabled(string missionName, bool enabled, string? slotKey = null)
	{
		var key = slotKey ?? SelectedSlotKey;
		var profile = Config.GetOrCreateProfile(key);

		if (enabled)
			profile.ExplicitlyEnabledMissions.Add(missionName);
		else
			profile.ExplicitlyEnabledMissions.Remove(missionName);

		_configService.SaveConfig();
		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges && string.Equals(key, ActiveSlotKey, StringComparison.OrdinalIgnoreCase))
			_reloadService.TriggerReloadDebounced();
	}

	public void SetMissionsEnabled(IEnumerable<string> missionNames, bool enabled, string? slotKey = null)
	{
		var key = slotKey ?? SelectedSlotKey;
		var profile = Config.GetOrCreateProfile(key);

		if (enabled)
		{
			foreach (var name in missionNames)
				profile.ExplicitlyEnabledMissions.Add(name);
		}
		else
		{
			foreach (var name in missionNames)
				profile.ExplicitlyEnabledMissions.Remove(name);
		}

		_configService.SaveConfig();
		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges && string.Equals(key, ActiveSlotKey, StringComparison.OrdinalIgnoreCase))
			_reloadService.TriggerReloadDebounced();
	}

	public void DisableAllMissions(string? slotKey = null)
	{
		var key = slotKey ?? SelectedSlotKey;
		var profile = Config.GetOrCreateProfile(key);

		profile.ActivePacks.Clear();
		profile.ExplicitlyEnabledMissions.Clear();

		_configService.SaveConfig();
		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges && string.Equals(key, ActiveSlotKey, StringComparison.OrdinalIgnoreCase))
			_reloadService.TriggerReloadDebounced();
	}

	public void SetPackEnabled(string packName, bool enabled, string? slotKey = null)
	{
		var key = slotKey ?? SelectedSlotKey;
		var profile = Config.GetOrCreateProfile(key);

		if (enabled)
			profile.ActivePacks.Add(packName);
		else
			profile.ActivePacks.Remove(packName);

		_configService.SaveConfig();
		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges && string.Equals(key, ActiveSlotKey, StringComparison.OrdinalIgnoreCase))
			_reloadService.TriggerReloadDebounced();
	}

	public void SwitchActiveSlot(int newSlot)
	{
		if (CurrentSlot == newSlot)
			return;

		Plugin.Log.LogInfo($"[MissionStateManager] Active save slot changed: {CurrentSlot} -> {newSlot}");
		CurrentSlot = newSlot;

		// When active slot changes, also default the UI's selected slot to the new active slot
		SelectedSlotKey = ActiveSlotKey;

		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges)
			_reloadService.TriggerReloadDebounced();
	}

	public void CreatePack(string name, string description = "")
	{
		if (string.IsNullOrWhiteSpace(name))
			return;

		name = name.Trim();
		if (Config.Packs.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
			return;

		Config.Packs.Add(new MissionPack
		{
			Name = name,
			Description = description,
			Missions = new List<string>()
		});

		_configService.SaveConfig();
		OnStateChanged?.Invoke();
	}

	public void DeletePack(string name)
	{
		var pack = Config.Packs.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
		if (pack != null)
		{
			Config.Packs.Remove(pack);
			foreach (var profile in Config.Profiles.Values)
				profile.ActivePacks.Remove(name);

			_configService.SaveConfig();
			OnStateChanged?.Invoke();

			if (Config.AutoReloadOnChanges)
				_reloadService.TriggerReloadDebounced();
		}
	}

	public void ToggleMissionInPack(string packName, string missionName)
	{
		var pack = Config.Packs.FirstOrDefault(p => string.Equals(p.Name, packName, StringComparison.OrdinalIgnoreCase));
		if (pack == null)
			return;

		int index = pack.Missions.FindIndex(m => string.Equals(m, missionName, StringComparison.OrdinalIgnoreCase));
		if (index >= 0)
			pack.Missions.RemoveAt(index);
		else
			pack.Missions.Add(missionName);

		_configService.SaveConfig();
		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges && IsPackActive(packName, ActiveSlotKey))
			_reloadService.TriggerReloadDebounced();
	}

	public void CopyProfile(string fromSlotKey, string toSlotKey)
	{
		var source = Config.GetOrCreateProfile(fromSlotKey);
		Config.Profiles[toSlotKey] = source.Clone();
		_configService.SaveConfig();
		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges && string.Equals(toSlotKey, ActiveSlotKey, StringComparison.OrdinalIgnoreCase))
			_reloadService.TriggerReloadDebounced();
	}

	public void ResetProfile(string slotKey)
	{
		Config.Profiles[slotKey] = new SaveSlotProfile();
		_configService.SaveConfig();
		OnStateChanged?.Invoke();

		if (Config.AutoReloadOnChanges && string.Equals(slotKey, ActiveSlotKey, StringComparison.OrdinalIgnoreCase))
			_reloadService.TriggerReloadDebounced();
	}
}
