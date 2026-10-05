using System;
using UnityEngine;

namespace SFM.CustomMissionsManager.Services;

public class MissionReloadService
{
	private float _reloadTimer = -1f;

	public void TriggerReloadDebounced(float delaySeconds = 0.35f)
	{
		_reloadTimer = delaySeconds;
	}

	public void Update()
	{
		if (_reloadTimer > 0f)
		{
			_reloadTimer -= Time.unscaledDeltaTime;
			if (_reloadTimer <= 0f)
			{
				_reloadTimer = -1f;
				ReloadAll();
			}
		}
	}

	public void ReloadAll()
	{
		Plugin.Log.LogInfo("[MissionReloadService] Reloading custom missions...");
		ReloadV1();
		ReloadV2();
	}

	public bool ReloadV1()
	{
		try
		{
			if (SFM_advanced_custom_mission.Plugin.missionManager != null)
			{
				SFM_advanced_custom_mission.Plugin.missionManager.ReloadMissions();
				Plugin.Log.LogInfo("[MissionReloadService] V1 missions reloaded successfully.");
				return true;
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[MissionReloadService] Failed to reload v1 missions: {ex.Message}");
		}
		return false;
	}

	public bool ReloadV2()
	{
		try
		{
			SFM_custom_mission_v2.Script.Engine.Reload();
			Plugin.Log.LogInfo("[MissionReloadService] V2 missions reloaded successfully.");
			return true;
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[MissionReloadService] Failed to reload v2 missions: {ex.Message}");
		}
		return false;
	}
}
